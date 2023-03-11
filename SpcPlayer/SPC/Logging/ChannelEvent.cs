using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpcPlayer.SPC.Logging
{
    /// <summary>
    /// Base class which represents an SPC event associated with a specific channel. Must be inherited.
    /// </summary>
    internal abstract class ChannelEvent : BaseEvent
    {
        private int _channel;
        protected ChannelEvent(EventType eventType, int cycleTime, int channel) : base(eventType, cycleTime)
        {
            _channel = channel;
        }

        public int Channel => _channel;
    }
}
