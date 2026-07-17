using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using NXG.Models;

namespace NXG.Services;

public class ProfileService : IProfileService
{
    private readonly List<ProfileModel> _profiles = new();
    private readonly ILoggingService _logger;

    public ProfileService(ILoggingService logger)
    {
        _logger = logger;
        InitializeBuiltInProfiles();
    }

    private void InitializeBuiltInProfiles()
    {
        // 1. CS2 (Counter-Strike 2) Profile
        _profiles.Add(new ProfileModel
        {
            Name = "CS2 Optimization",
            Description = "Enables low-latency networking, GPU scheduling, Game Mode, and highest CPU multimedia priority for CS2.",
            IsBuiltIn = true,
            EnabledTweakIds = new List<string> {
                "net_tcp_autotune", "game_net_throttling", "perf_mmcss_games_priority", 
                "perf_mmcss_games_gpu", "game_hags", "game_mode", "game_dvr_enable"
            }
        });

        // 2. Minecraft Profile
        _profiles.Add(new ProfileModel
        {
            Name = "Minecraft Optimization",
            Description = "Focuses on RAM cache stability, disabling window overlays, and improving garbage collector threads.",
            IsBuiltIn = true,
            EnabledTweakIds = new List<string> {
                "perf_disable_paging_exec", "game_hags", "game_mode", "game_fso_disable", "vis_win_animations"
            }
        });

        // 3. Valorant Profile
        _profiles.Add(new ProfileModel
        {
            Name = "Valorant Low Latency",
            Description = "Optimizes for raw input responsiveness, disabling full-screen overlay stutters, and setting maximum GPU priority.",
            IsBuiltIn = true,
            EnabledTweakIds = new List<string> {
                "game_fso_disable", "game_hags", "game_mode", "perf_responsiveness", "game_net_throttling"
            }
        });

        // 4. Fortnite Profile
        _profiles.Add(new ProfileModel
        {
            Name = "Fortnite Performance",
            Description = "Maximizes CPU core scaling, disables idle network pooling, and minimizes background services during matches.",
            IsBuiltIn = true,
            EnabledTweakIds = new List<string> {
                "game_mode", "game_dvr_enable", "net_tcp_autotune", "perf_responsiveness", "svc_WerSvc"
            }
        });

        // 5. GTA V Profile
        _profiles.Add(new ProfileModel
        {
            Name = "GTA V High Performance",
            Description = "Focuses on memory management, disabling system telemetry alerts, and freeing pagefile space.",
            IsBuiltIn = true,
            EnabledTweakIds = new List<string> {
                "perf_disable_paging_exec", "stor_last_access", "stor_ntfs_memory", "tel_diagtrack"
            }
        });

        // 6. Roblox Profile
        _profiles.Add(new ProfileModel
        {
            Name = "Roblox Ping Stabilizer",
            Description = "Lowers network ping jitter, flushes network buffers, and optimizes network interface adapters.",
            IsBuiltIn = true,
            EnabledTweakIds = new List<string> {
                "net_tcp_autotune", "net_tcp_chimney", "game_net_throttling", "vis_transparency"
            }
        });

        // 7. Apex Legends Profile
        _profiles.Add(new ProfileModel
        {
            Name = "Apex Legends Frametime Stabilizer",
            Description = "Reduces rendering lag, sets high MMCSS game thread priority, and minimizes visual overlays.",
            IsBuiltIn = true,
            EnabledTweakIds = new List<string> {
                "game_hags", "perf_mmcss_games_priority", "perf_mmcss_games_gpu", "game_dvr_enable", "vis_transparency"
            }
        });

        // 8. Battery Optimization
        _profiles.Add(new ProfileModel
        {
            Name = "Battery Saver (Laptops)",
            Description = "Disables non-essential background diagnostics, location services, and transparency to save power.",
            IsBuiltIn = true,
            EnabledTweakIds = new List<string> {
                "vis_transparency", "vis_win_animations", "svc_lfsvc", "svc_WerSvc", 
                "tel_diagtrack", "tel_dmwappushservice", "priv_activity_history"
            }
        });

        // 9. Maximum Performance
        _profiles.Add(new ProfileModel
        {
            Name = "Maximum Performance Suite",
            Description = "Enables all safe performance, network, storage, and telemetry optimizations in NXG.",
            IsBuiltIn = true,
            EnabledTweakIds = new List<string> {
                "tel_diagtrack", "tel_dmwappushservice", "tel_allow_telemetry", "priv_ads_id",
                "priv_tailored_exp", "priv_activity_history", "perf_responsiveness", "perf_search_indexer",
                "perf_kill_service_timeout", "perf_auto_end_tasks", "perf_menu_delay", "game_hags",
                "game_mode", "game_dvr_enable", "game_net_throttling", "wu_exclude_drivers",
                "wu_delivery_opt", "deb_widgets", "deb_cortana", "deb_copilot", "deb_start_websearch",
                "svc_RemoteRegistry", "svc_Fax", "svc_MapsBroker", "svc_lfsvc", "svc_WalletService",
                "stor_last_access", "net_tcp_autotune", "net_tcp_chimney", "vis_win_animations",
                "vis_transparency", "pol_smartscreen"
            }
        });
    }

    public List<ProfileModel> GetProfiles()
    {
        return _profiles;
    }

    public bool ExportProfile(ProfileModel profile, string filePath)
    {
        _logger.Log("Export Profile", $"Exporting profile '{profile.Name}' to: {filePath}");
        try
        {
            var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Export Profile Failed", $"Could not export profile. Error: {ex.Message}");
            return false;
        }
    }

    public ProfileModel? ImportProfile(string filePath)
    {
        _logger.Log("Import Profile", $"Importing profile from: {filePath}");
        try
        {
            if (!File.Exists(filePath)) return null;
            var json = File.ReadAllText(filePath);
            var profile = JsonSerializer.Deserialize<ProfileModel>(json);
            if (profile != null)
            {
                profile.IsBuiltIn = false;
                // Add to list or return it
                return profile;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Import Profile Failed", $"Could not parse profile file. Error: {ex.Message}");
        }
        return null;
    }

    public void ApplyProfile(ProfileModel profile, ITweakService tweakService)
    {
        _logger.Log("Apply Profile", $"Applying profile package '{profile.Name}' (contains {profile.EnabledTweakIds.Count} tweaks)...");
        
        int successCount = 0;
        var tweaks = tweakService.GetTweaks();
        
        foreach (var id in profile.EnabledTweakIds)
        {
            var tweak = tweaks.Find(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
            if (tweak != null)
            {
                if (tweakService.ApplyTweak(tweak))
                {
                    successCount++;
                }
            }
        }

        _logger.Log("Profile Applied", $"Applied profile '{profile.Name}': {successCount}/{profile.EnabledTweakIds.Count} tweaks successfully executed.");
    }
}
