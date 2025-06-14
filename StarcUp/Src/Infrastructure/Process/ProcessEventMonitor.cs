using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Threading.Tasks;

namespace StarcUp.Infrastructure.Windows
{
    /// <summary>
    /// WMI 이벤트를 사용한 프로세스 생성/종료 감지
    /// </summary>
    public class ProcessEventMonitor : IDisposable
    {
        #region Private Fields

        private ManagementEventWatcher _processStartWatcher;
        private ManagementEventWatcher _processStopWatcher;
        private readonly HashSet<string> _targetProcessNames;
        private bool _isMonitoring;
        private bool _isDisposed;

        #endregion

        #region Events

        /// <summary>
        /// 대상 프로세스가 시작되었을 때 발생
        /// </summary>
        public event EventHandler<ProcessEventArgs> ProcessStarted;

        /// <summary>
        /// 대상 프로세스가 종료되었을 때 발생
        /// </summary>
        public event EventHandler<ProcessEventArgs> ProcessStopped;

        #endregion

        #region Constructor

        public ProcessEventMonitor()
        {
            _targetProcessNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public ProcessEventMonitor(IEnumerable<string> targetProcessNames) : this()
        {
            foreach (string processName in targetProcessNames)
            {
                AddTargetProcess(processName);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 감지할 프로세스 이름 추가
        /// </summary>
        public void AddTargetProcess(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return;

            // .exe 확장자 제거
            string cleanName = processName.Replace(".exe", "");
            _targetProcessNames.Add(cleanName);

            Console.WriteLine($"[ProcessEventMonitor] 대상 프로세스 추가: {cleanName}");
        }

        /// <summary>
        /// 프로세스 이벤트 모니터링 시작
        /// </summary>
        public async Task<bool> StartMonitoringAsync()
        {
            if (_isMonitoring || _isDisposed)
                return false;

            try
            {
                Console.WriteLine("[ProcessEventMonitor] 프로세스 이벤트 모니터링 시작...");

                // 프로세스 시작 이벤트 감지
                await SetupProcessStartWatcher();

                // 프로세스 종료 이벤트 감지
                await SetupProcessStopWatcher();

                // 기존에 실행 중인 대상 프로세스 확인
                await CheckExistingProcesses();

                _isMonitoring = true;
                Console.WriteLine("[ProcessEventMonitor] 프로세스 이벤트 모니터링 활성화됨");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProcessEventMonitor] 모니터링 시작 실패: {ex.Message}");
                StopMonitoring();
                return false;
            }
        }

        /// <summary>
        /// 프로세스 이벤트 모니터링 중지
        /// </summary>
        public void StopMonitoring()
        {
            if (!_isMonitoring)
                return;

            try
            {
                Console.WriteLine("[ProcessEventMonitor] 프로세스 이벤트 모니터링 중지...");

                _processStartWatcher?.Stop();
                _processStartWatcher?.Dispose();
                _processStartWatcher = null;

                _processStopWatcher?.Stop();
                _processStopWatcher?.Dispose();
                _processStopWatcher = null;

                _isMonitoring = false;
                Console.WriteLine("[ProcessEventMonitor] 프로세스 이벤트 모니터링 중지됨");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProcessEventMonitor] 모니터링 중지 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 현재 대상 프로세스가 실행 중인지 확인
        /// </summary>
        public Process[] GetRunningTargetProcesses()
        {
            var runningProcesses = new List<Process>();

            foreach (string processName in _targetProcessNames)
            {
                try
                {
                    Process[] processes = Process.GetProcessesByName(processName);
                    runningProcesses.AddRange(processes);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ProcessEventMonitor] 프로세스 확인 실패 ({processName}): {ex.Message}");
                }
            }

            return runningProcesses.ToArray();
        }

        #endregion

        #region Private Methods

        private async Task SetupProcessStartWatcher()
        {
            await Task.Run(() =>
            {
                try
                {
                    // WQL 쿼리: 프로세스 생성 이벤트
                    string query = "SELECT * FROM Win32_ProcessStartTrace";

                    _processStartWatcher = new ManagementEventWatcher(query);
                    _processStartWatcher.EventArrived += OnProcessStarted;
                    _processStartWatcher.Start();

                    Console.WriteLine("[ProcessEventMonitor] 프로세스 시작 이벤트 감지 설정 완료");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ProcessEventMonitor] 프로세스 시작 감지 설정 실패: {ex.Message}");
                    throw;
                }
            });
        }

        private async Task SetupProcessStopWatcher()
        {
            await Task.Run(() =>
            {
                try
                {
                    // WQL 쿼리: 프로세스 종료 이벤트
                    string query = "SELECT * FROM Win32_ProcessStopTrace";

                    _processStopWatcher = new ManagementEventWatcher(query);
                    _processStopWatcher.EventArrived += OnProcessStopped;
                    _processStopWatcher.Start();

                    Console.WriteLine("[ProcessEventMonitor] 프로세스 종료 이벤트 감지 설정 완료");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ProcessEventMonitor] 프로세스 종료 감지 설정 실패: {ex.Message}");
                    throw;
                }
            });
        }

        private async Task CheckExistingProcesses()
        {
            await Task.Run(() =>
            {
                try
                {
                    Console.WriteLine("[ProcessEventMonitor] 기존 실행 중인 프로세스 확인...");

                    Process[] runningProcesses = GetRunningTargetProcesses();

                    foreach (Process process in runningProcesses)
                    {
                        try
                        {
                            var processInfo = new ProcessInfo
                            {
                                ProcessId = process.Id,
                                ProcessName = process.ProcessName,
                                MainWindowHandle = process.MainWindowHandle,
                                StartTime = process.StartTime
                            };

                            var eventArgs = new ProcessEventArgs(processInfo, ProcessEventType.Found);
                            ProcessStarted?.Invoke(this, eventArgs);

                            Console.WriteLine($"[ProcessEventMonitor] 기존 프로세스 발견: {process.ProcessName} (PID: {process.Id})");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ProcessEventMonitor] 기존 프로세스 정보 읽기 실패: {ex.Message}");
                        }
                        finally
                        {
                            process.Dispose();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ProcessEventMonitor] 기존 프로세스 확인 실패: {ex.Message}");
                }
            });
        }

        private void OnProcessStarted(object sender, EventArrivedEventArgs e)
        {
            try
            {
                ManagementBaseObject eventObj = e.NewEvent;
                string processName = eventObj["ProcessName"]?.ToString();
                uint processId = Convert.ToUInt32(eventObj["ProcessID"]);

                if (string.IsNullOrEmpty(processName))
                    return;

                // .exe 확장자 제거
                string cleanProcessName = processName.Replace(".exe", "");

                // 대상 프로세스인지 확인
                if (!_targetProcessNames.Contains(cleanProcessName))
                    return;

                Console.WriteLine($"[ProcessEventMonitor] 대상 프로세스 시작 감지: {cleanProcessName} (PID: {processId})");

                // Process 객체 가져오기 (잠시 대기 후 재시도)
                Task.Delay(500).ContinueWith(_ =>
                {
                    try
                    {
                        Process process = Process.GetProcessById((int)processId);

                        var processInfo = new ProcessInfo
                        {
                            ProcessId = (int)processId,
                            ProcessName = cleanProcessName,
                            MainWindowHandle = process.MainWindowHandle,
                            StartTime = DateTime.Now
                        };

                        var eventArgs = new ProcessEventArgs(processInfo, ProcessEventType.Started);
                        ProcessStarted?.Invoke(this, eventArgs);

                        process.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ProcessEventMonitor] 프로세스 정보 가져오기 실패: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProcessEventMonitor] 프로세스 시작 이벤트 처리 실패: {ex.Message}");
            }
        }

        private void OnProcessStopped(object sender, EventArrivedEventArgs e)
        {
            try
            {
                ManagementBaseObject eventObj = e.NewEvent;
                string processName = eventObj["ProcessName"]?.ToString();
                uint processId = Convert.ToUInt32(eventObj["ProcessID"]);

                if (string.IsNullOrEmpty(processName))
                    return;

                // .exe 확장자 제거
                string cleanProcessName = processName.Replace(".exe", "");

                // 대상 프로세스인지 확인
                if (!_targetProcessNames.Contains(cleanProcessName))
                    return;

                Console.WriteLine($"[ProcessEventMonitor] 대상 프로세스 종료 감지: {cleanProcessName} (PID: {processId})");

                var processInfo = new ProcessInfo
                {
                    ProcessId = (int)processId,
                    ProcessName = cleanProcessName,
                    MainWindowHandle = IntPtr.Zero,
                    StartTime = DateTime.MinValue
                };

                var eventArgs = new ProcessEventArgs(processInfo, ProcessEventType.Stopped);
                ProcessStopped?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ProcessEventMonitor] 프로세스 종료 이벤트 처리 실패: {ex.Message}");
            }
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            if (_isDisposed)
                return;

            StopMonitoring();
            _isDisposed = true;
        }

        #endregion
    }

    #region Event Args and Models

    /// <summary>
    /// 프로세스 이벤트 타입
    /// </summary>
    public enum ProcessEventType
    {
        Found,      // 기존에 실행 중이던 프로세스 발견
        Started,    // 프로세스 시작
        Stopped     // 프로세스 종료
    }

    /// <summary>
    /// 프로세스 정보
    /// </summary>
    public class ProcessInfo
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
        public IntPtr MainWindowHandle { get; set; }
        public DateTime StartTime { get; set; }

        public override string ToString()
        {
            return $"{ProcessName} (PID: {ProcessId}, Handle: 0x{MainWindowHandle.ToInt64():X})";
        }
    }

    /// <summary>
    /// 프로세스 이벤트 아규먼트
    /// </summary>
    public class ProcessEventArgs : EventArgs
    {
        public ProcessInfo ProcessInfo { get; }
        public ProcessEventType EventType { get; }

        public ProcessEventArgs(ProcessInfo processInfo, ProcessEventType eventType)
        {
            ProcessInfo = processInfo ?? throw new ArgumentNullException(nameof(processInfo));
            EventType = eventType;
        }

        public override string ToString()
        {
            return $"ProcessEvent: {EventType} - {ProcessInfo}";
        }
    }

    #endregion
}