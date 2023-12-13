using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BarRaider.SdTools;
using BarRaider.SdTools.Payloads;
using EarTrumpet.DataModel.WindowsAudio;
using EarTrumpet.Interop.MMDeviceAPI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace com.ceridwen.audio
{

    [PluginActionId("com.ceridwen.audio.action")]
    public class AudioAction : EncoderBase
    {
        private class PluginSettings
        {
            [JsonProperty(PropertyName = "commapps")]
            public string CommApps { get; set; }

            [JsonProperty(PropertyName = "direction")]
            public int Direction { get { return ((_parent.Selected.SelectedDeviceKind == AudioDeviceKind.Playback) ? 0 : 1); } set { _parent.Selected.SelectedDeviceKind = (value == 0) ? AudioDeviceKind.Playback : AudioDeviceKind.Recording; } }

            [JsonProperty(PropertyName = "editcolour")]
            public string EditColour { get; set; }

            [JsonProperty(PropertyName = "okcolour")]
            public string OkColour { get; set; }

            [JsonProperty(PropertyName = "errorcolour")] 
            public string ErrorColour { get; set; }

            [JsonProperty(PropertyName = "lock")]
            public bool Lock { get { return _lock; } set { _lock = value; _ = _parent.UpdateTriggerDescriptionsAsync(); } }

            [JsonProperty(PropertyName = "editdef")] 
            public bool EditDefault { get; set; }

            public List<string> CommunicationApps { get { if (CommApps == null) return new List<string>(); else return CommApps.ToLower().Split('\n').ToList(); } }

            private AudioAction _parent { get; }
            private bool _lock;

            public PluginSettings(AudioAction parent)
            {
                this._parent = parent;
            }
            public static PluginSettings CreateDefaultSettings(AudioAction parent)
            {
                PluginSettings instance = new PluginSettings(parent)
                {
                    CommApps = "",
                    EditColour = "#1c39bb",
                    OkColour = "#228b22",
                    ErrorColour = "#ba160c",
                    Lock = true,
                    EditDefault = false
                };
                return instance;
            }
        }

        #region Private Members

        private PluginSettings Settings { get; }
        private Stopwatch StopWatch { get; } = new Stopwatch();
        private SelectedDevice Selected { get; } = new SelectedDevice();
        private Process lastProc = Process.GetCurrentProcess();
        private ERole lastRole = ERole.eMultimedia;
        private int timeout = 0;
        #endregion

        public AudioAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this.Settings = PluginSettings.CreateDefaultSettings(this);
            }
            else
            {
                this.Settings = new PluginSettings(this);
                payload.Settings.Populate(this.Settings);

            }

            Selected.SelectedDeviceKind = (this.Settings.Direction == 0) ? AudioDeviceKind.Playback : AudioDeviceKind.Recording;

            Selected.SelectAudioDevice();
            Connection.SetFeedbackLayoutAsync("layouts/device.json");
            _ = SaveSettings();
            _ = UpdateTriggerDescriptionsAsync();
            _ = UpdateDisplay(true);
        }

        public override void Dispose()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Destructor called");
        }
 
        public async override void DialRotate(DialRotatePayload payload)
        {
            if (Selected.IsConfigurable || Settings.EditDefault)
            {
                if (Selected.SelectAudioDevice(payload.Ticks))
                    timeout = 60;
                else
                    timeout = -5;
            } else
            {
                timeout = -3;
            }
            await UpdateDisplay();
        }

        public override void DialDown(DialPayload payload)
        {
            StopWatch.Restart();
        }

        public override async void DialUp(DialPayload payload)
        {
            if (StopWatch.ElapsedMilliseconds > 1000 && Settings.EditDefault)
            {
                Selected.SetSystemAudioDevice();
                timeout = -2;
                await UpdateDisplay();
            }
            else
            {
                Process proc = WinProcessAPI.GetAPI().GetForegroundWindowProcess();
                ERole eRole = GetTargetRole(proc); 
                if (Selected.IsConfigurable) 
                {
                    if (SetDefaultAudioDevice(eRole))
                        timeout = -2;
                    else
                        timeout = -3;
                    await UpdateDisplay();
                }
                else
                {
                    SelectAudioDevice(proc, eRole);
                    timeout = -3;
                    await UpdateDisplay();
                }
            }
            StopWatch.Stop();
        }

        public override async void TouchPress(TouchpadPressPayload payload)
        {
            if (payload.IsLongPress && !this.Settings.Lock)
            {
                if (Selected.SelectedDeviceKind == AudioDeviceKind.Playback)
                {
                    Selected.SelectedDeviceKind = AudioDeviceKind.Recording;
                } else
                {
                    Selected.SelectedDeviceKind = AudioDeviceKind.Playback;
                }
                await SaveSettings();
                await UpdateDisplay();
            }
            if (timeout != 0)
                timeout = 0;
            Process proc = WinProcessAPI.GetAPI().GetForegroundWindowProcess();
            ERole eRole = GetTargetRole(proc);
            SelectAudioDevice(proc, eRole);
        }

        public override async void OnTick() {
            Process proc = WinProcessAPI.GetAPI().GetForegroundWindowProcess();
            if (proc.Id != lastProc.Id)
            {
                ERole eRole = GetTargetRole(proc);
                SelectAudioDevice(proc, eRole);
                timeout = 0;
            }
            else
            {
                if (timeout > 0)
                {
                    RefreshSelectedAudioDevice(true);
                    timeout--;
                }
                else if (timeout < 0)
                {
                    timeout = (timeout < -4) ? -4 : (timeout < -3) ? 60 : (timeout < -1) ? -1 : 0;
                }
                else
                {
                    RefreshSelectedAudioDevice();
                }
                await UpdateDisplay();
            }
        }

        public override async void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(Settings, payload.Settings);
            await UpdateDisplay();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        #region Private Methods

        private async Task UpdateDisplay(bool wait = false)
        {
            char[] delim = { '(' };
            String devDisp = Selected.SelectedDeviceName;
            String devName = devDisp.Split(delim).First();
            String devDesc = devDisp.Substring(devName.Length);
            String fb = "{";
            fb += "\"direction\":\"" + ((Selected.SelectedDeviceKind == AudioDeviceKind.Playback) ? "Audio Out" : "Audio In") + "\",";
            fb += "\"indicator\": {\"value\": \"icons/" + (wait ? "hourglass" : (Selected.SelectedDeviceKind == AudioDeviceKind.Playback) ? "out" : "in") + "\",";
            fb += "\"opacity\": " + (Selected.IsConfigurable || wait ? "1.0" : "0.5") + "},";
            fb += "\"device\":\"" + (wait ? "" : devName) + "\",";
            fb += "\"description\":\"" + (wait ? "" : devDesc) + "\"";
            if (timeout != -1)
                fb += ",\"canvas\": {\"background\":\"" + ((timeout > 0) ? this.Settings.EditColour : ((timeout == -2) ? this.Settings.OkColour : ((timeout <= -3) ? this.Settings.ErrorColour : ""))) + "\"}";
            fb += "}";
            await Connection.SetFeedbackAsync(JObject.Parse(fb));
        }

        private ERole GetTargetRole (Process proc)
        {
            if (Settings.CommunicationApps.Contains(proc.ProcessName.ToLower()))
            {
                return ERole.eCommunications;
            }
            else
            {
                return ERole.eMultimedia;
            }
        }

        private bool SetDefaultAudioDevice(ERole eRole = ERole.eMultimedia)
        {
            if (eRole == ERole.eCommunications)
            {
                return Selected.SetDefaultAudioDevice(eRole);
            }
            else
            {
                return Selected.SetDefaultAudioDevice();
            }
        }

        private async void SelectAudioDevice(Process proc, ERole eRole = ERole.eMultimedia)
        {
            await UpdateDisplay(true);
            lastRole = eRole;
            lastProc = proc;
            if (eRole == ERole.eCommunications)
            {
                Selected.SelectAudioDevice(eRole);
            }
            else
            {
                Selected.SelectAudioDevice(proc);
            }
        }

        private void RefreshSelectedAudioDevice(bool noUpdate = false)
        {
            if (noUpdate) 
            {
                Selected.RefreshAudioDevice(lastProc); 
            } else if (lastRole == ERole.eCommunications)
            {
                Selected.SelectAudioDevice(lastRole);
            }
            else
            {
                Selected.SelectAudioDevice(lastProc);
            }
        }

        private async Task UpdateTriggerDescriptionsAsync()
        {
            await SetTriggerDescriptionAsync("Choose Audio Device", "Select Audio Device", "Show Current Audio Device", Settings.Lock ? "" : "Switch Audio Direction");
        }
        private async Task SetTriggerDescriptionAsync(string rotate, string push, string touch, string longTouch)
        {
            await Connection.StreamDeckConnection.SendAsync(new SetTriggerDescriptionMessage(rotate, push, touch, longTouch, Connection.ContextId));
        }

        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(Settings));
        }

        #endregion
    }
}