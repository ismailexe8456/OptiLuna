using Microsoft.UI.Xaml.Controls;
using Dtrl.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Dtrl.Views;

public sealed partial class HardwarePage : Page
{
    public DashboardViewModel ViewModel { get; }

    public HardwarePage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<DashboardViewModel>();
    }
}
