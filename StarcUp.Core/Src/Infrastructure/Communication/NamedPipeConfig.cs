using System;
using StarcUp.Common.Logging;

namespace StarcUp.Infrastructure.Communication
{
    /// <summary>
    /// Named Pipe 설정 관리 클래스
    /// </summary>
    public static class NamedPipeConfig
    {
        /// <summary>
        /// 프로덕션 환경 파이프 이름
        /// </summary>
        public const string ProductionPipeName = "StarcUp";

        /// <summary>
        /// 개발 환경 파이프 이름
        /// </summary>
        public const string DevelopmentPipeName = "StarcUp.Dev";

        /// <summary>
        /// 현재 환경이 개발 환경인지 확인
        /// Debug 빌드면 개발 환경, Release 빌드면 프로덕션 환경
        /// </summary>
        /// <returns>개발 환경이면 true, 프로덕션 환경이면 false</returns>
        public static bool IsDevelopmentEnvironment()
        {
            #if DEBUG
            return true;
            #else
            return false;
            #endif
        }

        /// <summary>
        /// 현재 환경에 맞는 파이프 이름 반환
        /// </summary>
        /// <returns>환경에 맞는 파이프 이름</returns>
        public static string GetPipeNameForCurrentEnvironment()
        {
            return IsDevelopmentEnvironment() ? DevelopmentPipeName : ProductionPipeName;
        }

        /// <summary>
        /// 환경 정보를 콘솔에 출력
        /// </summary>
        public static void PrintEnvironmentInfo()
        {
            var isDev = IsDevelopmentEnvironment();
            var pipeName = GetPipeNameForCurrentEnvironment();
            var environment = isDev ? "Development" : "Production";

            LoggerHelper.Info($"🌐 실행 환경: {environment}");
            LoggerHelper.Info($"📡 Named Pipe: {pipeName}");
            
            #if DEBUG
            LoggerHelper.Info("   빌드 구성: DEBUG");
            #else
            LoggerHelper.Info("   빌드 구성: RELEASE");
            #endif
        }
    }
}