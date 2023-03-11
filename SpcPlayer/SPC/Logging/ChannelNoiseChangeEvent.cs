using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpcPlayer.SPC.Logging
{
    internal class ChannelNoiseChangeEvent : ChannelEvent
    {
        private bool _noiseEnabled;

        public ChannelNoiseChangeEvent(int cycleTime, int channel, bool noiseEnabled) : base(EventType.ChannelNoiseChange, cycleTime, channel)
        {
            _noiseEnabled = noiseEnabled;
        }

        public bool NoiseEnabled => _noiseEnabled;
    }
}
