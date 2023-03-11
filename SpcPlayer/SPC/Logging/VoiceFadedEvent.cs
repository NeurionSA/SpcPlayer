using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpcPlayer.SPC.Logging
{
    /// <summary>
    /// Represents the cessation of a note due to the voice's envelope having faded to 0.
    /// </summary>
    internal class VoiceFadedEvent : ChannelEvent
    {
        public VoiceFadedEvent(int cycleTime, int channel) : base(EventType.VoiceFaded, cycleTime, channel)
        {
        }
    }
}
