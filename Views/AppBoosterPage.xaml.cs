using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Dtrl.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Dtrl.Views;

public sealed partial class AppBoosterPage : Page
{
    public AppBoosterViewModel ViewModel { get; }

    public AppBoosterPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<AppBoosterViewModel>();
    }


    private async void BoostButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string gameName)
        {
            await ViewModel.ToggleBoostCommand.ExecuteAsync(gameName);
            
            // Show content dialog indicating status
            var dialog = new ContentDialog
            {
                Title = ViewModel.IsBoostActive ? "Boost Sequence Engaged" : "Boost Terminated",
                Content = ViewModel.IsBoostActive 
                    ? $"Optimized system threads successfully. Process '{gameName}' priority raised to High." 
                    : $"Boost sequence terminated. Restored standard priority and background services.",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    private void RemoveCustomButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string name)
        {
            ViewModel.RemoveCustomGameCommand.Execute(name);
        }
    }
}
