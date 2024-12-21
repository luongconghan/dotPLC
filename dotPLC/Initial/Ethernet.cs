
using dotPLC.Mitsubishi;
using dotPLC.Mitsubishi.Exceptions;
using dotPLC.Mitsubishi.Types;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

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
        internal int _readTimeout = 3000;
        /// <summary>
        /// _sendTimeout
        /// </summary>
        internal int _writeTimeout = 3000;
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
        /// Gets or sets the port number of the server.
        /// </summary>
        public int Port { get; set; } = 502;
        /// <summary>
        /// Gets or sets the IP-Address of the server.
        /// </summary>
        internal string _iPAddress = "127.0.0.1";
        /// <summary>
        ///  Gets or sets the IP-Address of the server.
        /// </summary>
        public string IPAddress
        {
            get
            {
                return _iPAddress;
            }
            set
            {
                if (!ValidateIPv4(value)) throw new ArgumentException("Invalid IP address.", nameof(IPAddress));
                _iPAddress = value;
            }
        }
        /// <summary>
        /// Gets a value indicating whether a connection to the server has been established.
        /// </summary>
        public bool Connected => _connected;
        /// <summary>
        /// Gets or sets the amount of time that a write operation blocks waiting for data to the server.
        /// </summary>
        public int ReadTimeout
        {
            get => _readTimeout;
            set
            {
                _readTimeout = value;
                if (_tcpclient == null)
                    return;
                _tcpclient.ReceiveTimeout = value;
                _stream.ReadTimeout = value;
            }
        }
        /// <summary>
        /// Gets or sets the amount of time that a read operation blocks waiting for data from the server.
        /// </summary>
        public int WriteTimeout
        {
            get => _writeTimeout;
            set
            {
                _writeTimeout = value;
                if (_tcpclient == null)
                    return;
                _tcpclient.SendTimeout = value;
                _stream.WriteTimeout= value;
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
        /// Chuyển đổi mảng bit thành mảng byte -Theo số lượng word 21 coil =>4byte
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
        /// Chuyển đổi mảng bit thành mảng byte -Theo số lượng coil 21 coil =>3byte
        /// </summary>
        /// <param name="coils">mảng bit</param>
        /// <returns>Mảng byte</returns>
        internal static byte[] ConvertBoolArrayToByteArrayOdd(bool[] coils)
        {
            int length = coils.Length;
            byte[] byteArray = new byte[length % 8 == 0 ? length / 8 : 1 + length / 8];
            for (int i = 0; i < coils.Length; i += 8)
            {
                int k = 0;
                for (int j = 0; j < 8 && i + j < length; ++j)
                {
                    if (coils[i + j])
                        k += 1 << j;
                }
                byteArray[i / 8] = (byte)k;
            }
            // Array.Reverse(byteArray);
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
        /// Phân tích label thành device và index
        /// </summary>
        /// <param name="label">label name</param>
        /// <param name="device">device</param>
        /// <param name="num">index</param>
        /// <returns>Trả về true nếu thành công;nếu không,false</returns>
        internal static void SettingDevice(string label, out string device, out int num)
        {
            if (label==null || label=="" || label.Length < 2)
            {
                throw new InvalidDeviceLabelNameException("The label name of device is invalid.",nameof(label));
            }
            if (label[0] == 'S' && label[1] == 'B')
            {
                label = label.Substring(2);
                device = "SB";
                if (!int.TryParse(label, NumberStyles.HexNumber, null, out num) || num > 0x7fff)
                    throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                return;
            }
            if (label[0] == 'S' && label[1] == 'W')
            {
                label = label.Substring(2);
                device = "SW";
                if (!int.TryParse(label, NumberStyles.HexNumber, null, out num) || num > 0x7fff)
                    throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                return;
            }
            if (label[0] == 'B' && label[1] != 'L')
            {
                label = label.Substring(1);
                device = "B";
                if (!int.TryParse(label, NumberStyles.HexNumber, null, out num) || num > 0x7fff)
                    throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                return;
            }
            if (label[0] == 'W')
            {
                label = label.Substring(1);
                device = "W";
                if (!int.TryParse(label, NumberStyles.HexNumber, null, out num) || num > 0x7fff)
                    throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                return;
            }
            if (label[0] == 'X')
            {
                label = label.Substring(1);
                device = "X";
                if (!int.TryParse(label, out num) || !isOctal(num) || num > 1777) //Check octal? 1777(octal)
                {
                    throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                }
                num = ConvertOctalToDecimal(num);
                return;
            }
            if (label[0] == 'Y')
            {
                label = label.Substring(1);
                device = "Y";
                if (!int.TryParse(label, out num) || !isOctal(num) || num > 1777) //Check octal? 1777(octal)
                {
                    throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                }
                num = ConvertOctalToDecimal(num);
                return;
            }
            int index_device = 0;
            for (int index = 0; index < label.Length; ++index)
            {
                if (label[index] >= '0' && label[index] <= '9')
                {
                    index_device = index;
                    break;
                }
            }
            device = label.Substring(0, index_device);
            if(!int.TryParse(label.Substring(index_device), out num))
            {
                throw new InvalidDeviceLabelNameException("The label name of device is invalid.",nameof(label));
            }
            CheckDeviceAndAddressMatch(device, num);
        }
        /// <summary>
        /// Ghép device và index thành label
        /// </summary>
        /// <param name="device">device</param>
        /// <param name="num">index</param>
        /// <param name="label">label name</param>
        /// <returns>Trả về true nếu thành công;nếu không,false</returns>
        internal static void SettingLabel(string device, int num, out string label)
        {
            switch (device)
            {
                case "SB":
                    label = device + num.ToString("X2");
                    CheckDeviceAndAddressMatch(device, num);
                    break;
                case "SW":
                    label = device + num.ToString("X2");
                    CheckDeviceAndAddressMatch(device, num);
                    break;
                case "B":
                    label = device + num.ToString("X2");
                    CheckDeviceAndAddressMatch(device, num);
                    break;
                case "W":
                    label = device + num.ToString("X2");
                    CheckDeviceAndAddressMatch(device, num);
                    break;
                case "X":
                    label = device + ConvertDecimalToOctal(num);
                    CheckDeviceAndAddressMatch(device, num);
                    break;
                case "Y":
                    label = device + ConvertDecimalToOctal(num);
                    CheckDeviceAndAddressMatch(device, num);
                    break;
                default:
                    label = device + num;
                    CheckDeviceAndAddressMatch(device, num);
                    break;
            }
        }

        /// <summary>
        /// Kiểm tra Device và Index có phù hợp không
        /// </summary>
        /// <param name="device">device</param>
        /// <param name="devicenum">devicenum</param>
        /// <returns>Trả về true nếu thành công;nếu không,false</returns>
        //internal static bool IsDeviceAndAddressMatch(string device, int num)
        //{
        //    switch (device)
        //    {
        //        //Register
        //        case "D":
        //            if (num > 7999) return false;
        //            else return true;//Decimal
        //        case "SW":
        //            if (num > 0x7fff) return false;
        //            else return true;//Hexadecimal
        //        case "W":
        //            if (num > 0x7fff) return false;
        //            else return true; //Hexadecimal
        //        case "TN":
        //            if (num > 1023) return false;
        //            else return true; //Timer word //Decimal 
        //        case "SD":
        //            if (num > 11999) return false;
        //            else return true; //Decimal
        //        case "R":
        //            if (num > 32767) return false;
        //            else return true; // Decimal
        //        case "Z":
        //            if (num > 19) return false;
        //            else return true;  // Decimal
        //        case "LZ":
        //            if (num > 1) return false;
        //            else return true;  // Decimal
        //        case "CN":
        //            if (num > 1023) return false;
        //            else return true; //Counter
        //        case "LCN":
        //            if (num > 1023) return false;
        //            else return true;     //Long counter  // Decimal
        //        case "SN":
        //            if (num > 1023) return false;
        //            else return true;  // Retentive timer-Timer có nhớ ST0.. //Decimal
        //        case "STN":
        //            if (num > 1023) return false;
        //            else return true; // Giống ở trên //Decimal

        //        //Bit:
        //        case "X":
        //            if (num > 1777) return false;
        //            else return true; //OCtal
        //        case "Y":
        //            if (num > 1777) return false;
        //            else return true; //OCtal
        //        case "M":
        //            if (num > 32767) return false;
        //            else return true; //Decimal
        //        case "L":
        //            if (num > 32767) return false;
        //            else return true; //Decimal
        //        case "F":
        //            if (num > 32767) return false;
        //            else return true;  //Decimal
        //        case "B":
        //            if (num > 0x7fff) return false; //255
        //            else return true; //Hexca
        //        case "S":
        //            if (num > 4095) return false;
        //            else return true;   //Step relay //Decimal
        //        case "SS":
        //            if (num > 1023) return false;
        //            else return true;  //Retentive timer  ST0 //Decimal Bit
        //        case "SC":
        //            if (num > 1023) return false;
        //            else return true;   //Retentive timer ST0 //Decimal Bit
        //        case "TC":
        //            if (num > 1023) return false;
        //            else return true;   //Timer T0 (bật) Decimal
        //        case "TS":
        //            if (num > 1023) return false;
        //            else return true;  // Timer T0 Decimal
        //        case "CS":
        //            if (num > 1023) return false;
        //            else return true;   //Counter  C0  Decimal
        //        case "CC":
        //            if (num > 1023) return false;
        //            else return true;   //Counter C0 Decimal
        //        case "SB":
        //            if (num > 0x7fff) return false;
        //            else return true;  //Link special relay Hex
        //        case "SM":
        //            if (num > 9999) return false;
        //            else return true; //Special relay Decmal
        //        case "BL":
        //            if (num > 31) return false;
        //            else return true;   //SFC block device Decimal
        //        default:
        //            return false;
        //    }
        //}
        internal static void CheckDeviceAndAddressMatch(string device, int devicenum)
        {
            switch (device)
            {
                case "B":
                    if (devicenum > 0x7FFF)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                    break;
                case "BL":
                    if (devicenum > 31)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                    break;
                case "CC":
                    if (devicenum > 1023)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                    break;
                case "CN":
                    if (devicenum > 1023)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                    break;
                case "CS":
                    if (devicenum > 1023)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                    break;
                case "D":
                    if (devicenum > 7999)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                    break;
                case "F":
                    if (devicenum > 32767)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                    break;
                case "L":
                    if (devicenum > 32767)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                    break;
                case "LCN":
                    if (devicenum > 1023)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                    break;
                case "LZ":
                    if (devicenum > 1)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                    break;
                case "M":
                    if (devicenum > 32767)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                    break;
                case "R":
                    if (devicenum > 32767)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                    break;
                case "S":
                    if (devicenum > 4095)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                    break;
                case "SB":
                    if (devicenum > 0x7FFF)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                    break;
                case "SC":
                    if (devicenum > 1023)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                    break;
                case "SD":
                    if (devicenum > 11999)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                    break;
                case "SM":
                    if (devicenum > 9999)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                    break;
                case "SN":
                    if (devicenum > 1023)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                    break;
                case "SS":
                    if (devicenum > 1023)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                    break;
                case "STN":
                    if (devicenum > 1023)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                    break;
                case "SW":
                    if (devicenum > 0x7FFF)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                    break;
                case "TC":
                    if (devicenum > 1023)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                    break;
                case "TN":
                    if (devicenum > 1023)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                    break;
                case "TS":
                    if (devicenum > 1023)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                    break;
                case "W":
                    if (devicenum > 0x7FFF)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                    break;
                case "X":
                    if (devicenum > 1777)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                    break;
                case "Y":
                    if (devicenum > 1777)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                    break;
                case "Z":
                    if (devicenum > 19)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.","label");
                    break;
                default:
                    throw new InvalidDeviceLabelNameException("The label name of device is invalid.","label");
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
        public abstract void Disconnect();

        /// <summary>
        /// validating an IP Address
        /// </summary>
        /// <param name="ipString">Ip-address.</param>
        /// <returns>Validate if true, Invalidate if false.</returns>
        internal bool ValidateIPv4(string ipString)
        {
            if (string.IsNullOrWhiteSpace(ipString))
            {
                return false;
            }

            string[] splitValues = ipString.Split('.');
            if (splitValues.Length != 4)
            {
                return false;
            }

            byte tempForParsing;

            return splitValues.All(r => byte.TryParse(r, out tempForParsing));
        }

        
        
    }
}
