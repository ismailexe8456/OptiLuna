using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NXG.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace NXG.Views;

public sealed partial class FocusModePage : Page
{
    public FocusModeViewModel ViewModel { get; }

    public FocusModePage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<FocusModeViewModel>();
    }

    public static string GetButtonText(bool isActive)
    {
        return isActive ? "Stop Focus Session" : "Start Focus Session";
    }

    public static bool Not(bool val)
    {
        return !val;
    }

    private void PresetButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string param && int.TryParse(param, out int mins))
        {
            ViewModel.SelectPresetCommand.Execute(mins);
        }
    }

    private void RemoveAppButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string app)
        {
            ViewModel.RemoveBlockedAppCommand.Execute(app);
        }
    }
}
