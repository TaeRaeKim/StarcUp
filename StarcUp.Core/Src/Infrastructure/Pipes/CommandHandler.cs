using System;
using System.Threading.Tasks;
using StarcUp.Business.GameDetection;
using StarcUp.DependencyInjection;

namespace StarcUp.Infrastructure.Pipes
{
    /// <summary>
    /// 명령 처리 핸들러
    /// </summary>
    public class CommandHandler : ICommandHandler
    {
        private readonly ServiceContainer _container;

        public CommandHandler(ServiceContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        /// <summary>
        /// 명령 처리
        /// </summary>
        /// <param name="command">명령</param>
        /// <param name="arguments">인자</param>
        /// <param name="commandId">명령 ID</param>
        /// <returns>처리 결과</returns>
        public async Task<string> HandleCommandAsync(string command, string[] arguments, string commandId)
        {
            try
            {
                Console.WriteLine($"🔄 명령 처리 시작: {command} (ID: {commandId})");

                var result = command.ToLower() switch
                {
                    "start-game-detect" => await HandleStartGameDetectAsync(arguments),
                    "stop-game-detect" => await HandleStopGameDetectAsync(arguments),
                    "get-game-status" => await HandleGetGameStatusAsync(arguments),
                    _ => $"UNKNOWN_COMMAND:{command}"
                };

                Console.WriteLine($"✅ 명령 처리 완료: {command} -> {result}");
                return $"SUCCESS:{result}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 명령 처리 실패: {command} -> {ex.Message}");
                return $"ERROR:{ex.Message}";
            }
        }

        /// <summary>
        /// 게임 감지 시작 명령 처리
        /// </summary>
        private async Task<string> HandleStartGameDetectAsync(string[] arguments)
        {
            try
            {
                var gameDetector = _container.Resolve<IGameDetector>();
                
                if (gameDetector.IsGameRunning)
                {
                    return "ALREADY_RUNNING";
                }

                gameDetector.StartDetection();
                
                // 감지 시작 후 잠시 대기하여 상태 확인
                await Task.Delay(500);
                
                return gameDetector.IsGameRunning ? "DETECTION_STARTED" : "DETECTION_STARTED_NO_GAME";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"게임 감지 시작 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 게임 감지 중지 명령 처리
        /// </summary>
        private async Task<string> HandleStopGameDetectAsync(string[] arguments)
        {
            try
            {
                var gameDetector = _container.Resolve<IGameDetector>();
                
                if (!gameDetector.IsGameRunning)
                {
                    return "NOT_RUNNING";
                }

                gameDetector.StopDetection();
                
                // 감지 중지 후 잠시 대기하여 상태 확인
                await Task.Delay(100);
                
                return "DETECTION_STOPPED";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"게임 감지 중지 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 게임 상태 조회 명령 처리
        /// </summary>
        private async Task<string> HandleGetGameStatusAsync(string[] arguments)
        {
            try
            {
                var gameDetector = _container.Resolve<IGameDetector>();
                
                var status = gameDetector.IsGameRunning ? "RUNNING" : "NOT_RUNNING";
                var gameInfo = gameDetector.CurrentGame != null ? 
                    $":{gameDetector.CurrentGame.ProcessId}" : "";
                
                return $"{status}{gameInfo}";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"게임 상태 조회 실패: {ex.Message}", ex);
            }
        }
    }
}