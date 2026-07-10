using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Dtrl.Models;
using Dtrl.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Dtrl.Views;

public sealed partial class NetworkPage : Page
{
    public NetworkViewModel ViewModel { get; }

    public NetworkPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<NetworkViewModel>();
    }

    public static Visibility GetVisibility(bool val)
    {
        return val ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void TweakToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggle && toggle.DataContext is Tweak tweak)
        {
            if (tweak.IsApplied == toggle.IsOn) return;

            tweak.IsApplied = toggle.IsOn;
            await ViewModel.ToggleTweakCommand.ExecuteAsync(tweak);
        }
    }
}
