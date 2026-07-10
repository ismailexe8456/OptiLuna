using System.Collections.Generic;

namespace Dtrl.Models;

public class ProfileModel
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsBuiltIn { get; set; } = false;
    public List<string> EnabledTweakIds { get; set; } = new();

    public Microsoft.UI.Xaml.Visibility BuiltInVisibility => IsBuiltIn ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
}
