using System;
using System.Runtime.InteropServices;

namespace Lucky.Home.Services.FluentModbus
{
    /// <summary>
    /// Imported from Fluentmodbus code to fix https://github.com/Apollo3zehn/FluentModbus/pull/100
    /// </summary>
    internal static class ModbusUtils
    {
        private static ushort SwitchEndianness(ushort value)
        {
            var bytes = BitConverter.GetBytes(value);
            return (ushort)((bytes[0] << 8) + bytes[1]);
        }

        private static T SwitchEndianness<T>(T value) where T : unmanaged
        {
            Span<T> data = stackalloc T[] { value };
            SwitchEndianness(data);
            return data[0];
        }

        public static void SwitchEndianness<T>(Memory<T> dataset) where T : unmanaged
        {
            SwitchEndianness(dataset.Span);
        }

        private static void SwitchEndianness<T>(Span<T> dataset) where T : unmanaged
        {
            var size = Marshal.SizeOf<T>();
            var dataset_bytes = MemoryMarshal.Cast<T, byte>(dataset);

            for (int i = 0; i < dataset_bytes.Length; i += size)
            {
                for (int j = 0; j < size / 2; j++)
                {
                    var i1 = i + j;
                    var i2 = i - j + size - 1;

                    (dataset_bytes[i2], dataset_bytes[i1]) = (dataset_bytes[i1], dataset_bytes[i2]);
                }
            }
        }
    }
}
