using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpcPlayer.SPC
{
    /// <summary>
    /// Class to disassemble instructions into human-readable form
    /// </summary>
    internal class SPCDisassembly
    {
        private static string hex(int value)
        {
            if (value < 10) return string.Format("{0}", value);

            if (value < 16) return string.Format("0{0:X}h", value);

            return string.Format("{0:X}h", value);
        }

        private static string disassemble(byte[] bytes, int startIndex)
        {
            StringBuilder sb = new StringBuilder();
            string args = "";
            string name = "";
            byte op = bytes[startIndex];
            int count = 1;      // number of bytes the op takes up
            ushort temp;

            switch (op)
            {
                case 0x0:
                    name = "NOP";
                    count = 1;
                    break;

                case 0x01:  // TCALL n
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
                    name = "TCALL";
                    args = string.Format("{0}", op >> 4);
                    count = 1;
                    break;

                case 0x02:  // SET1 d.n
                case 0x22:
                case 0x42:
                case 0x62:
                case 0x82:
                case 0xA2:
                case 0xC2:
                case 0xE2:
                    name = "SET1";
                    args = string.Format("[{0}],{1}",
                        hex(bytes[startIndex + 1]),
                        op >> 5);
                    count = 2;
                    break;

                case 0x03:  // BBS d.n, r
                case 0x23:
                case 0x43:
                case 0x63:
                case 0x83:
                case 0xA3:
                case 0xC3:
                case 0xE3:
                    name = "BBS";
                    args = string.Format("[{0}],{1},{2:X4}",
                        hex(bytes[startIndex + 1]),
                        op >> 5,
                        startIndex + (sbyte)bytes[startIndex + 2] + 3);
                    count = 3;
                    break;

                case 0x4:
                    name = "OR";
                    args = string.Format("A,[{0}]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x5:
                    name = "OR";
                    args = string.Format("A,[{0}]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0x6:
                    name = "OR";
                    args = "A,[X]";
                    count = 1;
                    break;

                case 0x7:
                    name = "OR";
                    args = string.Format("A,[[{0}+X]]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x8:
                    name = "OR";
                    args = string.Format("A,{0}",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x9:
                    name = "OR";
                    args = string.Format("[{0}],[{1}]",
                        hex(bytes[startIndex + 2]),
                        hex(bytes[startIndex + 1]));
                    count = 3;
                    break;

                case 0xB:
                    name = "ASL";
                    args = string.Format("[{0}]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xC:
                    name = "ASL";
                    args = string.Format("[{0}]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0xD:
                    name = "PUSH";
                    args = "PSW";
                    count = 1;
                    break;

                case 0xE:
                    name = "TSET1";
                    args = string.Format("[{0}]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0xF:
                    name = "BRK";
                    count = 1;
                    break;

                case 0x10:
                    name = "BPL";
                    args = string.Format("{0:X4}", startIndex + 2 + (sbyte)bytes[startIndex + 1]);
                    count = 2;
                    break;

                case 0x12:  // CLR1 d.n
                case 0x32:
                case 0x52:
                case 0x72:
                case 0x92:
                case 0xB2:
                case 0xD2:
                case 0xF2:
                    name = "CLR1";
                    args = string.Format("[{0}],{1}",
                        hex(bytes[startIndex + 1]),
                        op >> 5);
                    count = 2;
                    break;

                case 0x13:  // BBC d.0, r           5 cycles / 7 cycles
                case 0x33:  // BBC d.1, r
                case 0x53:  // BBC d.2, r
                case 0x73:  // BBC d.3, r
                case 0x93:  // BBC d.4, r
                case 0xB3:  // BBC d.5, r
                case 0xD3:  // BBC d.6, r
                case 0xF3:  // BBC d.7, r
                    name = "BBC";
                    args = string.Format("[{0}],{1},{2:X}",
                        hex(bytes[startIndex + 1]),
                        op >> 5,
                        startIndex + 3 + (sbyte)bytes[startIndex + 2]);
                    count = 3;
                    break;

                case 0x14:
                    name = "OR";
                    args = string.Format("A,[{0}+X]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x15:
                    name = "OR";
                    args = string.Format("A,[{0}+X]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0x16:
                    name = "OR";
                    args = string.Format("A,[{0}+Y]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0x17:
                    name = "OR";
                    args = string.Format("A,[[{0}]+Y]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x18:
                    name = "OR";
                    args = string.Format("[{0}],{1}",
                        hex(bytes[startIndex + 2]),
                        hex(bytes[startIndex +1]));
                    count = 3;
                    break;

                case 0x19:
                    name = "OR";
                    args = "[X],[Y]";
                    count = 1;
                    break;

                case 0x1A:
                    name = "DECW";
                    args = string.Format("[{0}]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x1B:
                    name = "ASL";
                    args = string.Format("[{0}+X]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x1C:
                    name = "ASL";
                    args = "A";
                    count = 1;
                    break;

                case 0x1D:
                    name = "DEC";
                    args = "X";
                    count = 1;
                    break;

                case 0x1E:
                    name = "CMP";
                    args = string.Format("X,[{0}]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0x1F:
                    name = "JMP";
                    args = string.Format("[{0}+X]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0x20:
                    name = "CLRP";
                    count = 1;
                    break;

                case 0x24:
                    name = "AND";
                    args = string.Format("A,[{0}]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x25:
                    name = "AND";
                    args = string.Format("A,[{0}]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0x26:
                    name = "AND";
                    args = "A,[X]";
                    count = 1;
                    break;

                case 0x27:
                    name = "AND";
                    args = string.Format("A,[[{0}+X]]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x28:
                    name = "AND";
                    args = string.Format("A,{0}",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x29:
                    name = "AND";
                    args = string.Format("[{0}],[{1}]",
                        hex(bytes[startIndex + 2]),
                        hex(bytes[startIndex + 1]));
                    count = 3;
                    break;

                case 0x2B:
                    name = "ROL";
                    args = string.Format("[{0}]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x2C:
                    name = "ROL";
                    args = string.Format("[{0}]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0x2D:
                    name = "PUSH";
                    args = "A";
                    count = 1;
                    break;

                case 0x2E:
                    name = "CBNE";
                    args = string.Format("[{0}],{1:X4}",
                        hex(bytes[startIndex + 1]),
                        startIndex + 3 + (sbyte)bytes[startIndex + 2]);
                    count = 3;
                    break;

                case 0x2F:
                    name = "BRA";
                    args = string.Format("{0:X4}", startIndex + 2 + (sbyte)bytes[startIndex + 1]);
                    count = 2;
                    break;

                case 0x30:
                    name = "BMI";
                    args = string.Format("{0:X4}", startIndex + 2 + (sbyte)bytes[startIndex + 1]);
                    count = 2;
                    break;

                case 0x34:
                    name = "AND";
                    args = string.Format("A,[{0}+X]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x35:
                    name = "AND";
                    args = string.Format("A,[{0}+X]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0x36:
                    name = "AND";
                    args = string.Format("A,[{0}+Y]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0x37:
                    name = "AND";
                    args = string.Format("[[{0}]+Y]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x38:
                    name = "AND";
                    args = string.Format("[{0}],{1}",
                        hex(bytes[startIndex + 2]),
                        hex(bytes[startIndex + 1]));
                    count = 3;
                    break;

                case 0x39:
                    name = "AND";
                    args = "[X],[Y]";
                    count = 1;
                    break;

                case 0x3A:
                    name = "INCW";
                    args = string.Format("[{0}]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x3B:
                    name = "ROL";
                    args = string.Format("[{0}+X]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x3C:
                    name = "ROL";
                    args = "A";
                    count = 1;
                    break;

                case 0x3D:
                    name = "INC";
                    args = "X";
                    count = 1;
                    break;

                case 0x3E:
                    name = "CMP";
                    args = string.Format("X,[{0}]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x3F:
                    name = "CALL";
                    args = string.Format("{0:X4}",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0x40:
                    name = "SETP";
                    count = 1;
                    break;

                case 0x44:
                    name = "EOR";
                    args = string.Format("A,[{0}]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x45:
                    name = "EOR";
                    args = string.Format("A,[{0}]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 2;
                    break;

                case 0x46:
                    name = "EOR";
                    args = "A,[X]";
                    count = 1;
                    break;

                case 0x47:
                    name = "EOR";
                    args = string.Format("A,[[{0}+X]]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x48:
                    name = "EOR";
                    args = string.Format("A,{0}",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x49:
                    name = "EOR";
                    args = string.Format("[{0}],[{1}]",
                        hex(bytes[startIndex + 2]),
                        hex(bytes[startIndex + 1]));
                    count = 3;
                    break;

                case 0x4B:
                    name = "LSR";
                    args = string.Format("[{0}]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x4C:
                    name = "LSR";
                    args = string.Format("[{0}]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0x4D:
                    name = "PUSH";
                    args = "X";
                    count = 1;
                    break;

                case 0x4E:
                    name = "TCLR1";
                    args = string.Format("[{0}]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0x4F:
                    name = "PCALL";
                    args = string.Format("{0}", hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x50:
                    name = "BVC";
                    args = string.Format("{0:X4}", startIndex + 2 + (sbyte)bytes[startIndex + 1]);
                    count = 2;
                    break;

                case 0x54:
                    name = "EOR";
                    args = string.Format("A,[{0}+X]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x55:
                    name = "EOR";
                    args = string.Format("A,[{0}+X]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0x56:
                    name = "EOR";
                    args = string.Format("A,[{0}+Y]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0x57:
                    name = "EOR";
                    args = string.Format("[[{0}]+Y]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x58:
                    name = "EOR";
                    args = string.Format("[{0}],{1}",
                        hex(bytes[startIndex + 2]),
                        hex(bytes[startIndex + 1]));
                    count = 3;
                    break;

                case 0x59:
                    name = "EOR";
                    args = "[X],[Y]";
                    count = 1;
                    break;

                case 0x5A:
                    name = "CMPW";
                    args = string.Format("YA,[{0}]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x5B:
                    name = "LSR";
                    args = string.Format("[{0}+X]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x5C:
                    name = "LSR";
                    args = "A";
                    count = 1;
                    break;

                case 0x5D:
                    name = "MOV";
                    args = "X,A";
                    count = 1;
                    break;

                case 0x5E:
                    name = "CMP";
                    args = string.Format("Y,[{0}]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0x5F:
                    name = "JMP";
                    args = string.Format("{0:X4}", bytes[startIndex + 1] + (bytes[startIndex + 2] << 8));
                    count = 3;
                    break;

                case 0x60:
                    name = "CLRC";
                    count = 1;
                    break;

                case 0x64:
                    name = "CMP";
                    args = string.Format("A,[{0}]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x65:
                    name = "CMP";
                    args = string.Format("A,[{0}]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0x66:
                    name = "CMP";
                    args = "A,[X]";
                    count = 1;
                    break;

                case 0x67:
                    name = "CMP";
                    args = string.Format("A,[[{0}+X]]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x68:
                    name = "CMP";
                    args = string.Format("A,{0}",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x69:
                    name = "CMP";
                    args = string.Format("[{0}],[{1}]",
                        hex(bytes[startIndex + 2]),
                        hex(bytes[startIndex + 1]));
                    count = 3;
                    break;

                case 0x6B:
                    name = "ROR";
                    args = string.Format("[{0}]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x6C:
                    name = "ROR";
                    args = string.Format("[{0}]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0x6D:
                    name = "PUSH";
                    args = "Y";
                    count = 1;
                    break;

                case 0x6E:
                    name = "DBNZ";
                    args = string.Format("[{0}],{1:X4}",
                        hex(bytes[startIndex + 1]),
                        startIndex + 3 + (sbyte)bytes[startIndex + 2]);
                    count = 3;
                    break;

                case 0x6F:
                    name = "RET";
                    count = 1;
                    break;

                case 0x70:
                    name = "BVS";
                    args = string.Format("{0:X4}",
                        startIndex + 2 + (sbyte)bytes[startIndex + 1]);
                    count = 2;
                    break;

                case 0x74:
                    name = "CMP";
                    args = string.Format("A,[{0}+X]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x75:
                    name = "CMP";
                    args = string.Format("A,[{0}+X]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0x76:
                    name = "CMP";
                    args = string.Format("A,[{0}+Y]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0x77:
                    name = "CMP";
                    args = string.Format("A,[[{0}]+Y]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x78:
                    name = "CMP";
                    args = string.Format("[{0}],{1}",
                        hex(bytes[startIndex + 2]),
                        hex(bytes[startIndex + 1]));
                    count = 3;
                    break;

                case 0x79:
                    name = "CMP";
                    args = "[X],[Y]";
                    count = 1;
                    break;

                case 0x7A:
                    name = "ADDW";
                    args = string.Format("YA,[{0}]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x7B:
                    name = "ROR";
                    args = string.Format("[{0}+X]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x7C:
                    name = "ROR";
                    args = "A";
                    count = 1;
                    break;

                case 0x7D:
                    name = "MOV";
                    args = "A,X";
                    count = 1;
                    break;

                case 0x7E:
                    name = "CMP";
                    args = string.Format("Y,[{0}]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x80:
                    name = "SETC";
                    count = 1;
                    break;

                case 0x84:
                    name = "ADC";
                    args = string.Format("A,[{0}]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x85:
                    name = "ADC";
                    args = string.Format("A,[{0}]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0x86:
                    name = "ADC";
                    args = "A,[X]";
                    count = 1;
                    break;

                case 0x87:
                    name = "ADC";
                    args = string.Format("A,[[{0}+X]]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x88:
                    name = "ADC";
                    args = string.Format("A,{0}",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x89:
                    name = "ADC";
                    args = string.Format("[{0}],[{1}]",
                        hex(bytes[startIndex + 2]),
                        hex(bytes[startIndex + 1]));
                    count = 3;
                    break;

                case 0x8A:
                    name = "EOR1";
                    temp = (ushort)(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8));
                    // address of operation is in 0x1FFF
                    args = string.Format("C,[{0}],{1}",
                        hex(temp & 0x1FFF),
                        temp >> 13);
                    count = 3;
                    break;

                case 0x8B:
                    name = "DEC";
                    args = string.Format("[{0}]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x8C:
                    name = "DEC";
                    args = string.Format("[{0}]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex +2] << 8)));
                    count = 3;
                    break;

                case 0x8D:
                    name = "MOV";
                    args = string.Format("Y,{0}",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x8E:
                    name = "POP";
                    args = "PSW";
                    count = 1;
                    break;

                case 0x8F:
                    name = "MOV";
                    args = string.Format("[{0}],{1}",
                        hex(bytes[startIndex + 2]),
                        hex(bytes[startIndex + 1]));
                    count = 3;
                    break;

                case 0x90:
                    name = "BCC";
                    args = string.Format("{0:X4}",
                        startIndex + 2 + (sbyte)bytes[startIndex + 1]);
                    count = 2;
                    break;

                case 0x94:
                    name = "ADC";
                    args = string.Format("A,[{0}+X]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x95:
                    name = "ADC";
                    args = string.Format("A,[{0}+X]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0x96:
                    name = "ADC";
                    args = string.Format("A,[{0}+Y]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0x97:
                    name = "ADC";
                    args = string.Format("A,[[{0}]+Y]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x98:
                    name = "ADC";
                    args = string.Format("[{0}],{1}",
                        hex(bytes[startIndex + 2]),
                        hex(bytes[startIndex + 1]));
                    count = 3;
                    break;

                case 0x99:
                    name = "ADC";
                    args = "[X],[Y]";
                    count = 1;
                    break;

                case 0x9A:
                    name = "SUBW";
                    args = string.Format("YA,[{0}]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x9B:
                    name = "DEC";
                    args = string.Format("[{0}+X]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0x9C:
                    name = "DEC";
                    args = "A";
                    count = 1;
                    break;

                case 0x9D:
                    name = "MOV";
                    args = "X,SP";
                    count = 1;
                    break;

                case 0x9E:
                    name = "DIV";
                    args = "YA,X";
                    count = 1;
                    break;

                case 0x9F:
                    name = "XCN";
                    args = "A";
                    count = 1;
                    break;

                case 0xA4:
                    name = "SBC";
                    args = string.Format("A,[{0}]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xA5:
                    name = "SBC";
                    args = string.Format("A,[{0}]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0xA6:
                    name = "SBC";
                    args = "A,[X]";
                    count = 1;
                    break;

                case 0xA7:
                    name = "SBC";
                    args = string.Format("A,[[{0}+X]]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xA8:
                    name = "SBC";
                    args = string.Format("A,{0}",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xA9:
                    name = "SBC";
                    args = string.Format("[{0}],[{1}]",
                        hex(bytes[startIndex + 2]),
                        hex(bytes[startIndex + 1]));
                    count = 3;
                    break;

                case 0xAA:
                    name = "MOV1";
                    temp = (ushort)(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8));
                    // address of operation is in 0x1FFF
                    args = string.Format("C,[{0}],{1}",
                        hex(temp & 0x1FFF),
                        temp >> 13);
                    count = 3;
                    break;

                case 0xAB:
                    name = "INC";
                    args = string.Format("[{0}]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xAC:
                    name = "INC";
                    args = string.Format("[{0}]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0xAD:
                    name = "CMP";
                    args = string.Format("Y,{0}",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xAE:
                    name = "POP";
                    args = "A";
                    count = 1;
                    break;

                case 0xAF:
                    name = "MOV";
                    args = "[X+],A";
                    count = 1;
                    break;

                case 0xB0:
                    name = "BCS";
                    args = string.Format("{0:X4}",
                        startIndex + 2 + (sbyte)bytes[startIndex + 1]);
                    count = 2;
                    break;

                case 0xB4:
                    name = "SBC";
                    args = string.Format("A,[{0}+X]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xB5:
                    name = "SBC";
                    args = string.Format("A,[{0}+X]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 2;
                    break;

                case 0xB6:
                    name = "SBC";
                    args = string.Format("A,[{0}+Y]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0xB7:
                    name = "SBC";
                    args = string.Format("A,[[{0}]+Y]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xB8:
                    name = "SBC";
                    args = string.Format("[{0}],{1}",
                        hex(bytes[startIndex + 2]),
                        hex(bytes[startIndex + 1]));
                    count = 3;
                    break;

                case 0xB9:
                    name = "SBC";
                    args = "[X],[Y]";
                    count = 1;
                    break;

                case 0xBA:
                    name = "MOVW";
                    args = string.Format("YA,[{0}]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xBB:
                    name = "INC";
                    args = string.Format("[{0}+X]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xBC:
                    name = "INC";
                    args = "A";
                    count = 1;
                    break;

                case 0xBD:
                    name = "MOV";
                    args = "SP,X";
                    count = 1;
                    break;

                case 0xBF:
                    name = "MOV";
                    args = "A,[X+]";
                    count = 1;
                    break;

                case 0xC4:
                    name = "MOV";
                    args = string.Format("[{0}],A",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xC5:
                    name = "MOV";
                    args = string.Format("[{0}],A",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0xC6:
                    name = "MOV";
                    args = "[X],A";
                    count = 1;
                    break;

                case 0xC7:
                    name = "MOV";
                    args = string.Format("[[{0}+X]],A",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 2;
                    break;

                case 0xC8:
                    name = "CMP";
                    args = string.Format("X,{0}",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xC9:
                    name = "MOV";
                    args = string.Format("[{0}],X",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0xCA:
                    name = "MOV";
                    temp = (ushort)(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8));
                    // address of operation is in 0x1FFF
                    args = string.Format("[{0}],{1},C",
                        hex(temp & 0x1FFF),
                        temp >> 13);
                    count = 3;
                    break;

                case 0xCB:
                    name = "MOV";
                    args = string.Format("[{0}],Y",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xCC:
                    name = "MOV";
                    args = string.Format("[{0}],Y",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0xCD:
                    name = "MOV";
                    args = string.Format("X,{0}",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xCE:
                    name = "POP";
                    args = "X";
                    count = 1;
                    break;

                case 0xCF:
                    name = "MUL";
                    args = "YA";
                    count = 1;
                    break;

                case 0xD0:
                    name = "BNE";
                    args = string.Format("{0:X4}", startIndex + 2 + (sbyte)bytes[startIndex + 1]);
                    count = 2;
                    break;

                case 0xD4:
                    name = "MOV";
                    args = string.Format("[{0}+X],A",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xD5:
                    name = "MOV";
                    args = string.Format("[{0}+X],A",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0xD6:
                    name = "MOV";
                    args = string.Format("[{0}+Y],A",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0xD7:
                    name = "MOV";
                    args = string.Format("[[{0}]+Y],A",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xD8:
                    name = "MOV";
                    args = string.Format("[{0}],X",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xD9:
                    name = "MOV";
                    args = string.Format("[{0}+Y],X",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xDA:
                    name = "MOVW";
                    args = string.Format("[{0}],YA",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xDB:
                    name = "MOV";
                    args = string.Format("[{0}+X],Y",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xDC:
                    name = "DEC";
                    args = "Y";
                    count = 1;
                    break;

                case 0xDD:
                    name = "MOV";
                    args = "A,Y";
                    count = 1;
                    break;

                case 0xDE:
                    name = "CBNE";
                    args = string.Format("[{0}+X],{1:X4}",
                        hex(bytes[startIndex + 1]),
                        startIndex + 3 + (sbyte)bytes[startIndex + 2]);
                    count = 3;
                    break;

                case 0xE4:
                    name = "MOV";
                    args = string.Format("A,[{0}]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xE5:
                    name = "MOV";
                    args = string.Format("A,[{0}]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0xE6:
                    name = "MOV";
                    args = "A,[X]";
                    count = 1;
                    break;

                case 0xE7:
                    name = "MOV";
                    args = string.Format("A,[[{0}+X]]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xE8:
                    name = "MOV";
                    args = string.Format("A,{0}",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xE9:
                    name = "MOV";
                    args = string.Format("X,[{0}]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0xEA:
                    name = "NOT1";
                    temp = (ushort)(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8));
                    // address of operation is in 0x1FFF
                    args = string.Format("[{0}],{1}",
                        hex(temp & 0x1FFF),
                        temp >> 13);
                    count = 3;
                    break;

                case 0xEB:
                    name = "MOV";
                    args = string.Format("Y,[{0}]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xEC:
                    name = "MOV";
                    args = string.Format("Y,[{0}]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0xEE:
                    name = "POP";
                    args = "Y";
                    count = 1;
                    break;

                case 0xF0:
                    name = "BEQ";
                    args = string.Format("{0:X4}", startIndex + 2 + (sbyte)bytes[startIndex + 1]);
                    count = 2;
                    break;

                case 0xF4:
                    name = "MOV";
                    args = string.Format("A,[{0}+X]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xF5:
                    name = "MOV";
                    args = string.Format("A,[{0}+X]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0xF6:
                    name = "MOV";
                    args = string.Format("A,[{0}+Y]",
                        hex(bytes[startIndex + 1] + (bytes[startIndex + 2] << 8)));
                    count = 3;
                    break;

                case 0xF7:
                    name = "MOV";
                    args = string.Format("A,[[{0}]+Y]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xF8:
                    name = "MOV";
                    args = string.Format("X,[{0}]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xF9:
                    name = "MOV";
                    args = string.Format("X,[{0}+Y]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xFA:
                    name = "MOV";
                    args = string.Format("[{0}],[{1}]",
                        hex(bytes[startIndex + 2]),
                        hex(bytes[startIndex + 1]));
                    count = 3;
                    break;

                case 0xFB:
                    name = "MOV";
                    args = string.Format("Y,[{0}+Y]",
                        hex(bytes[startIndex + 1]));
                    count = 2;
                    break;

                case 0xFC:
                    name = "INC";
                    args = "Y";
                    count = 1;
                    break;

                case 0xFD:
                    name = "MOV";
                    args = "Y,A";
                    count = 1;
                    break;

                case 0xFE:
                    name = "DBNZ";
                    args = string.Format("Y,{0:X4}",
                        startIndex + 2 + (sbyte)bytes[startIndex + 1]);
                    count = 2;
                    break;

                default:
                    throw new NotImplementedException(string.Format("Unhandled opcode {0:X2}", op));
            }

            // build the string
            // address
            sb.AppendFormat("{0:X4} ", startIndex);
            // then the bytes that make up the instruction
            for (int i = 0; i < 3; i++)
            {
                if (i < count)
                {
                    sb.AppendFormat("{0:X2} ", bytes[startIndex + i]);
                }
                else
                {
                    sb.Append("   ");
                }
            }

            // then append the disassembled line
            sb.AppendFormat(" {0,-6}{1}", name, args);

            return sb.ToString();
        }

        public static string Disassemble(byte[] bytes, int startIndex)
        {
            // check arguments
            ArgumentNullException.ThrowIfNull(bytes);
            if ((startIndex < 0) || (startIndex >= bytes.Length)) throw new ArgumentOutOfRangeException("startIndex");

            return disassemble(bytes, startIndex);
        }
    }
}
