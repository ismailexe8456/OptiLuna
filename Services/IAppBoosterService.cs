using System.Collections.Generic;
using System.Threading.Tasks;

namespace NXG.Services;

public interface IAppBoosterService
{
    List<string> GetDetectedGames();
    List<string> GetCustomGames();
    void AddCustomGame(string exePath);
    void RemoveCustomGame(string exePath);
    bool IsBoostActive { get; }
    string BoostedGameName { get; }
    Task<bool> StartBoostAsync(string gameExeName);
    Task<bool> StopBoostAsync();
}
