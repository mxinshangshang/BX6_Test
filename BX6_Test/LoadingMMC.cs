using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Threading;

namespace BX6_Test
{
    public partial class LoadingMMC : Form
    {
        string file;
        string PLCCom;
        string Contract;
        string JobNum;
        string TELECom;
        string[,] PLCPrm1;
        int[] PLCPrm = new int[13];

        protected override void WndProc(ref   Message m)                        //禁用左上角关闭按钮
        {
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_CLOSE = 0xF060;
            if (m.Msg == WM_SYSCOMMAND && (int)m.WParam == SC_CLOSE)
            {
                return;
            }
            base.WndProc(ref m);
        }

        public LoadingMMC(string file, string PLCCom, string[,] PLCPrm1, string contract, string jobnum, int[] PLCPrm, string TELECom)
        {
            InitializeComponent();

            this.file = file;
            this.PLCCom = PLCCom;
            this.TELECom = TELECom;
            this.PLCPrm1 = PLCPrm1;
            this.Contract = contract;
            this.JobNum = jobnum;
            this.PLCPrm = PLCPrm;

            serialPort1.PortName = PLCCom;
            serialPort1.BaudRate = 9600;
            serialPort1.DataBits = 7;
            serialPort1.StopBits = StopBits.One;
            serialPort1.Parity = Parity.Even;
            if (serialPort1.IsOpen == true)
            {
                serialPort1.Close();
            }
            serialPort1.Open();
            string a = ": 01 05 08 13 FF 00";
            string b = GetLRC(a);
            byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
            serialPort1.Write(message1, 0, b.Length);
            
            if (PLCPrm[7] == 1)
            {
                MessageShow messageshow = new MessageShow("请确认控制柜内所有的断路器在断开状态, ECB印板没有任何插件" + "\n\n" + "然后" + "\n" + "(1)请插上短接端子： XPSU_B、 XSPH、 XKNE、 XKBV、 XKTHMH、 XJH、 XTHMR" + "\n\n" + "(2)请插上从控制柜元器件来的端子： XMAIN、 XRKPH、 XPSU_E、 X24PS、 XJTHS" + "\n\n" + "(3)请插上测试设备红色线束的端子： XESE、 XVF、 XTC1、 XTC2、 XISPT、 XKV、 XCAN_EXT、RS232串口线" + "\n\n" + "(4)请接 JTHS 夹具" + "\n\n" + "(5)请把 JTHS 闭合");
                messageshow.ShowDialog();
            }
            if (PLCPrm[8] == 1)
            {
                MessageShow messageshow = new MessageShow("请确认控制柜内所有的断路器在断开状态, ECB印板没有任何插件" + "\n\n" + "然后" + "\n" + "(1)请插上短接端子： XPSU_B、 XSPH、 XKNE、 XKBV、 XKTHMH、 XJH、 XTHMR、 XCTB、 XCTD" + "\n\n" + "(2)请插上从控制柜元器件来的端子： XMAIN、 XRKPH、 XPSU_E、 X24PS、 XJTHS、 XCT" + "\n\n" + "(3)请插上测试设备红色线束的端子： XESE、 XVF、 XTC1、 XTC2、 XISPT、 XKV、 XCAN_EXT、RS232串口线" + "\n\n" + "(4)请接 X1 夹具" + "\n\n" + "(5)请把 JTHS 闭合");
                messageshow.ShowDialog();
            }
            MessageBox.Show("请插入 MMC 卡");
        }

        private string GetLRC(string a)
        {
            string original = a;
            //": 01 03 06 14 00 08 "起始数据地址高字节06 起始数据地址低字节14 接点个数高字节00 接点个数低字节08 + LRC校验码
            string[] aa = original.Split(' ');
            byte[] message = new byte[aa.Length - 1];
            byte lrc = 0;
            for (int i = 1; i < aa.Length; i++)
            {
                message[i - 1] = Convert.ToByte(aa[i], 16);
            }
            foreach (byte c in message)
            {
                lrc += c;
            }
            byte hex1 = (byte)-lrc;
            string hex = Convert.ToString(hex1, 16).ToUpper();
            return a.Replace(" ", "") + hex + "\r\n";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string a = ": 01 05 09 14 FF 00";
            string b = GetLRC(a);
            byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
            serialPort1.Write(message1, 0, b.Length);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string a = ": 01 05 09 14 00 00";                                          //PowerOff
            string b = GetLRC(a);
            byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
            serialPort1.Write(message1, 0, b.Length);
            if (MessageBox.Show("请确认 升级 是否完毕", "提示", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                MessageBox.Show("请拔出 MMC 卡");
                serialPort1.Close();
                Form AutoRun = new AutoR(file, PLCCom, PLCPrm1, Contract, JobNum, PLCPrm, TELECom);
                AutoRun.Show();
                this.Close();
            }
        }
    }
}
