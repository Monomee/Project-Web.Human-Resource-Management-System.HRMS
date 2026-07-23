using System;
using System.Diagnostics;
using System.Net.Http;
using HRMS.Application.Interfaces;

namespace HRMS.Infrastructure.Services;

public class DateTimeProvider : ITimeProvider
{
    private static readonly DateTime _baseUtcTime;
    private static readonly long _baseStopwatchTicks;
    private static readonly TimeZoneInfo _vietnamTimeZone;

    static DateTimeProvider()
    {
        _baseStopwatchTicks = Stopwatch.GetTimestamp();

        try
        {
            _vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch
        {
            _vietnamTimeZone = TimeZoneInfo.CreateCustomTimeZone("VN", TimeSpan.FromHours(7), "SE Asia Standard Time", "SE Asia Standard Time");
        }

        DateTime initialUtc = DateTime.UtcNow;

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var response = client.GetAsync("https://www.google.com", HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
            if (response.Headers.Date.HasValue)
            {
                initialUtc = response.Headers.Date.Value.UtcDateTime;
            }
        }
        catch
        {
            initialUtc = DateTime.UtcNow;
        }

        _baseUtcTime = initialUtc;
    }

    public DateTime GetUtcNow()
    {
        double elapsedSeconds = (Stopwatch.GetTimestamp() - _baseStopwatchTicks) / (double)Stopwatch.Frequency;
        return _baseUtcTime.AddSeconds(elapsedSeconds);
    }

    public DateTime GetLocalNow()
    {
        return TimeZoneInfo.ConvertTimeFromUtc(GetUtcNow(), _vietnamTimeZone);
    }

    public DateOnly GetToday()
    {
        return DateOnly.FromDateTime(GetLocalNow());
    }

    public TimeOnly GetCurrentTime()
    {
        return TimeOnly.FromDateTime(GetLocalNow());
    }
}
