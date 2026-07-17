using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NXG.Models;
using NXG.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace NXG.Views;

public sealed partial class BenchmarkPage : Page
{
    public BenchmarkViewModel ViewModel { get; }

    public BenchmarkPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<BenchmarkViewModel>();
    }

    public static Visibility GetVisibility(bool val)
    {
        return val ? Visibility.Visible : Visibility.Collapsed;
    }

    public static Visibility GetResultsPanelVisibility(BenchmarkResult? result)
    {
        return result != null ? Visibility.Visible : Visibility.Collapsed;
    }
}
