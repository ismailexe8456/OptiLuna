using System.Collections.Generic;

namespace Dtrl.Models;

public class DiskItem
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public bool IsDirectory { get; set; }
    
    // Bounds for Treemap drawing (percentage coordinates)
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    
    public List<DiskItem> Children { get; set; } = new();
}
