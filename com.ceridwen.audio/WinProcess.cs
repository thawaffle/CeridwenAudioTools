using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using BarRaider.SdTools;

namespace com.ceridwen.audio
{
    public class WinProcessAPI
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr hwnd, WindowEnumProc callback, IntPtr lParam);

        private delegate bool WindowEnumProc(IntPtr hwnd, IntPtr lparam);

        private Process _realProcess;

        public WinProcessAPI() { }

        public static WinProcessAPI GetAPI()
        {
            return new WinProcessAPI();
        }

        public Process GetForegroundWindowProcess()
        {
            try
            {
                GetWindowThreadProcessId(GetForegroundWindow(), out uint processID); // Get PID from window handle
                var foregroundProcess = Process.GetProcessById(Convert.ToInt32(processID)); // Get it as a C# obj
                if (foregroundProcess.ProcessName == "ApplicationFrameHost")
                {
                    return GetRealProcess(foregroundProcess);
                } else
                {
                    return foregroundProcess;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"GetForegroundWindowProcess Exception: {ex}");
            }
            return null;
        }


        private Process GetRealProcess(Process foregroundProcess)
        {
            EnumChildWindows(foregroundProcess.MainWindowHandle, ChildWindowCallback, IntPtr.Zero);
            return _realProcess;
        }

        private bool ChildWindowCallback(IntPtr hwnd, IntPtr lparam)
        {   GetWindowThreadProcessId(hwnd, out uint processID);
            var process = Process.GetProcessById(Convert.ToInt32(processID));
            if (process.ProcessName != "ApplicationFrameHost")
            {
                _realProcess = process;
            }
            return true;
        }



    }
}
