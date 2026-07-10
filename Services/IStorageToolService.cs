using System.Collections.Generic;
using System.Threading.Tasks;
using Dtrl.Models;

namespace Dtrl.Services;

public interface IStorageToolService
{
    Task<List<DiskItem>> ScanLargeFilesAsync(string path, long minSizeBytes = 52428800, int limit = 100);
    Task<List<List<DiskItem>>> ScanDuplicatesAsync(string path, string searchPattern = "*.*");
    Task<DiskItem> BuildTreemapDataAsync(string path, int maxDepth = 3);
    void LayoutTreemap(DiskItem root, double x, double y, double width, double height);
    Task<Dictionary<string, long>> GetCacheSizesAsync();
    Task<long> CleanCacheAsync(string cacheType);
}
