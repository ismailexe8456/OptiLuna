using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dtrl.Models;
using Dtrl.Services;

namespace Dtrl.ViewModels;

public partial class ProfilesViewModel : ObservableObject
{
    private readonly IProfileService _profileService;
    private readonly ITweakService _tweakService;
    private readonly ILoggingService _logger;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProfilePanelVisibility))]
    private ProfileModel _selectedProfile = new();

    public Microsoft.UI.Xaml.Visibility ProfilePanelVisibility => SelectedProfile != null && !string.IsNullOrEmpty(SelectedProfile.Name) ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusText = "Select a profile package to apply or export";

    public ObservableCollection<ProfileModel> Profiles { get; } = new();
    public ObservableCollection<string> SelectedProfileTweakNames { get; } = new();

    public ProfilesViewModel(IProfileService profileService, ITweakService tweakService, ILoggingService logger)
    {
        _profileService = profileService;
        _tweakService = tweakService;
        _logger = logger;
        
        LoadProfiles();
    }

    private void LoadProfiles()
    {
        Profiles.Clear();
        foreach (var p in _profileService.GetProfiles())
        {
            Profiles.Add(p);
        }
        if (Profiles.Count > 0)
        {
            SelectedProfile = Profiles[0];
        }
    }

    partial void OnSelectedProfileChanged(ProfileModel value)
    {
        SelectedProfileTweakNames.Clear();
        if (value == null || string.IsNullOrEmpty(value.Name)) return;

        var allTweaks = _tweakService.GetTweaks();
        foreach (var id in value.EnabledTweakIds)
        {
            var tweak = allTweaks.Find(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (tweak != null)
            {
                SelectedProfileTweakNames.Add($"• {tweak.Name} ({tweak.EstimatedImpact})");
            }
            else
            {
                SelectedProfileTweakNames.Add($"• Unknown Tweak (ID: {id})");
            }
        }
    }

    [RelayCommand]
    private async Task ApplySelectedProfileAsync()
    {
        if (SelectedProfile == null) return;
        IsBusy = true;
        StatusText = $"Applying profile package '{SelectedProfile.Name}'...";

        await Task.Run(() => _profileService.ApplyProfile(SelectedProfile, _tweakService));
        
        _tweakService.RefreshTweakStatuses();
        StatusText = $"Profile '{SelectedProfile.Name}' applied successfully.";
        IsBusy = false;
    }

    [RelayCommand]
    private async Task ExportProfileAsync()
    {
        if (SelectedProfile == null) return;

        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string dtrlDir = Path.Combine(appData, "DTRL", "Profiles");
        Directory.CreateDirectory(dtrlDir);
        string filePath = Path.Combine(dtrlDir, $"{SelectedProfile.Name.Replace(" ", "_")}.dtrl");

        IsBusy = true;
        StatusText = $"Exporting profile to {filePath}...";

        bool result = await Task.Run(() => _profileService.ExportProfile(SelectedProfile, filePath));
        
        StatusText = result ? $"Profile exported successfully to Documents/DTRL." : "Profile export failed.";
        IsBusy = false;
    }

    [RelayCommand]
    private async Task ImportProfileAsync()
    {
        // For CLI simulation, we import a simulated profile path.
        // In actual UI, FileOpenPicker will resolve the path.
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string filePath = Path.Combine(appData, "DTRL", "Profiles", "Custom.dtrl");

        if (!File.Exists(filePath))
        {
            StatusText = "No profile template custom file found to import. Create one by exporting first.";
            return;
        }

        IsBusy = true;
        StatusText = "Importing profile configurations...";

        var profile = await Task.Run(() => _profileService.ImportProfile(filePath));
        if (profile != null)
        {
            Profiles.Add(profile);
            SelectedProfile = profile;
            StatusText = $"Successfully imported profile '{profile.Name}'.";
        }
        else
        {
            StatusText = "Failed to import profile. File format is invalid.";
        }
        IsBusy = false;
    }
}
