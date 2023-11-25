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

namespace dotPLC.Mitsubishi
{
    /// <summary>
    /// Provides client connection for TCP network service via Seamless Message Protocol (SLMP).
    /// </summary>
    public sealed partial class SLMPClient : Ethernet
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
            IPAddress = ipaddress;
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
            IPAddress = ipaddress;
            Port = port;
            AutoReconnect = autoReconnect;
        }
        #endregion Constructor
        #region Field
        /// <summary>
        /// CancellationTokenSource auto-reconnect
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        /// <summary>
        /// isReconnecting
        /// </summary>
        public bool isReconnecting = false;
        /// <summary>
        /// _isSafe
        /// </summary>
        private int _isSafe = 1;
        /// <summary>
        /// _isSafeMethod
        /// </summary>
        private int _isSafeMethod = 1;
        /// <summary>
        /// _reconnectLimitTimer
        /// </summary>
        private System.Timers.Timer _reconnectLimitTimer;
        /// <summary>
        /// _breakTimer
        /// </summary>
        private System.Timers.Timer _breakTimer;
        /// <summary>
        /// _selfTestTimer
        /// </summary>
        private System.Timers.Timer _selfTestTimer;
        /// <summary>
        /// _queue
        /// </summary>
        private readonly TaskQueue _queue = new TaskQueue();
        /// <summary>
        /// _breakInterval
        /// </summary>
        private int _breakInterval = 50;
        /// <summary>
        /// _selfTestCheckInterval
        /// </summary>
        private int _selfTestCheckInterval = 500;
        /// <summary>
        /// _reconnectLimitInterval
        /// </summary>
        private int _reconnectLimitInterval = 600000;
        /// <summary>
        /// _autoReconnect
        /// </summary>
        private AutoReconnect _autoReconnect = AutoReconnect.None;
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
        #region Properties 
        /// <summary>
        /// Port number of the server.
        /// </summary>
        public int Port { get; set; } = 502;
        /// <summary>
        /// Auto-reconnect mode when the connection is lost from the server.
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
        ///The interval from when the connection is lost to the server is detected
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
        ///The interval since the last communication with the server
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
        /// <summary>
        /// Occurs when an established connection is lost.
        /// </summary>
        public event EventHandler<EventArgs> LostConnect;
        /// <summary>
        /// Occurs when reconnecting to the server successfully.
        /// </summary>
        public event EventHandler<EventArgs> Reconnected;
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
        private void InnerClose()
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
        int a = 0;
        int b = 0;
        /// <summary>
        /// check connection for auto-reconnect
        /// </summary>
        /// <param name="loopbackMessage">loopbackMessage</param>
        private void SelfTestCheckConnection(string loopbackMessage)
        {
            if (Interlocked.Exchange(ref _isSafeMethod, 0) != 1)
                return;
            a++;
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
        /// Read and Write data
        /// </summary>
        /// <param name="writeLenght"></param>
        /// <param name="readLenght"></param>
        private void StreamData(int writeLenght, int readLenght)
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
            catch (NullReferenceException ex)
            {
                _breakTimer.Stop();
                throw new Exception(ex.Message);
            }
            catch (Exception ex)
            {
                if (isReconnecting || !_isConnectStart) //test
                {
                    throw new Exception(ex.Message);
                }
                InnerClose();
                if (_autoReconnect == AutoReconnect.None || _autoReconnect == AutoReconnect.JustDetectDisconnected)
                {
                    throw new Exception(ex.Message);
                }
                else
                {
                    if (!isReconnecting)
                    {
                        ConfiguredTaskAwaitable t = ReconnectAsync().ConfigureAwait(false);
                        throw new Exception(ex.Message);
                    }
                }
            }
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
            catch (NullReferenceException ex)
            {
                _breakTimer.Stop();
                throw new Exception(ex.Message);
            }
            catch (Exception ex)
            {
                if (isReconnecting || !_isConnectStart) //test
                {
                    throw new Exception(ex.Message);
                }
                InnerClose();
                if (_autoReconnect == AutoReconnect.None || _autoReconnect == AutoReconnect.JustDetectDisconnected)
                {
                    throw new Exception(ex.Message);
                }
                else
                {
                    if (!isReconnecting)
                    {
                        ConfiguredTaskAwaitable t = ReconnectAsync().ConfigureAwait(false);
                        throw new Exception(ex.Message);
                    }
                }
            }
        }
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
                        b++;
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
                        _tcpclient = new TcpClient(IPAddress, Port);
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
            label = sWhitespace.Replace(label, "").ToUpper();
            int device_num;
            if (label[0] == 'S' && label[1] == 'B')
            {
                label = label.Substring(2);
                device = 0xA1;
                device_num = int.Parse(label, NumberStyles.HexNumber);
                byte[] bytes = BitConverter.GetBytes(device_num);
                low_num = bytes[0];
                mid_num = bytes[1];
                high_num = bytes[2];
            }
            else if (label[0] == 'S' && label[1] == 'W')
            {
                label = label.Substring(2);
                device = 0xB5;
                device_num = int.Parse(label, NumberStyles.HexNumber);
                byte[] bytes = BitConverter.GetBytes(device_num);
                low_num = bytes[0];
                mid_num = bytes[1];
                high_num = bytes[2];
            }
            else if (label[0] == 'B' && label[1] == 'L')
            {
                label = label.Substring(1);
                device = 0xDC;
                device_num = int.Parse(label, NumberStyles.HexNumber);
                byte[] bytes = BitConverter.GetBytes(device_num);
                low_num = bytes[0];
                mid_num = bytes[1];
                high_num = bytes[2];
            }
            else if (label[0] == 'W')
            {
                label = label.Substring(1);
                device = 0xB4;
                device_num = int.Parse(label, NumberStyles.HexNumber);
                byte[] bytes = BitConverter.GetBytes(device_num);
                low_num = bytes[0];
                mid_num = bytes[1];
                high_num = bytes[2];
            }
            else if (label[0] == 'X')
            {
                label = label.Substring(1);
                device = 0x9C;
                device_num = ConvertOctalToDecimal(int.Parse(label));
                byte[] bytes = BitConverter.GetBytes(device_num);
                low_num = bytes[0];
                mid_num = bytes[1];
                high_num = bytes[2];
            }
            else if (label[0] == 'Y')
            {
                label = label.Substring(1);
                device = 0x9D;
                device_num = ConvertOctalToDecimal(int.Parse(label));
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
                device = GetNameDevice(label.Substring(0, device_num_temp));
                device_num = int.Parse(label.Substring(device_num_temp));
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
        internal override byte GetNameDevice(string device)
        {
            switch (device)
            {
                case "B":
                    return 160;
                case "BL":
                    return 220;
                case "CC":
                    return 195;
                case "CN":
                    return 197;
                case "CS":
                    return 196;
                case "D":
                    return 168;
                case "F":
                    return 147;
                case "L":
                    return 146;
                case "LCN":
                    return 86;
                case "LZ":
                    return 98;
                case "M":
                    return 144;
                case "R":
                    return 175;
                case "S":
                    return 152;
                case "SB":
                    return 161;
                case "SC":
                    return 198;
                case "SD":
                    return 169;
                case "SM":
                    return 145;
                case "SN":
                    return 200;
                case "SS":
                    return 199;
                case "STN":
                    return 200;
                case "SW":
                    return 181;
                case "TC":
                    return 192;
                case "TN":
                    return 194;
                case "TS":
                    return 193;
                case "W":
                    return 180;
                case "X":
                    return 156;
                case "Y":
                    return 157;
                case "Z":
                    return 204;
                default:
                    throw new ArgumentOutOfRangeException("The specified device does not belong to the memory of the PLC");
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
            _tcpclient = new TcpClient();
            _isConnectStart = true;
            try
            {
                _tcpclient.ConnectAsync(IPAddress, Port).Wait(ReceiveTimeout);
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
            IPAddress = ipaddress;
            Port = port;
            _tcpclient = new TcpClient();
            _isConnectStart = true;
            try
            {
                _tcpclient.ConnectAsync(IPAddress, Port).Wait(ReceiveTimeout);
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
            _tcpclient = new TcpClient();
            _isConnectStart = true;
            try
            {
                await _tcpclient.ConnectAsync(IPAddress, Port).ConfigureAwait(false);
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
            IPAddress = !string.IsNullOrEmpty(ipaddress) ? ipaddress : throw new ArgumentException("IP address must valid.", nameof(ipaddress));
            Port = port;
            _tcpclient = new TcpClient();
            _isConnectStart = true;
            try
            {
                await _tcpclient.ConnectAsync(IPAddress, Port).ConfigureAwait(false);
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
            _connected = true;
            if (_autoReconnect == AutoReconnect.None)
                return;
            _breakTimer.Start();
        }
        /// <summary>
        /// Close connection to the server.
        /// </summary>
        public override void Close()
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
        internal override void WriteDevice(string label, bool value)
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
        }
        /// <summary>
        /// Write single value to the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">A single value to be written.</param>
        internal override void WriteDevice(string label, short value)
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
        }
        /// <summary>
        /// Write single value to the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">A single value to be written.</param>

        internal override void WriteDevice(string label, int value)
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
        }
        /// <summary>
        /// Write single value to the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">A single value to be written.</param>
        internal override void WriteDevice(string label, float value)
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
        internal override void WriteDeviceBlock(string label, params bool[] values)
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
        }
        /// <summary>
        ///  Write multiple values to the server in a batch.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="values">Values to be written.</param>
        internal override void WriteDeviceBlock(string label, params short[] values)
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
        }
        /// <summary>
        ///  Write multiple values to the server in a batch.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="values">Values to be written.</param>
        internal override void WriteDeviceBlock(string label, params int[] values)
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
        }
        /// <summary>
        ///  Write multiple values to the server in a batch.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="values">Values to be written.</param>
        internal override void WriteDeviceBlock(string label, params float[] values)
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
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Boolean:
                    {
                        bool[] results = new bool[values.Length];
                        int length = values.Length;
                        for (int i = 0; i < length; ++i)
                            results[i] = (bool)Convert.ChangeType(values[i], TypeCode.Boolean);
                        await WriteDeviceBlockAsync(label, results);
                        break;
                    }
                case TypeCode.Int16:
                    {
                        short[] results = new short[values.Length];
                        int length = values.Length;
                        for (int i = 0; i < length; ++i)
                            results[i] = (short)Convert.ChangeType(values[i], TypeCode.Int16);
                        await WriteDeviceBlockAsync(label, results);
                        break;
                    }
                case TypeCode.UInt16:
                    {
                        short[] results = new short[values.Length];
                        int length = values.Length;
                        for (int i = 0; i < length; ++i)
                            results[i] = (short)(ushort)Convert.ChangeType(values[i], TypeCode.UInt16);
                        await WriteDeviceBlockAsync(label, results);
                        break;
                    }
                case TypeCode.Int32:
                    {
                        int[] results = new int[values.Length];
                        int length = values.Length;
                        for (int i = 0; i < length; ++i)
                            results[i] = (int)Convert.ChangeType(values[i], TypeCode.Int32);
                        await WriteDeviceBlockAsync(label, results);
                        break;
                    }
                case TypeCode.UInt32:
                    {
                        int[] results = new int[values.Length];
                        int length = values.Length;
                        for (int i = 0; i < length; ++i)
                            results[i] = (int)(uint)Convert.ChangeType(values[i], TypeCode.UInt32);
                        await WriteDeviceBlockAsync(label, results);
                        break;
                    }
                case TypeCode.Single:
                    {
                        float[] results6 = new float[values.Length];
                        int length = values.Length;
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
        public override void WriteDeviceRandom(params Bit[] bits)
        {
            int datalength = bits.Length;
            byte[] datalenght_bytes = BitConverter.GetBytes(7 + datalength * 5);
            SendBuffer[7] = datalenght_bytes[0];
            SendBuffer[8] = datalenght_bytes[1];
            SendBuffer[11] = 0x02;
            SendBuffer[12] = 0x14;
            SendBuffer[13] = 0x01;
            SendBuffer[14] = 0x00;
            SendBuffer[15] = (byte)datalength;
            int index1 = 16;
            for (int index2 = 0; index2 < datalength; ++index2)
            {
                SettingDevice(bits[index2].Label, out SendBuffer[index1 + 3], out SendBuffer[index1], out SendBuffer[index1 + 1], out SendBuffer[index1 + 2]);
                if (bits[index2].Value)
                    SendBuffer[index1 + 4] = 0x01;
                else
                    SendBuffer[index1 + 4] = 0x00;
                index1 += 5;
            }
            StreamData(16 + datalength * 5, 11);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
        }
        /// <summary>
        /// Write multiple values to the server randomly.
        /// </summary>
        /// <param name="words"><see cref="dotPLC.Mitsubishi.Types.Word"/> values to be written.</param>
        public override void WriteDeviceRandom(params Word[] words)
        {
            int datalength = words.Length;
            byte[] datalenght_bytes = BitConverter.GetBytes(8 + datalength * 6);
            SendBuffer[7] = datalenght_bytes[0];
            SendBuffer[8] = datalenght_bytes[1];
            SendBuffer[11] = 0x02;
            SendBuffer[12] = 0x14;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SendBuffer[15] = (byte)datalength;
            SendBuffer[16] = 0x00;
            int index1 = 17;
            int num = 17 + datalength * 6;
            for (int index2 = 0; index2 < datalength; ++index2)
            {
                SettingDevice(words[index2].Label, out SendBuffer[index1 + 3], out SendBuffer[index1], out SendBuffer[index1 + 1], out SendBuffer[index1 + 2]);
                byte[] bytes2 = BitConverter.GetBytes(words[index2].Value);
                SendBuffer[index1 + 4] = bytes2[0];
                SendBuffer[index1 + 5] = bytes2[1];
                index1 += 6;
            }
            StreamData(17 + datalength * 6, 11);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];


            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
        }
        /// <summary>
        /// Write multiple values to the server randomly.
        /// </summary>
        /// <param name="dwords"><see cref="dotPLC.Mitsubishi.Types.DWord"/> values to be written.</param>
        public override void WriteDeviceRandom(params DWord[] dwords)
        {
            int datalength = dwords.Length;
            byte[] datalength_bytes = BitConverter.GetBytes(8 + datalength * 8);
            SendBuffer[7] = datalength_bytes[0];
            SendBuffer[8] = datalength_bytes[1];
            SendBuffer[11] = 0x02;
            SendBuffer[12] = 0x14;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SendBuffer[15] = 0x00;
            SendBuffer[16] = (byte)datalength;
            int index1 = 17;
            for (int index2 = 0; index2 < datalength; ++index2)
            {
                SettingDevice(dwords[index2].Label, out SendBuffer[index1 + 3], out SendBuffer[index1], out SendBuffer[index1 + 1], out SendBuffer[index1 + 2]);
                byte[] bytes2 = BitConverter.GetBytes(dwords[index2].Value);
                int index3 = 0;
                for (int index4 = 4; index4 <= 7; ++index4)
                {
                    SendBuffer[index1 + index4] = bytes2[index3];
                    ++index3;
                }
                index1 += 8;
            }
            StreamData(17 + datalength * 8, 11);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
        }
        /// <summary>
        /// Write multiple values to the server randomly.
        /// </summary>
        /// <param name="floats"><see cref="dotPLC.Mitsubishi.Types.Float"/> values to be written.</param>
        public override void WriteDeviceRandom(params Float[] floats)
        {
            int datalength = floats.Length;
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
        }
        /// <summary>
        /// Write multiple values to the server randomly.
        /// </summary>
        /// <param name="words"><see cref="dotPLC.Mitsubishi.Types.Word"/> values to be written.</param>
        /// <param name="dwords"><see cref="dotPLC.Mitsubishi.Types.DWord"/> values to be written.</param>
        /// <param name="floats"><see cref="dotPLC.Mitsubishi.Types.Float"/> values to be written.</param>
        public void WriteDeviceRandom(Word[] words = null, DWord[] dwords = null, Float[] floats = null)
        {
            int num_W = words == null ? 0 : words.Length;
            int num_DW = dwords == null ? 0 : dwords.Length;
            int num_FL = floats == null ? 0 : floats.Length;
            if (num_W == 0 && num_DW == 0 && num_FL == 0)
                return;
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
        internal override bool ReadSingleCoil(string label)
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
        internal override short ReadSingleRegister(string label)
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
        internal override int ReadSingleDouble(string label)
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
        internal override float ReadSingleFloat(string label)
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
        internal override bool[] ReadMultipleCoils(string label, int size)
        {
            SendBuffer[7] = 0x0C;
            SendBuffer[8] = 0x00;
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x04;
            SendBuffer[13] = 0x01;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            byte[] bytes = BitConverter.GetBytes(size);
            SendBuffer[19] = bytes[0];
            SendBuffer[20] = bytes[1];
            StreamData(21, 11 + (size % 2 == 0 ? size / 2 : size / 2 + 1));
            bool[] flagArray = new bool[size];
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
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
        internal override short[] ReadMultipleRegisters(string label, int size)
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
        internal override int[] ReadMultipleDoubles(string label, int size)
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
        internal override float[] ReadMultipleFloats(string label, int size)
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
                        var values = await ReadMultipleCoilsAsync(label, size);
                        for (int i = 0; i < size; ++i)
                            results[i] = (T)Convert.ChangeType(values[i], typeof(T));
                        return results;
                    }
                case TypeCode.Int16:
                    {
                        var values = await ReadMultipleRegistersAsync(label, size);
                        for (int i = 0; i < size; ++i)
                            results[i] = (T)Convert.ChangeType(values[i], typeof(T));
                        return results;
                    }
                case TypeCode.UInt16:
                    {
                        var values = await ReadMultipleRegistersAsync(label, size);
                        for (int i = 0; i < size; ++i)
                            results[i] = (T)Convert.ChangeType((ushort)values[i], typeof(T));
                        return results;
                    }
                case TypeCode.Int32:
                    {
                        var values = await ReadMultipleDoublesAsync(label, size);
                        for (int i = 0; i < size; ++i)
                            results[i] = (T)Convert.ChangeType(values[i], typeof(T));
                        return results;
                    }
                case TypeCode.UInt32:
                    {
                        int[] values5 = await ReadMultipleDoublesAsync(label, size);
                        for (int i = 0; i < size; ++i)
                            results[i] = (T)Convert.ChangeType((uint)values5[i], typeof(T));
                        return results;
                    }
                case TypeCode.Single:
                    {
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
        public override void ReadDeviceRandom(params Bit[] bits)
        {
            if (bits == null)
                return;
            Labels.Clear();
            dsWordBaseBits.Clear();
            for (int index = 0; index < bits.Length; ++index)
            {
                string device;
                int num;
                if (SettingDevice(bits[index].Label, out device, out num))
                {
                    if (Labels.ContainsKey(device))
                        Labels[device].Add(num);
                    else
                        Labels.Add(device, new List<int>() { num });
                }
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
            byte[] bytes = BitConverter.GetBytes(8 + count * 4);
            SendBuffer[7] = bytes[0];
            SendBuffer[8] = bytes[1];
            SendBuffer[11] = 0x03;
            SendBuffer[12] = 0x04;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SendBuffer[15] = (byte)count;
            SendBuffer[16] = 0x00;
            int index1 = 17;
            for (int i = 0; i < count; ++i)
            {
                SettingDevice(dsWordBaseBits[i].Label, out SendBuffer[index1 + 3], out SendBuffer[index1], out SendBuffer[index1 + 1], out SendBuffer[index1 + 2]);
                index1 += 4;
            }
            StreamData(17 + count * 4, 11 + count * 2);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
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
        public override void ReadDeviceRandom(params Word[] words)
        {
            int length = words.Length;
            byte[] bytes = BitConverter.GetBytes(8 + length * 4);
            SendBuffer[7] = bytes[0];
            SendBuffer[8] = bytes[1];
            SendBuffer[11] = 0x03;
            SendBuffer[12] = 0x04;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SendBuffer[15] = (byte)length;
            SendBuffer[16] = 0x00;
            int index1 = 17;
            for (int i = 0; i < length; ++i)
            {
                SettingDevice(words[i].Label, out SendBuffer[index1 + 3], out SendBuffer[index1], out SendBuffer[index1 + 1], out SendBuffer[index1 + 2]);
                index1 += 4;
            }
            StreamData(17 + length * 4, 11 + length * 2);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
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
        public override void ReadDeviceRandom(params DWord[] dwords)
        {
            int length = dwords.Length;
            byte[] bytes = BitConverter.GetBytes(8 + length * 4);
            SendBuffer[7] = bytes[0];
            SendBuffer[8] = bytes[1];
            SendBuffer[11] = 0x03;
            SendBuffer[12] = 0x04;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SendBuffer[15] = 0x00;
            SendBuffer[16] = (byte)length;
            int index1 = 17;
            for (int i = 0; i < length; ++i)
            {
                SettingDevice(dwords[i].Label, out SendBuffer[index1 + 3], out SendBuffer[index1], out SendBuffer[index1 + 1], out SendBuffer[index1 + 2]);
                index1 += 4;
            }
            StreamData(17 + length * 4, 11 + length * 4);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
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
        public override void ReadDeviceRandom(params Float[] floats)
        {
            int length = floats.Length;
            byte[] bytes = BitConverter.GetBytes(8 + length * 4);
            SendBuffer[7] = bytes[0];
            SendBuffer[8] = bytes[1];
            SendBuffer[11] = 0x03;
            SendBuffer[12] = 0x04;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SendBuffer[15] = 0x00;
            SendBuffer[16] = (byte)length;
            int index1 = 17;
            for (int i = 0; i < length; ++i)
            {
                SettingDevice(floats[i].Label, out SendBuffer[index1 + 3], out SendBuffer[index1], out SendBuffer[index1 + 1], out SendBuffer[index1 + 2]);
                index1 += 4;
            }
            StreamData(17 + length * 4, 11 + length * 4);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            }
            else
            {
                for (int i = 0; i < length * 4; i += 4)
                    floats[i / 4].Value = BitConverter.ToSingle(ReceveiBuffer, 11 + i);
            }
        }
        /// <summary>
        /// Read multiple values from the server randomly. <see langword="[RECOMMENDED]"></see>
        /// <example>
        /// <para>For example:</para>
        /// <code>
        /// - <see cref="dotPLC.Mitsubishi.Types.Bit"/>[] bits = <see href="new"/> <see cref="dotPLC.Mitsubishi.Types.Bit"/>[] { <see href="new"/> <see cref="dotPLC.Mitsubishi.Types.Bit"/>("X0"), <see href="new"/> <see cref="dotPLC.Mitsubishi.Types.Bit"/>("M10")};
        /// - <see cref="dotPLC.Mitsubishi.Types.Word"/>[] words = <see href="new"/> <see cref="dotPLC.Mitsubishi.Types.Word"/>[] { <see href="new"/> <see cref="dotPLC.Mitsubishi.Types.Word"/>("D0"), <see href="new"/> <see cref="dotPLC.Mitsubishi.Types.Word"/>("SD5"), <see href="new"/> <see cref="dotPLC.Mitsubishi.Types.Word"/>("SD10")};
        /// - <see cref="dotPLC.Mitsubishi.Types.DWord"/>[] dwords = <see href="new"/> <see cref="dotPLC.Mitsubishi.Types.DWord"/>[] { <see href="new"/> <see cref="dotPLC.Mitsubishi.Types.DWord"/>("D20"), <see href="new"/> <see cref="dotPLC.Mitsubishi.Types.DWord"/>("SD20")};
        /// - <see cref="dotPLC.Mitsubishi.Types.Float"/>[] floats = <see href="new"/> <see cref="dotPLC.Mitsubishi.Types.Float"/>[] { <see href="new"/> <see cref="dotPLC.Mitsubishi.Types.Float"/>("D30")};
        /// ReadDeviceRandom(null, words, null, floats);
        /// ReadDeviceRandom(bits, words, dwords, floats);
        /// </code>
        /// </example>
        /// </summary>
        /// <param name="bits"><see cref="dotPLC.Mitsubishi.Types.Bit"/> values to be read.</param>
        /// <param name="words"><see cref="dotPLC.Mitsubishi.Types.Word"/> values to be read.</param>
        /// <param name="dwords"><see cref="dotPLC.Mitsubishi.Types.DWord"/> values to be read.</param>
        /// <param name="floats"><see cref="dotPLC.Mitsubishi.Types.Float"/> values to be read.</param>
        public override void ReadDeviceRandom(Bit[] bits = null, Word[] words = null, DWord[] dwords = null, Float[] floats = null)
        {
            Labels.Clear();
            dsWordBaseBits.Clear();
            if (bits != null)
            {
                for (int index = 0; index < bits.Length; ++index)
                {
                    string device;
                    int num;
                    if (SettingDevice(bits[index].Label, out device, out num))
                    {
                        if (Labels.ContainsKey(device))
                            Labels[device].Add(num);
                        else
                            Labels.Add(device, new List<int>() { num });
                    }
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
            int num1 = dsWordBaseBits == null ? 0 : dsWordBaseBits.Count; //số bit dựa theo Word
            int num2 = words == null ? 0 : words.Length; //Số W
            int num3 = dwords == null ? 0 : dwords.Length; //Số Dw
            int num4 = floats == null ? 0 : floats.Length; // Số FL
            if (num1 == 0 && num2 == 0 && num3 == 0 && num4 == 0)
                return;
            byte[] bytes = BitConverter.GetBytes(8 + (num1 + num2 + num3 + num4) * 4);
            SendBuffer[7] = bytes[0];
            SendBuffer[8] = bytes[1];
            SendBuffer[11] = 0x03;
            SendBuffer[12] = 0x04;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SendBuffer[15] = (byte)(num2 + num1);
            SendBuffer[16] = (byte)(num3 + num4);
            int index1 = 17;
            int index2 = 17 + num1 * 4;
            int index3 = index2 + num2 * 4;
            int index4 = index3 + num3 * 4;
            if (num1 > 0)
            {
                for (int i = 0; i < num1; ++i)
                {
                    SettingDevice(dsWordBaseBits[i].Label, out SendBuffer[index1 + 3], out SendBuffer[index1], out SendBuffer[index1 + 1], out SendBuffer[index1 + 2]);
                    index1 += 4;
                }
            }
            if (num2 > 0)
            {
                for (int i = 0; i < num2; ++i)
                {
                    SettingDevice(words[i].Label, out SendBuffer[index2 + 3], out SendBuffer[index2], out SendBuffer[index2 + 1], out SendBuffer[index2 + 2]);
                    index2 += 4;
                }
            }
            if (num3 > 0)
            {
                for (int i = 0; i < num3; ++i)
                {
                    SettingDevice(dwords[i].Label, out SendBuffer[index3 + 3], out SendBuffer[index3], out SendBuffer[index3 + 1], out SendBuffer[index3 + 2]);
                    index3 += 4;
                }
            }
            if (num4 > 0)
            {
                for (int i = 0; i < num4; ++i)
                {
                    SettingDevice(floats[i].Label, out SendBuffer[index4 + 3], out SendBuffer[index4], out SendBuffer[index4 + 1], out SendBuffer[index4 + 2]);
                    index4 += 4;
                }
            }
            StreamData(17 + (num1 + num2 + num3 + num4) * 4, 11 + num1 * 2 + num2 * 2 + num3 * 4 + num4 * 4);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            }
            else
            {
                if (num1 > 0)
                {
                    for (int i = 0; i < num1 * 2; i += 2)
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
                if (num2 > 0)
                {
                    for (int i = 0; i < num2 * 2; i += 2)
                        words[i / 2].Value = BitConverter.ToInt16(ReceveiBuffer, 11 + num1 * 2 + i);
                }
                if (num3 > 0)
                {
                    for (int i = 0; i < num3 * 4; i += 4)
                        dwords[i / 4].Value = BitConverter.ToInt32(ReceveiBuffer, 11 + (num1 * 2 + num2 * 2) + i);
                }
                if (num4 > 0)
                {
                    for (int i = 0; i < num4 * 4; i += 4)
                        floats[i / 4].Value = BitConverter.ToSingle(ReceveiBuffer, 11 + (num1 * 2 + num2 * 2 + num3 * 4) + i);
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
            foreach (char ch in loopbackMessage)
            {
                if ((ch < '0' || ch > '9') && (ch < 'A' || ch > 'F') || loopbackMessage.Length > 960)
                    throw new ArgumentOutOfRangeException(nameof(loopbackMessage), loopbackMessage, "Data is sent for up to 960 bytes from the head by treating each character code (\"0\" to \"9\", \"A\" to \"F\") as a 1 byte value.");
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
            }
            else
                flag = ReceveiBuffer[11] == textlength_bytes[0] && ReceveiBuffer[12] == textlength_bytes[1] && loopbackMessage == Encoding.ASCII.GetString(ReceveiBuffer, 13, loopbackMessage.Length);
            return flag;
        }
        /// <summary>
        /// Read the model character string of the server.
        /// </summary>
        /// <returns>The model character string of the server.</returns>
        public override string GetCpuName()
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
            byte[] numArray = password.Length <= 32 && password.Length >= 6 ? BitConverter.GetBytes(8 + password.Length) : throw new ArgumentOutOfRangeException("The password length is the specified characters (6 to 32 characters).");
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
        }
        /// <summary>
        /// Changes the remote password from locked status to unlocked status. (Enables communication to the device.)
        /// </summary>
        /// <param name="password">Specifies a remote password.</param>
        public void RemoteUnlock(string password)
        {
            byte[] numArray = password.Length <= 32 && password.Length >= 6 ? BitConverter.GetBytes(8 + password.Length) : throw new ArgumentOutOfRangeException("The password length is the specified characters (6 to 32 characters).");
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
        public override void WriteText(string label, string text)
        {
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
        }
        /// <summary>
        /// Read text from the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of text to be read.</param>
        /// <returns>Returns text of the specified size.</returns>
        public override string ReadText(string label, int size)
        {
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
    }
}
