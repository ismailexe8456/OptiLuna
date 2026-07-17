using System.Collections.Generic;
using NXG.Models;

namespace NXG.Services;

public interface ITweakService
{
    List<Tweak> GetTweaks();
    bool ApplyTweak(Tweak tweak);
    bool RevertTweak(Tweak tweak);
    void RefreshTweakStatuses();
}
