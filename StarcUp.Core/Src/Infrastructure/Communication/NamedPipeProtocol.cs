using System;
using System.Text.Json;

namespace StarcUp.Infrastructure.Communication
{
    /// <summary>
    /// Named Pipe 통신 프로토콜 정의
    /// </summary>
    public static class NamedPipeProtocol
    {
        /// <summary>
        /// 메시지 타입
        /// </summary>
        public enum MessageType
        {
            Request,    // 요청
            Response,   // 응답
            Event       // 이벤트 (단방향)
        }

        /// <summary>
        /// 기본 메시지 구조
        /// </summary>
        public class BaseMessage
        {
            public string Id { get; set; }
            public MessageType Type { get; set; }
            public long Timestamp { get; set; }

            public BaseMessage()
            {
                Id = $"msg_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid().ToString("N")[..8]}";
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }

        /// <summary>
        /// 요청 메시지
        /// </summary>
        public class RequestMessage : BaseMessage
        {
            public string Command { get; set; }
            public JsonElement? Payload { get; set; }

            public RequestMessage() : base()
            {
                Type = MessageType.Request;
            }

            public RequestMessage(string command, object payload = null) : this()
            {
                Command = command;
                if (payload != null)
                {
                    var json = JsonSerializer.Serialize(payload);
                    Payload = JsonDocument.Parse(json).RootElement;
                }
            }
        }

        /// <summary>
        /// 응답 메시지
        /// </summary>
        public class ResponseMessage : BaseMessage
        {
            public string RequestId { get; set; }
            public bool Success { get; set; }
            public JsonElement? Data { get; set; }
            public string Error { get; set; }

            public ResponseMessage() : base()
            {
                Type = MessageType.Response;
            }

            public ResponseMessage(string requestId, bool success, object data = null, string error = null) : this()
            {
                RequestId = requestId;
                Success = success;
                Error = error;
                
                if (data != null)
                {
                    var json = JsonSerializer.Serialize(data);
                    Data = JsonDocument.Parse(json).RootElement;
                }
            }
        }

        /// <summary>
        /// 이벤트 메시지
        /// </summary>
        public class EventMessage : BaseMessage
        {
            public string Event { get; set; }
            public JsonElement? Data { get; set; }

            public EventMessage() : base()
            {
                Type = MessageType.Event;
            }

            public EventMessage(string eventName, object data = null) : this()
            {
                Event = eventName;
                if (data != null)
                {
                    var json = JsonSerializer.Serialize(data);
                    Data = JsonDocument.Parse(json).RootElement;
                }
            }
        }

        /// <summary>
        /// 지원되는 명령 목록
        /// </summary>
        public static class Commands
        {
            public const string Ping = "ping";
            public const string StartGameDetect = "start-game-detect";
            public const string StopGameDetect = "stop-game-detect";
            public const string GetGameStatus = "get-game-status";
            public const string GetUnitCounts = "get-unit-counts";
            public const string GetPlayerInfo = "get-player-info";
            
            // 프리셋 관련 명령
            public const string PresetInit = "preset-init";
            public const string PresetUpdate = "preset-update";
        }

        /// <summary>
        /// 지원되는 이벤트 목록
        /// </summary>
        public static class Events
        {
            // 게임 프로세스 관련 이벤트
            public const string GameDetected = "game-detected";
            public const string GameEnded = "game-ended";
            public const string GameStatusChanged = "game-status-changed";
            
            // WorkerManager 관련 이벤트
            public const string WorkerStatusChanged = "worker-status-changed";
            public const string GasBuildingAlert = "gas-building-alert";
            public const string WorkerPresetChanged = "worker-preset-changed";
            
            // PopulationManager 관련 이벤트
            public const string SupplyAlert = "supply-alert";
            
            // UpgradeManager 관련 이벤트
            public const string UpgradeDataUpdated = "upgrade-data-updated";
            public const string UpgradeStateChanged = "upgrade-state-changed";
            public const string UpgradeCompleted = "upgrade-completed";
            public const string UpgradeCancelled = "upgrade-cancelled";
            public const string UpgradeInit = "upgrade-init";
        }
    }
}