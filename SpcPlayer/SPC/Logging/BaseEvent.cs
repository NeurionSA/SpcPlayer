using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpcPlayer.SPC.Logging
{
    internal abstract class BaseEvent
    {
        // the cycle number the event occured on
        private int _cycleTime;
        // the type of the event
        private EventType _eventType;

        protected BaseEvent(EventType eventType, int cycleTime)
        {
            _cycleTime = cycleTime;
            _eventType = eventType;
        }

        public EventType EventType => _eventType;
        public int CycleTime => _cycleTime;
    }
}
