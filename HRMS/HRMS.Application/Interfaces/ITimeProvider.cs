using System;

namespace HRMS.Application.Interfaces;

public interface ITimeProvider
{
    DateTime GetUtcNow();
    DateTime GetLocalNow();
    DateOnly GetToday();
    TimeOnly GetCurrentTime();
}
