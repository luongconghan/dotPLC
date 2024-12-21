using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotPLC.Initial
{
    public interface IModbusFuntionCode
    {
        
        /// <summary>
        /// Read Coils from a slave (FC01).
        /// </summary>
        /// <param name="startingAddress">First address to be read.</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returned <see cref="bool"/>[] values.</returns>
        bool[] ReadCoils(int startingAddress, int size);

        /// <summary>
        /// Read Discrete Inputs from a slave (FC02).
        /// </summary>
        /// <param name="startingAddress">First address to be read.</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returned <see cref="bool"/>[] values.</returns>
        bool[] ReadDiscreteInputs(int startingAddress, int size);

        /// <summary>
        /// Read Holding Registers from a slave (FC03).
        /// </summary>
        /// <param name="startingAddress">First address to be read.</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returned <see cref="short"/>[] values.</returns>
        short[] ReadHoldingRegisters(int startingAddress, int size);

        /// <summary>
        /// Read Input Registers from a slave (FC04).
        /// </summary>
        /// <param name="startingAddress">First address to be read.</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returned <see cref="short"/>[] values.</returns>
        short[] ReadInputRegisters(int startingAddress, int size);

        /// <summary>
        /// Write Single Coil to a slave (FC05).
        /// </summary>
        /// <param name="startingAddress">Address to be written.</param>
        /// <param name="value">A single value to be written.</param>
        void WriteSingleCoil(int startingAddress, bool value);

        /// <summary>
        /// Write Single Register to a slave (FC06).
        /// </summary>
        /// <param name="startingAddress">First address to be written.</param>
        /// <param name="value">A value to be written.</param>
        void WriteSingleRegister(int startingAddress, short value);

        /// <summary>
        /// Write Multiple Coils to a slave (FC15).
        /// </summary>
        /// <param name="startingAddress">First address to be written.</param>
        /// <param name="values">Values to be written.</param>
        void WriteMultipleCoils(int startingAddress, bool[] values);

        /// <summary>
        /// Write Multiple Registers to a slave (FC16).
        /// </summary>
        /// <param name="startingAddress">First address to be written.</param>
        /// <param name="values">Values to be written.</param>
        void WriteMultipleRegisters(int startingAddress, short[] values);

        /// <summary>
        /// Read/Write Multiple Registers to a slave (FC23).
        /// </summary>
        /// <param name="startingAddressRead">First address to be read.</param>
        /// <param name="sizeRead">Number of values to be read.</param>
        /// <param name="startingAddressWrite">First address to be written.</param>
        /// <param name="values">Values to be written.</param>
        /// <returns>Returned <see cref="short"/>[] values.</returns>
        short[] ReadWriteMultipleRegisters(int startingAddressRead, int sizeRead, int startingAddressWrite, short[] values);

    }
}
