using System.Collections.Generic;

namespace NXG.Models;

public class DuplicateGroup
{
    public string GroupName { get; set; } = string.Empty;
    public List<DiskItem> Files { get; set; } = new();
}
