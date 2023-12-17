using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace com.ceridwen.audio
{ 
    public class WinProcessAPI
    {

        #region Private Members
        private struct ProcessBasicInformation
        {
            // These members must match PROCESS_BASIC_INFORMATION
            internal IntPtr Reserved1;
            internal IntPtr PebBaseAddress;
            internal IntPtr Reserved2_0;
            internal IntPtr Reserved2_1;
            internal IntPtr UniqueProcessId;
            internal IntPtr InheritedFromUniqueProcessId;
        }

        private delegate bool WindowEnumProc(IntPtr hwnd, IntPtr lparam);

        #endregion

        #region Public Methods

        public static Process GetForegroundWindowProcess()
        {
            try
            {
                var hWndForegroundWindow = GetForegroundWindow();
                GetWindowThreadProcessId(hWndForegroundWindow, out uint foregroundID); // Get PID from window handle
                var foregroundProcess = Process.GetProcessById(Convert.ToInt32(foregroundID));

                // This is a workaround for UWP apps.
                var candidateProcess = GetOrphanedWindowProcesses(hWndForegroundWindow, Convert.ToUInt32(foregroundProcess?.Id))?.FirstOrDefault();
                return candidateProcess ?? foregroundProcess;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"GetForegroundWindowProcess Exception: {ex}");
            }
            return Process.GetCurrentProcess();                
        }

        public static Process GetParentProcess(Process proc)
        {
            try
            {
                if (proc != null)
                {
                    var parentProcess = Process.GetProcessById(Convert.ToInt32(GetParentProcess(proc.Handle)));
                    if (parentProcess.ProcessName != "explorer" && parentProcess.ProcessName != "svchost")
                    {
                        return parentProcess;
                    }
                }
            }
            catch (Exception)
            {
            }
            return null;
        }

        #endregion

        #region Private Methods

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr hwnd, WindowEnumProc callback, IntPtr lParam);

        private static List<Process> GetOrphanedWindowProcesses(IntPtr windowHandle, uint exclude)
        {
            var ids = new List<Process>();

            if (windowHandle != IntPtr.Zero)
            {

                EnumChildWindows(windowHandle,
                        (hWnd, lParam) =>
                        {
                            GetWindowThreadProcessId(hWnd, out var pid);
                            if (pid != exclude && GetParentProcess(pid) != exclude)
                            {
                                try
                                {
                                    ids.Add(Process.GetProcessById(Convert.ToInt32(pid)));
                                } 
                                catch (Exception) 
                                { 
                                }
                            }
                            return true;

                        }, IntPtr.Zero);
            }

            return ids;
        }

        [DllImport("ntdll.dll")]
        private static extern uint NtQueryInformationProcess(IntPtr processHandle, uint processInformationClass, ref ProcessBasicInformation processInformation, uint processInformationLength, out uint returnLength);

        private static uint GetParentProcess(uint pid)
        {
            var proc = Process.GetProcessById(Convert.ToInt32(pid));

            if (proc != null)
            {
                return GetParentProcess(proc.Handle);
            } 
            else
            {
                return 0;
            }
        }

        private static uint GetParentProcess(IntPtr pHandle)
        {
            var data = new ProcessBasicInformation();

            uint status = NtQueryInformationProcess(pHandle, 0, ref data, Convert.ToUInt32(Marshal.SizeOf(data)), out var returnLength);
            if (status != 0)
            {
                return 0;
            }

            return Convert.ToUInt32(data.InheritedFromUniqueProcessId.ToInt32());
        }

        #endregion

    }
}
