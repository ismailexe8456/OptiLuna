using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Dtrl.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Dtrl.Views;

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
