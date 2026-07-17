using System.Collections.Generic;
using NXG.Models;

namespace NXG.Services;

public interface IProfileService
{
    List<ProfileModel> GetProfiles();
    bool ExportProfile(ProfileModel profile, string filePath);
    ProfileModel? ImportProfile(string filePath);
    void ApplyProfile(ProfileModel profile, ITweakService tweakService);
}
