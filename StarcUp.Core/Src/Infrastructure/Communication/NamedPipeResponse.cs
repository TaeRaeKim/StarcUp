using System;
using System.Text.Json.Serialization;

namespace StarcUp.Infrastructure.Communication
{
    /// <summary>
    /// Named Pipes 서버 응답 데이터 구조
    /// </summary>
    public class NamedPipeResponse
    {
        /// <summary>
        /// 응답 ID (요청 ID와 매칭)
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// 응답 성공 여부
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// 응답 데이터
        /// </summary>
        [JsonPropertyName("data")]
        public object Data { get; set; }

        /// <summary>
        /// 오류 메시지
        /// </summary>
        [JsonPropertyName("error")]
        public string Error { get; set; }

        /// <summary>
        /// 응답 타임스탬프
        /// </summary>
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        /// <summary>
        /// 성공 응답 생성
        /// </summary>
        /// <param name="id">응답 ID</param>
        /// <param name="data">응답 데이터</param>
        /// <returns>성공 응답</returns>
        public static NamedPipeResponse CreateSuccess(string id, object data = null)
        {
            return new NamedPipeResponse
            {
                Id = id,
                Success = true,
                Data = data,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }

        /// <summary>
        /// 실패 응답 생성
        /// </summary>
        /// <param name="id">응답 ID</param>
        /// <param name="error">오류 메시지</param>
        /// <returns>실패 응답</returns>
        public static NamedPipeResponse CreateError(string id, string error)
        {
            return new NamedPipeResponse
            {
                Id = id,
                Success = false,
                Error = error,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }
    }
}