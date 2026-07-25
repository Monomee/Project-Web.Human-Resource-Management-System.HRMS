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
        if (checkIn == null)
        {
            return new AttendanceCalculationResult { Status = "Vắng mặt" };
        }

        int lateMinutes = CalculateLateMinutes(checkIn.Value, shift);

        if (checkOut == null)
        {
            return new AttendanceCalculationResult
            {
                LateMinutes = lateMinutes,
                Status = lateMinutes > 0 ? "Đi muộn (Chưa ra)" : "Chưa check-out"
            };
        }

        int earlyLeaveMinutes = CalculateEarlyLeaveMinutes(checkOut.Value, shift);
        int overtimeMinutes = CalculateOvertimeMinutes(checkOut.Value, shift);
        int workingMinutes = CalculateWorkingMinutes(checkIn.Value, checkOut.Value, shift);
        string status = DetermineStatus(lateMinutes, earlyLeaveMinutes);

        return new AttendanceCalculationResult
        {
            WorkingMinutes = workingMinutes,
            LateMinutes = lateMinutes,
            EarlyLeaveMinutes = earlyLeaveMinutes,
            OvertimeMinutes = overtimeMinutes,
            Status = status
        };
    }

    private static int CalculateLateMinutes(TimeOnly checkIn, Shift shift)
    {
        var allowedCheckIn = shift.StartTime.AddMinutes(shift.LateToleranceMinute);
        if (checkIn > allowedCheckIn)
        {
            return (int)Math.Max(0, (checkIn - shift.StartTime).TotalMinutes);
        }
        return 0;
    }

    private static int CalculateEarlyLeaveMinutes(TimeOnly checkOut, Shift shift)
    {
        if (checkOut < shift.EndTime)
        {
            return (int)Math.Max(0, (shift.EndTime - checkOut).TotalMinutes);
        }
        return 0;
    }

    private static int CalculateOvertimeMinutes(TimeOnly checkOut, Shift shift)
    {
        var allowedCheckOut = shift.EndTime.AddMinutes(shift.LateCheckOutMinute);
        if (checkOut > allowedCheckOut)
        {
            return (int)Math.Max(0, (checkOut - shift.EndTime).TotalMinutes);
        }
        return 0;
    }

    private static int CalculateWorkingMinutes(TimeOnly checkIn, TimeOnly checkOut, Shift shift)
    {
        var actualStart = checkIn < shift.StartTime ? shift.StartTime : checkIn;
        var actualEnd = checkOut > shift.EndTime ? shift.EndTime : checkOut;

        if (actualEnd <= actualStart)
        {
            return 0;
        }

        int totalWorkMins = (int)(actualEnd - actualStart).TotalMinutes;
        int breakMins = CalculateBreakOverlapMinutes(actualStart, actualEnd, shift.BreakStart, shift.BreakEnd);

        return Math.Max(0, totalWorkMins - breakMins);
    }

    private static int CalculateBreakOverlapMinutes(TimeOnly actualStart, TimeOnly actualEnd, TimeOnly breakStart, TimeOnly breakEnd)
    {
        if (actualStart < breakEnd && actualEnd > breakStart)
        {
            var overlapStart = actualStart > breakStart ? actualStart : breakStart;
            var overlapEnd = actualEnd < breakEnd ? actualEnd : breakEnd;
            if (overlapEnd > overlapStart)
            {
                return (int)(overlapEnd - overlapStart).TotalMinutes;
            }
        }
        return 0;
    }

    private static string DetermineStatus(int lateMinutes, int earlyLeaveMinutes)
    {
        if (lateMinutes > 0 && earlyLeaveMinutes > 0)
        {
            return "Đi muộn & Về sớm";
        }
        if (lateMinutes > 0)
        {
            return "Đi muộn";
        }
        if (earlyLeaveMinutes > 0)
        {
            return "Về sớm";
        }
        return "Đủ công";
    }
}
