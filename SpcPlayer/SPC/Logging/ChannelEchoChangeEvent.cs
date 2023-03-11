using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpcPlayer.SPC.Logging
{
    internal class ChannelEchoChangeEvent : ChannelEvent
    {
        private bool _echoEnabled;

        public ChannelEchoChangeEvent(int cycleTime, int channel, bool echoEnabled) : base(EventType.ChannelEchoChange, cycleTime, channel)
        {
            _echoEnabled = echoEnabled;
        }

        public bool EchoEnabled => _echoEnabled;
    }
}
