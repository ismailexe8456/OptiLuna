using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Dtrl.Models;

public partial class Tweak : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TweakCategory Category { get; set; }
    public RiskLevel Risk { get; set; }
    public string EstimatedImpact { get; set; } = string.Empty;
    public bool RestartRequired { get; set; }

    // Optimization Target Type: "Registry", "Service", "Shell"
    public string TargetType { get; set; } = "Registry";

    // Registry fields
    public string RegistryHive { get; set; } = "HKCU"; // "HKCU", "HKLM"
    public string RegistryPath { get; set; } = string.Empty;
    public string RegistryValueName { get; set; } = string.Empty;
    public string RegistryType { get; set; } = "DWord"; // "DWord", "String", "QWord", "Binary"
    public object ActiveValue { get; set; } = 0;
    public object UndoValue { get; set; } = 1;

    // Service fields
    public string ServiceName { get; set; } = string.Empty;
    public int ActiveStartupType { get; set; } = 4; // 2=Auto, 3=Manual, 4=Disabled
    public int UndoStartupType { get; set; } = 3;

    // Shell/PowerShell fields
    public string ShellCommand { get; set; } = string.Empty;
    public string ShellUndo { get; set; } = string.Empty;

    // Dynamic state
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AppliedVisibility))]
    private bool _isApplied;

    public Microsoft.UI.Xaml.Visibility RestartVisibility => RestartRequired ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
    public Microsoft.UI.Xaml.Visibility AppliedVisibility => IsApplied ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    public string RiskText => Risk.ToString();
    public string CategoryText => Category.ToString();
}
