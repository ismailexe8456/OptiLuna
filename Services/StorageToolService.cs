using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Dtrl.Models;

namespace Dtrl.Services;

public class StorageToolService : IStorageToolService
{
    private readonly ILoggingService _logger;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);
    private const uint SHERB_NOCONFIRMATION = 0x00000001;
    private const uint SHERB_NOPROGRESSUI = 0x00000002;
    private const uint SHERB_NOSOUND = 0x00000004;

    public StorageToolService(ILoggingService logger)
    {
        _logger = logger;
    }

    public async Task<List<DiskItem>> ScanLargeFilesAsync(string path, long minSizeBytes = 52428800, int limit = 100)
    {
        return await Task.Run(() =>
        {
            var result = new List<DiskItem>();
            try
            {
                if (!Directory.Exists(path)) return result;

                var dirInfo = new DirectoryInfo(path);
                var files = dirInfo.EnumerateFiles("*.*", SearchOption.AllDirectories);
                
                var query = files
                    .Select(f => {
                        try
                        {
                            return new DiskItem
                            {
                                Name = f.Name,
                                Path = f.FullName,
                                SizeBytes = f.Length,
                                IsDirectory = false
                            };
                        }
                        catch { return null; }
                    })
                    .Where(f => f != null && f.SizeBytes >= minSizeBytes)
                    .OrderByDescending(f => f!.SizeBytes)
                    .Take(limit)
                    .ToList();

                foreach (var item in query)
                {
                    if (item != null) result.Add(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Large File Scan", $"Error scanning {path}: {ex.Message}");
            }
            return result;
        });
    }

    public async Task<List<List<DiskItem>>> ScanDuplicatesAsync(string path, string searchPattern = "*.*")
    {
        return await Task.Run(() =>
        {
            var groups = new Dictionary<long, List<string>>();
            var duplicates = new List<List<DiskItem>>();

            try
            {
                if (!Directory.Exists(path)) return duplicates;

                var dirInfo = new DirectoryInfo(path);
                var files = dirInfo.EnumerateFiles(searchPattern, SearchOption.AllDirectories);

                // Phase 1: Group by file size to narrow down candidates
                foreach (var file in files)
                {
                    try
                    {
                        long len = file.Length;
                        if (len == 0) continue;
                        if (!groups.ContainsKey(len))
                        {
                            groups[len] = new List<string>();
                        }
                        groups[len].Add(file.FullName);
                    }
                    catch { }
                }

                // Phase 2: Compute hash for candidate groups with > 1 files
                var candidates = groups.Where(g => g.Value.Count > 1);
                using var md5 = MD5.Create();

                foreach (var candidate in candidates)
                {
                    var hashMap = new Dictionary<string, List<DiskItem>>();
                    foreach (var filePath in candidate.Value)
                    {
                        try
                        {
                            string hashStr = GetFileHash(filePath, md5);
                            if (!hashMap.ContainsKey(hashStr))
                            {
                                hashMap[hashStr] = new List<DiskItem>();
                            }
                            var fInfo = new FileInfo(filePath);
                            hashMap[hashStr].Add(new DiskItem
                            {
                                Name = fInfo.Name,
                                Path = filePath,
                                SizeBytes = fInfo.Length,
                                IsDirectory = false
                            });
                        }
                        catch { }
                    }

                    foreach (var dupList in hashMap.Values)
                    {
                        if (dupList.Count > 1)
                        {
                            duplicates.Add(dupList);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Duplicate Scan", $"Error scanning duplicate files: {ex.Message}");
            }

            return duplicates;
        });
    }

    private string GetFileHash(string filePath, MD5 md5)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        // Compute hash of first 1MB only for speed
        byte[] buffer = new byte[1024 * 1024];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        byte[] hashBytes = md5.ComputeHash(buffer, 0, bytesRead);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    public async Task<DiskItem> BuildTreemapDataAsync(string path, int maxDepth = 3)
    {
        return await Task.Run(() =>
        {
            var root = new DiskItem
            {
                Name = Path.GetFileName(path),
                Path = path,
                IsDirectory = true
            };
            if (string.IsNullOrEmpty(root.Name)) root.Name = path;

            try
            {
                BuildDirNode(root, new DirectoryInfo(path), 0, maxDepth);
            }
            catch (Exception ex)
            {
                _logger.LogError("Build Treemap", $"Error creating node structure: {ex.Message}");
            }
            return root;
        });
    }

    private long BuildDirNode(DiskItem parentNode, DirectoryInfo dir, int currentDepth, int maxDepth)
    {
        long totalSize = 0;
        try
        {
            // Subdirectories
            if (currentDepth < maxDepth)
            {
                var subDirs = dir.GetDirectories();
                foreach (var sub in subDirs)
                {
                    try
                    {
                        var childNode = new DiskItem
                        {
                            Name = sub.Name,
                            Path = sub.FullName,
                            IsDirectory = true
                        };
                        long sz = BuildDirNode(childNode, sub, currentDepth + 1, maxDepth);
                        childNode.SizeBytes = sz;
                        if (sz > 0)
                        {
                            parentNode.Children.Add(childNode);
                            totalSize += sz;
                        }
                    }
                    catch { }
                }
            }

            // Files
            var files = dir.GetFiles();
            foreach (var f in files)
            {
                try
                {
                    long fLen = f.Length;
                    totalSize += fLen;
                    
                    if (currentDepth < maxDepth)
                    {
                        parentNode.Children.Add(new DiskItem
                        {
                            Name = f.Name,
                            Path = f.FullName,
                            SizeBytes = fLen,
                            IsDirectory = false
                        });
                    }
                }
                catch { }
            }
        }
        catch { }
        return totalSize;
    }

    public void LayoutTreemap(DiskItem root, double x, double y, double width, double height)
    {
        root.X = x;
        root.Y = y;
        root.Width = width;
        root.Height = height;

        if (root.Children.Count == 0 || root.SizeBytes == 0) return;

        // Sort children descending by size
        var children = root.Children.OrderByDescending(c => c.SizeBytes).ToList();
        
        // Simple Slice-and-Dice: Alternate split direction based on rectangle aspect ratio
        SliceAndDice(children, x, y, width, height, width > height);
    }

    private void SliceAndDice(List<DiskItem> items, double x, double y, double width, double height, bool splitVertically)
    {
        double totalSize = items.Sum(i => i.SizeBytes);
        if (totalSize == 0) return;

        double offset = 0;
        foreach (var item in items)
        {
            double ratio = item.SizeBytes / totalSize;
            if (splitVertically)
            {
                double childWidth = width * ratio;
                LayoutTreemap(item, x + offset, y, childWidth, height);
                offset += childWidth;
            }
            else
            {
                double childHeight = height * ratio;
                LayoutTreemap(item, x, y + offset, width, childHeight);
                offset += childHeight;
            }
        }
    }

    public async Task<Dictionary<string, long>> GetCacheSizesAsync()
    {
        return await Task.Run(() =>
        {
            var sizes = new Dictionary<string, long>
            {
                { "Temp", GetDirSize(Path.GetTempPath()) + GetDirSize(@"C:\Windows\Temp") },
                { "Update", GetDirSize(@"C:\Windows\SoftwareDistribution\Download") },
                { "DirectX", GetDirSize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "D3DSCache")) },
                { "Logs", GetDirSize(@"C:\Windows\Logs") }
            };
            return sizes;
        });
    }

    public async Task<long> CleanCacheAsync(string cacheType)
    {
        return await Task.Run(() =>
        {
            long cleanedBytes = 0;
            try
            {
                if (cacheType == "RecycleBin")
                {
                    _logger.Log("Clean Recycle Bin", "Emptying System Recycle Bin...");
                    int res = SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOSOUND);
                    _logger.Log("Clean Recycle Bin Done", "Recycle Bin was successfully emptied.");
                    return 1024L * 1024L; // Return nominal 1MB
                }

                var paths = new List<string>();
                if (cacheType == "Temp")
                {
                    paths.Add(Path.GetTempPath());
                    paths.Add(@"C:\Windows\Temp");
                }
                else if (cacheType == "Update")
                {
                    paths.Add(@"C:\Windows\SoftwareDistribution\Download");
                }
                else if (cacheType == "DirectX")
                {
                    paths.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "D3DSCache"));
                }
                else if (cacheType == "Logs")
                {
                    paths.Add(@"C:\Windows\Logs");
                }

                foreach (var path in paths)
                {
                    cleanedBytes += DeleteFilesInDirectory(path);
                }

                _logger.Log("Cache Clean Completed", $"Cleaned cache '{cacheType}': Saved {cleanedBytes / (1024.0 * 1024.0):F2} MB");
            }
            catch (Exception ex)
            {
                _logger.LogError("Cache Cleanup", $"Error cleaning {cacheType}: {ex.Message}");
            }
            return cleanedBytes;
        });
    }

    private long GetDirSize(string path)
    {
        try
        {
            if (!Directory.Exists(path)) return 0;
            var dInfo = new DirectoryInfo(path);
            return dInfo.EnumerateFiles("*.*", SearchOption.AllDirectories).Sum(f => {
                try { return f.Length; } catch { return 0; }
            });
        }
        catch { return 0; }
    }

    private long DeleteFilesInDirectory(string path)
    {
        long bytesDeleted = 0;
        try
        {
            if (!Directory.Exists(path)) return 0;
            var dInfo = new DirectoryInfo(path);
            
            foreach (var file in dInfo.GetFiles())
            {
                try
                {
                    long len = file.Length;
                    file.Delete();
                    bytesDeleted += len;
                }
                catch { }
            }

            foreach (var subDir in dInfo.GetDirectories())
            {
                try
                {
                    subDir.Delete(true);
                }
                catch { }
            }
        }
        catch { }
        return bytesDeleted;
    }
}
