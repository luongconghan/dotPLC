
using dotPLC.Initial;
using dotPLC.Mitsubishi.Exceptions;
using System;
using System.Text.RegularExpressions;

namespace dotPLC.Mitsubishi.Types
{
    /// <summary>
    /// Implements a <see cref="Float"/> [single-precision 32-bit] device
    /// </summary>
    public class Float
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
        /// Clear space.
        /// </summary>
        static readonly Regex sWhitespace = new Regex(@"\s+");
        /// <summary>
        /// Initializes a new instance of <see cref="Float"></see> [single-precision 32-bit] device.
        /// </summary>
        public Float()
        {
        }
        /// <summary>
        /// Initializes a new instance of <see cref="Float"></see> [single-precision 32-bit] device which determines label name.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        public Float(string label)
        {
            if (label == null || label == "")
                throw new InvalidDeviceLabelNameException("The label name of device is invalid.",nameof(label));
            label = sWhitespace.Replace(label, "").ToUpper();
            string device;
            int num;
            Ethernet.SettingDevice(label, out device, out num);
            _label = label;
            _device = device;
            _index = num;
        }
        /// <summary>
        /// Initializes a new instance of <see cref="Float"></see> [single-precision 32-bit] device which determines label name and value.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">[Single-precision 32-bit] real number.</param>
        public Float(string label, float value)
        {
            if (label == null || label == "")
                throw new InvalidDeviceLabelNameException("The label name of device is invalid.",nameof(label));
            label = sWhitespace.Replace(label, "").ToUpper();
            string device;
            int num;
            Ethernet.SettingDevice(label, out device, out num);
            _label = label;
            _device = device;
            _index = num;
            Value = value;
        }
        /// <summary>
        /// Initializes a new instance of <see cref="Float"></see> [single-precision 32-bit] device which determines device, index and value.
        /// </summary>
        /// <param name="device">device</param>
        /// <param name="index">index</param>
        /// <param name="value">value</param>
        protected internal Float(string device, int index, float value)
        {
            Ethernet.SettingLabel(device, index, out _label);
            _device = device;
            _index = index;
            Value = value;
        }
        /// <summary>
        /// Initializes a new instance of <see cref="Float"></see> [single-precision 32-bit] device which determines device and index.
        /// </summary>
        /// <param name="device">device</param>
        /// <param name="index">index</param>
        protected internal Float(string device, int index)
        {
            Ethernet.SettingLabel(device, index, out _label);
            _device = device;
            _index = index;
        }
        /// <summary>
        /// Gets or sets label name of device. (EX: D0, Y2, M10, etc.)
        /// </summary>
        public string Label
        {
            get => _label;
            set
            {
                if (value == null || value == "")
                    throw new InvalidDeviceLabelNameException("The label name of device is invalid.", nameof(Label));
                string label_temp = sWhitespace.Replace(value, "").ToUpper();
                string device;
                int num;
                Ethernet.SettingDevice(label_temp, out device, out num);
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
                Ethernet.SettingLabel(_device, value, out _label);
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
                Ethernet.SettingLabel(value, _index, out _label);
                _device = value;
            }
        }
        /// <summary>
        /// Gets or sets [single-precision 32-bit] value of device.
        /// </summary>
        public float Value { get; set; }
    }
}
