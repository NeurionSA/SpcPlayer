using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpcPlayer.SPC.Logging
{
    internal class ChannelPitchModChangeEvent : ChannelEvent
    {
        private bool _pitchModEnabled;

        public ChannelPitchModChangeEvent(int cycleTime, int channel, bool pitchModEnabled) : base(EventType.ChannelPitchModChange, cycleTime, channel)
        {
            _pitchModEnabled = pitchModEnabled;
        }

        public bool PitchModEnabled => _pitchModEnabled;
    }
}
