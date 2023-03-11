using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpcPlayer.SPC.Logging
{
    /// <summary>
    /// Represents the cessation of a note due to reaching the end a non-looping sample source.
    /// </summary>
    internal class SourceEndEvent : ChannelEvent
    {
        public SourceEndEvent(int cycleTime, int channel) : base(EventType.SourceEnd, cycleTime, channel)
        {
        }
    }
}
