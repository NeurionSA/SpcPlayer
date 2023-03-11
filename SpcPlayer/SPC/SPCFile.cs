using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpcPlayer.SPC
{
    internal class SPCFile
    {
        // the SPC700 register initial states
        private ushort _regPC;
        private byte _regA;
        private byte _regX;
        private byte _regY;
        private byte _regPSW;   // flags register
        private byte _regSP;

        // the SPC700's RAM
        private byte[]? _ram;

        // the state of the DSP registers
        private byte[]? _dspRegisters;

        // the "extra RAM"
        private byte[]? _extraRam;

        // ID666 tag information
        private string? _songTitle;
        private string? _gameTitle;
        private string? _dumperName;
        private string? _comments;
        private string? _dumpDate;
        private int _secondsBeforeFade;     // seconds to play song before fading out
        private int _fadeLength;            // length of fade in milliseconds
        private string? _artist;
        private bool _defaultChannelDisables;   // Not sure what this means
        private byte _sourceEmulator;

        private void createFromStream(Stream stream)
        {
            // check arguments
            ArgumentNullException.ThrowIfNull(stream);

            BinaryReader br = new BinaryReader(stream);
            long startPosition = stream.Position;

            // check the header
            string strScratch = Encoding.ASCII.GetString(br.ReadBytes(33));
            if (!strScratch.Equals("SNES-SPC700 Sound File Data v0.30")) throw new InvalidDataException();
            // check the next 2 bytes, which are expected to be 0x1A1A
            if (br.ReadUInt16() != 0x1A1A) throw new InvalidDataException();

            bool containsID666;

            // the next byte determines if there's ID666 info in the header
            switch (br.ReadByte())
            {
                case 26:
                    // contains ID666
                    containsID666 = true;
                    break;

                case 27:
                    // does not contain ID666 tag
                    containsID666 = false;
                    break;

                default:
                    throw new InvalidDataException();
            }
            // read in the minor version
            br.ReadByte();

            // read in the SPC700 registers
            _regPC = br.ReadUInt16();
            _regA = br.ReadByte();
            _regX = br.ReadByte();
            _regY = br.ReadByte();
            _regPSW = br.ReadByte();
            _regSP = br.ReadByte();
            // reserved
            br.ReadUInt16();

            if (containsID666)
            {
                // read in the ID666 tag, assume its in Text format
                _songTitle = StringHelper.TrimNull(Encoding.ASCII.GetString(br.ReadBytes(32)));
                _gameTitle = StringHelper.TrimNull(Encoding.ASCII.GetString(br.ReadBytes(32)));
                _dumperName = StringHelper.TrimNull(Encoding.ASCII.GetString(br.ReadBytes(16)));
                _comments = StringHelper.TrimNull(Encoding.ASCII.GetString(br.ReadBytes(32)));
                _dumpDate = StringHelper.TrimNull(Encoding.ASCII.GetString(br.ReadBytes(11)));
                strScratch = StringHelper.TrimNull(Encoding.ASCII.GetString(br.ReadBytes(3)));
                _secondsBeforeFade = int.Parse(strScratch);
                strScratch = StringHelper.TrimNull(Encoding.ASCII.GetString(br.ReadBytes(5)));
                _fadeLength = int.Parse(strScratch);
                _artist = StringHelper.TrimNull(Encoding.ASCII.GetString(br.ReadBytes(32)));
                switch (br.ReadByte())
                {
                    case 0:
                        _defaultChannelDisables = true;
                        break;

                    case 1:
                        _defaultChannelDisables = false;
                        break;

                    default:
                        throw new InvalidDataException();
                }
                _sourceEmulator = br.ReadByte();
                // read past the reserved bytes
                br.ReadBytes(45);
            }

            // read in the RAM
            _ram = br.ReadBytes(65536);
            // read in the DSP registers
            _dspRegisters = br.ReadBytes(128);
            // read past unused bytes
            br.ReadBytes(64);
            // read in the 'Extra RAM'
            _extraRam = br.ReadBytes(64);

            // TODO: check for and read in Extended ID666 information
            // see https://ocremix.org/info/SPC_Format_Specification
        }

        /// <summary>
        /// Creates a new instance of the class from the specified file.
        /// </summary>
        /// <param name="filename"></param>
        public SPCFile(string filename)
        {
            // check arguments
            ArgumentNullException.ThrowIfNull(filename);
            // make sure the file exists
            FileInfo fileInfo = new FileInfo(filename);
            if (!fileInfo.Exists) throw new FileNotFoundException(filename);

            // attempt to load the file
            FileStream fStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);

            createFromStream(fStream);

            // close the file stream
            fStream.Close();
        }

        public ushort RegisterPC => _regPC;
        public byte RegisterA => _regA;
        public byte RegisterX => _regX;
        public byte RegisterY => _regY;
        public byte RegisterPSW => _regPSW;
        public byte RegisterSP => _regSP;

        /// <summary>
        /// Gets a copy of the SPC700's RAM as an array of bytes.
        /// </summary>
        public byte[] RAM
        {
            get
            {
                return (byte[])_ram!.Clone();
            }
        }

        public byte[] DSP
        {
            get
            {
                return (byte[])_dspRegisters!.Clone();
            }
        }

        /// <summary>
        /// Gets a copy of the Extra RAM as an array of bytes.
        /// </summary>
        public byte[] ExtraRAM
        {
            get
            {
                return (byte[])_extraRam!.Clone();
            }
        }

        public string SongTitle => _songTitle!;
        public string GameTitle => _gameTitle!;
        public string DumperName => _dumperName!;
        public string Comments => _comments!;
        public string DumpDate => _dumpDate!;
        public int SecondsBeforeFade => _secondsBeforeFade;
        public int FadeLength => _fadeLength;
        public string Artist => _artist!;
        public bool DefaultChannelDisables => _defaultChannelDisables;
        public byte SourceEmulator => _sourceEmulator;
    }
}
