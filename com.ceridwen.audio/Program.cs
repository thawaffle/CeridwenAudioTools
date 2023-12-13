using System.Threading.Tasks;
using System.Windows.Threading;
using BarRaider.SdTools;
using EarTrumpet.DataModel.WindowsAudio;


namespace com.ceridwen.audio
{
    internal class Program {

        #region Private Members

        private static bool playMgrLoaded = false;
        private static bool recMgrLoaded = false;

        #endregion

        #region Public Methods

        static void Main(string[] args)
        {
            // Uncomment this line of code to allow for debugging
#if DEBUG_STARTUP
            while (!System.Diagnostics.Debugger.IsAttached) { System.Threading.Thread.Sleep(100); }
#endif
            Task.Factory.StartNew(() =>
            {
                var playmgr = WindowsAudioFactory.Create(AudioDeviceKind.Playback);
                playmgr.Loaded += (_, __) => PlayMgrLoaded();
                var recmgr = WindowsAudioFactory.Create(AudioDeviceKind.Recording);
                recmgr.Loaded += (_, __) => RecMgrLoaded();
                Dispatcher.Run();
            });

            while (!playMgrLoaded || !recMgrLoaded) { System.Threading.Thread.Sleep(10); }

            SDWrapper.Run(args);
        }

        #endregion

        #region Private Methods

        private static void PlayMgrLoaded()
        {
            playMgrLoaded = true;
        }
        private static void RecMgrLoaded()
        {
            recMgrLoaded = true;
        }

        #endregion
    }

}
