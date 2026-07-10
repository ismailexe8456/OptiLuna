using System.Threading.Tasks;
using Dtrl.Models;

namespace Dtrl.Services;

public interface IBenchmarkService
{
    Task<BenchmarkResult> RunBenchmarkSuiteAsync(string testDirectoryPath);
}
