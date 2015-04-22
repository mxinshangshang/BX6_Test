using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Data.OleDb;

namespace BX6_Test
{
    public partial class Manu : Form
    {
        public int iTextbox1 = 0;
        FileStream myFs;
        StreamWriter mySw;
        private Thread Send;
        string file;
        string PLCCom;
        string TELECom;
        string JobNum;

        string[,] PLCPrm1;
        string[,] PLCPrm2;

        string dataRE=null;
        string[] datare = new string[60];
        int iData = 0;

        protected override void WndProc(ref   Message m)                               //禁用左上角关闭按钮
        {
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_CLOSE = 0xF060;
            if (m.Msg == WM_SYSCOMMAND && (int)m.WParam == SC_CLOSE)
            {
                return;
            }
            base.WndProc(ref m);
        }

        public Manu(string file, string PLCCom, string[,] PLCPrm1, string[,] PLCPrm2,string jobnum,string TELECom)
        {
            InitializeComponent();

            this.StartPosition = FormStartPosition.Manual;
            int yHeight = SystemInformation.PrimaryMonitorSize.Height;//获取显示器屏幕宽度
            this.Location = new Point(0, yHeight / 2 - this.Height / 2);

            this.file = file;
            this.PLCCom = PLCCom;
            this.TELECom = TELECom;
            this.PLCPrm1 = PLCPrm1;
            this.PLCPrm2 = PLCPrm2;
            this.JobNum = jobnum;

            ch372.Init();
            ch372.OpenDevice(0);
            ch372.SetTimeout(0, 3000, 3000);

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

            this.button1.Enabled = false;
            this.button2.Enabled = false;
            this.button3.Enabled = false;

            Send = new Thread(SentToPLC);
            Send.IsBackground = true;
            Send.Start();

            //this.button1.Enabled = false;
            //this.button2.Enabled = false;
            //this.button3.Enabled = false;

            //Send = new Thread(SentToPLC);
            //Send.IsBackground = true;
            //Send.Start();
        }

        #region EnableButton
        private delegate void EnableButton();
        private void enablebutton()
        {
            this.button1.Enabled = true;
            this.button2.Enabled = true;
            this.button3.Enabled = true;
        }
        #endregion

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

        private void SentToPLC()                                                        //PLC在线监测线程
        {
            string a = ": 01 02 08 96 00 01";
            string b = GetLRC(a);
            byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
            dataRE = "";
            serialPort1.Write(message1, 0, b.Length);

            Thread.Sleep(1000);
            //if (dataRE.Contains("3A 30 31 30 32 30 31 39 36"))    //修改
            //{
                EnableButton ebutton = new EnableButton(enablebutton);
                button1.Invoke(ebutton);
                return;
            //}
            //else
            //{
            //    MessageBox.Show("PLC串口未监测到 PLC 在线" + "\n\n" + "请关闭软件确认好 PLC 在线 并且与 串口 连线正确后重试", "Error");
            //    System.Diagnostics.Process.GetCurrentProcess().Kill();
            //}
            //Thread.CurrentThread.Abort();
        }


        #region txt & USB communication                    

        private delegate void SetTextCallback(string text);
        private void SetText(string text)
        {
            if (iTextbox1 == 0)
            {
                //this.textBox1.Text = text;
                iTextbox1++;
            }
            else
            {
                string content = DateTime.Now.ToString() + " " + JobNum + " " + "手动连线测试 " + text + "\r\n";
                myFs = new FileStream(file, FileMode.Append, FileAccess.Write);
                mySw = new StreamWriter(myFs);
                mySw.Write(content);
                mySw.Close();
                myFs.Close();
            }
        }
        private void StartListen(object arry)
        {
            //int l = 0;
            int error = 0;
            string[,] Arry = arry as string[,];
            while (true)
            {
                byte[] buf = new byte[128];
                int len = 100;
                SetTextCallback settextbox = new SetTextCallback(SetText);
                System.Text.ASCIIEncoding asciiEncoding = new System.Text.ASCIIEncoding();
                if (ch372.ReadData(0, buf, ref len) == true && len != 0)
                {
                    string message = asciiEncoding.GetString(buf);
                    if (!this.IsHandleCreated) return;

                    for (int k = 0; k < (Arry.Length / 3); k++)
                    {
                        if (message.Contains("OPEN") && message.Contains(Arry[k, 0]) && message.Contains(Arry[k, 1]))
                        {
                            //textBox1.Invoke(settextbox, Arry[k, 2]+"开路");
                            error++;
                        }
                        else if (message.Contains("SHORT") &&(message.Contains(Arry[k, 0])|| message.Contains(Arry[k, 1])))
                        {
                            //textBox1.Invoke(settextbox, Arry[k, 2]+"短路");
                            error++;
                        }
                    }
                    //if (error == 0) textBox1.Invoke(settextbox, PLCPrm1[num++,1]+"Pass");
                }
            }
        }
        
        #endregion

        #region Port communication

        public delegate void DeleUpdateTextbox(string dataRe);
        private void UpdateTextbox(string dataRe)
        {
            //textBox2.AppendText(dataRe);
            datare[iData++] = dataRe;
            if (datare[iData-1] == "0A " && datare[iData-2] == "0D ")
            {
                dataRE = string.Join("", datare);
                textBox1.Text = dataRE;
                iData = 0;//0123修改
            }
            //}

        }
        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {

            Thread.Sleep(500);      //等待缓冲器满
            string dataRe;
            string[] data = new string[100];                           //修改
            //int j = 0;

            byte[] byteRead = new byte[serialPort1.BytesToRead];

            DeleUpdateTextbox deleupdatetextbox = new DeleUpdateTextbox(UpdateTextbox);

            serialPort1.Read(byteRead, 0, byteRead.Length);

            for (int i = 0; i < byteRead.Length; i++)
            {
                byte temp = byteRead[i];
                dataRe = temp.ToString("X2") + " ";
                textBox1.Invoke(deleupdatetextbox, dataRe);
            }
        }
        
        #endregion

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (serialPort1.IsOpen != true)
                {
                    serialPort1.Open();
                }

                string a = ": 01 05 08 11 FF 00";
                string b = GetLRC(a);
                byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
                serialPort1.Write(message1, 0, b.Length);

                serialPort1.Close();
                Form ManuWire = new ManuW(file, PLCCom, PLCPrm1, JobNum);
                ManuWire.Show();
            }
            catch
            {
                MessageBox.Show("请先关闭其他模式的窗口！！","Error");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (serialPort1.IsOpen != true)
                {
                    serialPort1.Open();
                }
                string a = ": 01 05 08 12 FF 00";
                string b = GetLRC(a);
                byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
                serialPort1.Write(message1, 0, b.Length);

                serialPort1.Close();
                Form ManuFun = new ManuF(file, PLCCom, PLCPrm2, JobNum);
                ManuFun.Show();
            }
            catch
            {
                MessageBox.Show("请先关闭其他模式的窗口！！","Error");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                if (serialPort1.IsOpen != true)
                {
                    serialPort1.Open();
                }
                string a = ": 01 05 08 13 FF 00";
                string b = GetLRC(a);
                byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
                serialPort1.Write(message1, 0, b.Length);

                serialPort1.Close();
                Form ManuRun = new ManuR(file, PLCCom, PLCPrm1, JobNum,TELECom);
                ManuRun.Show();
            }
            catch
            {
                MessageBox.Show("请先关闭其他模式的窗口！！","Error");
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
