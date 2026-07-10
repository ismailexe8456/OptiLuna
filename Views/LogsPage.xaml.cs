using Microsoft.UI.Xaml.Controls;
using Dtrl.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Dtrl.Views;

public sealed partial class LogsPage : Page
{
    public LogsViewModel ViewModel { get; }

    public LogsPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<LogsViewModel>();
    }
}
