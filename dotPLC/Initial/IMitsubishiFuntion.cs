using dotPLC.Mitsubishi.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotPLC.Initial
{
    /// <summary>
    ///  Represents the function of an Mitsubishi communication operation.
    /// </summary>
    public interface IMitsubishiFuntion
    {
        /// <summary>
        /// Write a single value to the server.
        /// </summary>
        /// <typeparam name="T">The data type of value.</typeparam>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="value">A single value to be written.</param>
        void WriteDevice<T>(string label, T value) where T : struct;
        /// <summary>
        /// Write multiple values to the server in a batch. 
        /// </summary>
        /// <typeparam name="T">The data type of value. (EX: <see cref="bool"></see>, <see cref="short"/>, <see cref="float"/>, etc.)</typeparam>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="values">Values to be written.</param>
        void WriteDeviceBlock<T>(string label, params T[] values) where T : struct;
        /// <summary>
        /// Write multiple values to the server randomly.
        /// </summary>
        /// <param name="bits"><see cref="dotPLC.Mitsubishi.Types.Bit"/> values to be written.</param>
        void WriteDeviceRandom(params Bit[] bits);
        /// <summary>
        /// Write multiple values to the server randomly.
        /// </summary>
        /// <param name="words"><see cref="dotPLC.Mitsubishi.Types.Word"/> values to be written.</param>
        void WriteDeviceRandom(params Word[] words);
        /// <summary>
        /// Write multiple values to the server randomly.
        /// </summary>
        /// <param name="dwords"><see cref="dotPLC.Mitsubishi.Types.DWord"/> values to be written.</param>
        void WriteDeviceRandom(params DWord[] dwords);
        /// <summary>
        /// Write multiple values to the server randomly.
        /// </summary>
        /// <param name="floats"><see cref="dotPLC.Mitsubishi.Types.Float"/> values to be written.</param>
        void WriteDeviceRandom(params Float[] floats);
        /// <summary>
        /// Read a single value from the server.
        /// </summary>
        /// <typeparam name="T">The data type of value. (EX: <see cref="bool"></see>, <see cref="short"/>, <see cref="float"/>, etc.)</typeparam>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <returns>Returned <typeparamref name="T"/> value.</returns>
        T ReadDevice<T>(string label) where T : struct;
        /// <summary>
        /// Read multiple values from the server in a batch.
        /// </summary>
        /// <typeparam name="T">The data type of value. (EX: <see cref="bool"></see>, <see cref="short"/>, <see cref="float"/>, etc.)</typeparam>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of values to be read.</param>
        /// <returns>Returned <typeparamref name="T"/>[] values.</returns>
        T[] ReadDeviceBlock<T>(string label, int size) where T : struct;
        /// <summary>
        /// Write text to the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="text">Text to be written.</param>
        void WriteText(string label, string text);
        /// <summary>
        /// Read text from the server.
        /// </summary>
        /// <param name="label">Label name. (EX: D0, Y2, M10, etc.)</param>
        /// <param name="size">Number of text to be read.</param>
        /// <returns>Returns text of the specified size.</returns>
        string ReadText(string label, int size);
        /// <summary>
        /// Read the model character string of the server.
        /// </summary>
        /// <returns>The model character string of the server.</returns>
        string GetCpuName();
        /// <summary>
        /// Read multiple values from the server randomly.
        /// </summary>
        /// <param name="bits"><see cref="dotPLC.Mitsubishi.Types.Bit"/> values to be read.</param>
        void ReadDeviceRandom(params Bit[] bits);
        /// <summary>
        /// Read multiple values from the server randomly.
        /// </summary>
        /// <param name="words"><see cref="dotPLC.Mitsubishi.Types.Word"/> values to be read.</param>
        void ReadDeviceRandom(params Word[] words);
        /// <summary>
        /// Read multiple values from the server randomly.
        /// </summary>
        /// <param name="dwords"><see cref="dotPLC.Mitsubishi.Types.DWord"/> values to be read.</param>
        void ReadDeviceRandom(params DWord[] dwords);
        /// <summary>
        /// Read multiple values from the server randomly.
        /// </summary>
        /// <param name="floats"><see cref="dotPLC.Mitsubishi.Types.Float"/> values to be read.</param>
        void ReadDeviceRandom(params Float[] floats);
        /// <summary>
        /// Read multiple values from the server randomly. <see langword="[RECOMMENDED]"></see>
        /// </summary>
        /// <param name="bits"><see cref="dotPLC.Mitsubishi.Types.Bit"/> values to be read.</param>
        /// <param name="words"><see cref="dotPLC.Mitsubishi.Types.Word"/> values to be read.</param>
        /// <param name="dwords"><see cref="dotPLC.Mitsubishi.Types.DWord"/> values to be read.</param>
        /// <param name="floats"><see cref="dotPLC.Mitsubishi.Types.Float"/> values to be read.</param>
        void ReadDeviceRandom(Bit[] bits = null, Word[] words = null, DWord[] dwords = null, Float[] floats = null);
    }
}
