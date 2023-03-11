#define SPCTOOL     // mimics behavior of SPCTool, even though it may not be accurate

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpcPlayer.SPC
{
    internal class SPCCore
    {
        // the clock speed of the SPC CPU, in Hz
        private const int SPC_CLOCK = 1024000;

        // the SPC file to source the core's initial state from
        private SPCFile? _sourceFile;

        // the DSP for the SPC
        private DSP _dsp;
        // timer for generating DSP samples (32,000 Hz)
        private int _dspSampleTimer;

        // the SPC700's CPU registers
        private CpuRegs _regs = new CpuRegs();

        // whether the timers are enabled
        private bool[] _timerEnabled = { false, false, false };
        // the frequency divider counters for the timers, as they run at even divisions of the CPU clock
        private int[] _timerFreqCounters = { 0, 0, 0 };
        private int[] _timerFreqDividers = { 128, 128, 16 };
        // the targets for the internal counters, from 0xFA - 0xFC
        private int[] _timerTargets = { 256, 256, 256 };
        // the internal counters for the 3 timers
        private int[] _timerCounters = { 0, 0, 0 };
        // total clock cycles, for debug and syncing 
        private long _totalClockCycles;

        // the SPC700's 64 kB memory space
        private byte[] _mem = new byte[65536];

        /// <summary>
        /// Creates a new instance of this class, with the desired sample rate for audio output.
        /// </summary>
        /// <param name="sampleRate"></param>
        public SPCCore(int sampleRate)
        {
            // check arguments
            if (sampleRate < 1) throw new ArgumentOutOfRangeException("sampleRate");

            // create the DSP
            _dsp = new DSP(this, sampleRate);
        }

        /// <summary>
        /// Sets the core's state to that of the specified SPC file.
        /// </summary>
        /// <param name="spcFile"></param>
        public void LoadStateFromFile(SPCFile spcFile)
        {
            // check arguments
            ArgumentNullException.ThrowIfNull(spcFile);
            // set the source file
            _sourceFile = spcFile;
            reset();
        }

        // resets the state of the core to the initial state of the source file
        private void reset()
        {
            // copy the RAM state from the source
            Array.Copy(_sourceFile!.RAM, _mem, 65536);
            // copy the register states
            _regs.PC = _sourceFile.RegisterPC;
            _regs.A = _sourceFile.RegisterA;
            _regs.X = _sourceFile.RegisterX;
            _regs.Y = _sourceFile.RegisterY;
            _regs.SP = _sourceFile.RegisterSP;
            _regs.PSW = _sourceFile.RegisterPSW;
            // copy the DSP state
            _dsp.LoadStateFromFile(_sourceFile);
            _dspSampleTimer = 0;

            // reset the timer counters and load their initial state
            for (int i = 0; i < 3; i++)
            {
                _timerEnabled[i] = (_mem[0xF1] & (1 << i)) != 0 ? true : false;
                _timerFreqCounters[i] = 0;
                _timerCounters[i] = 0;
                _timerTargets[i] = _mem[0xFA + i] != 0 ? _mem[0xFA + i] : 256;
            }

            _totalClockCycles = 0;
        }

        /// <summary>
        /// Reads a byte from the SPC700 memory map.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private byte readByte(int address)
        {
            byte ret;

            // mask address to 16 bits; I'm not sure if instructions should be able to wrap in the memory space,
            // but this will enable it. It's a problem I've only seen so far with Doom
            address &= 0xFFFF;

            if ((address >= 0xF0) && (address <= 0xFF))
            {
                // SPC700 registers, handle the read
                switch (address)
                {
                    case 0xF2:  // DSP Register Address
                        // nothing special happens, just return the value that's in memory
                        return _mem![address];

                    case 0xF3:  // DSP Data Address
                        // read a register from the DSP
                        return _dsp.ReadRegister(_mem![0xF2]);

                    case 0xF4:  // Port 0
                    case 0xF5:  // Port 1
                    case 0xF6:  // Port 2
                    case 0xF7:  // Port 3
                        // These are the ports used to communicate between the SNES and the SPC700
                        // Since we're not doing anything with that, just return the bytes that are already present in memory
                        return _mem![address];

                    case 0xFD:  // Timer 0 Counter
                    case 0xFE:  // Timer 1 Counter
                    case 0xFF:  // Timer 2 Counter
                        // read the value from the overflow counter and reset it to 0
                        ret = _mem![address];
                        _mem[address] = 0;
                        return ret;

                    default:
                        throw new NotImplementedException(string.Format("Unhandled read from SPC700 register {0:X}", address));
                }
            }
            else if ((address >= 0xFFC0) && (address <= 0xFFFF))
            {
                // this behaviour is not implemented yet
                // https://wiki.superfamicom.org/spc700-reference#spc-memory-map-and-registers-18
                //throw new NotImplementedException("Unimplemented read from IPL ROM area");

                // allow the read for now, this happens in Doom
                return _mem![address];
            }

            return _mem![address];
        }

        /// <summary>
        /// Writes a byte into the SPC700 memory map.
        /// </summary>
        /// <param name="address"></param>
        /// <param name="value"></param>
        private void writeByte(int address, byte value)
        {
            // TODO: add support for break on write

            // handle writes that go to special locations
            if ((address >= 0xF0) && (address <= 0xFF))
            {
                // SPC700 registers, handle the write
                switch (address)
                {
                    case 0xF1:  // Control Register
                        // react to the bits that were written
                        // if bit 5 is set
                        if ((value & 0x20) != 0)
                        {
                            // reset the input from ports 2 & 3 to 0
                            _mem![0xF6] = 0;
                            _mem[0xF7] = 0;
                        }
                        // if bit 4 is set
                        if ((value & 0x10) != 0)
                        {
                            // reset the input from ports 0 & 1 to 0
                            _mem![0xF4] = 0;
                            _mem[0xF5] = 0;
                        }
                        // restart timers that are enabled
                        for (int i = 0; i < 3; i++)
                        {
                            if ((value & (1 << i)) != 0)
                            {
                                // timer is enabled
                                _timerEnabled[i] = true;
                                // SPC Tool does not reset these, but they SHOULD be reset, I'm pretty sure
#if (!SPCTOOL)
                                _timerCounters[i] = 0;
                                _timerFreqCounters[i] = 0;
#endif
                            }
                            else
                            {
                                // timer is now disabled
                                _timerEnabled[i] = false;
                            }
                        }

                        // set the memory address like normal
                        _mem![address] = value;
                        break;

                    case 0xF2:  // DSP Register Address
                        // write it to the memory location as normal, we'll use that when actually reading/writing DSP registers
                        _mem![address] = value;
                        break;

                    case 0xF3:  // DSP Register Data
                        // write to the DSP
                        _dsp.WriteRegister(_mem![0xF2], value);
                        break;

                    case 0xF4:  // Port 0
                    case 0xF5:  // Port 1
                    case 0xF6:  // Port 2
                    case 0xF7:  // Port 3
                        // These are the ports used to communicate between the SNES and the SPC700
                        // Since we're not doing anything with that, we'll do nothing
                        break;

                    case 0xFA:  // Timer 0
                    case 0xFB:  // Timer 1
                    case 0xFC:  // Timer 2
                        // uh, I'm not exactly sure if we should do anything if the new timer target is less than the timer's
                        // current internal counter, for now I'll just leave it alone
                        _timerTargets[address - 0xFA] = value != 0 ? value : 256;
                        // write the value to memory as normal
                        _mem![address] = value;
                        break;

                    default:
                        throw new NotImplementedException(string.Format("Unhandled write to SPC700 register {0:X}", address ));
                }
            }
            else if ((address >= 0xFFC0) && (address <= 0xFFFF))
            {
                //throw new NotImplementedException("Unimplemented write to IPL ROM area");

                // allow the write to occur -- this is used by Clayfighter
                _mem![address] = value;
            }
            else
            {
                // normal write to memory space
                _mem![address] = value;
            }
        }

        /// <summary>
        /// Pushes a byte onto the stack
        /// </summary>
        /// <param name="value"></param>
        private void pushByte(byte value)
        {
            _mem![_regs.SP-- + 0x100] = value;
        }

        /// <summary>
        /// Pops a byte off the stack
        /// </summary>
        /// <returns></returns>
        private byte popByte()
        {
            return _mem![++_regs.SP + 0x100];
        }

        // handles the advancement of timers by simulating a number of CPU clock cycles
        private void tickCycles(int count)
        {
            _totalClockCycles += count;
            _dspSampleTimer += count;
            // frequency divider for the DSP's sample generation is 32
            if (_dspSampleTimer >= 32)
            {
                _dsp.GenerateSamples(_dspSampleTimer / 32);
                _dspSampleTimer %= 32;
            }

            // iterate through the 3 timers and handle logic if enabled
            // timers 0 and 1 increment every 128 CPU cycles
            // timer 2 increments every 16 CPU cycles
            for (int i = 0; i < 3; i++)
            {
                if (_timerEnabled[i])
                {
                    _timerFreqCounters[i] += count;
                    if (_timerFreqCounters[i] >= _timerFreqDividers[i])
                    {
                        // tick the timer
                        _timerFreqCounters[i] %= _timerFreqDividers[i];
                        _timerCounters[i]++;

                        // compare it to the corresponding byte in memory
                        if (_timerCounters[i] >= _timerTargets[i])
                        {
                            // reset the counter and increment the overflow counter, wrapping it within the range of 0 - 15
                            _timerCounters[i] -= _timerTargets[i];
                            _mem![0xFD + i] = (byte)((_mem[0xFD + i] + 1) & 0xF);
                        }
                    }
                }
            }
        }

        private int getDirectPageAddress(byte lowbyte)
        {
            return lowbyte + (_regs.FlagP ? 0x100 : 0 );
        }

        private byte op_adc(byte r1, byte r2)
        {
            // perform the addition and include the carry
            int res = r1 + r2 + (_regs.FlagC ? 1 : 0);

            // set the flag results
            _regs.FlagH = ((r1 & 0x0F) + (r2 & 0x0F) + (_regs.FlagC ? 1 : 0)) > 0x0F ? true : false;
            _regs.FlagC = res > 0xFF ? true : false;
            _regs.StoreFlagV(~(r1 ^ r2) & (r1 ^ res) & 0x80);
            _regs.StoreFlagsNZ((byte)res);

            return (byte)res;
        }

        private byte op_sbc(byte r1, byte r2)
        {
            // r1 = r1 - r2 - !C
            uint res = (uint)(r1 - r2 - (_regs.FlagC ? 0 : 1));

            // set the flag results
            _regs.FlagH = (r1 & 0xF) - (r2 & 0xF) - (_regs.FlagC ? 0 : 1) > 0xF ? false : true;
            _regs.FlagC = res <= 0xFF ? true : false;
            _regs.StoreFlagV((int)((r1 ^ r2) & (r1 ^ res) & 0x80));
            _regs.StoreFlagsNZ((byte)res);

            return (byte)res;
        }

        private ushort op_addw(ushort r1, ushort r2)
        {
            uint res, res_low, carry_low, res_high;

            // add together the low bytes
            res_low = (uint)((r1 & 0xFF) + (r2 & 0xFF));
            // determine if there's a carry from the low bytes
            carry_low = (uint)(res_low > 0xFF ? 1 : 0);

            // set the halfcarry flag
            // NOTE: this may in fact be incorrect, I am not 100% sure how the 16 bit ops affect the half-carry flag
            _regs.FlagH = (((r1 >> 8) & 0xF) + ((r2 >> 8) & 0xF) + carry_low) > 0x0F ? true : false;

            // add the high bytes together, including the carry from the low bytes
            res_high = (uint)((r1 >> 8) + (r2 >> 8) + carry_low);
            // set the carry flag
            _regs.FlagC = res_high > 0xFF ? true : false;
            // generate the actual result
            res = (res_low & 0xFF) + (res_high << 8);

            // do the rest of the flags
            _regs.StoreFlagV((int)(~(r1 ^ r2) & (r1 ^ res) & 0x8000));
            _regs.StoreFlagsNZ((ushort)res);

            return (ushort)res;
        }

        private ushort op_subw(ushort r1, ushort r2)
        {
            uint res, res_low, carry_low, res_high;

            // do the low bytes
            res_low = (uint)((r1 & 0xFF) - (r2 & 0xFF));
            // determine the carry from the low bytes
            carry_low = (uint)(res_low > 0xFF ? 1 : 0);

            // set the halfcarry flag
            // NOTE: this may in fact be incorrect, I am not 100% sure how the 16 bit ops affect the half-carry flag
            _regs.FlagH = ((r1 >> 8) & 0xF) - ((r2 >> 8) & 0xF) - carry_low > 0xF ? false : true;

            res_high = (uint)((r1 >> 8) - (r2 >> 8) - carry_low);
            // set the carry flag
            _regs.FlagC = res_high <= 0xFF ? true : false;
            // generate the actual result
            res = (res_low & 0xFF) + (res_high << 8);

            // do the rest of the flags
            _regs.StoreFlagV((int)((r1 ^ r2) & ((r1 ^ res) & 0x8000) >> 8));
            _regs.StoreFlagsNZ((ushort)res);

            return (ushort)res;
        }

        private byte op_and(byte r1, byte r2)
        {
            byte res = (byte)(r1 & r2);
            _regs.StoreFlagsNZ(res);

            return res;
        }

        private byte op_asl(byte value)
        {
            // set the carry flag to the uppermost bit of the value
            _regs.FlagC = (value & 0x80) != 0 ? true : false;
            byte res = (byte)(value << 1);
            // set the 2 other flags
            _regs.StoreFlagsNZ(res);

            return res;
        }

        private void op_div()
        {
            // I'm dumb so to make sure I got this right I basically copied the implementation from here:
            // https://github.com/uyjulian/spc2it/blob/master/spc700.c
            uint yva, work_x, i;
            yva = _regs.YA;
            work_x = (uint)(_regs.X << 9);

            _regs.FlagH = (_regs.X & 0xF) <= (_regs.Y & 0xF);

            for (i = 0; i < 9; i++)
            {
                yva <<= 1;
                if ((yva & 0x20000) != 0)
                    yva = (yva & 0x1FFFF) | 1;
                if (yva >= work_x)
                    yva ^= 1;
                if ((yva & 1) != 0)
                    yva = (yva - work_x) & 0x1FFFF;
            }

            _regs.FlagV = (yva & 0x100) != 0;
            _regs.YA = (ushort)((((yva >> 9) & 0xFF) << 8) + (yva & 0xFF));
            _regs.StoreFlagsNZ(_regs.YA);
        }

        private byte op_or(byte r1, byte r2)
        {
            byte res = (byte)(r1 | r2);
            _regs.StoreFlagsNZ(res);

            return res;
        }

        private byte op_ror(byte value)
        {
            // shift the value right by 1 and set the uppermost bit to the old C flag
            byte res = (byte)((value >> 1) | ((_regs.FlagC ? 1 : 0) << 7));
            // set the C flag to the lowest bit of the value
            _regs.FlagC = (value & 0x1) != 0 ? true : false;
            // set the other 2 flags
            _regs.StoreFlagsNZ(res);
            
            return res;
        }

        private byte op_rol(byte value)
        {
            // get the old C flag
            int oldC = _regs.FlagC ? 1 : 0;
            // set C to the uppermost bit of the value
            _regs.FlagC = (value & 0x80) != 0 ? true : false;
            // shift the value left by 1 and set the lowermost bit to
            byte res = (byte)((value << 1) + oldC);
            // set the other 2 flags
            _regs.StoreFlagsNZ(res);

            return res;
        }

        private byte op_inc(byte value)
        {
            byte res = (byte)(value + 1);
            _regs.StoreFlagsNZ(res);

            return res;
        }

        private byte op_dec(byte value)
        {
            byte res = (byte)(value - 1);
            _regs.StoreFlagsNZ(res);

            return res;
        }

        private byte op_lsr(byte value)
        {
            byte res = (byte)(value >> 1);
            // set the C flag to the lowest bit of the initial value
            _regs.FlagC = (value & 1) != 0 ? true : false;
            // set the other 2 flags
            _regs.StoreFlagsNZ(res);

            // return the shifted value
            return res;
        }

        private void op_cmp(byte r1, byte r2)
        {
            // set the N, Z, and C flags appropriately for the comparison
            uint temp = (uint)(r1 - r2);

            _regs.FlagC = temp <= 0xFF ? true : false;
            _regs.StoreFlagsNZ((byte)temp);
        }

        private byte op_eor(byte r1, byte r2)
        {
            byte res = (byte)(r1 ^ r2);
            _regs.StoreFlagsNZ(res);

            return res;
        }

        /// <summary>
        /// Executes the instruction at the Program Counter
        /// </summary>
        private void executeInstruction()
        {
            // fetch the opcode byte
            byte op = _mem![_regs.PC];
            int address, address2;
            byte data;
            ushort data16;
            uint temp;
            int cycleCount = 0; // the number of SPC700 clock cycles the instruction took to execute
            byte mask;

            // handle the opcode
            switch (op)
            {
                case 0x0:   // NOP                  2 cycles
                    _regs.PC++;
                    cycleCount = 2;
                    break;

                case 0x01:  // TCALL n              8 cycles
                case 0x11:
                case 0x21:
                case 0x31:
                case 0x41:
                case 0x51:
                case 0x61:
                case 0x71:
                case 0x81:
                case 0x91:
                case 0xA1:
                case 0xB1:
                case 0xC1:
                case 0xD1:
                case 0xE1:
                case 0xF1:
                    // call to address stored in high memory
                    // first compute the address used by the TCALL number
                    address = 0xFFC0 + ((15 - (op >> 4)) << 1);
                    // get the target address
                    address2 = _mem[address] + (_mem[address + 1] << 8);
                    // push return address onto the stack, high byte first
                    _regs.PC++;
                    pushByte((byte)(_regs.PC >> 8));
                    pushByte((byte)(_regs.PC & 0xFF));
                    // jump to the target address
                    _regs.PC = (ushort)address2;
                    cycleCount = 8;
                    break;

                case 0x02:  // SET1 d.n             4 cycles
                case 0x22:
                case 0x42:
                case 0x62:
                case 0x82:
                case 0xA2:
                case 0xC2:
                case 0xE2:
                    // set bit n in [d]
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    mask = (byte)(1 << (op >> 5));
                    writeByte(address, (byte)(readByte(address) | mask));
                    _regs.PC += 2;
                    cycleCount = 4;
                    break;

                case 0x03:  // BBS d.n, r           5 / 7 cycles
                case 0x23:
                case 0x43:
                case 0x63:
                case 0x83:
                case 0xA3:
                case 0xC3:
                case 0xE3:
                    // jump if bit n is set in [d]
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    mask = (byte)(1 << (op >> 5));
                    if ((readByte(address) & mask) != 0)
                    {
                        // bit is set, take the jump
                        _regs.PC = (ushort)(_regs.PC + 3 + (sbyte)_mem[_regs.PC + 2]);
                        cycleCount = 7;
                    }
                    else
                    {
                        // no jump
                        _regs.PC += 3;
                        cycleCount = 5;
                    }
                    break;

                case 0x4:   // OR A,[d]              3 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    _regs.A = op_or(_regs.A, readByte(address));
                    _regs.PC += 2;
                    cycleCount = 3;
                    break;

                case 0x5:   // OR A,[!a]            4 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8);
                    _regs.A = op_or(_regs.A, readByte(address));
                    _regs.PC += 3;
                    cycleCount = 4;
                    break;

                case 0x6:   // OR A,[X]             3 cycles
                    address = getDirectPageAddress(_regs.X);
                    _regs.A = op_or(_regs.A, readByte(address));
                    _regs.PC++;
                    cycleCount = 3;
                    break;

                case 0x7:   // OR A,[[d+X]]         6 cycles
                    address = getDirectPageAddress((byte)(_mem[_regs.PC + 1] + _regs.X));
                    address2 = readByte(address);
                    address2 += readByte(address + 1) << 8;
                    _regs.A = op_or(_regs.A, readByte(address2));
                    _regs.PC += 2;
                    cycleCount = 6;
                    break;

                case 0x8:   // OR A, #i             2 cycles
                    _regs.A = op_or(_regs.A, _mem[_regs.PC + 1]);
                    _regs.PC += 2;
                    cycleCount = 2;
                    break;

                case 0x9:   // OR [dd],[ds]         6 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 2]);
                    address2 = getDirectPageAddress(_mem[_regs.PC + 1]);
                    writeByte(address, op_or(readByte(address), readByte(address2)));
                    _regs.PC += 3;
                    cycleCount = 6;
                    break;

                case 0xB:   // ASL [d]              4 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    writeByte(address, op_asl(readByte(address)));
                    _regs.PC += 2;
                    cycleCount = 4;
                    break;

                case 0xC:   // ASL [!a]             5 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8);
                    writeByte(address, op_asl(readByte(address)));
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0xD:   // PUSH PSW             4 cycles
                    // push PSW onto the stack
                    pushByte(_regs.PSW);
                    _regs.PC++;
                    cycleCount = 4;
                    break;

                case 0xE:   // TSET1                6 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8);
                    // read the byte
                    mask = readByte(address);
                    // store flags of the byte & reg A
                    _regs.StoreFlagsNZ((byte)(mask & _regs.A));
                    // write out the byte ORed with A
                    writeByte(address, (byte)(mask | _regs.A));
                    _regs.PC += 3;
                    cycleCount = 6;
                    break;

                case 0xF:   // BRK                  8 cycles
                    // invokes TCALL 0
                    address2 = _mem[0xFFDE] + (_mem[0xFFDF] << 8);
                    // push return address onto the stack, high byte first
                    _regs.PC++;
                    pushByte((byte)(_regs.PC >> 8));
                    pushByte((byte)(_regs.PC & 0xFF));
                    // push the PSW onto the stack?
                    pushByte(_regs.PSW);
                    // jump to the target address
                    _regs.PC = (ushort)address2;
                    // Set Flag B, clear flag I
                    _regs.FlagB = true;
                    _regs.FlagI = false;
                    cycleCount = 8;
                    break;

                case 0x10:  // BPL r                2 / 4 cycles
                    // jump if flag N == 0
                    if (_regs.FlagN)
                    {
                        // no jump
                        _regs.PC += 2;
                        cycleCount = 2;
                    }
                    else
                    {
                        // jump
                        _regs.PC = (ushort)(_regs.PC + 2 + (sbyte)_mem[_regs.PC + 1]);
                        cycleCount = 4;
                    }
                    break;

                case 0x12:  // CLR1 d.n             4 cycles
                case 0x32:
                case 0x52:
                case 0x72:
                case 0x92:
                case 0xB2:
                case 0xD2:
                case 0xF2:
                    // clear bit n in [d]
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    mask = (byte)~(1 << (op >> 5));
                    writeByte(address, (byte)(readByte(address) & mask));
                    _regs.PC += 2;
                    cycleCount = 4;
                    break;

                case 0x13:  // BBC d.0, r           5 cycles / 7 cycles
                case 0x33:  // BBC d.1, r
                case 0x53:  // BBC d.2, r
                case 0x73:  // BBC d.3, r
                case 0x93:  // BBC d.4, r
                case 0xB3:  // BBC d.5, r
                case 0xD3:  // BBC d.6, r
                case 0xF3:  // BBC d.7, r
                    // jump if bit n is clear in [d]
                    // compute the test mask by getting the bit number from the opcode (it's the uppermost 3 bits)
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    mask = (byte)(1 << (op >> 5));
                    if ((readByte(address) & mask) == 0)
                    {
                        // bit is clear, take the jump
                        _regs.PC = (ushort)(_regs.PC + 3 + (sbyte)_mem[_regs.PC + 2]);
                        cycleCount = 7;
                    }
                    else
                    {
                        // no jump
                        _regs.PC += 3;
                        cycleCount = 5;
                    }
                    break;

                case 0x14:  // OR A,[d+X]           4 cycles
                    address = getDirectPageAddress((byte)(_mem[_regs.PC + 1] + _regs.X)) ;
                    _regs.A = op_or(_regs.A, readByte(address));
                    _regs.PC += 2;
                    cycleCount = 4;
                    break;

                case 0x15:  // OR A,[!a+X]          5 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8) + _regs.X;
                    _regs.A = op_or(_regs.A, readByte(address));
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0x16:  // OR A,[!a+Y]          5 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8) + _regs.Y;
                    _regs.A = op_or(_regs.A, readByte(address));
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0x17:  // OR A, [[d]+Y]        6 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    address2 = readByte(address);
                    address2 += (readByte(address + 1) << 8) + _regs.Y;
                    _regs.A = op_or(_regs.A, readByte(address2));
                    _regs.PC += 2;
                    cycleCount = 6;
                    break;

                case 0x18:  // OR [d],#i            5 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 2]);
                    writeByte(address, op_or(readByte(address), _mem[_regs.PC + 1]));
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0x19:  // OR [X],[Y]           5 cycles
                    address = getDirectPageAddress(_regs.X);
                    address2 = getDirectPageAddress(_regs.Y);
                    data = readByte(address2);
                    writeByte(address, op_or(readByte(address), data));
                    _regs.PC++;
                    cycleCount = 5;
                    break;

                case 0x1A:  // DECW [d]               6 cycles
                    // 16-bit decrement
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    data16 = (ushort)(readByte(address) + (readByte(address + 1) << 8));
                    data16--;
                    // set the flags appropriately
                    _regs.StoreFlagsNZ(data16);
                    // write the bytes back
                    writeByte(address, (byte)(data16 & 0xFF));
                    writeByte(address + 1, (byte)(data16 >> 8));

                    _regs.PC += 2;
                    cycleCount = 6;
                    break;

                case 0x1B:  // ASL [d+X]            5 cycles
                    address = getDirectPageAddress((byte)(_mem[_regs.PC + 1] + _regs.X));
                    writeByte(address, op_asl(readByte(address)));
                    _regs.PC += 2;
                    cycleCount = 5;
                    break;

                case 0x1C:  // ASL A                2 cycles
                    _regs.A = op_asl(_regs.A);
                    _regs.PC++;
                    cycleCount = 2;
                    break;

                case 0x1D:  // DEC X                2 cycles
                    _regs.X = op_dec(_regs.X);
                    _regs.PC++;
                    cycleCount = 2;
                    break;

                case 0x1E:  // CMP X,[!a]           4 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8);
                    op_cmp(_regs.X, readByte(address));
                    _regs.PC += 3;
                    cycleCount = 4;
                    break;

                case 0x1F:  // JMP [!a+X]          6 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8) + _regs.X;
                    address2 = readByte(address);
                    address2 += readByte(address + 1) << 8;
                    _regs.PC = (ushort)address2;
                    cycleCount = 6;
                    break;

                case 0x20:  // CLRP                 2 cycles
                    // clear DP
                    _regs.FlagP = false;
                    _regs.PC++;
                    cycleCount = 2;
                    break;

                case 0x24:  // AND A,[d]            3 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    _regs.A = op_and(_regs.A, readByte(address));
                    _regs.PC += 2;
                    cycleCount = 3;
                    break;

                case 0x25:  // AND A,[!a]           4 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8);
                    _regs.A = op_and(_regs.A, readByte(address));
                    _regs.PC += 3;
                    cycleCount = 4;
                    break;

                case 0x26:  // AND A,[X]            3 cycles
                    address = getDirectPageAddress(_regs.X);
                    _regs.A = op_and(_regs.A, readByte(address));
                    _regs.PC++;
                    cycleCount = 3;
                    break;

                case 0x27:  // AND A,[[d+X]]          6 cycles
                    address = getDirectPageAddress((byte)(_mem[_regs.PC + 1] + _regs.X));
                    address2 = readByte(address);
                    address2 += readByte(address + 1) << 8;
                    _regs.A = op_and(_regs.A, readByte(address2));
                    _regs.PC += 2;
                    cycleCount = 6;
                    break;

                case 0x28:  // AND A, #imm          2 cycles
                    _regs.A = op_and(_regs.A, _mem[_regs.PC + 1]);
                    _regs.PC += 2;
                    cycleCount = 2;
                    break;

                case 0x29:  // AND [dd],[ds]        6 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 2]);
                    address2 = getDirectPageAddress(_mem[_regs.PC + 1]);
                    // first byte read is the 2nd argument
                    data = readByte(address2);
                    // do the other read and then the op, then write
                    writeByte(address, op_and(readByte(address), data));
                    _regs.PC += 3;
                    cycleCount = 6;
                    break;

                case 0x2B:  // ROL [d]              4 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    writeByte(address, op_rol(readByte(address)));
                    _regs.PC += 2;
                    cycleCount = 4;
                    break;

                case 0x2C:  // ROL [!a]             5 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8);
                    writeByte(address, op_rol(readByte(address)));
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0x2D:  // PUSH A               4 cycles
                    pushByte(_regs.A);
                    _regs.PC++;
                    cycleCount = 4;
                    break;

                case 0x2E:  // CBNE [dp],r          5 / 7 cycles
                    // branch if A != [dp]
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    if (_regs.A != readByte(address))
                    {
                        // jump
                        _regs.PC = (ushort)(_regs.PC + 3 + (sbyte)_mem[_regs.PC + 2]);
                        cycleCount = 7;
                    }
                    else
                    {
                        // no jump
                        _regs.PC += 3;
                        cycleCount = 5;
                    }
                    break;

                case 0x2F:  // BRA                  4 cycles
                    // Branch (always, with relative operand)
                    address = _regs.PC + (sbyte)_mem[_regs.PC + 1] + 2;
                    _regs.PC = (ushort)address;
                    cycleCount = 4;
                    break;

                case 0x30:  // BMI r                2 cycles / 4 cycles
                    // branch if N == 1
                    if (_regs.FlagN)
                    {
                        // jump
                        _regs.PC = (ushort)(_regs.PC + 2 + (sbyte)_mem[_regs.PC + 1]);
                        cycleCount = 4;
                    }
                    else
                    {
                        // no jump
                        _regs.PC += 2;
                        cycleCount = 2;
                    }
                    break;

                case 0x34:  // AND A,[d+X]          4 cycles
                    address = getDirectPageAddress((byte)(_mem[_regs.PC + 1] + _regs.X)) ;
                    _regs.A = op_and(_regs.A, readByte(address));
                    _regs.PC += 2;
                    cycleCount = 4;
                    break;

                case 0x35:  // AND A, [!a+X]          5 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8) + _regs.X;
                    _regs.A = op_and(_regs.A, readByte(address));
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0x36:  // AND A,[!a+Y]         5 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8) + _regs.Y;
                    _regs.A = op_and(_regs.A, readByte(address));
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0x37:  // AND A,[[d]+Y]        6 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    address2 = readByte(address);
                    address2 += (readByte(address + 1) << 8) + _regs.Y;
                    _regs.A = op_and(_regs.A, readByte(address2));
                    _regs.PC += 2;
                    cycleCount = 6;
                    break;

                case 0x38:  // AND [d], #i            5 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 2]);
                    writeByte(address, op_and(readByte(address), _mem[_regs.PC + 1]));
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0x39:  // AND [X],[Y]          5 cycles
                    address = getDirectPageAddress(_regs.X);
                    address2 = getDirectPageAddress(_regs.Y);
                    data = readByte(address2);
                    writeByte(address, op_and(readByte(address), data));
                    _regs.PC++;
                    cycleCount = 5;
                    break;

                case 0x3A:  // INCW [d]               6 cycles
                    // 16-bit Increment
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    data16 = (ushort)(readByte(address) + (readByte(address + 1) << 8));
                    data16++;
                    // write the bytes back
                    writeByte(address, (byte)(data16 & 0xFF));
                    writeByte(address + 1, (byte)(data16 >> 8));
                    // set the flags appropriately
                    _regs.StoreFlagsNZ(data16);

                    _regs.PC += 2;
                    cycleCount = 6;
                    break;

                case 0x3B:  // ROL [d+X]            5 cycles
                    address = getDirectPageAddress((byte)(_mem[_regs.PC + 1] + _regs.X));
                    writeByte(address, op_rol(readByte(address)));
                    _regs.PC += 2;
                    cycleCount = 5;
                    break;

                case 0x3C:  // ROL A                2 cycles
                    _regs.A = op_rol(_regs.A);
                    _regs.PC++;
                    cycleCount = 2;
                    break;

                case 0x3D:  // INC X                2 cycles
                    _regs.X = op_inc(_regs.X);
                    _regs.PC++;
                    cycleCount = 2;
                    break;

                case 0x3E:  // CMP X, [d]             3 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    op_cmp(_regs.X, readByte(address));
                    _regs.PC += 2;
                    cycleCount = 3;
                    break;

                case 0x3F:  // CALL                 8 cycles
                    // get the target address
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8);
                    // push return address onto the stack, high byte first
                    _regs.PC += 3;
                    pushByte((byte)(_regs.PC >> 8));
                    pushByte((byte)(_regs.PC & 0xFF));
                    // jump to the target address
                    _regs.PC = (ushort)address;
                    cycleCount = 8;
                    break;

                case 0x40:  // SETP                 2 cycles
                    // set DP
                    _regs.FlagP = true;
                    _regs.PC++;
                    cycleCount = 2;
                    break;

                case 0x44:  // EOR A,[d]            3 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    _regs.A = op_eor(_regs.A, readByte(address));
                    _regs.PC += 2;
                    cycleCount = 3;
                    break;

                case 0x45:  // EOR A,[!a]           4 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8);
                    _regs.A = op_eor(_regs.A, readByte(address));
                    _regs.PC += 3;
                    cycleCount = 4;
                    break;

                case 0x46:  // EOR A,[X]            3 cycles
                    address = getDirectPageAddress(_regs.X);
                    _regs.A = op_eor(_regs.A, readByte(address));
                    _regs.PC++;
                    cycleCount = 3;
                    break;

                case 0x47:  // EOR A,[[d+X]]          6 cycles
                    address = getDirectPageAddress((byte)(_mem[_regs.PC + 1] + _regs.X));
                    address2 = readByte(address);
                    address2 += readByte(address + 1) << 8;
                    _regs.A = op_eor(_regs.A, readByte(address2));
                    _regs.PC += 2;
                    cycleCount = 6;
                    break;

                case 0x48:  // EOR A, #i            2 cycles
                    _regs.A = op_eor(_regs.A, _mem[_regs.PC + 1]);
                    _regs.PC += 2;
                    cycleCount = 2;
                    break;

                case 0x49:  // EOR [dd],[ds]        6 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 2]);
                    address2 = getDirectPageAddress(_mem[_regs.PC + 1]);
                    // first byte read is the 2nd argument
                    data = readByte(address2);
                    // do the other read and then the op, then write
                    writeByte(address, op_eor(readByte(address), data));
                    _regs.PC += 3;
                    cycleCount = 6;
                    break;

                case 0x4B:  // LSR [d]               4 cycles
                    // logical shift right by 1
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    writeByte(address, op_lsr(readByte(address)));
                    _regs.PC += 2;
                    cycleCount = 4;
                    break;

                case 0x4C:  // LSR [!a]             5 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8);
                    writeByte(address, op_lsr(readByte(address)));
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0x4D:  // PUSH X               4 cycles
                    pushByte(_regs.X);
                    _regs.PC++;
                    cycleCount = 4;
                    break;

                case 0x4E:  // TCLR1 [!a]           6 cycles
                    // test and clear
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8);
                    // read in the byte
                    mask = readByte(address);
                    // set flags based on the AND of the byte and reg A
                    _regs.StoreFlagsNZ((byte)(mask & _regs.A));
                    // write back out the byte anded with NOT reg A
                    writeByte(address, (byte)(mask & ~_regs.A));
                    _regs.PC += 3;
                    cycleCount = 6;
                    break;

                case 0x4F:  // PCALL u              6 cycles
                    // get the target address
                    address = 0xFF00 + _mem[_regs.PC + 1];
                    // push return address onto the stack, high byte first
                    _regs.PC += 2;
                    pushByte((byte)(_regs.PC >> 8));
                    pushByte((byte)(_regs.PC & 0xFF));
                    // jump to the target address
                    _regs.PC = (ushort)address;
                    cycleCount = 6;
                    break;

                case 0x50:  // BVC r                2 / 4 cycles
                    // branch if V == 0
                    if (!_regs.FlagV)
                    {
                        // jump
                        _regs.PC = (ushort)(_regs.PC + 2 + (sbyte)_mem[_regs.PC + 1]);
                        cycleCount = 4;
                    }
                    else
                    {
                        // no jump
                        _regs.PC += 2;
                        cycleCount = 2;
                    }
                    break;

                case 0x54:  // EOR A,[d+X]          4 cycles
                    address = getDirectPageAddress((byte)(_mem[_regs.PC + 1] + _regs.X));
                    _regs.A = op_eor(_regs.A, readByte(address));
                    _regs.PC += 2;
                    cycleCount = 4;
                    break;

                case 0x55:  // EOR A,[!a+X]         5 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8) + _regs.X;
                    _regs.A = op_eor(_regs.A, readByte(address));
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0x56:  // EOR A,[!a+Y]         5 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8) + _regs.Y;
                    _regs.A = op_eor(_regs.A, readByte(address));
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0x57:  // EOR A,[[d]+Y]        6 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    address2 = readByte(address);
                    address2 += (readByte(address + 1) << 8) + _regs.Y;
                    _regs.A = op_eor(_regs.A, readByte(address2));
                    _regs.PC += 2;
                    cycleCount = 6;
                    break;

                case 0x58:  // EOR [d],#i           5 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 2]);
                    writeByte(address, op_eor(readByte(address), _mem[_regs.PC + 1]));
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0x59:  // EOR [X],[Y]          5 cycles
                    address = getDirectPageAddress(_regs.X);
                    address2 = getDirectPageAddress(_regs.Y);
                    data = readByte(address2);
                    writeByte(address, op_eor(readByte(address), data));
                    _regs.PC++;
                    cycleCount = 5;
                    break;

                case 0x5A:  // CMPW YA,[d]          4 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    data16 = readByte(address);
                    data16 += (ushort)(readByte(address + 1) << 8);
                    
                    temp = (uint)(_regs.YA - data16);
                    // set the flags
                    _regs.FlagC = temp <= 0xFFFF;
                    _regs.StoreFlagsNZ((ushort)temp);
                    _regs.PC += 2;
                    cycleCount = 4;
                    break;

                case 0x5B:  // LSR [d+X]            5 cycles
                    address = getDirectPageAddress((byte)(_mem[_regs.PC + 1] + _regs.X));
                    writeByte(address, op_lsr(readByte(address)));
                    _regs.PC += 2;
                    cycleCount = 5;
                    break;

                case 0x5C:  // LSR A                2 cycles
                    _regs.A = op_lsr(_regs.A);
                    _regs.PC++;
                    cycleCount = 2;
                    break;

                case 0x5D:  // MOV X, A             2 cycles
                    _regs.X = _regs.A;
                    _regs.StoreFlagsNZ(_regs.X);
                    _regs.PC++;
                    cycleCount = 2;
                    break;

                case 0x5E:  // CMP Y,[!a]           4 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8);
                    op_cmp(_regs.Y, readByte(address));
                    _regs.PC += 3;
                    cycleCount = 4;
                    break;

                case 0x5F:  // JMP                  3 cycles
                    // get the target address and set the PC
                    _regs.PC = (ushort)(_mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8));
                    cycleCount = 3;
                    break;

                case 0x60:  // CLRC                 2 cycles
                    // clear C
                    _regs.FlagC = false;
                    _regs.PC++;
                    cycleCount = 2;
                    break;

                case 0x64:  // CMP A, [d]             3 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    op_cmp(_regs.A, readByte(address));
                    _regs.PC += 2;
                    cycleCount = 3;
                    break;

                case 0x65:  // CMP A,[!a]           4 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8);
                    op_cmp(_regs.A, readByte(address));
                    _regs.PC += 3;
                    cycleCount = 4;
                    break;

                case 0x66:  // CMP A,[X]            3 cycles
                    address = getDirectPageAddress(_regs.X);
                    op_cmp(_regs.A, readByte(address));
                    _regs.PC++;
                    cycleCount = 3;
                    break;

                case 0x67:  // CMP A,[[d+X]]        6 cycles
                    address = getDirectPageAddress((byte)(_mem[_regs.PC + 1] + _regs.X));
                    address2 = readByte(address);
                    address2 += readByte(address + 1) << 8;
                    op_cmp(_regs.A, readByte(address2));
                    _regs.PC += 2;
                    cycleCount = 6;
                    break;

                case 0x68:  // CMP A, #i            2 cycles
                    // compare A with immediate by subtracting #i from A, discarding the result
                    op_cmp(_regs.A, _mem[_regs.PC + 1]);
                    _regs.PC += 2;
                    cycleCount = 2;
                    break;

                case 0x69:  // CMP [dd],[ds]        6 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 2]);
                    address2 = getDirectPageAddress(_mem[_regs.PC + 1]);
                    // the first byte read is the 2nd argument
                    data = readByte(address2);
                    // then the other byte is read
                    op_cmp(readByte(address), data);
                    _regs.PC += 3;
                    cycleCount = 6;
                    break;

                case 0x6B:  // ROR [d]               4 cycles
                    // rotate right by 1
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    writeByte(address, op_ror(readByte(address)));
                    _regs.PC += 2;
                    cycleCount = 4;
                    break;

                case 0x6C:  // ROR [!a]             5 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8);
                    writeByte(address, op_ror(readByte(address)));
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0x6D:  // PUSH Y               4 cycles
                    pushByte(_regs.Y);
                    _regs.PC++;
                    cycleCount = 4;
                    break;

                case 0x6E:  // DBNZ [d],r         5 cycles / 7 cycles
                    // decrement [dp] and jump if result != 0
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    _mem[address]--;
                    if (_mem[address] != 0)
                    {
                        // jump
                        _regs.PC = (ushort)(_regs.PC + 3 + (sbyte)_mem[_regs.PC + 2]);
                        cycleCount = 7;
                    }
                    else
                    {
                        // no jump
                        _regs.PC += 3;
                        cycleCount = 5;
                    }
                    break;

                case 0x6F:  // RET                  5 cycles
                    // pop the PC off the stack, low byte first
                    address = popByte();
                    address += popByte() << 8;
                    // set the PC to the new address
                    _regs.PC = (ushort)address;
                    cycleCount = 5;
                    break;

                case 0x70:  // BVS r                2 / 4 cycles
                    // jump if V == 1
                    if (_regs.FlagV)
                    {
                        // jump
                        _regs.PC = (ushort)(_regs.PC + 2 + (sbyte)_mem[_regs.PC + 1]);
                        cycleCount = 4;
                    }
                    else
                    {
                        // no jump
                        _regs.PC += 2;
                        cycleCount = 2;
                    }
                    break;

                case 0x74:  // CMP A,[d+X]         4 cycles
                    address = getDirectPageAddress((byte)(_mem[_regs.PC + 1] + _regs.X));
                    op_cmp(_regs.A, readByte(address));
                    _regs.PC += 2;
                    cycleCount = 4;
                    break;

                case 0x75:  // CMP A,[!a+X]          5 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8) + _regs.X;
                    op_cmp(_regs.A, readByte(address));
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0x76:  // CMP A,[!a+Y]          5 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8) + _regs.Y;
                    op_cmp(_regs.A, readByte(address));
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0x77:  // CMP A,[[d]+Y]       6 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    address2 = readByte(address);
                    address2 += (readByte(address + 1) << 8) + _regs.Y;
                    op_cmp(_regs.A, readByte(address2));
                    _regs.PC += 2;
                    cycleCount = 6;
                    break;

                case 0x78:  // CMP [d],#i            5 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 2]);
                    op_cmp(readByte(address), _mem[_regs.PC + 1]);
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0x79:  // CMP [X],[Y]          5 cycles
                    address = getDirectPageAddress(_regs.X);
                    address2 = getDirectPageAddress(_regs.Y);
                    data = readByte(address2);
                    op_cmp(readByte(address), data);
                    _regs.PC++;
                    cycleCount = 5;
                    break;

                case 0x7A:  // ADDW YA,[d]          5 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    _regs.YA = op_addw(_regs.YA, (ushort)(readByte(address) + (readByte(address + 1) << 8)));
                    _regs.PC += 2;
                    cycleCount = 5;
                    break;

                case 0x7B:  // ROR [d+X]            5 cycles
                    address = getDirectPageAddress((byte)(_mem[_regs.PC + 1] + _regs.X));
                    writeByte(address, op_ror(readByte(address)));
                    _regs.PC += 2;
                    cycleCount = 5;
                    break;

                case 0x7C:  // ROC A                2 cycles
                    _regs.A = op_ror(_regs.A);
                    _regs.PC++;
                    cycleCount = 2;
                    break;

                case 0x7D:  // MOV A, X             2 cycles
                    _regs.A = _regs.X;
                    _regs.StoreFlagsNZ(_regs.A);
                    _regs.PC++;
                    cycleCount = 2;
                    break;

                case 0x7E:  // CMP Y,[d]            3 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    op_cmp(_regs.Y, readByte(address));
                    _regs.PC += 2;
                    cycleCount = 3;
                    break;

                case 0x80:  // SETC                 2 cycles
                    _regs.FlagC = true;
                    _regs.PC++;
                    cycleCount = 2;
                    break;

                case 0x84:  // ADC A,[d]             3 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    _regs.A = op_adc(_regs.A, readByte(address));
                    _regs.PC += 2;
                    cycleCount = 3;
                    break;

                case 0x85:  // ADC A,[!a]            4 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8);
                    _regs.A = op_adc(_regs.A, readByte(address));
                    _regs.PC += 3;
                    cycleCount = 4;
                    break;

                case 0x86:  // ADC A,[X]            3 cycles
                    address = getDirectPageAddress(_regs.X);
                    _regs.A = op_adc(_regs.A, readByte(address));
                    _regs.PC++;
                    cycleCount = 3;
                    break;

                case 0x87:  // ADC A,[[d+X]]        6 cycles
                    address = getDirectPageAddress((byte)(_mem[_regs.PC + 1] + _regs.X));
                    address2 = readByte(address);
                    address2 += readByte(address + 1) << 8;
                    _regs.A = op_adc(_regs.A, readByte(address2));
                    _regs.PC += 2;
                    cycleCount = 6;
                    break;

                case 0x88:  // ADC A,#i             2 cycles
                    _regs.A = op_adc(_regs.A, _mem[_regs.PC + 1]);
                    _regs.PC += 2;
                    cycleCount = 2;
                    break;

                case 0x89:  // ADC [dd],[ds]        6 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 2]);
                    address2 = getDirectPageAddress(_mem[_regs.PC + 1]);
                    data = readByte(address2);
                    writeByte(address, op_adc(readByte(address), data));
                    _regs.PC += 3;
                    cycleCount = 6;
                    break;

                case 0x8A:  // EOR1 C, m.b          5 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8);
                    // get the bit mask
                    mask = (byte)(1 << (address >> 13));
                    // get the actual address
                    address &= 0x1FFF;
                    // read the value
                    data = readByte(address);
                    // xor the bit in the value, set C to the new bit
                    data ^= mask;
                    _regs.FlagC = (mask & readByte(address)) != 0 ? true : false;
                    // write the value back out
                    writeByte(address, data);
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0x8B:  // DEC [d]              4 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    writeByte(address, op_dec(readByte(address)));
                    _regs.PC += 2;
                    cycleCount = 4;
                    break;

                case 0x8C:  // DEC [!a]             5 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8);
                    writeByte(address, op_dec(readByte(address)));
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0x8D:  // MOV Y, #i            2 cycles
                    _regs.Y = _mem[_regs.PC + 1];
                    _regs.StoreFlagsNZ(_regs.Y);
                    _regs.PC += 2;
                    cycleCount = 2;
                    break;

                case 0x8E:  // POP PSW              4 cycles
                    // pop PSW off the stack
                    _regs.PSW = popByte();
                    _regs.PC++;
                    cycleCount = 4;
                    break;

                case 0x8F:  // MOV [d], #i            5 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 2]);
                    writeByte(address, _mem[_regs.PC + 1]);
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0x90:  // BCC r                2 / 4 cycles
                    // jump if C == 0
                    if (_regs.FlagC == false)
                    {
                        // jump
                        _regs.PC = (ushort)(_regs.PC + 2 + (sbyte)_mem[_regs.PC + 1]);
                        cycleCount = 4;
                    }
                    else
                    {
                        // no jump
                        _regs.PC += 2;
                        cycleCount = 2;
                    }
                    break;

                case 0x94:  // ADC A, [d+X]          4 cycles
                    address = getDirectPageAddress((byte)(_mem[_regs.PC + 1] + _regs.X));
                    _regs.A = op_adc(_regs.A, readByte(address));
                    _regs.PC += 2;
                    cycleCount = 4;
                    break;

                case 0x95:  // ADC A, [!a+X]         5 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8) + _regs.X;
                    _regs.A = op_adc(_regs.A, readByte(address));
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0x96:  // ADC A, [!a+Y]         5 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8) + _regs.Y;
                    _regs.A = op_adc(_regs.A, readByte(address));
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0x97:  // ADC A,[[d]+Y]        6 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    // read the bytes to build address2
                    address2 = readByte(address);
                    address2 += (readByte(address + 1) << 8) + _regs.Y;
                    _regs.A = op_adc(_regs.A, readByte(address2));
                    _regs.PC += 2;
                    cycleCount = 6;
                    break;

                case 0x98:  // ADC [d], #i          5 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 2]);
                    writeByte(address, op_adc(readByte(address), _mem[_regs.PC + 1]));
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0x99:  // ADC [X],[Y]          5 cycles
                    address = getDirectPageAddress(_regs.X);
                    address2 = getDirectPageAddress(_regs.Y);
                    data = readByte(address2);
                    writeByte(address, op_adc(readByte(address), data));
                    _regs.PC++;
                    cycleCount = 5;
                    break;

                case 0x9A:  // SUBW YA,[d]          5 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    _regs.YA = op_subw(_regs.YA, (ushort)(readByte(address) + (readByte(address + 1) << 8)));
                    _regs.PC += 2;
                    cycleCount = 5;
                    break;

                case 0x9B:  // DEC [d+X]            5 cycles
                    address = getDirectPageAddress((byte)(_mem[_regs.PC + 1] + _regs.X));
                    writeByte(address, op_dec(readByte(address)));
                    _regs.PC += 2;
                    cycleCount = 5;
                    break;

                case 0x9C:  // DEC A                2 cycles
                    _regs.A = op_dec(_regs.A);
                    _regs.PC++;
                    cycleCount = 2;
                    break;

                case 0x9D:  // MOV X,SP             2 cycles
                    _regs.X = _regs.SP;
                    _regs.StoreFlagsNZ(_regs.X);
                    _regs.PC++;
                    cycleCount = 2;
                    break;

                case 0x9E:  // DIV YA,X             12 cycles
                    op_div();
                    _regs.PC++;
                    cycleCount = 12;
                    break;

                case 0x9F:  // XCN A                5 cycles
                    // swap the nibbles in register A
                    _regs.A = (byte)((_regs.A >> 4) + ((_regs.A & 0xF) << 4));
                    _regs.StoreFlagsNZ(_regs.A);
                    _regs.PC++;
                    cycleCount = 5;
                    break;

                case 0xA0:  // EI                   3 cycles
                    // set interrupt flag
                    _regs.FlagI = true;
                    _regs.PC++;
                    cycleCount = 3;
                    break;

                case 0xA4:  // SBC A, [d]             3 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    _regs.A = op_sbc(_regs.A, readByte(address));
                    _regs.PC += 2;
                    cycleCount = 3;
                    break;

                case 0xA5:  // SBC A,[!a]           4 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8);
                    _regs.A = op_sbc(_regs.A, readByte(address));
                    _regs.PC += 3;
                    cycleCount = 4;
                    break;

                case 0xA6:  // SBC A,[X]            3 cycles
                    address = getDirectPageAddress(_regs.X);
                    _regs.A = op_sbc(_regs.A, readByte(address));
                    _regs.PC++;
                    cycleCount = 3;
                    break;

                case 0xA7:  // SBC A,[[d+X]]        6 cycles
                    address = getDirectPageAddress((byte)(_mem[_regs.PC + 1] + _regs.X));
                    address2 = readByte(address);
                    address2 += readByte(address + 1) << 8;
                    _regs.A = op_sbc(_regs.A, readByte(address2));
                    _regs.PC += 2;
                    cycleCount = 6;
                    break;

                case 0xA8:  // SBC A, #i            2 cycles
                    _regs.A = op_sbc(_regs.A, _mem[_regs.PC + 1]);
                    _regs.PC += 2;
                    cycleCount = 2;
                    break;

                case 0xA9:  // SBC [dd],[ds]        6 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 2]);
                    address2 = getDirectPageAddress(_mem[_regs.PC + 1]);
                    // first byte read is the 2nd argument
                    data = readByte(address2);
                    // do the other read and then the op, then write
                    writeByte(address, op_sbc(readByte(address), data));
                    _regs.PC += 3;
                    cycleCount = 6;
                    break;

                case 0xAA:  // MOV1 C,mem.bit       4 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8);
                    // get the bit mask
                    mask = (byte)(1 << (address >> 13));
                    // get the actual address
                    address &= 0x1FFF;
                    // read the value and set/clear C appropriately
                    _regs.FlagC = (mask & readByte(address)) != 0 ? true : false;
                    _regs.PC += 3;
                    cycleCount = 4;
                    break;

                case 0xAB:  // INC [d]               4 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    writeByte(address, op_inc(readByte(address)));
                    _regs.PC += 2;
                    cycleCount = 4;
                    break;

                case 0xAC:  // INC [!a]             5 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8);
                    writeByte(address, op_inc(readByte(address)));
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0xAD:  // CMP Y, #i            2 cycles
                    op_cmp(_regs.Y, _mem[_regs.PC + 1]);
                    _regs.PC += 2;
                    cycleCount = 2;
                    break;

                case 0xAE:  // POP A                4 cycles
                    _regs.A = popByte();
                    _regs.PC++;
                    cycleCount = 4;
                    break;

                case 0xAF:  // MOV [X+],A           4 cycles
                    address = getDirectPageAddress(_regs.X);
                    writeByte(address, _regs.A);
                    _regs.X++;
                    _regs.PC++;
                    cycleCount = 4;
                    break;

                case 0xB0:  // BCS r                2 cycles / 4 cycles
                    // jump if C == 1
                    if (_regs.FlagC)
                    {
                        // C == 1
                        _regs.PC = (ushort)(_regs.PC + 2 + (sbyte)_mem[_regs.PC + 1]);
                        cycleCount = 4;
                    }
                    else
                    {
                        // C == 0
                        _regs.PC += 2;
                        cycleCount = 2;
                    }
                    break;

                case 0xB4:  // SBC A,[d+X]          4 cycles
                    address = getDirectPageAddress((byte)(_mem[_regs.PC + 1] + _regs.X));
                    _regs.A = op_sbc(_regs.A, readByte(address));
                    _regs.PC += 2;
                    cycleCount = 4;
                    break;

                case 0xB5:  // SBC A,[!a+X]         5 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8) + _regs.X;
                    _regs.A = op_sbc(_regs.A, readByte(address));
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0xB6:  // SBC A,[!a+Y]         5 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8) + _regs.Y;
                    _regs.A = op_sbc(_regs.A, readByte(address));
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0xB7:  // SBC A,[[d]+Y]        6 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    address2 = readByte(address);
                    address2 += (readByte(address + 1) << 8) + _regs.Y;
                    _regs.A = op_sbc(_regs.A, readByte(address2));
                    _regs.PC += 2;
                    cycleCount = 6;
                    break;

                case 0xB8:  // SBC [d],#i           5 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 2]);
                    writeByte(address, op_sbc(readByte(address), _mem[_regs.PC + 1]));
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0xB9:  // SBC [X],[Y]          5 cycles
                    address = getDirectPageAddress(_regs.X);
                    address2 = getDirectPageAddress(_regs.Y);
                    data = readByte(address2);
                    writeByte(address, op_sbc(readByte(address), data));
                    _regs.PC++;
                    cycleCount = 5;
                    break;

                case 0xBA:  // MOVW YA, dp          5 cycles
                    // 16 bit read
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    _regs.A = readByte(address);
                    _regs.Y = readByte(address + 1);
                    _regs.StoreFlagsNZ(_regs.YA);
                    _regs.PC += 2;
                    cycleCount = 5;
                    break;

                case 0xBB:  // INC [d+X]            5 cycles
                    address = getDirectPageAddress((byte)(_mem[_regs.PC + 1] + _regs.X));
                    writeByte(address, op_inc(readByte(address)));
                    _regs.PC += 2;
                    cycleCount = 5;
                    break;

                case 0xBC:  // INC A                2 cycles
                    _regs.A = op_inc(_regs.A);
                    _regs.PC++;
                    cycleCount = 2;
                    break;

                case 0xBD:  // MOV SP,X             2 cycles
                    _regs.SP = _regs.X;
                    _regs.PC++;
                    cycleCount = 2;
                    break;

                case 0xBF:  // MOV A, [X+]          4 cycles
                    address = getDirectPageAddress(_regs.X);
                    _regs.A = readByte(address);
                    // increment X after
                    _regs.X++;
                    // flags for A
                    _regs.StoreFlagsNZ(_regs.A);
                    _regs.PC++;
                    cycleCount = 4;
                    break;

                case 0xC0:  // DI                   3 cycles
                    // clear the interrupt flag
                    _regs.FlagI = false;
                    _regs.PC++;
                    cycleCount = 3;
                    break;

                case 0xC4:  // MOV [dp], A          4 cycles
                    // determine the address of the write
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    writeByte(address, _regs.A);
                    _regs.PC += 2;
                    cycleCount = 4;
                    break;

                case 0xC5:  // MOV [!a], A          5 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8);
                    writeByte(address, _regs.A);
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0xC6:  // MOV [X},A            4 cycles
                    address = getDirectPageAddress(_regs.X);
                    writeByte(address, _regs.A);
                    _regs.PC++;
                    cycleCount = 4;
                    break;

                case 0xC7:  // MOV [[d+X]],A        7 cycles
                    address = getDirectPageAddress((byte)(_mem[_regs.PC + 1] + _regs.X));
                    address2 = readByte(address);
                    address2 += readByte(address + 1) << 8;
                    // write A to the address
                    writeByte(address2, _regs.A);
                    _regs.PC += 2;
                    cycleCount = 7;
                    break;

                case 0xC8:  // CMP X, #i            2 cycles
                    op_cmp(_regs.X, _mem[_regs.PC + 1]);
                    _regs.PC += 2;
                    cycleCount = 2;
                    break;

                case 0xC9:  // MOV [!a],X           5 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8);
                    writeByte(address, _regs.X);
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0xCA:  // MOV1 m.b,C           6 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8);
                    // get the bit mask
                    mask = (byte)(1 << (address >> 13));
                    // get the actual address
                    address &= 0x1FFF;
                    // read the value, and set/clear the bit appropriately, then write it back
                    data = readByte(address);
                    data = (byte)(_regs.FlagC ? data | mask : data & ~mask);
                    writeByte(address, data);
                    _regs.PC += 3;
                    cycleCount = 6;
                    break;

                case 0xCB:  // MOV d, Y             4 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    writeByte(address, _regs.Y);
                    _regs.PC += 2;
                    cycleCount = 4;
                    break;

                case 0xCC:  // MOV [!a],Y           5 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8);
                    writeByte(address, _regs.Y);
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0xCD:  // MOV X, #i            2 cycles
                    _regs.X = _mem[_regs.PC + 1];
                    _regs.StoreFlagsNZ(_regs.X);
                    _regs.PC += 2;
                    cycleCount = 2;
                    break;

                case 0xCE:  // POP X                4 cycles
                    _regs.X = popByte();
                    _regs.PC++;
                    cycleCount = 4;
                    break;

                case 0xCF:  // MUL YA               9 cycles
                    // YA = Y * A
                    _regs.YA = (ushort)(_regs.Y * _regs.A);
                    _regs.StoreFlagsNZ(_regs.YA);
                    _regs.PC++;
                    cycleCount = 9;
                    break;

                case 0xD0:  // BNE r                2 cycles / 4 cycles
                    // jump if flag Z == 0
                    if (_regs.FlagZ)
                    {
                        // Z == 1
                        _regs.PC += 2;
                        cycleCount = 2;
                    }
                    else
                    {
                        // Z == 0
                        _regs.PC = (ushort)(_regs.PC + 2 + (sbyte)_mem[_regs.PC + 1]);
                        cycleCount = 4;
                    }
                    break;

                case 0xD4:  // MOV [dp+X], A        5 cycles
                    address = getDirectPageAddress((byte)(_mem[_regs.PC + 1] + _regs.X)) ;
                    writeByte(address, _regs.A);
                    _regs.PC += 2;
                    cycleCount = 5;
                    break;

                case 0xD5:  // MOV [!a+X], A        6 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8) + _regs.X;
                    writeByte(address, _regs.A);
                    _regs.PC += 3;
                    cycleCount = 6;
                    break;

                case 0xD6:  // MOV [!a+Y], A        6 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8) + _regs.Y;
                    writeByte(address, _regs.A);
                    _regs.PC += 3;
                    cycleCount = 6;
                    break;

                case 0xD7:  // MOV [[d]+Y], A        7 cycles
                    // get the addres to grab a WORD address from
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    // read in that address and add reg.Y to it
                    address2 = readByte(address);
                    address2 += (readByte(address + 1) << 8) + _regs.Y;
                    // finally do the write from A
                    writeByte(address2, _regs.A);
                    _regs.PC += 2;
                    cycleCount = 7;
                    break;

                case 0xD8:  // MOV d, X             4 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    writeByte(address, _regs.X);
                    _regs.PC += 2;
                    cycleCount = 4;
                    break;

                case 0xD9:  // MOV [d+Y],X          5 cycles
                    address = getDirectPageAddress((byte)(_mem[_regs.PC + 1] + _regs.Y));
                    writeByte(address, _regs.X);
                    _regs.PC += 2;
                    cycleCount = 5;
                    break;

                case 0xDA:  // MOVW [d], YA        5 cycles
                    // 16-bit MOVE
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    // write A first as lower byte, then Y as upper byte
                    writeByte(address, _regs.A);
                    writeByte(address + 1, _regs.Y);
                    _regs.PC += 2;
                    // NOTE: 5 cycles according to https://wiki.superfamicom.org/spc700-reference
                    // but testing with SPCTool is showing 4 cycles, so I'm not sure;
                    // for now I'll go with the 4 cycles, though SPCTool is probably wrong
#if SPCTOOL
                    cycleCount = 4;
#else
                    cycleCount = 5;
#endif
                    break;

                case 0xDB:  // MOV [d+X],Y          5 cycles
                    address = getDirectPageAddress((byte)(_mem[_regs.PC + 1] + _regs.X)) ;
                    writeByte(address, _regs.Y);
                    _regs.PC += 2;
                    cycleCount = 5;
                    break;

                case 0xDC:  // DEC Y                2 cycles
                    _regs.Y = op_dec(_regs.Y);
                    _regs.PC++;
                    cycleCount = 2;
                    break;

                case 0xDD:  // MOV A, Y             2 cycles
                    _regs.A = _regs.Y;
                    _regs.StoreFlagsNZ(_regs.A);
                    _regs.PC++;
                    cycleCount = 2;
                    break;

                case 0xDE:  // CBNE [d+X], rel      6 / 8 cycles
                    address = getDirectPageAddress((byte)(_mem[_regs.PC + 1] + _regs.X));
                    // jump if value at address != A
                    if (_regs.A != readByte(address))
                    {
                        // jump
                        _regs.PC = (ushort)(_regs.PC + 3 + (sbyte)_mem[_regs.PC + 2]);
                        cycleCount = 8;
                    }
                    else
                    {
                        // no jump
                        _regs.PC += 3;
                        cycleCount = 6;
                    }
                    break;

                case 0xE0:  // CLRV                 2 cycles
                    // clear V and H
                    _regs.FlagV = false;
                    _regs.FlagH = false;
                    _regs.PC++;
                    cycleCount = 2;
                    break;

                case 0xE4:  // MOV A, d             3 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    _regs.A = readByte(address);
                    _regs.StoreFlagsNZ(_regs.A);
                    _regs.PC += 2;
                    cycleCount = 3;
                    break;

                case 0xE5:  // MOV A,!a             4 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8);
                    _regs.A = readByte(address);
                    _regs.StoreFlagsNZ(_regs.A);
                    _regs.PC += 3;
                    cycleCount = 4;
                    break;

                case 0xE6:  // MOV A,[X]            3 cycles
                    address = getDirectPageAddress(_regs.X);
                    _regs.A = readByte(address);
                    _regs.StoreFlagsNZ(_regs.A);
                    _regs.PC++;
                    cycleCount = 3;
                    break;

                case 0xE7:  // MOVE A,[[d+X]]       6 cycles
                    address = getDirectPageAddress((byte)(_mem[_regs.PC + 1] + _regs.X));
                    address2 = readByte(address);
                    address2 += readByte(address + 1) << 8;
                    _regs.A = readByte(address2);
                    _regs.StoreFlagsNZ(_regs.A);
                    _regs.PC += 2;
                    cycleCount = 6;
                    break;

                case 0xE8:  // MOV A, #i            2 cycles
                    // load immediate into A, affects N and Z flags
                    _regs.A = _mem[_regs.PC + 1];
                    _regs.StoreFlagsNZ(_regs.A);
                    _regs.PC += 2;
                    cycleCount = 2;
                    break;

                case 0xE9:  // MOV X,[!a]           4 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8);
                    _regs.X = readByte(address);
                    _regs.StoreFlagsNZ(_regs.X);
                    _regs.PC += 3;
                    cycleCount = 4;
                    break;

                case 0xEA:  // NOT1 mem.bit         5 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8);
                    mask = (byte)(1 << (address >> 13));
                    address &= 0x1FFF;
                    writeByte(address, (byte)(readByte(address) ^ mask));
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0xEB:  // MOV Y,[d]             3 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    _regs.Y = readByte(address);
                    _regs.StoreFlagsNZ(_regs.Y);
                    _regs.PC += 2;
                    cycleCount = 3;
                    break;

                case 0xEC:  // MOV Y,[!a]           4 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8);
                    _regs.Y = readByte(address);
                    _regs.StoreFlagsNZ(_regs.Y);
                    _regs.PC += 3;
                    cycleCount = 4;
                    break;

                case 0xED:  // NOTC                 3 cycles
                    // complement carry
                    _regs.FlagC = !_regs.FlagC;
                    _regs.PC++;
                    cycleCount = 3;
                    break;

                case 0xEE:  // POP Y                4 cycles
                    _regs.Y = popByte();
                    _regs.PC++;
                    cycleCount = 4;
                    break;

                case 0xF0:  // BEQ r                2 cycles / 4 cycles
                    // jump if Z == 1
                    if (_regs.FlagZ)
                    {
                        // Z == 1
                        _regs.PC = (ushort)(_regs.PC + 2 + (sbyte)_mem[_regs.PC + 1]);
                        cycleCount = 4;
                    }
                    else
                    {
                        // Z == 0
                        _regs.PC += 2;
                        cycleCount = 2;
                    }
                    break;

                case 0xF4:  // MOV A, d+X           4 cycles
                    address = getDirectPageAddress((byte)(_mem[_regs.PC + 1] + _regs.X));
                    _regs.A = readByte(address);
                    _regs.StoreFlagsNZ(_regs.A);
                    _regs.PC += 2;
                    cycleCount = 4;
                    break;

                case 0xF5:  // MOV A, !a+X          5 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8) + _regs.X;
                    _regs.A = readByte(address);
                    _regs.StoreFlagsNZ(_regs.A);
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0xF6:  // MOV A,[!a+Y]          5 cycles
                    address = _mem[_regs.PC + 1] + (_mem[_regs.PC + 2] << 8) + _regs.Y;
                    _regs.A = readByte(address);
                    _regs.StoreFlagsNZ(_regs.A);
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0xF7:  // MOV A,[[d]+Y]       6 cycles
                    // get the addres to grab a WORD address from
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    // read in that address and add reg.Y to it
                    address2 = readByte(address);
                    address2 += (readByte(address + 1) << 8) + _regs.Y;
                    // finally do the read into A
                    _regs.A = readByte(address2);
                    _regs.StoreFlagsNZ(_regs.A);
                    _regs.PC += 2;
                    cycleCount = 6;
                    break;

                case 0xF8:  // MOV X,[d]          3 cycles
                    address = getDirectPageAddress(_mem[_regs.PC + 1]);
                    _regs.X = readByte(address);
                    _regs.StoreFlagsNZ(_regs.X);
                    _regs.PC += 2;
                    cycleCount = 3;
                    break;

                case 0xF9:  // MOV X,[d+Y]          4 cycles
                    address = getDirectPageAddress((byte)(_mem[_regs.PC + 1] + _regs.Y));
                    _regs.X = readByte(address);
                    _regs.StoreFlagsNZ(_regs.X);
                    _regs.PC += 2;
                    cycleCount = 4;
                    break;

                case 0xFA:  // MOV [dd],[ds]           5 cycles
                    address2 = getDirectPageAddress(_mem[_regs.PC + 1]);
                    address = getDirectPageAddress(_mem[_regs.PC + 2]);
                    writeByte(address, readByte(address2));
                    _regs.PC += 3;
                    cycleCount = 5;
                    break;

                case 0xFB:  // MOV Y,[d+X]          4 cycles
                    address = getDirectPageAddress((byte)(_mem[_regs.PC + 1] + _regs.X));
                    _regs.Y = readByte(address);
                    _regs.StoreFlagsNZ(_regs.Y);
                    _regs.PC += 2;
                    cycleCount = 4;
                    break;

                case 0xFC:  // INC Y                2 cycles
                    _regs.Y = op_inc(_regs.Y);
                    _regs.PC++;
                    cycleCount = 2;
                    break;

                case 0xFD:  // MOV Y,A             2 cycles
                    // affects N and Z flags
                    _regs.Y = _regs.A;
                    _regs.StoreFlagsNZ(_regs.Y);
                    _regs.PC++;
                    cycleCount = 2;
                    break;

                case 0xFE:  // DBNZ Y, r            4 cycles / 6 cycles
                    // decrement Y and branch if result != 0
                    unchecked { _regs.Y--; }
                    if (_regs.Y != 0)
                    {
                        // jump
                        _regs.PC = (ushort)(_regs.PC + 2 + (sbyte)_mem[_regs.PC + 1]);
                        cycleCount = 6;
                    }
                    else
                    {
                        // don't jump
                        _regs.PC += 2;
                        cycleCount = 4;
                    }
                    break;

                default:
                    throw new Exception(string.Format("Unhandled opcode {0:X2} at {1:X4}", op, _regs.PC));
            }

            // tick thru the clock cycles
            tickCycles(cycleCount);
        }

        /// <summary>
        /// Executes instructions until a minimum number of clock cycles have elapsed.
        /// </summary>
        /// <param name="cycleCount"></param>
        public void ExecuteCycles(int cycleCount)
        {
            // check arguments
            if (cycleCount < 0) throw new ArgumentOutOfRangeException("cycleCount");

            long cycleStart = _totalClockCycles;

            while (_totalClockCycles < cycleStart + cycleCount)
            {
                executeInstruction();
            }
        }


        public void Reset()
        {
            reset();
        }

        /// <summary>
        /// Gets the DSP object bound to the Core.
        /// </summary>
        public DSP DSP => _dsp;

        /// <summary>
        /// Allows direct access to the SPCCore's memory, for speed considerations.
        /// </summary>
        /// <returns></returns>
        public byte[] Memory => _mem;
    }
}
