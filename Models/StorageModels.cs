using System.Collections.Generic;

namespace Dtrl.Models;

public class DuplicateGroup
{
    public string GroupName { get; set; } = string.Empty;
    public List<DiskItem> Files { get; set; } = new();
}
