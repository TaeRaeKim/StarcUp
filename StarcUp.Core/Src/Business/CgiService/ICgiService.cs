using System.Threading.Tasks;

namespace StarcUp.Core.Business.CgiService
{
    public interface ICgiService
    {
        Task StartGameDetectionAsync();
        Task StopGameDetectionAsync();
        Task<bool> IsGameDetectionRunningAsync();
        Task<string> GetGameStatusAsync();
    }
}