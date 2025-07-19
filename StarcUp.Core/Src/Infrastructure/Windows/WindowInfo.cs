using System;

namespace StarcUp.Infrastructure.Windows
{
    public class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public string Title { get; set; }
        public string ProcessName { get; set; }
        public int ProcessId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public DateTime LastUpdated { get; set; }

        public WindowInfo()
        {
            Handle = IntPtr.Zero;
            Title = string.Empty;
            ProcessName = string.Empty;
            ProcessId = 0;
            X = 0;
            Y = 0;
            Width = 0;
            Height = 0;
            LastUpdated = DateTime.Now;
        }

        public WindowInfo(IntPtr handle, string title, string processName, int processId, int x, int y, int width, int height)
        {
            Handle = handle;
            Title = title ?? string.Empty;
            ProcessName = processName ?? string.Empty;
            ProcessId = processId;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            LastUpdated = DateTime.Now;
        }

        public bool HasSizeOrPositionChanged(WindowInfo other)
        {
            if (other == null) return true;
            
            return X != other.X || Y != other.Y || Width != other.Width || Height != other.Height;
        }

        public override string ToString()
        {
            return $"Window[{ProcessName}({ProcessId})] - {Title} at ({X},{Y}) size {Width}x{Height}";
        }

        public WindowInfo Clone()
        {
            return new WindowInfo(Handle, Title, ProcessName, ProcessId, X, Y, Width, Height);
        }
    }
}