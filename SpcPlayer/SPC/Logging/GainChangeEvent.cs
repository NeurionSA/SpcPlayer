using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpcPlayer.SPC.Logging
{
    internal class GainChangeEvent : ChannelEvent
    {
        private GainMode _gainMode;
        private int _gainParameter;

        public GainChangeEvent(int cycleTime, int channel, GainMode gainMode, int gainParameter) : base(EventType.GainChange, cycleTime, channel)
        {
            _gainMode = gainMode;
            _gainParameter = gainParameter;
        }

        public GainMode GainMode => _gainMode;
        public int GainParameter => _gainParameter;
    }
}
