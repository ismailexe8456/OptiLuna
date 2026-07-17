using System.Threading.Tasks;
using NXG.Models;

namespace NXG.Services;

public interface IBenchmarkService
{
    Task<BenchmarkResult> RunBenchmarkSuiteAsync(string testDirectoryPath);
}
