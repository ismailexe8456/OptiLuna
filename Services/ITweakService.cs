using System.Collections.Generic;
using Dtrl.Models;

namespace Dtrl.Services;

public interface ITweakService
{
    List<Tweak> GetTweaks();
    bool ApplyTweak(Tweak tweak);
    bool RevertTweak(Tweak tweak);
    void RefreshTweakStatuses();
}
