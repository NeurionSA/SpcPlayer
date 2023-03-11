using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpcPlayer.SPC.Logging
{
    internal enum EventType
    {
        KeyOn,
        KeyOff,
        PitchChange,
        VolumeChange,
        SourceEnd,
        VoiceFaded,
        AttackDecayChange,
        SustainChange,
        GainChange,
        ChannelPitchModChange,
        ChannelEchoChange,
        ChannelNoiseChange,
    }
}
