using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using HRMS.Application.DTOs.Attendance;
using HRMS.Application.Interfaces;
using HRMS.Domain.Entities;
using HRMS.Infrastructure.Persistence;

namespace HRMS.Infrastructure.Services;

public class AttendanceService : IAttendanceService
{
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

    public async Task LockPeriodAsync(int periodId)
    {
        var period = await _db.TimesheetPeriods.FindAsync(periodId)
            ?? throw new InvalidOperationException($"Không tìm thấy kỳ công ID={periodId}.");

        period.IsLocked = true;
        await _db.SaveChangesAsync();
    }

    public async Task<List<AttendanceImportResultDto>> ImportAndSaveAsync(Stream fileStream, int periodId)
    {
        var period = await _db.TimesheetPeriods.FindAsync(periodId)
            ?? throw new InvalidOperationException($"Kỳ công ID={periodId} không tồn tại.");

        if (period.IsLocked)
            throw new InvalidOperationException($"Kỳ công '{period.Name}' đã bị khóa. Không thể import thêm.");

        var rawRows = await _excelParser.ParseAsync(fileStream);

        if (!rawRows.Any())
            return new List<AttendanceImportResultDto>();

        rawRows = rawRows
            .Select(r => new AttendanceRowDto
            {
                EmployeeCode = r.EmployeeCode.Trim().ToUpperInvariant(),
                CheckedAt    = r.CheckedAt,
                CheckType    = r.CheckType.Trim().ToUpperInvariant()
            })
            .GroupBy(r => new { r.EmployeeCode, r.CheckedAt, r.CheckType })
            .Select(g => g.First())
            .ToList();

        foreach (var row in rawRows)
        {
            var checkedDate = DateOnly.FromDateTime(row.CheckedAt);
            if (checkedDate < period.StartDate || checkedDate > period.EndDate)
            {
                throw new InvalidOperationException(
                    $"Dữ liệu quẹt thẻ ngày {checkedDate:dd/MM/yyyy} (của nhân viên {row.EmployeeCode}) không nằm trong kỳ công đang chọn '{period.Name}' ({period.StartDate:dd/MM/yyyy} - {period.EndDate:dd/MM/yyyy}).");
            }
        }

        var employeeCodes = rawRows.Select(r => r.EmployeeCode).Distinct().ToList();

        var userDict = await _db.Users
            .Where(u => employeeCodes.Contains(u.EmployeeCode))
            .ToDictionaryAsync(u => u.EmployeeCode, StringComparer.OrdinalIgnoreCase);

        var userIds = userDict.Values.Select(u => u.Id).ToList();
        var existingLogs = await _db.AttendanceLogs
            .Where(log => log.PeriodId == periodId && userIds.Contains(log.UserId))
            .Select(log => new { log.UserId, log.CheckedAt, log.CheckType })
            .ToListAsync();

        var existingLogsSet = existingLogs
            .Select(log => (log.UserId, log.CheckedAt, CheckType: log.CheckType.Trim().ToUpperInvariant()))
            .ToHashSet();

        var grouped = rawRows
            .GroupBy(r => (r.EmployeeCode, WorkDate: DateOnly.FromDateTime(r.CheckedAt)));

        var results      = new List<AttendanceImportResultDto>();
        var logsToInsert = new List<AttendanceLog>();

        foreach (var group in grouped)
        {
            var (employeeCode, workDate) = group.Key;

            if (!userDict.TryGetValue(employeeCode, out var user))
            {
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

            var inRecords  = group.Where(r => r.CheckType == "IN").ToList();
            var outRecords = group.Where(r => r.CheckType == "OUT").ToList();

            TimeOnly? checkIn  = inRecords.Any()
                ? TimeOnly.FromDateTime(inRecords.Min(r => r.CheckedAt))
                : null;

            TimeOnly? checkOut = outRecords.Any()
                ? TimeOnly.FromDateTime(outRecords.Max(r => r.CheckedAt))
                : null;

            var (workValue, lateMinutes, note) = CalculateAttendance(checkIn, checkOut);

            int duplicateDbCount = 0;
            int totalRowsInGroup = group.Count();

            foreach (var row in group)
            {
                var isDuplicateInDb = existingLogsSet.Contains((user.Id, row.CheckedAt, row.CheckType));
                if (isDuplicateInDb)
                {
                    duplicateDbCount++;
                }
                else
                {
                    logsToInsert.Add(new AttendanceLog
                    {
                        UserId    = user.Id,
                        PeriodId  = periodId,
                        CheckedAt = row.CheckedAt,
                        CheckType = row.CheckType,
                        Source    = "Excel"         
                    });
                }
            }

            if (duplicateDbCount == totalRowsInGroup)
            {
                note += " (Bản ghi đã tồn tại trong DB)";
            }
            else if (duplicateDbCount > 0)
            {
                note += $" (Trùng lặp DB {duplicateDbCount}/{totalRowsInGroup} dòng, đã cập nhật các dòng mới)";
            }

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
        }

        if (logsToInsert.Any())
        {
            await _db.AttendanceLogs.AddRangeAsync(logsToInsert);
            await _db.SaveChangesAsync();
        }

        return results;
    }

    public async Task<List<AttendanceLogDto>> GetLogsAsync(int periodId)
    {
        return await _db.AttendanceLogs
            .Where(log => log.PeriodId == periodId)
            .OrderBy(log => log.CheckedAt)  
            .Select(log => new AttendanceLogDto
            {
                Id           = log.Id,
                CheckedAt    = log.CheckedAt,
                CheckType    = log.CheckType,
                Source       = log.Source,
                UserId       = log.UserId,
                EmployeeCode = log.User.EmployeeCode,
                EmployeeName = log.User.FullName,
                PeriodId     = log.PeriodId
            })
            .ToListAsync();
    }

    public async Task UpdateLogAsync(int logId, UpdateAttendanceLogDto dto)
    {
        var log = await _db.AttendanceLogs
            .Include(l => l.Period) 
            .FirstOrDefaultAsync(l => l.Id == logId)
            ?? throw new InvalidOperationException($"Không tìm thấy bản ghi chấm công ID={logId}.");

        if (log.Period.IsLocked)
            throw new InvalidOperationException(
                $"Kỳ công '{log.Period.Name}' đã bị khóa. Không thể sửa bản ghi chấm công.");

        log.CheckedAt = dto.CheckedAt;
        log.CheckType = dto.CheckType.Trim().ToUpperInvariant();
        log.Source    = "Manual";  

        await _db.SaveChangesAsync();
    }

    public async Task<AttendanceLogDto?> GetLogByIdAsync(int logId)
    {
        return await _db.AttendanceLogs
            .Where(log => log.Id == logId)
            .Select(log => new AttendanceLogDto
            {
                Id           = log.Id,
                CheckedAt    = log.CheckedAt,
                CheckType    = log.CheckType,
                Source       = log.Source,
                UserId       = log.UserId,
                EmployeeCode = log.User.EmployeeCode,
                EmployeeName = log.User.FullName,
                PeriodId     = log.PeriodId
            })
            .FirstOrDefaultAsync();
    }

    private static (double workValue, int lateMinutes, string note)
        CalculateAttendance(TimeOnly? checkIn, TimeOnly? checkOut)
    {
        if (checkIn == null)
            return (0.0, 0, "Vắng mặt — không có dữ liệu quẹt vào");

        int lateMinutes = 0;
        string lateNote = "";

        if (checkIn.Value > IN_STANDARD)
        {
            lateMinutes = (int)(checkIn.Value - IN_STANDARD).TotalMinutes;
            lateNote    = $", đi muộn {lateMinutes} phút";
        }

        if (checkOut == null)
            return (0.5, lateMinutes, $"Nửa công — không có dữ liệu quẹt ra{lateNote}");

        if (checkOut.Value >= OUT_STANDARD)
        {
            return (1.0, lateMinutes, $"Đủ công (1.0){lateNote}");
        }
        else if (checkOut.Value >= NOON)
        {
            return (0.5, lateMinutes, $"Nửa công — về sớm (ra lúc {checkOut:HH:mm}){lateNote}");
        }
        else
        {
            return (0.5, lateMinutes, $"Nửa công — chỉ làm sáng (ra lúc {checkOut:HH:mm}){lateNote}");
        }
    }
}
