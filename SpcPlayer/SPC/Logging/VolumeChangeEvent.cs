using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpcPlayer.SPC.Logging
{
    internal class VolumeChangeEvent : ChannelEvent
    {
        private int _volume;
        private bool _isLeftVolume;

        public VolumeChangeEvent(int cycleTime, int channel, int volume, bool isLeftVolume) : base(EventType.VolumeChange, cycleTime, channel)
        {
            _volume = volume;
            _isLeftVolume = isLeftVolume;
        }

        public int Volume => _volume;
        public bool IsLeftVolume => _isLeftVolume;
    }
}
