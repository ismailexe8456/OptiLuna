using Microsoft.UI.Xaml.Controls;
using NXG.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace NXG.Views;

public sealed partial class RecoveryPage : Page
{
    public RecoveryViewModel ViewModel { get; }

    public RecoveryPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<RecoveryViewModel>();
    }
}
