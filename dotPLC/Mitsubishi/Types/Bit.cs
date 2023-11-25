
using dotPLC.Initial;
using System;
using System.Text.RegularExpressions;

namespace dotPLC.Mitsubishi.Types
{
    /// <summary>
    /// Implements a <see cref="Bit"/> device.
    /// </summary>
    public class Bit
    {
        /// <summary>
        /// Initializes a new instance of <see cref="Bit"></see> device.
        /// </summary>
        public Bit()
        {
        }
        /// <summary>
        /// Initializes a new instance of <see cref="Bit"></see> device which determines label name.
        /// </summary>
        /// <param name="label">Label name. (EX: M0,Y2...)</param>
        public Bit(string label)
        {
            label = sWhitespace.Replace(label, "").ToUpper();
            string device;
            int num;
            if (!Ethernet.SettingDevice(label, out device, out num))
                return;
            _label = label;
            _device = device;
            _index = num;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Bit"></see> device which determines label name and value.
        /// </summary>
        /// <param name="label">Label name. (EX: M0,Y2...)</param>
        /// <param name="value">True or false.</param>
        public Bit(string label, bool value)
        {
            label = sWhitespace.Replace(label, "").ToUpper();
            string device;
            int num;
            if (!Ethernet.SettingDevice(label, out device, out num))
                throw new ArgumentOutOfRangeException("The specified device does not belong to the memory of the PLC");
            _label = label;
            Value = value;
            _device = device;
            _index = num;
        }
        /// <summary>
        /// Initializes a new instance of <see cref="Bit"></see> device which determines device, index and value.
        /// </summary>
        /// <param name="device">Device name. (EX: X,M,Y...)</param>
        /// <param name="index">Index of device</param>
        /// <param name="value">True or false.</param>
        internal Bit(string device, int index, bool value)
        {
            device = sWhitespace.Replace(device, "").ToUpper();
            if (!Ethernet.SettingLabel(device, index, out _label))
                throw new ArgumentOutOfRangeException("The specified device does not belong to the memory of the PLC");
            _device = device;
            _index = index;
            Value = value;
        }
        /// <summary>
        /// Initializes a new instance of <see cref="Bit"></see> device which determines device, index and value.
        /// </summary>
        /// <param name="device">Device name. (EX: X,M,Y...)</param>
        /// <param name="index">Index of device</param>
        internal Bit(string device, int index)
        {
            device = sWhitespace.Replace(device, "").ToUpper();
            if (!Ethernet.SettingLabel(device, index, out _label))
                return;
            _device = device;
            _index = index;
        }

        /// <summary>
        /// Gets or sets label name of device.
        /// </summary>
        public string Label
        {
            get => _label;
            set
            {
                string label_temp = sWhitespace.Replace(value, "").ToUpper();
                string device;
                int num;
                if (!Ethernet.SettingDevice(label_temp, out device, out num))
                    throw new ArgumentOutOfRangeException("The specified device does not belong to the memory of the PLC");
                _device = device;
                _index = num;
                _label = label_temp;
            }
        }

        /// <summary>
        /// Gets or sets index of device.
        /// </summary>
        protected internal int Index
        {
            get => _index;
            set
            {
                if (!Ethernet.SettingLabel(_device, value, out _label))
                    return;
                _index = value;
            }
        }
        /// <summary>
        /// Gets or sets device name.
        /// </summary>
        protected internal string Device
        {
            get => _device;
            set
            {
                string device_temp = sWhitespace.Replace(value, "").ToUpper();
                if (!Ethernet.SettingLabel(device_temp, _index, out _label))
                    return;
                _device = device_temp;
            }
        }
        /// <summary>
        /// Gets or sets value of device.
        /// </summary>
        public bool Value { get; set; }

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
    }
}
