using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EarTrumpet.Interop
{
    internal class Settings
    {
        public bool UseLogarithmicVolume { get; set; }
    }

    internal class App
    {
        static public Settings Settings { get; } = new Settings();
    }
}
