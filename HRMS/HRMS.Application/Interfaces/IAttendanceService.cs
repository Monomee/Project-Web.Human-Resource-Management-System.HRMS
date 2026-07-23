using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HRMS.Application.DTOs.Attendance;
using HRMS.Application.DTOs.TimePeriod;

namespace HRMS.Application.Interfaces;

public interface IAttendanceService
{
    // Employee Check In & Check Out
    DateOnly GetTodayDate();
    Task CheckInAsync(int userId, DateOnly? date = null, TimeOnly? checkInTime = null);
    Task CheckOutAsync(int userId, DateOnly? date = null, TimeOnly? checkOutTime = null);
    Task<AttendanceDetailDto?> GetTodayAttendanceAsync(int userId, DateOnly date);
    Task<List<AttendanceDetailDto>> GetAttendanceHistoryAsync(int? userId, DateOnly? startDate, DateOnly? endDate, int? departmentId = null, int? periodId = null);
    Task<AttendanceDetailDto?> GetAttendanceByIdAsync(int id);
    Task UpdateAttendanceAsync(int id, TimeOnly? checkInTime, TimeOnly? checkOutTime);

    // Shift Management
    Task<List<ShiftDto>> GetShiftsAsync();
    Task<ShiftDto> CreateShiftAsync(CreateShiftDto dto);
    Task UpdateShiftAsync(UpdateShiftDto dto);
    Task DeleteShiftAsync(int shiftId);

    // Shift Assignment
    Task<List<ShiftAssignmentDto>> GetShiftAssignmentsAsync();
    Task AssignShiftAsync(CreateShiftAssignmentDto dto, int assignedByAccountId);
    Task DeleteShiftAssignmentAsync(int assignmentId);

    // Backward compatibility methods for TimesheetPeriod management & legacy Excel import
    Task<List<TimesheetPeriodDto>> GetPeriodsAsync();
    Task LockPeriodAsync(int periodId);
    Task UnlockPeriodAsync(int periodId);
    Task<List<AttendanceImportResultDto>> ImportAndSaveAsync(Stream fileStream, int periodId);
}
