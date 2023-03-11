using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpcPlayer.SPC.Logging
{
    internal class SustainChangeEvent : ChannelEvent
    {
        private int _sustainLevel;
        private int _sustainRate;

        public SustainChangeEvent(int cycleTime, int channel, int sustainLevel, int sustainRate) : base(EventType.SustainChange, cycleTime, channel)
        {
            _sustainLevel = sustainLevel;
            _sustainRate = sustainRate;
        }

        public int SustainLevel => _sustainLevel;
        public int SustainRate => _sustainRate;
    }
}
