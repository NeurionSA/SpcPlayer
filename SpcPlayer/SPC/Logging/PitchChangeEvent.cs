using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpcPlayer.SPC.Logging
{
    internal class PitchChangeEvent : ChannelEvent
    {
        private int _frequency;

        public PitchChangeEvent(int cycleTime, int channel, int frequency) : base(EventType.PitchChange, cycleTime, channel)
        {
            _frequency = frequency;
        }

        public int Frequency => _frequency;
    }
}
