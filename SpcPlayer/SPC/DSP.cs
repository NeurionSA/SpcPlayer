#define ADVANCED_LOG_ENABLED    // use an advanced event log during playback to generate a big list of useful information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpcPlayer.SPC.Logging;

namespace SpcPlayer.SPC
{
    internal class DSP
    {
        // this constant is used to decay the value of a voice's VU meter every sample
        private const float voiceVuDecayRate = 1 / 1000f;
        // number of native (32 Khz) samples per counter event
        private static int[] counterRates =
        {
            0, 2048, 1536, 1280, 1024, 768, 640, 512,
            384, 320, 256, 192, 160, 128, 96, 80,
            64, 48, 40, 32, 24, 20, 16, 12,
            10, 8, 6, 5, 4, 3, 2, 1
        };

        // Gaussian table by libopenspc
        // Take note of the 'int32' datatype. These 11-bit hex values are all
        // positive and must be treated as signed.
        private static int[] gauss_coeffs =
        {
            0x000, 0x000, 0x000, 0x000, 0x000, 0x000, 0x000, 0x000,
            0x000, 0x000, 0x000, 0x000, 0x000, 0x000, 0x000, 0x000,
            0x001, 0x001, 0x001, 0x001, 0x001, 0x001, 0x001, 0x001,
            0x001, 0x001, 0x001, 0x002, 0x002, 0x002, 0x002, 0x002,
            0x002, 0x002, 0x003, 0x003, 0x003, 0x003, 0x003, 0x004,
            0x004, 0x004, 0x004, 0x004, 0x005, 0x005, 0x005, 0x005,
            0x006, 0x006, 0x006, 0x006, 0x007, 0x007, 0x007, 0x008,
            0x008, 0x008, 0x009, 0x009, 0x009, 0x00A, 0x00A, 0x00A,
            0x00B, 0x00B, 0x00B, 0x00C, 0x00C, 0x00D, 0x00D, 0x00E,
            0x00E, 0x00F, 0x00F, 0x00F, 0x010, 0x010, 0x011, 0x011,
            0x012, 0x013, 0x013, 0x014, 0x014, 0x015, 0x015, 0x016,
            0x017, 0x017, 0x018, 0x018, 0x019, 0x01A, 0x01B, 0x01B,
            0x01C, 0x01D, 0x01D, 0x01E, 0x01F, 0x020, 0x020, 0x021,
            0x022, 0x023, 0x024, 0x024, 0x025, 0x026, 0x027, 0x028,
            0x029, 0x02A, 0x02B, 0x02C, 0x02D, 0x02E, 0x02F, 0x030,
            0x031, 0x032, 0x033, 0x034, 0x035, 0x036, 0x037, 0x038,
            0x03A, 0x03B, 0x03C, 0x03D, 0x03E, 0x040, 0x041, 0x042,
            0x043, 0x045, 0x046, 0x047, 0x049, 0x04A, 0x04C, 0x04D,
            0x04E, 0x050, 0x051, 0x053, 0x054, 0x056, 0x057, 0x059,
            0x05A, 0x05C, 0x05E, 0x05F, 0x061, 0x063, 0x064, 0x066,
            0x068, 0x06A, 0x06B, 0x06D, 0x06F, 0x071, 0x073, 0x075,
            0x076, 0x078, 0x07A, 0x07C, 0x07E, 0x080, 0x082, 0x084,
            0x086, 0x089, 0x08B, 0x08D, 0x08F, 0x091, 0x093, 0x096,
            0x098, 0x09A, 0x09C, 0x09F, 0x0A1, 0x0A3, 0x0A6, 0x0A8,
            0x0AB, 0x0AD, 0x0AF, 0x0B2, 0x0B4, 0x0B7, 0x0BA, 0x0BC,
            0x0BF, 0x0C1, 0x0C4, 0x0C7, 0x0C9, 0x0CC, 0x0CF, 0x0D2,
            0x0D4, 0x0D7, 0x0DA, 0x0DD, 0x0E0, 0x0E3, 0x0E6, 0x0E9,
            0x0EC, 0x0EF, 0x0F2, 0x0F5, 0x0F8, 0x0FB, 0x0FE, 0x101,
            0x104, 0x107, 0x10B, 0x10E, 0x111, 0x114, 0x118, 0x11B,
            0x11E, 0x122, 0x125, 0x129, 0x12C, 0x130, 0x133, 0x137,
            0x13A, 0x13E, 0x141, 0x145, 0x148, 0x14C, 0x150, 0x153,
            0x157, 0x15B, 0x15F, 0x162, 0x166, 0x16A, 0x16E, 0x172,
            0x176, 0x17A, 0x17D, 0x181, 0x185, 0x189, 0x18D, 0x191,
            0x195, 0x19A, 0x19E, 0x1A2, 0x1A6, 0x1AA, 0x1AE, 0x1B2,
            0x1B7, 0x1BB, 0x1BF, 0x1C3, 0x1C8, 0x1CC, 0x1D0, 0x1D5,
            0x1D9, 0x1DD, 0x1E2, 0x1E6, 0x1EB, 0x1EF, 0x1F3, 0x1F8,
            0x1FC, 0x201, 0x205, 0x20A, 0x20F, 0x213, 0x218, 0x21C,
            0x221, 0x226, 0x22A, 0x22F, 0x233, 0x238, 0x23D, 0x241,
            0x246, 0x24B, 0x250, 0x254, 0x259, 0x25E, 0x263, 0x267,
            0x26C, 0x271, 0x276, 0x27B, 0x280, 0x284, 0x289, 0x28E,
            0x293, 0x298, 0x29D, 0x2A2, 0x2A6, 0x2AB, 0x2B0, 0x2B5,
            0x2BA, 0x2BF, 0x2C4, 0x2C9, 0x2CE, 0x2D3, 0x2D8, 0x2DC,
            0x2E1, 0x2E6, 0x2EB, 0x2F0, 0x2F5, 0x2FA, 0x2FF, 0x304,
            0x309, 0x30E, 0x313, 0x318, 0x31D, 0x322, 0x326, 0x32B,
            0x330, 0x335, 0x33A, 0x33F, 0x344, 0x349, 0x34E, 0x353,
            0x357, 0x35C, 0x361, 0x366, 0x36B, 0x370, 0x374, 0x379,
            0x37E, 0x383, 0x388, 0x38C, 0x391, 0x396, 0x39B, 0x39F,
            0x3A4, 0x3A9, 0x3AD, 0x3B2, 0x3B7, 0x3BB, 0x3C0, 0x3C5,
            0x3C9, 0x3CE, 0x3D2, 0x3D7, 0x3DC, 0x3E0, 0x3E5, 0x3E9,
            0x3ED, 0x3F2, 0x3F6, 0x3FB, 0x3FF, 0x403, 0x408, 0x40C,
            0x410, 0x415, 0x419, 0x41D, 0x421, 0x425, 0x42A, 0x42E,
            0x432, 0x436, 0x43A, 0x43E, 0x442, 0x446, 0x44A, 0x44E,
            0x452, 0x455, 0x459, 0x45D, 0x461, 0x465, 0x468, 0x46C,
            0x470, 0x473, 0x477, 0x47A, 0x47E, 0x481, 0x485, 0x488,
            0x48C, 0x48F, 0x492, 0x496, 0x499, 0x49C, 0x49F, 0x4A2,
            0x4A6, 0x4A9, 0x4AC, 0x4AF, 0x4B2, 0x4B5, 0x4B7, 0x4BA,
            0x4BD, 0x4C0, 0x4C3, 0x4C5, 0x4C8, 0x4CB, 0x4CD, 0x4D0,
            0x4D2, 0x4D5, 0x4D7, 0x4D9, 0x4DC, 0x4DE, 0x4E0, 0x4E3,
            0x4E5, 0x4E7, 0x4E9, 0x4EB, 0x4ED, 0x4EF, 0x4F1, 0x4F3,
            0x4F5, 0x4F6, 0x4F8, 0x4FA, 0x4FB, 0x4FD, 0x4FF, 0x500,
            0x502, 0x503, 0x504, 0x506, 0x507, 0x508, 0x50A, 0x50B,
            0x50C, 0x50D, 0x50E, 0x50F, 0x510, 0x511, 0x511, 0x512,
            0x513, 0x514, 0x514, 0x515, 0x516, 0x516, 0x517, 0x517,
            0x517, 0x518, 0x518, 0x518, 0x518, 0x518, 0x519, 0x519
        };

        // the DSP's registers
        private byte[] _regs = new byte[128];
        // the DSP's voices
        private SPCVoice[] _voices;

        // logging stuff
        private Dictionary<int, int> _sourceBaseFreq = new Dictionary<int, int>();
        private EventLogger _eventLog = new EventLogger();
        private int _lastCycleLog;      // the last cycle a log message was written on
        private bool _suppressLogging = false;

        // values derived from global regs 0x0C, 0x1C (MVOLL, MVOLR)
        private int _masterVolumeLeft;
        private int _masterVolumeRight;

        // values derived from global regs 0x2C, 0x3C (EVOLL, EVOLR)
        private int _echoVolumeLeft;
        private int _echoVolumeRight;

        // values derived from global echo registers, and a arrays for the FIR filter set up as a ring buffer
        private int _echoFeedback;
        private int _echoBufferPointer;         // the start of the echo ring buffer in SPC memory
        private int _echoBufferOffset;          // offset within the echo ring buffer for where the next sample is read from
        private int _echoDelay;                 // length of the echo ring buffer = 512 16-bit stereo samples * this value
        private int _firBufferPtr;              // current position within the FIR ring buffers
        private int[] _firBufferL = new int[8];
        private int[] _firBufferR = new int[8];
        // an internal echo buffer to use, separate from the one in SPC main memory;
        // this exists as an option because some of the SPC dumps I've encountered have dirty echo buffers when playback
        // starts, potentially causing a brief spike of unpleasant echo noise
        private short[] _internalEchoBuffer = new short[15360];
        private bool _useInternalEchoBuffer = true;

        // values derived from global reg 0x6C (FLAG)
        private bool _resetFlag = false;
        private bool _muteFlag = false;
        private bool _echoEnabled = false;
        private int _noiseClock;
        private int _noiseCounter;      // counter that increments on each generated sample, compared to counterRates[_noiseClock]
        private short _noiseLevel;    // the current value of the global noise generator's output
        private Random _random = new Random();  // RNG for the noise generator

        // the SPCCore instance the DSP will read memory from
        private SPCCore _core;

        // internal stream for the generated waveform buffer
        private MemoryStream _waveStream;
        // BinaryWriter for operating on the waveform buffer.
        private BinaryWriter _waveWriter;
        // BinaryReader for operating on the waveform buffer.
        private BinaryReader _waveReader;

        // the output sample rate
        private int _sampleRate;

        // the total number of cycles in a 32KHz clock
        private int _totalCycles;

        // whether or not to do the heavy advanced logging for song analysis
        private bool _enableHeavyLogging = false;

        // VU meters for the 8 channels and the main output
        private VUMeter[] _voiceMeters = new VUMeter[8];
        private VUMeter _mainMeter;

        private void initAdvancedLoggingFeatures()
        {
            // add the predicted C-5 frequencies for the instruments in Ogre Battle - Revolt
            //_sourceBaseFreq.Add(2, 31911);
            //_sourceBaseFreq.Add(5, 31911);
            //_sourceBaseFreq.Add(6, 31911);
            //_sourceBaseFreq.Add(8, 31911);
            //_sourceBaseFreq.Add(9, 4186);
            //_sourceBaseFreq.Add(11, 8367);
            //_sourceBaseFreq.Add(15, 31454);
            //_sourceBaseFreq.Add(16, 43110);
            //_sourceBaseFreq.Add(17, 25105);
            //_sourceBaseFreq.Add(18, 25105);
            //_sourceBaseFreq.Add(20, 25105);
            //_sourceBaseFreq.Add(22, 31908);
            //_sourceBaseFreq.Add(23, 31908);
            //_sourceBaseFreq.Add(24, 31911);
            //_sourceBaseFreq.Add(26, 32622);
            //_sourceBaseFreq.Add(27, 16215);
            //_sourceBaseFreq.Add(30, 16215);
            //_sourceBaseFreq.Add(31, 16215);
            //_sourceBaseFreq.Add(40, 31914);

            // copy those to the fancy event log
            foreach (KeyValuePair<int, int> entry in _sourceBaseFreq)
            {
                _eventLog.SetSourceBaseRate(entry.Key, entry.Value);
            }
        }

        /// <summary>
        /// Creates a new instance of this class, connecting it to the specified SPCCore instance.
        /// </summary>
        /// <param name="core"></param>
        /// <param name="sampleRate"></param>
        public DSP(SPCCore core, int sampleRate)
        {
            // check arguments
            ArgumentNullException.ThrowIfNull(core);
            if (sampleRate < 1) throw new ArgumentOutOfRangeException("sampleRate");

            _core = core;
            _sampleRate = sampleRate;

            // create the wave stream, reader, and writer
            _waveStream = new MemoryStream();
            _waveWriter = new BinaryWriter(_waveStream);
            _waveReader = new BinaryReader(_waveStream);

            // create the voices and their VU meters
            _voices = new SPCVoice[8];
            for (int i = 0; i < 8; i++)
            {
                _voices[i] = new SPCVoice();
                _voiceMeters[i] = new VUMeter(512);
            }
            // create the main output VU meter
            _mainMeter = new VUMeter(512);

            // create the register array
            _regs = new byte[128];

            initAdvancedLoggingFeatures();
        }

        public void LoadStateFromFile(SPCFile file)
        {
            ArgumentNullException.ThrowIfNull(file);

            // copy the register bytes from the file
            Array.Copy(file.DSP, _regs, 128);

            // reset the DSP's status
            reset();

            // ensure special code is executed for the initial state by writing to all relevant registers
            // registers for each channel
            for (int i = 0; i < 8; i++)
            {
                WriteRegister((i << 4) + 0x0, _regs[(i << 4) + 0x0]);
                WriteRegister((i << 4) + 0x1, _regs[(i << 4) + 0x1]);
                WriteRegister((i << 4) + 0x2, _regs[(i << 4) + 0x2]);
                WriteRegister((i << 4) + 0x3, _regs[(i << 4) + 0x3]);
                WriteRegister((i << 4) + 0x4, _regs[(i << 4) + 0x4]);
                WriteRegister((i << 4) + 0x5, _regs[(i << 4) + 0x5]);
                WriteRegister((i << 4) + 0x6, _regs[(i << 4) + 0x6]);
                WriteRegister((i << 4) + 0x7, _regs[(i << 4) + 0x7]);
                WriteRegister((i << 4) + 0xF, _regs[(i << 4) + 0xF]);
            }
            // global registers
            WriteRegister(0x0C, _regs[0x0C]);
            WriteRegister(0x1C, _regs[0x1C]);
            WriteRegister(0x2C, _regs[0x2C]);
            WriteRegister(0x3C, _regs[0x3C]);
            WriteRegister(0x4C, _regs[0x4C]);
            WriteRegister(0x5C, _regs[0x5C]);
            WriteRegister(0x6C, _regs[0x6C]);
            WriteRegister(0x0D, _regs[0x0D]);
            WriteRegister(0x2D, _regs[0x2D]);
            WriteRegister(0x3D, _regs[0x3D]);
            WriteRegister(0x4D, _regs[0x4D]);
            WriteRegister(0x6D, _regs[0x6D]);
            WriteRegister(0x7D, _regs[0x7D]);
            _suppressLogging = false;
        }

        private void reset()
        {
            // reset the voice's initial states, as well as their meters
            for (int i = 0; i < 8; i++)
            {
                _voices[i].Reset();
                _voiceMeters[i].Reset();
            }
            // reset the main output voice meter
            _mainMeter.Reset();

            // reset the FIR buffer
            for (int i = 0; i < 8; i++)
            {
                _firBufferL[i] = 0;
                _firBufferR[i] = 0;
            }
            _firBufferPtr = 0;

            // if the internal echo buffer is in use, zero it out
            if (_useInternalEchoBuffer)
            {
                for (int i = 0; i < _internalEchoBuffer.Length; i++)
                {
                    _internalEchoBuffer[i] = 0;
                }
            }

            // reset the noise generator
            _noiseCounter = 0;
            _noiseClock = 0;

            // reset logging stuff
            _eventLog.Clear();
            _totalCycles = 0;
            _lastCycleLog = 0;
        }

        private void logToConsole(string comment)
        {
            if (!_suppressLogging)
            {
                // if the last cycle matches the current cycle, don't print the cycle number
                if (_lastCycleLog == _totalCycles)
                {
                    Console.WriteLine("[DSP]       {0,9}           : {1}", "", comment);
                }
                else
                {
                    // also print the cycle difference between this log entry and the last
                    int delta = _totalCycles - _lastCycleLog;
                    Console.WriteLine("[DSP] cycle {0,9} (+ {1,5}) : {2}", _totalCycles, delta, comment);
                }
            }
            _lastCycleLog = _totalCycles;
        }

        /// <summary>
        /// Decodes the next 4 sample chunk into the voice's BRR ring buffer and advances the variable that tracks the
        /// current position within the block. Does NOT do anything about reaching the end of the block.
        /// </summary>
        /// <param name="voice"></param>
        private void decodeBrrBuffer(SPCVoice voice)
        {
            // get the block's header byte
            voice.BrrBlockHeader = _core.Memory[voice.BrrBlockPointer];

            // decode the block
            int shift = voice.BrrBlockHeader >> 4;
            int filter = (voice.BrrBlockHeader & 0xC) >> 2;
            bool loopFlag = (voice.BrrBlockHeader & 0x2) != 0;
            bool endFlag = (voice.BrrBlockHeader & 0x1) != 0;

            // set the initial values of last1 and last2, accounting for wrapping within the buffer
            int last1 = voice.BrrDecodeBuffer[(voice.BrrBufferWritePosition + 11) % 12];
            int last2 = voice.BrrDecodeBuffer[(voice.BrrBufferWritePosition + 10) % 12];

            for (int i = 0; i < 4; i++)
            {
                // get the sample's data nibble (note: lower sample is in the high nibble)
                // calculate the shift value for the nibble
                int nibbleShift = (i % 2) == 0 ? 4 : 0;
                // calculate the offset into the BRR block
                int offset = (i >> 1) + 1 + voice.BrrDecodeOffset;
                // get the sample's nibble value
                // NOTE: the address wraps within the 64 KB memory space, and so far only one game's sound dumps have made
                // this necessary. DOOM does some strange things where it modifies entries in the source table on the fly
                // to change the start address of the percussion source, with an address of 0xFFFF used for when the source
                // is currently unused
                int value = (_core.Memory[(ushort)(voice.BrrBlockPointer + offset)] >> nibbleShift) & 0xF;

                // shift the bits left by the shift field from the header
                // the value is signed 2's complement, so convert it
                if (shift <= 0xC)
                    value = (value < 8 ? value : value - 16) << shift;
                else
                    value = value < 8 ? 1 << 11 : (-1) << 11;

                // apply the appropriate 'filter' to the value
                switch (filter)
                {
                    case 1:
                        value += ((-last1 >> 4) & ~1) + last1;
                        break;

                    case 2:
                        value += (last1 << 1);
                        value += ((-3 * last1) >> 5) & ~1;
                        value -= last2;
                        value += (last2 >> 4) & ~1;
                        break;

                    case 3:
                        value += last1 << 1;
                        value += ((-13 * last1) >> 6) & ~1;
                        value += (((last2 * 3) >> 4) & ~1) + -last2;
                        break;
                }

                // clamp the value to signed 17 bits, don't know why, but doing this makes all the sound effects
                // work exactly as they are supposed to; those guys at Sony sure are weird!
                if (value > 0xFFFF) value = 0xFFFF;
                else if (value < -0xFFFF) value = -0xFFFF;
                // then drop the lowest bit (clipping it to 15 bits)
                value &= ~1;
                // clip the value (just need to cast it to short)
                voice.BrrDecodeBuffer[voice.BrrBufferWritePosition + i] = (short)value;

                // swap last1 into last2, and the value into last1
                last2 = last1;
                last1 = (short)value;
            }

            // increment the byte offset within the block by 2
            voice.BrrDecodeOffset += 2;
            // increment the buffer write position
            voice.BrrBufferWritePosition = (voice.BrrBufferWritePosition + 4) % 12;
        }

        // gets the next sample to output from the voice's decode buffer
        private short getNextSample(int voiceIndex)
        {
            SPCVoice voice = _voices[voiceIndex];

            short sample = 0;

            // check the noise-enable flag
            if (voice.NoiseEnabled)
            {
                // the sample is the output of the global noise generator
                sample = _noiseLevel;
            }
            else
            {
                int i, d, outx;
                // 2-point linear interpolation
                i = voice.InterpolationIndex >> 12;
                d = voice.InterpolationIndex & 0xFFF;
                outx = (voice.BrrDecodeBuffer[(voice.BrrBufferReadPosition + i + 0) % 12] * (4095 - d)) >> 12;
                outx += (voice.BrrDecodeBuffer[(voice.BrrBufferReadPosition + i + 1) % 12] * d) >> 12;
                sample = (short)outx;

                //// 4-point gaussian interpolation
                //i = voice.interpolationIndex >> 12;
                //d = (voice.interpolationIndex >> 4) & 0xFF;
                //outx = ((gauss_coeffs[255 - d] * voice.brrDecodeBuffer[(voice.brrBufferReadPosition + i + 0) % 12]) & ~2047);
                //outx += ((gauss_coeffs[511 - d] * voice.brrDecodeBuffer[(voice.brrBufferReadPosition + i + 1) % 12]) & ~2047);
                //outx += ((gauss_coeffs[256 + d] * voice.brrDecodeBuffer[(voice.brrBufferReadPosition + i + 2) % 12]) & ~2047);
                //outx += ((gauss_coeffs[0 + d] * voice.brrDecodeBuffer[(voice.brrBufferReadPosition + i + 3) % 12]) & ~2047);
                //sample = (short)((outx >> 11) & ~1);
            }

            int pitch = voice.Pitch;
            if (voice.PitchModEnabled && !voice.NoiseEnabled)
            {
                pitch = voice.Pitch * (_voices[voiceIndex - 1].OutX + 32768) >> 15;
            }
            voice.InterpolationIndex += pitch;
            // if the interpolation index >= 0x4000 then advance the buffer and decode the next 4 sample chunk
            while (voice.InterpolationIndex >= 0x4000)
            {
                voice.InterpolationIndex -= 0x4000;
                // increment buffer position by 4, wrapping if needed
                voice.BrrBufferReadPosition = (voice.BrrBufferReadPosition + 4) % 12;

                // if the the end of the block has been reached, figure out what to do based on the current block header
                if (voice.BrrDecodeOffset >= 8)
                {
                    voice.BrrDecodeOffset = 0;
                    byte head = voice.BrrBlockHeader;
                    
                    bool loopFlag = (head & 0x2) != 0;
                    bool endFlag = (head & 0x1) != 0;

                    // if the end flag is set in this next block, set the appropriate bit in the ENDX register
                    if (endFlag)
                    {
                        _regs[0x7C] |= (byte)(1 << voiceIndex);
                    }
                    // end flag set and loop flag clear means the voice goes off and the envelope is instantly set to 0
                    if (endFlag && !loopFlag)
                    {
                        voice.Status = VoiceStatus.Release;
                        voice.Envelope = 0;
                        if (_enableHeavyLogging)
                        {
                            // log the end of the voice playback by reaching the end of a non-looping sample
                            _eventLog.LogSourceEnd(_totalCycles, voiceIndex);
                        }
                    }
                    // if both end and loop flags are set...
                    if (endFlag && loopFlag)
                    {
                        // we return to the sample's loop start position instead, and then decode from there
                        voice.BrrBlockPointer = voice.SourceSampleLoopStart;
                        decodeBrrBuffer(voice);
                    }
                    else
                    {
                        // we decode the next block
                        voice.BrrBlockPointer += 9;
                        decodeBrrBuffer(voice);
                    }
                }
                else
                {
                    // the end of the current block has not been reached, decode the next group of 4 samples as normal
                    decodeBrrBuffer(voice);
                }
            }

            return sample;
        }

        /// <summary>
        /// Generates a number of samples and adds them to the DSP's internal buffer.
        /// </summary>
        /// <param name="numSamples"></param>
        public void GenerateSamples(int numSamples)
        {
            // loop for the number of desired samples
            for (int i = 0; i < numSamples; i++)
            {
                // TODO: support non-native playback rate

                // main accumulators for sample output
                int outLeft = 0;
                int outRight = 0;
                // accumulators for echo mixing
                int echoLeft = 0;
                int echoRight = 0;

                // loop through the 8 voices
                for (int j = 0; j < 8; j++)
                {
                    SPCVoice voice = _voices[j];

                    // skip the voice if it's inactive
                    if (!voice.IsActive)
                    {
                        // but not before adding 0, 0 to its VU Buffer
                        _voiceMeters[j].AddSample(0, 0);
                        continue;
                    }

                    // get the voice's sample
                    int sample = getNextSample(j);
                    int c, cyc = _totalCycles - voice.EnvelopeCycle;

                    // and now for envelope calculation
                    switch (voice.Status)
                    {
                        case VoiceStatus.Attack:
                            c = counterRates[(voice.AttackRate << 1) + 1];

                            if (c == 0)
                            {
                                voice.EnvelopeCycle = _totalCycles;
                            }
                            else if (cyc > c)
                            {
                                voice.EnvelopeCycle += c;
                                // linear increase
                                voice.Envelope += 32;
                            }

                            // when the envelope exceeds 0x7FF it switches to Decay or Sustain
                            if (voice.Envelope > 0x7FF)
                            {
                                voice.Envelope = 0x7FF;
                                if (voice.SustainLevel != 7) voice.Status = VoiceStatus.Decay;
                                else voice.Status = VoiceStatus.Sustain;
                            }
                            break;

                        case VoiceStatus.Decay:
                            c = counterRates[(voice.DecayRate << 1) + 0x10];

                            if (c == 0)
                            {
                                voice.EnvelopeCycle = _totalCycles;
                            }
                            else if (cyc > c)
                            {
                                voice.EnvelopeCycle += c;
                                // exponential decrease
                                voice.Envelope -= ((voice.Envelope - 1) >> 8) + 1;

                                // when the upper 3 bits of the envelope match the sustain level, switch to the sustain phase
                                if ((voice.Envelope >> 8) == voice.SustainLevel) voice.Status = VoiceStatus.Sustain;
                            }
                            break;

                        case VoiceStatus.Sustain:
                            c = counterRates[voice.SustainRate];

                            if (c == 0)
                            {
                                voice.EnvelopeCycle = _totalCycles;
                            }
                            else if (cyc > c)
                            {
                                voice.EnvelopeCycle += c;
                                // exponential decrease
                                voice.Envelope -= ((voice.Envelope - 1) >> 8) + 1;
                            }
                            break;

                        case VoiceStatus.Release:
                            c = counterRates[0x1A];

                            if (c == 0)
                            {
                                voice.EnvelopeCycle = _totalCycles;
                            }
                            else if (cyc > c)
                            {
                                voice.EnvelopeCycle += c;
                                // linear decrease
                                voice.Envelope -= 8; // 1/256th

                                if (voice.Envelope <= 0)
                                {
                                    voice.Envelope = 0;
                                    // voice has decayed to 0, log it
                                    if (_enableHeavyLogging) _eventLog.LogVoiceFaded(_totalCycles, j);
                                }
                            }

                            break;

                        case VoiceStatus.Gain:
                            // check which gain mode
                            if (voice.GainMode == GainMode.Direct)
                            {
                                // set the envelope from the gain parameter
                                voice.Envelope = voice.GainParameter << 4;
                            }
                            else
                            {
                                // perform the change only if the counter allows it
                                c = counterRates[voice.GainParameter];

                                if (c == 0)
                                {
                                    voice.EnvelopeCycle = _totalCycles;
                                }
                                else if (cyc > c)
                                {
                                    voice.EnvelopeCycle += c;

                                    switch (voice.GainMode)
                                    {
                                        case GainMode.LinearDecrease:
                                            voice.Envelope -= 32;
                                            break;

                                        case GainMode.ExponentialDecrease:
                                            voice.Envelope -= ((voice.Envelope - 1) >> 8) + 1;
                                            break;

                                        case GainMode.LinearIncrease:
                                            voice.Envelope += 32;
                                            break;

                                        case GainMode.BentIncrease:
                                            voice.Envelope += (voice.Envelope < 0x600) ? 32 : 8;
                                            break;
                                    }
                                }
                            }
                            break;
                    }

                    // clamp the envelope to 11 bits
                    voice.Envelope = voice.Envelope < 0 ? 0 : (voice.Envelope > 0x7FF ? 0x7FF : voice.Envelope);

                    // set the voice's ENVX register to the upper 7 bits of the envelope
                    _regs[(j << 4) + 8] = (byte)(voice.Envelope >> 4);

                    // apply the envelope to the sample
                    // (at this point the sample is signed 15 bits, the envelope is unsigned 11 bits)
                    sample = (sample * voice.Envelope) >> 11;

                    // set the voice's OUTX register to the upper bits of the sample
                    _regs[(j << 4) + 9] = (byte)(sample >> 7);
                    // also set the voice's internal outX to the value of the sample, as the outX of 'previous' channels
                    // is used for calculating pitch modulation
                    voice.OutX = sample;

                    // if we want to MUTE a specific source/channel, this is where we would set the sample to 0
                    if (voice.IsMuted) sample = 0;

                    // calculate the voice's Left and Right values
                    int voiceLeft = (sample * voice.VolumeLeft) >> 7;
                    int voiceRight = (sample * voice.VolumeRight) >> 7;

                    // add the samples to the VU buffer
                    _voiceMeters[j].AddSample((short)voiceLeft, (short)voiceRight);

                    // add the voice to the main output
                    outLeft += voiceLeft;
                    outRight += voiceRight;
                    // add the voice's output to the echo output if applicable
                    if (voice.EchoEnabled)
                    {
                        echoLeft += voiceLeft;
                        echoRight += voiceRight;
                    }
                }

                // apply the master volume to the accumulators
                outLeft = (outLeft * _masterVolumeLeft) >> 7;
                outRight = (outRight * _masterVolumeRight) >> 7;

                // from looking at libopenspc, it looks like I start by reading a pair of stereo samples in from the
                // echo ring buffer in memory and slotting them into the FIR filter queue
                // this is always done, even if echo is disabled by the FLG register

                // read in the pair of stereo samples from the echo ring buffer, placing them into the FIR buffer
                int echoBase = (_echoBufferPointer + _echoBufferOffset) & 0xFFFF;
                // if we're using the internal buffer, source the data from there instead of SPC memory
                if (_useInternalEchoBuffer)
                {
                    // source from internal buffer
                    _firBufferL[_firBufferPtr] = _internalEchoBuffer[_echoBufferOffset >> 1];
                    _firBufferR[_firBufferPtr] = _internalEchoBuffer[(_echoBufferOffset >> 1) + 1];
                }
                else
                {
                    // source the data from SPC memory
                    _firBufferL[_firBufferPtr] = (short)(_core.Memory[echoBase] + (_core.Memory[echoBase + 1] << 8));
                    _firBufferR[_firBufferPtr] = (short)(_core.Memory[echoBase + 2] + (_core.Memory[echoBase + 3] << 8));
                }
                
                // evaluate the FIR filter
                // set the initial state with the first sample
                int firL = _firBufferL[_firBufferPtr] * (sbyte)_regs[0x7F];
                int firR = _firBufferR[_firBufferPtr] * (sbyte)_regs[0x7F];
                // then loop with the other 7 samples in the ring buffer
                for (int k = 0; k < 7; k++)
                {
                    // rotate the buffer pointer
                    _firBufferPtr = (_firBufferPtr + 1) & 0x7;
                    // add in the next element of the filter (going from register 0x6F down to 0xF)
                    firL += _firBufferL[_firBufferPtr] * (sbyte)_regs[((6 - k) << 4) + 0xF];
                    firR += _firBufferR[_firBufferPtr] * (sbyte)_regs[((6 - k) << 4) + 0xF];
                }
                // _firBufferPtr is now pointing to the oldest sample in the ring, the one that will be replaced next
                // time the filter is evaluated
                // add the filter's output to the main output, applying the echo volume registers
                outLeft += firL * _echoVolumeLeft >> 14;
                outRight += firR * _echoVolumeRight >> 14;

                // now we check to see if echo buffer write is enabled
                if (_echoEnabled)
                {
                    // adjust the FIR output by the echo feedback volume and mix that into the value that gets
                    // written to the echo buffer
                    echoLeft += firL * _echoFeedback >> 14;
                    echoRight += firR * _echoFeedback >> 14;
                    // clamp the value to write to the the echo buffer to the valid range
                    echoLeft = Math.Min(Math.Max(echoLeft, short.MinValue), short.MaxValue);
                    echoRight = Math.Min(Math.Max(echoRight, short.MinValue), short.MaxValue);
                    // branch if we're using the internal echo buffer
                    if (_useInternalEchoBuffer)
                    {
                        // use the internal buffer
                        _internalEchoBuffer[_echoBufferOffset >> 1] = (short)echoLeft;
                        _internalEchoBuffer[(_echoBufferOffset >> 1) + 1] = (short)echoRight;
                    }
                    else
                    {
                        // write back to SPC memory
                        _core.Memory[echoBase] = (byte)(echoLeft & 0xFF);
                        _core.Memory[echoBase + 1] = (byte)(echoLeft >> 8);
                        _core.Memory[echoBase + 2] = (byte)(echoRight & 0xFF);
                        _core.Memory[echoBase + 3] = (byte)(echoRight >> 8);
                    }                   
                }
                // advance the position of the echo buffer offset
                _echoBufferOffset += 4;
                // wrap the buffer offset within the buffer's range
                if (_echoBufferOffset >= (_echoDelay << 11))
                    _echoBufferOffset = 0;

                // clamp the accumulators to the valid range
                outLeft = Math.Min(Math.Max(outLeft, short.MinValue), short.MaxValue);
                outRight = Math.Min(Math.Max(outRight, short.MinValue), short.MaxValue);

                // add the samples to the main output VU meter
                _mainMeter.AddSample((short)outLeft, (short)outRight);

                // write to the output buffer
                _waveWriter.Write((short)outLeft);
                _waveWriter.Write((short)outRight);

                // increment the cycle counter
                _totalCycles++;

                // increment the noise generator's counter and compare it to the clock
                _noiseCounter++;
                if (_noiseCounter >= counterRates[_noiseClock])
                {
                    _noiseCounter = 0;
                    _noiseLevel = (short)_random.Next(short.MinValue, short.MaxValue);

                }
            }
        }

        /// <summary>
        /// Retrieves all the samples from the output buffer.
        /// </summary>
        /// <returns></returns>
        public byte[] RetrieveSamples()
        {
            // return the bytes
            return RetrieveSamples((int)_waveStream.Length);
        }

        /// <summary>
        /// Retrieves a number of samples from the output buffer.
        /// </summary>
        /// <returns></returns>
        public byte[] RetrieveSamples(int count)
        {
            byte[] ret;

            // seek back to the beginning of the buffer
            _waveStream.Seek(0, SeekOrigin.Begin);

            // get the byte array
            ret = _waveReader.ReadBytes(count);

            // if the number of bytes requested was less than the size of the buffer, rebase the buffer at 0
            if (count < _waveStream.Length)
            {
                byte[] bytes = _waveReader.ReadBytes((int)(_waveStream.Length - _waveStream.Position));

                _waveStream.Seek(0, SeekOrigin.Begin);
                _waveStream.SetLength(0);

                _waveWriter.Write(bytes);
            }
            else
            {
                // clear the buffer
                _waveStream.Seek(0, SeekOrigin.Begin);
                _waveStream.SetLength(0);
            }
            
            // return the array
            return ret;
        }

        /// <summary>
        /// Gets how many bytes are currently in the audio output buffer.
        /// </summary>
        public long BufferLength => _waveStream.Length;

        private void setPitch(int voiceIndex)
        {
            // set the voice's pitch
            SPCVoice voice = _voices[voiceIndex];

            // the old pitch, for logging purpose
            int oldPitch = voice.Pitch;

            voice.Pitch = _regs[(voiceIndex << 4) + 0x2] + ((_regs[(voiceIndex << 4) + 0x3] & 0x3F) << 8);
            voice.Rate = (int)(32000 * voice.Pitch / Math.Pow(2, 12));

            if (_enableHeavyLogging)
            {
                // if the voice is not in the release phase, and the pitch actually changed, log it
                if ((voice.Status != VoiceStatus.Release) && (voice.Pitch != oldPitch))
                    _eventLog.LogPitchChange(_totalCycles, voiceIndex, voice.Rate);
            }
        }

        private void keyOn()
        {
            for (int i = 0, m = 1; i < 8; i++, m <<= 1)
            {
                if ((_regs[0x4C] & m) != 0)
                {
                    SPCVoice voice = _voices[i];
                    voice.EnvelopeCycle = _totalCycles;

                    // if the source has a known C-5 frequency
                    if (_sourceBaseFreq.ContainsKey(voice.SourceIndex))
                    {
                        // print the log with the predicted note number
                        string note = NotePrediction.GetNoteName(_sourceBaseFreq[voice.SourceIndex], voice.Rate);
                        //logToConsole(string.Format("KEY-ON v{0}, s{1,2}, at {2} ({3} Hz)", i, voice.SourceIndex, note, voice.Rate));
                    }
                    else
                    {
                        // print the log as normal
                        //logToConsole(string.Format("KEY-ON v{0}, s{1,2}, at {2} Hz", i, voice.SourceIndex, voice.Rate));
                    }

                    if (_enableHeavyLogging)
                    {
                        _eventLog.LogKeyOn(
                            _totalCycles,
                            i,
                            voice.SourceIndex,
                            voice.Rate,
                            voice.VolumeLeft,
                            voice.VolumeRight);
                    }
                    

                    // voice has been key-on'd
                    // clear the corresponding bit in the ENDX register
                    _regs[0x7C] &= (byte)~m;

                    // set status appropriately based on current envelope/gain model
                    if (voice.EnableADSR)
                    {
                        voice.Status = VoiceStatus.Attack;
                        voice.Envelope = 0;
                    }
                    else
                    {
                        voice.Status = VoiceStatus.Gain;
                    }
                    voice.InterpolationIndex = 0;

                    // calculate the voice's source sample start and sample loop start
                    int entryOffset = (_regs[0x5D] << 8) + (voice.SourceIndex << 2);
                    voice.SourceSampleStart = _core.Memory[entryOffset] + (_core.Memory[entryOffset + 1] << 8);
                    voice.SourceSampleLoopStart = _core.Memory[entryOffset + 2] + (_core.Memory[entryOffset + 3] << 8);

                    // set the pointer for the first BRR block to decode
                    voice.BrrBlockPointer = voice.SourceSampleStart;
                    voice.BrrDecodeOffset = 0;
                    voice.BrrBufferReadPosition = 0;
                    voice.BrrBufferWritePosition = 0;

                    // clear the contents of the decode buffer
                    for (int j = 0; j < 12; j++) voice.BrrDecodeBuffer[j] = 0;

                    // decode the first 2 groups of 4 samples into the buffer
                    decodeBrrBuffer(voice);
                    decodeBrrBuffer(voice);
                }
            }
        }

        private void keyOff()
        {
            for (int i = 0, m = 1; i < 8; i++, m <<= 1)
            {
                if ((_regs[0x5C] & m) != 0)
                {
                    if (_enableHeavyLogging)
                    {
                        // only log the event if the voice is actually active
                        if (_voices[i].IsActive) _eventLog.LogKeyOff(_totalCycles, i);
                    }

                    // voice has been released
                    _voices[i].Status = VoiceStatus.Release;
                }
            }
        }

        public byte ReadRegister(int register)
        {
            // get the register's value that will be returned
            byte ret = _regs[register];

            // handle any special behavior based on register
            switch (register)
            {
                case 0x7C:  // ENDX
                    // log it
                    //if (!suppressLogging)
                    //    logToConsole("Read from ENDX register");
                    break;
            }

            return ret;
        }

        public void WriteRegister(int register, byte value)
        {
            // the voice that (may) be associated with this register
            SPCVoice voice = _voices[register >> 4];

            // remember the old value, for the purposes of logging when a write actually changes a register's value
            byte prev = _regs[register];
            bool prevFlag;

            // set the register's value, then handle any necessary special behavior
            _regs[register] = value;

            switch (register)
            {
                case 0x00:  // Left channel volume for each voice
                case 0x10:
                case 0x20:
                case 0x30:
                case 0x40:
                case 0x50:
                case 0x60:
                case 0x70:
                    voice.VolumeLeft = (sbyte)value;

                    if (_enableHeavyLogging)
                    {
                        // if the value changed and the voice is not in the release phase, log it
                        if ((_regs[register] != prev) && (voice.Status != VoiceStatus.Release))
                            _eventLog.LogVolumeChange(_totalCycles, register >> 4, voice.VolumeLeft, true);
                    }
                    break;

                case 0x01:  // Right channel volume for each voice
                case 0x11:
                case 0x21:
                case 0x31:
                case 0x41:
                case 0x51:
                case 0x61:
                case 0x71:
                    voice.VolumeRight = (sbyte)value;

                    if (_enableHeavyLogging)
                    {
                        // if the value changed and the voice is not in the release phase, log it
                        if ((_regs[register] != prev) && (voice.Status != VoiceStatus.Release))
                            _eventLog.LogVolumeChange(_totalCycles, register >> 4, voice.VolumeRight, false);
                    }
                    break;

                case 0x02:  // Lower 8 bits of pitch for each voice
                case 0x12:
                case 0x22:
                case 0x32:
                case 0x42:
                case 0x52:
                case 0x62:
                case 0x72:
                    setPitch(register >> 4);
                    break;

                case 0x03:  // Upper 8 bits of pitch for each voice
                case 0x13:
                case 0x23:
                case 0x33:
                case 0x43:
                case 0x53:
                case 0x63:
                case 0x73:
                    setPitch(register >> 4);
                    break;

                case 0x04:  // Source number for each voice
                case 0x14:
                case 0x24:
                case 0x34:
                case 0x44:
                case 0x54:
                case 0x64:
                case 0x74:
                    voice.SourceIndex = value;
                    break;

                case 0x05:  // ADSR-1 for each voice
                case 0x15:
                case 0x25:
                case 0x35:
                case 0x45:
                case 0x55:
                case 0x65:
                case 0x75:
                    // handle changing modes
                    voice.EnableADSR = (value & 0x80) != 0;
                    // if the voice was in an ADSR state previously and ADSR is now disabled, switch to GAIN mode
                    if (!voice.EnableADSR)
                    {
                        switch (voice.Status)
                        {
                            case VoiceStatus.Attack:
                            case VoiceStatus.Decay:
                            case VoiceStatus.Sustain:
                                voice.Status = VoiceStatus.Gain;
                                break;
                        }
                    }

                    voice.AttackRate = value & 0xF;
                    voice.DecayRate = (value >> 4) & 7;

                    // if heavy logging is enabled, log the change
                    if (_enableHeavyLogging)
                        _eventLog.LogAttackDecayChange(_totalCycles, register >> 4, voice.EnableADSR, voice.AttackRate, voice.DecayRate);
                    break;

                case 0x06:  // ADSR-2 for each voice
                case 0x16:
                case 0x26:
                case 0x36:
                case 0x46:
                case 0x56:
                case 0x66:
                case 0x76:
                    voice.SustainLevel = value >> 5;
                    voice.SustainRate = value & 0x1F;

                    // if heavy logging is enabled, log the change
                    if (_enableHeavyLogging)
                        _eventLog.LogSustainChange(_totalCycles, register >> 4, voice.SustainLevel, voice.SustainRate);
                    break;

                case 0x07:  // GAIN for each voice
                case 0x17:
                case 0x27:
                case 0x37:
                case 0x47:
                case 0x57:
                case 0x67:
                case 0x77:
                    // check the high bit of the value
                    if ((value & 0x80) == 0)
                    {
                        // direct mode
                        voice.GainMode = GainMode.Direct;
                        voice.GainParameter = value & 0x7F;
                    }
                    else
                    {
                        voice.GainParameter = value & 0x1F;
                        voice.GainMode = (GainMode)(value >> 5);
                    }
                    // if the value changed, log it
                    if ((_regs[register] != prev) && _enableHeavyLogging)
                        _eventLog.LogGainChange(_totalCycles, register >> 4, voice.GainMode, voice.GainParameter);
                    break;

                case 0x0F:  // COEF
                case 0x1F:
                case 0x2F:
                case 0x3F:
                case 0x4F:
                case 0x5F:
                case 0x6F:
                case 0x7F:
                    // log it
                    if (prev != value)
                        logToConsole(string.Format("Change to COEF register {0:X2} -> {1:X2}", register, value));
                    break;

                case 0x0C:  // Master Volume Left
                    _masterVolumeLeft = (sbyte)value;
                    break;

                case 0x1C:  // Master Volume Right
                    _masterVolumeRight = (sbyte)value;
                    break;

                case 0x2C:  // Echo Volume Left
                    if (prev != value) logToConsole(string.Format("Change to EVOL-L register -> {0:X2}", value));
                    _echoVolumeLeft = (sbyte)value;
                    break;

                case 0x3C:  // Echo Volume Right
                    if (prev != value) logToConsole(string.Format("Change to EVOL-R register -> {0:X2}", value));
                    _echoVolumeRight = (sbyte)value;
                    break;

                case 0x4C:  // Key-On
                    keyOn();
                    break;

                case 0x5C:  // Key-Off
                    keyOff();
                    break;

                case 0x6C:  // DSP Flags
                    // check the RESET flag
                    _resetFlag = (value & 0x80) != 0;
                    _muteFlag = (value & 0x40) != 0;
                    _echoEnabled = (value & 0x20) == 0;   // echo is enabled when the bit is CLEAR
                    _noiseClock = value & 0x1F;

                    if (prev != value) logToConsole(string.Format("Change to FLG register -> {0:X2}", value));
                    break;

                case 0x7C:  // ENDX
                    // any write to this register will clear ALL bits, regardless of whatever the write value is
                    _regs[0x7C] = 0;
                    break;

                case 0x0D:  // Echo Feedback
                    if (prev != value) logToConsole(string.Format("Change to EFB register -> {0:X2}", value));
                    _echoFeedback = (sbyte)value;
                    break;

                case 0x2D:  // Pitch Modulation
                    for (int i = 0, m = 1; i < 8; i++, m <<= 1)
                    {
                        prevFlag = _voices[i].PitchModEnabled;
                        _voices[i].PitchModEnabled = (value & m) != 0;
                        // if the value changed, log it
                        if ((prevFlag != _voices[i].EchoEnabled) && _enableHeavyLogging)
                            _eventLog.LogChannelPitchModChange(_totalCycles, i, _voices[i].PitchModEnabled);
                    }
                    if (prev != value) logToConsole(string.Format("Change to PMON register -> {0:X2}", value));
                    break;

                case 0x3D:  // Noise Enable
                    for (int i = 0, m = 1; i < 8; i++, m <<= 1)
                    {
                        prevFlag = _voices[i].NoiseEnabled;
                        _voices[i].NoiseEnabled = (value & m) != 0;
                        // if the value changed, log it
                        if ((prevFlag != _voices[i].EchoEnabled) && _enableHeavyLogging)
                            _eventLog.LogChannelNoiseChange(_totalCycles, i, _voices[i].NoiseEnabled);
                    }
                    //if (prev != value) logToConsole(string.Format("Change to NON register -> {0:X2}", value));
                    break;

                case 0x4D:  // Echo Enable
                    for (int i = 0, m = 1; i < 8; i++, m <<= 1)
                    {
                        prevFlag = _voices[i].EchoEnabled;
                        _voices[i].EchoEnabled = (value & m) != 0;
                        // if the value changed, log it
                        if ((prevFlag != _voices[i].EchoEnabled) && _enableHeavyLogging)
                            _eventLog.LogChannelEchoChange(_totalCycles, i, _voices[i].EchoEnabled);
                    }
                    //if (prev != value) logToConsole(string.Format("Change to EON register -> {0:X2}", value));
                    break;

                case 0x5D:  // DIR
                    throw new Exception("Write to Source Directory register during playback");

                case 0x6D:  // ESA (Echo Buffer Start Offset)
                    if (prev != value) logToConsole(string.Format("Change to ESA register -> {0:X2}", value));
                    _echoBufferPointer = value << 8;
                    // move the echo buffer pointer so it's at the start of the echo buffer
                    _echoBufferOffset = 0;
                    break;

                case 0x7D:  // EDL
                    if (prev != value) logToConsole(string.Format("Change to EDL register -> {0:X2}", value));
                    _echoDelay = value & 0xF;
                    // TODO: move the echo buffer pointer so it falls within the valid range defined by EDL and ESA
                    break;

                default:
                    throw new Exception(string.Format("Write to unhandled DSP register {0:X2}", register));
            }
        }

        public void SetVoiceMuted(int index, bool muted)
        {
            _voices[index].IsMuted = muted;
        }

        /// <summary>
        /// Dumps the contents of the advanced event log to the console and also copies it to the clipboard.
        /// </summary>
        public void DumpEventLog()
        {
            if (_enableHeavyLogging)
            {
                string log = _eventLog.GenerateLogString();

                // dump it to the console and copy to the clipboard
                Console.WriteLine(log);
                Clipboard.SetText(log);
            }
        }

        /// <summary>
        /// Gets the contents of a DSP register without invoking any of the logic in ReadRegister.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public byte GetRegisterDirect(int index)
        {
            return _regs[index];
        }

        /// <summary>
        /// Gets one of the SPCVoices used by the DSP.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public SPCVoice Voices(int index)
        {
            // check arguments
            if (index < 0 || index >= 8) throw new ArgumentOutOfRangeException("index");

            return _voices[index];
        }

        /// <summary>
        /// Gets the Left channel value of a specific voice's VU meter.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public float GetVoiceMeterLeft(int index)
        {
            // check arguments
            if (index < 0 || index >= 8) throw new ArgumentOutOfRangeException("index");

            return _voiceMeters[index].Left;
        }

        /// <summary>
        /// Gets the Right channel value of a specific voice's VU meter.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public float GetVoiceMeterRight(int index)
        {
            // check arguments
            if (index < 0 || index >= 8) throw new ArgumentOutOfRangeException("index");

            return _voiceMeters[index].Right;
        }

        public float GetMainMeterLeft()
        {
            return _mainMeter.Left;
        }

        public float GetMainMeterRight()
        {
            return _mainMeter.Right;
        }

        internal EventLogger EventLog => _eventLog;

        public bool EnableHeavyLogging
        {
            get { return _enableHeavyLogging; }
            set { _enableHeavyLogging = value; }
        }

        /// <summary>
        /// Gets the total number of samples that have been generated by the DSP.
        /// </summary>
        public int TotalCycles => _totalCycles;
    }
}
