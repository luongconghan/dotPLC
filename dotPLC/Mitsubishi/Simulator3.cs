using dotPLC.Initial;
using dotPLC.Mitsubishi.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace dotPLC.Mitsubishi
{
    /// <summary>
    /// Provides client connection for TCP network service to connect to GX Simulator3.
    /// </summary>
    public sealed class Simulator3 : Ethernet
    {
        /// <summary>
        /// Data to ping to Gx Simulator3.
        /// </summary>
        private byte[] data_ping = new byte[10] { 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29 };
        /// <summary>
        /// Ping
        /// </summary>
        private Ping ping;
        /// <summary>
        /// str_Spacket for settup connect.
        /// </summary>
        private string str_Spacket_1_48 = "5a0000ff";
        /// <summary>
        /// str_Spacket for settup connect.
        /// </summary>
        private string str_Spacket_2_97 = "57010000001111070000ffff030000fe03000020001caa161400000000000000000000000000000000000000000121010000000001";
        /// <summary>
        /// str_Spacket for settup connect.
        /// </summary>
        private string str_Spacket_3_95 = "57010100001111070000ffff030000fe0300001e001caa16140000000000000000000000000000000000000000016202000000";
        /// <summary>
        /// str_Spacket for settup connect.
        /// </summary>
        private string str_Spacket_4_117 = "57010200001111070000ffff030000fe03000034001caa16140000000000000000000000000000000000000000041003000000000000000000000000003f0000000000000000000200";
        /// <summary>
        /// str_Spacket for settup connect.
        /// </summary>
        private string str_Spacket_5_117 = "57010300001111070000ffff030000fe03000034001caa16140000000000000000000000000000000000000000041004000000000000000000000000003f0000000000000000008e01";
        /// <summary>
        /// str_Spacket for settup connect.
        /// </summary>
        private string str_Spacket_6_117 = "57010400001111070000ffff030000fe03000034001caa16140000000000000000000000000000000000000000041005000000000000000000000000003f00f1010000000000000200";
        /// <summary>
        /// str_Spacket for settup connect.
        /// </summary>
        private string str_Spacket_7_117 = "57010500001111070000ffff030000fe03000034001caa16140000000000000000000000000000000000000000041006000000000000000000000000003f00f1010000000000007400";
        /// <summary>
        /// str_Spacket for settup connect.
        /// </summary>
        private string str_Spacket_8_117 = "57010600001111070000ffff030000fe03000034001caa16140000000000000000000000000000000000000000041007000000000000000000000000003f008f010000000000000e00";
        /// <summary>
        /// str_Spacket for settup connect.
        /// </summary>
        private string str_Spacket_9_117 = "57010700001111070000ffff030000fe03000034001caa16140000000000000000000000000000000000000000041008000000000000000000000000003f0091050000000000000800";
        /// <summary>
        /// str_Spacket for settup connect.
        /// </summary>
        private string str_Spacket_10_113 = "57010800001111070000ffff030000fe03000030001caa161400000000000000000000000000000000000000000b2a09000000010001070400400000000000000000000000";
        /// <summary>
        /// ds_byte_to_connect
        /// </summary>
        private List<byte[]> ds_byte_to_connect;
        /// <summary>
        /// ds_string_byte
        /// </summary>
        private List<string> ds_string_byte;
        /// <summary>
        /// Initializes a new instance of the <see cref="Simulator3"></see> class.
        /// </summary>
        public Simulator3()
        {
            IPAddress = "127.0.0.1";
            Port = 5511;
        }
        /// <summary>
        /// Port number of GX Simulator3, default is 5511.
        /// </summary>
        public int Port { get; } = 5511;
        /// <summary>
        /// Write a single value to the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">A single value to be written.</param>
        internal override void WriteDevice(string label, bool value)
        {
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            ++SendBuffer[2];
            SendBuffer[19] = 0x42;
            SendBuffer[20] = 0x00;
            SendBuffer[45] = 0x14;
            SendBuffer[46] = 0x11;
            ++SendBuffer[47];
            SendBuffer[61] = 0x00;
            SendBuffer[62] = 0x01;
            SendBuffer[63] = 0x00;
            SendBuffer[64] = 0x00;
            SendBuffer[66] = 0x01;
            SendBuffer[68] = 0x00;
            for (int index = 70; index <= 84; ++index)
                SendBuffer[index] = 0x00;
            SettingDevice(label, out SendBuffer[69], out SendBuffer[71], out SendBuffer[72], out SendBuffer[73]);
            if (value)
                SendBuffer[85] = 0x01;
            else
                SendBuffer[85] = 0x00;
            SendBuffer[86] = 0x00;
            _stream.Write(SendBuffer, 0, 87);
            _stream.Read(ReceveiBuffer, 0, 53);
        }
        /// <summary>
        /// Write a single value to the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">A single value to be written.</param>
        internal override void WriteDevice(string label, short value)
        {
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            label = sWhitespace.Replace(label, "").ToUpper();
            ++SendBuffer[2];
            SendBuffer[19] = 0x42;
            SendBuffer[20] = 0x00;
            SendBuffer[45] = 0x14;
            SendBuffer[46] = 0x11;
            SendBuffer[61] = 0x01;
            SendBuffer[62] = 0x00;
            SendBuffer[63] = 0x00;
            SendBuffer[64] = 0x00;
            SendBuffer[66] = 0x01;
            SendBuffer[68] = 0x00;
            for (int index = 70; index <= 84; ++index)
                SendBuffer[index] = 0x00;
            SettingDevice(label, out SendBuffer[69], out SendBuffer[71], out SendBuffer[72], out SendBuffer[73]);
            byte[] bytes = BitConverter.GetBytes(value);
            SendBuffer[85] = bytes[0];
            SendBuffer[86] = bytes[1];
            _stream.Write(SendBuffer, 0, 87);
            _stream.Read(ReceveiBuffer, 0, 512);
        }
        /// <summary>
        /// Write a single value to the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">A single value to be written.</param>
        internal override void WriteDevice(string label, int value)
        {
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            ++SendBuffer[2];
            SendBuffer[19] = 0x38; //56
            SendBuffer[20] = 0x00;
            SendBuffer[45] = 0x14;
            SendBuffer[46] = 0x10; //0x016
            ++SendBuffer[47];
            SettingDevice(label, out SendBuffer[61], out SendBuffer[63], out SendBuffer[64], out SendBuffer[65]);
            SendBuffer[62] = 0x00;
            for (int i = 66; i <= 72; i++)
            {
                SendBuffer[i] = 0x00;
            }
            SendBuffer[71] = 0x02;
            byte[] bytes = BitConverter.GetBytes(value);
            for (int index = 0; index < 4; ++index)
                SendBuffer[73 + index] = bytes[index];
            _stream.Write(SendBuffer, 0, 77);
            _stream.Read(ReceveiBuffer, 0, 512);
        }
        /// <summary>
        /// Write a single value to the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">A single value to be written.</param>
        internal override void WriteDevice(string label, float value)
        {
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            ++SendBuffer[2];
            SendBuffer[19] = 0x38; //56
            SendBuffer[20] = 0x00;
            SendBuffer[45] = 0x14;
            SendBuffer[46] = 0x10; //0x016
            ++SendBuffer[47];
            SettingDevice(label, out SendBuffer[61], out SendBuffer[63], out SendBuffer[64], out SendBuffer[65]);
            SendBuffer[62] = 0x00;
            for (int i = 66; i <= 72; i++)
            {
                SendBuffer[i] = 0x00;
            }
            SendBuffer[71] = 0x02;
            byte[] bytes = BitConverter.GetBytes(value);
            for (int index = 0; index < 4; ++index)
                SendBuffer[73 + index] = bytes[index];
            _stream.Write(SendBuffer, 0, 77);
            _stream.Read(ReceveiBuffer, 0, 512);
        }
        /// <summary>
        ///  Write multiple values to the server in a batch.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="values">Values to be written.</param>
        internal override void WriteDeviceBlock(string label, params short[] values)
        {
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            int num = values != null ? values.Length : throw new ArgumentNullException(nameof(values), "Array data must be non-null");
            ++SendBuffer[2];
            byte[] bytes1 = BitConverter.GetBytes(52 + num * 2);
            SendBuffer[19] = bytes1[0];
            SendBuffer[20] = bytes1[1];
            SendBuffer[45] = 0x14;
            SendBuffer[46] = 0x10; //0x016
            ++SendBuffer[47];
            SettingDevice(label, out SendBuffer[61], out SendBuffer[63], out SendBuffer[64], out SendBuffer[65]);
            SendBuffer[62] = 0x00;
            for (int index = 66; index <= 70; ++index)
                SendBuffer[index] = 0x00;
            byte[] bytes2 = BitConverter.GetBytes(num);
            SendBuffer[71] = bytes2[0];
            SendBuffer[72] = bytes2[1];
            int index1 = 72;
            for (int index2 = 0; index2 < num; ++index2)
            {
                byte[] bytes3 = BitConverter.GetBytes(values[index2]);
                for (int index3 = 0; index3 < 2; ++index3)
                {
                    ++index1;
                    SendBuffer[index1] = bytes3[index3];
                }
            }
            _stream.Write(SendBuffer, 0, index1 + 1);
            _stream.Read(ReceveiBuffer, 0, 512);
        }
        /// <summary>
        ///  Write multiple values to the server in a batch.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="values">Values to be written.</param>
        internal override void WriteDeviceBlock(string label, params int[] values)
        {
            int num1 = values != null ? values.Length : throw new ArgumentNullException(nameof(values), "Array data must be non-null");
            ++SendBuffer[2];
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            byte[] bytes1 = BitConverter.GetBytes(52 + 4 * num1);
            SendBuffer[19] = bytes1[0];
            SendBuffer[20] = bytes1[1];
            SendBuffer[45] = 0x14;
            SendBuffer[46] = 0x10; //0x016
            ++SendBuffer[47];
            SettingDevice(label, out SendBuffer[61], out SendBuffer[63], out SendBuffer[64], out SendBuffer[65]);
            SendBuffer[62] = 0x00;
            for (int index = 66; index <= 70; ++index)
                SendBuffer[index] = 0x00;
            byte[] bytes2 = BitConverter.GetBytes(num1 * 2);
            SendBuffer[71] = bytes2[0];
            SendBuffer[72] = bytes2[1];
            int num2 = 0;
            for (int index1 = 0; index1 < num1; ++index1)
            {
                byte[] bytes3 = BitConverter.GetBytes(values[index1]);
                for (int index2 = 0; index2 < 4; ++index2)
                {
                    SendBuffer[73 + num2] = bytes3[index2];
                    ++num2;
                }
            }
            _stream.Write(SendBuffer, 0, 73 + num1 * 4);
            _stream.Read(ReceveiBuffer, 0, 512);
        }
        /// <summary>
        ///  Write multiple values to the server in a batch.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="values">Values to be written.</param>
        internal override void WriteDeviceBlock(string label, params float[] values)
        {
            int num1 = values != null ? values.Length : throw new ArgumentNullException(nameof(values), "Array data must be non-null");
            ++SendBuffer[2];
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            byte[] bytes1 = BitConverter.GetBytes(52 + 4 * num1);
            SendBuffer[19] = bytes1[0];
            SendBuffer[20] = bytes1[1];
            SendBuffer[45] = 0x14;
            SendBuffer[46] = 0x10; //0x016
            ++SendBuffer[47];
            SettingDevice(label, out SendBuffer[61], out SendBuffer[63], out SendBuffer[64], out SendBuffer[65]);
            SendBuffer[62] = 0x00;
            for (int index = 66; index <= 70; ++index)
                SendBuffer[index] = 0x00;
            byte[] bytes2 = BitConverter.GetBytes(num1 * 2);
            SendBuffer[71] = bytes2[0];
            SendBuffer[72] = bytes2[1];
            int num2 = 0;
            for (int index1 = 0; index1 < num1; ++index1)
            {
                byte[] bytes3 = BitConverter.GetBytes(values[index1]);
                for (int index2 = 0; index2 < 4; ++index2)
                {
                    SendBuffer[73 + num2] = bytes3[index2];
                    ++num2;
                }
            }
            _stream.Write(SendBuffer, 0, 73 + num1 * 4);
            _stream.Read(ReceveiBuffer, 0, 512);
        }
        /// <summary>
        ///  Write multiple values to the server in a batch.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="values">Values to be written.</param>
        internal override void WriteDeviceBlock(string label, params bool[] values)
        {
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            int num = values.Length % 16;
            int length = values.Length;
            int size = values != null ? num == 0 ? length / 16 : (length / 16) + 1 : throw new ArgumentNullException(nameof(values), "Array data must be non-null");
            byte[] byteArray = ConvertBoolArrayToByteArray(values);
            if (num != 0)
            {
                short[] numArray = ReadMultipleRegisters(label, length);
                byte[] bytes = BitConverter.GetBytes(numArray[numArray.Length - 1]);
                if (num <= 8)
                {
                    byteArray[2 * size - 2] |= (byte)((bytes[0] >> num) << num);
                    byteArray[2 * size - 1] = bytes[1];
                }
                else if (num > 0)
                    byteArray[2 * size - 1] |= (byte)((bytes[1] >> (num - 8)) << (num - 8));
            }
            ++SendBuffer[2];
            byte[] bytes1 = BitConverter.GetBytes(52 + byteArray.Length);
            SendBuffer[19] = bytes1[0];
            SendBuffer[20] = bytes1[1];
            SendBuffer[45] = 0x14;
            SendBuffer[46] = 0x10; //0x016
            ++SendBuffer[47];
            SettingDevice(label, out SendBuffer[61], out SendBuffer[63], out SendBuffer[64], out SendBuffer[65]);
            SendBuffer[62] = 0x00;
            for (int index = 66; index <= 70; ++index)
                SendBuffer[index] = 0x00;
            byte[] bytes2 = BitConverter.GetBytes((byteArray.Length / 2) * 16);
            SendBuffer[71] = bytes2[0];
            SendBuffer[72] = bytes2[1];
            int count = 73 + byteArray.Length;
            int index1 = 0;
            for (int index2 = 73; index2 < count; ++index2)
            {
                SendBuffer[index2] = byteArray[index1];
                ++index1;
            }
            _stream.Write(SendBuffer, 0, count);
            _stream.Read(ReceveiBuffer, 0, 53);
        }
        /// <summary>
        /// Write text to the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="text">Text to be written.</param>
        public override void WriteText(string label, string text)
        {
            if (text.Length % 2 != 0)
            {
                SendBuffer[51] = 0x00;
                SendBuffer[52] = 0x00;
                ++SendBuffer[2];
                SendBuffer[19] = 0x42;
                SendBuffer[20] = 0x00;
                SendBuffer[45] = 0x04;
                SendBuffer[46] = 0x12; //0x018
                ++SendBuffer[47];
                SendBuffer[61] = 0x01;
                for (int i = 62; i <= 72; i++)
                {
                    SendBuffer[i] = 0x00;
                }
                SendBuffer[68] = 0x01;
                int num1 = text.Length / 2;
                SettingDevice(label, out SendBuffer[71], out SendBuffer[73], out SendBuffer[74], out SendBuffer[75]);
                int num2 = num1 + (SendBuffer[73] | SendBuffer[74] << 8 | SendBuffer[75] << 16);
                byte[] bytes = BitConverter.GetBytes(num2);
                SendBuffer[73] = bytes[0];
                SendBuffer[74] = bytes[1];
                SendBuffer[75] = bytes[2];

                for (int index = 76; index <= 86; ++index)
                    SendBuffer[index] = 0x00;
                _stream.Write(SendBuffer, 0, 87);
                _stream.Read(ReceveiBuffer, 0, 57);
                text += Convert.ToChar(ReceveiBuffer[56]).ToString();
            }

            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            ++SendBuffer[2];
            text = text.Length % 2 == 0 ? text : text + " ";
            int length = text.Length;
            byte[] bytes1 = BitConverter.GetBytes(52 + length);
            SendBuffer[19] = bytes1[0];
            SendBuffer[20] = bytes1[1];
            SendBuffer[45] = 0x14;
            SendBuffer[46] = 0x10; //0x016
            ++SendBuffer[47];
            SettingDevice(label, out SendBuffer[61], out SendBuffer[63], out SendBuffer[64], out SendBuffer[65]);
            SendBuffer[62] = 0x00;
            for (int index = 66; index <= 70; ++index)
                SendBuffer[index] = 0x00;
            byte[] bytes2 = BitConverter.GetBytes(length / 2);
            SendBuffer[71] = bytes2[0];
            SendBuffer[72] = bytes2[1];
            byte[] bytes3 = Encoding.ASCII.GetBytes(text);
            Array.Copy(bytes3, 0, SendBuffer, 73, bytes3.Length);
            _stream.Write(SendBuffer, 0, 73 + length);
            _stream.Read(ReceveiBuffer, 0, 512);
        }
        /// <summary>
        /// Write multiple values to the server randomly.
        /// </summary>
        /// <param name="words"><see cref="dotPLC.Mitsubishi.Types.Word"/> values to be written.</param>
        public override void WriteDeviceRandom(params Word[] words)
        {
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            int length = words.Length;
            ++SendBuffer[2];
            byte[] bytes1 = BitConverter.GetBytes(48 + length * 16 + length * 2);
            SendBuffer[19] = bytes1[0];
            SendBuffer[20] = bytes1[1];
            SendBuffer[45] = 0x14;
            SendBuffer[46] = 0x11;
            ++SendBuffer[47];
            SendBuffer[61] = (byte)length;
            SendBuffer[62] = 0x00;
            for (int index = 63; index <= 68; ++index)
                SendBuffer[index] = 0x00;
            SendBuffer[66] = (byte)length;
            int index1 = 69;
            for (int index2 = 0; index2 < length; ++index2)
            {
                SettingDevice(words[index2].Label, out SendBuffer[index1], out SendBuffer[index1 + 2], out SendBuffer[index1 + 3], out SendBuffer[index1 + 4]);
                SendBuffer[index1 + 1] = 0x00;
                for (int index3 = index1 + 5; index3 <= index1 + 15; ++index3)
                    SendBuffer[index3] = 0x00;
                index1 += 16;
            }
            int index4 = index1;
            for (int index5 = 0; index5 < length; ++index5)
            {
                byte[] bytes2 = BitConverter.GetBytes(words[index5].Value);
                SendBuffer[index4] = bytes2[0];
                SendBuffer[index4 + 1] = bytes2[1];
                index4 += 2;
            }
            int count1 = 69 + length * 16 + length * 2;
            int count2 = 53;
            _stream.Write(SendBuffer, 0, count1);
            _stream.Read(ReceveiBuffer, 0, count2);
        }
        /// <summary>
        /// Write multiple values to the server randomly.
        /// </summary>
        /// <param name="dwords"><see cref="dotPLC.Mitsubishi.Types.DWord"/> values to be written.</param>
        public override void WriteDeviceRandom(params DWord[] dwords)
        {
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            int length = dwords.Length;
            ++SendBuffer[2];
            byte[] bytes1 = BitConverter.GetBytes(48 + length * 2 * 16 + length * 2 * 2);
            SendBuffer[19] = bytes1[0];
            SendBuffer[20] = bytes1[1];
            SendBuffer[45] = 0x14;
            SendBuffer[46] = 0x11;
            ++SendBuffer[47];
            SendBuffer[61] = (byte)(length * 2);
            SendBuffer[62] = 0x00;
            for (int index = 63; index <= 68; ++index)
                SendBuffer[index] = 0x00;
            SendBuffer[66] = (byte)(length * 2);
            int index1 = 69;
            for (int index2 = 0; index2 < length; ++index2)
            {
                byte device;
                int num = SettingDevice(dwords[index2].Label, out device, out SendBuffer[index1 + 2], out SendBuffer[index1 + 3], out SendBuffer[index1 + 4]);
                SendBuffer[index1] = device;
                SendBuffer[index1 + 1] = 0x00;
                for (int index3 = index1 + 5; index3 <= index1 + 15; ++index3)
                    SendBuffer[index3] = 0x00;
                int index4 = index1 + 16;
                SendBuffer[index4] = device;
                SendBuffer[index4 + 1] = 0x00;
                byte[] bytes2 = BitConverter.GetBytes(num + 1);
                SendBuffer[index4 + 2] = bytes2[0];
                SendBuffer[index4 + 3] = bytes2[1];
                SendBuffer[index4 + 4] = bytes2[2];
                for (int index5 = index4 + 5; index5 <= index4 + 15; ++index5)
                    SendBuffer[index5] = 0x00;
                index1 = index4 + 16;
            }
            int num1 = index1;
            for (int index6 = 0; index6 < length; ++index6)
            {
                byte[] bytes3 = BitConverter.GetBytes(dwords[index6].Value);
                for (int index7 = 0; index7 < 4; ++index7)
                    SendBuffer[num1 + index7] = bytes3[index7];
                num1 += 4;
            }
            int count1 = 69 + length * 2 * 16 + length * 2 * 2;
            int count2 = 53;
            _stream.Write(SendBuffer, 0, count1);
            _stream.Read(ReceveiBuffer, 0, count2);
        }
        /// <summary>
        /// Write multiple values to the server randomly.
        /// </summary>
        /// <param name="floats"><see cref="dotPLC.Mitsubishi.Types.Float"/> values to be written.</param>
        public override void WriteDeviceRandom(params Float[] floats)
        {
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            int length = floats.Length;
            ++SendBuffer[2];
            byte[] bytes1 = BitConverter.GetBytes(48 + length * 2 * 16 + length * 2 * 2);
            SendBuffer[19] = bytes1[0];
            SendBuffer[20] = bytes1[1];
            SendBuffer[45] = 0x14;
            SendBuffer[46] = 0x11;
            ++SendBuffer[47];
            SendBuffer[61] = (byte)(length * 2);
            SendBuffer[62] = 0x00;
            for (int index = 63; index <= 68; ++index)
                SendBuffer[index] = 0x00;
            SendBuffer[66] = (byte)(length * 2);
            int index1 = 69;
            for (int index2 = 0; index2 < length; ++index2)
            {
                byte device;
                int num = SettingDevice(floats[index2].Label, out device, out SendBuffer[index1 + 2], out SendBuffer[index1 + 3], out SendBuffer[index1 + 4]);
                SendBuffer[index1] = device;
                SendBuffer[index1 + 1] = 0x00;
                for (int index3 = index1 + 5; index3 <= index1 + 15; ++index3)
                    SendBuffer[index3] = 0x00;
                int index4 = index1 + 16;
                SendBuffer[index4] = device;
                SendBuffer[index4 + 1] = 0x00;
                byte[] bytes2 = BitConverter.GetBytes(num + 1);
                SendBuffer[index4 + 2] = bytes2[0];
                SendBuffer[index4 + 3] = bytes2[1];
                SendBuffer[index4 + 4] = bytes2[2];
                for (int index5 = index4 + 5; index5 <= index4 + 15; ++index5)
                    SendBuffer[index5] = 0x00;
                index1 = index4 + 16;
            }
            int num1 = index1;
            for (int index6 = 0; index6 < length; ++index6)
            {
                byte[] bytes3 = BitConverter.GetBytes(floats[index6].Value);
                for (int index7 = 0; index7 < 4; ++index7)
                    SendBuffer[num1 + index7] = bytes3[index7];
                num1 += 4;
            }
            int count1 = 69 + length * 2 * 16 + length * 2 * 2;
            int count2 = 53;
            _stream.Write(SendBuffer, 0, count1);
            _stream.Read(ReceveiBuffer, 0, count2);
        }
        /// <summary>
        /// Write multiple values to the server randomly.
        /// </summary>
        /// <param name="bits"><see cref="dotPLC.Mitsubishi.Types.Bit"/> values to be written.</param>
        public override void WriteDeviceRandom(params Bit[] bits)
        {
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            int length = bits.Length;
            int num = (length % 16 == 0 ? length : length + 16 - length % 16) / 16;
            ++SendBuffer[2];
            byte[] bytes = BitConverter.GetBytes(48 + length * 16 + num * 2);
            SendBuffer[19] = bytes[0];
            SendBuffer[20] = bytes[1];
            SendBuffer[45] = 0x14;
            SendBuffer[46] = 0x11;
            ++SendBuffer[47];
            SendBuffer[61] = 0x00;
            SendBuffer[62] = (byte)length;
            for (int index = 63; index <= 68; ++index)
                SendBuffer[index] = 0x00;
            SendBuffer[66] = (byte)length;
            int index1 = 69;
            for (int index2 = 0; index2 < length; ++index2)
            {
                SettingDevice(bits[index2].Label, out SendBuffer[index1], out SendBuffer[index1 + 2], out SendBuffer[index1 + 3], out SendBuffer[index1 + 4]);
                SendBuffer[index1 + 1] = 0x00;
                for (int index3 = index1 + 5; index3 <= index1 + 15; ++index3)
                    SendBuffer[index3] = 0x00;
                index1 += 16;
            }
            int destinationIndex = index1;
            bool[] coils = new bool[length];
            for (int index4 = 0; index4 < length; ++index4)
                coils[index4] = bits[index4].Value;
            Array.Copy(ConvertBoolArrayToByteArray(coils), 0, SendBuffer, destinationIndex, num * 2);
            int count1 = 69 + length * 16 + num * 2;
            _stream.Write(SendBuffer, 0, count1);
            _stream.Read(ReceveiBuffer, 0, 53);
        }
        /// <summary>
        /// Write multiple values to the server randomly.
        /// </summary>
        /// <param name="bits"><see cref="dotPLC.Mitsubishi.Types.Bit"/> values to be written.</param>
        private void WriteDeviceRandomSub(params Bit[] bits)
        {
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            int length = bits.Length;
            int num = (length % 16 == 0 ? length : length + 16 - length % 16) / 16;
            ++SendBuffer[2];
            byte[] bytes = BitConverter.GetBytes(48 + length * 16 + num * 2);
            SendBuffer[19] = bytes[0];
            SendBuffer[20] = bytes[1];
            SendBuffer[45] = 0x14;
            SendBuffer[46] = 0x11;
            ++SendBuffer[47];
            SendBuffer[61] = 0x00;
            SendBuffer[62] = (byte)length;
            for (int index = 63; index <= 68; ++index)
                SendBuffer[index] = 0x00;
            SendBuffer[66] = (byte)length;
            int index1 = 69;
            for (int index2 = 0; index2 < length; ++index2)
            {
                SettingDevice(bits[index2].Label, out SendBuffer[index1], out SendBuffer[index1 + 2], out SendBuffer[index1 + 3], out SendBuffer[index1 + 4]);
                SendBuffer[index1 + 1] = 0x00;
                for (int index3 = index1 + 5; index3 <= index1 + 15; ++index3)
                    SendBuffer[index3] = 0x00;
                index1 += 16;
            }
            int destinationIndex = index1;
            bool[] coils = new bool[length];
            for (int index4 = 0; index4 < length; ++index4)
                coils[index4] = bits[index4].Value;
            Array.Copy(ConvertBoolArrayToByteArray(coils), 0, SendBuffer, destinationIndex, num * 2);
            int count1 = 69 + length * 16 + num * 2;
            _stream.Write(SendBuffer, 0, count1);
            _stream.Read(ReceveiBuffer, 0, 53);
        }
        /// <summary>
        /// Write multiple values to the server randomly.
        /// </summary>
        /// <param name="bits"><see cref="dotPLC.Mitsubishi.Types.Bit"/> values to be written.</param>
        /// <param name="words"><see cref="dotPLC.Mitsubishi.Types.Word"/> values to be written.</param>
        /// <param name="dwords"><see cref="dotPLC.Mitsubishi.Types.DWord"/> values to be written.</param>
        /// <param name="floats"><see cref="dotPLC.Mitsubishi.Types.Float"/> values to be written.</param>
        public void WriteDeviceRandom(Bit[] bits = null, Word[] words = null, DWord[] dwords = null, Float[] floats = null)
        {
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            int length = bits == null ? 0 : bits.Length;
            int num1 = words == null ? 0 : words.Length;
            int num2 = dwords == null ? 0 : dwords.Length;
            int num3 = floats == null ? 0 : floats.Length;
            int num4 = num1 + num2 * 2 + num3 * 2;
            if (length + num4 == 0)
                return;
            if (length > 0 && num4 == 0)
            {
                WriteDeviceRandomSub(bits);
                return;
            }
            else
            {
                int num5 = (length % 16 == 0 ? length : length + 16 - length % 16) / 16;
                ++SendBuffer[2];
                byte[] bytes1 = BitConverter.GetBytes((length != 0 ? (length + num4) * 16 + (num4 + num5) * 2 + 4 : num4 * 16 + num4 * 2) + 48);
                SendBuffer[19] = bytes1[0];
                SendBuffer[20] = bytes1[1];
                SendBuffer[45] = 0x14;
                SendBuffer[46] = 0x11;
                ++SendBuffer[47];
                SendBuffer[61] = (byte)num4;
                SendBuffer[62] = (byte)length;
                for (int index = 63; index <= 68; ++index)
                    SendBuffer[index] = 0x00;
                SendBuffer[66] = (byte)num4;
                int index1 = 69;
                int index2 = 69 + num1 * 16;
                int index3 = index2 + num2 * 32;
                int index4 = index3 + num3 * 32;
                if (num1 > 0)
                {
                    for (int index5 = 0; index5 < num1; ++index5)
                    {
                        SettingDevice(words[index5].Label, out SendBuffer[index1], out SendBuffer[index1 + 2], out SendBuffer[index1 + 3], out SendBuffer[index1 + 4]);
                        SendBuffer[index1 + 1] = 0x00;
                        for (int index6 = index1 + 5; index6 <= index1 + 15; ++index6)
                            SendBuffer[index6] = 0x00;
                        index1 += 16;
                    }
                }
                if (num2 > 0)
                {
                    for (int index7 = 0; index7 < num2; ++index7)
                    {
                        byte device;
                        int num6 = SettingDevice(dwords[index7].Label, out device, out SendBuffer[index2 + 2], out SendBuffer[index2 + 3], out SendBuffer[index2 + 4]);
                        SendBuffer[index2] = device;
                        SendBuffer[index2 + 1] = 0x00;
                        for (int index8 = index2 + 5; index8 <= index2 + 15; ++index8)
                            SendBuffer[index8] = 0x00;
                        int index9 = index2 + 16;
                        SendBuffer[index9] = device;
                        SendBuffer[index9 + 1] = 0x00;
                        byte[] bytes2 = BitConverter.GetBytes(num6 + 1);
                        SendBuffer[index9 + 2] = bytes2[0];
                        SendBuffer[index9 + 3] = bytes2[1];
                        SendBuffer[index9 + 4] = bytes2[2];
                        for (int index10 = index9 + 5; index10 <= index9 + 15; ++index10)
                            SendBuffer[index10] = 0x00;
                        index2 = index9 + 16;
                    }
                }
                if (num3 > 0)
                {
                    for (int index11 = 0; index11 < num3; ++index11)
                    {
                        byte device;
                        int num7 = SettingDevice(floats[index11].Label, out device, out SendBuffer[index3 + 2], out SendBuffer[index3 + 3], out SendBuffer[index3 + 4]);
                        SendBuffer[index3] = device;
                        SendBuffer[index3 + 1] = 0x00;
                        for (int index12 = index3 + 5; index12 <= index3 + 15; ++index12)
                            SendBuffer[index12] = 0x00;
                        int index13 = index3 + 16;
                        SendBuffer[index13] = device;
                        SendBuffer[index13 + 1] = 0x00;
                        byte[] bytes3 = BitConverter.GetBytes(num7 + 1);
                        SendBuffer[index13 + 2] = bytes3[0];
                        SendBuffer[index13 + 3] = bytes3[1];
                        SendBuffer[index13 + 4] = bytes3[2];
                        for (int index14 = index13 + 5; index14 <= index13 + 15; ++index14)
                            SendBuffer[index14] = 0x00;
                        index3 = index13 + 16;
                    }
                }
                if (length > 0 && num4 > 0)
                {
                    SendBuffer[index4] = 0x00;
                    SendBuffer[index4 + 1] = (byte)length;
                    SendBuffer[index4 + 2] = 0x00;
                    SendBuffer[index4 + 3] = 0x00;
                    index4 += 4;
                    for (int index15 = 0; index15 < length; ++index15)
                    {
                        SettingDevice(bits[index15].Label, out SendBuffer[index4], out SendBuffer[index4 + 2], out SendBuffer[index4 + 3], out SendBuffer[index4 + 4]);
                        SendBuffer[index4 + 1] = 0x00;
                        for (int index16 = index4 + 5; index16 <= index4 + 15; ++index16)
                            SendBuffer[index16] = 0x00;
                        index4 += 16;
                    }
                }
                int destinationIndex = index4;
                if (num1 > 0)
                {
                    for (int index17 = 0; index17 < num1; ++index17)
                    {
                        byte[] bytes4 = BitConverter.GetBytes(words[index17].Value);
                        SendBuffer[destinationIndex] = bytes4[0];
                        SendBuffer[destinationIndex + 1] = bytes4[1];
                        destinationIndex += 2;
                    }
                }
                if (num2 > 0)
                {
                    for (int index18 = 0; index18 < num2; ++index18)
                    {
                        byte[] bytes5 = BitConverter.GetBytes(dwords[index18].Value);
                        for (int index19 = 0; index19 < 4; ++index19)
                            SendBuffer[destinationIndex + index19] = bytes5[index19];
                        destinationIndex += 4;
                    }
                }
                if (num3 > 0)
                {
                    for (int index20 = 0; index20 < num3; ++index20)
                    {
                        byte[] bytes6 = BitConverter.GetBytes(floats[index20].Value);
                        for (int index21 = 0; index21 < 4; ++index21)
                            SendBuffer[destinationIndex + index21] = bytes6[index21];
                        destinationIndex += 4;
                    }
                }
                if (length > 0)
                {
                    bool[] coils = new bool[length];
                    for (int index22 = 0; index22 < length; ++index22)
                        coils[index22] = bits[index22].Value;
                    Array.Copy(ConvertBoolArrayToByteArray(coils), 0, SendBuffer, destinationIndex, num5 * 2);
                }
                int count1 = destinationIndex + num5 * 2;
                int count2 = 53;
                _stream.Write(SendBuffer, 0, count1);
                _stream.Read(ReceveiBuffer, 0, count2);
            }
        }
        /// <summary>
        /// Read a single value from the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <returns>Returned value.</returns>
        internal override bool ReadSingleCoil(string label)
        {
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            ++SendBuffer[2];
            SendBuffer[19] = 0x42;
            SendBuffer[20] = 0x00;
            SendBuffer[45] = 0x04;
            SendBuffer[46] = 0x12; //0x018
            ++SendBuffer[47];
            SendBuffer[61] = 0x00;
            SendBuffer[62] = 0x01;
            SendBuffer[63] = 0x00;
            SendBuffer[64] = 0x00;
            SendBuffer[66] = 0x00;
            SendBuffer[68] = 0x01;
            SendBuffer[69] = 0x00;
            SendBuffer[70] = 0x00;
            SettingDevice(label, out SendBuffer[71], out SendBuffer[73], out SendBuffer[74], out SendBuffer[75]);
            SendBuffer[72] = 0x00;
            for (int index = 76; index <= 86; ++index)
                SendBuffer[index] = 0x00;
            _stream.Write(SendBuffer, 0, 87);
            _stream.Read(ReceveiBuffer, 0, 512);
            return ReceveiBuffer[55] == 0x01;
        }
        /// <summary>
        /// Read a single value from the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <returns>Returned value.</returns>
        internal override short ReadSingleRegister(string label)
        {
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            ++SendBuffer[2];
            SendBuffer[19] = 0x42;
            SendBuffer[20] = 0x00;
            SendBuffer[45] = 0x04;
            SendBuffer[46] = 0x12; //0x018
            ++SendBuffer[47];
            SendBuffer[61] = 0x01;
            for (int i = 62; i <= 72; i++)
            {
                SendBuffer[i] = 0x00;
            }
            SendBuffer[68] = 0x01;
            SettingDevice(label, out SendBuffer[71], out SendBuffer[73], out SendBuffer[74], out SendBuffer[75]);
            for (int index = 76; index <= 86; ++index)
                SendBuffer[index] = 0x00;
            _stream.Write(SendBuffer, 0, 87);
            _stream.Read(ReceveiBuffer, 0, 57);
            return BitConverter.ToInt16(ReceveiBuffer, 55);
        }
        /// <summary>
        /// Read a single value from the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <returns>Returned value.</returns>
        internal override int ReadSingleDouble(string label)
        {
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            ++SendBuffer[2];
            SendBuffer[19] = 0x34; //(byte)52;
            SendBuffer[20] = 0x00;
            SendBuffer[45] = 0x04;
            SendBuffer[46] = 0x10; //0x016
            ++SendBuffer[47];
            SettingDevice(label, out SendBuffer[61], out SendBuffer[63], out SendBuffer[64], out SendBuffer[65]);
            SendBuffer[62] = 0x00;
            for (int index = 66; index <= 70; ++index)
                SendBuffer[index] = 0x00;
            SendBuffer[71] = 0x02;
            SendBuffer[72] = 0x00;
            _stream.Write(SendBuffer, 0, 73);
            _stream.Read(ReceveiBuffer, 0, 57);
            return BitConverter.ToInt32(ReceveiBuffer, 53);
        }
        /// <summary>
        /// Read a single value from the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <returns>Returned value.</returns>
        internal override float ReadSingleFloat(string label)
        {
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            ++SendBuffer[2];
            SendBuffer[19] = 0x34; //(byte)52;
            SendBuffer[20] = 0x00;
            SendBuffer[45] = 0x04;
            SendBuffer[46] = 0x10; //0x016
            ++SendBuffer[47];
            SettingDevice(label, out SendBuffer[61], out SendBuffer[63], out SendBuffer[64], out SendBuffer[65]);
            SendBuffer[62] = 0x00;
            for (int index = 66; index <= 70; ++index)
                SendBuffer[index] = 0x00;
            SendBuffer[71] = 0x02;
            SendBuffer[72] = 0x00;
            _stream.Write(SendBuffer, 0, 73);
            _stream.Read(ReceveiBuffer, 0, 57);
            return BitConverter.ToSingle(ReceveiBuffer, 53);
        }
        /// <summary>
        /// Read multiple values from the server in a batch.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returned values.</returns>
        internal override bool[] ReadMultipleCoils(string label, int size) => ConvertMultipleWordToBoolArray(ReadMultipleRegisters(label, size), size);
        /// <summary>
        /// Read multiple values from the server in a batch.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returned values.</returns>
        internal override short[] ReadMultipleRegisters(string label, int size)
        {
            ++SendBuffer[2];
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            SendBuffer[19] = 0x34; //(byte)52;
            SendBuffer[20] = 0x00;
            SendBuffer[45] = 0x04;
            SendBuffer[46] = 0x10; //0x016
            ++SendBuffer[47];
            SettingDevice(label, out SendBuffer[61], out SendBuffer[63], out SendBuffer[64], out SendBuffer[65]);
            SendBuffer[62] = 0x00;
            for (int index = 66; index <= 70; ++index)
                SendBuffer[index] = 0x00;
            if (IsCoil(SendBuffer[61]))
            {
                int num = size % 16 == 0 ? size : size + 16 - size % 16;
                byte[] bytes = BitConverter.GetBytes(num);
                int length = num / 16;
                SendBuffer[71] = bytes[0];
                SendBuffer[72] = bytes[1];
                _stream.Write(SendBuffer, 0, 73);
                _stream.Read(ReceveiBuffer, 0, 53 + length * 2);
                short[] numArray = new short[length];
                for (int index = 0; index < length * 2; index += 2)
                    numArray[index / 2] = BitConverter.ToInt16(ReceveiBuffer, 53 + index);
                return numArray;
            }
            byte[] bytes1 = BitConverter.GetBytes(size);
            SendBuffer[71] = bytes1[0];
            SendBuffer[72] = bytes1[1];
            _stream.Write(SendBuffer, 0, 73);
            _stream.Read(ReceveiBuffer, 0, 53 + size * 2);
            short[] numArray1 = new short[size];
            for (int index = 0; index < size * 2; index += 2)
                numArray1[index / 2] = BitConverter.ToInt16(ReceveiBuffer, 53 + index);
            return numArray1;
        }
        /// <summary>
        /// Read multiple values from the server in a batch.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returned values.</returns>
        internal override int[] ReadMultipleDoubles(string label, int size)
        {
            ++SendBuffer[2];
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            SendBuffer[19] = 0x34; //(byte)52;
            SendBuffer[20] = 0x00;
            SendBuffer[45] = 0x04;
            SendBuffer[46] = 0x10; //0x016
            ++SendBuffer[47];
            SettingDevice(label, out SendBuffer[61], out SendBuffer[63], out SendBuffer[64], out SendBuffer[65]);
            SendBuffer[62] = 0x00;
            for (int index = 66; index <= 70; ++index)
                SendBuffer[index] = 0x00;
            byte[] bytes = BitConverter.GetBytes(size * 2);
            SendBuffer[71] = bytes[0];
            SendBuffer[72] = bytes[1];
            int num = 52 + size * 2;
            _stream.Write(SendBuffer, 0, 73);
            _stream.Read(ReceveiBuffer, 0, 53 + size * 4);
            int[] numArray = new int[size];
            for (int index = 0; index < size * 4; index += 4)
                numArray[index / 4] = BitConverter.ToInt32(ReceveiBuffer, 53 + index);
            return numArray;
        }
        /// <summary>
        /// Read multiple values from the server in a batch.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returned values.</returns>
        internal override float[] ReadMultipleFloats(string label, int size)
        {
            ++SendBuffer[2];
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            SendBuffer[19] = 0x34; //(byte)52;
            SendBuffer[20] = 0x00;
            SendBuffer[45] = 0x04;
            SendBuffer[46] = 0x10; //0x016
            ++SendBuffer[47];
            SettingDevice(label, out SendBuffer[61], out SendBuffer[63], out SendBuffer[64], out SendBuffer[65]);
            SendBuffer[62] = 0x00;
            for (int index = 66; index <= 70; ++index)
                SendBuffer[index] = 0x00;
            byte[] bytes = BitConverter.GetBytes(size * 2);
            SendBuffer[71] = bytes[0];
            SendBuffer[72] = bytes[1];
            int num = 52 + size * 2;
            _stream.Write(SendBuffer, 0, 73);
            _stream.Read(ReceveiBuffer, 0, 53 + size * 4);
            float[] numArray = new float[size];
            for (int index = 0; index < size * 4; index += 4)
                numArray[index / 4] = BitConverter.ToSingle(ReceveiBuffer, 53 + index);
            return numArray;
        }
        /// <summary>
        /// Read text from the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of text to be read.</param>
        /// <returns>Returns text of the specified size.</returns>
        public override string ReadText(string label, int size)
        {
            ++SendBuffer[2];
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            SendBuffer[19] = 0x34; //(byte)52;
            SendBuffer[20] = 0x00;
            SendBuffer[45] = 0x04;
            SendBuffer[46] = 0x10; //0x016
            ++SendBuffer[47];
            SettingDevice(label, out SendBuffer[61], out SendBuffer[63], out SendBuffer[64], out SendBuffer[65]);
            SendBuffer[62] = 0x00;
            for (int index = 66; index <= 70; ++index)
                SendBuffer[index] = 0x00;
            int count = size;
            if (size % 2 != 0)
                ++size;
            byte[] bytes = BitConverter.GetBytes(size / 2);
            SendBuffer[71] = bytes[0];
            SendBuffer[72] = bytes[1];
            int num = 52 + size;
            _stream.Write(SendBuffer, 0, 73);
            _stream.Read(ReceveiBuffer, 0, 53 + size);
            string empty = string.Empty;
            return count % 2 != 0 ? Encoding.ASCII.GetString(ReceveiBuffer, 53, count) : Encoding.ASCII.GetString(ReceveiBuffer, 53, size);
        }
        /// <summary>
        /// Read the model character string of the server.
        /// </summary>
        /// <returns>The model character string of the server.</returns>
        public override string GetCpuName()
        {
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            ++SendBuffer[2];
            SendBuffer[19] = 0x20;
            SendBuffer[20] = 0x00;
            SendBuffer[45] = 0x01;
            SendBuffer[46] = 0x21;
            ++SendBuffer[47];
            SendBuffer[52] = 0x01;
            _stream.Write(SendBuffer, 0, 53);
            _stream.Read(ReceveiBuffer, 0, 77);
            return Encoding.ASCII.GetString(ReceveiBuffer, 53, 16);
        }
        /// <summary>
        /// Read multiple values from the server randomly.
        /// </summary>
        /// <param name="bits"><see cref="dotPLC.Mitsubishi.Types.Bit"/> values to be read.</param>
        public override void ReadDeviceRandom(params Bit[] bits)
        {
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            ++SendBuffer[2];
            int length = bits.Length;
            byte[] bytes = BitConverter.GetBytes(50 + length * 16);
            SendBuffer[19] = bytes[0];
            SendBuffer[20] = bytes[1];
            SendBuffer[45] = 0x04;
            SendBuffer[46] = 0x12; //0x018
            ++SendBuffer[47];
            SendBuffer[61] = 0x00;
            SendBuffer[62] = (byte)length;
            for (int index = 63; index <= 70; ++index)
                SendBuffer[index] = 0x00;
            SendBuffer[68] = (byte)length;
            int index1 = 71;
            for (int index2 = 0; index2 < length; ++index2)
            {
                SettingDevice(bits[index2].Label, out SendBuffer[index1], out SendBuffer[index1 + 2], out SendBuffer[index1 + 3], out SendBuffer[index1 + 4]);
                SendBuffer[index1 + 1] = 0x00;
                for (int index3 = index1 + 5; index3 <= index1 + 15; ++index3)
                    SendBuffer[index3] = 0x00;
                index1 += 16;
            }
            int count1 = 71 + length * 16;
            int num = (length % 16 == 0 ? length : length + 16 - length % 16) / 16;
            int count2 = 55 + num * 2;
            _stream.Write(SendBuffer, 0, count1);
            _stream.Read(ReceveiBuffer, 0, count2);
            bool[] boolArray = ConvertMultipleByteToBoolArray(ReceveiBuffer, 55, num * 2, length);
            for (int index4 = 0; index4 < length; ++index4)
                bits[index4].Value = boolArray[index4];
        }
        /// <summary>
        /// Read multiple values from the server randomly.
        /// </summary>
        /// <param name="words"><see cref="dotPLC.Mitsubishi.Types.Word"/> values to be read.</param>
        public override void ReadDeviceRandom(params Word[] words)
        {
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            ++SendBuffer[2];
            int length = words.Length;
            byte[] bytes = BitConverter.GetBytes(50 + length * 16);
            SendBuffer[19] = bytes[0];
            SendBuffer[20] = bytes[1];
            SendBuffer[45] = 0x04;
            SendBuffer[46] = 0x12; //0x018
            ++SendBuffer[47];
            SendBuffer[61] = (byte)length;
            SendBuffer[62] = 0x00;
            for (int index = 63; index <= 70; ++index)
                SendBuffer[index] = 0x00;
            SendBuffer[68] = (byte)length;
            int index1 = 71;
            for (int index2 = 0; index2 < length; ++index2)
            {
                SettingDevice(words[index2].Label, out SendBuffer[index1], out SendBuffer[index1 + 2], out SendBuffer[index1 + 3], out SendBuffer[index1 + 4]);
                SendBuffer[index1 + 1] = 0x00;
                for (int index3 = index1 + 5; index3 <= index1 + 15; ++index3)
                    SendBuffer[index3] = 0x00;
                index1 += 16;
            }
            int count1 = 71 + length * 16;
            int count2 = 55 + length * 2;
            _stream.Write(SendBuffer, 0, count1);
            _stream.Read(ReceveiBuffer, 0, count2);
            for (int index4 = 0; index4 < length * 2; index4 += 2)
                words[index4 / 2].Value = BitConverter.ToInt16(ReceveiBuffer, 55 + index4);
        }
        /// <summary>
        /// Read multiple values from the server randomly.
        /// </summary>
        /// <param name="dwords"><see cref="dotPLC.Mitsubishi.Types.DWord"/> values to be read.</param>
        public override void ReadDeviceRandom(params DWord[] dwords)
        {
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            ++SendBuffer[2];
            int length = dwords.Length;
            byte[] bytes1 = BitConverter.GetBytes(50 + length * 2 * 16);
            SendBuffer[19] = bytes1[0];
            SendBuffer[20] = bytes1[1];
            SendBuffer[45] = 0x04;
            SendBuffer[46] = 0x12; //0x018
            ++SendBuffer[47];
            SendBuffer[61] = (byte)(length * 2);
            SendBuffer[62] = 0x00;
            for (int index = 63; index <= 70; ++index)
                SendBuffer[index] = 0x00;
            SendBuffer[68] = (byte)(length * 2);
            int index1 = 71;
            for (int index2 = 0; index2 < length; ++index2)
            {
                byte device;
                int num = SettingDevice(dwords[index2].Label, out device, out SendBuffer[index1 + 2], out SendBuffer[index1 + 3], out SendBuffer[index1 + 4]);
                SendBuffer[index1] = device;
                SendBuffer[index1 + 1] = 0x00;
                for (int index3 = index1 + 5; index3 <= index1 + 15; ++index3)
                    SendBuffer[index3] = 0x00;
                int index4 = index1 + 16;
                SendBuffer[index4] = device;
                SendBuffer[index4 + 1] = 0x00;
                byte[] bytes2 = BitConverter.GetBytes(num + 1);
                SendBuffer[index4 + 2] = bytes2[0];
                SendBuffer[index4 + 3] = bytes2[1];
                SendBuffer[index4 + 4] = bytes2[2];
                for (int index5 = index4 + 5; index5 <= index4 + 15; ++index5)
                    SendBuffer[index5] = 0x00;
                index1 = index4 + 16;
            }
            int count1 = 71 + length * 2 * 16;
            int count2 = 55 + length * 2 * 2;
            _stream.Write(SendBuffer, 0, count1);
            _stream.Read(ReceveiBuffer, 0, count2);
            for (int index6 = 0; index6 < length * 4; index6 += 4)
                dwords[index6 / 4].Value = BitConverter.ToInt32(ReceveiBuffer, 55 + index6);
        }
        /// <summary>
        /// Read multiple values from the server randomly.
        /// </summary>
        /// <param name="floats"><see cref="dotPLC.Mitsubishi.Types.Float"/> values to be read.</param>
        public override void ReadDeviceRandom(params Float[] floats)
        {
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            ++SendBuffer[2];
            int length = floats.Length;
            byte[] bytes1 = BitConverter.GetBytes(50 + length * 2 * 16);
            SendBuffer[19] = bytes1[0];
            SendBuffer[20] = bytes1[1];
            SendBuffer[45] = 0x04;
            SendBuffer[46] = 0x12; //0x018
            ++SendBuffer[47];
            SendBuffer[61] = (byte)(length * 2);
            SendBuffer[62] = 0x00;
            for (int index = 63; index <= 70; ++index)
                SendBuffer[index] = 0x00;
            SendBuffer[68] = (byte)(length * 2);
            int index1 = 71;
            for (int index2 = 0; index2 < length; ++index2)
            {
                byte device;
                int num = SettingDevice(floats[index2].Label, out device, out SendBuffer[index1 + 2], out SendBuffer[index1 + 3], out SendBuffer[index1 + 4]);
                SendBuffer[index1] = device;
                SendBuffer[index1 + 1] = 0x00;
                for (int index3 = index1 + 5; index3 <= index1 + 15; ++index3)
                    SendBuffer[index3] = 0x00;
                int index4 = index1 + 16;
                SendBuffer[index4] = device;
                SendBuffer[index4 + 1] = 0x00;
                byte[] bytes2 = BitConverter.GetBytes(num + 1);
                SendBuffer[index4 + 2] = bytes2[0];
                SendBuffer[index4 + 3] = bytes2[1];
                SendBuffer[index4 + 4] = bytes2[2];
                for (int index5 = index4 + 5; index5 <= index4 + 15; ++index5)
                    SendBuffer[index5] = 0x00;
                index1 = index4 + 16;
            }
            int count1 = 71 + length * 2 * 16;
            int count2 = 55 + length * 2 * 2;
            _stream.Write(SendBuffer, 0, count1);
            _stream.Read(ReceveiBuffer, 0, count2);
            for (int index6 = 0; index6 < length * 4; index6 += 4)
                floats[index6 / 4].Value = BitConverter.ToSingle(ReceveiBuffer, 55 + index6);
        }
        /// <summary>
        /// Read multiple values from the server randomly. <see langword="[RECOMMENDED]"></see>
        /// </summary>
        /// <param name="bits"><see cref="dotPLC.Mitsubishi.Types.Bit"/> values to be read.</param>
        /// <param name="words"><see cref="dotPLC.Mitsubishi.Types.Word"/> values to be read.</param>
        /// <param name="dwords"><see cref="dotPLC.Mitsubishi.Types.DWord"/> values to be read.</param>
        /// <param name="floats"><see cref="dotPLC.Mitsubishi.Types.Float"/> values to be read.</param>
        public override void ReadDeviceRandom(Bit[] bits = null, Word[] words = null, DWord[] dwords = null, Float[] floats = null)
        {
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            int size = bits == null ? 0 : bits.Length;
            int num1 = words == null ? 0 : words.Length;
            int num2 = dwords == null ? 0 : dwords.Length;
            int num3 = floats == null ? 0 : floats.Length;
            int num4 = num1 + num2 * 2 + num3 * 2;
            if (size + num4 == 0)
                return;
            if (size > 0 && num4 == 0)
            {
                ReadDeviceRandom(bits);
            }
            else
            {
                byte[] bytes1 = BitConverter.GetBytes((size != 0 ? (size + num4) * 16 + 4 : num4 * 16) + 50);
                ++SendBuffer[2];
                SendBuffer[19] = bytes1[0];
                SendBuffer[20] = bytes1[1];
                SendBuffer[45] = 0x04;
                SendBuffer[46] = 0x12; //0x018
                ++SendBuffer[47];
                SendBuffer[61] = (byte)num4;
                SendBuffer[62] = (byte)size;
                for (int index = 63; index <= 70; ++index)
                    SendBuffer[index] = 0x00;
                SendBuffer[68] = num4 > 0 ? (byte)num4 : (byte)size;
                int index1 = 71;
                int index2 = 71 + num1 * 16;
                int index3 = index2 + num2 * 32;
                int index4 = index3 + num3 * 32;
                if (num1 > 0)
                {
                    for (int index5 = 0; index5 < num1; ++index5)
                    {
                        SettingDevice(words[index5].Label, out SendBuffer[index1], out SendBuffer[index1 + 2], out SendBuffer[index1 + 3], out SendBuffer[index1 + 4]);
                        SendBuffer[index1 + 1] = 0x00;
                        for (int index6 = index1 + 5; index6 <= index1 + 15; ++index6)
                            SendBuffer[index6] = 0x00;
                        index1 += 16;
                    }
                }
                if (num2 > 0)
                {
                    for (int index7 = 0; index7 < num2; ++index7)
                    {
                        byte device;
                        int num5 = SettingDevice(dwords[index7].Label, out device, out SendBuffer[index2 + 2], out SendBuffer[index2 + 3], out SendBuffer[index2 + 4]);
                        SendBuffer[index2] = device;
                        SendBuffer[index2 + 1] = 0x00;
                        for (int index8 = index2 + 5; index8 <= index2 + 15; ++index8)
                            SendBuffer[index8] = 0x00;
                        int index9 = index2 + 16;
                        SendBuffer[index9] = device;
                        SendBuffer[index9 + 1] = 0x00;
                        byte[] bytes2 = BitConverter.GetBytes(num5 + 1);
                        SendBuffer[index9 + 2] = bytes2[0];
                        SendBuffer[index9 + 3] = bytes2[1];
                        SendBuffer[index9 + 4] = bytes2[2];
                        for (int index10 = index9 + 5; index10 <= index9 + 15; ++index10)
                            SendBuffer[index10] = 0x00;
                        index2 = index9 + 16;
                    }
                }
                if (num3 > 0)
                {
                    for (int index11 = 0; index11 < num3; ++index11)
                    {
                        byte device;
                        int num6 = SettingDevice(floats[index11].Label, out device, out SendBuffer[index3 + 2], out SendBuffer[index3 + 3], out SendBuffer[index3 + 4]);
                        SendBuffer[index3] = device;
                        SendBuffer[index3 + 1] = 0x00;
                        for (int index12 = index3 + 5; index12 <= index3 + 15; ++index12)
                            SendBuffer[index12] = 0x00;
                        int index13 = index3 + 16;
                        SendBuffer[index13] = device;
                        SendBuffer[index13 + 1] = 0x00;
                        byte[] bytes3 = BitConverter.GetBytes(num6 + 1);
                        SendBuffer[index13 + 2] = bytes3[0];
                        SendBuffer[index13 + 3] = bytes3[1];
                        SendBuffer[index13 + 4] = bytes3[2];
                        for (int index14 = index13 + 5; index14 <= index13 + 15; ++index14)
                            SendBuffer[index14] = 0x00;
                        index3 = index13 + 16;
                    }
                }
                if (size > 0 && num4 > 0)
                {
                    SendBuffer[index4] = 0x00;
                    SendBuffer[index4 + 1] = (byte)size;
                    SendBuffer[index4 + 2] = 0x00;
                    SendBuffer[index4 + 3] = 0x00;
                    index4 += 4;
                    for (int index15 = 0; index15 < size; ++index15)
                    {
                        SettingDevice(bits[index15].Label, out SendBuffer[index4], out SendBuffer[index4 + 2], out SendBuffer[index4 + 3], out SendBuffer[index4 + 4]);
                        SendBuffer[index4 + 1] = 0x00;
                        for (int index16 = index4 + 5; index16 <= index4 + 15; ++index16)
                            SendBuffer[index16] = 0x00;
                        index4 += 16;
                    }
                }
                int count1 = index4;
                int num7 = (size % 16 == 0 ? size : size + 16 - size % 16) / 16;
                int count2 = 55 + (num7 + num4) * 2;
                _stream.Write(SendBuffer, 0, count1);
                _stream.Read(ReceveiBuffer, 0, count2);
                if (num1 > 0)
                {
                    for (int index17 = 0; index17 < num1 * 2; index17 += 2)
                        words[index17 / 2].Value = BitConverter.ToInt16(ReceveiBuffer, 55 + index17);
                }
                if (num2 > 0)
                {
                    for (int index18 = 0; index18 < num2 * 4; index18 += 4)
                        dwords[index18 / 4].Value = BitConverter.ToInt32(ReceveiBuffer, 55 + num1 * 2 + index18);
                }
                if (num3 > 0)
                {
                    for (int index19 = 0; index19 < num3 * 4; index19 += 4)
                        floats[index19 / 4].Value = BitConverter.ToSingle(ReceveiBuffer, 55 + num1 * 2 + num2 * 4 + index19);
                }
                if (size <= 0)
                    return;
                bool[] boolArray = ConvertMultipleByteToBoolArray(ReceveiBuffer, 55 + num1 * 2 + num2 * 4 + num3 * 4, num7 * 2, size);
                for (int index20 = 0; index20 < size; ++index20)
                    bits[index20].Value = boolArray[index20];
            }
        }
        /// <summary>
        /// Establish connection to GX Simulator3.
        /// </summary>
        public override void Connect()
        {
            ping = new Ping();
            if (ping.Send("127.0.0.1", 5000, data_ping).Status != IPStatus.Success)
                throw new Exception("Ping timeout");
            _tcpclient = new TcpClient();
            try
            {
                _tcpclient.ConnectAsync(IPAddress, Port).Wait(ReceiveTimeout);
            }
            catch
            {
                throw new Exception("Connect Timeout");
            }
            _stream = _tcpclient.GetStream();
            _connected = true;
            if (_tcpclient.Connected)
            {
                DataForConnect();
                for (int index = 0; index < ds_byte_to_connect.Count; ++index)
                {
                    _stream.Write(ds_byte_to_connect[index], 0, ds_byte_to_connect[index].Length);
                    _stream.Read(ReceveiBuffer, 0, 512);
                }
            }
        }
        /// <summary>
        /// Close connection to GX Simulator3.
        /// </summary>
        public override void Close()
        {
            if (_tcpclient != null)
                _tcpclient.Close();
            if (_stream != null)
                _stream.Close();
            _tcpclient = null;
            _connected = false;
            SetupBuffer();
        }
        /// <summary>
        /// To perform remote RUN/STOP/PAUSE of GX Simulator3.
        /// </summary>
        /// <param name="status">Specifies a <see cref="dotPLC.Mitsubishi.CpuStatus"></see> mode.</param>
        public void SetCpuStatus(CpuStatus status)
        {
            SendBuffer[51] = 0x00;
            SendBuffer[52] = 0x00;
            ++SendBuffer[2];
            SendBuffer[19] = 0x20;
            SendBuffer[20] = 0x00;
            SendBuffer[45] = 0x10; //0x016
            ++SendBuffer[47];
            SendBuffer[51] = 0x01;
            switch (status)
            {
                case CpuStatus.RUN:
                    SendBuffer[46] = 0x01;
                    SendBuffer[19] = 0x22;
                    _stream.Write(SendBuffer, 0, 55);
                    break;
                case CpuStatus.PAUSE:
                    SendBuffer[46] = 0x03;
                    _stream.Write(SendBuffer, 0, 53);
                    break;
                case CpuStatus.STOP:
                    SendBuffer[46] = 0x02;
                    _stream.Write(SendBuffer, 0, 53);
                    break;
                default:
                    return;
            }
            _stream.Read(ReceveiBuffer, 0, 53);
        }
        /// <summary>
        /// Setup byte ban đầu
        /// </summary>
        protected internal override void SetupBuffer()
        {
            SendBuffer[0] = 0x57;
            SendBuffer[1] = 0x01;
            SendBuffer[2] = 0x08;
            SendBuffer[5] = 0x11;
            SendBuffer[6] = 0x11;
            SendBuffer[7] = 0x07;
            SendBuffer[10] = byte.MaxValue;
            SendBuffer[11] = byte.MaxValue;
            SendBuffer[12] = 0x03;
            SendBuffer[15] = 0xFE;
            SendBuffer[16] = 0x03;
            SendBuffer[21] = 0x1C;
            SendBuffer[22] = 0xAA;
            SendBuffer[23] = 0x16;
            SendBuffer[24] = 0x14;
            SendBuffer[47] = 0x09;
        }
        /// <summary>
        /// Tách Label name
        /// </summary>
        /// <param name="label">Label name</param>
        /// <param name="device">device</param>
        /// <param name="low_num">byte[0]</param>
        /// <param name="mid_num">byte[1]</param>
        /// <param name="high_num">byte[2]</param>
        /// <returns>Return index of device.</returns>
        protected internal override int SettingDevice(string label, out byte device, out byte low_num, out byte mid_num, out byte high_num)
        {
            label = sWhitespace.Replace(label, "").ToUpper();
            int num1;
            if (label[0] == 'S' && label[1] == 'B')
            {
                label = label.Substring(2);
                device = 0x15; //0x021
                num1 = int.Parse(label, NumberStyles.HexNumber);
                byte[] bytes = BitConverter.GetBytes(num1);
                low_num = bytes[0];
                mid_num = bytes[1];
                high_num = bytes[2];
            }
            else if (label[0] == 'S' && label[1] == 'W')
            {
                label = label.Substring(2);
                device = 0x31;
                num1 = int.Parse(label, NumberStyles.HexNumber);
                byte[] bytes = BitConverter.GetBytes(num1);
                low_num = bytes[0];
                mid_num = bytes[1];
                high_num = bytes[2];
            }
            else if (label[0] == 'B')
            {
                label = label.Substring(1);
                device = 0x14;
                num1 = int.Parse(label, NumberStyles.HexNumber);
                byte[] bytes = BitConverter.GetBytes(num1);
                low_num = bytes[0];
                mid_num = bytes[1];
                high_num = bytes[2];
            }
            else if (label[0] == 'W')
            {
                label = label.Substring(1);
                device = 0x30; //0x048;
                num1 = int.Parse(label, NumberStyles.HexNumber);
                byte[] bytes = BitConverter.GetBytes(num1);
                low_num = bytes[0];
                mid_num = bytes[1];
                high_num = bytes[2];
            }
            else if (label[0] == 'X')
            {
                label = label.Substring(1);
                device = 0x10; //0x10; //0x016
                num1 = ConvertOctalToDecimal(int.Parse(label));
                byte[] bytes = BitConverter.GetBytes(num1);
                low_num = bytes[0];
                mid_num = bytes[1];
                high_num = bytes[2];
            }
            else if (label[0] == 'Y')
            {
                label = label.Substring(1);
                device = 0x11;
                num1 = ConvertOctalToDecimal(int.Parse(label));
                byte[] bytes = BitConverter.GetBytes(num1);
                low_num = bytes[0];
                mid_num = bytes[1];
                high_num = bytes[2];
            }
            else
            {
                int num2 = 0;
                for (int index = 0; index < label.Length; ++index)
                {
                    if (label[index] >= '0' && label[index] <= '9')
                    {
                        num2 = index;
                        break;
                    }
                }
                device = GetNameDevice(label.Substring(0, num2));
                num1 = int.Parse(label.Substring(num2));
                byte[] bytes = BitConverter.GetBytes(num1);
                low_num = bytes[0];
                mid_num = bytes[1];
                high_num = bytes[2];
            }
            return num1;
        }
        /// <summary>
        /// Lấy byte của tên device
        /// </summary>
        /// <param name="device">device name</param>
        /// <returns>byte của device</returns>
        internal override byte GetNameDevice(string device)
        {
            switch (device)
            {
                case "B":
                    return 20;
                case "BL":
                    return 114;
                case "CC":
                    return 68;
                case "CN":
                    return 70;
                case "CS":
                    return 69;
                case "D":
                    return 32;
                case "F":
                    return 4;
                case "L":
                    return 3;
                case "LCN":
                    return 86;
                case "LZ":
                    return 98;
                case "M":
                    return 1;
                case "R":
                    return 39;
                case "S":
                    return 8;
                case "SB":
                    return 21;
                case "SC":
                    return 72;
                case "SD":
                    return 33;
                case "SM":
                    return 2;
                case "SN":
                    return 74;
                case "SS":
                    return 73;
                case "STN":
                    return 74;
                case "SW":
                    return 49;
                case "TC":
                    return 64;
                case "TN":
                    return 66;
                case "TS":
                    return 65;
                case "W":
                    return 48;
                case "X":
                    return 16;
                case "Y":
                    return 17;
                case "Z":
                    return 96;
                default:
                    throw new ArgumentOutOfRangeException("The specified device does not belong to the memory of the PLC");
            }
        }
        /// <summary>
        /// Kiểm tra xem device có phải là Coil không
        /// </summary>
        /// <param name="device"></param>
        /// <returns>Trả về true nếu thành công;nếu không,false</returns>
        private bool IsCoil(byte device)
        {
            switch (device)
            {
                case 1:
                    return true;
                case 2:
                    return true;
                case 3:
                    return true;
                case 4:
                    return true;
                case 8:
                    return true;
                case 16:
                    return true;
                case 17:
                    return true;
                case 20:
                    return true;
                case 21:
                    return true;
                case 32:
                    return false;
                case 33:
                    return false;
                case 39:
                    return false;
                case 48:
                    return false;
                case 49:
                    return false;
                case 64:
                    return true;
                case 65:
                    return true;
                case 66:
                    return false;
                case 68:
                    return true;
                case 69:
                    return true;
                case 70:
                    return false;
                case 72:
                    return true;
                case 73:
                    return true;
                case 74:
                    return false;
                case 86:
                    return false;
                case 96:
                    return false;
                case 98:
                    return false;
                case 114:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException("The specified device does not belong to the memory of the PLC");
            }
        }
        /// <summary>
        /// Settup dữ liệu kết nối (10 lần)
        /// </summary>
        private void DataForConnect()
        {
            ds_byte_to_connect = new List<byte[]>();
            ds_string_byte = new List<string>()
      {
        str_Spacket_1_48,
        str_Spacket_2_97,
        str_Spacket_3_95,
        str_Spacket_4_117,
        str_Spacket_5_117,
        str_Spacket_6_117,
        str_Spacket_7_117,
        str_Spacket_8_117,
        str_Spacket_9_117,
        str_Spacket_10_113
      };
            foreach (string HexString in ds_string_byte)
                ds_byte_to_connect.Add(ToByteArray(HexString));
        }
        /// <summary>
        /// Chuyển chuỗi hex sang mảng byte
        /// </summary>
        /// <param name="HexString">chuỗi hex</param>
        /// <returns>Mảng byte</returns>
        public static byte[] ToByteArray(string HexString)
        {
            int length = HexString.Length;
            byte[] byteArray = new byte[length / 2];
            for (int startIndex = 0; startIndex < length; startIndex += 2)
                byteArray[startIndex / 2] = Convert.ToByte(HexString.Substring(startIndex, 2), 16);
            return byteArray;
        }
    }
}
