using Microsoft.UI.Xaml.Controls;
using NXG.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace NXG.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<DashboardViewModel>();
    }
}
