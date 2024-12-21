using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;
using System.Threading;

namespace dotPLC.Initial
{
    public abstract class Serial
    {
        /// <summary>
        /// _serialport
        /// </summary>
        internal SerialPort _serialport;
        /// <summary>
        /// _serialcomport
        /// </summary>
        internal string _serialcomport = "COM1";
        /// <summary>
        /// field
        /// </summary>
        internal int _baudrate = 9600;
        /// <summary>
        /// field
        /// </summary>
        internal Parity _parity = Parity.Even;
        /// <summary>
        /// field
        /// </summary>
        internal StopBits _stopBits = StopBits.One;
        /// <summary>
        /// field
        /// </summary>
        internal int _readtimeout = 3000;
        /// <summary>
        /// field
        /// </summary>
        internal int _writetimeout = 3000;
        /// <summary>
        /// field
        /// </summary>
        internal int _connecttimeout = 3000;
        /// <summary>
        /// field
        /// </summary>
        internal bool _connected = false;
        /// <summary>
        /// field
        /// </summary>
        protected internal byte[] SendBuffer = new byte[65536];
        /// <summary>
        /// field
        /// </summary>
        protected internal byte[] ReceveiBuffer = new byte[65536];
        /// <summary>
        /// field
        /// </summary>
        internal bool _isDataReceived;
        /// <summary>
        /// field
        /// </summary>
        internal int _bytesToRead;
        /// <summary>
        /// field
        /// </summary>
        internal const long _ticksWait = TimeSpan.TicksPerMillisecond * 2000;
        /// <summary>
        /// field
        /// </summary>
        internal SpinWait _spinWait = new SpinWait();

        /// <summary>
        /// Calculate Modbus CRC 
        /// </summary>
        /// <param name="data">data</param>
        /// <param name="count">count</param>
        /// <returns>short</returns>
        internal static short CalculateCRC(byte[] data, int count)
        {
            int crc = 0xFFFF; //S1
            for (int i = 0; i < count; i++)//S6
            {
                crc = crc ^ data[i]; //S2
                for (int j = 0; j < 8; j++) //S5
                {
                    crc = (crc & 1) == 0 ? crc >> 1 : (crc >> 1) ^ 0xA001;//Rút gọn: S3+S4
                }
            }
            return (short)crc;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Serial"></see> class.
        /// </summary>
        public Serial() => SetupBuffer();
        protected internal abstract void SetupBuffer();
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
        /// Gets or sets the serial baud rate.
        /// </summary>
        public int Baudrate
        {
            get
            {
                return _baudrate;
            }
            set
            {
                _baudrate = value;
                _serialport.BaudRate = value;
            }
        }

        /// <summary>
        /// Gets or sets the parity-checking protocol.
        /// </summary>
        public Parity Parity
        {
            get
            {
                return _parity;
            }
            set
            {
                _parity = value;
                _serialport.Parity = value;
            }
        }

        /// <summary>
        /// Gets or sets the standard number of stopbits per byte.
        /// </summary>
        public StopBits StopBits
        {
            get
            {
                return _stopBits;
            }
            set
            {
                _stopBits = value;

                _serialport.StopBits = value;
            }
        }

        /// <summary>
        /// Gets or sets the port for communications, including but not limited to all available COM ports.
        /// </summary>
        public string SerialPort
        {
            get => _serialcomport;
            set
            {
                _serialcomport = value;
                _serialport.PortName = value;
            }
        }

        /// <summary>
        /// Gets or sets the unit identifier.
        /// </summary>
        public byte UnitIdentifier
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the amount of time that a connect operation blocks waiting from the slave.
        /// </summary>
        public int ConnectTimeout
        {
            get
            {
                return _connecttimeout;
            }
            set
            {
                _connecttimeout = value;
            }
        }

        /// <summary>
        /// Establish connection to a slave.
        /// </summary>
        public void Connect()
        {
            if (_serialport != null)
            {
                if (!_serialport.IsOpen)
                {
                    _serialport.PortName = _serialcomport;
                    _serialport.BaudRate = _baudrate;
                    _serialport.Parity = _parity;
                    _serialport.StopBits = _stopBits;
                    _serialport.ReadTimeout = _readtimeout;
                    _serialport.WriteTimeout = _writetimeout;
                    _serialport.Open();
                    _connected = true;
                }
            }
        }
        /// <summary>
        /// Close connection to a slave.
        /// </summary>
        public void Disconnect()
        {
            if (_serialport != null)
            {
                _serialport.Close();
            }
        }

    }
}
