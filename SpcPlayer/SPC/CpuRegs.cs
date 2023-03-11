using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpcPlayer.SPC
{
    internal class CpuRegs
    {
        private const byte PSW_CARRY = 0x1;
        private const byte PSW_ZERO = 0x2;
        private const byte PSW_INTERRUPT = 0x4;
        private const byte PSW_HALFCARRY = 0x8;
        private const byte PSW_BREAK = 0x10;
        private const byte PSW_DIRECTPAGE = 0x20;
        private const byte PSW_OVERFLOW = 0x40;
        private const byte PSW_NEGATIVE = 0x80;

        private ushort _PC;
        private byte _A;
        private byte _X;
        private byte _Y;
        private byte _SP;

        // the individual flags
        private bool _pswCarry;
        private bool _pswZero;
        private bool _pswInterrupt;
        private bool _pswHalfCarry;
        private bool _pswBreak;
        private bool _pswDirectPage;
        private bool _pswOverflow;
        private bool _pswNegative;

        public ushort PC
        {
            get { return _PC; }
            set { _PC = value; }
        }
        public byte A
        {
            get { return _A; }
            set { _A = value; }
        }
        public byte X
        {
            get { return _X; }
            set { _X = value; }
        }
        public byte Y
        {
            get { return _Y; }
            set { _Y = value; }
        }
        public byte SP
        {
            get { return _SP; }
            set { _SP = value; }
        }
        public byte PSW
        {
            get
            {
                // build a new value and return that
                int res = 0;

                res |= _pswCarry ? PSW_CARRY : 0;
                res |= _pswZero ? PSW_ZERO : 0;
                res |= _pswInterrupt ? PSW_INTERRUPT : 0;
                res |= _pswHalfCarry ? PSW_HALFCARRY : 0;
                res |= _pswBreak ? PSW_BREAK : 0;
                res |= _pswDirectPage ? PSW_DIRECTPAGE : 0;
                res |= _pswOverflow ? PSW_OVERFLOW : 0;
                res |= _pswNegative ? PSW_NEGATIVE : 0;

                return (byte)res;
            }
            set
            {
                // get the flag values from the passed value
                _pswCarry = (value & PSW_CARRY) != 0;
                _pswZero = (value & PSW_ZERO) != 0;
                _pswInterrupt = (value & PSW_INTERRUPT) != 0;
                _pswHalfCarry = (value & PSW_HALFCARRY) != 0;
                _pswBreak = (value & PSW_BREAK) != 0;
                _pswDirectPage = (value & PSW_DIRECTPAGE) != 0;
                _pswOverflow = (value & PSW_OVERFLOW) != 0;
                _pswNegative = (value & PSW_NEGATIVE) != 0;
            }
        }
        public ushort YA
        {
            get
            {
                return (ushort)(_A | (_Y << 8));
            }
            set
            {
                _A = (byte)(value & 0xFF);
                _Y = (byte)(value >> 8);
            }
        }

        /// <summary>
        /// Sets or clears flag N based on the given value
        /// </summary>
        /// <param name="value"></param>
        public void StoreFlagN(byte value)
        {
            // the flag is set to the most significant bit of the value
            _pswNegative = (value & 0x80) != 0 ? true : false;
        }

        public void StoreFlagN(ushort value)
        {
            // the flag is set to the most significant bit of the value
            _pswNegative = (value & 0x8000) != 0 ? true : false;
        }

        public void StoreFlagV(int value)
        {
            // set if the value is non-zero, clear otherwise
            _pswOverflow = value != 0 ? true : false;
        }

        public void StoreFlagH(int value)
        {
            _pswHalfCarry = value != 0 ? true : false;
        }

        /// <summary>
        /// Sets or clears flags N and Z based on the given value
        /// </summary>
        /// <param name="value"></param>
        public void StoreFlagsNZ(byte value)
        {
            _pswNegative = (value & 0x80) != 0 ? true : false;
            _pswZero = value == 0 ? true : false;
        }

        /// <summary>
        /// Sets or clears flags N and Z based on the given value
        /// </summary>
        /// <param name="value"></param>
        public void StoreFlagsNZ(ushort value)
        {
            _pswNegative = (value & 0x8000) != 0 ? true : false;
            _pswZero = value == 0 ? true : false;
        }

        public bool FlagC
        {
            get { return _pswCarry; }
            set { _pswCarry = value; }
        }
        public bool FlagZ
        {
            get { return _pswZero; }
            set { _pswZero = value; }
        }
        public bool FlagI
        {
            get { return _pswInterrupt; }
            set { _pswInterrupt = value; }
        }
        public bool FlagH
        {
            get { return _pswHalfCarry; }
            set { _pswHalfCarry = value; }
        }
        public bool FlagB
        {
            get { return _pswBreak; }
            set { _pswBreak = value; }
        }
        public bool FlagP
        {
            get { return _pswDirectPage; }
            set { _pswDirectPage = value; }
        }
        public bool FlagV
        {
            get { return _pswOverflow; }
            set { _pswOverflow = value; }
        }
        public bool FlagN
        {
            get { return _pswNegative; }
            set { _pswNegative = value; }
        }


    }
}
