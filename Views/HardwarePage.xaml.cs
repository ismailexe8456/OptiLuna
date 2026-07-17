using Microsoft.UI.Xaml.Controls;
using NXG.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace NXG.Views;

public sealed partial class HardwarePage : Page
{
    public DashboardViewModel ViewModel { get; }

    public HardwarePage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<DashboardViewModel>();
    }
}
