using System;
using HRMS.Domain.Entities;

namespace HRMS.Domain.Calculations;

public class AttendanceCalculationResult
{
    public int WorkingMinutes { get; set; }
    public int LateMinutes { get; set; }
    public int EarlyLeaveMinutes { get; set; }
    public int OvertimeMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
}

public static class AttendanceCalculator
{
    public static AttendanceCalculationResult Calculate(TimeOnly? checkIn, TimeOnly? checkOut, Shift shift)
    {
        var result = new AttendanceCalculationResult();

        if (checkIn == null)
        {
            result.Status = "Vắng mặt";
            return result;
        }

        // 1. Tính Late Minutes
        var allowedCheckIn = shift.StartTime.AddMinutes(shift.LateToleranceMinute);
        if (checkIn.Value > allowedCheckIn)
        {
            result.LateMinutes = (int)Math.Max(0, (checkIn.Value - shift.StartTime).TotalMinutes);
        }

        // Nếu chưa Check Out
        if (checkOut == null)
        {
            result.Status = result.LateMinutes > 0 ? "Đi muộn (Chưa ra)" : "Chưa check-out";
            return result;
        }

        // 2. Tính Early Leave Minutes
        if (checkOut.Value < shift.EndTime)
        {
            result.EarlyLeaveMinutes = (int)Math.Max(0, (shift.EndTime - checkOut.Value).TotalMinutes);
        }

        // 3. Tính Overtime Minutes
        var allowedCheckOut = shift.EndTime.AddMinutes(shift.LateCheckOutMinute);
        if (checkOut.Value > allowedCheckOut)
        {
            result.OvertimeMinutes = (int)Math.Max(0, (checkOut.Value - shift.EndTime).TotalMinutes);
        }

        // 4. Tính Working Minutes
        var actualStart = checkIn.Value < shift.StartTime ? shift.StartTime : checkIn.Value;
        var actualEnd = checkOut.Value > shift.EndTime ? shift.EndTime : checkOut.Value;

        if (actualEnd > actualStart)
        {
            int totalWorkMins = (int)(actualEnd - actualStart).TotalMinutes;

            // Trừ thời gian nghỉ trưa nếu làm việc vắt qua khoảng nghỉ
            var breakStart = shift.BreakStart;
            var breakEnd = shift.BreakEnd;
            if (actualStart < breakEnd && actualEnd > breakStart)
            {
                var overlapStart = actualStart > breakStart ? actualStart : breakStart;
                var overlapEnd = actualEnd < breakEnd ? actualEnd : breakEnd;
                if (overlapEnd > overlapStart)
                {
                    int breakMins = (int)(overlapEnd - overlapStart).TotalMinutes;
                    totalWorkMins = Math.Max(0, totalWorkMins - breakMins);
                }
            }
            result.WorkingMinutes = totalWorkMins;
        }

        // 5. Xác định Trạng thái công
        if (result.LateMinutes > 0 && result.EarlyLeaveMinutes > 0)
        {
            result.Status = "Đi muộn & Về sớm";
        }
        else if (result.LateMinutes > 0)
        {
            result.Status = "Đi muộn";
        }
        else if (result.EarlyLeaveMinutes > 0)
        {
            result.Status = "Về sớm";
        }
        else
        {
            result.Status = "Đủ công";
        }

        return result;
    }
}
