
using dotPLC.Mitsubishi;
using dotPLC.Mitsubishi.Types;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace dotPLC.Initial
{
    /// <summary>
    ///  Provides client connection for TCP network service.
    /// </summary>
    public abstract class Ethernet
    {
        /// <summary>
        /// _tcpclient
        /// </summary>
        internal TcpClient _tcpclient;
        /// <summary>
        /// _stream
        /// </summary>
        internal NetworkStream _stream;
        /// <summary>
        /// _connected
        /// </summary>
        internal bool _connected = false;
        /// <summary>
        /// _connected
        /// </summary>
        internal bool _isConnectStart = false;
        /// <summary>
        /// _receiveTimeout
        /// </summary>
        private int _receiveTimeout = 3000;
        /// <summary>
        /// _sendTimeout
        /// </summary>
        private int _sendTimeout = 3000;
        /// <summary>
        /// BufferSize
        /// </summary>
        internal const int BufferSize = 65536;
        /// <summary>
        /// SendBuffer
        /// </summary>
        protected internal byte[] SendBuffer = new byte[65536];
        /// <summary>
        /// ReceveiBuffer
        /// </summary>
        protected internal byte[] ReceveiBuffer = new byte[65536];
        /// <summary>
        /// Remove whitespace.
        /// </summary>
        internal static readonly Regex sWhitespace = new Regex("\\s+");
        /// <summary>
        /// Initializes a new instance of the <see cref="Ethernet"></see> class.
        /// </summary>
        public Ethernet() => SetupBuffer();
        /// <summary>
        /// IP Address of the server.
        /// </summary>
        public string IPAddress { get; set; } = "127.0.0.1";
        /// <summary>
        /// Gets a value indicating whether a connection to the server has been established.
        /// </summary>
        public bool Connected => _connected;
        /// <summary>
        /// Gets or sets the amount of time that a write operation blocks waiting for data to the server.
        /// </summary>
        public int ReceiveTimeout
        {
            get => _receiveTimeout;
            set
            {
                _receiveTimeout = value;
                if (_tcpclient == null)
                    return;
                _tcpclient.ReceiveTimeout = value;
            }
        }
        /// <summary>
        /// Gets or sets the amount of time that a read operation blocks waiting for data from the server.
        /// </summary>
        public int SendTimeout
        {
            get => _sendTimeout;
            set
            {
                _sendTimeout = value;
                if (_tcpclient == null)
                    return;
                _tcpclient.SendTimeout = value;
            }
        }
        /// <summary>
        /// Chuyển đổi octal thành decimal
        /// </summary>
        /// <param name="oct">số oct</param>
        /// <returns>số decimal</returns>
        internal static int ConvertOctalToDecimal(int oct)
        {
            int num = 0;
            int y = 0;
            while (oct > 0)
            {
                num += oct % 10 * (int)Math.Pow(8.0, (double)y);
                oct /= 10;
                ++y;
            }
            return num;
        }
        /// <summary>
        /// Chuyển đổi Octal thành Decimal
        /// </summary>
        /// <param name="dec">số dec</param>
        /// <returns>số oct</returns>
        internal static int ConvertDecimalToOctal(int dec)
        {
            int octal = 0;
            int num = 1;
            for (; dec != 0; dec /= 8)
            {
                octal += dec % 8 * num;
                num *= 10;
            }
            return octal;
        }
        /// <summary>
        /// Chuyển đổi 1 byte thành bit 
        /// </summary>
        /// <param name="b">giá trị của byte</param>
        /// <returns>8 bit</returns>
        internal static bool[] ConvertByteToBoolArray(byte b)
        {
            bool[] boolArray = new bool[8];
            for (int index = 0; index < 8; ++index)
                boolArray[index] = ((uint)b & (uint)(1 << index)) > 0U;
            Array.Reverse(boolArray);
            return boolArray;
        }
        /// <summary>
        /// Chuyển đổi word (2byte) thành 16bit
        /// </summary>
        /// <param name="word">word 2byte</param>
        /// <returns>16 bit</returns>
        internal static bool[] ConvertWordToBoolArray(short word)
        {
            bool[] boolArray = new bool[16];
            for (int index = 0; index < 16; ++index)
                boolArray[index] = ((uint)word & (uint)(1 << index)) > 0U;
            return boolArray;
        }
        /// <summary>
        /// Chuyển đổi nhiều Word thành nhiều bit
        /// </summary>
        /// <param name="word">word</param>
        /// <param name="size">số lượng</param>
        /// <returns>mảng bit</returns>
        internal static bool[] ConvertMultipleWordToBoolArray(short[] word, int size)
        {
            bool[] boolArray = new bool[size];
            int index1 = 0;
            for (int index2 = 0; index2 < word.Length; ++index2)
            {
                for (int index3 = 0; index3 < 16 && index1 != size; ++index3)
                {
                    boolArray[index1] = ((uint)word[index2] & (uint)(1 << index3)) > 0U;
                    ++index1;
                }
            }
            return boolArray;
        }
        /// <summary>
        /// Chuyển đổi mảng bit thành mảng byte
        /// </summary>
        /// <param name="coils">mảng bit</param>
        /// <returns>Mảng byte</returns>
        internal static byte[] ConvertBoolArrayToByteArray(bool[] coils)
        {
            int length = coils.Length;
            byte[] byteArray = new byte[(length % 16 == 0 ? length / 16 : length / 16 + 1) * 2];
            for (int index1 = 0; index1 < coils.Length; index1 += 8)
            {
                int num = 0;
                for (int index2 = 0; index2 < 8 && index1 + index2 < length; ++index2)
                {
                    if (coils[index1 + index2])
                        num += 1 << index2;
                }
                byteArray[index1 / 8] = (byte)num;
            }
            return byteArray;
        }
        /// <summary>
        /// Chuyển đổi nhiều byte thành nhiều bit
        /// </summary>
        /// <param name="data">mảng byte</param>
        /// <param name="startindex">địa chỉ bắt đầu</param>
        /// <param name="length">độ dài</param>
        /// <param name="size">kích thước</param>
        /// <returns>Mảng bit</returns>
        internal static bool[] ConvertMultipleByteToBoolArray(byte[] data, int startindex, int length, int size)
        {
            bool[] boolArray = new bool[size];
            int k = 0;
            for (int i = startindex; i < startindex + length; ++i)
            {
                for (int index3 = 0; index3 < 8 && k != size; ++index3)
                {
                    boolArray[k] = (data[i] & (uint)(1 << index3)) > 0U;
                    ++k;
                }
            }
            return boolArray;
        }
        /// <summary>
        /// Chuyển đổi mảng bit thành mảng byte  theo tiêu chuẩn SLMP
        /// </summary>
        /// <param name="coils">mảng bit</param>
        /// <returns>mảng byte</returns>
        internal static byte[] ConvertBoolArrayToByteArraySLMP(bool[] coils)
        {
            bool[] destinationArray;
            if (coils.Length % 2 != 0)
            {
                destinationArray = new bool[coils.Length + 1];
                Array.Copy(coils, destinationArray, coils.Length);
            }
            else
                destinationArray = coils;
            byte[] byteArraySlmp = new byte[destinationArray.Length / 2];
            int k = 0;
            for (int i = 0; i < destinationArray.Length; i += 2)
            {
                int num1 = destinationArray[i] ? 1 : 0;
                int num2 = destinationArray[i + 1] ? 1 : 0;
                byteArraySlmp[k] = (byte)(num1 + 15 + num2);
                byteArraySlmp[k] = (byte)((num1 == 1 ? 15 : 0) + num1 + num2);
                ++k;
            }
            return byteArraySlmp;
        }
        /// <summary>
        /// Nhận mảng byte từ chế độ RemoteControl
        /// </summary>
        /// <param name="mode">RemoteControl mode</param>
        /// <returns>mảng byte</returns>
        internal static byte[] CmdRemoteControl(RemoteControl mode)
        {
            switch (mode)
            {
                case dotPLC.Mitsubishi.RemoteControl.RUN_FORCE:
                    return new byte[19] { 0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00, 0x0A, 0x00, 0x00, 0x00, 0x01, 0x10, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00 };
                case dotPLC.Mitsubishi.RemoteControl.RUN:
                    return new byte[19] { 0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00, 0x0A, 0x00, 0x00, 0x00, 0x01, 0x10, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 };
                case dotPLC.Mitsubishi.RemoteControl.STOP:
                    return new byte[17] { 0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00, 0x08, 0x00, 0x00, 0x00, 0x02, 0x10, 0x00, 0x00, 0x00, 0x00 };
                case dotPLC.Mitsubishi.RemoteControl.PAUSE:
                    return new byte[17] { 0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00, 0x08, 0x00, 0x00, 0x00, 0x03, 0x10, 0x00, 0x00, 0x01, 0x00 };
                case dotPLC.Mitsubishi.RemoteControl.PAUSE_FORCE:
                    return new byte[17] { 0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00, 0x08, 0x00, 0x00, 0x00, 0x03, 0x10, 0x00, 0x00, 0x03, 0x00 };
                case dotPLC.Mitsubishi.RemoteControl.CLEAR_ERROR:
                    return new byte[15] { 0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00, 0x06, 0x00, 0x00, 0x00, 0x17, 0x16, 0x00, 0x00 };
                case dotPLC.Mitsubishi.RemoteControl.RESET:
                    return new byte[17] { 0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00, 0x08, 0x00, 0x00, 0x00, 0x06, 0x10, 0x00, 0x00, 0x00, 0x00 };
                case dotPLC.Mitsubishi.RemoteControl.CLEAR_LATCH:
                    return new byte[17] { 0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00, 0x08, 0x00, 0x00, 0x00, 0x05, 0x10, 0x00, 0x00, 0x00, 0x00 };
                default:
                    return null;
            }
        }
        /// <summary>
        /// Chuyển đổi chuỗi hex thành byte, Vd: FF55 => thành 0xFF, 0X55
        /// </summary>
        /// <param name="hex">chuỗi hex</param>
        /// <returns>mảng byte</returns>
        private static byte[] ConvertStringToByteArrray(string hex) => Enumerable.Range(0, hex.Length).Where((x => x % 2 == 0)).Select((x => Convert.ToByte(hex.Substring(x, 2), 16))).ToArray();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startindex"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        internal static bool[] ConvertByteArrayToBoolArray(byte[] data, int startindex, int size)
        {
            bool[] boolArray = new bool[size];
            int num = startindex + (size % 2 == 0 ? size / 2 : size / 2 + 1);
            int k = 0;
            for (int i = startindex; i < num; ++i)
            {
                boolArray[k] = (data[i] >> 4 & 1) == 1;
                if (k + 1 < size)
                {
                    boolArray[k + 1] = (data[i] & 1) == 1;
                    k += 2;
                }
                else
                    break;
            }
            return boolArray;
        }
        /// <summary>
        /// Thiết lập byte ban đầu
        /// </summary>
        protected internal abstract void SetupBuffer();
        /// <summary>
        /// Phân tích label thành Device(1 byte) và Index (Mảng byte)
        /// </summary>
        /// <param name="label">Label name</param>
        /// <param name="device">Device</param>
        /// <param name="low_num">byte[0]</param>
        /// <param name="mid_num">byte[1]</param>
        /// <param name="high_num">byte[2]</param>
        /// <returns>index</returns>
        protected internal abstract int SettingDevice(string label, out byte device, out byte low_num, out byte mid_num, out byte high_num);
        /// <summary>
        /// Lấy byte đại diện cho device
        /// </summary>
        /// <param name="device">device</param>
        /// <returns>byte đại diện cho device</returns>
        internal abstract byte GetNameDevice(string device);
        /// <summary>
        /// Phân tích label thành device và index
        /// </summary>
        /// <param name="label">label name</param>
        /// <param name="device">device</param>
        /// <param name="num">index</param>
        /// <returns>Trả về true nếu thành công;nếu không,false</returns>
        internal static bool SettingDevice(string label, out string device, out int num)
        {
            if (label.Length < 2)
            {
                device = label;
                num = 0;
                return false;
            }
            if (label[0] == 'S' && label[1] == 'B')
            {
                label = label.Substring(2);
                device = "SB";
                return int.TryParse(label, NumberStyles.HexNumber, null, out num) && IsSizeMatch(device, num);
            }
            if (label[0] == 'S' && label[1] == 'W')
            {
                label = label.Substring(2);
                device = "SW";
                return int.TryParse(label, NumberStyles.HexNumber, null, out num) && IsSizeMatch(device, num);
            }
            if (label[0] == 'B' && label[1] != 'L')
            {
                label = label.Substring(1);
                device = "B";
                return int.TryParse(label, NumberStyles.HexNumber, null, out num) && IsSizeMatch(device, num);
            }
            if (label[0] == 'W')
            {
                label = label.Substring(1);
                device = "W";
                return int.TryParse(label, NumberStyles.HexNumber, null, out num) && IsSizeMatch(device, num);
            }
            if (label[0] == 'X')
            {
                label = label.Substring(1);
                device = "X";
                bool flag = int.TryParse(label, out num);
                if (!isOctal(num))
                    return false;
                num = ConvertOctalToDecimal(num);
                return flag && IsSizeMatch(device, num);
            }
            if (label[0] == 'Y')
            {
                label = label.Substring(1);
                device = "Y";
                bool flag = int.TryParse(label, out num);
                if (!isOctal(num))
                    return false;
                num = ConvertOctalToDecimal(num);
                return flag && IsSizeMatch(device, num);
            }
            int num1 = 0;
            for (int index = 0; index < label.Length; ++index)
            {
                if (label[index] >= '0' && label[index] <= '9')
                {
                    num1 = index;
                    break;
                }
            }
            device = label.Substring(0, num1);
            return int.TryParse(label.Substring(num1), out num) && IsSizeMatch(device, num);
        }
        /// <summary>
        /// Ghép device và index thành label
        /// </summary>
        /// <param name="device">device</param>
        /// <param name="num">index</param>
        /// <param name="label">label name</param>
        /// <returns>Trả về true nếu thành công;nếu không,false</returns>
        internal static bool SettingLabel(string device, int num, out string label)
        {
            switch (device)
            {
                case "SB":
                    label = device + num.ToString("X2");
                    return IsSizeMatch(device, num);
                case "SW":
                    label = device + num.ToString("X2");
                    return IsSizeMatch(device, num);
                case "B":
                    label = device + num.ToString("X2");
                    return IsSizeMatch(device, num);
                case "W":
                    label = device + num.ToString("X2");
                    return IsSizeMatch(device, num);
                case "X":
                    label = device + ConvertDecimalToOctal(num);
                    return IsSizeMatch(device, num);
                case "Y":
                    label = device + ConvertDecimalToOctal(num);
                    return IsSizeMatch(device, num);
                default:
                    label = device + num;
                    return IsSizeMatch(device, num);
            }
        }
        /// <summary>
        /// Kiểm tra Device và Index có phù hợp không
        /// </summary>
        /// <param name="device">device</param>
        /// <param name="num">index</param>
        /// <returns>Trả về true nếu thành công;nếu không,false</returns>
        internal static bool IsSizeMatch(string device, int num)
        {
            switch (device)
            {
                case "B":
                    return num <= short.MaxValue;
                case "BL":
                    return num <= 31;
                case "CC":
                    return num <= 1023;
                case "CN":
                    return num <= 1023;
                case "CS":
                    return num <= 1023;
                case "D":
                    return num <= 7999;
                case "F":
                    return num <= short.MaxValue;
                case "L":
                    return num <= short.MaxValue;
                case "LCN":
                    return num <= 1023;
                case "LZ":
                    return num <= 1;
                case "M":
                    return num <= short.MaxValue;
                case "R":
                    return num <= short.MaxValue;
                case "S":
                    return num <= 4095;
                case "SB":
                    return num <= short.MaxValue;
                case "SC":
                    return num <= 1023;
                case "SD":
                    return num <= 11999;
                case "SM":
                    return num <= 9999;
                case "SN":
                    return num <= 1023;
                case "SS":
                    return num <= 1023;
                case "STN":
                    return num <= 1023;
                case "SW":
                    return num <= short.MaxValue;
                case "TC":
                    return num <= 1023;
                case "TN":
                    return num <= 1023;
                case "TS":
                    return num <= 1023;
                case "W":
                    return num <= short.MaxValue;
                case "X":
                    return num <= 1777;
                case "Y":
                    return num <= 1777;
                case "Z":
                    return num <= 19;
                default:
                    return false;
            }
        }
        /// <summary>
        /// Kiểm tra index có phải là octal không
        /// </summary>
        /// <param name="n">index</param>
        /// <returns>Trả về true nếu thành công;nếu không,false</returns>
        internal static bool isOctal(int n)
        {
            for (; n > 0; n /= 10)
            {
                if (n % 10 >= 8)
                    return false;
            }
            return true;
        }
        /// <summary>
        /// Establish connection to the server.
        /// </summary>
        public abstract void Connect();
        /// <summary>
        /// Close connection to the server.
        /// </summary>
        public abstract void Close();
        /// <summary>
        /// Write a single value to the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">A single value to be written.</param>
        internal abstract void WriteDevice(string label, bool value);
        /// <summary>
        /// Write a single value to the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">A single value to be written.</param>
        internal abstract void WriteDevice(string label, short value);
        /// <summary>
        /// Write a single value to the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">A single value to be written.</param>
        internal abstract void WriteDevice(string label, int value);
        /// <summary>
        /// Write a single value to the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">A single value to be written.</param>
        internal abstract void WriteDevice(string label, float value);
        /// <summary>
        /// Write a single value to the server.
        /// </summary>
        /// <typeparam name="T">The data type of value.</typeparam>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">A single value to be written.</param>
        public void WriteDevice<T>(string label, T value) where T : struct
        {
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Boolean:
                    WriteDevice(label, (bool)Convert.ChangeType(value, TypeCode.Boolean));
                    break;
                case TypeCode.Int16:
                    WriteDevice(label, (short)Convert.ChangeType(value, TypeCode.Int16));
                    break;
                case TypeCode.UInt16:
                    WriteDevice(label, (short)(ushort)Convert.ChangeType(value, TypeCode.UInt16));
                    break;
                case TypeCode.Int32:
                    WriteDevice(label, (int)Convert.ChangeType(value, TypeCode.Int32));
                    break;
                case TypeCode.UInt32:
                    WriteDevice(label, (int)(uint)Convert.ChangeType(value, TypeCode.UInt32));
                    break;
                case TypeCode.Single:
                    WriteDevice(label, (float)Convert.ChangeType(value, TypeCode.Single));
                    break;
                default:
                    throw new InvalidCastException("Invalid input data type.");
            }
        }
        /// <summary>
        /// Write multiple values to the server in a batch. 
        /// </summary>
        /// <typeparam name="T">The data type of value. (EX: <see cref="bool"></see>, <see cref="short"/>, <see cref="float"/>, etc.)</typeparam>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="values">Values to be written.</param>
        public void WriteDeviceBlock<T>(string label, params T[] values) where T : struct
        {
            int length = values.Length;
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Boolean:
                    {
                        var values_temp = new bool[length];
                        for (int index = 0; index < length; ++index)
                            values_temp[index] = (bool)Convert.ChangeType(values[index], TypeCode.Boolean);
                        WriteDeviceBlock(label, values_temp);
                        break;
                    }
                case TypeCode.Int16:
                    {
                        var values_temp = new short[length];
                        for (int index = 0; index < length; ++index)
                            values_temp[index] = (short)Convert.ChangeType(values[index], TypeCode.Int16);
                        WriteDeviceBlock(label, values_temp);
                        break;
                    }
                case TypeCode.UInt16:
                    {
                        var values_temp = new short[length];
                        for (int index = 0; index < length; ++index)
                            values_temp[index] = (short)(ushort)Convert.ChangeType(values[index], TypeCode.UInt16);
                        WriteDeviceBlock(label, values_temp);
                        break;
                    }
                case TypeCode.Int32:
                    {
                        var values_temp = new int[length];
                        for (int index = 0; index < length; ++index)
                            values_temp[index] = (int)Convert.ChangeType(values[index], TypeCode.Int32);
                        WriteDeviceBlock(label, values_temp);
                        break;
                    }
                case TypeCode.UInt32:
                    {
                        var values_temp = new int[length];
                        for (int index = 0; index < length; ++index)
                            values_temp[index] = (int)(uint)Convert.ChangeType(values[index], TypeCode.UInt32);
                        WriteDeviceBlock(label, values_temp);
                        break;
                    }
                case TypeCode.Single:
                    {
                        var values_temp = new float[length];
                        for (int index = 0; index < length; ++index)
                            values_temp[index] = (float)Convert.ChangeType(values[index], TypeCode.Single);
                        WriteDeviceBlock(label, values_temp);
                        break;
                    }
                default:
                    throw new InvalidCastException("Invalid input data type.");
            }
        }
        /// <summary>
        ///  Write multiple values to the server in a batch.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="values">Values to be written.</param>
        internal abstract void WriteDeviceBlock(string label, params bool[] values);
        /// <summary>
        ///  Write multiple values to the server in a batch.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="values">Values to be written.</param>
        internal abstract void WriteDeviceBlock(string label, params short[] values);
        /// <summary>
        ///  Write multiple values to the server in a batch.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="values">Values to be written.</param>
        internal abstract void WriteDeviceBlock(string label, params int[] values);
        /// <summary>
        ///  Write multiple values to the server in a batch.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="values">Values to be written.</param>
        internal abstract void WriteDeviceBlock(string label, params float[] values);


        /// <summary>
        /// Write multiple values to the server randomly.
        /// </summary>
        /// <param name="bits"><see cref="dotPLC.Mitsubishi.Types.Bit"/> values to be written.</param>
        public abstract void WriteDeviceRandom(params Bit[] bits);
        /// <summary>
        /// Write multiple values to the server randomly.
        /// </summary>
        /// <param name="words"><see cref="dotPLC.Mitsubishi.Types.Word"/> values to be written.</param>
        public abstract void WriteDeviceRandom(params Word[] words);
        /// <summary>
        /// Write multiple values to the server randomly.
        /// </summary>
        /// <param name="dwords"><see cref="dotPLC.Mitsubishi.Types.DWord"/> values to be written.</param>
        public abstract void WriteDeviceRandom(params DWord[] dwords);
        /// <summary>
        /// Write multiple values to the server randomly.
        /// </summary>
        /// <param name="floats"><see cref="dotPLC.Mitsubishi.Types.Float"/> values to be written.</param>
        public abstract void WriteDeviceRandom(params Float[] floats);
        /// <summary>
        /// Read a single value from the server.
        /// </summary>
        /// <typeparam name="T">The data type of value. (EX: <see cref="bool"></see>, <see cref="short"/>, <see cref="float"/>, etc.)</typeparam>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <returns>Returned <typeparamref name="T"/> value.</returns>
        public T ReadDevice<T>(string label) where T : struct
        {
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Boolean:
                    return (T)Convert.ChangeType(ReadSingleCoil(label), typeof(T));
                case TypeCode.Int16:
                    return (T)Convert.ChangeType(ReadSingleRegister(label), typeof(T));
                case TypeCode.UInt16:
                    return (T)Convert.ChangeType((ushort)ReadSingleRegister(label), typeof(T));
                case TypeCode.Int32:
                    return (T)Convert.ChangeType(ReadSingleDouble(label), typeof(T));
                case TypeCode.UInt32:
                    return (T)Convert.ChangeType((uint)ReadSingleDouble(label), typeof(T));
                case TypeCode.Single:
                    return (T)Convert.ChangeType(ReadSingleFloat(label), typeof(T));
                default:
                    throw new InvalidCastException("Invalid input data type.");
            }
        }
        /// <summary>
        /// Read a single value from the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <returns>Returned value.</returns>
        internal abstract bool ReadSingleCoil(string label);
        /// <summary>
        /// Read a single value from the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <returns>Returned value.</returns>
        internal abstract short ReadSingleRegister(string label);
        /// <summary>
        /// Read a single value from the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <returns>Returned value.</returns>
        internal abstract int ReadSingleDouble(string label);
        /// <summary>
        /// Read a single value from the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <returns>Returned value.</returns>
        internal abstract float ReadSingleFloat(string label);
        /// <summary>
        /// Read multiple values from the server in a batch.
        /// </summary>
        /// <typeparam name="T">The data type of value. (EX: <see cref="bool"></see>, <see cref="short"/>, <see cref="float"/>, etc.)</typeparam>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returned <typeparamref name="T"/>[] values.</returns>
        public T[] ReadDeviceBlock<T>(string label, int size) where T : struct
        {
            T[] results = new T[size];
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Boolean:
                    {
                        bool[] values = ReadMultipleCoils(label, size);
                        for (int index = 0; index < size; ++index)
                            results[index] = (T)Convert.ChangeType(values[index], typeof(T));
                        return results;
                    }
                case TypeCode.Int16:
                    {
                        short[] values = ReadMultipleRegisters(label, size);
                        for (int index = 0; index < size; ++index)
                            results[index] = (T)Convert.ChangeType(values[index], typeof(T));
                        return results;
                    }
                case TypeCode.UInt16:
                    {
                        short[] values = ReadMultipleRegisters(label, size);
                        for (int index = 0; index < size; ++index)
                            results[index] = (T)Convert.ChangeType((ushort)values[index], typeof(T));
                        return results;
                    }
                case TypeCode.Int32:
                    {
                        int[] values = ReadMultipleDoubles(label, size);
                        for (int index = 0; index < size; ++index)
                            results[index] = (T)Convert.ChangeType(values[index], typeof(T));
                        return results;
                    }
                case TypeCode.UInt32:
                    {
                        int[] values = ReadMultipleDoubles(label, size);
                        for (int index = 0; index < size; ++index)
                            results[index] = (T)Convert.ChangeType((uint)values[index], typeof(T));
                        return results;
                    }
                case TypeCode.Single:
                    {
                        float[] values = ReadMultipleFloats(label, size);
                        for (int index = 0; index < size; ++index)
                            results[index] = (T)Convert.ChangeType(values[index], typeof(T));
                        return results;
                    }
                default:
                    throw new InvalidCastException("Invalid input data type.");
            }
        }
        /// <summary>
        /// Read multiple values from the server in a batch.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returned values.</returns>
        internal abstract bool[] ReadMultipleCoils(string label, int size);
        /// <summary>
        /// Read multiple values from the server in a batch.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returned values.</returns>
        internal abstract short[] ReadMultipleRegisters(string label, int size);
        /// <summary>
        /// Read multiple values from the server in a batch.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returned values.</returns>
        internal abstract int[] ReadMultipleDoubles(string label, int size);
        /// <summary>
        /// Read multiple values from the server in a batch.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returned values.</returns>
        internal abstract float[] ReadMultipleFloats(string label, int size);
        /// <summary>
        /// Write text to the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="text">Text to be written.</param>
        public abstract void WriteText(string label, string text);
        /// <summary>
        /// Read text from the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of text to be read.</param>
        /// <returns>Returns text of the specified size.</returns>
        public abstract string ReadText(string label, int size);
        /// <summary>
        /// Read the model character string of the server.
        /// </summary>
        /// <returns>The model character string of the server.</returns>
        public abstract string GetCpuName();
        /// <summary>
        /// Read multiple values from the server randomly.
        /// </summary>
        /// <param name="bits"><see cref="dotPLC.Mitsubishi.Types.Bit"/> values to be read.</param>
        public abstract void ReadDeviceRandom(params Bit[] bits);
        /// <summary>
        /// Read multiple values from the server randomly.
        /// </summary>
        /// <param name="words"><see cref="dotPLC.Mitsubishi.Types.Word"/> values to be read.</param>
        public abstract void ReadDeviceRandom(params Word[] words);
        /// <summary>
        /// Read multiple values from the server randomly.
        /// </summary>
        /// <param name="dwords"><see cref="dotPLC.Mitsubishi.Types.DWord"/> values to be read.</param>
        public abstract void ReadDeviceRandom(params DWord[] dwords);
        /// <summary>
        /// Read multiple values from the server randomly.
        /// </summary>
        /// <param name="floats"><see cref="dotPLC.Mitsubishi.Types.Float"/> values to be read.</param>
        public abstract void ReadDeviceRandom(params Float[] floats);
        /// <summary>
        /// Read multiple values from the server randomly. <see langword="[RECOMMENDED]"></see>
        /// </summary>
        /// <param name="bits"><see cref="dotPLC.Mitsubishi.Types.Bit"/> values to be read.</param>
        /// <param name="words"><see cref="dotPLC.Mitsubishi.Types.Word"/> values to be read.</param>
        /// <param name="dwords"><see cref="dotPLC.Mitsubishi.Types.DWord"/> values to be read.</param>
        /// <param name="floats"><see cref="dotPLC.Mitsubishi.Types.Float"/> values to be read.</param>
        public abstract void ReadDeviceRandom(Bit[] bits = null, Word[] words = null, DWord[] dwords = null, Float[] floats = null);
    }
}
