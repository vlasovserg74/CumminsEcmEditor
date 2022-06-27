﻿using CumminsEcmEditor.Tools.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CumminsEcmEditor.IntelHex
{
    public class Record
    {
        #region Intel HEX386 Example
        // HEX386?
        // Intel Hex File
        // :200000005C50058074C82C8020500580FFFFFFFF0046365200000000FFFFFFFFFFFFFFFF10
        // |**||||**||||****^^^^****^^^^****^^^^****^^^^****^^^^****^^^^****^^^^****||
        // |**||||**^^^^--------------------------Data--------------------------^^^^||
        // |**||||^^ : Type                                                         ||
        // |**||||     00 : More lines to follow                                    ||
        // |**||||     01 : This is the last line and loading should stop           ||
        // |**||||     02 : Indicates the current segment address                   ||
        // |**||||          To calculate absolute address of each record (line),    ||
        // |**||||          we have to shift the current segment address 4 bits     ||
        // |**||||          to the left and then add it to the record address       ||
        // |**||||                                                                  ||
        // |**||||          Record   Address       0080                             ||
        // |**||||          Segment  Address      B0E0    +                         ||
        // |**||||                              ----------                          ||
        // |**||||          Absolute Address      B0E80                             ||
        // |**||||                                                                  ||
        // |**^^^^ : 16-bit address. or record address in current segment           ||
        // |^^ : RecordLength.    Tells the loader how many bytes are in the line   ||
        // ^ : Signals Line start                                                   ||
        //                                                              CheckSum  : ^^
        // If 2F is the checksum of the record and calculated as
        // 01h + NOT(02h + 01h + 00h + 00h + FFh + CFh)

        // Another opinion on Type Values
        // 00: Data
        // 01: End of File
        // 02: Extended Segment Address
        //      :020000021200EA
        // 03: Star Segment Address
        //      :040000030003800C1
        // 04: Extended Linear Address
        //      :02000004FFFFFC
        //      > therefore physical address =FFFF____
        #endregion

        #region Private Properties
        private byte RecordLength { get; set; }
        private int Address { get; set; }
        private int ExtendedLinearAddress { get; set; }
        private byte RecordType { get; set; }
        private byte[] Data { get; set; }
        private byte CheckSum { get; set; }
        #endregion

        #region Constructors
        private Record(string hexLine, int extendedLinearAddress)
        {
            ExtendedLinearAddress = extendedLinearAddress;
            ProcessRecord(hexLine, extendedLinearAddress);
        }
        #endregion

        #region Private Methods
        private void ProcessRecord(string hexLine, int extendedLinearAddress)
        {
            RecordLength = hexLine[1..3].HexToByte();
            Address = hexLine[3..7].HexToInt();
            RecordType = hexLine[7..9].HexToByte();
            if (RecordType == 0x04)
                ExtendedLinearAddress = hexLine[9..(9 + 4)].HexToInt() << 16;
            else
                ExtendedLinearAddress = extendedLinearAddress;
            if (RecordLength > 0)
                Data = GetDataFromHexString(hexLine[9..(9 + (RecordLength * 2))]);
            CheckSum = hexLine[(hexLine.Length - 2)..(hexLine.Length)].HexToByte();
        }
        private byte[] GetDataFromHexString(string hexString)
        {
            byte[] result = new byte[RecordLength];
            for (int b = 0; b < RecordLength; b++)
            {
                int loopPos = b * 2;
                string loopString = "";
                for (int l = 0; l < 2; l++)
                    loopString += hexString[loopPos + l];
                result[b] = loopString.HexToByte();
            }
            return result;
        }
        #endregion

        #region Static Methods
        public static bool NewRecord(string hexLine, int extendedLinearAddress, out Record record, out int eLA)
        {
            record = null;
            eLA = -1;
                if (hexLine[0] == ':' && hexLine.Length % 2 == 1)
                {
                    record = new(hexLine, extendedLinearAddress);
                    eLA = record.ExtendedLinearAddress;
                    return true;
                }
            return false;
        }
        #endregion
    }
}
