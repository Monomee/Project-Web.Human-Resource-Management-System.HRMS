using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HRMS.Application.DTOs.Attendance;
using HRMS.Application.DTOs.TimePeriod;
using HRMS.Application.Interfaces;
using HRMS.Domain.Calculations;
using HRMS.Domain.Entities;
using HRMS.Infrastructure.Persistence;

namespace HRMS.Infrastructure.Services;

public class AttendanceService : IAttendanceService
{
    private static readonly Shift FallbackDefaultShift = new Shift
    {
        Id = 0,
        Name = "Ca Hành Chính Standard",
        StartTime = new TimeOnly(8, 0),
        EndTime = new TimeOnly(17, 30),
        BreakStart = new TimeOnly(12, 0),
        BreakEnd = new TimeOnly(13, 30),
        LateToleranceMinute = 15,
        EarlyCheckInMinute = 30,
        LateCheckOutMinute = 30,
        IsActive = true
    };

    private readonly ApplicationDbContext _db;
    private readonly IExcelParserService _excelParser;
    private readonly ITimeProvider _timeProvider;

    public AttendanceService(ApplicationDbContext db, IExcelParserService excelParser, ITimeProvider timeProvider)
    {
        _db = db;
        _excelParser = excelParser;
        _timeProvider = timeProvider;
    }

    #region Shift Resolution Helper
    private async Task<Shift> ResolveShiftAsync(int userId, DateOnly date)
    {
        // 1. Kiểm tra ShiftAssignment riêng cho nhân viên
        var assignment = await _db.ShiftAssignments
            .Include(sa => sa.Shift)
            .Where(sa => sa.EmployeeId == userId && sa.StartDate <= date && date <= sa.EndDate && sa.Shift.IsActive)
            .OrderByDescending(sa => sa.CreatedAt)
            .FirstOrDefaultAsync();

        if (assignment != null && assignment.Shift != null)
        {
            return assignment.Shift;
        }

        // 2. Kiểm tra Position.DefaultShiftId của nhân viên
        var user = await _db.Users
            .Include(u => u.Position)
                .ThenInclude(p => p.DefaultShift)
            .FirstOrDefaultAsync(u => u.Id == userId);

        var defaultShift = await _db.Shifts
            .FirstOrDefaultAsync(s => s.IsActive && s.Name.Contains("Hành Chính"));

        return defaultShift ?? FallbackDefaultShift;
    }

    private async Task<int?> ResolvePeriodIdAsync(DateOnly date)
    {
        var period = await _db.TimesheetPeriods
            .FirstOrDefaultAsync(p => p.StartDate <= date && date <= p.EndDate);
        return period?.Id;
    }
    #endregion

    #region Employee Check-In & Check-Out
    public DateOnly GetTodayDate() => _timeProvider.GetToday();

    private DateTime GetServerNow()
    {
        return _timeProvider.GetLocalNow();
    }

    public async Task CheckInAsync(int userId, DateOnly? date = null, TimeOnly? checkInTime = null)
    {
        var serverNow = GetServerNow();
        var targetDate = date ?? DateOnly.FromDateTime(serverNow);
        var targetCheckInTime = checkInTime ?? TimeOnly.FromDateTime(serverNow);

        var period = await _db.TimesheetPeriods
            .FirstOrDefaultAsync(p => p.StartDate <= targetDate && targetDate <= p.EndDate);

        if (period != null && period.IsLocked)
        {
            throw new InvalidOperationException($"Kỳ công '{period.Name}' đã bị khóa sổ. Không thể thực hiện Check-In.");
        }

        var attendance = await _db.Attendances
            .FirstOrDefaultAsync(a => a.EmployeeId == userId && a.AttendanceDate == targetDate);

        if (attendance != null && attendance.CheckInTime != null)
        {
            throw new InvalidOperationException($"Nhân viên ID={userId} đã thực hiện Check-In ngày {targetDate:dd/MM/yyyy} rồi.");
        }

        if (attendance == null)
        {
            attendance = new Attendance
            {
                EmployeeId = userId,
                AttendanceDate = targetDate,
                CheckInTime = targetCheckInTime,
                PeriodId = period?.Id
            };
            await _db.Attendances.AddAsync(attendance);
        }
        else
        {
            attendance.CheckInTime = targetCheckInTime;
            if (!attendance.PeriodId.HasValue)
            {
                attendance.PeriodId = period?.Id;
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task CheckOutAsync(int userId, DateOnly? date = null, TimeOnly? checkOutTime = null)
    {
        var serverNow = GetServerNow();
        var targetDate = date ?? DateOnly.FromDateTime(serverNow);
        var targetCheckOutTime = checkOutTime ?? TimeOnly.FromDateTime(serverNow);

        var attendance = await _db.Attendances
            .Include(a => a.Period)
            .FirstOrDefaultAsync(a => a.EmployeeId == userId && a.AttendanceDate == targetDate);

        if (attendance == null || attendance.CheckInTime == null)
        {
            throw new InvalidOperationException($"Nhân viên ID={userId} chưa thực hiện Check-In ngày {targetDate:dd/MM/yyyy}.");
        }

        if (attendance.Period != null && attendance.Period.IsLocked)
        {
            throw new InvalidOperationException($"Kỳ công '{attendance.Period.Name}' đã bị khóa sổ. Không thể thực hiện Check-Out.");
        }

        if (attendance.CheckOutTime != null)
        {
            throw new InvalidOperationException($"Nhân viên ID={userId} đã thực hiện Check-Out ngày {targetDate:dd/MM/yyyy} rồi.");
        }

        if (targetCheckOutTime <= attendance.CheckInTime.Value)
        {
            throw new InvalidOperationException($"Giờ Check-Out ({targetCheckOutTime:HH:mm}) phải lớn hơn giờ Check-In ({attendance.CheckInTime.Value:HH:mm}).");
        }

        attendance.CheckOutTime = targetCheckOutTime;
        await _db.SaveChangesAsync();
    }

    public async Task<AttendanceDetailDto?> GetTodayAttendanceAsync(int userId, DateOnly date)
    {
        var user = await _db.Users
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return null;

        var attendance = await _db.Attendances
            .Include(a => a.Period)
            .FirstOrDefaultAsync(a => a.EmployeeId == userId && a.AttendanceDate == date);

        var shift = await ResolveShiftAsync(userId, date);
        var calc = AttendanceCalculator.Calculate(attendance?.CheckInTime, attendance?.CheckOutTime, shift);

        return new AttendanceDetailDto
        {
            Id = attendance?.Id ?? 0,
            EmployeeId = user.Id,
            EmployeeCode = user.EmployeeCode,
            EmployeeName = user.FullName,
            DepartmentName = user.Department?.Name ?? string.Empty,
            AttendanceDate = date,
            PeriodId = attendance?.PeriodId,
            PeriodName = attendance?.Period?.Name ?? string.Empty,
            CheckInTime = attendance?.CheckInTime,
            CheckOutTime = attendance?.CheckOutTime,
            ShiftName = shift.Name,
            WorkingMinutes = calc.WorkingMinutes,
            LateMinutes = calc.LateMinutes,
            EarlyLeaveMinutes = calc.EarlyLeaveMinutes,
            OvertimeMinutes = calc.OvertimeMinutes,
            Status = calc.Status
        };
    }

    public async Task<List<AttendanceDetailDto>> GetAttendanceHistoryAsync(int? userId, DateOnly? startDate, DateOnly? endDate, int? departmentId = null, int? periodId = null)
    {
        var query = _db.Attendances
            .Include(a => a.Employee)
                .ThenInclude(e => e.Department)
            .Include(a => a.Period)
            .AsQueryable();

        if (userId.HasValue && userId.Value > 0)
        {
            query = query.Where(a => a.EmployeeId == userId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(a => a.AttendanceDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.AttendanceDate <= endDate.Value);
        }

        if (departmentId.HasValue && departmentId.Value > 0)
        {
            query = query.Where(a => a.Employee.DepartmentId == departmentId.Value);
        }

        if (periodId.HasValue && periodId.Value > 0)
        {
            query = query.Where(a => a.PeriodId == periodId.Value);
        }

        var list = await query
            .OrderByDescending(a => a.AttendanceDate)
            .ThenBy(a => a.Employee.EmployeeCode)
            .ToListAsync();

        var result = new List<AttendanceDetailDto>();

        foreach (var item in list)
        {
            var shift = await ResolveShiftAsync(item.EmployeeId, item.AttendanceDate);
            var calc = AttendanceCalculator.Calculate(item.CheckInTime, item.CheckOutTime, shift);

            result.Add(new AttendanceDetailDto
            {
                Id = item.Id,
                EmployeeId = item.EmployeeId,
                EmployeeCode = item.Employee.EmployeeCode,
                EmployeeName = item.Employee.FullName,
                DepartmentName = item.Employee.Department?.Name ?? string.Empty,
                AttendanceDate = item.AttendanceDate,
                PeriodId = item.PeriodId,
                PeriodName = item.Period?.Name ?? string.Empty,
                CheckInTime = item.CheckInTime,
                CheckOutTime = item.CheckOutTime,
                ShiftName = shift.Name,
                WorkingMinutes = calc.WorkingMinutes,
                LateMinutes = calc.LateMinutes,
                EarlyLeaveMinutes = calc.EarlyLeaveMinutes,
                OvertimeMinutes = calc.OvertimeMinutes,
                Status = calc.Status
            });
        }

        return result;
    }

    public async Task<AttendanceDetailDto?> GetAttendanceByIdAsync(int id)
    {
        var attendance = await _db.Attendances
            .Include(a => a.Employee)
                .ThenInclude(e => e.Department)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (attendance == null) return null;

        var shift = await ResolveShiftAsync(attendance.EmployeeId, attendance.AttendanceDate);
        var calc = AttendanceCalculator.Calculate(attendance.CheckInTime, attendance.CheckOutTime, shift);

        return new AttendanceDetailDto
        {
            Id = attendance.Id,
            EmployeeId = attendance.EmployeeId,
            EmployeeCode = attendance.Employee.EmployeeCode,
            EmployeeName = attendance.Employee.FullName,
            DepartmentName = attendance.Employee.Department?.Name ?? string.Empty,
            AttendanceDate = attendance.AttendanceDate,
            CheckInTime = attendance.CheckInTime,
            CheckOutTime = attendance.CheckOutTime,
            ShiftName = shift.Name,
            WorkingMinutes = calc.WorkingMinutes,
            LateMinutes = calc.LateMinutes,
            EarlyLeaveMinutes = calc.EarlyLeaveMinutes,
            OvertimeMinutes = calc.OvertimeMinutes,
            Status = calc.Status
        };
    }

    public async Task UpdateAttendanceAsync(int id, TimeOnly? checkInTime, TimeOnly? checkOutTime)
    {
        var attendance = await _db.Attendances
            .Include(a => a.Period)
            .FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new InvalidOperationException($"Không tìm thấy bản ghi chấm công ID={id}.");

        if (attendance.Period != null && attendance.Period.IsLocked)
        {
            throw new InvalidOperationException($"Kỳ công '{attendance.Period.Name}' đã bị khóa sổ. Không thể chỉnh sửa giờ chấm công.");
        }

        if (checkInTime.HasValue && checkOutTime.HasValue && checkOutTime.Value <= checkInTime.Value)
        {
            throw new InvalidOperationException($"Giờ Check-Out ({checkOutTime.Value:HH:mm}) phải lớn hơn giờ Check-In ({checkInTime.Value:HH:mm}).");
        }

        attendance.CheckInTime = checkInTime;
        attendance.CheckOutTime = checkOutTime;

        await _db.SaveChangesAsync();
    }
    #endregion

    #region Shift Management
    public async Task<List<ShiftDto>> GetShiftsAsync()
    {
        return await _db.Shifts
            .OrderBy(s => s.StartTime)
            .Select(s => new ShiftDto
            {
                Id = s.Id,
                Name = s.Name,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                BreakStart = s.BreakStart,
                BreakEnd = s.BreakEnd,
                LateToleranceMinute = s.LateToleranceMinute,
                EarlyCheckInMinute = s.EarlyCheckInMinute,
                LateCheckOutMinute = s.LateCheckOutMinute,
                IsActive = s.IsActive
            })
            .ToListAsync();
    }

    public async Task<ShiftDto> CreateShiftAsync(CreateShiftDto dto)
    {
        ValidateShiftDto(dto);

        var shift = new Shift
        {
            Name = dto.Name.Trim(),
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            BreakStart = dto.BreakStart,
            BreakEnd = dto.BreakEnd,
            LateToleranceMinute = dto.LateToleranceMinute,
            EarlyCheckInMinute = dto.EarlyCheckInMinute,
            LateCheckOutMinute = dto.LateCheckOutMinute,
            IsActive = dto.IsActive
        };

        await _db.Shifts.AddAsync(shift);
        await _db.SaveChangesAsync();

        return new ShiftDto
        {
            Id = shift.Id,
            Name = shift.Name,
            StartTime = shift.StartTime,
            EndTime = shift.EndTime,
            BreakStart = shift.BreakStart,
            BreakEnd = shift.BreakEnd,
            LateToleranceMinute = shift.LateToleranceMinute,
            EarlyCheckInMinute = shift.EarlyCheckInMinute,
            LateCheckOutMinute = shift.LateCheckOutMinute,
            IsActive = shift.IsActive
        };
    }

    public async Task UpdateShiftAsync(UpdateShiftDto dto)
    {
        ValidateShiftDto(dto);

        var shift = await _db.Shifts.FindAsync(dto.Id)
            ?? throw new InvalidOperationException($"Không tìm thấy Ca làm việc ID={dto.Id}.");

        shift.Name = dto.Name.Trim();
        shift.StartTime = dto.StartTime;
        shift.EndTime = dto.EndTime;
        shift.BreakStart = dto.BreakStart;
        shift.BreakEnd = dto.BreakEnd;
        shift.LateToleranceMinute = dto.LateToleranceMinute;
        shift.EarlyCheckInMinute = dto.EarlyCheckInMinute;
        shift.LateCheckOutMinute = dto.LateCheckOutMinute;
        shift.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
    }

    private static void ValidateShiftDto(CreateShiftDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new InvalidOperationException("Tên Ca làm việc không được để trống.");
        }
        if (dto.EndTime <= dto.StartTime)
        {
            throw new InvalidOperationException("Giờ Kết thúc ca làm việc phải lớn hơn giờ Bắt đầu.");
        }
        if (dto.BreakEnd <= dto.BreakStart)
        {
            throw new InvalidOperationException("Giờ Kết thúc nghỉ trưa phải lớn hơn giờ Bắt đầu nghỉ trưa.");
        }
        if (dto.BreakStart < dto.StartTime || dto.BreakEnd > dto.EndTime)
        {
            throw new InvalidOperationException("Khoảng thời gian nghỉ trưa phải nằm trong khoảng thời gian ca làm việc.");
        }
        if (dto.LateToleranceMinute < 0 || dto.EarlyCheckInMinute < 0 || dto.LateCheckOutMinute < 0)
        {
            throw new InvalidOperationException("Dung sai thời gian không được là số âm.");
        }
    }

    public async Task DeleteShiftAsync(int shiftId)
    {
        var shift = await _db.Shifts.FindAsync(shiftId)
            ?? throw new InvalidOperationException($"Không tìm thấy Ca làm việc ID={shiftId}.");

        shift.IsActive = false;
        await _db.SaveChangesAsync();
    }
    #endregion

    #region Shift Assignment
    public async Task<List<ShiftAssignmentDto>> GetShiftAssignmentsAsync()
    {
        return await _db.ShiftAssignments
            .Include(sa => sa.Employee)
            .Include(sa => sa.Shift)
            .OrderByDescending(sa => sa.CreatedAt)
            .Select(sa => new ShiftAssignmentDto
            {
                Id = sa.Id,
                EmployeeId = sa.EmployeeId,
                EmployeeCode = sa.Employee.EmployeeCode,
                EmployeeName = sa.Employee.FullName,
                ShiftId = sa.ShiftId,
                ShiftName = sa.Shift.Name,
                StartDate = sa.StartDate,
                EndDate = sa.EndDate,
                AssignedBy = sa.AssignedBy,
                CreatedAt = sa.CreatedAt
            })
            .ToListAsync();
    }

    public async Task AssignShiftAsync(CreateShiftAssignmentDto dto, int assignedByAccountId)
    {
        if (dto.EmployeeId <= 0 || !await _db.Users.AnyAsync(u => u.Id == dto.EmployeeId))
        {
            throw new InvalidOperationException("Nhân viên không tồn tại trong hệ thống.");
        }

        if (dto.ShiftId <= 0 || !await _db.Shifts.AnyAsync(s => s.Id == dto.ShiftId && s.IsActive))
        {
            throw new InvalidOperationException("Ca làm việc không tồn tại hoặc đã bị vô hiệu hóa.");
        }

        if (dto.EndDate < dto.StartDate)
        {
            throw new InvalidOperationException("Ngày kết thúc phân ca không thể nhỏ hơn ngày bắt đầu.");
        }

        var assignment = new ShiftAssignment
        {
            EmployeeId = dto.EmployeeId,
            ShiftId = dto.ShiftId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            AssignedBy = assignedByAccountId > 0 ? assignedByAccountId : null,
            CreatedAt = DateTime.Now
        };

        await _db.ShiftAssignments.AddAsync(assignment);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteShiftAssignmentAsync(int assignmentId)
    {
        var assignment = await _db.ShiftAssignments.FindAsync(assignmentId);
        if (assignment != null)
        {
            _db.ShiftAssignments.Remove(assignment);
            await _db.SaveChangesAsync();
        }
    }
    #endregion

    #region Backward Compatibility
    public async Task<List<TimesheetPeriodDto>> GetPeriodsAsync()
    {
        return await _db.TimesheetPeriods
            .OrderByDescending(p => p.StartDate)
            .Select(p => new TimesheetPeriodDto
            {
                Id = p.Id,
                Name = p.Name,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                IsLocked = p.IsLocked
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

    public async Task UnlockPeriodAsync(int periodId)
    {
        var period = await _db.TimesheetPeriods.FindAsync(periodId)
            ?? throw new InvalidOperationException($"Không tìm thấy kỳ công ID={periodId}.");

        period.IsLocked = false;
        await _db.SaveChangesAsync();
    }

    public async Task<List<AttendanceImportResultDto>> ImportAndSaveAsync(Stream fileStream, int periodId)
    {
        var period = await _db.TimesheetPeriods.FindAsync(periodId)
            ?? throw new InvalidOperationException($"Kỳ công ID={periodId} không tồn tại.");

        if (period.IsLocked)
            throw new InvalidOperationException($"Kỳ công '{period.Name}' đã bị khóa. Không thể import thêm.");

        var rawRows = await _excelParser.ParseAsync(fileStream);
        if (!rawRows.Any()) return new List<AttendanceImportResultDto>();

        var employeeCodes = rawRows.Select(r => r.EmployeeCode.Trim().ToUpperInvariant()).Distinct().ToList();
        var userDict = await _db.Users
            .Where(u => employeeCodes.Contains(u.EmployeeCode))
            .ToDictionaryAsync(u => u.EmployeeCode, StringComparer.OrdinalIgnoreCase);

        var grouped = rawRows
            .GroupBy(r => (Code: r.EmployeeCode.Trim().ToUpperInvariant(), WorkDate: DateOnly.FromDateTime(r.CheckedAt)));

        var results = new List<AttendanceImportResultDto>();

        foreach (var group in grouped)
        {
            var (employeeCode, workDate) = group.Key;

            if (!userDict.TryGetValue(employeeCode, out var user))
            {
                results.Add(new AttendanceImportResultDto
                {
                    EmployeeCode = employeeCode,
                    EmployeeName = "??? (Không tìm thấy)",
                    WorkDate = workDate,
                    HasError = true,
                    Note = $"Mã nhân viên '{employeeCode}' không tồn tại."
                });
                continue;
            }

            var inRecords = group.Where(r => string.Equals(r.CheckType.Trim(), "IN", StringComparison.OrdinalIgnoreCase)).ToList();
            var outRecords = group.Where(r => string.Equals(r.CheckType.Trim(), "OUT", StringComparison.OrdinalIgnoreCase)).ToList();

            TimeOnly? checkIn = inRecords.Any() ? TimeOnly.FromDateTime(inRecords.Min(r => r.CheckedAt)) : null;
            TimeOnly? checkOut = outRecords.Any() ? TimeOnly.FromDateTime(outRecords.Max(r => r.CheckedAt)) : null;

            var existingAttendance = await _db.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == user.Id && a.AttendanceDate == workDate);

            if (existingAttendance == null)
            {
                var att = new Attendance
                {
                    EmployeeId = user.Id,
                    AttendanceDate = workDate,
                    CheckInTime = checkIn,
                    CheckOutTime = checkOut,
                    PeriodId = periodId
                };
                await _db.Attendances.AddAsync(att);
            }
            else
            {
                if (checkIn.HasValue) existingAttendance.CheckInTime = checkIn.Value;
                if (checkOut.HasValue) existingAttendance.CheckOutTime = checkOut.Value;
            }

            var shift = await ResolveShiftAsync(user.Id, workDate);
            var calc = AttendanceCalculator.Calculate(checkIn, checkOut, shift);

            results.Add(new AttendanceImportResultDto
            {
                EmployeeCode = employeeCode,
                EmployeeName = user.FullName,
                WorkDate = workDate,
                CheckIn = checkIn,
                CheckOut = checkOut,
                LateMinutes = calc.LateMinutes,
                WorkValue = calc.WorkingMinutes >= 420 ? 1.0 : (calc.WorkingMinutes >= 210 ? 0.5 : 0.0),
                Note = calc.Status,
                HasError = false
            });
        }

        await _db.SaveChangesAsync();
        return results;
    }
    #endregion
}
