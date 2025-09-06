using CommunicationLib.Utils;
using FluentModbus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CommunicationLibUnitTest
{
    [TestClass]
    public class ModBusUtilsTests
    {
        [TestMethod]
        public void WriteSiemensData_Test()
        {
            ModbusClient client = ModBusUtils.MakeModbusRtuClient("COM21");

            // V区 short
            short value = 8;
            ModBusUtils.WriteSiemensData(client, 1, "VB0", value);
            Span<short> resp = ModBusUtils.ReadSiemensData<short>(client, 1, "VB0", 1);
            Assert.AreEqual(8, resp[0]);

            // V区 ushort
            ushort uvalue = 1234;
            ModBusUtils.WriteSiemensData(client, 1, "VB2", uvalue);
            Span<ushort> uresp = ModBusUtils.ReadSiemensData<ushort>(client, 1, "VB2", 1);
            Assert.AreEqual((ushort)1234, uresp[0]);

            // V区 int
            int ivalue = 0x12345678;
            ModBusUtils.WriteSiemensData(client, 1, "VD4", ivalue);
            Span<int> iresp = ModBusUtils.ReadSiemensData<int>(client, 1, "VD4", 1);
            Assert.AreEqual(0x12345678, iresp[0]);

            // V区 float
            float fvalue = 3.14f;
            ModBusUtils.WriteSiemensData(client, 1, "VD8", fvalue);
            Span<float> fresp = ModBusUtils.ReadSiemensData<float>(client, 1, "VD8", 1);
            Assert.AreEqual(3.14f, fresp[0], 0.0001f);

            // Q区写入应抛异常
            Assert.ThrowsException<NotSupportedException>(() =>
            {
                ModBusUtils.WriteSiemensData(client, 1, "Q0.0", (byte)1);
            });

            // I区写入应抛异常
            Assert.ThrowsException<NotSupportedException>(() =>
            {
                ModBusUtils.WriteSiemensData(client, 1, "I0.0", (byte)1);
            });

            // A区写入应抛异常
            Assert.ThrowsException<NotSupportedException>(() =>
            {
                ModBusUtils.WriteSiemensData(client, 1, "AIW0", (short)1);
            });

            // 不支持类型写入应抛异常
            Assert.ThrowsException<NotSupportedException>(() =>
            {
                ModBusUtils.WriteSiemensData(client, 1, "VB10", DateTime.Now);
            });
        }
    }
}
