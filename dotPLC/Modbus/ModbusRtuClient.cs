using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using dotPLC.Initial;
using dotPLC.Modbus.Exceptions;

namespace dotPLC.Modbus
{
    /// <summary>
    /// Provides client connection for Modbus RTU.
    /// </summary>
    public partial class ModbusRtuClient : Serial, IModbusFuntionCode
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="ModbusRtuClient"></see> class.
        /// </summary>
        public ModbusRtuClient()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModbusRtuClient"></see> class which determines the serial port.
        /// </summary>
        /// <param name="serialPort">The serial port.</param>
        public ModbusRtuClient(string serialPort) : this()
        {
            if (string.IsNullOrEmpty(serialPort) || string.IsNullOrWhiteSpace(serialPort))
                throw new ArgumentException(nameof(serialPort));
            _serialcomport = serialPort;
        }

        /// <summary>
        /// _queue
        /// </summary>
        internal readonly TaskQueue _queue = new TaskQueue();

        /// <summary>
        /// CheckAndThrowExceptionCode
        /// </summary>
        /// <param name="buffer">buffer</param>
        /// <param name="address">address</param>
        /// <param name="crcCount">crcCount</param>
        /// <param name="isDataReceived">isDataReceived</param>
        private static void CheckAndThrowExceptionCode(byte[] buffer, byte address, int crcCount, bool isDataReceived)
        {
            if (buffer[1] == address & buffer[2] == 0x01)
            {
                throw new FunctionCodeNotSupportedException("Function code not supported by master");
            }
            if (buffer[1] == address & buffer[2] == 0x02)
            {
                throw new StartingAddressInvalidException("Starting address invalid or starting address + quantity invalid");
            }
            if (buffer[1] == address & buffer[2] == 0x03)
            {
                throw new SizeInvalidException("quantity invalid");
            }
            if (buffer[1] == address & buffer[2] == 0x04)
            {
                throw new ModbusException("error reading");
            }

            var crcRead = BitConverter.GetBytes(CalculateCRC(buffer, crcCount));
            if ((crcRead[0] != buffer[crcCount] | crcRead[1] != buffer[crcCount + 1]) & isDataReceived)
            {
                throw new CRCCheckFailedException("Response CRC check failed");
            }
            else if (!isDataReceived)
            {
                throw new TimeoutException("No Response from Modbus Slave");
            }
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
            SendBuffer[0] = UnitIdentifier; //ID
            SendBuffer[1] = 0x01; //FC01 -Read coils
            SendBuffer[2] = (byte)(startingAddress >> 8); // Starting address: High byte
            SendBuffer[3] = (byte)(startingAddress & 0xFF); // Starting address: Low byte

            SendBuffer[4] = (byte)(size >> 8); // Count: High byte
            SendBuffer[5] = (byte)(size & 0xFF); // Count: Low byte
            short crc = CalculateCRC(SendBuffer, 6);
            SendBuffer[6] = (byte)(crc & 0xFF); // CRC: Low byte
            SendBuffer[7] = (byte)(crc >> 8); // CRC: Low byte


            _bytesToRead = size % 8 == 0 ? 5 + size / 8 : 6 + size / 8;
            _isDataReceived = false;
            ReceveiBuffer[0] = 0x00;
            _serialport.Write(SendBuffer, 0, 8);
            DateTime dateTimeSend = DateTime.Now;// thời gian vừa gửi xong
            //Vòng lặp này là connectimeout tương đương với readtimeout => Chỉ cho phét đọc dữ liệu trong thời gian cho phép
            while (ReceveiBuffer[0] != UnitIdentifier && !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * _readtimeout))
            {
                //Vòng lặp này để đợi đến khi sự kiện serialport.RecveiData đọc xong dữ liệu, nếu chưa cho phép đọc thì đợi 1ms liên tục cho đến khi Read xong mới thôi
                while (!_isDataReceived && !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * _readtimeout))
                    _spinWait.SpinOnce(); // Thread.Sleep(1);
            }

            //Lỗi modbus
            CheckAndThrowExceptionCode(ReceveiBuffer, 0x81, ReceveiBuffer[2] + 3, _isDataReceived);

            bool[] coilArray = new bool[size];
            int num = 2, check = 1;
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
            SendBuffer[0] = UnitIdentifier; //ID
            SendBuffer[1] = 0x02; //FC02 -ReadDiscreteInputs
            SendBuffer[2] = (byte)(startingAddress >> 8); // Starting address: High byte
            SendBuffer[3] = (byte)(startingAddress & 0xFF); // Starting address: Low byte

            SendBuffer[4] = (byte)(size >> 8); // Count: High byte
            SendBuffer[5] = (byte)(size & 0xFF); // Count: Low byte
            short crc = CalculateCRC(SendBuffer, 6);
            SendBuffer[6] = (byte)(crc & 0xFF); // CRC: Low byte
            SendBuffer[7] = (byte)(crc >> 8); // CRC: Low byte


            _bytesToRead = size % 8 == 0 ? 5 + size / 8 : 6 + size / 8;
            _isDataReceived = false;
            ReceveiBuffer[0] = 0x00;
            _serialport.Write(SendBuffer, 0, 8);
            DateTime dateTimeSend = DateTime.Now;// thời gian vừa gửi xong
            //Vòng lặp này là connectimeout tương đương với readtimeout => Chỉ cho phét đọc dữ liệu trong thời gian cho phép
            while (ReceveiBuffer[0] != UnitIdentifier && !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * _readtimeout))
            {
                //Vòng lặp này để đợi đến khi sự kiện serialport.RecveiData đọc xong dữ liệu, nếu chưa cho phép đọc thì đợi 1ms liên tục cho đến khi Read xong mới thôi
                while (!_isDataReceived && !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * _readtimeout))
                    _spinWait.SpinOnce(); // Thread.Sleep(1);
            }

            //Lỗi modbus
            CheckAndThrowExceptionCode(ReceveiBuffer, 0x82, ReceveiBuffer[2] + 3, _isDataReceived);

            bool[] coilArray = new bool[size];
            int num = 2, check = 1;
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
            SendBuffer[0] = UnitIdentifier; //ID
            SendBuffer[1] = 0x03; //FC03 -ReadHoldingRegisters
            SendBuffer[2] = (byte)(startingAddress >> 8); // Starting address: High byte
            SendBuffer[3] = (byte)(startingAddress & 0xFF); // Starting address: Low byte
            SendBuffer[4] = (byte)(size >> 8); // Count: High byte
            SendBuffer[5] = (byte)(size & 0xFF); // Count: Low byte
            short crc = CalculateCRC(SendBuffer, 6);
            SendBuffer[6] = (byte)(crc & 0xFF); // CRC: Low byte
            SendBuffer[7] = (byte)(crc >> 8); // CRC: Low byte


            _bytesToRead = 5 + size * 2;
            _isDataReceived = false;
            ReceveiBuffer[0] = 0x00;
            _serialport.Write(SendBuffer, 0, 8);
            DateTime dateTimeSend = DateTime.Now;// thời gian vừa gửi xong
            //Vòng lặp này là connectimeout tương đương với readtimeout => Chỉ cho phét đọc dữ liệu trong thời gian cho phép
            while (ReceveiBuffer[0] != UnitIdentifier && !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * _readtimeout))
            {
                //Vòng lặp này để đợi đến khi sự kiện serialport.RecveiData đọc xong dữ liệu, nếu chưa cho phép đọc thì đợi 1ms liên tục cho đến khi Read xong mới thôi
                while (!_isDataReceived && !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * _readtimeout))
                    _spinWait.SpinOnce(); // Thread.Sleep(1);
            }

            //Lỗi modbus
            CheckAndThrowExceptionCode(ReceveiBuffer, 0x83, size * 2 + 3, _isDataReceived);

            short[] numArray = new short[size];
            int num = 0;
            for (int index = 3; index < 3 + size * 2; index += 2)
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
            SendBuffer[0] = UnitIdentifier; //ID
            SendBuffer[1] = 0x04; //FC04 -ReadInputRegisters
            SendBuffer[2] = (byte)(startingAddress >> 8); // Starting address: High byte
            SendBuffer[3] = (byte)(startingAddress & 0xFF); // Starting address: Low byte
            SendBuffer[4] = (byte)(size >> 8); // Count: High byte
            SendBuffer[5] = (byte)(size & 0xFF); // Count: Low byte
            short crc = CalculateCRC(SendBuffer, 6);
            SendBuffer[6] = (byte)(crc & 0xFF); // CRC: Low byte
            SendBuffer[7] = (byte)(crc >> 8); // CRC: Low byte


            _bytesToRead = 5 + size * 2;
            _isDataReceived = false;
            ReceveiBuffer[0] = 0x00;
            _serialport.Write(SendBuffer, 0, 8);
            DateTime dateTimeSend = DateTime.Now;// thời gian vừa gửi xong
            //Vòng lặp này là connectimeout tương đương với readtimeout => Chỉ cho phét đọc dữ liệu trong thời gian cho phép
            while (ReceveiBuffer[0] != UnitIdentifier && !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * _readtimeout))
            {
                //Vòng lặp này để đợi đến khi sự kiện serialport.RecveiData đọc xong dữ liệu, nếu chưa cho phép đọc thì đợi 1ms liên tục cho đến khi Read xong mới thôi
                while (!_isDataReceived && !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * _readtimeout))
                    _spinWait.SpinOnce(); // Thread.Sleep(1);
            }

            //Lỗi modbus
            CheckAndThrowExceptionCode(ReceveiBuffer, 0x84, size * 2 + 3, _isDataReceived);

            short[] numArray = new short[size];
            int num = 0;
            for (int index = 3; index < 3 + size * 2; index += 2)
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

            SendBuffer[0] = UnitIdentifier; //ID
            SendBuffer[1] = 0x17; //FC23 -ReadWriteMultipleRegisters
            SendBuffer[2] = (byte)(startingAddressRead >> 8); // Starting address: High byte
            SendBuffer[3] = (byte)(startingAddressRead & 0xFF); // Starting address: Low byte
            SendBuffer[4] = (byte)(sizeRead >> 8); // Read Count: High byte 0->125
            SendBuffer[5] = (byte)(sizeRead & 0xFF); // Read Count: Low byte
            SendBuffer[6] = (byte)(startingAddressWrite >> 8); // Starting address: High byte
            SendBuffer[7] = (byte)(startingAddressWrite & 0xFF); // Starting address: Low byte
            SendBuffer[8] = (byte)(values.Length >> 8); // Read Count: High byte 1=>242
            SendBuffer[9] = (byte)(values.Length & 0xFF); // Read Count: Low byte
            SendBuffer[10] = (byte)(values.Length * 2);
            int k = 0;
            for (int i = 0; i < values.Length; i++)
            {
                SendBuffer[11 + k] = (byte)(values[i] >> 8);
                SendBuffer[12 + k] = (byte)(values[i] & 0xFF);
                k += 2;
            }

            short crc = CalculateCRC(SendBuffer, 11 + values.Length * 2);
            SendBuffer[11 + values.Length * 2] = (byte)(crc & 0xFF); // CRC: Low byte
            SendBuffer[12 + values.Length * 2] = (byte)(crc >> 8); // CRC: Low byte


            _bytesToRead = 5 + sizeRead * 2;
            _isDataReceived = false;
            ReceveiBuffer[0] = 0x00;
            _serialport.Write(SendBuffer, 0, 13 + values.Length * 2);
            DateTime dateTimeSend = DateTime.Now;// thời gian vừa gửi xong
            //Vòng lặp này là connectimeout tương đương với readtimeout => Chỉ cho phét đọc dữ liệu trong thời gian cho phép
            while (ReceveiBuffer[0] != UnitIdentifier && !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * _readtimeout))
            {
                //Vòng lặp này để đợi đến khi sự kiện serialport.RecveiData đọc xong dữ liệu, nếu chưa cho phép đọc thì đợi 1ms liên tục cho đến khi Read xong mới thôi
                while (!_isDataReceived && !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * _readtimeout))
                    _spinWait.SpinOnce(); // Thread.Sleep(1);
            }

            //Lỗi modbus
            CheckAndThrowExceptionCode(ReceveiBuffer, 0x97, sizeRead * 2 + 3, _isDataReceived);

            short[] numArray = new short[sizeRead];
            int num = 0;
            for (int index = 3; index < 3 + sizeRead * 2; index += 2)
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
        public void WriteMultipleCoils(int startingAddress, params bool[] values)
        {
            if (startingAddress > 65535 || startingAddress < 0)
            {
                throw new ArgumentException("Starting address must be 0 - 65535.", nameof(startingAddress));
            }
            if (values == null || values.Length > 1968 || values.Length < 1)
                throw new ArgumentException("Size of values must be 1 - 1968", nameof(values));
            byte[] data = ConvertBoolArrayToByteArrayOdd(values);

            SendBuffer[0] = UnitIdentifier; //ID
            SendBuffer[1] = 0x0F; //FC15 -WriteSingleCoil
            SendBuffer[2] = (byte)(startingAddress >> 8); // Starting address: High byte
            SendBuffer[3] = (byte)(startingAddress & 0xFF); // Starting address: Low byte
            SendBuffer[4] = (byte)(values.Length >> 8); // Quantity of Coils Hi: High byte
            SendBuffer[5] = (byte)(values.Length & 0xFF); // Quantity of Coils Lo: Low byte
            SendBuffer[6] = (byte)data.Length; //Byte count
            for (int i = 0; i < data.Length; i++)
            {
                SendBuffer[7 + i] = data[i];
            }
            short crc = CalculateCRC(SendBuffer, 7 + data.Length);
            SendBuffer[7 + data.Length] = (byte)(crc & 0xFF); // CRC: Low byte
            SendBuffer[8 + data.Length] = (byte)(crc >> 8); // CRC: High byte

            _bytesToRead = 8;
            _isDataReceived = false;
            ReceveiBuffer[0] = 0x00;
            _serialport.Write(SendBuffer, 0, 9 + data.Length);
            DateTime dateTimeSend = DateTime.Now;// thời gian vừa gửi xong
            //Vòng lặp này là connectimeout tương đương với readtimeout => Chỉ cho phét đọc dữ liệu trong thời gian cho phép
            while (ReceveiBuffer[0] != UnitIdentifier && !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * _readtimeout))
            {
                //Vòng lặp này để đợi đến khi sự kiện serialport.RecveiData đọc xong dữ liệu, nếu chưa cho phép đọc thì đợi 1ms liên tục cho đến khi Read xong mới thôi
                while (!_isDataReceived && !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * _readtimeout))
                    _spinWait.SpinOnce(); // Thread.Sleep(1);
            }
            //Lỗi modbus
            CheckAndThrowExceptionCode(ReceveiBuffer, 0x8F, 6, _isDataReceived);
        }

        /// <summary>
        /// Write Multiple Registers to a slave (FC16).
        /// </summary>
        /// <param name="startingAddress">First address to be written.</param>
        /// <param name="values">Values to be written.</param>
        public void WriteMultipleRegisters(int startingAddress, params short[] values)
        {
            if (startingAddress > 65535 || startingAddress < 0)
            {
                throw new ArgumentException("Starting address must be 0 - 65535.", nameof(startingAddress));
            }
            if (values == null || values.Length > 123 || values.Length < 1)
                throw new ArgumentException("Size of values must be 1 - 123", nameof(values));

            SendBuffer[0] = UnitIdentifier; //ID
            SendBuffer[1] = 0x10; //FC16 -WriteSingleRegister
            SendBuffer[2] = (byte)(startingAddress >> 8); // Starting address: High byte
            SendBuffer[3] = (byte)(startingAddress & 0xFF); // Starting address: Low byte
            SendBuffer[4] = (byte)(values.Length >> 8); // Quantity of Registers Hi: High byte
            SendBuffer[5] = (byte)(values.Length & 0xFF); //Quantity of Registers Lo: Low byte
            SendBuffer[6] = (byte)(values.Length * 2); //Byte Count
            int k = 0;
            for (int i = 0; i < values.Length; i++)
            {
                SendBuffer[7 + k] = (byte)(values[i] >> 8);
                SendBuffer[8 + k] = (byte)(values[i] & 0xFF);
                k += 2;
            }

            short crc = CalculateCRC(SendBuffer, 7 + values.Length * 2);
            SendBuffer[7 + values.Length * 2] = (byte)(crc & 0xFF); // CRC: Low byte
            SendBuffer[8 + values.Length * 2] = (byte)(crc >> 8); // CRC: High byte

            _bytesToRead = 8;
            _isDataReceived = false;
            ReceveiBuffer[0] = 0x00;
            _serialport.Write(SendBuffer, 0, 9 + values.Length * 2);
            DateTime dateTimeSend = DateTime.Now;// thời gian vừa gửi xong
            //Vòng lặp này là connectimeout tương đương với readtimeout => Chỉ cho phét đọc dữ liệu trong thời gian cho phép
            while (ReceveiBuffer[0] != UnitIdentifier && !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * _readtimeout))
            {
                //Vòng lặp này để đợi đến khi sự kiện serialport.RecveiData đọc xong dữ liệu, nếu chưa cho phép đọc thì đợi 1ms liên tục cho đến khi Read xong mới thôi
                while (!_isDataReceived && !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * _readtimeout))
                    _spinWait.SpinOnce(); // Thread.Sleep(1);
            }
            //Lỗi modbus
            CheckAndThrowExceptionCode(ReceveiBuffer, 0x90, 6, _isDataReceived);
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
            SendBuffer[0] = UnitIdentifier; //ID
            SendBuffer[1] = 0x05; //FC05 -WriteSingleCoil
            SendBuffer[2] = (byte)(startingAddress >> 8); // Starting address: High byte
            SendBuffer[3] = (byte)(startingAddress & 0xFF); // Starting address: Low byte
            SendBuffer[4] = (byte)(value == true ? 0xFF : 0x00); // Write Data: High byte
            SendBuffer[5] = 0x00;// Write Data: Low byte
            short crc = CalculateCRC(SendBuffer, 6);
            SendBuffer[6] = (byte)(crc & 0xFF); // CRC: Low byte
            SendBuffer[7] = (byte)(crc >> 8); // CRC: Low byte

            _bytesToRead = 8;
            _isDataReceived = false;
            ReceveiBuffer[0] = 0x00;
            _serialport.Write(SendBuffer, 0, 8);
            DateTime dateTimeSend = DateTime.Now;// thời gian vừa gửi xong
            //Vòng lặp này là connectimeout tương đương với readtimeout => Chỉ cho phét đọc dữ liệu trong thời gian cho phép
            while (ReceveiBuffer[0] != UnitIdentifier && !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * _readtimeout))
            {
                //Vòng lặp này để đợi đến khi sự kiện serialport.RecveiData đọc xong dữ liệu, nếu chưa cho phép đọc thì đợi 1ms liên tục cho đến khi Read xong mới thôi
                while (!_isDataReceived && !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * _readtimeout))
                    _spinWait.SpinOnce(); // Thread.Sleep(1);
            }
            //Lỗi modbus
            CheckAndThrowExceptionCode(ReceveiBuffer, 0x85, 6, _isDataReceived);
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
            SendBuffer[0] = UnitIdentifier; //ID
            SendBuffer[1] = 0x06; //FC06 -WriteSingleRegister
            SendBuffer[2] = (byte)(startingAddress >> 8); // Starting address: High byte
            SendBuffer[3] = (byte)(startingAddress & 0xFF); // Starting address: Low byte
            SendBuffer[4] = (byte)(value >> 8); // Write Data: High byte
            SendBuffer[5] = (byte)(value & 0xFF); // Write Data: Low byte
            short crc = CalculateCRC(SendBuffer, 6);
            SendBuffer[6] = (byte)(crc & 0xFF); // CRC: Low byte
            SendBuffer[7] = (byte)(crc >> 8); // CRC: Low byte

            _bytesToRead = 8;
            _isDataReceived = false;
            ReceveiBuffer[0] = 0x00;
            _serialport.Write(SendBuffer, 0, 8);
            DateTime dateTimeSend = DateTime.Now;// thời gian vừa gửi xong
            //Vòng lặp này là connectimeout tương đương với readtimeout => Chỉ cho phét đọc dữ liệu trong thời gian cho phép
            while (ReceveiBuffer[0] != UnitIdentifier && !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * _readtimeout))
            {
                //Vòng lặp này để đợi đến khi sự kiện serialport.RecveiData đọc xong dữ liệu, nếu chưa cho phép đọc thì đợi 1ms liên tục cho đến khi Read xong mới thôi
                while (!_isDataReceived && !((DateTime.Now.Ticks - dateTimeSend.Ticks) > TimeSpan.TicksPerMillisecond * _readtimeout))
                    _spinWait.SpinOnce(); // Thread.Sleep(1);
            }
            //Lỗi modbus
            CheckAndThrowExceptionCode(ReceveiBuffer, 0x86, 6, _isDataReceived);
        }

        /// <summary>
        /// SetupBuffer
        /// </summary>
        protected internal override void SetupBuffer()
        {
            _serialport = new SerialPort();
            _serialport.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
        }

        /// <summary>
        /// CheckModbusPacket
        /// </summary>
        /// <param name="readBuffer">readBuffer</param>
        /// <param name="length">length</param>
        /// <returns>bool</returns>
        private static bool CheckModbusPacket(byte[] readBuffer, int length)
        {
            // minimum length 6 bytes
            if (length < 6)
                return false;
            //SlaveID correct
            if ((readBuffer[0] < 1) | (readBuffer[0] > 247))
                return false;
            //CRC correct?
            byte[] crc = new byte[2];
            crc = BitConverter.GetBytes(CalculateCRC(readBuffer, length - 2));
            if (crc[0] != readBuffer[length - 2] || crc[1] != readBuffer[length - 1])
                return false;
            return true;
        }

        /// <summary>
        /// DataReceivedHandler
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">e</param>
        private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
        {
            _serialport.DataReceived -= DataReceivedHandler;
            if (_bytesToRead == 0)
            {
                _serialport.DiscardInBuffer(); //Xoá dữ liệu được nhận
                _serialport.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
                return;
            }
            int readIndex = 0;
            DateTime dateTimeLastRead = DateTime.Now;
            do
            {
                try
                {
                    dateTimeLastRead = DateTime.Now;
                    //Đợi đến khi BytesToRead khác 0
                    while (_serialport.BytesToRead == 0)
                    {
                        Thread.Sleep(10);
                        if ((DateTime.Now.Ticks - dateTimeLastRead.Ticks) > _ticksWait)
                            break;
                    }
                    readIndex += _serialport.Read(ReceveiBuffer, readIndex, _serialport.BytesToRead);
                }
                catch { }
                //Vòng lặp do while bên ngoài này đảm bảo gói tin đọc được đủ, đúng crc.. nếu không nó lại vào vòng lặp trong để đọc tiếp
                if (_bytesToRead <= readIndex)
                    break;
                if (_bytesToRead <= readIndex || CheckModbusPacket(ReceveiBuffer, (readIndex <= 256) ? readIndex : 256))
                    break;
            }
            while ((DateTime.Now.Ticks - dateTimeLastRead.Ticks) < _ticksWait);
            _bytesToRead = 0;
            _isDataReceived = true;
            _serialport.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
        }

        /// <summary>
        /// Read Coils from a slave as an asynchronous operation (FC01).
        /// </summary>
        /// <param name="startingAddress">First address to be read.</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// <see href="TResult"></see> is <see cref="bool"></see>[].</returns>
        public async Task<bool[]> ReadCoilsAsync(int startingAddress, int size)
            => await _queue.Enqueue(async () => await Task.Run(() => ReadCoils(startingAddress, size)).ConfigureAwait(false)).ConfigureAwait(false);

        /// <summary>
        /// Read Discrete Inputs from a slave as an asynchronous operation (FC02).
        /// </summary>
        /// <param name="startingAddress">First address to be read.</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// <see href="TResult"></see> is <see cref="bool"></see>[].</returns>
        public async Task<bool[]> ReadDiscreteInputsAsync(int startingAddress, int size)
            => await _queue.Enqueue(async () => await Task.Run(() => ReadDiscreteInputs(startingAddress, size)).ConfigureAwait(false)).ConfigureAwait(false);

        /// <summary>
        /// Read Holding Registers from a slave as an asynchronous operation (FC03).
        /// </summary>
        /// <param name="startingAddress">First address to be read.</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// <see href="TResult"></see> is <see cref="short"></see>[].</returns>
        public async Task<short[]> ReadHoldingRegistersAsync(int startingAddress, int size)
            => await _queue.Enqueue(async () => await Task.Run(() => ReadHoldingRegisters(startingAddress, size)).ConfigureAwait(false)).ConfigureAwait(false);

        /// <summary>
        /// Read Input Registers from a slave as an asynchronous operation (FC04).
        /// </summary>
        /// <param name="startingAddress">First address to be read.</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// <see href="TResult"></see> is <see cref="short"></see>[].</returns>
        public async Task<short[]> ReadInputRegistersAsync(int startingAddress, int size)
            => await _queue.Enqueue(async () => await Task.Run(() => ReadInputRegisters(startingAddress, size)).ConfigureAwait(false)).ConfigureAwait(false);

        /// <summary>
        /// Read/Write Multiple Registers to a slave as an asynchronous operation (FC23).
        /// </summary>
        /// <param name="startingAddressRead">First address to be read.</param>
        /// <param name="sizeRead">Number of values to be read.</param>
        /// <param name="startingAddressWrite">First address to be written.</param>
        /// <param name="values">Values to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// <see href="TResult"></see> is <see cref="short"></see>[].</returns>
        public async Task<short[]> ReadWriteMultipleRegistersAsync(int startingAddressRead, int sizeRead, int startingAddressWrite, params short[] values)
            => await _queue.Enqueue(async () => await Task.Run(() => ReadWriteMultipleRegisters(startingAddressRead, sizeRead, startingAddressWrite, values)).ConfigureAwait(false)).ConfigureAwait(false);

        /// <summary>
        /// Write Multiple Coils to a slave as an asynchronous operation (FC15).
        /// </summary>
        /// <param name="startingAddress">First address to be written.</param>
        /// <param name="values">Values to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        public async Task WriteMultipleCoilsAsync(int startingAddress, params bool[] values)
            => await _queue.Enqueue(async () => await Task.Run(() => WriteMultipleCoils(startingAddress, values)).ConfigureAwait(false)).ConfigureAwait(false);

        /// <summary>
        /// Write Multiple Registers to a slave as an asynchronous operation (FC16).
        /// </summary>
        /// <param name="startingAddress">First address to be written.</param>
        /// <param name="values">Values to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        public async Task WriteMultipleRegistersAsync(int startingAddress, params short[] values)
            => await _queue.Enqueue(async () => await Task.Run(() => WriteMultipleRegisters(startingAddress, values)).ConfigureAwait(false)).ConfigureAwait(false);

        /// <summary>
        /// Write Single Coil to a slave as an asynchronous operation (FC05).
        /// </summary>
        /// <param name="startingAddress">Address to be written.</param>
        /// <param name="value">A single value to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        public async Task WriteSingleCoilAsync(int startingAddress, bool value)
            => await _queue.Enqueue(async () => await Task.Run(() => WriteSingleCoil(startingAddress, value)).ConfigureAwait(false)).ConfigureAwait(false);

        /// <summary>
        /// Write Single Register to a slave as an asynchronous operation (FC06).
        /// </summary>
        /// <param name="startingAddress">First address to be written.</param>
        /// <param name="value">A value to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        public async Task WriteSingleRegisterAsync(int startingAddress, short value)
            => await _queue.Enqueue(async () => await Task.Run(() => WriteSingleRegister(startingAddress, value)).ConfigureAwait(false)).ConfigureAwait(false);

    }
}

