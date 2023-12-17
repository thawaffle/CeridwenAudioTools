using System;
using System.Data;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using EarTrumpet.DataModel.Audio;
using EarTrumpet.DataModel.WindowsAudio;
using EarTrumpet.DataModel.WindowsAudio.Internal;
using EarTrumpet.Interop.MMDeviceAPI;

namespace com.ceridwen.audio
{
    internal class SelectedDevice
    {
        #region Public Members

        public IAudioDevice Selected { get { if (_selected == null) return GetDeviceManager()?.GetDefaultDevice(ERole.eMultimedia); else return _selected; } }
        public bool IsConfigurable { get { return (_process != null); }}
        public AudioDeviceKind SelectedDeviceKind { get { return _kind;  } set { _kind = value;
                _deviceNameSorted = new EncapsulatedSortedList<string, IAudioDevice>(GetDeviceManager()?.Devices, ad => ad?.DisplayName, new AudioDeviceNameComparer());
                _deviceIdMap = new EncapsulatedConcurrentDictionary<string, IAudioDevice>(GetDeviceManager()?.Devices, o => o?.Id);
                _sessionPidMap = new EncapsulatedConcurrentDictionary<int, IAudioDeviceSession>(GetDeviceManager()?.GetDefaultDevice()?.Groups, o => o.ProcessId); 
                _sessionNameMap = new EncapsulatedConcurrentDictionary<string, IAudioDeviceSession>(GetDeviceManager()?.GetDefaultDevice()?.Groups, o => o?.ExeName);
            } }
        public string SelectedDeviceName { get { return Selected.DisplayName; } }

        #endregion

        #region Private Members
        private AudioDeviceKind _kind;
        private IAudioDevice _selected = null;
        private Process _process = null;
        private EncapsulatedSortedList<string, IAudioDevice> _deviceNameSorted;
        private EncapsulatedConcurrentDictionary<string, IAudioDevice> _deviceIdMap;
        private EncapsulatedConcurrentDictionary<int, IAudioDeviceSession> _sessionPidMap;
        private EncapsulatedConcurrentDictionary<string, IAudioDeviceSession> _sessionNameMap;
        #endregion

        #region Constructors/Detructors

        public SelectedDevice(AudioDeviceKind kind = AudioDeviceKind.Playback)
        {
            SelectedDeviceKind = kind;
        }

        #endregion

        #region Public Methods

        public void SelectAudioDevice(ERole eRole = ERole.eMultimedia)
        {
            SelectAudioDevice(GetDeviceManager().GetDefaultDevice(eRole));
            _process = null;
        }

        public void SelectAudioDevice(IAudioDevice device)
        {
            _selected = device;
        }

        public bool SelectAudioDevice(int offset)
        {
            int currentIndex = _deviceNameSorted.IndexOfValue(Selected);
            int index = currentIndex + offset;
            if (index >= _deviceNameSorted?.Count) index = _deviceNameSorted.Count - 1;
            if (index < 0) index = 0;
            SelectAudioDevice(_deviceNameSorted?.ElementAtOrDefault(index));
            return (index != currentIndex);
        }

        public void SelectAudioDevice(Process focused)
        {
            IAudioDevice device = ScanForDefaultAudioDevice(focused, out Process process);
            SelectAudioDevice(device, process);
        }

        public void RefreshAudioDevice(Process focused)
        {
            if (!IsConfigurable)
            {
                ScanForDefaultAudioDevice(focused, out _process);
            }
        }

        public void SetSystemAudioDevice()
        {
            SetDefaultAudioDevice(new[]{ ERole.eMultimedia, ERole.eConsole});
        }

        public bool SetDefaultAudioDevice()
        {
            if (_process != null)
            {
                IAudioDevice target = Selected;
                SetDefaultAudioDevice(_process);
                return (Selected?.Id == target?.Id);
            } else
            {
                return false;
            }
        }

        public bool SetDefaultAudioDevice(ERole eRole)
        {
            return SetDefaultAudioDevice(new[] { eRole });
        }

        public bool SetDefaultAudioDevice(ERole[] eRoles)
        {
            bool result = true;
            AudioDeviceManager devmgr = GetDeviceManager();

            for (int i = 0; i < eRoles.Length; i++)
            {
                devmgr?.SetDefaultDevice(Selected, eRoles[i]);
                result &= devmgr?.GetDefaultDevice(eRoles[i])?.Id == Selected?.Id;
            }
            return result;
        }

        public void SetDefaultAudioDevice(Process focused)
        {
            SetDefaultAudioDevice(focused.Id);
            SelectAudioDevice(focused);
        }

        public void SetDefaultAudioDevice(int pid)
        {
            AudioDeviceManager devmgr = GetDeviceManager();
            devmgr?.SetDefaultEndPoint(Selected?.Id, pid);
        }

        #endregion

        #region Private Methods
        private AudioDeviceManager GetDeviceManager()
        {
            return GetDeviceManager(SelectedDeviceKind);
        }

        private AudioDeviceManager GetDeviceManager(AudioDeviceKind kind)
        {
            return (AudioDeviceManager)WindowsAudioFactory.Create(kind);
        }

        private void SelectAudioDevice(IAudioDevice device, Process process)
        {
            _process = process;
            SelectAudioDevice(device);
        }

        private bool IsProcessConfigurable(Process focused, out IAudioDeviceSession session)
        {
            if (_sessionPidMap.TryFind(focused.Id, out var s))
            {
                session = s;
                return true;
            } 
            else
            {
                // This is a workaround for discord
                return _sessionNameMap.TryFind(focused?.ProcessName, out session);
            }
        }

        private IAudioDevice GetDefaultAudioDevice(Process focused, out Process remap)
        {
            remap = focused;
            
            if (focused == null) 
            {
                return null; 
            }

            AudioDeviceManager devmgr = GetDeviceManager();
            string devId = devmgr?.GetDefaultEndPoint(focused.Id);


            if (devId != "")
                return GetDeviceFromId(devId);
            else if (IsProcessConfigurable(focused, out IAudioDeviceSession session))
            {
                if (session != null) remap = Process.GetProcessById(session.ProcessId);
                return (AudioDevice)devmgr?.GetDefaultDevice();
            }
            else
                return null;
        }

        private IAudioDevice GetDeviceFromId(string devId)
        {
            if (_deviceIdMap.TryFind(devId, out IAudioDevice device))
            {
                return device;
            } else
            {
                return null;
            }
        }

        private IList<Process> GetChildProcesses(Process process)
        {
            if (process != null)
            {
                return new ManagementObjectSearcher(
                 $"Select * From Win32_Process Where ParentProcessID={process.Id}")
             .Get()
             .Cast<ManagementObject>()
             .Select(mo =>
             {
                 try
                 { return Process.GetProcessById(Convert.ToInt32(mo["ProcessID"])); }
                 catch (Exception) { return process; }
             })
             .ToList();
            }
            else
            {
                return new List<Process>();
            }
        }


        private IAudioDevice ScanForDefaultAudioDevice(Process focused, out Process process)
        {
            var candidate = ScanForDefaultAudioDeviceInternal(focused, out process);
            // This is a workaround for Steam
            return candidate ?? ScanForDefaultAudioDeviceInternal(WinProcessAPI.GetParentProcess(focused), out process);
        }

        private IAudioDevice ScanForDefaultAudioDeviceInternal(Process focused, out Process process)
        {
            IAudioDevice device = GetDefaultAudioDevice(focused, out process);
            if (device != null)
            {
                return device;
            }
            else
            {
                foreach (Process child in GetChildProcesses(focused))
                {
                    device = GetDefaultAudioDevice(child, out process);
                    if (device != null)
                    {
                        return device;
                    }
                }
                process = null;
                return null;
            }
        }

        #endregion
    }
}
