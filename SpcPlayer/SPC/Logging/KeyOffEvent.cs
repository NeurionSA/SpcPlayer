using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpcPlayer.SPC.Logging
{
    /// <summary>
    /// Represents a Key-Off event.
    /// </summary>
    internal class KeyOffEvent : ChannelEvent
    {
        public KeyOffEvent(int cycleTime, int channel) : base(EventType.KeyOff, cycleTime, channel)
        {
        }
    }
}
