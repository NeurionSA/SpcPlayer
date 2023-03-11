using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpcPlayer.SPC
{
    internal enum VoiceStatus
    {
        Attack,
        Decay,
        Sustain,
        Release,
        Gain,
    }

    internal enum GainMode
    {
        Direct = 0,
        LinearDecrease = 4,
        ExponentialDecrease = 5,
        LinearIncrease = 6,
        BentIncrease = 7,
    }
}
