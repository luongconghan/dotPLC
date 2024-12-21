using dotPLC.Initial;
using dotPLC.Mitsubishi.Types;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using dotPLC.Mitsubishi.Exceptions;

namespace dotPLC.Mitsubishi
{
    /// <summary>
    /// Provides client connection for TCP network service via Seamless Message Protocol (SLMP).
    /// </summary>
    public sealed partial class SLMPClient : Ethernet, IMitsubishiFuntion, IMitsubishiFunctionAsync
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="SLMPClient"></see> class.
        /// </summary>
        public SLMPClient()
        {
            _reconnectLimitTimer = new System.Timers.Timer();
            _reconnectLimitTimer.Interval = _reconnectLimitInterval;
            _reconnectLimitTimer.Elapsed += new ElapsedEventHandler(_reconnectLimitTimer_Elapsed);
            _breakTimer = new System.Timers.Timer();
            _breakTimer.Interval = _breakInterval;
            _breakTimer.Elapsed += new ElapsedEventHandler(_breakTimer_Elapsed);
            _selfTestTimer = new System.Timers.Timer();
            _selfTestTimer.Interval = _selfTestCheckInterval;
            _selfTestTimer.Elapsed += new ElapsedEventHandler(_selfTestTimer_Elapsed);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SLMPClient"></see> class which determines the ip-address and the port number.
        /// </summary>
        /// <param name="ipaddress">IP Address of the server.</param>
        /// <param name="port">Port number of the server.</param>
        public SLMPClient(string ipaddress = "127.0.0.1", int port = 502)
         : this()
        {
            if (!ValidateIPv4(ipaddress)) throw new ArgumentException("Invalid IP address.", nameof(ipaddress));
            _iPAddress = ipaddress;
            Port = port;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SLMPClient"></see> class which determines AutoReconnect mode.
        /// </summary>
        /// <param name="autoReconnect">Auto-reconnect mode when connection is lost from the server.</param>
        public SLMPClient(AutoReconnect autoReconnect)
          : this()
        {
            AutoReconnect = autoReconnect;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SLMPClient"></see> class which determines the ip-address, the port number and AutoReconnect mode.
        /// </summary>
        /// <param name="ipaddress">IP Address of the server</param>
        /// <param name="port">Port number of the server.</param>
        /// <param name="autoReconnect">Auto-reconnect mode when connection is lost from the server.</param>
        public SLMPClient(string ipaddress = "127.0.0.1", int port = 502, AutoReconnect autoReconnect = AutoReconnect.None)
          : this(ipaddress, port)
        {
            if (!ValidateIPv4(ipaddress)) throw new ArgumentException("Invalid IP address.", nameof(ipaddress));
            _iPAddress = ipaddress;
            Port = port;
            AutoReconnect = autoReconnect;
        }

        #endregion Constructor

        #region Field

        /// <summary>
        /// _selfTestConnectionString
        /// </summary>
        private string _selfTestConnectionString = "1998";

        /// <summary>
        /// Labels
        /// </summary>
        private Dictionary<string, List<int>> Labels = new Dictionary<string, List<int>>();

        /// <summary>
        /// dsWordBaseBits
        /// </summary>
        private List<Word> dsWordBaseBits = new List<Word>();

        /// <summary>
        /// _getCpuName
        /// </summary>
        private byte[] _getCpuName = new byte[15] { 0x50, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00, 0x06, 0x00, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00 };

        #endregion Field

        #region Field for auto-reconnect
        /// <summary>
        /// CancellationTokenSource auto-reconnect
        /// </summary>
        internal CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        /// <summary>
        /// isReconnecting
        /// </summary>
        internal bool isReconnecting = false;
        /// <summary>
        /// _isSafe
        /// </summary>
        internal int _isSafe = 1;
        /// <summary>
        /// _isSafeMethod
        /// </summary>
        internal int _isSafeMethod = 1;
        /// <summary>
        /// _reconnectLimitTimer
        /// </summary>
        internal System.Timers.Timer _reconnectLimitTimer;
        /// <summary>
        /// _breakTimer
        /// </summary>
        internal System.Timers.Timer _breakTimer;
        /// <summary>
        /// _selfTestTimer
        /// </summary>
        internal System.Timers.Timer _selfTestTimer;
        /// <summary>
        /// _queue
        /// </summary>
        internal readonly TaskQueue _queue = new TaskQueue();
        /// <summary>
        /// _breakInterval
        /// </summary>
        internal int _breakInterval = 50;
        /// <summary>
        /// _selfTestCheckInterval
        /// </summary>
        internal int _selfTestCheckInterval = 500;
        /// <summary>
        /// _reconnectLimitInterval
        /// </summary>
        internal int _reconnectLimitInterval = 600000;
        /// <summary>
        /// _autoReconnect
        /// </summary>
        internal AutoReconnect _autoReconnect = AutoReconnect.None;

        #endregion

        /// <summary>
        /// Auto-Reconnect
        /// </summary>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        private async Task ReconnectAsync()
        {
            if (Interlocked.Exchange(ref _isSafe, 0) != 1)
                return;
            if (_autoReconnect == AutoReconnect.Limit)
                _reconnectLimitTimer.Start();
            isReconnecting = true;
            await Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await Task.Delay(50, _cancellationTokenSource.Token).ConfigureAwait(false);
                        if (_autoReconnect == AutoReconnect.JustDetectDisconnected)
                        {
                            _breakTimer.Start();
                            break;
                        }
                        if (_tcpclient != null)
                        {
                            _tcpclient.Close();
                            if (_stream != null)
                                _stream.Close();
                            _tcpclient = null;
                        }
                        _tcpclient = new TcpClient(_iPAddress, Port);
                        _stream = _tcpclient.GetStream();
                        _connected = true;
                        Reconnected?.Invoke(this, null);
                        if (_autoReconnect == AutoReconnect.None)
                            break;
                        _breakTimer.Start();
                        break;
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (Exception)
                    {
                        if (_tcpclient != null)
                        {
                            _tcpclient.Close();
                            if (_stream != null)
                                _stream.Close();
                            _tcpclient = null;
                        }
                    }
                }
            }).ConfigureAwait(false);
            if (_cancellationTokenSource.IsCancellationRequested)
                _cancellationTokenSource = new CancellationTokenSource();
            isReconnecting = false;
            _reconnectLimitTimer.Stop();
            Interlocked.Exchange(ref _isSafe, 1);
        }
        /// <summary>
        /// Read and Write data
        /// </summary>
        /// <param name="writeLenght"></param>
        /// <param name="readLenght"></param>
        internal void StreamData(int writeLenght, int readLenght)
        {
            try
            {
                _selfTestTimer.Stop();
                _breakTimer.Stop();
                _reconnectLimitTimer.Stop();
                _stream.Write(SendBuffer, 0, writeLenght);
                _stream.Read(ReceveiBuffer, 0, readLenght);
                if (_autoReconnect == AutoReconnect.None)
                    return;
                _breakTimer.Start();
            }
            catch (NullReferenceException)
            {
                _breakTimer.Stop();
                throw new SocketNotOpenedException("Socket not opened.");
            }
            catch (Exception)
            {
                if (isReconnecting || !_isConnectStart) //test
                {
                    throw new ConnectionException("Connection was aborted.");
                }
                InnerClose();
                if (_autoReconnect == AutoReconnect.None || _autoReconnect == AutoReconnect.JustDetectDisconnected)
                {
                    throw new ConnectionException("Connection was aborted.");
                }
                else
                {
                    if (!isReconnecting)
                    {
                        ConfiguredTaskAwaitable t = ReconnectAsync().ConfigureAwait(false);
                        throw new ConnectionException("Connection was aborted.");
                    }
                }
            }
        }
        /// <summary>
        /// Close Connect Inner
        /// </summary>
        internal void InnerClose()
        {
            _breakTimer.Stop();
            _selfTestTimer.Stop();
            _reconnectLimitTimer.Stop();
            if (_tcpclient != null)
                _tcpclient.Close();
            if (_stream != null)
                _stream.Close();
            _tcpclient = null;
            _connected = false;
            if (_isConnectStart)
                LostConnect?.Invoke(this, null);
        }
        /// <summary>
        /// Occurs when an established connection is lost.
        /// </summary>
        public event EventHandler<EventArgs> LostConnect;
        /// <summary>
        /// Occurs when reconnecting to the server successfully.
        /// </summary>
        public event EventHandler<EventArgs> Reconnected;
        #region Properties 

        /// <summary>
        /// Gets or sets the Auto-reconnect mode when the connection is lost.
        /// </summary>
        public AutoReconnect AutoReconnect
        {
            get => _autoReconnect;
            set
            {
                if (value == AutoReconnect.JustDetectDisconnected)
                {
                    if (_isConnectStart && !_breakTimer.Enabled)
                        _breakTimer.Start();
                    if (isReconnecting)
                        _cancellationTokenSource.Cancel();
                }

                else if (value != 0) //Forever, Limit
                {
                    if (_isConnectStart && !_breakTimer.Enabled)
                        _breakTimer.Start();

                }
                else
                {
                    _breakTimer.Stop();
                    _selfTestTimer.Stop();
                    _reconnectLimitTimer.Stop();
                    if (isReconnecting)
                        _cancellationTokenSource.Cancel();
                }
                _autoReconnect = value;
            }
        }
        /// <summary>
        ///Gets or sets the interval from when the connection is lost to the server is detected
        ///, expressed in milliseconds, at which attempt to auto-reconnect to the server will be stopped.<para></para>
        /// Note: Only works when the auto-reconnect mode is <see cref="AutoReconnect.Limit"/>
        /// </summary>
        public int LimitInterval
        {
            get => _reconnectLimitInterval;
            set
            {
                _reconnectLimitInterval = value >= 1 ? value : throw new ArgumentOutOfRangeException(nameof(LimitInterval), "'Value '" + value + "' is not a valid value for Interval. Interval must be greater than 0.");
                _reconnectLimitTimer.Interval = value;
            }
        }
        /// <summary>
        ///Gets or sets the interval since the last communication with the server
        ///, expressed in milliseconds, at which the established connection with the server is checked.
        /// </summary>
        public int BreakInterval
        {
            get => _breakInterval;
            set
            {
                _breakInterval = value >= 1 ? value : throw new ArgumentOutOfRangeException(nameof(BreakInterval), "'Value '" + value + "' is not a valid value for Interval. Interval must be greater than 0.");
                _breakTimer.Interval = value;
            }
        }
        #endregion Properties 
        #region Event
        /// <summary>
        /// Occurs when communication in trouble.
        /// </summary>
        public event EventHandler<TroubleshootingEventArgs> Trouble;

        #endregion Event
        #region Support methods
        /// <summary>
        /// SetupBuffer
        /// </summary>
        protected internal override void SetupBuffer()
        {
            SendBuffer[0] = 0x50;
            SendBuffer[1] = 0;
            SendBuffer[2] = 0;
            SendBuffer[3] = 0xff;
            SendBuffer[4] = 0xff;
            SendBuffer[5] = 0x03;
            for (int i = 6; i <= 20; i++)
            {
                SendBuffer[i] = 0;
            }
            SendBuffer[9] = 0x01;
        }
       
        /// <summary>
        /// check connection for auto-reconnect
        /// </summary>
        /// <param name="loopbackMessage">loopbackMessage</param>
        private void SelfTestCheckConnection(string loopbackMessage)
        {
            if (Interlocked.Exchange(ref _isSafeMethod, 0) != 1)
                return;
            foreach (char ch in loopbackMessage)
            {
                if ((ch < '0' || ch > '9') && (ch < 'A' || ch > 'F') || loopbackMessage.Length > 960)
                {
                    Interlocked.Exchange(ref _isSafeMethod, 1);
                    throw new ArgumentOutOfRangeException(nameof(loopbackMessage), loopbackMessage, "The order of character strings for up to 960 1-byte characters (\"0\" to \"9\", \"A\" to \"F\") is sent from the head.");
                }
            }
            byte[] bytes_datalength = BitConverter.GetBytes(8 + loopbackMessage.Length);
            SendBuffer[7] = bytes_datalength[0];
            SendBuffer[8] = bytes_datalength[1];
            SendBuffer[11] = 0x19;
            SendBuffer[12] = 0x06;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            byte[] loopbackMessagelength_bytes = BitConverter.GetBytes(loopbackMessage.Length);
            SendBuffer[15] = loopbackMessagelength_bytes[0];
            SendBuffer[16] = loopbackMessagelength_bytes[1];
            byte[] loopbackMessage_bytes = Encoding.ASCII.GetBytes(loopbackMessage);
            Array.Copy(loopbackMessage_bytes, 0, SendBuffer, 17, loopbackMessage_bytes.Length);
            try
            {

                _stream.Write(SendBuffer, 0, 17 + loopbackMessage.Length);
                _stream.Read(ReceveiBuffer, 0, 13 + loopbackMessage.Length);

                if (ReceveiBuffer[9] != 0 || ReceveiBuffer[10] > 0)
                {
                    int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                    Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                }
                else if (ReceveiBuffer[11] == loopbackMessagelength_bytes[0] && ReceveiBuffer[12] == loopbackMessagelength_bytes[1] && loopbackMessage == Encoding.ASCII.GetString(ReceveiBuffer, 13, loopbackMessage.Length))
                {

                }
            }
            catch
            {
                //Close();
                _selfTestTimer.Stop();
                _breakTimer.Stop();
                _reconnectLimitTimer.Stop();
                if (isReconnecting)
                {
                    _cancellationTokenSource.Cancel();
                    // isReconnecting = false;
                }
                if (_tcpclient != null)
                {
                    _tcpclient.Close();
                }
                if (_stream != null)
                    _stream.Close();
                _tcpclient = null;
                _connected = false;
                LostConnect?.Invoke(this, null);
                if (_autoReconnect == AutoReconnect.None || isReconnecting || _autoReconnect == AutoReconnect.JustDetectDisconnected)
                {
                    Interlocked.Exchange(ref _isSafeMethod, 1);
                    return;
                }
                ConfiguredTaskAwaitable t = ReconnectAsync().ConfigureAwait(false);
            }
            Interlocked.Exchange(ref _isSafeMethod, 1);

        }
        
        /// <summary>
        /// Read and Write data as async.
        /// </summary>
        /// <param name="writeLenght">writeLenght</param>
        /// <param name="readLenght">readLenght</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        private async Task StreamDataAsync(int writeLenght, int readLenght)
        {
            try
            {
                _selfTestTimer.Stop();
                _breakTimer.Stop();
                _reconnectLimitTimer.Stop();
                await _stream.WriteAsync(SendBuffer, 0, writeLenght).ConfigureAwait(false);
                await _stream.ReadAsync(ReceveiBuffer, 0, readLenght).ConfigureAwait(false);
                if (_autoReconnect == AutoReconnect.None)
                    return;
                _breakTimer.Start();
            }
            catch (NullReferenceException)
            {
                _breakTimer.Stop();
                throw new SocketNotOpenedException("Socket not opened.");
            }
            catch (Exception)
            {
                if (isReconnecting || !_isConnectStart) //test
                {
                    throw new ConnectionException("Connection was aborted.");
                }
                InnerClose();
                if (_autoReconnect == AutoReconnect.None || _autoReconnect == AutoReconnect.JustDetectDisconnected)
                {
                    throw new ConnectionException("Connection was aborted.");
                }
                else
                {
                    if (!isReconnecting)
                    {
                        ConfiguredTaskAwaitable t = ReconnectAsync().ConfigureAwait(false);
                        throw new ConnectionException("Connection was aborted.");
                    }
                }
            }
        }
        /// <summary>
        /// split label name
        /// </summary>
        /// <param name="label">label</param>
        /// <param name="device">device</param>
        /// <param name="low_num">low_num</param>
        /// <param name="mid_num">mid_num</param>
        /// <param name="high_num">high_num</param>
        /// <returns>index</returns>
        protected internal override int SettingDevice(string label, out byte device, out byte low_num, out byte mid_num, out byte high_num)
        {
            if (label == null || label == "")
            {
                throw new InvalidDeviceLabelNameException("The label name of device is invalid.", nameof(label));
            }
            label = sWhitespace.Replace(label, "").ToUpper();
            int device_num;
            if (label[0] == 'S' && label[1] == 'B')
            {
                label = label.Substring(2);
                device = 0xA1;
                if (!int.TryParse(label, NumberStyles.HexNumber, null, out device_num) || device_num > 0x7FFF)
                {
                    throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                }
                byte[] bytes = BitConverter.GetBytes(device_num);
                low_num = bytes[0];
                mid_num = bytes[1];
                high_num = bytes[2];
            }
            else if (label[0] == 'S' && label[1] == 'W')
            {
                label = label.Substring(2);
                device = 0xB5;
                if (!int.TryParse(label, NumberStyles.HexNumber, null, out device_num) || device_num > 0x7FFF)
                {
                    throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                }
                byte[] bytes = BitConverter.GetBytes(device_num);
                low_num = bytes[0];
                mid_num = bytes[1];
                high_num = bytes[2];
            }
            else if (label[0] == 'B' && label[1] != 'L') //Bit-B (not BL)
            {
                label = label.Substring(1);
                device = 0xA0;
                if (!int.TryParse(label, NumberStyles.HexNumber, null, out device_num) || device_num > 0x7FFF)
                {
                    throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                }
                // GetNameDeviceAndCheckAddress("B", device_num); //Ném lỗi label trong hàm này
                byte[] bytes = BitConverter.GetBytes(device_num);
                low_num = bytes[0];
                mid_num = bytes[1];
                high_num = bytes[2];
            }
            else if (label[0] == 'W')
            {
                label = label.Substring(1);
                device = 0xB4;
                if (!int.TryParse(label, NumberStyles.HexNumber, null, out device_num) || device_num > 0x7FFF)
                {
                    throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                }
                byte[] bytes = BitConverter.GetBytes(device_num);
                low_num = bytes[0];
                mid_num = bytes[1];
                high_num = bytes[2];
            }
            else if (label[0] == 'X')
            {
                label = label.Substring(1);
                device = 0x9C;
                if (!int.TryParse(label, out device_num) || device_num > 1777)
                {
                    throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                }
                device_num = ConvertOctalToDecimal(device_num);
                byte[] bytes = BitConverter.GetBytes(device_num);
                low_num = bytes[0];
                mid_num = bytes[1];
                high_num = bytes[2];
            }
            else if (label[0] == 'Y')
            {
                label = label.Substring(1);
                device = 0x9D;
                if (!int.TryParse(label, out device_num) || device_num > 1777)
                {
                    throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                }
                device_num = ConvertOctalToDecimal(device_num);
                byte[] bytes = BitConverter.GetBytes(device_num);
                low_num = bytes[0];
                mid_num = bytes[1];
                high_num = bytes[2];
            }
            else
            {
                int device_num_temp = 0;
                for (int index = 0; index < label.Length; ++index)
                {
                    if (label[index] >= '0' && label[index] <= '9')
                    {
                        device_num_temp = index;
                        break;
                    }
                }
                if (!int.TryParse(label.Substring(device_num_temp), out device_num))
                {
                    throw new InvalidDeviceLabelNameException("The label name of device is invalid.", nameof(label));
                }
                device = GetNameDeviceAndCheckAddress(label.Substring(0, device_num_temp), device_num); //Ném lỗi label trong hàm này
                byte[] bytes = BitConverter.GetBytes(device_num);
                low_num = bytes[0];
                mid_num = bytes[1];
                high_num = bytes[2];
            }

            return device_num;
        }
        /// <summary>
        /// GetNameDevice
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        internal byte GetNameDeviceAndCheckAddress(string device, int devicenum)
        {
            switch (device)
            {
                case "B":
                    if (devicenum > 0x7FFF)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                    return 160;
                case "BL":
                    if (devicenum > 31)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                    return 220;
                case "CC":
                    if (devicenum > 1023)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                    return 195;
                case "CN":
                    if (devicenum > 1023)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                    return 197;
                case "CS":
                    if (devicenum > 1023)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                    return 196;
                case "D":
                    if (devicenum > 7999)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                    return 168;
                case "F":
                    if (devicenum > 32767)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                    return 147;
                case "L":
                    if (devicenum > 32767)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                    return 146;
                case "LCN":
                    if (devicenum > 1023)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                    return 86;
                case "LZ":
                    if (devicenum > 1)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                    return 98;
                case "M":
                    if (devicenum > 32767)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                    return 144;
                case "R":
                    if (devicenum > 32767)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                    return 175;
                case "S":
                    if (devicenum > 4095)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                    return 152;
                case "SB":
                    if (devicenum > 0x7FFF)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                    return 161;
                case "SC":
                    if (devicenum > 1023)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                    return 198;
                case "SD":
                    if (devicenum > 11999)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                    return 169;
                case "SM":
                    if (devicenum > 9999)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                    return 145;
                case "SN":
                    if (devicenum > 1023)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                    return 200;
                case "SS":
                    if (devicenum > 1023)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                    return 199;
                case "STN":
                    if (devicenum > 1023)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                    return 200;
                case "SW":
                    if (devicenum > 0x7FFF)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                    return 181;
                case "TC":
                    if (devicenum > 1023)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                    return 192;
                case "TN":
                    if (devicenum > 1023)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                    return 194;
                case "TS":
                    if (devicenum > 1023)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                    return 193;
                case "W":
                    if (devicenum > 0x7FFF)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                    return 180;
                case "X":
                    if (devicenum > 1777)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                    return 156;
                case "Y":
                    if (devicenum > 1777)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                    return 157;
                case "Z":
                    if (devicenum > 19)
                        throw new DeviceAddressOutOfRangeException("The address of device was out of the range of the PLC.", "label");
                    return 204;
                default:
                    throw new InvalidDeviceLabelNameException("The label name of device is invalid.");
            }
        }
        /// <summary>
        /// _selfTestTimer_Elapsed
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">ElapsedEventArgs</param>
        private void _selfTestTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_autoReconnect == AutoReconnect.None)
                _selfTestTimer.Stop();
            else
                SelfTestCheckConnection(_selfTestConnectionString);
        }
        /// <summary>
        /// _breakTimer_Elapsed
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">ElapsedEventArgs</param>
        private void _breakTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _breakTimer.Stop();
            _selfTestTimer.Start();
        }
        /// <summary>
        /// _reconnectLimitTimer_Elapsed
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">ElapsedEventArgs</param>
        private void _reconnectLimitTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _reconnectLimitTimer.Stop();
            if (_autoReconnect == AutoReconnect.None || _autoReconnect == AutoReconnect.JustDetectDisconnected)
                return;
            _cancellationTokenSource.Cancel();
        }
        #endregion Support methods

        #region Methods

        #endregion Methods

        /// <summary>
        /// Establish connection to the server.
        /// </summary>
        public override void Connect()
        {
            _tcpclient = new TcpClient() { SendTimeout = _writeTimeout, ReceiveTimeout = _readTimeout };
            _isConnectStart = true;
            try
            {
                _tcpclient.ConnectAsync(_iPAddress, Port).Wait(ReadTimeout);
            }
            catch
            {
                //  LostConnect?.Invoke(this, null);
                if (_autoReconnect != AutoReconnect.None && !isReconnecting)
                {
                    ConfiguredTaskAwaitable t = ReconnectAsync().ConfigureAwait(false);
                }
                throw new Exception("Connect Timeout");
            }
            _stream = _tcpclient.GetStream();
            _stream.ReadTimeout = _readTimeout;
            _stream.WriteTimeout = _writeTimeout;
            _connected = true;
            if (_autoReconnect == AutoReconnect.None)
                return;
            _breakTimer.Start();
        }
        /// <summary>
        /// Establish connection to the server using the specified ip-address and port number.
        /// </summary>
        /// <param name="ipaddress">IP Address of the server.</param>
        /// <param name="port">Port number of the server.</param>
        public void Connect(string ipaddress, int port)
        {
            if (!ValidateIPv4(ipaddress)) throw new ArgumentException("Invalid IP address.", nameof(ipaddress));
            _iPAddress = ipaddress;
            Port = port;
            _tcpclient = new TcpClient() { SendTimeout = _writeTimeout, ReceiveTimeout = _readTimeout };
            _isConnectStart = true;
            try
            {
                _tcpclient.ConnectAsync(_iPAddress, Port).Wait(ReadTimeout);
            }
            catch
            {
                // LostConnect?.Invoke(this, null);
                if (_autoReconnect != AutoReconnect.None && !isReconnecting)
                {
                    ConfiguredTaskAwaitable t = ReconnectAsync().ConfigureAwait(false);
                }
                throw new Exception("Connect Timeout");
            }
            _stream = _tcpclient.GetStream();
            _stream.ReadTimeout = _readTimeout;
            _stream.WriteTimeout = _writeTimeout;
            _connected = true;
            if (_autoReconnect == AutoReconnect.None)
                return;
            _breakTimer.Start();
        }
        /// <summary>
        /// Establish connection to the server as an asynchronous operation.
        /// </summary>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        public async Task ConnectAsync()
        {
            _tcpclient = new TcpClient() {SendTimeout = _writeTimeout,ReceiveTimeout = _readTimeout};
            _isConnectStart = true;
            try
            {
                await _tcpclient.ConnectAsync(_iPAddress, Port).ConfigureAwait(false);
            }
            catch
            {
                if (_autoReconnect != AutoReconnect.None && !isReconnecting)
                {
                    ConfiguredTaskAwaitable t = ReconnectAsync().ConfigureAwait(false);
                }
                throw new Exception("Connect Timeout");
            }
            _stream = _tcpclient.GetStream();
            _stream.ReadTimeout = _readTimeout;
            _stream.WriteTimeout = _writeTimeout;
            _connected = true;
            if (_autoReconnect == AutoReconnect.None)
                return;
            _breakTimer.Start();
        }
        /// <summary>
        /// Establish connection to the server using the specified ip-address and port number as an asynchronous operation.
        /// </summary>
        /// <param name="ipaddress">IP Address of the server.</param>
        /// <param name="port">Port number of the server.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        public async Task ConnectAsync(string ipaddress, int port)
        {
            if (!ValidateIPv4(ipaddress)) throw new ArgumentException("Invalid IP address.", nameof(ipaddress));
            Port = port;
            _tcpclient = new TcpClient() { SendTimeout = _writeTimeout, ReceiveTimeout = _readTimeout };
            _isConnectStart = true;
            try
            {
                await _tcpclient.ConnectAsync(_iPAddress, Port).ConfigureAwait(false);
            }
            catch
            {
                if (_autoReconnect != AutoReconnect.None && !isReconnecting)
                {
                    ConfiguredTaskAwaitable t = ReconnectAsync().ConfigureAwait(false);
                }
                throw new Exception("Connect Timeout");
            }
            _stream = _tcpclient.GetStream();
            _stream.ReadTimeout = _readTimeout;
            _stream.WriteTimeout = _writeTimeout;
            _connected = true;
            if (_autoReconnect == AutoReconnect.None)
                return;
            _breakTimer.Start();
        }
        /// <summary>
        /// Close connection to the server.
        /// </summary>
        public override void Disconnect()
        {
            _selfTestTimer.Stop();
            _breakTimer.Stop();
            _reconnectLimitTimer.Stop();
            _isConnectStart = false;
            if (isReconnecting)
            {
                _cancellationTokenSource.Cancel();
                // isReconnecting = false;
            }
            if (_tcpclient != null) _tcpclient.Close();
            if (_stream != null)
                _stream.Close();
            _tcpclient = null;
            _connected = false;
        }

        /// <summary>
        /// Write single value to the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">A single value to be written.</param>
        internal  void WriteDevice(string label, bool value)
        {
            SendBuffer[7] = 0x0D;
            SendBuffer[8] = 0x00;
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x14;
            SendBuffer[13] = 0x01;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            SendBuffer[19] = 0x01;
            SendBuffer[20] = 0x00;
            if (value)
                SendBuffer[21] = 0x10;
            else
                SendBuffer[21] = 0x00;
            StreamData(22, 11);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            StreamData(0, 20 - 11); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// Write single value to the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">A single value to be written.</param>
        internal void WriteDevice(string label, short value)
        {
            SendBuffer[7] = 0x0E;
            SendBuffer[8] = 0x00;
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x14;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            SendBuffer[19] = 0x01;
            SendBuffer[20] = 0x00;
            byte[] bytes = BitConverter.GetBytes(value);
            SendBuffer[21] = bytes[0];
            SendBuffer[22] = bytes[1];
            StreamData(23, 11);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            StreamData(0, 20 - 11); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// Write single value to the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">A single value to be written.</param>

        internal void WriteDevice(string label, int value)
        {
            SendBuffer[7] = 0x10;
            SendBuffer[8] = 0x00;
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x14;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            SendBuffer[19] = 0x02;
            SendBuffer[20] = 0x00;
            byte[] bytes = BitConverter.GetBytes(value);
            for (int index = 0; index < 4; ++index)
                SendBuffer[21 + index] = bytes[index];
            StreamData(25, 11);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            StreamData(0, 20 - 11); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// Write single value to the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">A single value to be written.</param>
        internal void WriteDevice(string label, float value)
        {
            SendBuffer[7] = 0x10;
            SendBuffer[8] = 0x00;
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x14;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            SendBuffer[19] = 0x02;
            SendBuffer[20] = 0x00;
            byte[] bytes = BitConverter.GetBytes(value);
            for (int index = 0; index < 4; ++index)
                SendBuffer[21 + index] = bytes[index];
            StreamData(25, 11);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            StreamData(0, 20 - 11); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// Write single value to the server as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">A single value to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        internal async Task WriteDeviceAsync(string label, bool value) => await _queue.Enqueue(() => WriteDeviceSubAsync(label, value)).ConfigureAwait(false);
        /// <summary>
        /// Write single value to the server as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">A single value to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        internal async Task WriteDeviceAsync(string label, short value) => await _queue.Enqueue(() => WriteDeviceSubAsync(label, value)).ConfigureAwait(false);
        /// <summary>
        /// Write single value to the server as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">A single value to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        internal async Task WriteDeviceAsync(string label, int value) => await _queue.Enqueue(() => WriteDeviceSubAsync(label, value)).ConfigureAwait(false);
        /// <summary>
        /// Write single value to the server as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">A single value to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        internal async Task WriteDeviceAsync(string label, float value) => await _queue.Enqueue(() => WriteDeviceSubAsync(label, value)).ConfigureAwait(false);
        /// <summary>
        /// Write single value to the server as an asynchronous operation.
        /// </summary>
        /// <typeparam name="T">The data type of value. (EX: <see cref="bool"></see>, <see cref="short"/>, <see cref="float"/>, etc.)</typeparam>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">A single value to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        public async Task WriteDeviceAsync<T>(string label, T value) where T : struct
        {
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Boolean:
                    await WriteDeviceAsync(label, (bool)Convert.ChangeType(value, TypeCode.Boolean));
                    break;
                case TypeCode.Int16:
                    await WriteDeviceAsync(label, (short)Convert.ChangeType(value, TypeCode.Int16));
                    break;
                case TypeCode.UInt16:
                    await WriteDeviceAsync(label, (short)(ushort)Convert.ChangeType(value, TypeCode.UInt16));
                    break;
                case TypeCode.Int32:
                    await WriteDeviceAsync(label, (int)Convert.ChangeType(value, TypeCode.Int32));
                    break;
                case TypeCode.UInt32:
                    await WriteDeviceAsync(label, (int)(uint)Convert.ChangeType(value, TypeCode.UInt32));
                    break;
                case TypeCode.Single:
                    await WriteDeviceAsync(label, (float)Convert.ChangeType(value, TypeCode.Single));
                    break;
                default:
                    throw new InvalidCastException("Invalid input data type.");
            }
        }
        /// <summary>
        ///  Write multiple values to the server in a batch.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="values">Values to be written.</param>
        internal void WriteDeviceBlock(string label, params bool[] values)
        {
            byte[] coillength_bytes = BitConverter.GetBytes(values.Length);
            byte[] byteArraySlmp = ConvertBoolArrayToByteArraySLMP(values);
            byte[] datalength_bytes = BitConverter.GetBytes(12 + byteArraySlmp.Length);
            SendBuffer[7] = datalength_bytes[0];
            SendBuffer[8] = datalength_bytes[1];
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x14;
            SendBuffer[13] = 0x01;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            SendBuffer[19] = coillength_bytes[0];
            SendBuffer[20] = coillength_bytes[1];
            for (int index = 0; index < byteArraySlmp.Length; ++index)
                SendBuffer[21 + index] = byteArraySlmp[index];
            StreamData(21 + byteArraySlmp.Length, 11);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            StreamData(0, 20 - 11); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        ///  Write multiple values to the server in a batch.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="values">Values to be written.</param>
        internal void WriteDeviceBlock(string label, params short[] values)
        {
            int datalength = values != null ? values.Length : throw new ArgumentNullException(nameof(values), "Array data must be non-null");
            byte[] datalength_bytes = BitConverter.GetBytes(12 + datalength * 2);
            SendBuffer[7] = datalength_bytes[0];
            SendBuffer[8] = datalength_bytes[1];
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x14;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            byte[] bytes2 = BitConverter.GetBytes(datalength);
            SendBuffer[19] = bytes2[0];
            SendBuffer[20] = bytes2[1];
            int k = 0;
            for (int i = 0; i < datalength; ++i)
            {
                byte[] bytes3 = BitConverter.GetBytes(values[i]);
                for (int j = 0; j < 2; ++j)
                    SendBuffer[21 + k + j] = bytes3[j];
                k += 2;
            }
            StreamData(21 + datalength * 2, 11);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            StreamData(0, 20 - 11); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        ///  Write multiple values to the server in a batch.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="values">Values to be written.</param>
        internal void WriteDeviceBlock(string label, params int[] values)
        {
            int datalength = values != null ? values.Length : throw new ArgumentNullException(nameof(values), "Array data must be non-null");
            byte[] datalength_bytes = BitConverter.GetBytes(12 + datalength * 4);
            SendBuffer[7] = datalength_bytes[0];
            SendBuffer[8] = datalength_bytes[1];
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x14;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            byte[] bytes2 = BitConverter.GetBytes(datalength * 2);
            SendBuffer[19] = bytes2[0];
            SendBuffer[20] = bytes2[1];
            int k = 0;
            for (int i = 0; i < datalength; ++i)
            {
                byte[] bytes3 = BitConverter.GetBytes(values[i]);
                for (int j = 0; j < 4; ++j)
                {
                    SendBuffer[21 + k] = bytes3[j];
                    ++k;
                }
            }
            StreamData(21 + datalength * 4, 11);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            StreamData(0, 20 - 11); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        ///  Write multiple values to the server in a batch.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="values">Values to be written.</param>
        internal void WriteDeviceBlock(string label, params float[] values)
        {
            int datalength = values != null ? values.Length : throw new ArgumentNullException(nameof(values), "Array data must be non-null");
            byte[] datalength_bytes = BitConverter.GetBytes(12 + datalength * 4);
            SendBuffer[7] = datalength_bytes[0];
            SendBuffer[8] = datalength_bytes[1];
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x14;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            byte[] bytes2 = BitConverter.GetBytes(datalength * 2);
            SendBuffer[19] = bytes2[0];
            SendBuffer[20] = bytes2[1];
            int k = 0;
            for (int i = 0; i < datalength; ++i)
            {
                byte[] bytes3 = BitConverter.GetBytes(values[i]);
                for (int j = 0; j < 4; ++j)
                {
                    SendBuffer[21 + k] = bytes3[j];
                    ++k;
                }
            }
            StreamData(21 + datalength * 4, 11);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            StreamData(0, 20 - 11); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// Write multiple values to the server in a batch as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="values">Values to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        internal async Task WriteDeviceBlockAsync(string label, bool[] values) => await _queue.Enqueue(() => WriteDeviceBlockSubAsync(label, values)).ConfigureAwait(false);
        /// <summary>
        /// Write multiple values to the server in a batch as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="values">Values to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        internal async Task WriteDeviceBlockAsync(string label, short[] values) => await _queue.Enqueue(() => WriteDeviceBlockSubAsync(label, values)).ConfigureAwait(false);
        /// <summary>
        /// Write multiple values to the server in a batch as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="values">Values to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        internal async Task WriteDeviceBlockAsync(string label, int[] values) => await _queue.Enqueue(() => WriteDeviceBlockSubAsync(label, values)).ConfigureAwait(false);
        /// <summary>
        /// Write multiple values to the server in a batch as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="values">Values to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        internal async Task WriteDeviceBlockAsync(string label, float[] values) => await _queue.Enqueue(() => WriteDeviceBlockSubAsync(label, values)).ConfigureAwait(false);
        /// <summary>
        /// Write multiple values to the server in a batch as an asynchronous operation.
        /// </summary>
        /// <typeparam name="T">The data type of value. (EX: <see cref="bool"></see>, <see cref="short"/>, <see cref="float"/>, etc.)</typeparam>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="values">Values to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        public async Task WriteDeviceBlockAsync<T>(string label, params T[] values) where T : struct
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            int length = values.Length;
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Boolean:
                    {
                        if (length < 1 || length > 3584) //Page57 (SLMP-Mitsubishi.PDF)
                            throw new SizeOutOfRangeException("Size must be 1 to 3584 points.", nameof(values));
                        bool[] results = new bool[values.Length];
                        for (int i = 0; i < length; ++i)
                            results[i] = (bool)Convert.ChangeType(values[i], TypeCode.Boolean);
                        await WriteDeviceBlockAsync(label, results);
                        break;
                    }
                case TypeCode.Int16:
                    {
                        if (length < 1 || length > 960) //Page57 (SLMP-Mitsubishi.PDF)
                            throw new SizeOutOfRangeException("Size must be 1 to 960 points.", nameof(values));
                        short[] results = new short[values.Length];
                        for (int i = 0; i < length; ++i)
                            results[i] = (short)Convert.ChangeType(values[i], TypeCode.Int16);
                        await WriteDeviceBlockAsync(label, results);
                        break;
                    }
                case TypeCode.UInt16:
                    {
                        if (length < 1 || length > 960) //Page57 (SLMP-Mitsubishi.PDF)
                            throw new SizeOutOfRangeException("Size must be 1 to 960 points.", nameof(values));
                        short[] results = new short[values.Length];
                        for (int i = 0; i < length; ++i)
                            results[i] = (short)(ushort)Convert.ChangeType(values[i], TypeCode.UInt16);
                        await WriteDeviceBlockAsync(label, results);
                        break;
                    }
                case TypeCode.Int32:
                    {
                        if (length < 1 || length > 480) //Page57 (SLMP-Mitsubishi.PDF)
                            throw new SizeOutOfRangeException("Size must be 1 to 480 points.", nameof(values));
                        int[] results = new int[values.Length];
                        for (int i = 0; i < length; ++i)
                            results[i] = (int)Convert.ChangeType(values[i], TypeCode.Int32);
                        await WriteDeviceBlockAsync(label, results);
                        break;
                    }
                case TypeCode.UInt32:
                    {
                        if (length < 1 || length > 480) //Page57 (SLMP-Mitsubishi.PDF)
                            throw new SizeOutOfRangeException("Size must be 1 to 480 points.", nameof(values));
                        int[] results = new int[values.Length];
                        for (int i = 0; i < length; ++i)
                            results[i] = (int)(uint)Convert.ChangeType(values[i], TypeCode.UInt32);
                        await WriteDeviceBlockAsync(label, results);
                        break;
                    }
                case TypeCode.Single:
                    {
                        if (length < 1 || length > 480) //Page57 (SLMP-Mitsubishi.PDF)
                            throw new SizeOutOfRangeException("Size must be 1 to 480 points.", nameof(values));
                        float[] results6 = new float[values.Length];
                        for (int i = 0; i < length; ++i)
                            results6[i] = (float)Convert.ChangeType(values[i], TypeCode.Single);
                        await WriteDeviceBlockAsync(label, results6);
                        break;
                    }
                default:
                    throw new InvalidCastException("Invalid input data type.");
            }
        }
        /// <summary>
        /// Write multiple values to the server randomly.
        /// </summary>
        /// <param name="bits"><see cref="dotPLC.Mitsubishi.Types.Bit"/> values to be written.</param>
        public  void WriteDeviceRandom(params Bit[] bits)
        {
            if (bits == null)
                throw new ArgumentNullException(nameof(bits));
            int datalength = bits.Length;
            if (datalength < 1 || datalength > 188) //Page57 (SLMP-Mitsubishi.PDF)
                throw new SizeOutOfRangeException("Size must be 1 to 3584 points.", nameof(bits));

            byte[] datalenght_bytes = BitConverter.GetBytes(7 + datalength * 5);
            SendBuffer[7] = datalenght_bytes[0];
            SendBuffer[8] = datalenght_bytes[1];
            SendBuffer[11] = 0x02;
            SendBuffer[12] = 0x14;
            SendBuffer[13] = 0x01;
            SendBuffer[14] = 0x00;
            SendBuffer[15] = (byte)datalength;
            int index_B = 16;
            for (int i = 0; i < datalength; ++i)
            {
                if (bits[i] == null)
                    throw new ArgumentNullException(nameof(bits), string.Format("bits[{0}] is null.", i));
                SettingDevice(bits[i].Label, out SendBuffer[index_B + 3], out SendBuffer[index_B], out SendBuffer[index_B + 1], out SendBuffer[index_B + 2]);
                if (bits[i].Value)
                    SendBuffer[index_B + 4] = 0x01;
                else
                    SendBuffer[index_B + 4] = 0x00;
                index_B += 5;
            }
            StreamData(16 + datalength * 5, 11);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            StreamData(0, 20 - 11); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// Write multiple values to the server randomly.
        /// </summary>
        /// <param name="words"><see cref="dotPLC.Mitsubishi.Types.Word"/> values to be written.</param>
        public  void WriteDeviceRandom(params Word[] words)
        {
            if (words == null)
                throw new ArgumentNullException(nameof(words));
            int datalength = words.Length;
            if (datalength < 1 || datalength > 160) //Page57 (SLMP-Mitsubishi.PDF)
                throw new SizeOutOfRangeException("Size must be 1 to 160 points.", nameof(words));

            byte[] datalenght_bytes = BitConverter.GetBytes(8 + datalength * 6);
            SendBuffer[7] = datalenght_bytes[0];
            SendBuffer[8] = datalenght_bytes[1];
            SendBuffer[11] = 0x02;
            SendBuffer[12] = 0x14;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SendBuffer[15] = (byte)datalength;
            SendBuffer[16] = 0x00;
            int index_W = 17;
            int num = 17 + datalength * 6;
            for (int i = 0; i < datalength; ++i)
            {
                if (words[i] == null)
                    throw new ArgumentNullException(nameof(words), string.Format("words[{0}] is null.", i));
                SettingDevice(words[i].Label, out SendBuffer[index_W + 3], out SendBuffer[index_W], out SendBuffer[index_W + 1], out SendBuffer[index_W + 2]);
                byte[] bytes2 = BitConverter.GetBytes(words[i].Value);
                SendBuffer[index_W + 4] = bytes2[0];
                SendBuffer[index_W + 5] = bytes2[1];
                index_W += 6;
            }
            StreamData(17 + datalength * 6, 11);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            StreamData(0, 20 - 11); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// Write multiple values to the server randomly.
        /// </summary>
        /// <param name="dwords"><see cref="dotPLC.Mitsubishi.Types.DWord"/> values to be written.</param>
        public  void WriteDeviceRandom(params DWord[] dwords)
        {
            if (dwords == null)
                throw new ArgumentNullException(nameof(dwords));
            int datalength = dwords.Length;
            if (datalength < 1 || datalength > 137) //Page57 (SLMP-Mitsubishi.PDF)
                throw new SizeOutOfRangeException("Size must be 1 to 137 points.", nameof(dwords));
            byte[] datalength_bytes = BitConverter.GetBytes(8 + datalength * 8);
            SendBuffer[7] = datalength_bytes[0];
            SendBuffer[8] = datalength_bytes[1];
            SendBuffer[11] = 0x02;
            SendBuffer[12] = 0x14;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SendBuffer[15] = 0x00;
            SendBuffer[16] = (byte)datalength;
            int index_DW = 17;
            for (int i = 0; i < datalength; ++i)
            {
                if (dwords[i] == null)
                    throw new ArgumentNullException(nameof(dwords), string.Format("dwords[{0}] is null.", i));
                SettingDevice(dwords[i].Label, out SendBuffer[index_DW + 3], out SendBuffer[index_DW], out SendBuffer[index_DW + 1], out SendBuffer[index_DW + 2]);
                byte[] data_bytes = BitConverter.GetBytes(dwords[i].Value);
                int k = 0;
                for (int j = 4; j <= 7; ++j)
                {
                    SendBuffer[index_DW + j] = data_bytes[k];
                    ++k;
                }
                index_DW += 8;
            }
            StreamData(17 + datalength * 8, 11);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            StreamData(0, 20 - 11); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// Write multiple values to the server randomly.
        /// </summary>
        /// <param name="floats"><see cref="dotPLC.Mitsubishi.Types.Float"/> values to be written.</param>
        public  void WriteDeviceRandom(params Float[] floats)
        {
            if (floats == null)
                throw new ArgumentNullException(nameof(floats));
            int datalength = floats.Length;
            if (datalength < 1 || datalength > 137) //Page57 (SLMP-Mitsubishi.PDF)
                throw new SizeOutOfRangeException("Size must be 1 to 137 points.", nameof(floats));
            byte[] datalength_bytes = BitConverter.GetBytes(8 + datalength * 8);
            SendBuffer[7] = datalength_bytes[0];
            SendBuffer[8] = datalength_bytes[1];
            SendBuffer[11] = 0x02;
            SendBuffer[12] = 0x14;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SendBuffer[15] = 0x00;
            SendBuffer[16] = (byte)datalength;
            int index_FL = 17;
            for (int i = 0; i < datalength; ++i)
            {
                if (floats[i] == null)
                    throw new ArgumentNullException(nameof(floats), string.Format("floats[{0}] is null.", i));
                SettingDevice(floats[i].Label, out SendBuffer[index_FL + 3], out SendBuffer[index_FL], out SendBuffer[index_FL + 1], out SendBuffer[index_FL + 2]);
                byte[] data_bytes = BitConverter.GetBytes(floats[i].Value);

                int k = 0;
                for (int j = 4; j <= 7; ++j)
                {
                    SendBuffer[index_FL + j] = data_bytes[k];
                    ++k;
                }
                index_FL += 8;
            }
            StreamData(17 + datalength * 8, 11);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            StreamData(0, 20 - 11); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// Write multiple values to the server randomly.
        /// </summary>
        /// <param name="words"><see cref="dotPLC.Mitsubishi.Types.Word"/> values to be written.</param>
        /// <param name="dwords"><see cref="dotPLC.Mitsubishi.Types.DWord"/> values to be written.</param>
        /// <param name="floats"><see cref="dotPLC.Mitsubishi.Types.Float"/> values to be written.</param>
        public void WriteDeviceRandom(Word[] words = null, DWord[] dwords = null, Float[] floats = null)
        {
            if (words == null && dwords == null && floats == null)
                throw new ArgumentNullException(nameof(floats));

            int num_W = words == null ? 0 : words.Length;
            int num_DW = dwords == null ? 0 : dwords.Length;
            int num_FL = floats == null ? 0 : floats.Length;
            int size = num_W * 12 + (num_DW + num_FL) * 14;
            if (size < 1 || size > 1920) //Page57 (SLMP-Mitsubishi.PDF)
                throw new SizeOutOfRangeException("Size must be 1 to 1920.\n[size \u2264 word points x 12 + (double points + float points) x 14 \u2264 1920]");
            byte[] datalength_bytes = BitConverter.GetBytes(8 + num_W * 6 + (num_DW + num_FL) * 8);
            SendBuffer[7] = datalength_bytes[0];
            SendBuffer[8] = datalength_bytes[1];
            SendBuffer[11] = 0x02;
            SendBuffer[12] = 0x14;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SendBuffer[15] = (byte)num_W;
            SendBuffer[16] = (byte)(num_DW + num_FL);
            int index_W = 17; //Word
            int index_DW = 17 + num_W * 6; //Dword
            int index_FL = 17 + num_W * 6 + num_DW * 8; //Float
            if (num_W > 0)
            {
                for (int i = 0; i < num_W; ++i)
                {
                    if (words[i] == null)
                        throw new ArgumentNullException(nameof(words), string.Format("words[{0}] is null.", i));
                    SettingDevice(words[i].Label, out SendBuffer[index_W + 3], out SendBuffer[index_W], out SendBuffer[index_W + 1], out SendBuffer[index_W + 2]);
                    byte[] data_bytes = BitConverter.GetBytes(words[i].Value);
                    SendBuffer[index_W + 4] = data_bytes[0];
                    SendBuffer[index_W + 5] = data_bytes[1];
                    index_W += 6;
                }
            }
            if (num_DW > 0)
            {
                for (int i = 0; i < num_DW; ++i)
                {
                    if (dwords[i] == null)
                        throw new ArgumentNullException(nameof(dwords), string.Format("dwords[{0}] is null.", i));
                    SettingDevice(dwords[i].Label, out SendBuffer[index_DW + 3], out SendBuffer[index_DW], out SendBuffer[index_DW + 1], out SendBuffer[index_DW + 2]);
                    byte[] data_bytes = BitConverter.GetBytes(dwords[i].Value);
                    int k = 0;
                    for (int j = 4; j <= 7; ++j)
                    {
                        SendBuffer[index_DW + j] = data_bytes[k];
                        ++k;
                    }
                    index_DW += 8;
                }
            }
            if (num_FL > 0)
            {
                for (int i = 0; i < num_FL; ++i)
                {
                    if (floats[i] == null)
                        throw new ArgumentNullException(nameof(floats), string.Format("floats[{0}] is null.", i));
                    SettingDevice(floats[i].Label, out SendBuffer[index_FL + 3], out SendBuffer[index_FL], out SendBuffer[index_FL + 1], out SendBuffer[index_FL + 2]);
                    byte[] data_bytes = BitConverter.GetBytes(floats[i].Value);
                    int k = 0;
                    for (int j = 4; j <= 7; ++j)
                    {
                        SendBuffer[index_FL + j] = data_bytes[k];
                        ++k;
                    }
                    index_FL += 8;
                }
            }
            StreamData(17 + num_W * 6 + num_DW * 8 + num_FL * 8, 11);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            StreamData(0, 20 - 11); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// Write multiple values to the server randomly as an asynchronous operation.
        /// </summary>
        /// <param name="bits"><see cref="dotPLC.Mitsubishi.Types.Bit"/> values to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        public async Task WriteDeviceRandomAsync(params Bit[] bits) => await _queue.Enqueue(() => WriteDeviceRandomSubAsync(bits)).ConfigureAwait(false);
        /// <summary>
        /// Write multiple values to the server randomly as an asynchronous operation.
        /// </summary>
        /// <param name="words"><see cref="dotPLC.Mitsubishi.Types.Word"/> values to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        public async Task WriteDeviceRandomAsync(params Word[] words) => await _queue.Enqueue(() => WriteDeviceRandomSubAsync(words)).ConfigureAwait(false);
        /// <summary>
        /// Write multiple values to the server randomly as an asynchronous operation.
        /// </summary>
        /// <param name="dwords"><see cref="dotPLC.Mitsubishi.Types.DWord"/> values to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        public async Task WriteDeviceRandomAsync(params DWord[] dwords) => await _queue.Enqueue(() => WriteDeviceRandomSubAsync(dwords)).ConfigureAwait(false);
        /// <summary>
        /// Write multiple values to the server randomly as an asynchronous operation.
        /// </summary>
        /// <param name="floats"><see cref="dotPLC.Mitsubishi.Types.Float"/> values to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        public async Task WriteDeviceRandomAsync(params Float[] floats) => await _queue.Enqueue(() => WriteDeviceRandomSubAsync(floats)).ConfigureAwait(false);
        /// <summary>
        /// Write multiple values to the server randomly as an asynchronous operation.
        /// </summary>
        /// <param name="words"><see cref="dotPLC.Mitsubishi.Types.Word"/> values to be written.</param>
        /// <param name="dwords"><see cref="dotPLC.Mitsubishi.Types.DWord"/> values to be written.</param>
        /// <param name="floats"><see cref="dotPLC.Mitsubishi.Types.Float"/> values to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        public async Task WriteDeviceRandomAsync(Word[] words = null, DWord[] dwords = null, Float[] floats = null) => await _queue.Enqueue(() => WriteDeviceRandomSubAsync(words, dwords, floats)).ConfigureAwait(false);

        /// <summary>
        /// Read a single value from the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <returns>Returned value.</returns>
        internal bool ReadSingleCoil(string label)
        {
            bool flag = false;
            SendBuffer[7] = 0x0C;
            SendBuffer[8] = 0x00;
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x04;
            SendBuffer[13] = 0x01;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            SendBuffer[19] = 0x01;
            SendBuffer[20] = 0x00;
            StreamData(21, 12);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                StreamData(0, 20 - 12); //Khi PLC gửi lỗi (20byte)
            }
            else
                flag = ReceveiBuffer[11] == 0x10;
            return flag;
        }
        /// <summary>
        /// Read a single value from the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <returns>Returned value.</returns>
        internal short ReadSingleRegister(string label)
        {
            short num = 0;
            SendBuffer[7] = 0x0C;
            SendBuffer[8] = 0x00;
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x04;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            SendBuffer[19] = 0x01;
            SendBuffer[20] = 0x00;
            StreamData(21, 13);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                StreamData(0, 20 - 13); //Khi PLC gửi lỗi (20byte)
            }
            else
                num = BitConverter.ToInt16(ReceveiBuffer, 11);
            return num;
        }
        /// <summary>
        /// Read a single value from the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <returns>Returned value.</returns>
        internal int ReadSingleDouble(string label)
        {
            int num = 0;
            SendBuffer[7] = 0x0C;
            SendBuffer[8] = 0x00;
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x04;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            SendBuffer[19] = 0x02;
            SendBuffer[20] = 0x00;
            StreamData(21, 15);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                StreamData(0, 20 - 15); //Khi PLC gửi lỗi (20byte)
            }
            else
                num = BitConverter.ToInt32(ReceveiBuffer, 11);
            return num;
        }
        /// <summary>
        /// Read a single value from the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <returns>Returned value.</returns>
        internal float ReadSingleFloat(string label)
        {
            float num = 0.0f;
            SendBuffer[7] = 0x0C;
            SendBuffer[8] = 0x00;
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x04;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            SendBuffer[19] = 0x02;
            SendBuffer[20] = 0x00;
            StreamData(21, 15);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                StreamData(0, 20 - 15); //Khi PLC gửi lỗi (20byte)
            }
            else
                num = BitConverter.ToSingle(ReceveiBuffer, 11);
            return num;
        }
        /// <summary>
        /// Read a single value from the server as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// <see href="TResult"/> is <see cref="bool"/></returns>
        internal async Task<bool> ReadSingleCoilAsync(string label) => await _queue.Enqueue(() => ReadSingleCoilSubAsync(label)).ConfigureAwait(false);
        /// <summary>
        /// Read a single value from the server as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// <see href="TResult"/> is <see cref="short"/></returns>
        internal async Task<short> ReadSingleRegisterAsync(string label) => await _queue.Enqueue(() => ReadRegisterSubAsync(label)).ConfigureAwait(false);
        /// <summary>
        /// Read a single value from the server as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// <see href="TResult"/> is <see cref="int"/></returns>
        internal async Task<int> ReadSingleDoubleAsync(string label) => await _queue.Enqueue(() => ReadDoubleSubAsync(label)).ConfigureAwait(false);
        /// <summary>
        /// Read a single value from the server as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// <see href="TResult"/> is <see cref="float"/></returns>
        internal async Task<float> ReadSingleFloatAsync(string label) => await _queue.Enqueue(() => ReadFloatSubAsync(label)).ConfigureAwait(false);
        /// <summary>
        /// Read a single value from the server as an asynchronous operation.
        /// </summary>
        /// <typeparam name="T">The data type of value. (EX: <see cref="bool"></see>, <see cref="short"/>, <see cref="float"/>, etc.)</typeparam>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// Returned <typeparamref name="T"/> value.</returns>
        public async Task<T> ReadDeviceAsync<T>(string label) where T : struct
        {
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Boolean:
                    return (T)Convert.ChangeType(await ReadSingleCoilAsync(label), typeof(T));
                case TypeCode.Int16:
                    return (T)Convert.ChangeType(await ReadRegisterSubAsync(label), typeof(T));
                case TypeCode.UInt16:
                    return (T)Convert.ChangeType((ushort)await ReadRegisterSubAsync(label), typeof(T));
                case TypeCode.Int32:
                    return (T)Convert.ChangeType(await ReadDoubleSubAsync(label), typeof(T));
                case TypeCode.UInt32:
                    return (T)Convert.ChangeType((uint)await ReadDoubleSubAsync(label), typeof(T));
                case TypeCode.Single:
                    return (T)Convert.ChangeType(await ReadFloatSubAsync(label), typeof(T));
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
        internal bool[] ReadMultipleCoils(string label, int size)
        {
            SendBuffer[8] = 0x00;
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x04;
            SendBuffer[13] = 0x01;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            byte[] bytes = BitConverter.GetBytes(size);
            SendBuffer[19] = bytes[0];
            SendBuffer[20] = bytes[1];
            int num_of_byte = size % 2 == 0 ? size / 2 : size / 2 + 1;
            StreamData(21, 11 + num_of_byte);
            bool[] flagArray = new bool[size];
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                if (11 + num_of_byte < 20)
                    StreamData(0, 20 - (11 + num_of_byte)); //Khi PLC gửi lỗi (20byte)
            }
            else
                flagArray = ConvertByteArrayToBoolArray(ReceveiBuffer, 11, size);
            return flagArray;
        }
        /// <summary>
        /// Read multiple values from the server in a batch.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returned values.</returns>
        internal short[] ReadMultipleRegisters(string label, int size)
        {
            short[] numArray = new short[size];
            SendBuffer[7] = 0x0C;
            SendBuffer[8] = 0x00;
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x04;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            byte[] bytes = BitConverter.GetBytes(size);
            SendBuffer[19] = bytes[0];
            SendBuffer[20] = bytes[1];
            StreamData(21, 11 + size * 2);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                if (11 + size * 2 < 20)
                    StreamData(0, 20 - (11 + size * 2)); //Khi PLC gửi lỗi (20byte)
            }
            else
            {
                for (int index = 0; index < size * 2; index += 2)
                    numArray[index / 2] = BitConverter.ToInt16(ReceveiBuffer, 11 + index);
            }
            return numArray;
        }
        /// <summary>
        /// Read multiple values from the server in a batch.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returned values.</returns>
        internal int[] ReadMultipleDoubles(string label, int size)
        {
            int[] numArray = new int[size];
            SendBuffer[7] = 0x0C;
            SendBuffer[8] = 0x00;
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x04;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            byte[] bytes = BitConverter.GetBytes(size * 2);
            SendBuffer[19] = bytes[0];
            SendBuffer[20] = bytes[1];
            StreamData(21, 11 + size * 4);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                if (11 + size * 4 < 20)
                    StreamData(0, 20 - (11 + size * 4)); //Khi PLC gửi lỗi (20byte)
            }
            else
            {
                for (int index = 0; index < size * 4; index += 4)
                    numArray[index / 4] = BitConverter.ToInt32(ReceveiBuffer, 11 + index);
            }
            return numArray;
        }
        /// <summary>
        /// Read multiple values from the server in a batch.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returned values.</returns>
        internal float[] ReadMultipleFloats(string label, int size)
        {
            float[] numArray = new float[size];
            SendBuffer[7] = 0x0C;
            SendBuffer[8] = 0x00;
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x04;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            byte[] bytes = BitConverter.GetBytes(size * 2);
            SendBuffer[19] = bytes[0];
            SendBuffer[20] = bytes[1];
            StreamData(21, 11 + size * 4);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                if (11 + size * 4 < 20)
                    StreamData(0, 20 - (11 + size * 4)); //Khi PLC gửi lỗi (20byte)
            }
            else
            {
                for (int index = 0; index < size * 4; index += 4)
                    numArray[index / 4] = BitConverter.ToSingle(ReceveiBuffer, 11 + index);
            }
            return numArray;
        }
        /// <summary>
        /// Read multiple values from the server in a batch as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// <see href="TResult"></see> is <see cref="bool"></see>[].</returns>
        internal async Task<bool[]> ReadMultipleCoilsAsync(string label, int size) => await _queue.Enqueue(() => ReadMultipleCoilsSubAsync(label, size)).ConfigureAwait(false);
        /// <summary>
        /// Read multiple values from the server in a batch as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// <see href="TResult"></see> is <see cref="short"></see>[].</returns>
        internal async Task<short[]> ReadMultipleRegistersAsync(string label, int size) => await _queue.Enqueue(() => ReadRegistersAsync(label, size)).ConfigureAwait(false);
        /// <summary>
        /// Read multiple values from the server in a batch as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// <see href="TResult"></see> is <see cref="int"></see>[].</returns>
        internal async Task<int[]> ReadMultipleDoublesAsync(string label, int size) => await _queue.Enqueue(() => ReadMultipleDoubleSubAsync(label, size)).ConfigureAwait(false);
        /// <summary>
        /// Read multiple values from the server in a batch as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// <see href="TResult"></see> is <see cref="float"></see>[].</returns>
        internal async Task<float[]> ReadMultipleFloatsAsync(string label, int size) => await _queue.Enqueue(() => ReadFloatsAsync(label, size)).ConfigureAwait(false);
        /// <summary>
        /// Read multiple values from the server in a batch as an asynchronous operation.
        /// </summary>
        /// <typeparam name="T">The data type of value. (EX: <see cref="bool"></see>, <see cref="short"/>, <see cref="float"/>, etc.)</typeparam>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// Returned <typeparamref name="T"/>[] values.</returns>
        public async Task<T[]> ReadDeviceBlockAsync<T>(string label, int size) where T : struct
        {
            T[] results = new T[size];
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Boolean:
                    {
                        if (size < 1 || size > 3584) //Page57 (SLMP-Mitsubishi.PDF)
                            throw new SizeOutOfRangeException("Size must be 1 to 3584 points.", nameof(size));
                        var values = await ReadMultipleCoilsAsync(label, size);
                        for (int i = 0; i < size; ++i)
                            results[i] = (T)Convert.ChangeType(values[i], typeof(T));
                        return results;
                    }
                case TypeCode.Int16:
                    {
                        if (size < 1 || size > 960) //Page57 (SLMP-Mitsubishi.PDF)
                            throw new SizeOutOfRangeException("Size must be 1 to 960 points.", nameof(size));
                        var values = await ReadMultipleRegistersAsync(label, size);
                        for (int i = 0; i < size; ++i)
                            results[i] = (T)Convert.ChangeType(values[i], typeof(T));
                        return results;
                    }
                case TypeCode.UInt16:
                    {
                        if (size < 1 || size > 960) //Page57 (SLMP-Mitsubishi.PDF)
                            throw new SizeOutOfRangeException("Size must be 1 to 960 points.", nameof(size));
                        var values = await ReadMultipleRegistersAsync(label, size);
                        for (int i = 0; i < size; ++i)
                            results[i] = (T)Convert.ChangeType((ushort)values[i], typeof(T));
                        return results;
                    }
                case TypeCode.Int32:
                    {
                        if (size < 1 || size > 480) //Page57 (SLMP-Mitsubishi.PDF) 960Word/2=480double
                            throw new SizeOutOfRangeException("Size must be 1 to 480 points.", nameof(size));
                        var values = await ReadMultipleDoublesAsync(label, size);
                        for (int i = 0; i < size; ++i)
                            results[i] = (T)Convert.ChangeType(values[i], typeof(T));
                        return results;
                    }
                case TypeCode.UInt32:
                    {
                        if (size < 1 || size > 480) //Page57 (SLMP-Mitsubishi.PDF) 960Word/2=480double
                            throw new SizeOutOfRangeException("Size must be 1 to 480 points.", nameof(size));
                        int[] values5 = await ReadMultipleDoublesAsync(label, size);
                        for (int i = 0; i < size; ++i)
                            results[i] = (T)Convert.ChangeType((uint)values5[i], typeof(T));
                        return results;
                    }
                case TypeCode.Single:
                    {
                        if (size < 1 || size > 480) //Page57 (SLMP-Mitsubishi.PDF) 960Word/2=480float
                            throw new SizeOutOfRangeException("Size must be 1 to 480 points.", nameof(size));
                        var values = await ReadMultipleFloatsAsync(label, size);
                        for (int i = 0; i < size; ++i)
                            results[i] = (T)Convert.ChangeType(values[i], typeof(T));
                        return results;
                    }
                default:
                    throw new InvalidCastException("Invalid input data type.");
            }
        }
        /// <summary>
        /// Read multiple values from the server randomly.
        /// </summary>
        /// <param name="bits"><see cref="dotPLC.Mitsubishi.Types.Bit"/> values to be read.</param>
        public  void ReadDeviceRandom(params Bit[] bits)
        {
            if (bits == null)
                throw new ArgumentNullException(nameof(bits));
            Labels.Clear();
            dsWordBaseBits.Clear();
            for (int index = 0; index < bits.Length; ++index)
            {
                if (bits[index] == null)
                    throw new ArgumentNullException(nameof(bits), string.Format("bits[{0}] is null.", index));
                string device;
                int num;
                SettingDevice(bits[index].Label, out device, out num);
                if (Labels.ContainsKey(device))
                    Labels[device].Add(num);
                else
                    Labels.Add(device, new List<int>() { num });
            }
            foreach (KeyValuePair<string, List<int>> item in Labels)
                item.Value.Sort();
            foreach (KeyValuePair<string, List<int>> item in Labels)
            {
                int num = 0;
                for (int index = 0; index < item.Value.Count; ++index)
                {
                    if (index == 0)
                    {
                        num = item.Value[0];
                        var word = new Word(); word.Create(item.Key, item.Value[0]);
                        dsWordBaseBits.Add(word);
                    }
                    else if (item.Value[index] > num + 15)
                    {
                        num = item.Value[index];
                        var word = new Word(); word.Create(item.Key, item.Value[index]);
                        dsWordBaseBits.Add(word);
                    }
                }
            }
            int count = dsWordBaseBits.Count;
            if (count < 1 || count > 192) //Page57 (SLMP-Mitsubishi.PDF)
                throw new SizeOutOfRangeException("Size must be 1 to 192.\n[size = bits (Convert to word points)]", nameof(bits));
            byte[] bytes = BitConverter.GetBytes(8 + count * 4);
            SendBuffer[7] = bytes[0];
            SendBuffer[8] = bytes[1];
            SendBuffer[11] = 0x03;
            SendBuffer[12] = 0x04;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SendBuffer[15] = (byte)count;
            SendBuffer[16] = 0x00;
            int index_B = 17;
            for (int i = 0; i < count; ++i)
            {
                SettingDevice(dsWordBaseBits[i].Label, out SendBuffer[index_B + 3], out SendBuffer[index_B], out SendBuffer[index_B + 1], out SendBuffer[index_B + 2]);
                index_B += 4;
            }
            StreamData(17 + count * 4, 11 + count * 2);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                if (11 + count * 2 < 20)
                    StreamData(0, 20 - (11 + count * 2)); //Khi PLC gửi lỗi (20byte)
            }
            else
            {
                for (int i = 0; i < count * 2; i += 2)
                    dsWordBaseBits[i / 2].Value = BitConverter.ToInt16(ReceveiBuffer, 11 + i);
                for (int i = 0; i < bits.Length; ++i)
                {
                    foreach (Word word in dsWordBaseBits)
                    {
                        if (bits[i].Device == word.Device)
                        {
                            int gap = bits[i].Index - word.Index;
                            if (gap <= 15 && gap >= 0)
                            {
                                bool[] boolArray = ConvertWordToBoolArray(word.Value);
                                bits[i].Value = boolArray[gap];
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Read multiple values from the server randomly.
        /// </summary>
        /// <param name="words"><see cref="dotPLC.Mitsubishi.Types.Word"/> values to be read.</param>
        public  void ReadDeviceRandom(params Word[] words)
        {
            if (words == null)
                throw new ArgumentNullException(nameof(words));
            int length = words.Length;
            if (length < 1 || length > 192) //Page57 (SLMP-Mitsubishi.PDF)
                throw new SizeOutOfRangeException("Size must be 1 to 192 points.", nameof(words));

            byte[] bytes = BitConverter.GetBytes(8 + length * 4);
            SendBuffer[7] = bytes[0];
            SendBuffer[8] = bytes[1];
            SendBuffer[11] = 0x03;
            SendBuffer[12] = 0x04;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SendBuffer[15] = (byte)length;
            SendBuffer[16] = 0x00;
            int index_W = 17;
            for (int i = 0; i < length; ++i)
            {
                if (words[i] == null)
                    throw new ArgumentNullException(nameof(words), string.Format("words[{0}] is null.", i));
                SettingDevice(words[i].Label, out SendBuffer[index_W + 3], out SendBuffer[index_W], out SendBuffer[index_W + 1], out SendBuffer[index_W + 2]);
                index_W += 4;
            }
            StreamData(17 + length * 4, 11 + length * 2);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                if (11 + length * 2 < 20)
                    StreamData(0, 20 - (11 + length * 2)); //Khi PLC gửi lỗi (20byte)
            }
            else
            {
                for (int i = 0; i < length * 2; i += 2)
                    words[i / 2].Value = BitConverter.ToInt16(ReceveiBuffer, 11 + i);
            }
        }
        /// <summary>
        /// Read multiple values from the server randomly.
        /// </summary>
        /// <param name="dwords"><see cref="dotPLC.Mitsubishi.Types.DWord"/> values to be read.</param>
        public  void ReadDeviceRandom(params DWord[] dwords)
        {
            if (dwords == null)
                throw new ArgumentNullException(nameof(dwords));
            int length = dwords.Length;
            if (length < 1 || length > 192) //Page57 (SLMP-Mitsubishi.PDF)
                throw new SizeOutOfRangeException("Size must be 1 to 192 points.", nameof(dwords));
            byte[] bytes = BitConverter.GetBytes(8 + length * 4);
            SendBuffer[7] = bytes[0];
            SendBuffer[8] = bytes[1];
            SendBuffer[11] = 0x03;
            SendBuffer[12] = 0x04;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SendBuffer[15] = 0x00;
            SendBuffer[16] = (byte)length;
            int index_DW = 17;
            for (int i = 0; i < length; ++i)
            {
                if (dwords[i] == null)
                    throw new ArgumentNullException(nameof(dwords), string.Format("dwords[{0}] is null.", i));
                SettingDevice(dwords[i].Label, out SendBuffer[index_DW + 3], out SendBuffer[index_DW], out SendBuffer[index_DW + 1], out SendBuffer[index_DW + 2]);
                index_DW += 4;
            }
            StreamData(17 + length * 4, 11 + length * 4);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                if (11 + length * 4 < 20)
                    StreamData(0, 20 - (11 + length * 4)); //Khi PLC gửi lỗi (20byte)
            }
            else
            {
                for (int i = 0; i < length * 4; i += 4)
                    dwords[i / 4].Value = BitConverter.ToInt32(ReceveiBuffer, 11 + i);
            }
        }
        /// <summary>
        /// Read multiple values from the server randomly.
        /// </summary>
        /// <param name="floats"><see cref="dotPLC.Mitsubishi.Types.Float"/> values to be read.</param>
        public  void ReadDeviceRandom(params Float[] floats)
        {
            if (floats == null)
                throw new ArgumentNullException(nameof(floats));
            int length = floats.Length;
            if (length < 1 || length > 192) //Page57 (SLMP-Mitsubishi.PDF)
                throw new SizeOutOfRangeException("Size must be 1 to 192 points.", nameof(floats));
            byte[] bytes = BitConverter.GetBytes(8 + length * 4);
            SendBuffer[7] = bytes[0];
            SendBuffer[8] = bytes[1];
            SendBuffer[11] = 0x03;
            SendBuffer[12] = 0x04;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SendBuffer[15] = 0x00;
            SendBuffer[16] = (byte)length;
            int index_FL = 17;
            for (int i = 0; i < length; ++i)
            {
                if (floats[i] == null)
                    throw new ArgumentNullException(nameof(floats), string.Format("floats[{0}] is null.", i));
                SettingDevice(floats[i].Label, out SendBuffer[index_FL + 3], out SendBuffer[index_FL], out SendBuffer[index_FL + 1], out SendBuffer[index_FL + 2]);
                index_FL += 4;
            }
            StreamData(17 + length * 4, 11 + length * 4);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                if (11 + length * 4 < 20)
                    StreamData(0, 20 - (11 + length * 4)); //Khi PLC gửi lỗi (20byte)
            }
            else
            {
                for (int i = 0; i < length * 4; i += 4)
                    floats[i / 4].Value = BitConverter.ToSingle(ReceveiBuffer, 11 + i);
            }
        }
        /// <summary>
        /// Read multiple values from the server randomly. <see langword="[RECOMMENDED]"></see>
        /// </summary>
        /// <param name="bits"><see cref="dotPLC.Mitsubishi.Types.Bit"/> values to be read.</param>
        /// <param name="words"><see cref="dotPLC.Mitsubishi.Types.Word"/> values to be read.</param>
        /// <param name="dwords"><see cref="dotPLC.Mitsubishi.Types.DWord"/> values to be read.</param>
        /// <param name="floats"><see cref="dotPLC.Mitsubishi.Types.Float"/> values to be read.</param>
        public  void ReadDeviceRandom(Bit[] bits = null, Word[] words = null, DWord[] dwords = null, Float[] floats = null)
        {
            Labels.Clear();
            dsWordBaseBits.Clear();
            if (bits != null)
            {
                for (int index = 0; index < bits.Length; ++index)
                {
                    if (bits[index] == null)
                        throw new ArgumentNullException(nameof(bits),string.Format("bits[{0}] is null.",index));
                    string device;
                    int num;
                    SettingDevice(bits[index].Label, out device, out num);
                    if (Labels.ContainsKey(device))
                        Labels[device].Add(num);
                    else
                        Labels.Add(device, new List<int>() { num });
                }
                foreach (KeyValuePair<string, List<int>> item in Labels)
                    item.Value.Sort();
                foreach (KeyValuePair<string, List<int>> item in Labels)
                {
                    int num = 0;
                    for (int index = 0; index < item.Value.Count; ++index)
                    {
                        if (index == 0)
                        {
                            num = item.Value[0];
                            var word = new Word(); word.Create(item.Key, item.Value[0]);
                            dsWordBaseBits.Add(word);
                        }
                        else if (item.Value[index] > num + 15)
                        {
                            num = item.Value[index];
                            var word = new Word(); word.Create(item.Key, item.Value[index]);
                            dsWordBaseBits.Add(word);
                        }
                    }
                }
            }
            int num_of_Bit_base_W = dsWordBaseBits == null ? 0 : dsWordBaseBits.Count; //số bit dựa theo Word
            int num_of_W = words == null ? 0 : words.Length; //Số W
            int num_of_DW = dwords == null ? 0 : dwords.Length; //Số Dw
            int num_of_FL = floats == null ? 0 : floats.Length; // Số FL
            if (num_of_Bit_base_W == 0 && num_of_W == 0 && num_of_DW == 0 && num_of_FL == 0)
                return;
            int size = num_of_Bit_base_W + num_of_W + num_of_DW + num_of_FL;
            if (size < 1 || size > 192) //Page57 (SLMP-Mitsubishi.PDF)
                throw new SizeOutOfRangeException("Size must be 1 to 192." +
                    "\n[size = bits points (Convert to word points) + word points + double points + float points.]"
                    + "\n[size = " + num_of_Bit_base_W + " + " + num_of_W + " + " + num_of_DW + " + " + num_of_FL + " = " + size);

            byte[] bytes = BitConverter.GetBytes(8 + (num_of_Bit_base_W + num_of_W + num_of_DW + num_of_FL) * 4);
            SendBuffer[7] = bytes[0];
            SendBuffer[8] = bytes[1];
            SendBuffer[11] = 0x03;
            SendBuffer[12] = 0x04;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SendBuffer[15] = (byte)(num_of_W + num_of_Bit_base_W);
            SendBuffer[16] = (byte)(num_of_DW + num_of_FL);
            int index_B = 17;
            int index_W = 17 + num_of_Bit_base_W * 4;
            int index_DW = index_W + num_of_W * 4;
            int index_FL = index_DW + num_of_DW * 4;
            if (num_of_Bit_base_W > 0)
            {
                for (int i = 0; i < num_of_Bit_base_W; ++i)
                {
                    SettingDevice(dsWordBaseBits[i].Label, out SendBuffer[index_B + 3], out SendBuffer[index_B], out SendBuffer[index_B + 1], out SendBuffer[index_B + 2]);
                    index_B += 4;
                }
            }
            if (num_of_W > 0)
            {
                for (int i = 0; i < num_of_W; ++i)
                {
                    if (words[i] == null)
                        throw new ArgumentNullException(nameof(words), string.Format("words[{0}] is null.", i));
                    SettingDevice(words[i].Label, out SendBuffer[index_W + 3], out SendBuffer[index_W], out SendBuffer[index_W + 1], out SendBuffer[index_W + 2]);
                    index_W += 4;
                }
            }
            if (num_of_DW > 0)
            {
                for (int i = 0; i < num_of_DW; ++i)
                {
                    if (dwords[i] == null)
                        throw new ArgumentNullException(nameof(dwords), string.Format("dwords[{0}] is null.", i));
                    SettingDevice(dwords[i].Label, out SendBuffer[index_DW + 3], out SendBuffer[index_DW], out SendBuffer[index_DW + 1], out SendBuffer[index_DW + 2]);
                    index_DW += 4;
                }
            }
            if (num_of_FL > 0)
            {
                for (int i = 0; i < num_of_FL; ++i)
                {
                    if (floats[i] == null)
                        throw new ArgumentNullException(nameof(floats), string.Format("floats[{0}] is null.", i));
                    SettingDevice(floats[i].Label, out SendBuffer[index_FL + 3], out SendBuffer[index_FL], out SendBuffer[index_FL + 1], out SendBuffer[index_FL + 2]);
                    index_FL += 4;
                }
            }
            StreamData(17 + (num_of_Bit_base_W + num_of_W + num_of_DW + num_of_FL) * 4, 11 + num_of_Bit_base_W * 2 + num_of_W * 2 + num_of_DW * 4 + num_of_FL * 4);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                if (11 + num_of_Bit_base_W * 2 + num_of_W * 2 + num_of_DW * 4 + num_of_FL * 4 < 20)
                    StreamData(0, 20 - (11 + num_of_Bit_base_W * 2 + num_of_W * 2 + num_of_DW * 4 + num_of_FL * 4)); //Khi PLC gửi lỗi (20byte)
            }
            else
            {
                if (num_of_Bit_base_W > 0)
                {
                    for (int i = 0; i < num_of_Bit_base_W * 2; i += 2)
                        dsWordBaseBits[i / 2].Value = BitConverter.ToInt16(ReceveiBuffer, 11 + i);
                    for (int i = 0; i < bits.Length; ++i)
                    {
                        foreach (Word dsWordBaseBit in dsWordBaseBits)
                        {
                            if (bits[i].Device == dsWordBaseBit.Device)
                            {
                                int j = bits[i].Index - dsWordBaseBit.Index;
                                if (j <= 15 && j >= 0)
                                {
                                    bool[] boolArray = ConvertWordToBoolArray(dsWordBaseBit.Value);
                                    bits[i].Value = boolArray[j];
                                }
                            }
                        }
                    }
                }
                if (num_of_W > 0)
                {
                    for (int i = 0; i < num_of_W * 2; i += 2)
                        words[i / 2].Value = BitConverter.ToInt16(ReceveiBuffer, 11 + num_of_Bit_base_W * 2 + i);
                }
                if (num_of_DW > 0)
                {
                    for (int i = 0; i < num_of_DW * 4; i += 4)
                        dwords[i / 4].Value = BitConverter.ToInt32(ReceveiBuffer, 11 + (num_of_Bit_base_W * 2 + num_of_W * 2) + i);
                }
                if (num_of_FL > 0)
                {
                    for (int i = 0; i < num_of_FL * 4; i += 4)
                        floats[i / 4].Value = BitConverter.ToSingle(ReceveiBuffer, 11 + (num_of_Bit_base_W * 2 + num_of_W * 2 + num_of_DW * 4) + i);
                }
            }
        }
        /// <summary>
        /// Read multiple values from the server randomly as an asynchronous operation.
        /// </summary>
        /// <param name="bits"><see cref="dotPLC.Mitsubishi.Types.Bit"/> values to be read.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        public async Task ReadDeviceRandomAsync(params Bit[] bits) => await _queue.Enqueue(() => ReadRandomSubAsync(bits)).ConfigureAwait(false);
        /// <summary>
        /// Read multiple values from the server randomly as an asynchronous operation.
        /// </summary>
        /// <param name="words"><see cref="dotPLC.Mitsubishi.Types.Word"/> values to be read.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        public async Task ReadDeviceRandomAsync(params Word[] words) => await _queue.Enqueue(() => ReadRandomSubAsync(words)).ConfigureAwait(false);
        /// <summary>
        /// Read multiple values from the server randomly as an asynchronous operation.
        /// </summary>
        /// <param name="dwords"><see cref="dotPLC.Mitsubishi.Types.DWord"/> values to be read.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        public async Task ReadDeviceRandomAsync(params DWord[] dwords) => await _queue.Enqueue(() => ReadRandomSubAsync(dwords)).ConfigureAwait(false);
        /// <summary>
        /// Read multiple values from the server randomly as an asynchronous operation.
        /// </summary>
        /// <param name="floats"><see cref="dotPLC.Mitsubishi.Types.Float"/> values to be read.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        public async Task ReadDeviceRandomAsync(params Float[] floats) => await _queue.Enqueue(() => ReadRandomSubAsync(floats)).ConfigureAwait(false);
        /// <summary>
        /// Read multiple values from the server randomly as an asynchronous operation. <see langword="[RECOMMENDED]"></see>
        /// </summary>
        /// <param name="bits"><see cref="dotPLC.Mitsubishi.Types.Bit"/> values to be read.</param>
        /// <param name="words"><see cref="dotPLC.Mitsubishi.Types.Word"/> values to be read.</param>
        /// <param name="dwords"><see cref="dotPLC.Mitsubishi.Types.DWord"/> values to be read.</param>
        /// <param name="floats"><see cref="dotPLC.Mitsubishi.Types.Float"/> values to be read.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        public async Task ReadDeviceRandomAsync(Bit[] bits, Word[] words, DWord[] dwords, Float[] floats) => await _queue.Enqueue(() => ReadRandomSubAsync(bits, words, dwords, floats)).ConfigureAwait(false);
        /// <summary>
        /// To test whether the communication function between the client and the server operates normally or not.
        /// </summary>
        /// <param name="loopbackMessage">The order of character strings for up to 960 1-byte characters ("0" to "9", "A" to "F") is sent from the head.</param>
        /// <returns>Returns <see cref="bool"></see> indicates that true is normal, false is abnormal.</returns>
        public bool SelfTest(string loopbackMessage)
        {
            if (loopbackMessage == "" || loopbackMessage == null)
                throw new ArgumentNullException(nameof(loopbackMessage), "Data length must be greater than 1 and less than or equal to 960 characters, character codes are allowed (\"0\" to \"9\", \"A\" to \"F\").");
            foreach (char ch in loopbackMessage)
            {
                if ((ch < '0' || ch > '9') && (ch < 'A' || ch > 'F') || loopbackMessage.Length > 960)
                    throw new ArgumentOutOfRangeException(nameof(loopbackMessage), loopbackMessage, "Data length must be greater than 1 and less than or equal to 960 characters, character codes are allowed (\"0\" to \"9\", \"A\" to \"F\").");
            }
            byte[] datalength_bytes = BitConverter.GetBytes(8 + loopbackMessage.Length);
            SendBuffer[7] = datalength_bytes[0];
            SendBuffer[8] = datalength_bytes[1];
            SendBuffer[11] = 0x19;
            SendBuffer[12] = 0x06;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            byte[] textlength_bytes = BitConverter.GetBytes(loopbackMessage.Length);
            SendBuffer[15] = textlength_bytes[0];
            SendBuffer[16] = textlength_bytes[1];
            byte[] text_bytes = Encoding.ASCII.GetBytes(loopbackMessage);
            Array.Copy(text_bytes, 0, SendBuffer, 17, text_bytes.Length);
            StreamData(17 + loopbackMessage.Length, 13 + loopbackMessage.Length);
            bool flag;
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                flag = false;
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                if (13 + loopbackMessage.Length < 20)
                    StreamData(0, 20 - (13 + loopbackMessage.Length)); //Khi PLC gửi lỗi (20byte)
            }
            else
                flag = ReceveiBuffer[11] == textlength_bytes[0] && ReceveiBuffer[12] == textlength_bytes[1] && loopbackMessage == Encoding.ASCII.GetString(ReceveiBuffer, 13, loopbackMessage.Length);
            return flag;
        }
        /// <summary>
        /// Read the model character string of the server.
        /// </summary>
        /// <returns>The model character string of the server.</returns>
        public  string GetCpuName()
        {
            Array.Copy(_getCpuName, SendBuffer, _getCpuName.Length);
            StreamData(_getCpuName.Length, 512);
            string cpuName;
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = ReceveiBuffer[10] << 8 + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                cpuName = "An error occurred when getting the model.";
            }
            else
            {
                int num = (ReceveiBuffer[28] << 8) + ReceveiBuffer[27];
                cpuName = Encoding.ASCII.GetString(ReceveiBuffer, 11, 16);
            }
            return cpuName;
        }
        /// <summary>
        /// Changes the remote password from unlocked status to locked status. (Communication to the device is disabled.)
        /// </summary>
        /// <param name="password">Specifies a remote password.</param>
        public void RemoteLock(string password)
        {
            if (password == "" || password == null)
                throw new ArgumentNullException(nameof(password), "The password length is the specified characters (6 to 32 characters).");
            byte[] numArray = password.Length <= 32 && password.Length >= 6 ? BitConverter.GetBytes(8 + password.Length) : throw new ArgumentOutOfRangeException(nameof(password), "The password length is the specified characters (6 to 32 characters).");
            SendBuffer[7] = numArray[0];
            SendBuffer[8] = numArray[1];
            SendBuffer[11] = 0x31;
            SendBuffer[12] = 0x16;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            byte[] datalength_bytes = BitConverter.GetBytes(password.Length);
            SendBuffer[15] = datalength_bytes[0];
            SendBuffer[16] = datalength_bytes[1];
            byte[] password_bytes = Encoding.ASCII.GetBytes(password);
            Array.Copy(password_bytes, 0, SendBuffer, 17, password_bytes.Length);
            StreamData(17 + password.Length, 11);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            StreamData(0, 20 - 11); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// Changes the remote password from locked status to unlocked status. (Enables communication to the device.)
        /// </summary>
        /// <param name="password">Specifies a remote password.</param>
        public void RemoteUnlock(string password)
        {
            if (password == "" || password == null)
                throw new ArgumentNullException(nameof(password), "The password length is the specified characters (6 to 32 characters).");
            byte[] numArray = password.Length <= 32 && password.Length >= 6 ? BitConverter.GetBytes(8 + password.Length) : throw new ArgumentOutOfRangeException(nameof(password), "The password length is the specified characters (6 to 32 characters).");
            SendBuffer[7] = numArray[0];
            SendBuffer[8] = numArray[1];
            SendBuffer[11] = 0x30;
            SendBuffer[12] = 0x16;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            byte[] passwordlength_bytes = BitConverter.GetBytes(password.Length);
            SendBuffer[15] = passwordlength_bytes[0];
            SendBuffer[16] = passwordlength_bytes[1];
            byte[] password_bytes = Encoding.ASCII.GetBytes(password);
            Array.Copy(password_bytes, 0, SendBuffer, 17, password_bytes.Length);
            StreamData(17 + password.Length, 11);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            StreamData(0, 20 - 11); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// To perform a remote operation of the server. (EX: RUN/PAUSE/STOP/CLEAR/RESET...)
        /// </summary>
        /// <param name="mode">Specifies a <see cref="dotPLC.Mitsubishi.RemoteControl"></see> mode. (EX: RUN/PAUSE/STOP/CLEAR/RESET...)</param>
        public void RemoteControl(RemoteControl mode)
        {
            byte[] sourceArray = CmdRemoteControl(mode);
            Array.Copy(sourceArray, SendBuffer, sourceArray.Length);
            StreamData(sourceArray.Length, 11);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            StreamData(0, 20 - 11); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// To test whether the communication function between the client and the server operates normally or not.
        /// </summary>
        /// <param name="loopbackMessage">The order of character strings for up to 960 1-byte characters ("0" to "9", "A" to "F") is sent from the head.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// <see href="TResult"/> is <see cref="bool"/> indicates that true is normal, false is abnormal.</returns>
        public async Task<bool> SelfTestAsync(string loopbackMessage) => await _queue.Enqueue(() => SelfTestSubAsync(loopbackMessage)).ConfigureAwait(false);
        /// <summary>
        /// Read the model character string of the server as an asynchronous operation.
        /// </summary>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// Value is the model character string of the server.</returns>
        public async Task<string> GetCpuNameAsync() => await _queue.Enqueue(() => GetCpuNameSubAsync()).ConfigureAwait(false);
        /// <summary>
        /// Changes the remote password from unlocked status to locked status as an asynchronous operation. (Communication to the device is disabled.)
        /// </summary>
        /// <param name="password">Specifies a remote password.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        public async Task RemoteLockAsync(string password) => await _queue.Enqueue(() => RemoteLockSubAsync(password)).ConfigureAwait(false);
        /// <summary>
        /// Changes the remote password from locked status to unlocked status as an asynchronous operation. (Enables communication to the device.)
        /// </summary>
        /// <param name="password">Specifies a remote password.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        public async Task RemoteUnlockAsync(string password) => await _queue.Enqueue(() => RemoteUnlockSubAsync(password)).ConfigureAwait(false);
        /// <summary>
        /// To perform a remote operation of the server as an asynchronous operation. (EX: RUN/PAUSE/STOP/CLEAR/RESET...)
        /// </summary>
        /// <param name="mode">Specifies a <see cref="dotPLC.Mitsubishi.RemoteControl"></see> mode. (EX: RUN/PAUSE/STOP/CLEAR/RESET...)</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        public async Task RemoteControlAsync(RemoteControl mode) => await _queue.Enqueue(() => RemoteControlSubAsync(mode)).ConfigureAwait(false);
        /// <summary>
        /// Write text to the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="text">Text to be written.</param>
        public  void WriteText(string label, string text)
        {
            if (text == null || text == "")
                throw new ArgumentNullException(nameof(text));
            if (text.Length < 1 || text.Length  > 960) //Page57 (SLMP-Mitsubishi.PDF)
                throw new SizeOutOfRangeException("Size must be 1 to 960 points.", nameof(text));
            if (text.Length % 2 != 0)
            {
                int num1 = text.Length / 2;
                SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
                int num2 = num1 + (SendBuffer[15] | SendBuffer[16] << 8 | SendBuffer[17] << 16);
                SendBuffer[7] = 0x0C;
                SendBuffer[8] = 0x00;
                SendBuffer[11] = 0x01;
                SendBuffer[12] = 0x04;
                SendBuffer[13] = 0x00;
                SendBuffer[14] = 0x00;
                byte[] bytes = BitConverter.GetBytes(num2);
                SendBuffer[15] = bytes[0];
                SendBuffer[16] = bytes[1];
                SendBuffer[17] = bytes[2];
                SendBuffer[19] = 0x01;
                SendBuffer[20] = 0x00;
                StreamData(21, 13);
                if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
                {
                    int errorCode_Read = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                    Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode_Read));
                    StreamData(0, 20 - 11); //Khi PLC gửi lỗi (20byte)
                    return;
                }
                text += Convert.ToChar(ReceveiBuffer[12]).ToString();

            }
            int length = text.Length;
            byte[] datalength_bytes = BitConverter.GetBytes(12 + length * 2);
            SendBuffer[7] = datalength_bytes[0];
            SendBuffer[8] = datalength_bytes[1];
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x14;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            byte[] bytes2 = BitConverter.GetBytes(length / 2);
            SendBuffer[19] = bytes2[0];
            SendBuffer[20] = bytes2[1];
            byte[] bytes3 = Encoding.ASCII.GetBytes(text);
            Array.Copy(bytes3, 0, SendBuffer, 21, bytes3.Length);
            StreamData(21 + length, 11);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            StreamData(0, 20 - 11); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// Read text from the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of text to be read.</param>
        /// <returns>Returns text of the specified size.</returns>
        public  string ReadText(string label, int size)
        {
            if (size < 1 || size > 960) //Page57 (SLMP-Mitsubishi.PDF)
                throw new SizeOutOfRangeException("Size must be 1 to 960 points.", nameof(size));
            string str = string.Empty;
            int count = size;
            if (size % 2 != 0)
                ++size;
            SendBuffer[7] = 0x0C;
            SendBuffer[8] = 0x00;
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x04;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            byte[] bytes = BitConverter.GetBytes(size / 2);
            SendBuffer[19] = bytes[0];
            SendBuffer[20] = bytes[1];
            StreamData(21, 11 + size);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                if (11 + size < 20)
                    StreamData(0, 20 - (11 + size)); //Khi PLC gửi lỗi (20byte)
            }
            else
                str = count % 2 != 0 ? Encoding.ASCII.GetString(ReceveiBuffer, 11, count) : Encoding.ASCII.GetString(ReceveiBuffer, 11, size);
            return str;
        }
        /// <summary>
        /// Write text to the server as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="text">Text to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        public async Task WriteTextAsync(string label, string text) => await _queue.Enqueue(() => WriteTextSubAsync(label, text)).ConfigureAwait(false);
        /// <summary>
        /// Read text from the server as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of text to be read.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// Value is text of the specified size.</returns>
        public async Task<string> ReadTextAsync(string label, int size) => await _queue.Enqueue(() => ReadTextSubAsync(label, size)).ConfigureAwait(false);

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
                    throw new InvalidDataTypeException("Invalid input data type.");
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
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            int length = values.Length;
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Boolean:
                    {
                        if (length < 1 || length > 3584) //Page57 (SLMP-Mitsubishi.PDF)
                            throw new SizeOutOfRangeException("Size must be 1 to 3584 points.", nameof(values));
                        var values_temp = new bool[length];
                        for (int index = 0; index < length; ++index)
                            values_temp[index] = (bool)Convert.ChangeType(values[index], TypeCode.Boolean);
                        WriteDeviceBlock(label, values_temp);
                        break;
                    }
                case TypeCode.Int16:
                    {
                        if (length < 1 || length > 960) //Page57 (SLMP-Mitsubishi.PDF)
                            throw new SizeOutOfRangeException("Size must be 1 to 960 points.", nameof(values));
                        var values_temp = new short[length];
                        for (int index = 0; index < length; ++index)
                            values_temp[index] = (short)Convert.ChangeType(values[index], TypeCode.Int16);
                        WriteDeviceBlock(label, values_temp);
                        break;
                    }
                case TypeCode.UInt16:
                    {
                        if (length < 1 || length > 960) //Page57 (SLMP-Mitsubishi.PDF)
                            throw new SizeOutOfRangeException("Size must be 1 to 960 points.", nameof(values));
                        var values_temp = new short[length];
                        for (int index = 0; index < length; ++index)
                            values_temp[index] = (short)(ushort)Convert.ChangeType(values[index], TypeCode.UInt16);
                        WriteDeviceBlock(label, values_temp);
                        break;
                    }
                case TypeCode.Int32:
                    {
                        if (length < 1 || length > 480) //Page57 (SLMP-Mitsubishi.PDF)
                            throw new SizeOutOfRangeException("Size must be 1 to 480 points.", nameof(values));
                        var values_temp = new int[length];
                        for (int index = 0; index < length; ++index)
                            values_temp[index] = (int)Convert.ChangeType(values[index], TypeCode.Int32);
                        WriteDeviceBlock(label, values_temp);
                        break;
                    }
                case TypeCode.UInt32:
                    {
                        if (length < 1 || length > 480) //Page57 (SLMP-Mitsubishi.PDF)
                            throw new SizeOutOfRangeException("Size must be 1 to 480 points.", nameof(values));
                        var values_temp = new int[length];
                        for (int index = 0; index < length; ++index)
                            values_temp[index] = (int)(uint)Convert.ChangeType(values[index], TypeCode.UInt32);
                        WriteDeviceBlock(label, values_temp);
                        break;
                    }
                case TypeCode.Single:
                    {
                        if (length < 1 || length > 480) //Page57 (SLMP-Mitsubishi.PDF)
                            throw new SizeOutOfRangeException("Size must be 1 to 480 points.", nameof(values));
                        var values_temp = new float[length];
                        for (int index = 0; index < length; ++index)
                            values_temp[index] = (float)Convert.ChangeType(values[index], TypeCode.Single);
                        WriteDeviceBlock(label, values_temp);
                        break;
                    }
                default:
                    throw new InvalidDataTypeException("Invalid input data type.");
            }
        }

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
                    throw new InvalidDataTypeException("Invalid input data type.");
            }
        }

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
                        if (size < 1 || size > 3584) //Page57 (SLMP-Mitsubishi.PDF)
                            throw new SizeOutOfRangeException("Size must be 1 to 3584 points.", nameof(size));
                        bool[] values = ReadMultipleCoils(label, size);
                        for (int index = 0; index < size; ++index)
                            results[index] = (T)Convert.ChangeType(values[index], typeof(T));
                        return results;
                    }
                case TypeCode.Int16:
                    {
                        if (size < 1 || size > 960) //Page57 (SLMP-Mitsubishi.PDF)
                            throw new SizeOutOfRangeException("Size must be 1 to 960 points.", nameof(size));
                        short[] values = ReadMultipleRegisters(label, size);
                        for (int index = 0; index < size; ++index)
                            results[index] = (T)Convert.ChangeType(values[index], typeof(T));
                        return results;
                    }
                case TypeCode.UInt16:
                    {
                        if (size < 1 || size > 960) //Page57 (SLMP-Mitsubishi.PDF)
                            throw new SizeOutOfRangeException("Size must be 1 to 960 points.", nameof(size));
                        short[] values = ReadMultipleRegisters(label, size);
                        for (int index = 0; index < size; ++index)
                            results[index] = (T)Convert.ChangeType((ushort)values[index], typeof(T));
                        return results;
                    }
                case TypeCode.Int32:
                    {
                        if (size < 1 || size > 480) //Page57 (SLMP-Mitsubishi.PDF) 960Word/2=480double
                            throw new SizeOutOfRangeException("Size must be 1 to 480 points.", nameof(size));
                        int[] values = ReadMultipleDoubles(label, size);
                        for (int index = 0; index < size; ++index)
                            results[index] = (T)Convert.ChangeType(values[index], typeof(T));
                        return results;
                    }
                case TypeCode.UInt32:
                    {
                        if (size < 1 || size > 480) //Page57 (SLMP-Mitsubishi.PDF) 960Word/2=480double
                            throw new SizeOutOfRangeException("Size must be 1 to 480 points.", nameof(size));
                        int[] values = ReadMultipleDoubles(label, size);
                        for (int index = 0; index < size; ++index)
                            results[index] = (T)Convert.ChangeType((uint)values[index], typeof(T));
                        return results;
                    }
                case TypeCode.Single:
                    {
                        if (size < 1 || size > 480) //Page57 (SLMP-Mitsubishi.PDF) 960Word/2=480float
                            throw new SizeOutOfRangeException("Size must be 1 to 480 points.", nameof(size));
                        float[] values = ReadMultipleFloats(label, size);
                        for (int index = 0; index < size; ++index)
                            results[index] = (T)Convert.ChangeType(values[index], typeof(T));
                        return results;
                    }
                default:
                    throw new InvalidDataTypeException("Data type is not compatible with the PLC.");
            }
        }
    }
}
