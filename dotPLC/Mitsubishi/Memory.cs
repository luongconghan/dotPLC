using System;
using System.Reflection;

namespace dotPLC.Mitsubishi
{
    /// <summary>
    /// Represents a data memory area of the PLC.
    /// </summary>
    public class Memory
    {
        /// <summary>
        /// D device memory.
        /// </summary>
        public byte[] D { get; set; } = new byte[16000];//7999
        /// <summary>
        /// SW device memory.
        /// </summary>
        public byte[] SW { get; set; } = new byte[65535]; //1ff -sai [7fff=32766]
        /// <summary>
        /// W device memory.
        /// </summary>
        public byte[] W { get; set; } = new byte[65535]; //same
        /// <summary>
        /// TN device memory.
        /// </summary>
        public byte[] TN { get; set; } = new byte[2048]; //511 sai 1023
        /// <summary>
        /// SD device memory.
        /// </summary>
        public byte[] SD { get; set; } = new byte[24000]; //SD11999
        /// <summary>
        /// R device memory.
        /// </summary>
        public byte[] R { get; set; } = new byte[65535]; //R32767
        /// <summary>
        /// Z device memory.
        /// </summary>
        public byte[] Z { get; set; } = new byte[40]; //19
        /// <summary>
        /// LZ device memory.
        /// </summary>
        public byte[] LZ { get; set; } = new byte[4]; //1Dw
        /// <summary>
        /// CN device memory.
        /// </summary>
        public byte[] CN { get; set; } = new byte[2048];//1023 sai
        /// <summary>
        /// LCN device memory.
        /// </summary>
        public byte[] LCN { get; set; } = new byte[2048]; //63 => 64*4= -sai 1023
        /// <summary>
        /// SN device memory.
        /// </summary>
        public byte[] SN { get; set; } = new byte[2048]; //15 => 16*2=32 -sai same
        /// <summary>
        /// STN device memory.
        /// </summary>
        public byte[] STN { get; set; } = new byte[2048]; //15 => 16*2=32-sai same
        /// <summary>
        /// X device memory.
        /// </summary>
        public bool[] X { get; set; } = new bool[1024]; // 1777(octal) =1023(decimal)
        /// <summary>
        /// Y device memory.
        /// </summary>
        public bool[] Y { get; set; } = new bool[1024]; //same
        /// <summary>
        /// M device memory.
        /// </summary>
        public bool[] M { get; set; } = new bool[32767]; //M7679_sai 32767
        /// <summary>
        /// L device memory.
        /// </summary>
        public bool[] L { get; set; } = new bool[32767]; //same sai same
        /// <summary>
        /// F device memory.
        /// </summary>
        public bool[] F { get; set; } = new bool[32767]; //f127 sai same
        /// <summary>
        /// B device memory.
        /// </summary>
        public bool[] B { get; set; } = new bool[0x7ff]; //ff=255 sai 7fff
        /// <summary>
        /// S device memory.
        /// </summary>
        public bool[] S { get; set; } = new bool[4095]; //S4095
        /// <summary>
        /// SS device memory.
        /// </summary>
        public bool[] SS { get; set; } = new bool[1023]; //ss15 sai 1023
        /// <summary>
        /// SC device memory.
        /// </summary>
        public bool[] SC { get; set; } = new bool[1023]; //same sai same
        /// <summary>
        /// TC device memory.
        /// </summary>
        public bool[] TC { get; set; } = new bool[1023]; //TC511 sai same
        /// <summary>
        /// TS device memory.
        /// </summary>
        public bool[] TS { get; set; } = new bool[1023]; //same sai same
        /// <summary>
        /// CS device memory.
        /// </summary>
        public bool[] CS { get; set; } = new bool[1023]; //255 sai same
        /// <summary>
        /// CC device memory.
        /// </summary>
        public bool[] CC { get; set; } = new bool[1023]; // same
        /// <summary>
        /// SB device memory.
        /// </summary>
        public bool[] SB { get; set; } = new bool[0x7fff]; //1ff =511
        /// <summary>
        /// SM device memory.
        /// </summary>
        public bool[] SM { get; set; } = new bool[9999]; //9999
        /// <summary>
        /// BL device memory.
        /// </summary>
        public bool[] BL { get; set; } = new bool[32]; //31
        /// <summary>
        /// Convert bool array to a byte.
        /// </summary>
        /// <param name="source">Bool array.</param>
        /// <returns>Returned byte.</returns>
        public static byte ConvertBoolArrayToByte(bool[] source)
        {
            byte result = 0;
            // This assumes the array never contains more than 8 elements!
            int index = 8 - source.Length;

            // Loop through the array
            foreach (bool b in source)
            {
                // if the element is 'true' set the bit at that position
                if (b)
                    result |= (byte)(1 << (7 - index));

                index++;
            }

            return result;
        }
        /// <summary>
        /// Convert a byte to bool array.[8-bit]
        /// </summary>
        /// <param name="b">A byte.</param>
        /// <returns>Returned bool array.[8-bit]</returns>
        public static bool[] ConvertByteToBoolArray(byte b)
        {
            // prepare the return result
            bool[] result = new bool[8];

            // check each bit in the byte. if 1 set to true, if 0 set to false
            for (int i = 0; i < 8; i++)
                result[i] = (b & (1 << i)) != 0;

            // reverse the array
            Array.Reverse(result);

            return result;
        }
        /// <summary>
        /// Clear data memory area of the PLC.
        /// </summary>
        public void Clear()
        {
            Type type = this.GetType();
            PropertyInfo[] properties = type.GetProperties();
            for (int i = 0; i < properties.Length; i++)
            {
                var coils = properties[i].GetValue(this) as bool[];
                if (coils != null)
                    Array.Clear(coils, 0, coils.Length);
                else
                {
                    var registers = properties[i].GetValue(this) as byte[];
                    if (registers != null)
                        Array.Clear(registers, 0, registers.Length);
                }
            }
        }
    }
}
