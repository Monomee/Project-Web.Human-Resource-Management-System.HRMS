using Microsoft.EntityFrameworkCore;
using HRMS.Application.DTOs.TimePeriod;
using HRMS.Application.DTOs.Attendance;
using HRMS.Application.Interfaces;
using HRMS.Domain.Entities;
using HRMS.Infrastructure.Persistence;

namespace HRMS.Infrastructure.Services;

/// <summary>
/// Dịch vụ quản lý kỳ công (TimesheetPeriod): Create, Read, Update.
///
/// LƯU Ý KIẾN TRÚC:
///   - Đặt ở Infrastructure vì cần truy cập ApplicationDbContext (EF Core).
///   - Implement ITimePeriodService được định nghĩa ở tầng Application.
///   - Tầng WebUI chỉ phụ thuộc vào ITimePeriodService, không biết lớp này tồn tại.
/// </summary>
public class TimePeriodService : ITimePeriodService
{
    private readonly ApplicationDbContext _db;

    public TimePeriodService(ApplicationDbContext db)
    {
        _db = db;
    }

    // ════════════════════════════════════════════════════════════════
    // 1. ĐỌC DANH SÁCH KỲ CÔNG
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Lấy toàn bộ kỳ công, sắp xếp mới nhất trước.
    /// Dùng .Select() để chỉ kéo đúng các cột cần thiết, không kéo navigation properties.
    /// </summary>
    public async Task<List<TimesheetPeriodDto>> GetAllAsync()
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

    /// <summary>
    /// Lấy 1 kỳ công theo Id. Trả về null nếu không tồn tại.
    /// </summary>
    public async Task<TimesheetPeriodDto?> GetByIdAsync(int id)
    {
        return await _db.TimesheetPeriods
            .Where(p => p.Id == id)
            .Select(p => new TimesheetPeriodDto
            {
                Id        = p.Id,
                Name      = p.Name,
                StartDate = p.StartDate,
                EndDate   = p.EndDate,
                IsLocked  = p.IsLocked
            })
            .FirstOrDefaultAsync();
    }

    // ════════════════════════════════════════════════════════════════
    // 2. TẠO KỲ CÔNG MỚI
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tạo một kỳ công mới với các bước validation nghiệp vụ:
    ///   1. Tên không được trùng với kỳ đã có trong DB.
    ///   2. Khoảng ngày không được chồng lấp (overlap) với kỳ công khác.
    /// </summary>
    public async Task<int> CreateAsync(CreateTimesheetPeriodDto dto)
    {
        // Đảm bảo DTO đã được validate (StartDate, EndDate not null vì có [Required])
        var startDate = dto.StartDate!.Value;
        var endDate   = dto.EndDate!.Value;

        // ── Validation 1: Tên không trùng ───────────────────────────
        bool nameExists = await _db.TimesheetPeriods
            .AnyAsync(p => p.Name == dto.Name.Trim());

        if (nameExists)
            throw new InvalidOperationException($"Kỳ công với tên '{dto.Name}' đã tồn tại.");

        // ── Validation 2: Khoảng ngày không chồng lấp ───────────────
        // Hai khoảng [A, B] và [C, D] chồng lấp khi: A <= D AND C <= B
        bool dateOverlap = await _db.TimesheetPeriods
            .AnyAsync(p => p.StartDate <= endDate && startDate <= p.EndDate);

        if (dateOverlap)
            throw new InvalidOperationException(
                $"Khoảng thời gian {startDate:dd/MM/yyyy} – {endDate:dd/MM/yyyy} " +
                "bị chồng lấp với một kỳ công đã tồn tại.");

        // ── Tạo entity và lưu DB ─────────────────────────────────────
        var period = new TimesheetPeriod
        {
            Name      = dto.Name.Trim(),
            StartDate = startDate,
            EndDate   = endDate,
            IsLocked  = false  // Kỳ mới tạo luôn ở trạng thái mở
        };

        _db.TimesheetPeriods.Add(period);
        await _db.SaveChangesAsync();

        return period.Id; // Trả về Id vừa được DB sinh ra
    }

    // ════════════════════════════════════════════════════════════════
    // 3. CẬP NHẬT KỲ CÔNG
    // ════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cập nhật thông tin kỳ công với các bước validation:
    ///   1. Kỳ công phải tồn tại.
    ///   2. Kỳ công chưa bị khóa (IsLocked = false).
    ///   3. Tên mới không trùng với kỳ KHÁC trong DB.
    ///   4. Khoảng ngày mới không chồng lấp với kỳ KHÁC.
    /// </summary>
    public async Task UpdateAsync(int id, UpdateTimesheetPeriodDto dto)
    {
        // ── Kiểm tra kỳ công tồn tại ────────────────────────────────
        var period = await _db.TimesheetPeriods.FindAsync(id)
            ?? throw new InvalidOperationException($"Không tìm thấy kỳ công ID={id}.");

        // ── Nghiệp vụ: không cho sửa kỳ đã khóa ────────────────────
        if (period.IsLocked)
            throw new InvalidOperationException(
                $"Kỳ công '{period.Name}' đã bị khóa, không thể chỉnh sửa.");

        var startDate = dto.StartDate!.Value;
        var endDate   = dto.EndDate!.Value;

        // ── Validation: Tên không trùng với kỳ KHÁC ─────────────────
        bool nameExists = await _db.TimesheetPeriods
            .AnyAsync(p => p.Name == dto.Name.Trim() && p.Id != id);

        if (nameExists)
            throw new InvalidOperationException($"Kỳ công với tên '{dto.Name}' đã tồn tại.");

        // ── Validation: Khoảng ngày không chồng lấp với kỳ KHÁC ─────
        bool dateOverlap = await _db.TimesheetPeriods
            .AnyAsync(p => p.Id != id
                        && p.StartDate <= endDate
                        && startDate   <= p.EndDate);

        if (dateOverlap)
            throw new InvalidOperationException(
                $"Khoảng thời gian {startDate:dd/MM/yyyy} – {endDate:dd/MM/yyyy} " +
                "bị chồng lấp với một kỳ công đã tồn tại.");

        // ── Cập nhật và lưu ─────────────────────────────────────────
        period.Name      = dto.Name.Trim();
        period.StartDate = startDate;
        period.EndDate   = endDate;

        await _db.SaveChangesAsync();
    }

    public async Task LockPeriodAsync(int id)
    {
        var period = await _db.TimesheetPeriods.FindAsync(id)
            ?? throw new InvalidOperationException($"Không tìm thấy kỳ công ID={id}.");

        period.IsLocked = true;
        await _db.SaveChangesAsync();
    }

    public async Task UnlockPeriodAsync(int id)
    {
        var period = await _db.TimesheetPeriods.FindAsync(id)
            ?? throw new InvalidOperationException($"Không tìm thấy kỳ công ID={id}.");

        period.IsLocked = false;
        await _db.SaveChangesAsync();
    }
}
