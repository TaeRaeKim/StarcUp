using System;

namespace StarcUp.Common.Logging
{
    public class ConsoleLogger : ILogger
    {
        private readonly LogLevel _minLogLevel;
        private bool _disposed;
        
        public string ClassName => "ConsoleLogger";

        public ConsoleLogger(LogLevel minLogLevel = LogLevel.Info)
        {
            _minLogLevel = minLogLevel;
        }

        public void Debug(string message)
        {
            Log(LogLevel.Debug, message, ConsoleColor.Gray);
        }

        public void Info(string message)
        {
            Log(LogLevel.Info, message, ConsoleColor.White);
        }

        public void Warning(string message)
        {
            Log(LogLevel.Warning, message, ConsoleColor.Yellow);
        }

        public void Error(string message)
        {
            Log(LogLevel.Error, message, ConsoleColor.Red);
        }

        public void Error(string message, Exception exception)
        {
            Log(LogLevel.Error, $"{message} - Exception: {exception}", ConsoleColor.Red);
        }

        public void Fatal(string message)
        {
            Log(LogLevel.Fatal, message, ConsoleColor.DarkRed);
        }

        public void Fatal(string message, Exception exception)
        {
            Log(LogLevel.Fatal, $"{message} - Exception: {exception}", ConsoleColor.DarkRed);
        }

        private void Log(LogLevel level, string message, ConsoleColor color)
        {
            if (_disposed || level < _minLogLevel) return;

            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [{level}] {message}");
            Console.ForegroundColor = originalColor;
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}