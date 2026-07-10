using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Dtrl.Models;
using Dtrl.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Dtrl.Views;

public sealed partial class PowerPlanPage : Page
{
    public PowerPlanViewModel ViewModel { get; }

    public PowerPlanPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<PowerPlanViewModel>();
    }

    public static Visibility GetVisibility(bool val)
    {
        return val ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TabPivot == null || ViewModel == null) return;

        switch (TabPivot.SelectedIndex)
        {
            case 0:
                ViewModel.SelectedTab = "Desktop";
                break;
            case 1:
                ViewModel.SelectedTab = "Laptop";
                break;
            case 2:
                ViewModel.SelectedTab = "Custom";
                break;
        }

        ViewModel.LoadSettings();
    }

    private async void PowerToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggle && toggle.DataContext is Tweak tweak)
        {
            if (tweak.IsApplied == toggle.IsOn) return;

            // Confirm if Dangerous
            if (toggle.IsOn && !ViewModel.DisableConfirmations && tweak.Risk == RiskLevel.Dangerous)
            {
                var checkBox = new CheckBox
                {
                    Content = "Don't show this again",
                    Margin = new Thickness(0, 16, 0, 0)
                };

                var contentPanel = new StackPanel { Spacing = 8 };
                contentPanel.Children.Add(new TextBlock
                {
                    Text = $"Warning: Applying '{tweak.Name}' can lead to higher processor temperatures and increased energy consumption.\n\nDo you want to proceed?",
                    TextWrapping = TextWrapping.Wrap
                });
                contentPanel.Children.Add(checkBox);

                var dialog = new ContentDialog
                {
                    Title = "Advanced Power Tweak Warning",
                    Content = contentPanel,
                    PrimaryButtonText = "Proceed",
                    CloseButtonText = "Cancel",
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result != ContentDialogResult.Primary)
                {
                    tweak.IsApplied = false;
                    toggle.IsOn = false;
                    return;
                }

                if (checkBox.IsChecked == true)
                {
                    ViewModel.DisableConfirmations = true;
                }
            }

            tweak.IsApplied = toggle.IsOn;
            await ViewModel.ToggleSettingCommand.ExecuteAsync(tweak);
        }
    }
}
