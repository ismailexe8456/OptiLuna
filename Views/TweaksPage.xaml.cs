using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NXG.Models;
using NXG.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace NXG.Views;

public sealed partial class TweaksPage : Page
{
    public TweaksViewModel ViewModel { get; }

    public TweaksPage()
    {
        ViewModel = App.Services.GetRequiredService<TweaksViewModel>();
        this.InitializeComponent();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ViewModel.FilterTweaks();
    }

    private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.FilterTweaks();
    }

    private void CategoryChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button clickedButton && clickedButton.Tag is string category)
        {
            ViewModel.SelectedCategory = category;
            ViewModel.FilterTweaks();
        }
    }

    private async void TweakToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggle && toggle.DataContext is Tweak tweak)
        {
            if (tweak.IsApplied == toggle.IsOn) return;

            if (toggle.IsOn && !ViewModel.DisableConfirmations && (tweak.Risk == RiskLevel.Dangerous || tweak.Risk == RiskLevel.Advanced))
            {
                var checkBox = new CheckBox
                {
                    Content = "Don't show this again",
                    Margin = new Thickness(0, 16, 0, 0)
                };

                var contentPanel = new StackPanel { Spacing = 8 };
                contentPanel.Children.Add(new TextBlock
                {
                    Text = $"Applying this tweak alters low-level system settings:\n\nTweak: {tweak.Name}\nDescription: {tweak.Description}\nEstimated Impact: {tweak.EstimatedImpact}\n\nDo you want to proceed with applying this change?",
                    TextWrapping = TextWrapping.Wrap
                });
                contentPanel.Children.Add(checkBox);

                var dialog = new ContentDialog
                {
                    Title = $"{tweak.Risk} Configuration Alert",
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
            await ViewModel.ToggleTweakCommand.ExecuteAsync(tweak);
        }
    }
}
