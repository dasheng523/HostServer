using FluentModbus;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CommunicationLib.Utils
{
    public class ModBusUtils
    {

        /// <summary>
        /// 创建一个Rtu客户端
        /// </summary>
        /// <param name="comNum">串口</param>
        /// <param name="endian">默认大端</param>
        /// <returns></returns>
        public static ModbusRtuClient MakeModbusRtuClient(String comNum, ModbusEndianness endian = ModbusEndianness.BigEndian) {
            var client = new ModbusRtuClient();
            client.Connect(comNum, endian);
            return client;
        }

        /// <summary>
        /// 关闭Rtu客户端
        /// </summary>
        /// <param name="client"></param>
        public static void CloseRtuClient(ModbusRtuClient client) {
            client.Close();
        }

        public static Span<T> ReadSiemensData<T>(ModbusClient client, int serverId, String varName, int length) where T : unmanaged
        {
            int address = FromSiemensAddress(varName);
            string dataType = typeof(T).Name;
            bool supported = IsSupportedType(dataType);
            if (!supported)
                throw new NotSupportedException($"ReadSiemensData 不支持的数据类型: {dataType}");

            char firstChar = varName[0];
            switch (firstChar)
            {
                case 'Q':
                    if (typeof(T) != typeof(byte))
                        throw new NotSupportedException($"ReadSiemens 读取Q区变量只支持 byte 类型，当前类型: {dataType}");
                    return MemoryMarshal.Cast<byte, T>(client.ReadCoils(serverId, address, length));
                case 'I':
                    if (typeof(T) != typeof(byte))
                        throw new NotSupportedException($"ReadSiemens 读取I区变量只支持 byte 类型，当前类型: {dataType}");
                    return MemoryMarshal.Cast<byte, T>(client.ReadDiscreteInputs(serverId, address, length));
                case 'A':
                    return client.ReadInputRegisters<T>(serverId, address, length);
                case 'V':
                    return client.ReadHoldingRegisters<T>(serverId, address, length);
                default:
                    throw new NotSupportedException($"ReadSiemens 不支持的地址类型: {firstChar}");
            }
        }

        public static void WriteSiemensData<T>(ModbusClient client, int serverId, String varName, T value) where T : unmanaged
        {
            int address = FromSiemensAddress(varName);
            string dataType = typeof(T).Name;
            bool supported = IsSupportedType(dataType);
            if (!supported)
                throw new NotSupportedException($"WriteSiemensData 不支持的数据类型: {dataType}");

            char firstChar = varName[0];
            switch (firstChar)
            {
                case 'Q':
                    throw new NotSupportedException("Q 区（离散量输入）为只读，不能写入。");
                case 'I':
                    throw new NotSupportedException("I 区（离散量输入）为只读，不能写入。");
                case 'A':
                    // AI区为输入寄存器，只读，不能写
                    throw new NotSupportedException("A 区（输入寄存器）为只读，不能写入。");
                case 'V':
                    // V区为保持寄存器，支持写
                    // 按类型写入
                    if (typeof(T) == typeof(byte) || typeof(T) == typeof(ushort))
                        client.WriteSingleRegister(serverId, address, Convert.ToUInt16(value));
                    else if (typeof(T) == typeof(short))
                        client.WriteSingleRegister(serverId, address, Convert.ToInt16(value));
                    else if (typeof(T) == typeof(uint))
                    {
                        // 32位，写两个寄存器
                        uint v = Convert.ToUInt32(value);
                        ushort hi = (ushort)(v >> 16);
                        ushort lo = (ushort)(v & 0xFFFF);
                        client.WriteMultipleRegisters(serverId, address, new ushort[] { hi, lo });
                    }
                    else if (typeof(T) == typeof(int))
                    {
                        int v = Convert.ToInt32(value);
                        ushort hi = (ushort)((uint)v >> 16);
                        ushort lo = (ushort)((uint)v & 0xFFFF);
                        client.WriteMultipleRegisters(serverId, address, new ushort[] { hi, lo });
                    }
                    else if (typeof(T) == typeof(float))
                    {
                        // float转为2个ushort
                        var bytes = BitConverter.GetBytes(Convert.ToSingle(value));
                        ushort[] regs = new ushort[2];
                        regs[0] = BitConverter.ToUInt16(bytes, 2); // 高位
                        regs[1] = BitConverter.ToUInt16(bytes, 0); // 低位
                        client.WriteMultipleRegisters(serverId, address, regs);
                    }
                    else if (typeof(T) == typeof(double))
                    {
                        // double转为4个ushort
                        var bytes = BitConverter.GetBytes(Convert.ToDouble(value));
                        ushort[] regs = new ushort[4];
                        regs[0] = BitConverter.ToUInt16(bytes, 6);
                        regs[1] = BitConverter.ToUInt16(bytes, 4);
                        regs[2] = BitConverter.ToUInt16(bytes, 2);
                        regs[3] = BitConverter.ToUInt16(bytes, 0);
                        client.WriteMultipleRegisters(serverId, address, regs);
                    }
                    else
                    {
                        throw new NotSupportedException($"WriteSiemensData 不支持的数据类型: {dataType}");
                    }
                    break;
                default:
                    throw new NotSupportedException($"WriteSiemensData 不支持的地址类型: {firstChar}");
            }
        }

        private static bool IsSupportedType(string dataType)
        {
            switch (dataType)
            {
                case "Byte":     // System.Byte
                case "SByte":    // System.SByte
                case "UInt16":   // ushort
                case "Int16":    // short
                case "UInt32":   // uint (double word)
                case "Int32":    // int
                case "UInt64":   // ulong
                case "Int64":    // long
                case "Single":   // float
                case "Double":   // double
                    return true;
                default:
                    return false;
            }
        }

        public enum SiemensDataType
        {
            Byte,
            Word,
            DoubleWord
        }


        public static int FromSiemensAddress(string siemensAddress)
        {
            if (string.IsNullOrEmpty(siemensAddress))
                throw new ArgumentNullException(nameof(siemensAddress));

            // 提取地址类型和数值
            string addressType = siemensAddress.Substring(0, 1);
            string remainingPart = siemensAddress.Substring(1);

            switch (addressType)
            {
                case "Q": // 离散量输出（线圈）
                    {
                        var parts = remainingPart.Split('.');
                        if (parts.Length != 2)
                            throw new ArgumentException("无效的Q地址格式", nameof(siemensAddress));

                        int byteIndex = int.Parse(parts[0]);
                        int bitIndex = int.Parse(parts[1]);
                        return byteIndex * 8 + bitIndex;
                    }

                case "I": // 离散量输入（触点）
                    {
                        var parts = remainingPart.Split('.');
                        if (parts.Length != 2)
                            throw new ArgumentException("无效的I地址格式", nameof(siemensAddress));

                        int byteIndex = int.Parse(parts[0]);
                        int bitIndex = int.Parse(parts[1]);
                        return byteIndex * 8 + bitIndex;
                    }

                case "A": // 输入寄存器（模拟量输入）
                    {
                        if (remainingPart.Length < 2)
                            throw new ArgumentException("无效的AI地址格式", nameof(siemensAddress));

                        string dataType = remainingPart.Substring(0, 2);
                        int address = int.Parse(remainingPart.Substring(2));

                        if (dataType != "IB" && dataType != "IW" && dataType != "ID")
                            throw new ArgumentException("无效的AI地址类型", nameof(siemensAddress));

                        return (address / 2);
                    }

                case "V": // 保持寄存器（V存储器）
                    {
                        if (remainingPart.Length < 2)
                            throw new ArgumentException("无效的V地址格式", nameof(siemensAddress));

                        char dataTypeChar = remainingPart[0];
                        int address = int.Parse(remainingPart.Substring(1));

                        if (dataTypeChar != 'B' && dataTypeChar != 'W' && dataTypeChar != 'D')
                            throw new ArgumentException("无效的V地址类型", nameof(siemensAddress));

                        address = address / 2;
                        return address;
                    }

                default:
                    throw new ArgumentException("不支持的地址类型", nameof(siemensAddress));
            }
        }
    }
}
