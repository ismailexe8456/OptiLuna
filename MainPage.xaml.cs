using System;
using Microsoft.UI.Xaml.Controls;
using NXG.Views;
using NXG.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace NXG;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }

    public MainPage()
    {
        InitializeComponent();
        ViewModel = App.Services.GetRequiredService<MainViewModel>();
        
        // Navigate content frame to Dashboard on start
        ContentFrame.Navigate(typeof(DashboardPage));
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
            if (args.IsSettingsSelected)
            {
                ContentFrame.Navigate(typeof(SettingsPage));
                sender.Header = "NXG Settings";
                return;
            }

            if (args.SelectedItemContainer != null)
            {
                string tag = args.SelectedItemContainer.Tag.ToString() ?? "Dashboard";
                sender.Header = $"NXG - {args.SelectedItemContainer.Content}";

                Type pageType = tag switch
                {
                    "Dashboard" => typeof(DashboardPage),
                    "Tweaks" => typeof(TweaksPage),
                    "Profiles" => typeof(ProfilesPage),
                    "PowerPlan" => typeof(PowerPlanPage),
                    "AppBooster" => typeof(AppBoosterPage),
                    "FocusMode" => typeof(FocusModePage),
                    "Hardware" => typeof(HardwarePage),
                    "Storage" => typeof(StoragePage),
                    "Network" => typeof(NetworkPage),
                    "SystemInfo" => typeof(SystemInfoPage),
                    "Benchmarks" => typeof(BenchmarkPage),
                    "Recovery" => typeof(RecoveryPage),
                    "Logs" => typeof(LogsPage),
                    _ => typeof(DashboardPage)
                };

                ContentFrame.Navigate(pageType);
            }
    }

    private void SearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        string query = sender.Text.ToLower().Trim();
        if (string.IsNullOrEmpty(query)) return;

        // Perform navigation search match shortcuts
        if (query.Contains("tweak") || query.Contains("optimize"))
        {
            ContentFrame.Navigate(typeof(TweaksPage));
            NavView.Header = "NXG - System Tweaks";
        }
        else if (query.Contains("hard") || query.Contains("cpu") || query.Contains("gpu") || query.Contains("ram"))
        {
            ContentFrame.Navigate(typeof(HardwarePage));
            NavView.Header = "NXG - Hardware Monitor";
        }
        else if (query.Contains("network") || query.Contains("ping") || query.Contains("dns"))
        {
            ContentFrame.Navigate(typeof(NetworkPage));
            NavView.Header = "NXG - Network Diagnostics";
        }
        else if (query.Contains("clean") || query.Contains("storage") || query.Contains("temp") || query.Contains("space"))
        {
            ContentFrame.Navigate(typeof(StoragePage));
            NavView.Header = "NXG - Storage Clean & Map";
        }
        else if (query.Contains("bench"))
        {
            ContentFrame.Navigate(typeof(BenchmarkPage));
            NavView.Header = "NXG - Benchmarks";
        }
        else if (query.Contains("restore") || query.Contains("recovery") || query.Contains("undo"))
        {
            ContentFrame.Navigate(typeof(RecoveryPage));
            NavView.Header = "NXG - Restore & Undo";
        }
    }
}
