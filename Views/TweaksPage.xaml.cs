using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Dtrl.Models;
using Dtrl.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Dtrl.Views;

public sealed partial class TweaksPage : Page
{
    public TweaksViewModel ViewModel { get; }

    public TweaksPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<TweaksViewModel>();
    }

    public static Visibility GetVisibility(bool val)
    {
        return val ? Visibility.Visible : Visibility.Collapsed;
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

            var itemsControl = FindParent<ItemsControl>(clickedButton);
            if (itemsControl != null)
            {
                UpdateChipSelectionVisuals(itemsControl, clickedButton);
            }
        }
    }

    private void ItemsControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ItemsControl itemsControl)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                UpdateChipSelectionVisuals(itemsControl, null);
            });
        }
    }

    private void UpdateChipSelectionVisuals(ItemsControl itemsControl, Button selectedButton)
    {
        for (int i = 0; i < itemsControl.Items.Count; i++)
        {
            var container = itemsControl.ContainerFromIndex(i) as ContentPresenter;
            if (container != null && VisualTreeHelper.GetChildrenCount(container) > 0)
            {
                var btn = VisualTreeHelper.GetChild(container, 0) as Button;
                if (btn != null)
                {
                    var category = itemsControl.Items[i] as string;
                    bool isSelected = (selectedButton != null && btn == selectedButton) || 
                                     (selectedButton == null && category == ViewModel.SelectedCategory);

                    if (isSelected)
                    {
                        btn.Style = (Style)Application.Current.Resources["AccentButtonStyle"];
                    }
                    else
                    {
                        btn.Style = (Style)Application.Current.Resources["DefaultButtonStyle"];
                    }
                }
            }
        }
    }

    private T FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        DependencyObject parentObject = VisualTreeHelper.GetParent(child);
        if (parentObject == null) return null;
        if (parentObject is T parent) return parent;
        return FindParent<T>(parentObject);
    }

    private async void TweakToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggle && toggle.DataContext is Tweak tweak)
        {
            // Stop trigger loop if already in sync
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

            // Sync model and apply/revert
            tweak.IsApplied = toggle.IsOn;
            await ViewModel.ToggleTweakCommand.ExecuteAsync(tweak);
        }
    }
}
