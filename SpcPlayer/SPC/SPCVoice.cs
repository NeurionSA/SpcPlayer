using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpcPlayer.SPC
{
    /// <summary>
    /// Class for tracking the states of the DSP's voices.
    /// </summary>
    internal class SPCVoice
    {
        private VoiceStatus _status;   // voice's current status

        // derived from regs $x0, $x1 (VOL)
        private int _volumeLeft;          // voice's left volume
        private int _volumeRight;         // voice's right volume

        // derived from regs $x2, $x3 (P)
        private int _pitch;                 // voice's pitch setting
        private int _rate;                  // sample playback rate in Hz

        private int _interpolationIndex;    // voice's interpolation index (distance between discrete samples, with 0x1000 being the next discrete sample)

        // derived from regs $x4 (SRCN)
        private int _sourceIndex;           // source index for the voice
        private int _sourceSampleStart;     // pointer into memory to the first BRR block for the source
        private int _sourceSampleLoopStart; // pointer into memory of the BRR block to return to when the voice loops

        // derived from regs $x5, $x6 (ADSR)
        private bool _enableADSR;           // whether to use the ADSR envelope (true) or GAIN (false)
        private int _attackRate;
        private int _decayRate;
        private int _sustainLevel;
        private int _sustainRate;

        // derived from regs $x7 (GAIN)
        private GainMode _gainMode;
        private int _gainParameter;

        // derived from regs $x8 (ENVX)
        private int _envelope;
        private int _envelopeCycle;     // cycle reference for envelope functions

        // derived from regs $x9 (OUTX)
        private int _outx;

        // derived from global reg $2D (PMON)
        private bool _pitchModEnabled;

        // derived from global reg $3D (NON)
        private bool _noiseEnabled;

        // derived from global reg $4D (EON)
        private bool _echoEnabled;

        // fields involved with sample decoding
        private int _brrBlockPointer;          // pointer into memory of the current block that is being decoded
        private int _brrDecodeOffset;          // the current position within the block in bytes
                                               // ring buffer that BRR samples are decoded into, 12 samples long
        private short[] _brrDecodeBuffer = new short[12];
        private byte _brrBlockHeader;         // the header byte of the block that is being decoded
        private int _brrBufferReadPosition;   // the current read position within the decode ring buffer
        private int _brrBufferWritePosition;  // the current write position within the decode ring buffer

        // allows the user to mute the channel on demand
        private bool _isMuted = false;

        public SPCVoice()
        {

        }

        private void reset()
        {
            // reset the status, envelope, and VU meter stuff
            _status = VoiceStatus.Release;
            _envelope = 0;
        }

        /// <summary>
        /// Resets the state of the voice.
        /// </summary>
        public void Reset()
        {
            reset();
        }

        /// <summary>
        /// Gets whether the voice is in an active state (is not in Release phase with an envelope of 0)
        /// </summary>
        public bool IsActive => !((_status == VoiceStatus.Release) && (_envelope == 0));

        internal VoiceStatus Status { get => _status; set => _status = value; }

        internal int VolumeLeft { get => _volumeLeft; set => _volumeLeft = value; }
        internal int VolumeRight { get => _volumeRight; set => _volumeRight = value; }

        internal int Pitch { get => _pitch; set => _pitch = value; }
        internal int Rate { get => _rate; set => _rate = value; }

        internal int InterpolationIndex { get => _interpolationIndex; set => _interpolationIndex = value; }

        internal int SourceIndex { get => _sourceIndex; set => _sourceIndex = value; }
        internal int SourceSampleStart { get => _sourceSampleStart; set => _sourceSampleStart = value; }
        internal int SourceSampleLoopStart { get => _sourceSampleLoopStart; set => _sourceSampleLoopStart = value; }
    
        internal bool EnableADSR { get => _enableADSR; set => _enableADSR = value; }
        internal int AttackRate { get => _attackRate; set => _attackRate = value; }
        internal int DecayRate { get => _decayRate; set => _decayRate = value; }
        internal int SustainLevel { get => _sustainLevel; set => _sustainLevel = value; }
        internal int SustainRate { get => _sustainRate; set => _sustainRate = value; }

        internal GainMode GainMode { get => _gainMode; set => _gainMode = value; }
        internal int GainParameter { get => _gainParameter; set => _gainParameter = value; }

        internal int Envelope { get => _envelope; set => _envelope = value; }
        internal int EnvelopeCycle { get => _envelopeCycle; set => _envelopeCycle = value; }

        internal int OutX { get => _outx; set => _outx = value; }

        internal bool PitchModEnabled { get => _pitchModEnabled; set => _pitchModEnabled = value; }

        internal bool NoiseEnabled { get => _noiseEnabled; set => _noiseEnabled = value; }

        internal bool EchoEnabled { get => _echoEnabled; set => _echoEnabled = value; }

        /// <summary>
        /// Pointer into memory of the current block that is being decoded.
        /// </summary>
        internal int BrrBlockPointer { get => _brrBlockPointer; set => _brrBlockPointer = value; }
        /// <summary>
        /// The position within the current block in bytes.
        /// </summary>
        internal int BrrDecodeOffset { get => _brrDecodeOffset; set => _brrDecodeOffset = value; }
        /// <summary>
        /// Ring buffer that BRR samples are decoded into. 12 samples long.
        /// </summary>
        internal short[] BrrDecodeBuffer { get => _brrDecodeBuffer; }
        /// <summary>
        /// Header byte of the block that is being decoded.
        /// </summary>
        internal byte BrrBlockHeader { get => _brrBlockHeader; set => _brrBlockHeader = value; }
        /// <summary>
        /// The current read position within the decode ring buffer.
        /// </summary>
        internal int BrrBufferReadPosition { get => _brrBufferReadPosition; set => _brrBufferReadPosition = value; }
        /// <summary>
        /// The current write position within the decode ring buffer.
        /// </summary>
        internal int BrrBufferWritePosition { get => _brrBufferWritePosition; set => _brrBufferWritePosition = value; }

        /// <summary>
        /// Whether or not the voice's output has been muted by the user.
        /// </summary>
        public bool IsMuted { get => _isMuted; set => _isMuted = value; }

        
    }
}
