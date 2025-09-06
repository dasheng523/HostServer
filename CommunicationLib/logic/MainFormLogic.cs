using FluentModbus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CommunicationLib.logic
{
    public class MainFormLogic
    {
        public static void TestTcpConnect()
        {
            using (var client = new ModbusTcpClient())
            {
                try
                {
                    client.Connect(new IPEndPoint(IPAddress.Parse("192.168.3.8"), 502), ModbusEndianness.BigEndian);

                    // 读取I区数据
                    Span<byte> resp = client.ReadCoils(1, 0, 10);
                    for (int i = 0; i < resp.Length; i++)
                    {
                        Console.WriteLine($"Coils Value {i} is {resp[i]}");
                    }

                    // 读取Q区数据
                    Span<byte> resp2 = client.ReadDiscreteInputs(1, 0, 10);
                    for (int i = 0; i < resp.Length; i++)
                    {
                        Console.WriteLine($"Discrete Value {i} is {resp[i]}");
                    }

                    // 读取V区数据
                    Span<byte> resp3 = client.ReadHoldingRegisters(1, 0, 10);
                    for (int i = 0; i < resp.Length; i++)
                    {
                        Console.WriteLine($"Holding Value {i} is {resp[i]}");
                    }
                }
                catch (ModbusException ex)
                {
                    Console.WriteLine($"Modbus 协议相关错误: {ex.Message}");
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"IO Error 端口可能不存在或被占用: {ex.Message}");
                }
                catch (TimeoutException ex)
                {
                    Console.WriteLine($"TimeoutException 连接超时: {ex.Message}");
                }
            }
        }

        public static void TestLightUp() {
            using (var client = new ModbusTcpClient())
            {
                client.Connect(new IPEndPoint(IPAddress.Parse("192.168.3.8"), 502), ModbusEndianness.BigEndian);

                // 写入寄存器数据
                short value = 0x0100;
                client.WriteSingleRegister(1, 0, value);
            }
        }

        public static void TestLightDown()
        {
            using (var client = new ModbusTcpClient())
            {
                client.Connect(new IPEndPoint(IPAddress.Parse("192.168.3.8"), 502), ModbusEndianness.BigEndian);

                // 写入寄存器数据
                short value = 0x0000;
                client.WriteSingleRegister(1, 0, value);
            }
        }


        public static void TestConnect()
        {
            using (var client = new ModbusRtuClient())
            {
                try
                {
                    client.Connect("COM21", ModbusEndianness.BigEndian);

                    // 读取寄存器数据
                    Span<ushort> resp = client.ReadHoldingRegisters<ushort>(1, 0, 10);
                    for (int i = 0; i < resp.Length; i++)
                    {
                        Console.WriteLine($"Value {i} is {resp[i]}");
                    }

                    // 写入寄存器数据
                    client.WriteSingleRegister(1, 1, 88);

                    // read back from server to prove correctness
                    var shortDataResult = client.ReadHoldingRegisters<short>(1, 1, 1);
                    Console.WriteLine(shortDataResult[0]); // 应该打印88
                }
                catch (ModbusException ex)
                {
                    Console.WriteLine($"Modbus 协议相关错误: {ex.Message}");
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"IO Error 端口可能不存在或被占用: {ex.Message}");
                }
                catch (TimeoutException ex)
                {
                    Console.WriteLine($"TimeoutException 连接超时: {ex.Message}");
                }
            }
        }

        private static ModbusRtuServer rtuServer;
        private static ModbusTcpServer tcpServer;
        private static readonly Random random = new Random();

        /// <summary>
        ///测试启动服务器
        /// </summary>
        public static void TestStartServer()
        {
            rtuServer = new ModbusRtuServer(1)
            {
                ReadTimeout = 500,
                WriteTimeout = 500,
                EnableRaisingEvents = true
            };
            rtuServer.Start(port: "COM20");

            var registers = rtuServer.GetHoldingRegisters(1);

            lock (rtuServer.Lock)
            {
                var rand = random.Next();
                registers.SetBigEndian<ushort>(address: 0, (ushort)rand);
            }

            rtuServer.RegistersChanged += (sender, registerAddresses) => {
                // registerAddresses 是发生变化的寄存器地址集合
                var changedRegisters = rtuServer.GetHoldingRegisters(1);   // C#无法在lamdba表达式中直接访问局部变量，所以需要重新获取寄存器对象

                foreach (var addr in registerAddresses.Registers)
                {
                    // 读取变化后的值（以 ushort 为例，按大端方式）
                    ushort value = changedRegisters.GetBigEndian<ushort>(addr);
                    Console.WriteLine($"寄存器地址 {addr} 的新值为: {value}");
                }
            };
        }

        /// <summary>
        /// 测试停止服务器
        /// </summary>
        public static void TestStopServer()
        {
            rtuServer?.Dispose();
        }

        /// <summary>
        ///测试启动服务器
        /// </summary>
        public static void TestStartTcpServer()
        {
            tcpServer = new ModbusTcpServer()
            {
                EnableRaisingEvents = true
            };
            tcpServer.Start();

            var registers = tcpServer.GetHoldingRegisters();

            lock (tcpServer.Lock)
            {
                var rand = random.Next();
                registers.SetBigEndian<ushort>(address: 0, (ushort)rand);
            }

            tcpServer.RegistersChanged += (sender, registerAddresses) => {
                // registerAddresses 是发生变化的寄存器地址集合
                var changedRegisters = tcpServer.GetHoldingRegisters();   // C#无法在lamdba表达式中直接访问局部变量，所以需要重新获取寄存器对象

                foreach (var addr in registerAddresses.Registers)
                {
                    // 读取变化后的值（以 ushort 为例，按大端方式）
                    ushort value = changedRegisters.GetBigEndian<ushort>(addr);
                    Console.WriteLine($"寄存器地址 {addr} 的新值为: {value}");
                }
            };
        }

        /// <summary>
        /// 测试停止服务器
        /// </summary>
        public static void TestStopTcpServer()
        {
            tcpServer?.Dispose();
        }
    }
}
