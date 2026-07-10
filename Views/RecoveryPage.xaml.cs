using Microsoft.UI.Xaml.Controls;
using Dtrl.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Dtrl.Views;

public sealed partial class RecoveryPage : Page
{
    public RecoveryViewModel ViewModel { get; }

    public RecoveryPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<RecoveryViewModel>();
    }
}
