using dotPLC.Initial;
using System;
using System.Text.RegularExpressions;

namespace dotPLC.Mitsubishi.Types
{
    /// <summary>
    /// Implements a <see cref="DWord"/> [32-bit data] device.
    /// </summary>
    public class DWord
    {
        /// <summary>
        /// label
        /// </summary>
        private string _label;
        /// <summary>
        /// index
        /// </summary>
        private int _index;
        /// <summary>
        /// device
        /// </summary>
        private string _device;

        /// <summary>
        /// Initializes a new instance of <see cref="DWord"></see> [32-bit data] device.
        /// </summary>
        public DWord()
        {
        }
        /// <summary>
        /// Initializes a new instance of <see cref="DWord"></see> [32-bit data] device which determines label name.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        public DWord(string label)
        {
            label = sWhitespace.Replace(label, "").ToUpper();
            string device;
            int num;
            if (!Ethernet.SettingDevice(label, out device, out num))
                throw new ArgumentOutOfRangeException("The specified device does not belong to the memory of the PLC");
            _label = label;
            _device = device;
            _index = num;
        }
        /// <summary>
        /// Initializes a new instance of <see cref="DWord"></see> [32-bit data] device which determines label name and [signed 32-bit] value.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">Signed 32-bit data.</param>
        public DWord(string label, int value)
        {
            label = sWhitespace.Replace(label, "").ToUpper();
            string device;
            int num;
            if (!Ethernet.SettingDevice(label, out device, out num))
                throw new ArgumentOutOfRangeException("The specified device does not belong to the memory of the PLC");
            _label = label;
            _device = device;
            _index = num;
            Value = value;
        }
        /// <summary>
        /// Initializes a new instance of <see cref="DWord"></see> [32-bit data] device which determines label name and [unsigned 32-bit] value.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">Unsigned 32-bit data.</param>
        public DWord(string label, uint value)
        {
            label = sWhitespace.Replace(label, "").ToUpper();
            string device;
            int num;
            if (!Ethernet.SettingDevice(label, out device, out num))
                throw new ArgumentOutOfRangeException("The specified device does not belong to the memory of the PLC");
            _label = label;
            _device = device;
            _index = num;
            ValueU = value;
        }
        /// <summary>
        /// Gets or sets label name of device. (EX: D0, Y2, M10, etc.)
        /// </summary>
        public string Label
        {
            get => _label;
            set
            {
                string device;
                string label_temp = sWhitespace.Replace(value, "").ToUpper();
                int num;
                if (!Ethernet.SettingDevice(label_temp, out device, out num))
                    throw new ArgumentOutOfRangeException("The specified device does not belong to the memory of the PLC");
                _device = device;
                _index = num;
                _label = label_temp;
            }
        }
        /// <summary>
        /// index
        /// </summary>
        protected internal int Index
        {
            get => _index;
            set
            {
                if (!Ethernet.SettingLabel(_device, value, out _label))
                    throw new ArgumentOutOfRangeException("The specified device does not belong to the memory of the PLC");
                _index = value;
            }
        }
        /// <summary>
        /// device
        /// </summary>
        protected internal string Device
        {
            get => _device;
            set
            {
                if (!Ethernet.SettingLabel(value, _index, out _label))
                    throw new ArgumentOutOfRangeException("The specified device does not belong to the memory of the PLC");
                _device = value;
            }
        }
        /// <summary>
        /// value
        /// </summary>
        private int _value;
        /// <summary>
        /// value
        /// </summary>
        private uint _valueU;
        /// <summary>
        /// Clear space.
        /// </summary>
        static readonly Regex sWhitespace = new Regex(@"\s+");
        /// <summary>
        /// Gets or sets [signed 32-bit] value of device.
        /// </summary>
        public int Value
        {
            get => _value;
            set
            {
                _value = value;
                _valueU = (uint)value;
            }
        }
        /// <summary>
        /// Gets or sets [unsigned 32-bit] value of device.
        /// </summary>
        public uint ValueU
        {
            get => _valueU;
            set
            {
                _valueU = value;
                _value = (int)value;
            }
        }
    }
}
