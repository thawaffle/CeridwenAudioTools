using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using EarTrumpet.DataModel.Audio;
using EarTrumpet.DataModel.WindowsAudio;
using EarTrumpet.DataModel.WindowsAudio.Internal;
using EarTrumpet.Interop.MMDeviceAPI;

namespace com.ceridwen.audio
{
    internal class SelectedDevice
    {
        #region Public Members

        public bool IsConfigurable { get { return (_process != null); }}
        public AudioDeviceKind SelectedDeviceKind { get; set; } 
        public string SelectedDeviceName { get 
            {
                try
                {
                    return Selected.DisplayName;
                } catch (Exception)
                {
                    return "";
                }
            } 
        }

        #endregion

        #region Private Members

        private IAudioDevice Selected { get { if (_selected == null) return GetDeviceManager()?.GetDefaultDevice(ERole.eMultimedia); else return _selected; } }
        private IAudioDevice _selected = null;
        private Process _process = null;
        private ConcurrentDictionary<AudioDeviceKind, EncapsulatedSortedList<string, string, IAudioDevice>> _deviceNameSorted { get; } = new ConcurrentDictionary<AudioDeviceKind, EncapsulatedSortedList<string, string, IAudioDevice>>();
        private ConcurrentDictionary<AudioDeviceKind, EncapsulatedConcurrentDictionary<string, string, IAudioDevice>> _deviceIdMap { get; } = new ConcurrentDictionary<AudioDeviceKind, EncapsulatedConcurrentDictionary<string, string, IAudioDevice>>();
        private ConcurrentDictionary<AudioDeviceKind, EncapsulatedConcurrentDictionary<int, string, IAudioDeviceSession>> _sessionPidMap { get; } = new ConcurrentDictionary<AudioDeviceKind, EncapsulatedConcurrentDictionary<int, string, IAudioDeviceSession>>();
        private ConcurrentDictionary<AudioDeviceKind, EncapsulatedConcurrentDictionary<string, string, IAudioDeviceSession>> _sessionNameMap { get; } = new ConcurrentDictionary<AudioDeviceKind, EncapsulatedConcurrentDictionary<string, string, IAudioDeviceSession>>();
        private int DeviceNameIndexCount { get { return (int)_deviceNameSorted?[SelectedDeviceKind]?.Count; } }

        #endregion

        #region Constructors/Detructors

        public SelectedDevice(AudioDeviceKind kind = AudioDeviceKind.Playback)
        {
            SelectedDeviceKind = kind;

            foreach (AudioDeviceKind k in Enum.GetValues(typeof(AudioDeviceKind)))
            {
                _deviceNameSorted.TryAdd(k, new EncapsulatedSortedList<string, string, IAudioDevice>(GetDeviceManager(k)?.Devices, ad => ad?.DisplayName, ad => ad?.Id, new AudioDeviceNameComparer()));
                _deviceIdMap.TryAdd(k, new EncapsulatedConcurrentDictionary<string, string, IAudioDevice>(GetDeviceManager(k)?.Devices, o => o?.Id, o=>o?.Id));
                _sessionPidMap.TryAdd(k, new EncapsulatedConcurrentDictionary<int, string, IAudioDeviceSession>(GetDeviceManager(k)?.GetDefaultDevice()?.Groups, o => (int)o?.ProcessId, o => o?.Id));
                _sessionNameMap.TryAdd(k, new EncapsulatedConcurrentDictionary<string, string, IAudioDeviceSession>(GetDeviceManager(k)?.GetDefaultDevice()?.Groups, o => o?.ExeName, o => o?.Id));
            }
        }

        #endregion

        #region Public Methods

        public void SelectAudioDevice(ERole eRole = ERole.eMultimedia)
        {
            _process = null;
            try
            {
                SelectAudioDevice(GetDeviceManager().GetDefaultDevice(eRole));
            } 
            catch (Exception) 
            { 
            }
        }
       
        public bool SelectAudioDevice(int offset)
        {
            try
            {
                int currentIndex = GetDeviceNameIndex(Selected);
                int index = currentIndex + offset;
                if (index >= DeviceNameIndexCount) index = DeviceNameIndexCount - 1;
                if (index < 0) index = 0;
                SelectAudioDevice(GetDeviceAtNameIndex(index));
                return (index != currentIndex);
            } 
            catch (Exception)
            {
                return false;
            }
        }

        public void SelectAudioDevice(Process focused)
        {
            try
            {
                IAudioDevice device = ScanForDefaultAudioDevice(focused, out Process process);
                SelectAudioDevice(device, process);
            } 
            catch (Exception)
            {
            }
        }

        public void RefreshAudioDevice(Process focused)
        {
            try
            {
                if (!IsConfigurable)
                {
                    ScanForDefaultAudioDevice(focused, out _process);
                }
            }
            catch (Exception)
            {
            }
        }

        public void SetSystemAudioDevice()
        {
            try
            {
                SetDefaultAudioDevice(new[] { ERole.eMultimedia, ERole.eConsole });
            }
            catch (Exception)
            {
            }

        }

        public bool SetDefaultAudioDevice()
        {
            try
            {
                if (_process != null)
                {
                    IAudioDevice target = Selected;
                    SetDefaultAudioDevice(_process);
                    return (Selected?.Id == target?.Id);
                }
            }
            catch (Exception)
            {
            }
            return false;
        }

        public bool SetDefaultAudioDevice(ERole eRole)
        {
            try
            {
                return SetDefaultAudioDevice(new[] { eRole });
            } 
            catch (Exception)
            {
                return false;
            }
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
        private void SelectAudioDevice(IAudioDevice device)
        {
            _selected = device;
        }

        private void SelectAudioDevice(IAudioDevice device, Process process)
        {
            _process = process;
            SelectAudioDevice(device);
        }

        private bool FindSessionByPID(int pid, out IAudioDeviceSession session)
        {
            session = null;
            return _sessionPidMap?[SelectedDeviceKind] != null ? _sessionPidMap[SelectedDeviceKind].TryFind(pid, out session) : false;
        }

        private bool FindSessionByName(string name, out IAudioDeviceSession session)
        {
            session = null;
            return _sessionNameMap?[SelectedDeviceKind] != null ? _sessionNameMap[SelectedDeviceKind].TryFind(name, out session) : false;
        }
        private bool IsProcessConfigurable(Process focused, out IAudioDeviceSession session)
        {
            return FindSessionByPID(focused.Id, out session) ? true : /* This is a workaround for discord: */ FindSessionByName(focused?.ProcessName, out session);
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

        private int GetDeviceNameIndex(IAudioDevice device)
        {
            return _deviceNameSorted?[SelectedDeviceKind] != null ? _deviceNameSorted[SelectedDeviceKind].IndexOfValue(device) : 0;
        }

        private IAudioDevice GetDeviceAtNameIndex(int index)
        {
            return _deviceNameSorted?[SelectedDeviceKind]?.ElementAtOrDefault(index);
        }

        IAudioDevice GetAudioDeviceById(string id)
        {
            IAudioDevice device = null;
            _deviceIdMap?[SelectedDeviceKind]?.TryFind(id, out device);
            return device;

        } 
        private IAudioDevice GetDeviceFromId(string devId)
        {
            return GetAudioDeviceById(devId);
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
                foreach (Process child in WinProcessAPI.GetChildProcesses(focused))
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

        private bool SetDefaultAudioDevice(ERole[] eRoles)
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

        private void SetDefaultAudioDevice(Process focused)
        {
            SetDefaultAudioDevice(focused.Id);
            SelectAudioDevice(focused);
        }

        private void SetDefaultAudioDevice(int pid)
        {
            AudioDeviceManager devmgr = GetDeviceManager();
            devmgr?.SetDefaultEndPoint(Selected?.Id, pid);
        }


        #endregion
    }
}
