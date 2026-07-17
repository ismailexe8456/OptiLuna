using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NXG.Models;
using NXG.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace NXG.Views;

public sealed partial class ProfilesPage : Page
{
    public ProfilesViewModel ViewModel { get; }

    public ProfilesPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<ProfilesViewModel>();
    }

    public static Visibility GetVisibility(bool val)
    {
        return val ? Visibility.Visible : Visibility.Collapsed;
    }

    public static Visibility GetProfilePanelVisibility(ProfileModel? model)
    {
        return model != null ? Visibility.Visible : Visibility.Collapsed;
    }
}
