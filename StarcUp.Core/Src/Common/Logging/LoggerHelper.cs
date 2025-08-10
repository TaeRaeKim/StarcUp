using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace StarcUp.Common.Logging
{
    /// <summary>
    /// 전역 로거 헬퍼 클래스 - 의존성 주입이 어려운 곳에서 사용
    /// </summary>
    public static class LoggerHelper
    {
        private static ILoggerFactory? _loggerFactory;
        private static readonly object _lock = new object();

        /// <summary>
        /// LoggerFactory 인스턴스 설정 (애플리케이션 시작 시 한 번만 호출)
        /// </summary>
        public static void Initialize(ILoggerFactory loggerFactory)
        {
            lock (_lock)
            {
                _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            }
        }

        /// <summary>
        /// 호출한 클래스명을 자동으로 감지하여 로거 생성
        /// </summary>
        private static ILogger GetLogger([CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "")
        {
            lock (_lock)
            {
                if (_loggerFactory == null)
                {
                    // 초기화되지 않은 경우 콘솔 로거 사용
                    return new ConsoleLogger(LogLevel.Debug);
                }

                // 파일 경로에서 클래스명 추출
                var fileName = System.IO.Path.GetFileNameWithoutExtension(sourceFilePath);
                return _loggerFactory.CreateLogger(fileName);
            }
        }

        // 편의 메서드들 - 호출한 클래스명을 자동으로 감지
        public static void Debug(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "") 
            => GetLogger(memberName, sourceFilePath).Debug(message);
        
        public static void Info(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "") 
            => GetLogger(memberName, sourceFilePath).Info(message);
        
        public static void Warning(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "") 
            => GetLogger(memberName, sourceFilePath).Warning(message);
        
        public static void Error(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "") 
            => GetLogger(memberName, sourceFilePath).Error(message);
        
        public static void Error(string message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "") 
            => GetLogger(memberName, sourceFilePath).Error(message, exception);
        
        public static void Fatal(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "") 
            => GetLogger(memberName, sourceFilePath).Fatal(message);
        
        public static void Fatal(string message, Exception exception, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "") 
            => GetLogger(memberName, sourceFilePath).Fatal(message, exception);
    }
}