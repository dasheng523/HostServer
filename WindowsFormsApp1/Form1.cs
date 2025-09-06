using CommunicationLib.logic;
using CommunicationLib.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MainFormLogic.TestLightUp();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            MainFormLogic.TestStartServer();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            MainFormLogic.TestStopServer();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            MainFormLogic.TestStartTcpServer();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            MainFormLogic.TestStopTcpServer();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            MainFormLogic.TestLightDown();
        }
    }
}
