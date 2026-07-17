using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NXG.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace NXG.Views;

public sealed partial class SystemInfoPage : Page
{
    public SystemInfoViewModel ViewModel { get; }

    public SystemInfoPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<SystemInfoViewModel>();
    }

    public static Visibility GetVisibility(bool val)
    {
        return val ? Visibility.Visible : Visibility.Collapsed;
    }
}
