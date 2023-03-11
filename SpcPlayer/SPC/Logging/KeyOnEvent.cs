using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpcPlayer.SPC.Logging
{
    /// <summary>
    /// Represents a Key-On event.
    /// </summary>
    internal class KeyOnEvent : ChannelEvent
    {
        private int _sourceIndex;
        private int _frequency;
        private int _volumeLeft;
        private int _volumeRight;
        public KeyOnEvent(int cycleTime, int channel, int sourceIndex, int frequency, int leftVolume, int rightVolume) : base(EventType.KeyOn, cycleTime, channel)
        {
            _sourceIndex = sourceIndex;
            _frequency = frequency;
            _volumeLeft = leftVolume;
            _volumeRight = rightVolume;
        }

        public int SourceIndex => _sourceIndex;
        public int Frequency => _frequency;
        /// <summary>
        /// The channel's left volume at the time of Key-On.
        /// </summary>
        public int LeftVolume => _volumeLeft;
        /// <summary>
        /// The channel's right volume at the time of Key-On.
        /// </summary>
        public int RightVolume => _volumeRight;
    }
}
