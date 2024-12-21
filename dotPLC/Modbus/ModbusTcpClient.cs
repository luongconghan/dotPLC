using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using dotPLC.Initial;
using dotPLC.Mitsubishi;
using dotPLC.Modbus.Exceptions;

namespace dotPLC.Modbus
{
    /// <summary>
    /// Provides client connection for Modbus TCP.
    /// </summary>
    public sealed class ModbusTcpClient : Ethernet, IModbusFuntionCode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModbusTcpClient"></see> class.
        /// </summary>
        public ModbusTcpClient()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModbusTcpClient"></see> class which determines the ip-address and the port number.
        /// </summary>
        /// <param name="ipaddress">IP Address of the server.</param>
        /// <param name="port">Port number of the server.</param>
        public ModbusTcpClient(string ipaddress = "127.0.0.1", int port = 502)
         : this()
        {
            if (!ValidateIPv4(ipaddress)) throw new ArgumentException("Invalid IP address.", nameof(ipaddress));
            _iPAddress = ipaddress;
            Port = port;
        }

        /// <summary>
        /// _transactionID
        /// </summary>
        private ushort _transactionID = 0;

        /// <summary>
        /// Gets or sets the unit identifier.
        /// </summary>
        public byte UnitIdentifier { get; set; } = 0x01;

        /// <summary>
        /// CheckAndThrowExceptionCode
        /// </summary>
        /// <param name="buffer">buffer</param>
        /// <param name="address">address</param>
        private static void CheckAndThrowExceptionCode(byte[] buffer, byte address)
        {
            if (buffer[7] == address & buffer[8] == 0x01)
            {
                throw new FunctionCodeNotSupportedException("Function code not supported by master");
            }
            if (buffer[7] == address & buffer[8] == 0x02)
            {
                throw new StartingAddressInvalidException("Starting address invalid or starting address + quantity invalid");
            }
            if (buffer[7] == address & buffer[8] == 0x03)
            {
                throw new SizeInvalidException("quantity invalid");
            }
            if (buffer[7] == address & buffer[8] == 0x04)
            {
                throw new ModbusException("error reading");
            }
        }

        /// <summary>
        /// Write/Read stream data
        /// </summary>
        /// <param name="writeLenght">number of write lenght</param>
        /// <param name="readLenght">number of read lenght</param>
        private void StreamData(int writeLenght, int readLenght)
        {
            try
            {
                _stream.Write(SendBuffer, 0, writeLenght);
                _stream.Read(ReceveiBuffer, 0, readLenght);
            }
            catch (NullReferenceException)
            {
                throw new SocketNotOpenedException("Socket not opened.");
            }
            catch (Exception)
            {
                throw new ConnectionException("Connection was aborted.");
            }
        }

        /// <summary>
        /// Close connection to the server.
        /// </summary>
        public override void Disconnect()
        {
            _isConnectStart = false;
            if (_tcpclient != null) _tcpclient.Close();
            if (_stream != null)
                _stream.Close();
            _tcpclient = null;
            _connected = false;
        }

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
                throw new ConnectionException("Connect Timeout");
            }
            _stream = _tcpclient.GetStream();
            _stream.ReadTimeout = _readTimeout;
            _stream.WriteTimeout = _writeTimeout;
            _connected = true;
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
                throw new ConnectionException("Connect Timeout");
            }
            _stream = _tcpclient.GetStream();
            _stream.ReadTimeout = _readTimeout;
            _stream.WriteTimeout = _writeTimeout;
            _connected = true;
        }

        /// <summary>
        /// Read Coils from a slave (FC01).
        /// </summary>
        /// <param name="startingAddress">First address to be read.</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returned <see cref="bool"/>[] values.</returns>
        public bool[] ReadCoils(int startingAddress, int size)
        {
            if (startingAddress > 65535 || startingAddress < 0)
            {
                throw new ArgumentException("Starting address must be 0 - 65535.", nameof(startingAddress));
            }
            if (size > 2000 || size < 1)
            {
                throw new ArgumentException("Size must be 1 - 125", nameof(size));
            }
            //MBAP Header
            _transactionID++;
            int lenght = 6; //Message length =FC +Data
            SendBuffer[0] = (byte)(_transactionID >> 8); //Transaction Identifier Hi 
            SendBuffer[1] = (byte)(_transactionID & 0xFF); //Transaction Identifier Hi 
            SendBuffer[2] = 0x00; //Protocol ID Hi (=0 Modbus)
            SendBuffer[3] = 0x00; //Protocol ID Lo
            SendBuffer[4] = (byte)(lenght >> 8); //Lenght Hi
            SendBuffer[5] = (byte)(lenght & 0xFF);  //Lenght Lo
            SendBuffer[6] = UnitIdentifier; //Unit Identifier
            SendBuffer[7] = 0x01; //FC01 -Read coils
            SendBuffer[8] = (byte)(startingAddress >> 8); // Starting address: High byte
            SendBuffer[9] = (byte)(startingAddress & 0xFF); // Starting address: Low byte
            SendBuffer[10] = (byte)(size >> 8); // Count: High byte
            SendBuffer[11] = (byte)(size & 0xFF); // Count: Low byte
            int bytesToRead = size % 8 == 0 ? 9 + size / 8 : 10 + size / 8;
            StreamData(12, bytesToRead);
            CheckAndThrowExceptionCode(ReceveiBuffer, 0x81);
            bool[] coilArray = new bool[size];
            int num = 8, check = 1;
            for (int index = 0; index < size; index++)
            {
                if (index % 8 == 0)
                { num++; check = 1; }
                coilArray[index] = Convert.ToBoolean((ReceveiBuffer[num]) & check);
                check = check << 1;
            }
            return coilArray;
        }

        /// <summary>
        /// Read Discrete Inputs from a slave (FC02).
        /// </summary>
        /// <param name="startingAddress">First address to be read.</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returned <see cref="bool"/>[] values.</returns>
        public bool[] ReadDiscreteInputs(int startingAddress, int size)
        {
            if (startingAddress > 65535 || startingAddress < 0)
            {
                throw new ArgumentException("Starting address must be 0 - 65535.", nameof(startingAddress));
            }
            if (size > 2000 || size < 1)
            {
                throw new ArgumentException("Size must be 1 - 125", nameof(size));
            }
            //MBAP Header
            _transactionID++;
            int lenght = 6; //Message length =FC +Data
            SendBuffer[0] = (byte)(_transactionID >> 8); //Transaction Identifier Hi 
            SendBuffer[1] = (byte)(_transactionID & 0xFF); //Transaction Identifier Hi 
            SendBuffer[2] = 0x00; //Protocol ID Hi (=0 Modbus)
            SendBuffer[3] = 0x00; //Protocol ID Lo
            SendBuffer[4] = (byte)(lenght >> 8); //Lenght Hi
            SendBuffer[5] = (byte)(lenght & 0xFF);  //Lenght Lo
            SendBuffer[6] = UnitIdentifier; //Unit Identifier
            SendBuffer[7] = 0x02; //FC02 -ReadDiscreteInputs
            SendBuffer[8] = (byte)(startingAddress >> 8); // Starting address: High byte
            SendBuffer[9] = (byte)(startingAddress & 0xFF); // Starting address: Low byte
            SendBuffer[10] = (byte)(size >> 8); // Count: High byte
            SendBuffer[11] = (byte)(size & 0xFF); // Count: Low byte
            int bytesToRead = size % 8 == 0 ? 9 + size / 8 : 10 + size / 8;
            StreamData(12, bytesToRead);
            CheckAndThrowExceptionCode(ReceveiBuffer, 0x82);
            bool[] coilArray = new bool[size];
            int num = 8, check = 1;
            for (int index = 0; index < size; index++)
            {
                if (index % 8 == 0)
                { num++; check = 1; }
                coilArray[index] = Convert.ToBoolean((ReceveiBuffer[num]) & check);
                check = check << 1;
            }
            return coilArray;
        }

        /// <summary>
        /// Read Holding Registers from a slave (FC03).
        /// </summary>
        /// <param name="startingAddress">First address to be read.</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returned <see cref="short"/>[] values.</returns>
        public short[] ReadHoldingRegisters(int startingAddress, int size)
        {
            if (startingAddress > 65535 || startingAddress < 0)
            {
                throw new ArgumentException("Starting address must be 0 - 65535.", nameof(startingAddress));
            }
            if (size > 125 || size < 1)
            {
                throw new ArgumentException("Size must be 1 - 125", nameof(size));
            }
            //MBAP Header
            _transactionID++;
            int lenght = 6; //Message length =FC +Data
            SendBuffer[0] = (byte)(_transactionID >> 8); //Transaction Identifier Hi 
            SendBuffer[1] = (byte)(_transactionID & 0xFF); //Transaction Identifier Hi 
            SendBuffer[2] = 0x00; //Protocol ID Hi (=0 Modbus)
            SendBuffer[3] = 0x00; //Protocol ID Lo
            SendBuffer[4] = (byte)(lenght >> 8); //Lenght Hi
            SendBuffer[5] = (byte)(lenght & 0xFF);  //Lenght Lo
            SendBuffer[6] = UnitIdentifier; //Unit Identifier
            SendBuffer[7] = 0x03; //FC03 ReadHoldingRegisters
            SendBuffer[8] = (byte)(startingAddress >> 8); // Starting address: High byte
            SendBuffer[9] = (byte)(startingAddress & 0xFF); // Starting address: Low byte
            SendBuffer[10] = (byte)(size >> 8); // Count: High byte
            SendBuffer[11] = (byte)(size & 0xFF); // Count: Low byte
            StreamData(12, 9 + size * 2);
            CheckAndThrowExceptionCode(ReceveiBuffer, 0x83);
            short[] numArray = new short[size];
            int num = 0;
            for (int index = 9; index < 9 + size * 2; index += 2)
            {
                numArray[num] = (short)((ReceveiBuffer[index] << 8) + ReceveiBuffer[index + 1]);
                num++;
            }
            return numArray;
        }

        /// <summary>
        /// Read Input Registers from a slave (FC04).
        /// </summary>
        /// <param name="startingAddress">First address to be read.</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returned <see cref="short"/>[] values.</returns>
        public short[] ReadInputRegisters(int startingAddress, int size)
        {
            if (startingAddress > 65535 || startingAddress < 0)
            {
                throw new ArgumentException("Starting address must be 0 - 65535.", nameof(startingAddress));
            }
            if (size > 125 || size < 1)
            {
                throw new ArgumentException("Size must be 1 - 125", nameof(size));
            }
            //MBAP Header
            _transactionID++;
            int lenght = 6; //Message length =FC +Data
            SendBuffer[0] = (byte)(_transactionID >> 8); //Transaction Identifier Hi 
            SendBuffer[1] = (byte)(_transactionID & 0xFF); //Transaction Identifier Hi 
            SendBuffer[2] = 0x00; //Protocol ID Hi (=0 Modbus)
            SendBuffer[3] = 0x00; //Protocol ID Lo
            SendBuffer[4] = (byte)(lenght >> 8); //Lenght Hi
            SendBuffer[5] = (byte)(lenght & 0xFF);  //Lenght Lo
            SendBuffer[6] = UnitIdentifier; //Unit Identifier
            SendBuffer[7] = 0x04; //FC04 -ReadInputRegisters
            SendBuffer[8] = (byte)(startingAddress >> 8); // Starting address: High byte
            SendBuffer[9] = (byte)(startingAddress & 0xFF); // Starting address: Low byte
            SendBuffer[10] = (byte)(size >> 8); // Count: High byte
            SendBuffer[11] = (byte)(size & 0xFF); // Count: Low byte
            StreamData(12, 9 + size * 2);
            CheckAndThrowExceptionCode(ReceveiBuffer, 0x84);
            short[] numArray = new short[size];
            int num = 0;
            for (int index = 9; index < 9 + size * 2; index += 2)
            {
                numArray[num] = (short)((ReceveiBuffer[index] << 8) + ReceveiBuffer[index + 1]);
                num++;
            }
            return numArray;
        }

        /// <summary>
        /// Read/Write Multiple Registers to a slave (FC23).
        /// </summary>
        /// <param name="startingAddressRead">First address to be read.</param>
        /// <param name="sizeRead">Number of values to be read.</param>
        /// <param name="startingAddressWrite">First address to be written.</param>
        /// <param name="values">Values to be written.</param>
        /// <returns>Returned <see cref="short"/>[] values.</returns>
        public short[] ReadWriteMultipleRegisters(int startingAddressRead, int sizeRead, int startingAddressWrite, params short[] values)
        {
            if (startingAddressRead > 65535 || startingAddressRead < 0)
            {
                throw new ArgumentException("Starting address must be 0 - 65535.", nameof(startingAddressRead));
            }
            if (startingAddressWrite > 65535 || startingAddressWrite < 0)
            {
                throw new ArgumentException("Starting address must be 0 - 65535.", nameof(startingAddressWrite));
            }
            if (sizeRead > 125 || sizeRead < 0)
            {
                throw new ArgumentException("Read size must be 0 - 125", nameof(sizeRead));
            }

            if (values == null || values.Length > 121 || values.Length < 1)
            {
                throw new ArgumentException("Write size must be 1 - 121", nameof(values));
            }
            _transactionID++;
            int lenght = 11 + values.Length * 2; //Message length =FC +Data
            SendBuffer[0] = (byte)(_transactionID >> 8); //Transaction Identifier Hi 
            SendBuffer[1] = (byte)(_transactionID & 0xFF); //Transaction Identifier Lo 
            SendBuffer[2] = 0x00; //Protocol ID Hi (=0 Modbus)
            SendBuffer[3] = 0x00; //Protocol ID Lo
            SendBuffer[4] = (byte)(lenght >> 8); //Lenght Hi
            SendBuffer[5] = (byte)(lenght & 0xFF);  //Lenght Lo
            SendBuffer[6] = UnitIdentifier; //Unit Identifier
            SendBuffer[7] = 0x17;//FC23 -ReadWriteMultipleRegisters
            SendBuffer[8] = (byte)(startingAddressRead >> 8); // Starting address: High byte
            SendBuffer[9] = (byte)(startingAddressRead & 0xFF); // Starting address: Low byte
            SendBuffer[10] = (byte)(sizeRead >> 8); // Read Count: High byte 0->125
            SendBuffer[11] = (byte)(sizeRead & 0xFF); // Read Count: Low byte
            SendBuffer[12] = (byte)(startingAddressWrite >> 8); // Starting address: High byte
            SendBuffer[13] = (byte)(startingAddressWrite & 0xFF); // Starting address: Low byte
            SendBuffer[14] = (byte)(values.Length >> 8); // Read Count: High byte 1=>242
            SendBuffer[15] = (byte)(values.Length & 0xFF); // Read Count: Low byte
            SendBuffer[16] = (byte)(values.Length * 2); //Number of bytes
            int k = 0;
            for (int i = 0; i < values.Length; i++)
            {
                SendBuffer[17 + k] = (byte)(values[i] >> 8);
                SendBuffer[18 + k] = (byte)(values[i] & 0xFF);
                k += 2;
            }
            StreamData(17 + values.Length * 2, 9 + sizeRead * 2);
            CheckAndThrowExceptionCode(ReceveiBuffer, 0x97);
            short[] numArray = new short[sizeRead];
            int num = 0;
            for (int index = 9; index < 9 + sizeRead * 2; index += 2)
            {
                numArray[num] = (short)((ReceveiBuffer[index] << 8) + ReceveiBuffer[index + 1]);
                num++;
            }
            return numArray;
        }

        /// <summary>
        /// Write Multiple Coils to a slave (FC15).
        /// </summary>
        /// <param name="startingAddress">First address to be written.</param>
        /// <param name="values">Values to be written.</param>
        public void WriteMultipleCoils(int startingAddress, bool[] values)
        {
            if (startingAddress > 65535 || startingAddress < 0)
            {
                throw new ArgumentException("Starting address must be 0 - 65535.", nameof(startingAddress));
            }
            if (values == null || values.Length > 1968 || values.Length < 1)
                throw new ArgumentException("Size of values must be 1 - 1968", nameof(values));
            byte[] data = ConvertBoolArrayToByteArrayOdd(values);
            //MBAP Header
            _transactionID++;
            int lenght = 7 + data.Length; //Message length =FC +Data
            SendBuffer[0] = (byte)(_transactionID >> 8); //Transaction Identifier Hi 
            SendBuffer[1] = (byte)(_transactionID & 0xFF); //Transaction Identifier Hi 
            SendBuffer[2] = 0x00; //Protocol ID Hi (=0 Modbus)
            SendBuffer[3] = 0x00; //Protocol ID Lo
            SendBuffer[4] = (byte)(lenght >> 8); //Lenght Hi
            SendBuffer[5] = (byte)(lenght & 0xFF);  //Lenght Lo
            SendBuffer[6] = UnitIdentifier; //Unit Identifier
            SendBuffer[7] = 0x0F; //FC15 -WriteSingleCoil
            SendBuffer[8] = (byte)(startingAddress >> 8); // Starting address: High byte
            SendBuffer[9] = (byte)(startingAddress & 0xFF); // Starting address: Low byte
            SendBuffer[10] = (byte)(values.Length >> 8); ; // Write points: High byte
            SendBuffer[11] = (byte)(values.Length & 0xFF);// Write points: Low byte
            SendBuffer[12] = (byte)data.Length;// Number of bytes
            Array.Copy(data, 0, SendBuffer, 13, data.Length);
            StreamData(13 + data.Length, 12);
            CheckAndThrowExceptionCode(ReceveiBuffer, 0x8F);
        }

        /// <summary>
        /// Write Multiple Registers to a slave (FC16).
        /// </summary>
        /// <param name="startingAddress">First address to be written.</param>
        /// <param name="values">Values to be written.</param>
        public void WriteMultipleRegisters(int startingAddress, short[] values)
        {
            if (startingAddress > 65535 || startingAddress < 0)
            {
                throw new ArgumentException("Starting address must be 0 - 65535.", nameof(startingAddress));
            }
            if (values == null || values.Length > 123 || values.Length < 1)
                throw new ArgumentException("Size of values must be 1 - 123", nameof(values));
            //MBAP Header
            _transactionID++;
            int lenght = 7 + values.Length * 2; //Message length =FC +Data
            SendBuffer[0] = (byte)(_transactionID >> 8); //Transaction Identifier Hi 
            SendBuffer[1] = (byte)(_transactionID & 0xFF); //Transaction Identifier Hi 
            SendBuffer[2] = 0x00; //Protocol ID Hi (=0 Modbus)
            SendBuffer[3] = 0x00; //Protocol ID Lo
            SendBuffer[4] = (byte)(lenght >> 8); //Lenght Hi
            SendBuffer[5] = (byte)(lenght & 0xFF);  //Lenght Lo
            SendBuffer[6] = UnitIdentifier; //Unit Identifier
            SendBuffer[7] = 0x0F; //FC15 -WriteSingleCoil
            SendBuffer[8] = (byte)(startingAddress >> 8); // Starting address: High byte
            SendBuffer[9] = (byte)(startingAddress & 0xFF); // Starting address: Low byte
            SendBuffer[10] = (byte)(values.Length >> 8); ; // Write points: High byte
            SendBuffer[11] = (byte)(values.Length & 0xFF);// Write points: Low byte
            SendBuffer[12] = (byte)(values.Length * 2);// Number of bytes
            int k = 0;
            for (int i = 0; i < values.Length; i++)
            {
                SendBuffer[13 + k] = (byte)(values[i] >> 8);
                SendBuffer[14 + k] = (byte)(values[i] & 0xFF);
                k += 2;
            }
            StreamData(13 + values.Length * 2, 12);
            CheckAndThrowExceptionCode(ReceveiBuffer, 0x90);
        }

        /// <summary>
        /// Write Single Coil to a slave (FC05).
        /// </summary>
        /// <param name="startingAddress">Address to be written.</param>
        /// <param name="value">A single value to be written.</param>
        public void WriteSingleCoil(int startingAddress, bool value)
        {
            if (startingAddress > 65535 || startingAddress < 0)
            {
                throw new ArgumentException("Starting address must be 0 - 65535.", nameof(startingAddress));
            }
            //MBAP Header
            _transactionID++;
            int lenght = 6; //Message length =FC +Data
            SendBuffer[0] = (byte)(_transactionID >> 8); //Transaction Identifier Hi 
            SendBuffer[1] = (byte)(_transactionID & 0xFF); //Transaction Identifier Hi 
            SendBuffer[2] = 0x00; //Protocol ID Hi (=0 Modbus)
            SendBuffer[3] = 0x00; //Protocol ID Lo
            SendBuffer[4] = (byte)(lenght >> 8); //Lenght Hi
            SendBuffer[5] = (byte)(lenght & 0xFF);  //Lenght Lo
            SendBuffer[6] = UnitIdentifier; //Unit Identifier
            SendBuffer[7] = 0x05; //FC05 -WriteSingleCoil
            SendBuffer[8] = (byte)(startingAddress >> 8); // Starting address: High byte
            SendBuffer[9] = (byte)(startingAddress & 0xFF); // Starting address: Low byte
            SendBuffer[10] = (byte)(value == true ? 0xFF : 0x00); // Write Data: High byte
            SendBuffer[11] = 0x00;// Write Data: Low byte
            StreamData(12, 12);
            CheckAndThrowExceptionCode(ReceveiBuffer, 0x85);
        }

        /// <summary>
        /// Write Single Register to a slave (FC06).
        /// </summary>
        /// <param name="startingAddress">First address to be written.</param>
        /// <param name="value">A value to be written.</param>
        public void WriteSingleRegister(int startingAddress, short value)
        {
            if (startingAddress > 65535 || startingAddress < 0)
            {
                throw new ArgumentException("Starting address must be 0 - 65535.", nameof(startingAddress));
            }
            //MBAP Header
            _transactionID++;
            int lenght = 6; //Message length =FC +Data
            SendBuffer[0] = (byte)(_transactionID >> 8); //Transaction Identifier Hi 
            SendBuffer[1] = (byte)(_transactionID & 0xFF); //Transaction Identifier Hi 
            SendBuffer[2] = 0x00; //Protocol ID Hi (=0 Modbus)
            SendBuffer[3] = 0x00; //Protocol ID Lo
            SendBuffer[4] = (byte)(lenght >> 8); //Lenght Hi
            SendBuffer[5] = (byte)(lenght & 0xFF);  //Lenght Lo
            SendBuffer[6] = UnitIdentifier; //Unit Identifier
            SendBuffer[7] = 0x06; //FC06 -WriteSingleRegister
            SendBuffer[8] = (byte)(startingAddress >> 8); // Starting address: High byte
            SendBuffer[9] = (byte)(startingAddress & 0xFF); // Starting address: Low byte
            SendBuffer[10] = (byte)(value >> 8); ; // Write Data: High byte
            SendBuffer[11] = (byte)(value & 0xFF);// Write Data: Low byte
            StreamData(12, 12);
            CheckAndThrowExceptionCode(ReceveiBuffer, 0x86);
        }

        /// <summary>
        /// SettingDevice
        /// </summary>
        /// <param name="label">label</param>
        /// <param name="device">device</param>
        /// <param name="low_num">low_num</param>
        /// <param name="mid_num">mid_num</param>
        /// <param name="high_num">high_num</param>
        /// <returns></returns>
        protected internal override int SettingDevice(string label, out byte device, out byte low_num, out byte mid_num, out byte high_num)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// SetupBuffer
        /// </summary>
        protected internal override void SetupBuffer()
        {
            
        }
       
    }
}
