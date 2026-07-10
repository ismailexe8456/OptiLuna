using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dtrl.Services;

public interface IFocusModeService
{
    bool IsFocusActive { get; }
    TimeSpan TimeRemaining { get; }
    List<string> GetBlockedApps();
    void SetBlockedApps(List<string> apps);
    Task<bool> StartFocusAsync(int durationMinutes, bool muteNotifications, bool closeApps);
    Task<bool> StopFocusAsync();
    event EventHandler<TimeSpan>? TimerTick;
    event EventHandler? FocusEnded;
}
