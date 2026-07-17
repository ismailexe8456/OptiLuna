using Microsoft.UI.Xaml.Controls;
using NXG.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace NXG.Views;

public sealed partial class LogsPage : Page
{
    public LogsViewModel ViewModel { get; }

    public LogsPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<LogsViewModel>();
    }
}
