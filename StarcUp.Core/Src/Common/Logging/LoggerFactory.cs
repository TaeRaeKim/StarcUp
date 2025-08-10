using System;

namespace StarcUp.Common.Logging
{
    /// <summary>
    /// 클래스별 로거 인스턴스를 생성하는 팩토리
    /// </summary>
    public interface ILoggerFactory : IDisposable
    {
        ILogger CreateLogger<T>();
        ILogger CreateLogger(Type type);
        ILogger CreateLogger(string className);
    }

    public class LoggerFactory : ILoggerFactory
    {
        private readonly LogLevel _minLogLevel;
        private readonly bool _writeToConsole;
        private readonly FileLogger _rootLogger;
        private bool _disposed;

        public LoggerFactory(LogLevel minLogLevel = LogLevel.Info, bool writeToConsole = true)
        {
            _minLogLevel = minLogLevel;
            _writeToConsole = writeToConsole;
            _rootLogger = new FileLogger(_minLogLevel, _writeToConsole);
        }

        public ILogger CreateLogger<T>()
        {
            return CreateLogger(typeof(T));
        }

        public ILogger CreateLogger(Type type)
        {
            return CreateLogger(type.Name);
        }

        public ILogger CreateLogger(string className)
        {
            return new ClassLogger(className, _rootLogger);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _rootLogger?.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    /// 클래스명을 포함하는 Logger 래퍼
    /// </summary>
    internal class ClassLogger : ILogger
    {
        private readonly string _className;
        private readonly ILogger _innerLogger;

        public ClassLogger(string className, ILogger innerLogger)
        {
            _className = className ?? "Unknown";
            _innerLogger = innerLogger ?? throw new ArgumentNullException(nameof(innerLogger));
        }

        public string ClassName => _className;

        public void Debug(string message)
        {
            _innerLogger.Debug(FormatMessage(message));
        }

        public void Info(string message)
        {
            _innerLogger.Info(FormatMessage(message));
        }

        public void Warning(string message)
        {
            _innerLogger.Warning(FormatMessage(message));
        }

        public void Error(string message)
        {
            _innerLogger.Error(FormatMessage(message));
        }

        public void Error(string message, Exception exception)
        {
            _innerLogger.Error(FormatMessage(message), exception);
        }

        public void Fatal(string message)
        {
            _innerLogger.Fatal(FormatMessage(message));
        }

        public void Fatal(string message, Exception exception)
        {
            _innerLogger.Fatal(FormatMessage(message), exception);
        }

        private string FormatMessage(string message)
        {
            return $"[{_className}] {message}";
        }

        public void Dispose()
        {
            // ClassLogger는 내부 로거를 소유하지 않으므로 Dispose하지 않음
        }
    }
}