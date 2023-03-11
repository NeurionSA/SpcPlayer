using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpcPlayer.SPC
{
    /// <summary>
    /// Class for calculating the values of a stereo 16-bit VU meter.
    /// </summary>
    internal class VUMeter
    {
        private int _bufferSize;    // the size of the ring buffer, in samples

        private float _vuMeterLeft;     // the calculated value of the left channel for the meter
        private float _vuMeterRight;    // the calculated value of the right channel for the meter

        private short[] _vuBufferLeft;
        private short[] _vuBufferRight;
        private int _vuBufferPos;
        private bool _vuMetersDirty = false;

        /// <summary>
        /// Creates a new instance of the class with the specified sample buffer size.
        /// </summary>
        /// <param name="bufferSize"></param>
        public VUMeter(int bufferSize)
        {
            if (bufferSize <= 0) throw new ArgumentOutOfRangeException("bufferSize");

            _bufferSize = bufferSize;

            _vuBufferLeft = new short[_bufferSize];
            _vuBufferRight = new short[_bufferSize];
        }

        /// <summary>
        /// Resets the status of the meter.
        /// </summary>
        public void Reset()
        {
            _vuBufferPos = 0;
            _vuMeterLeft = 0;
            _vuMeterRight = 0;
            _vuMetersDirty = false;

            for (int i = 0; i < _bufferSize; i++)
            {
                _vuBufferLeft[i] = 0;
                _vuBufferRight[i] = 0;
            }
        }

        /// <summary>
        /// Adds a sample to the VU ring buffer.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        public void AddSample(short left, short right)
        {
            _vuBufferLeft[_vuBufferPos] = left;
            _vuBufferRight[_vuBufferPos] = right;
            _vuBufferPos++;
            if (_vuBufferPos >= _bufferSize) _vuBufferPos = 0;
            _vuMetersDirty = true;
        }

        private void calculateVUMeters()
        {
            // loop through the VU meter buffers and find the largest absolute value
            //long accumLeft = 0;
            //long accumRight = 0;
            int maxLeft = 0;
            int maxRight = 0;
            for (int i = 0; i < _bufferSize; i++)
            {
                //accumLeft += Math.Abs((int)_vuBufferLeft[i]);
                //accumRight += Math.Abs((int)_vuBufferRight[i]);
                maxLeft = Math.Max(Math.Abs((int)_vuBufferLeft[i]), maxLeft);
                maxRight = Math.Max(Math.Abs((int)_vuBufferRight[i]), maxRight);
            }

            // calculate the new VU meter values
            _vuMeterLeft = maxLeft / (float)short.MaxValue;
            _vuMeterRight = maxRight / (float)short.MaxValue;

            _vuMeterLeft = (float)Math.Sqrt(_vuMeterLeft);
            _vuMeterRight = (float)Math.Sqrt(_vuMeterRight);

            //_vuMeterLeft = (float)Math.Sqrt((accumLeft / (double)short.MaxValue) / (double)_bufferSize);
            //_vuMeterRight = (float)Math.Sqrt((accumRight / (double)short.MaxValue) / (double)_bufferSize);

            _vuMetersDirty = false;
        }

        /// <summary>
        /// Gets the value of the meter's left channel.
        /// </summary>
        public float Left
        {
            get
            {
                if (_vuMetersDirty) calculateVUMeters();
                return _vuMeterLeft;
            }
        }

        /// <summary>
        /// Gets the value of the meter's right channel.
        /// </summary>
        public float Right
        {
            get
            {
                if (_vuMetersDirty) calculateVUMeters();
                return _vuMeterRight;
            }
        }
    }
}
