using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpcPlayer.SPC.Analysis
{
    /// <summary>
    /// Encapsulates the entire playtime of a single note in SongAnalysis.
    /// </summary>
    internal class Note
    {
        private int _startTime;
        private int _duration;
        private int _channel;
        private int _sourceIndex;
        private int _frequency;
        private int _volumeLeft;
        private int _volumeRight;

        public Note(int startTime, int duration, int channel, int sourceIndex, int frequency, int volumeLeft, int volumeRight)
        {
            _startTime = startTime;
            _duration = duration;
            _channel = channel;
            _sourceIndex = sourceIndex;
            _frequency = frequency;
            _volumeLeft = volumeLeft;
            _volumeRight = volumeRight;
        }

        public int StartTime => _startTime;
        public int Duration => _duration;
        public int Channel => _channel;
        public int SourceIndex => _sourceIndex;
        public int Frequency => _frequency;
        public int VolumeLeft => _volumeLeft;
        public int VolumeRight => _volumeRight;
    }
}
