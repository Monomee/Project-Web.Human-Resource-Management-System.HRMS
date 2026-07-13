using Microsoft.EntityFrameworkCore;
using HRMS.Application.DTOs.Attendance;
using HRMS.Application.Interfaces;
using HRMS.Domain.Entities;
using HRMS.Infrastructure.Persistence;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Dịch vụ nghiệp vụ chấm công: nhận file Excel, tính toán ngày công, lưu vào DB.
///
/// LƯU Ý KIẾN TRÚC: Lớp này đặt ở Infrastructure vì cần truy cập ApplicationDbContext.
/// Nó implement IAttendanceService được định nghĩa ở tầng Application.
/// </summary>
public class AttendanceService : IAttendanceService
{
    // ────────────────────────────────────────────────────────────────
    // KHUNG GIỜ LÀM VIỆC CỐ ĐỊNH CỦA CÔNG TY
    // Intern đọc: đây là "thước đo" để so sánh giờ quẹt thẻ thực tế.
    // ────────────────────────────────────────────────────────────────
    private static readonly TimeOnly IN_STANDARD  = new TimeOnly(8, 0);   // 08:00 sáng
    private static readonly TimeOnly OUT_STANDARD = new TimeOnly(17, 30); // 17:30 chiều
    private static readonly TimeOnly NOON         = new TimeOnly(12, 0);  // 12:00 trưa (mốc nửa ngày)

    private readonly ApplicationDbContext _db;
    private readonly IExcelParserService  _excelParser;

    public AttendanceService(ApplicationDbContext db, IExcelParserService excelParser)
    {
        _db          = db;
        _excelParser = excelParser;
    }

    // ════════════════════════════════════════════════════════════════
    // 1. LẤY DANH SÁCH KỲ CÔNG (cho dropdown trên UI)
    // ════════════════════════════════════════════════════════════════
    public async Task<List<TimesheetPeriodDto>> GetPeriodsAsync()
    {
        return await _db.TimesheetPeriods
            .OrderByDescending(p => p.StartDate)
            .Select(p => new TimesheetPeriodDto
            {
                Id        = p.Id,
                Name      = p.Name,
                StartDate = p.StartDate,
                EndDate   = p.EndDate,
                IsLocked  = p.IsLocked
            })
            .ToListAsync();
    }

    // ════════════════════════════════════════════════════════════════
    // 2. KHÓA KỲ CÔNG
    // ════════════════════════════════════════════════════════════════
    public async Task LockPeriodAsync(int periodId)
    {
        var period = await _db.TimesheetPeriods.FindAsync(periodId)
            ?? throw new InvalidOperationException($"Không tìm thấy kỳ công ID={periodId}.");

        period.IsLocked = true;
        await _db.SaveChangesAsync();
    }

    // ════════════════════════════════════════════════════════════════
    // 3. IMPORT VÀ TÍNH TOÁN NGÀY CÔNG — HÀM CHÍNH
    // ════════════════════════════════════════════════════════════════
    public async Task<List<AttendanceImportResultDto>> ImportAndSaveAsync(Stream fileStream, int periodId)
    {
        // ── KIỂM TRA KỲ CÔNG ────────────────────────────────────────
        var period = await _db.TimesheetPeriods.FindAsync(periodId)
            ?? throw new InvalidOperationException($"Kỳ công ID={periodId} không tồn tại.");

        if (period.IsLocked)
            throw new InvalidOperationException($"Kỳ công '{period.Name}' đã bị khóa. Không thể import thêm.");

        // ── BƯỚC 1: ĐỌC FILE EXCEL → DANH SÁCH DÒNG THÔ ─────────────
        // ExcelParserService đọc từng dòng Excel, map vào AttendanceRowDto
        var rawRows = await _excelParser.ParseAsync(fileStream);

        if (!rawRows.Any())
            return new List<AttendanceImportResultDto>();

        // ── BƯỚC 1.5: KIỂM TRA PHẠM VI NGÀY CỦA DỮ LIỆU CÓ PHÙ HỢP KỲ CÔNG KHÔNG ─────────
        foreach (var row in rawRows)
        {
            var checkedDate = DateOnly.FromDateTime(row.CheckedAt);
            if (checkedDate < period.StartDate || checkedDate > period.EndDate)
            {
                throw new InvalidOperationException(
                    $"Dữ liệu quẹt thẻ ngày {checkedDate:dd/MM/yyyy} (của nhân viên {row.EmployeeCode}) không nằm trong kỳ công đang chọn '{period.Name}' ({period.StartDate:dd/MM/yyyy} - {period.EndDate:dd/MM/yyyy}).");
            }
        }

        // ── BƯỚC 2: TẢI DỮ LIỆU NHÂN VIÊN TỪ DB ────────────────────
        // Lấy tất cả mã nhân viên xuất hiện trong file Excel
        var employeeCodes = rawRows.Select(r => r.EmployeeCode).Distinct().ToList();

        // Tra cứu 1 lần từ DB → tránh query DB trong vòng lặp (N+1 problem)
        var userDict = await _db.Users
            .Where(u => employeeCodes.Contains(u.EmployeeCode))
            .ToDictionaryAsync(u => u.EmployeeCode);

        // ── BƯỚC 3: GOM NHÓM THEO (EmployeeCode, Ngày làm việc) ──────
        //
        // Giải thích cho Intern: Một nhân viên trong 1 ngày có thể quẹt thẻ
        // nhiều lần (quẹt IN lúc 08:02, rồi ra ngoài quẹt OUT lúc 12:00,
        // quẹt IN lại lúc 13:00, rồi quẹt OUT cuối cùng lúc 17:45).
        // Ta cần gom tất cả các lần quẹt của 1 người trong 1 ngày lại,
        // rồi lấy: CheckIn = lần IN SỚM NHẤT, CheckOut = lần OUT MUỘN NHẤT.
        var grouped = rawRows
            .GroupBy(r => (r.EmployeeCode, WorkDate: DateOnly.FromDateTime(r.CheckedAt)));

        // ── BƯỚC 4: TÍNH TOÁN VÀ CHUẨN BỊ KẾT QUẢ ─────────────────
        var results      = new List<AttendanceImportResultDto>();
        var logsToInsert = new List<AttendanceLog>();

        foreach (var group in grouped)
        {
            var (employeeCode, workDate) = group.Key;

            // Kiểm tra mã nhân viên có tồn tại trong hệ thống không
            if (!userDict.TryGetValue(employeeCode, out var user))
            {
                // Mã nhân viên không tồn tại → đánh dấu lỗi, bỏ qua
                results.Add(new AttendanceImportResultDto
                {
                    EmployeeCode = employeeCode,
                    EmployeeName = "??? (Không tìm thấy)",
                    WorkDate     = workDate,
                    HasError     = true,
                    Note         = $"Mã nhân viên '{employeeCode}' không tồn tại trong hệ thống."
                });
                continue;
            }

            // Lấy lần IN sớm nhất và lần OUT muộn nhất trong ngày
            var inRecords  = group.Where(r => r.CheckType == "IN").ToList();
            var outRecords = group.Where(r => r.CheckType == "OUT").ToList();

            TimeOnly? checkIn  = inRecords.Any()
                ? TimeOnly.FromDateTime(inRecords.Min(r => r.CheckedAt))
                : null;

            TimeOnly? checkOut = outRecords.Any()
                ? TimeOnly.FromDateTime(outRecords.Max(r => r.CheckedAt))
                : null;

            // ── THUẬT TOÁN TÍNH CÔNG ────────────────────────────────
            // Intern đọc kỹ phần này — đây là bản chất nghiệp vụ chấm công.
            var (workValue, lateMinutes, note) = CalculateAttendance(checkIn, checkOut);
            // ────────────────────────────────────────────────────────

            results.Add(new AttendanceImportResultDto
            {
                EmployeeCode = employeeCode,
                EmployeeName = user.FullName,
                WorkDate     = workDate,
                CheckIn      = checkIn,
                CheckOut     = checkOut,
                LateMinutes  = lateMinutes,
                WorkValue    = workValue,
                Note         = note,
                HasError     = false
            });

            // Chuẩn bị bản ghi để lưu vào DB (mỗi dòng Excel gốc = 1 AttendanceLog)
            foreach (var row in group)
            {
                logsToInsert.Add(new AttendanceLog
                {
                    UserId    = user.Id,
                    PeriodId  = periodId,
                    CheckedAt = row.CheckedAt,
                    CheckType = row.CheckType,
                    Source    = "Excel"         // Đánh dấu nguồn gốc dữ liệu
                });
            }
        }

        // ── BƯỚC 5: LƯU TẤT CẢ VÀO DB TRONG 1 LẦN ─────────────────
        // Dùng AddRangeAsync để bulk insert, tiết kiệm số lần round-trip đến DB
        if (logsToInsert.Any())
        {
            await _db.AttendanceLogs.AddRangeAsync(logsToInsert);
            await _db.SaveChangesAsync();
        }

        return results;
    }

    // ════════════════════════════════════════════════════════════════
    // THUẬT TOÁN TÍNH NGÀY CÔNG — PRIVATE HELPER
    //
    // Trả về: (workValue, lateMinutes, note)
    //
    // ┌─────────────────────────────────────────────────────────────┐
    // │  BẢNG QUY TẮC TÍNH CÔNG                                    │
    // ├────────────────┬──────────────┬───────────────────────────  │
    // │  Điều kiện     │  WorkValue   │  Ý nghĩa                    │
    // ├────────────────┼──────────────┼───────────────────────────  │
    // │  Không có IN   │     0.0      │  Vắng mặt cả ngày           │
    // │  Có IN, ko OUT │     0.5      │  Chỉ quẹt vào, không ra     │
    // │  OUT < 12:00   │     0.5      │  Chỉ làm buổi sáng          │
    // │  OUT >= 17:30  │     1.0      │  Đủ ngày công               │
    // │  12:00 ≤ OUT   │     0.5      │  Về sớm (làm đến trưa)      │
    // │    < 17:30     │              │                              │
    // └─────────────────────────────────────────────────────────────┘
    // ════════════════════════════════════════════════════════════════
    private static (double workValue, int lateMinutes, string note)
        CalculateAttendance(TimeOnly? checkIn, TimeOnly? checkOut)
    {
        // ── TRƯỜNG HỢP 1: VẮNG MẶT ──────────────────────────────────
        // Không có bất kỳ bản ghi quẹt IN nào → nhân viên không đến làm
        if (checkIn == null)
            return (0.0, 0, "Vắng mặt — không có dữ liệu quẹt vào");

        // ── TÍNH PHÚT ĐI MUỘN ───────────────────────────────────────
        // So sánh giờ quẹt IN thực tế với giờ chuẩn 08:00.
        // Nếu checkIn > IN_STANDARD → đi muộn → tính số phút chênh lệch.
        // Ví dụ: checkIn = 08:15 → lateMinutes = (08:15 - 08:00).TotalMinutes = 15 phút.
        int lateMinutes = 0;
        string lateNote = "";

        if (checkIn.Value > IN_STANDARD)
        {
            // Chuyển về TimeSpan để tính phút, rồi ép sang int (bỏ giây lẻ)
            lateMinutes = (int)(checkIn.Value - IN_STANDARD).TotalMinutes;
            lateNote    = $", đi muộn {lateMinutes} phút";
        }

        // ── TRƯỜNG HỢP 2: CÓ IN NHƯNG KHÔNG CÓ OUT ─────────────────
        // Nhân viên quẹt vào nhưng không quẹt ra → tính 0.5 công.
        // Quy tắc này bảo vệ công ty: chỉ tính đủ ngày khi có đủ bằng chứng ra về.
        if (checkOut == null)
            return (0.5, lateMinutes, $"Nửa công — không có dữ liệu quẹt ra{lateNote}");

        // ── TRƯỜNG HỢP 3: TÍNH CÔNG DỰA VÀO GIỜ OUT ────────────────
        if (checkOut.Value >= OUT_STANDARD)
        {
            // Quẹt ra lúc 17:30 trở đi → đủ ngày công
            return (1.0, lateMinutes, $"Đủ công (1.0){lateNote}");
        }
        else if (checkOut.Value >= NOON)
        {
            // Quẹt ra từ 12:00 đến trước 17:30 → về sớm → tính 0.5 công
            return (0.5, lateMinutes, $"Nửa công — về sớm (ra lúc {checkOut:HH:mm}){lateNote}");
        }
        else
        {
            // Quẹt ra trước 12:00 → chỉ làm buổi sáng → tính 0.5 công
            return (0.5, lateMinutes, $"Nửa công — chỉ làm sáng (ra lúc {checkOut:HH:mm}){lateNote}");
        }
    }
}
