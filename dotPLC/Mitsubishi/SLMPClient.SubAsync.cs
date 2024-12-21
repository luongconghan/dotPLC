using dotPLC.Initial;
using dotPLC.Mitsubishi.Exceptions;
using dotPLC.Mitsubishi.Types;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace dotPLC.Mitsubishi
{
    public partial class SLMPClient
    {
        #region SubAsync Method
        /// <summary>
        /// Write single value to the server as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">A single value to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        private async Task WriteDeviceSubAsync(string label, bool value)
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
            await StreamDataAsync(22, 11).ConfigureAwait(false);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            await StreamDataAsync(0, 20 - 11).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// Write single value to the server as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">A single value to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        private async Task WriteDeviceSubAsync(string label, short value)
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
            byte[] data = BitConverter.GetBytes(value);
            SendBuffer[21] = data[0];
            SendBuffer[22] = data[1];
            await StreamDataAsync(23, 11).ConfigureAwait(false);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            await StreamDataAsync(0, 20 - 11).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// Write single value to the server as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">A single value to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        private async Task WriteDeviceSubAsync(string label, int value)
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
            byte[] data = BitConverter.GetBytes(value);
            for (int i = 0; i < 4; ++i)
                SendBuffer[21 + i] = data[i];
            await StreamDataAsync(25, 11).ConfigureAwait(false);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            await StreamDataAsync(0, 20 - 11).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// Write single value to the server as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">A single value to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        private async Task WriteDeviceSubAsync(string label, float value)
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
            byte[] data = BitConverter.GetBytes(value);
            for (int i = 0; i < 4; ++i)
                SendBuffer[21 + i] = data[i];
            await StreamDataAsync(25, 11).ConfigureAwait(false);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            await StreamDataAsync(0, 20 - 11).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// Write multiple values to the server in a batch as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="values">Values to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        private async Task WriteDeviceBlockSubAsync(string label, bool[] values)
        {
            byte[] num_of_size = BitConverter.GetBytes(values.Length);
            byte[] byte_of_coils = ConvertBoolArrayToByteArraySLMP(values);
            byte[] dataLenght = BitConverter.GetBytes(12 + byte_of_coils.Length);
            SendBuffer[7] = dataLenght[0];
            SendBuffer[8] = dataLenght[1];
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x14;
            SendBuffer[13] = 0x01;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            SendBuffer[19] = num_of_size[0];
            SendBuffer[20] = num_of_size[1];
            for (int i = 0; i < byte_of_coils.Length; ++i)
                SendBuffer[21 + i] = byte_of_coils[i];
            await StreamDataAsync(21 + byte_of_coils.Length, 11).ConfigureAwait(false);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            await StreamDataAsync(0, 20 - 11).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// Write multiple values to the server in a batch as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="values">Values to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        private async Task WriteDeviceBlockSubAsync(string label, short[] values)
        {
            int datalength = values != null ? values.Length : throw new ArgumentNullException(nameof(values), "Array data must be non-null");
            byte[] datalenght_bytes = BitConverter.GetBytes(12 + datalength * 2);
            SendBuffer[7] = datalenght_bytes[0];
            SendBuffer[8] = datalenght_bytes[1];
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
            await StreamDataAsync(21 + datalength * 2, 11).ConfigureAwait(false);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            await StreamDataAsync(0, 20 - 11).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// Write multiple values to the server in a batch as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="values">Values to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        private async Task WriteDeviceBlockSubAsync(string label, int[] values)
        {
            int datalength = values != null ? values.Length : throw new ArgumentNullException(nameof(values), "Array data must be non-null");
            byte[] datalenght_bytes = BitConverter.GetBytes(12 + datalength * 4);
            SendBuffer[7] = datalenght_bytes[0];
            SendBuffer[8] = datalenght_bytes[1];
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
            await StreamDataAsync(21 + datalength * 4, 11).ConfigureAwait(false);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            await StreamDataAsync(0, 20 - 11).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// Write multiple values to the server in a batch as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="values">Values to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        private async Task WriteDeviceBlockSubAsync(string label, float[] values)
        {
            int size = values != null ? values.Length : throw new ArgumentNullException(nameof(values), "Array data must be non-null");
            byte[] dataLenght = BitConverter.GetBytes(12 + size * 4);
            SendBuffer[7] = dataLenght[0];
            SendBuffer[8] = dataLenght[1];
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x14;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            byte[] num_of_size = BitConverter.GetBytes(size * 2);
            SendBuffer[19] = num_of_size[0];
            SendBuffer[20] = num_of_size[1];
            int k = 0;
            for (int i = 0; i < size; ++i)
            {
                byte[] float_to_byte = BitConverter.GetBytes(values[i]);
                for (int j = 0; j < 4; ++j)
                {
                    SendBuffer[21 + k] = float_to_byte[j];
                    ++k;
                }
            }
            await StreamDataAsync(21 + size * 4, 11).ConfigureAwait(false);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            await StreamDataAsync(0, 20 - 11).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// Write multiple values to the server randomly as an asynchronous operation.
        /// </summary>
        /// <param name="bits"><see cref="dotPLC.Mitsubishi.Types.Bit"/> values to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        private async Task WriteDeviceRandomSubAsync(params Bit[] bits)
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
            int index = 16;
            for (int i = 0; i < datalength; ++i)
            {
                if (bits[i] == null)
                    throw new ArgumentNullException(nameof(bits), string.Format("bits[{0}] is null.", i));
                SettingDevice(bits[i].Label, out SendBuffer[index + 3], out SendBuffer[index], out SendBuffer[index + 1], out SendBuffer[index + 2]);
                if (bits[i].Value)
                    SendBuffer[index + 4] = 0x01;
                else
                    SendBuffer[index + 4] = 0x00;
                index += 5;
            }
            int total_byte = 16 + datalength * 5;
            await StreamDataAsync(total_byte, 11).ConfigureAwait(false);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            await StreamDataAsync(0, 20 - 11).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// Write multiple values to the server randomly as an asynchronous operation.
        /// </summary>
        /// <param name="words"><see cref="dotPLC.Mitsubishi.Types.Word"/> values to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        private async Task WriteDeviceRandomSubAsync(params Word[] words)
        {
            if (words == null)
                throw new ArgumentNullException(nameof(words));
            int datalength = words.Length;
            if (datalength < 1 || datalength > 160) //Page57 (SLMP-Mitsubishi.PDF)
                throw new SizeOutOfRangeException("Size must be 1 to 160 points.", nameof(words));
            byte[] datalength_bytes = BitConverter.GetBytes(8 + datalength * 6);
            SendBuffer[7] = datalength_bytes[0];
            SendBuffer[8] = datalength_bytes[1];
            SendBuffer[11] = 0x02;
            SendBuffer[12] = 0x14;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SendBuffer[15] = (byte)datalength;
            SendBuffer[16] = 0x00;
            int index_W = 17;
            int index_DW = 17 + datalength * 6;
            for (int i = 0; i < datalength; ++i)
            {
                if (words[i] == null)
                    throw new ArgumentNullException(nameof(words), string.Format("words[{0}] is null.", i));
                SettingDevice(words[i].Label, out SendBuffer[index_W + 3], out SendBuffer[index_W], out SendBuffer[index_W + 1], out SendBuffer[index_W + 2]);
                byte[] value_w = BitConverter.GetBytes(words[i].Value);
                SendBuffer[index_W + 4] = value_w[0];
                SendBuffer[index_W + 5] = value_w[1];
                index_W += 6;
            }
            int total_byte = 17 + datalength * 6;
            await StreamDataAsync(total_byte, 11).ConfigureAwait(false);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            await StreamDataAsync(0, 20 - 11).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// Write multiple values to the server randomly as an asynchronous operation.
        /// </summary>
        /// <param name="dwords"><see cref="dotPLC.Mitsubishi.Types.DWord"/> values to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        private async Task WriteDeviceRandomSubAsync(params DWord[] dwords)
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
            int total_byte = 17 + datalength * 8;
            await StreamDataAsync(total_byte, 11).ConfigureAwait(false);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            await StreamDataAsync(0, 20 - 11).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// Write multiple values to the server randomly as an asynchronous operation.
        /// </summary>
        /// <param name="floats"><see cref="dotPLC.Mitsubishi.Types.Float"/> values to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        private async Task WriteDeviceRandomSubAsync(params Float[] floats)
        {
            if (floats == null)
                throw new ArgumentNullException(nameof(floats));
            int datalength = floats.Length;
            if (datalength < 1 || datalength > 137) //Page57 (SLMP-Mitsubishi.PDF)
                throw new SizeOutOfRangeException("Size must be 1 to 137 points.", nameof(floats));
            byte[] datalenght_bytes = BitConverter.GetBytes(8 + datalength * 8);
            SendBuffer[7] = datalenght_bytes[0];
            SendBuffer[8] = datalenght_bytes[1];
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
            int total_byte = 17 + datalength * 8;
            await StreamDataAsync(total_byte, 11).ConfigureAwait(false);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            await StreamDataAsync(0, 20 - 11).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// Write multiple values to the server randomly as an asynchronous operation.
        /// </summary>
        /// <param name="words"><see cref="dotPLC.Mitsubishi.Types.Word"/> values to be written.</param>
        /// <param name="dwords"><see cref="dotPLC.Mitsubishi.Types.DWord"/> values to be written.</param>
        /// <param name="floats"><see cref="dotPLC.Mitsubishi.Types.Float"/> values to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        private async Task WriteDeviceRandomSubAsync(Word[] words, DWord[] dwords, Float[] floats)
        {
            if (words == null && dwords == null && floats == null)
                return;

            int num_W = words == null ? 0 : words.Length;
            int num_DW = dwords == null ? 0 : dwords.Length;
            int num_FL = floats == null ? 0 : floats.Length;
            int size = num_W * 12 + (num_DW + num_FL) * 14;
            if (size < 1 || size > 1920) //Page57 (SLMP-Mitsubishi.PDF)
                throw new SizeOutOfRangeException("Size must be 1 to 1920.\n[size \u2264 word points x 12 + (double points + float points) x 14 \u2264 1920]");
            byte[] datalength = BitConverter.GetBytes(8 + num_W * 6 + (num_DW + num_FL) * 8);
            SendBuffer[7] = datalength[0];
            SendBuffer[8] = datalength[1];
            SendBuffer[11] = 0x02;
            SendBuffer[12] = 0x14;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SendBuffer[15] = (byte)num_W;
            SendBuffer[16] = (byte)(num_DW + num_FL);
            int index_W = 17;
            int index_DW = 17 + num_W * 6;
            int index_FL = 17 + num_W * 6 + num_DW * 8;
            if (num_W > 0)
            {
                for (int i = 0; i < num_W; ++i)
                {
                    if (words[i] == null)
                        throw new ArgumentNullException(nameof(words), string.Format("words[{0}] is null.", i));
                    SettingDevice(words[i].Label, out SendBuffer[index_W + 3], out SendBuffer[index_W], out SendBuffer[index_W + 1], out SendBuffer[index_W + 2]);
                    byte[] value_w = BitConverter.GetBytes(words[i].Value);
                    SendBuffer[index_W + 4] = value_w[0];
                    SendBuffer[index_W + 5] = value_w[1];
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
                    byte[] value_w = BitConverter.GetBytes(dwords[i].Value);
                    SendBuffer[index_DW + 4] = value_w[0];
                    SendBuffer[index_DW + 5] = value_w[1];
                    SendBuffer[index_DW + 6] = value_w[2];
                    SendBuffer[index_DW + 7] = value_w[3];
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
                    byte[] value_w = BitConverter.GetBytes(floats[i].Value);
                    SendBuffer[index_FL + 4] = value_w[0];
                    SendBuffer[index_FL + 5] = value_w[1];
                    SendBuffer[index_FL + 6] = value_w[2];
                    SendBuffer[index_FL + 7] = value_w[3];
                    index_FL += 8;
                }
            }
            int total_byte = 17 + num_W * 6 + num_DW * 8 + num_FL * 8;
            await StreamDataAsync(total_byte, 11).ConfigureAwait(false);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            await StreamDataAsync(0, 20 - 11).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// Write text to the server as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="text">Text to be written.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        private async Task WriteTextSubAsync(string label, string text)
        {
            if (text == null || text == "")
                throw new ArgumentNullException(nameof(text));
            if (text.Length < 1 || text.Length > 960) //Page57 (SLMP-Mitsubishi.PDF)
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
                await StreamDataAsync(21, 13);//Đọc thanh ghi cuối
                if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
                {
                    int errorCode_Read = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                    Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode_Read));
                    await StreamDataAsync(0, 20 - 11).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
                    return;
                }
                text += Convert.ToChar(ReceveiBuffer[12]).ToString();
            }
            int length = text.Length;
            byte[] datalenght_bytes = BitConverter.GetBytes(12 + length * 2);
            SendBuffer[7] = datalenght_bytes[0];
            SendBuffer[8] = datalenght_bytes[1];
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
            await StreamDataAsync(21 + length, 11).ConfigureAwait(false);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            await StreamDataAsync(0, 20 - 11).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// Read a single value from the server as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// <see href="TResult"/> is <see cref="bool"/></returns>
        private async Task<bool> ReadSingleCoilSubAsync(string label)
        {
            bool coil = false;
            SendBuffer[7] = 0x0C;
            SendBuffer[8] = 0x00;
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x04;
            SendBuffer[13] = 0x01;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            SendBuffer[19] = 0x01;
            SendBuffer[20] = 0x00;
            await StreamDataAsync(21, 12).ConfigureAwait(false);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                await StreamDataAsync(0, 20 - 12).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
            }
            else
                coil = ReceveiBuffer[11] == 0x10;
            return coil;
        }
        /// <summary>
        /// Read a single value from the server as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// <see href="TResult"/> is <see cref="short"/></returns>
        private async Task<short> ReadRegisterSubAsync(string label)
        {
            short value = 0;
            SendBuffer[7] = 0x0C;
            SendBuffer[8] = 0x00;
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x04;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            SendBuffer[19] = 0x01;
            SendBuffer[20] = 0x00;
            await StreamDataAsync(21, 13).ConfigureAwait(false);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                await StreamDataAsync(0, 20 - 13).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
            }
            else
                value = BitConverter.ToInt16(ReceveiBuffer, 11);
            return value;
        }
        /// <summary>
        /// Read a single value from the server as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// <see href="TResult"/> is <see cref="int"/></returns>
        private async Task<int> ReadDoubleSubAsync(string label)
        {
            int value = 0;
            SendBuffer[7] = 0x0C;
            SendBuffer[8] = 0x00;
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x04;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            SendBuffer[19] = 0x02;
            SendBuffer[20] = 0x00;
            await StreamDataAsync(21, 15).ConfigureAwait(false);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                await StreamDataAsync(0, 20 - 15).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
            }
            else
                value = BitConverter.ToInt32(ReceveiBuffer, 11);
            return value;
        }
        /// <summary>
        /// Read a single value from the server as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// <see href="TResult"/> is <see cref="float"/></returns>
        private async Task<float> ReadFloatSubAsync(string label)
        {
            float value = 0.0f;
            SendBuffer[7] = 0x0C;
            SendBuffer[8] = 0x00;
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x04;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            SendBuffer[19] = 0x02;
            SendBuffer[20] = 0x00;
            await StreamDataAsync(21, 15).ConfigureAwait(false);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                await StreamDataAsync(0, 20 - 15).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
            }
            else
                value = BitConverter.ToSingle(ReceveiBuffer, 11);
            return value;
        }
        /// <summary>
        /// Read multiple values from the server in a batch as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// <see href="TResult"></see> is <see cref="bool"></see>[].</returns>
        private async Task<bool[]> ReadMultipleCoilsSubAsync(string label, int size)
        {
            SendBuffer[7] = 0x0C;
            SendBuffer[8] = 0x00;
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x04;
            SendBuffer[13] = 0x01;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            byte[] num_of_size = BitConverter.GetBytes(size);
            SendBuffer[19] = num_of_size[0];
            SendBuffer[20] = num_of_size[1];
            int num_of_byte = size % 2 == 0 ? size / 2 : size / 2 + 1;
            await StreamDataAsync(21, 11 + num_of_byte).ConfigureAwait(false);
            bool[] coils = new bool[size];
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                if (11 + num_of_byte < 20)
                    await StreamDataAsync(0, 20 - (11 + num_of_byte)).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
            }
            else
                coils = ConvertByteArrayToBoolArray(ReceveiBuffer, 11, size);
            return coils;
        }
        /// <summary>
        /// Read multiple values from the server in a batch as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// <see href="TResult"></see> is <see cref="short"></see>[].</returns>
        private async Task<short[]> ReadRegistersAsync(string label, int size)
        {
            short[] values = new short[size];
            SendBuffer[7] = 0x0C;
            SendBuffer[8] = 0x00;
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x04;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            byte[] num_of_size = BitConverter.GetBytes(size);
            SendBuffer[19] = num_of_size[0];
            SendBuffer[20] = num_of_size[1];
            await StreamDataAsync(21, 11 + size * 2).ConfigureAwait(false);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                if (11 + size * 2 < 20)
                    await StreamDataAsync(0, 20 - (11 + size * 2)).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
            }
            else
            {
                for (int i = 0; i < size * 2; i += 2)
                    values[i / 2] = BitConverter.ToInt16(ReceveiBuffer, 11 + i);
            }
            return values;
        }
        /// <summary>
        /// Read multiple values from the server in a batch as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// <see href="TResult"></see> is <see cref="int"></see>[].</returns>
        private async Task<int[]> ReadMultipleDoubleSubAsync(string label, int size)
        {
            int[] values = new int[size];
            SendBuffer[7] = 0x0C;
            SendBuffer[8] = 0x00;
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x04;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            byte[] num_of_size = BitConverter.GetBytes(size * 2);
            SendBuffer[19] = num_of_size[0];
            SendBuffer[20] = num_of_size[1];
            await StreamDataAsync(21, 11 + size * 4).ConfigureAwait(false);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                if (11 + size * 4 < 20)
                    await StreamDataAsync(0, 20 - (11 + size * 4)).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
            }
            else
            {
                for (int i = 0; i < size * 4; i += 4)
                    values[i / 4] = BitConverter.ToInt32(ReceveiBuffer, 11 + i);
            }
            return values;
        }
        /// <summary>
        /// Read multiple values from the server in a batch as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// <see href="TResult"></see> is <see cref="float"></see>[].</returns>
        private async Task<float[]> ReadFloatsAsync(string label, int size)
        {
           
            float[] values = new float[size];
            SendBuffer[7] = 0x0C;
            SendBuffer[8] = 0x00;
            SendBuffer[11] = 0x01;
            SendBuffer[12] = 0x04;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            SettingDevice(label, out SendBuffer[18], out SendBuffer[15], out SendBuffer[16], out SendBuffer[17]);
            byte[] num_of_size = BitConverter.GetBytes(size * 2);
            SendBuffer[19] = num_of_size[0];
            SendBuffer[20] = num_of_size[1];
            await StreamDataAsync(21, 11 + size * 4).ConfigureAwait(false);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                if (11 + size * 4 < 20)
                    await StreamDataAsync(0, 20 - (11 + size * 4)).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
            }
            else
            {
                for (int i = 0; i < size * 4; i += 4)
                    values[i / 4] = BitConverter.ToSingle(ReceveiBuffer, 11 + i);
            }
            return values;
        }
        /// <summary>
        /// Read multiple values from the server randomly as an asynchronous operation.
        /// </summary>
        /// <param name="bits"><see cref="dotPLC.Mitsubishi.Types.Bit"/> values to be read.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        private async Task ReadRandomSubAsync(params Bit[] bits)
        {
            if (bits == null)
                return;
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
            await StreamDataAsync(17 + count * 4, 11 + count * 2).ConfigureAwait(false);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                if (11 + count * 2 < 20)
                    await StreamDataAsync(0, 20 - (11 + count * 2)).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
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
        /// Read multiple values from the server randomly as an asynchronous operation.
        /// </summary>
        /// <param name="words"><see cref="dotPLC.Mitsubishi.Types.Word"/> values to be read.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        private async Task ReadRandomSubAsync(params Word[] words)
        {
            if (words == null)
                throw new ArgumentNullException(nameof(words));
            int length = words.Length;
            if (length < 1 || length > 192) //Page57 (SLMP-Mitsubishi.PDF)
                throw new SizeOutOfRangeException("Size must be 1 to 192 points.", nameof(words));
            byte[] datalength = BitConverter.GetBytes(8 + length * 4);
            SendBuffer[7] = datalength[0];
            SendBuffer[8] = datalength[1];
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
            int total_byte = 17 + length * 4;
            int total_byte_recv = 11 + length * 2;
            await StreamDataAsync(total_byte, total_byte_recv).ConfigureAwait(false);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                if (total_byte_recv < 20)
                    await StreamDataAsync(0, 20 - total_byte_recv).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
            }
            else
            {
                for (int i = 0; i < length * 2; i += 2)
                    words[i / 2].Value = BitConverter.ToInt16(ReceveiBuffer, 11 + i);
            }
        }
        /// <summary>
        /// Read multiple values from the server randomly as an asynchronous operation.
        /// </summary>
        /// <param name="dwords"><see cref="dotPLC.Mitsubishi.Types.DWord"/> values to be read.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        private async Task ReadRandomSubAsync(params DWord[] dwords)
        {
            if (dwords == null)
                throw new ArgumentNullException(nameof(dwords));
            int length = dwords.Length;
            if (length < 1 || length > 192) //Page57 (SLMP-Mitsubishi.PDF)
                throw new SizeOutOfRangeException("Size must be 1 to 192 points.", nameof(dwords));
            byte[] datalength = BitConverter.GetBytes(8 + length * 4);
            SendBuffer[7] = datalength[0];
            SendBuffer[8] = datalength[1];
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
            int total_byte = 17 + length * 4;
            int total_byte_recv = 11 + length * 4;
            await StreamDataAsync(total_byte, total_byte_recv).ConfigureAwait(false);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                if (total_byte_recv < 20)
                    await StreamDataAsync(0, 20 - total_byte_recv).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
            }
            else
            {
                for (int i = 0; i < length * 4; i += 4)
                    dwords[i / 4].Value = BitConverter.ToInt32(ReceveiBuffer, 11 + i);
            }
        }
        /// <summary>
        /// Read multiple values from the server randomly as an asynchronous operation.
        /// </summary>
        /// <param name="floats"><see cref="dotPLC.Mitsubishi.Types.Float"/> values to be read.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        private async Task ReadRandomSubAsync(params Float[] floats)
        {
            if (floats == null)
                throw new ArgumentNullException(nameof(floats));
            int length = floats.Length;
            if (length < 1 || length > 192) //Page57 (SLMP-Mitsubishi.PDF)
                throw new SizeOutOfRangeException("Size must be 1 to 192 points.", nameof(floats));
            byte[] datalength = BitConverter.GetBytes(8 + length * 4);
            SendBuffer[7] = datalength[0];
            SendBuffer[8] = datalength[1];
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
            int total_byte = 17 + length * 4;
            int total_byte_recv = 11 + length * 4;
            await StreamDataAsync(total_byte, total_byte_recv).ConfigureAwait(false);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                if (total_byte_recv < 20)
                    await StreamDataAsync(0, 20 - total_byte_recv).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
            }
            else
            {
                for (int i = 0; i < length * 4; i += 4)
                    floats[i / 4].Value = BitConverter.ToSingle(ReceveiBuffer, 11 + i);
            }
        }
        /// <summary>
        /// Read multiple values from the server randomly as an asynchronous operation.
        /// </summary>
        /// <param name="bits"><see cref="dotPLC.Mitsubishi.Types.Bit"/> values to be read.</param>
        /// <param name="words"><see cref="dotPLC.Mitsubishi.Types.Word"/> values to be read.</param>
        /// <param name="dwords"><see cref="dotPLC.Mitsubishi.Types.DWord"/> values to be read.</param>
        /// <param name="floats"><see cref="dotPLC.Mitsubishi.Types.Float"/> values to be read.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        private async Task ReadRandomSubAsync(Bit[] bits, Word[] words, DWord[] dwords, Float[] floats)
        {
            Labels.Clear();
            dsWordBaseBits.Clear();
            if (bits != null)
            {
                for (int index = 0; index < bits.Length; ++index)
                {
                    string device;
                    int num;
                    SettingDevice(bits[index].Label, out device, out num);
                        if (Labels.ContainsKey(device))
                            Labels[device].Add(num);
                        else
                            Labels.Add(device, new List<int>() { num });
                }
                foreach (KeyValuePair<string, List<int>> label in Labels)
                    label.Value.Sort();
                foreach (KeyValuePair<string, List<int>> label in Labels)
                {
                    int num = 0;
                    for (int index = 0; index < label.Value.Count; ++index)
                    {
                        if (index == 0)
                        {
                            num = label.Value[0];
                            var word = new Word(); word.Create(label.Key, label.Value[0]);
                            dsWordBaseBits.Add(word);
                        }
                        else if (label.Value[index] > num + 15)
                        {
                            num = label.Value[index];
                            var word = new Word(); word.Create(label.Key, label.Value[index]);
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
                    SettingDevice(words[i].Label, out SendBuffer[index_W + 3], out SendBuffer[index_W], out SendBuffer[index_W + 1], out SendBuffer[index_W + 2]);
                    index_W += 4;
                }
            }
            if (num_of_DW > 0)
            {
                for (int i = 0; i < num_of_DW; ++i)
                {
                    SettingDevice(dwords[i].Label, out SendBuffer[index_DW + 3], out SendBuffer[index_DW], out SendBuffer[index_DW + 1], out SendBuffer[index_DW + 2]);
                    index_DW += 4;
                }
            }
            if (num_of_FL > 0)
            {
                for (int i = 0; i < num_of_FL; ++i)
                {
                    SettingDevice(floats[i].Label, out SendBuffer[index_FL + 3], out SendBuffer[index_FL], out SendBuffer[index_FL + 1], out SendBuffer[index_FL + 2]);
                    index_FL += 4;
                }
            }
            await StreamDataAsync(17 + (num_of_Bit_base_W + num_of_W + num_of_DW + num_of_FL) * 4, 11 + num_of_Bit_base_W * 2 + num_of_W * 2 + num_of_DW * 4 + num_of_FL * 4).ConfigureAwait(false);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                if (11 + num_of_Bit_base_W * 2 + num_of_W * 2 + num_of_DW * 4 + num_of_FL * 4 < 20)
                    await StreamDataAsync(0, 20 - (11 + num_of_Bit_base_W * 2 + num_of_W * 2 + num_of_DW * 4 + num_of_FL * 4)).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
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
        /// Read text from the server as an asynchronous operation.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of text to be read.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// Value is text of the specified size.</returns>
        private async Task<string> ReadTextSubAsync(string label, int size)
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
            await StreamDataAsync(21, 11 + size).ConfigureAwait(false);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                if (11 + size < 20)
                    await StreamDataAsync(0, 20 - (11 + size)).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
            }
            else
                str = count % 2 != 0 ? Encoding.ASCII.GetString(ReceveiBuffer, 11, count) : Encoding.ASCII.GetString(ReceveiBuffer, 11, size);
            return str;
        }

        /// <summary>
        /// To test whether the communication function between the client and the server operates normally or not.
        /// </summary>
        /// <param name="loopbackMessage">The order of character strings for up to 960 1-byte characters ("0" to "9", "A" to "F") is sent from the head.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// <see href="TResult"/> is <see cref="bool"/> indicates that true is normal, false is abnormal.</returns>
        private async Task<bool> SelfTestSubAsync(string loopbackMessage)
        {
            if (loopbackMessage == "" || loopbackMessage == null)
                throw new ArgumentNullException(nameof(loopbackMessage), "Data length must be greater than 1 and less than or equal to 960 characters, character codes are allowed (\"0\" to \"9\", \"A\" to \"F\").");
            foreach (char ch in loopbackMessage)
            {
                if ((ch < '0' || ch > '9') && (ch < 'A' || ch > 'F') || loopbackMessage.Length > 960)
                    throw new ArgumentOutOfRangeException(nameof(loopbackMessage), loopbackMessage, "Data length must be greater than 1 and less than or equal to 960 characters, character codes are allowed (\"0\" to \"9\", \"A\" to \"F\").");
            }
            byte[] datalenght_bytes = BitConverter.GetBytes(8 + loopbackMessage.Length);
            SendBuffer[7] = datalenght_bytes[0];
            SendBuffer[8] = datalenght_bytes[1];
            SendBuffer[11] = 0x19;
            SendBuffer[12] = 0x06;
            SendBuffer[13] = 0x00;
            SendBuffer[14] = 0x00;
            byte[] textlength_bytes = BitConverter.GetBytes(loopbackMessage.Length);
            SendBuffer[15] = textlength_bytes[0];
            SendBuffer[16] = textlength_bytes[1];
            byte[] text_bytes = Encoding.ASCII.GetBytes(loopbackMessage);
            Array.Copy(text_bytes, 0, SendBuffer, 17, text_bytes.Length);
            await StreamDataAsync(17 + loopbackMessage.Length, 13 + loopbackMessage.Length).ConfigureAwait(false);
            bool flag;
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                flag = false;
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                if (13 + loopbackMessage.Length < 20)
                    await StreamDataAsync(0, 20 - (13 + loopbackMessage.Length)).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
            }
            else
                flag = ReceveiBuffer[11] == textlength_bytes[0] && ReceveiBuffer[12] == textlength_bytes[1] && loopbackMessage == Encoding.ASCII.GetString(ReceveiBuffer, 13, loopbackMessage.Length);
            return flag;
        }
        /// <summary>
        /// Read the model character string of the server as an asynchronous operation.
        /// </summary>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task{TResult}"></see> represents an asynchronous operation that can return a value.
        /// Value is the model character string of the server.</returns>
        private async Task<string> GetCpuNameSubAsync()
        {
            string name = null;
            Array.Copy(_getCpuName, SendBuffer, _getCpuName.Length);
            await StreamDataAsync(_getCpuName.Length, 512).ConfigureAwait(false);
            if (ReceveiBuffer[9] != 0x00 || ReceveiBuffer[10] != 0x00)
            {
                int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
                Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
                name = "An error occurred when getting the model.";
            }
            else
            {
                name = Encoding.ASCII.GetString(ReceveiBuffer, 11, 16);
            }
            return name;
        }
        /// <summary>
        /// Changes the remote password from unlocked status to locked status as an asynchronous operation. (Communication to the device is disabled.)
        /// </summary>
        /// <param name="password">Specifies a remote password.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        private async Task RemoteLockSubAsync(string password)
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
            byte[] datalenght_bytes = BitConverter.GetBytes(password.Length);
            SendBuffer[15] = datalenght_bytes[0];
            SendBuffer[16] = datalenght_bytes[1];
            byte[] password_bytes = Encoding.ASCII.GetBytes(password);
            Array.Copy(password_bytes, 0, SendBuffer, 17, password_bytes.Length);
            await StreamDataAsync(17 + password.Length, 11).ConfigureAwait(false);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            await StreamDataAsync(0, 20 - 11).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// Changes the remote password from locked status to unlocked status as an asynchronous operation. (Enables communication to the device.)
        /// </summary>
        /// <param name="password">Specifies a remote password.</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        private async Task RemoteUnlockSubAsync(string password)
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
            await StreamDataAsync(17 + password.Length, 11).ConfigureAwait(false);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            await StreamDataAsync(0, 20 - 11).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
        }
        /// <summary>
        /// To perform a remote operation of the server as an asynchronous operation. (EX: RUN/PAUSE/STOP/CLEAR/RESET...)
        /// </summary>
        /// <param name="mode">Specifies a <see cref="dotPLC.Mitsubishi.RemoteControl"></see> mode. (EX: RUN/PAUSE/STOP/CLEAR/RESET...)</param>
        /// <returns>Returns <see cref="System.Threading.Tasks.Task"></see> The task object representing the asynchronous operation.</returns>
        private async Task RemoteControlSubAsync(RemoteControl mode)
        {
            byte[] cmdBuffer = CmdRemoteControl(mode);
            Array.Copy(cmdBuffer, SendBuffer, cmdBuffer.Length);
            await StreamDataAsync(cmdBuffer.Length, 11).ConfigureAwait(false);
            if (ReceveiBuffer[9] == 0x00 && ReceveiBuffer[10] == 0x00)
                return;
            int errorCode = (ReceveiBuffer[10] << 8) + ReceveiBuffer[9];
            Trouble?.Invoke(this, new TroubleshootingEventArgs(errorCode));
            await StreamDataAsync(0, 20 - 11).ConfigureAwait(false); //Khi PLC gửi lỗi (20byte)
        }
        #endregion SubAsync Method
    }
}
