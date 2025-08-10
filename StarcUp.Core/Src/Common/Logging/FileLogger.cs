using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace StarcUp.Common.Logging
{
    public class FileLogger : ILogger
    {
        private readonly string _logDirectory;
        private readonly LogLevel _minLogLevel;
        private readonly bool _writeToConsole;
        private readonly BlockingCollection<LogEntry> _logQueue;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _logWriterTask;
        private StreamWriter? _currentWriter;
        private DateTime _currentFileDate;
        private readonly object _writerLock = new object();
        private bool _disposed;
        
        public string ClassName => "FileLogger";

        public FileLogger(LogLevel minLogLevel = LogLevel.Info, bool writeToConsole = true)
        {
            _minLogLevel = minLogLevel;
            _writeToConsole = writeToConsole;
            
            // %APPDATA%\StarcUp\Logs 경로 설정
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _logDirectory = Path.Combine(appDataPath, "StarcUp", "Logs");
            
            // 디렉토리가 없으면 생성
            Directory.CreateDirectory(_logDirectory);

            _logQueue = new BlockingCollection<LogEntry>(1000);
            _cancellationTokenSource = new CancellationTokenSource();
            _logWriterTask = Task.Run(() => ProcessLogQueue(_cancellationTokenSource.Token));
        }

        public void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        public void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        public void Warning(string message)
        {
            Log(LogLevel.Warning, message);
        }

        public void Error(string message)
        {
            Log(LogLevel.Error, message);
        }

        public void Error(string message, Exception exception)
        {
            Log(LogLevel.Error, $"{message} - Exception: {exception}");
        }

        public void Fatal(string message)
        {
            Log(LogLevel.Fatal, message);
        }

        public void Fatal(string message, Exception exception)
        {
            Log(LogLevel.Fatal, $"{message} - Exception: {exception}");
        }

        private void Log(LogLevel level, string message)
        {
            if (level < _minLogLevel) return;

            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message,
                ThreadId = Thread.CurrentThread.ManagedThreadId
            };

            if (!_logQueue.TryAdd(entry, 10))
            {
                // 큐가 가득 차면 콘솔에만 출력
                if (_writeToConsole)
                {
                    Console.WriteLine(FormatLogEntry(entry));
                }
            }
        }

        private void ProcessLogQueue(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (_logQueue.TryTake(out var entry, 100, cancellationToken))
                    {
                        WriteLogEntry(entry);
                    }
                }

                // 남은 로그 처리
                while (_logQueue.TryTake(out var entry))
                {
                    WriteLogEntry(entry);
                }
            }
            catch (OperationCanceledException)
            {
                // 정상 종료
            }
            finally
            {
                lock (_writerLock)
                {
                    _currentWriter?.Dispose();
                    _currentWriter = null;
                }
            }
        }

        private void WriteLogEntry(LogEntry entry)
        {
            try
            {
                var formatted = FormatLogEntry(entry);

                // 콘솔 출력
                if (_writeToConsole)
                {
                    var originalColor = Console.ForegroundColor;
                    Console.ForegroundColor = GetConsoleColor(entry.Level);
                    Console.WriteLine(formatted);
                    Console.ForegroundColor = originalColor;
                }

                // 파일 출력
                lock (_writerLock)
                {
                    EnsureLogFileWriter(entry.Timestamp);
                    _currentWriter?.WriteLine(formatted);
                    _currentWriter?.Flush();
                }
            }
            catch (Exception ex)
            {
                // 로깅 실패 시 콘솔에만 출력
                if (_writeToConsole)
                {
                    Console.WriteLine($"[LOGGER ERROR] Failed to write log: {ex.Message}");
                }
            }
        }

        private void EnsureLogFileWriter(DateTime timestamp)
        {
            var fileDate = timestamp.Date;
            
            if (_currentWriter == null || _currentFileDate != fileDate)
            {
                _currentWriter?.Dispose();
                
                var fileName = $"StarcUp_{fileDate:yyyy-MM-dd}.log";
                var filePath = Path.Combine(_logDirectory, fileName);
                
                _currentWriter = new StreamWriter(filePath, append: true, Encoding.UTF8)
                {
                    AutoFlush = false
                };
                _currentFileDate = fileDate;
            }
        }

        private string FormatLogEntry(LogEntry entry)
        {
            return $"[{entry.Timestamp:HH:mm:ss.fff}] [{entry.Level}] [T{entry.ThreadId:D3}] {entry.Message}";
        }

        private ConsoleColor GetConsoleColor(LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Fatal => ConsoleColor.DarkRed,
                _ => ConsoleColor.White
            };
        }

        public void Dispose()
        {
            if (_disposed) return;

            _cancellationTokenSource.Cancel();
            
            // 로그 작성 태스크 종료 대기 (최대 2초)
            _logWriterTask.Wait(2000);
            
            _logQueue?.Dispose();
            _cancellationTokenSource?.Dispose();
            
            lock (_writerLock)
            {
                _currentWriter?.Dispose();
                _currentWriter = null;
            }

            _disposed = true;
        }

        private class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public LogLevel Level { get; set; }
            public string Message { get; set; } = string.Empty;
            public int ThreadId { get; set; }
        }
    }
}