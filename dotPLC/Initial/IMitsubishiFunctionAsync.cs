using dotPLC.Mitsubishi.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotPLC.Initial
{
    public interface IMitsubishiFunctionAsync
    {
        Task WriteDeviceAsync<T>(string label, T value) where T : struct;
        Task WriteDeviceBlockAsync<T>(string label, params T[] values) where T : struct;
        Task WriteDeviceRandomAsync(params Bit[] bits);
        Task WriteDeviceRandomAsync(params Word[] words);
        Task WriteDeviceRandomAsync(params DWord[] dwords);
        Task WriteDeviceRandomAsync(params Float[] floats);
        Task WriteDeviceRandomAsync(Word[] words = null, DWord[] dwords = null, Float[] floats = null);
        Task<T> ReadDeviceAsync<T>(string label) where T : struct;
        Task<T[]> ReadDeviceBlockAsync<T>(string label, int size) where T : struct;
        Task ReadDeviceRandomAsync(params Bit[] bits);
        Task ReadDeviceRandomAsync(params Word[] words);
        Task ReadDeviceRandomAsync(params DWord[] dwords);
        Task ReadDeviceRandomAsync(params Float[] floats);
        Task ReadDeviceRandomAsync(Bit[] bits, Word[] words, DWord[] dwords, Float[] floats);
        Task<string> GetCpuNameAsync();
        Task WriteTextAsync(string label, string text);
        Task<string> ReadTextAsync(string label, int size);
    }
}
