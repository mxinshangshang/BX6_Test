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
using System.Text.RegularExpressions;

namespace BX6_Test
{
    public partial class AutoR : Form
    {
        #region Global Variables
        private Thread CheckRun;
        private Thread Send;
        private Thread Door;
        //private Thread Call;
        private Thread opening;
        private Thread closeing;
        private Thread R;
        private Thread T;
        private Thread RE;
        FileStream myFs;
        StreamWriter mySw;
        string file;
        string PLCCom;
        string Contract;
        string JobNum;
        string TELECom;
        string HWversion="";
        string SWversion="";

        public int iTextbox5 = 0;
        string[] words = new string[2];
        string[,] PLCPrm1;
        int[] PLCPrm = new int[13];

        short[] M = new short[3];

        string dataRE = null;
        string[] datare = new string[100];
        int iData = 0;

        bool ing = false;
        //bool next = false;
        bool tally = false;
        bool encoder = false;
        //bool openornot = false;
        //bool closeornot = false;
        bool floor1 = false;
        bool floor2 = false;
        bool floor3 = false;
        bool one = false;
        bool two = false;
        bool three = false;
        bool runup = false;
        bool rundown = false;
        bool runover = false;
        bool stoprun = false;
        bool goground = false;
        bool learningtrip = false;
        bool inspectiontrip = false;
        bool carcalltrip = false;
        bool First = false;
        bool Second = false;
        bool Third = false;
        bool FirstDoor = false;
        bool SecondDoor = false;
        bool ThirdDoor = false;
        bool NEXT = true;
        bool version = false;
        public bool closeit = false;

        int error1 = 0;
        int error2 = 0;
        int error3 = 0;

        int trackbarValue = 0;
        int firstplace = 0;
        int secondplace = 0;
        #endregion

        //static AutoResetEvent waitCallCarHandler = new AutoResetEvent(false);

        protected override void WndProc(ref   Message m)                //禁用左上角关闭按钮
        {
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_CLOSE = 0xF060;
            if (m.Msg == WM_SYSCOMMAND && (int)m.WParam == SC_CLOSE)
            {
                return;
            }
            base.WndProc(ref m);
        }

        public AutoR(string file, string PLCCom, string[,] PLCPrm1, string contract, string jobnum, int[] PLCPrm, string TELECom)
        {
            InitializeComponent();

            this.file = file;
            this.PLCCom = PLCCom;
            this.PLCPrm1 = PLCPrm1;
            this.Contract = contract;
            this.JobNum = jobnum;
            this.PLCPrm = PLCPrm;
            this.TELECom = TELECom;

            groupBox2.Visible = false;
            groupBox3.Visible = false;

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

            serialPort2.PortName = TELECom;
            serialPort2.BaudRate = 9600;
            serialPort2.DataBits = 8;
            serialPort2.StopBits = StopBits.One;
            serialPort2.Parity = Parity.None;

            if (serialPort2.IsOpen == true)
            {
                serialPort2.Close();
            }

            serialPort2.Open();


            trackBar1.Maximum = 84000;
            trackBar1.Minimum = 0;

            //button8.Enabled = false;
            //button9.Enabled = false;
            label2.Text = "           ";
            label4.Text = "           ";
            label6.Text = "           ";
            //label8.Text = "           ";
            //label10.Text = "           ";
            //label12.Text = "           ";
            label2.BackColor = Color.LightGray;
            label4.BackColor = Color.LightGray;
            label6.BackColor = Color.LightGray;
            //label8.BackColor = Color.LightGray;
            //label10.BackColor = Color.LightGray;
            //label12.BackColor = Color.LightGray;

            if (PLCPrm[7] == 1)
            {
                label8.Text = "JTHS";
                label10.Text = "夹具处";
            }
            if (PLCPrm[8] == 1)
            {
                label8.Text = "X2";
                label10.Text = "接头处";
            }

            if (PLCPrm[7] == 1)
            {
                pictureBox5.Visible = false;
                pictureBox3.Visible = true;
            }
            if (PLCPrm[8] == 1)
            {
                pictureBox5.Visible = true;
                pictureBox3.Visible = true;
            }
        }

        #region txt & USB communication

        private delegate void SetTextCallback(string text);
        private void SetText(string text)
        {
            this.textBox1.Text += Environment.NewLine + text + "\r\n";//"\r\n";// "\r\n" + text;
            this.textBox1.SelectionStart = this.textBox1.Text.Length;
            this.textBox1.ScrollToCaret();

            string content = DateTime.Now.ToString() + " " + Contract + " " + JobNum + " " + "自动运行测试 " + text + "\r\n";
            myFs = new FileStream(file, FileMode.Append, FileAccess.Write);
            mySw = new StreamWriter(myFs);
            mySw.Write(content);
            mySw.Close();
            myFs.Close();
        }

        private delegate void EnconderNumberCallback(string text);
        private void SetNumber(string text)
        {
            this.textBox3.Text = text;//"\r\n";// "\r\n" + text;
            if (Convert.ToInt32(text, 10) < 84000 && Convert.ToInt32(text, 10) > 0)
            {
                this.trackBar1.Value = Convert.ToInt32(text, 10);
                trackbarValue=Convert.ToInt32(text, 10);
            }
        }

        #endregion

        #region Port communication

        public delegate void DeleUpdateTextbox(string dataRe);
        private void UpdateTextbox(string dataRe)
        {
            EnconderNumberCallback setnumber = new EnconderNumberCallback(SetNumber);
            SetTextCallback settextbox = new SetTextCallback(SetText);

            if (dataRe == "3A " || ing ==true)
            {
                ing = true;
                datare[iData++] = dataRe;

                if (iData == 99)
                {
                    iData = 2;
                }

                if (datare[iData - 1] == "0A " && datare[iData - 2] == "0D ")
                {
                    dataRE = string.Join("", datare);
                    textBox2.Text = dataRE;
                    if (dataRE.Contains("3A 30 31 30 33 "))
                    {
                        string C = dataRE;// "3A 30 31 30 33 30 32 30 39 42 30 34 31 0D 0A ";
                        string ascii = C.Substring(21, 12 * 2).Replace(" ", "");
                        List<byte> buffer = new List<byte>();
                        for (int j = 0; j < ascii.Length; j += 2)
                        {
                            string temp = ascii.Substring(j, 2);
                            byte value = Convert.ToByte(temp, 16);
                            buffer.Add(value);
                        }
                        string str = System.Text.Encoding.ASCII.GetString(buffer.ToArray());
                        string[] result = Regex.Split(str, @"(?<=\G.{4})(?!$)");
                        try
                        {
                            Convert.ToInt32(result[1], 16);
                        }
                        catch
                        {
                            result[0] = "0";
                            result[1] = "0";
                        }
                        textBox3.Invoke(setnumber, (Convert.ToInt32(result[0], 16) + Convert.ToInt32(result[1], 16) * 65536).ToString());
                        if ((Convert.ToInt32(result[0], 16) + Convert.ToInt32(result[1], 16) * 65536) == 999999)
                        {
                            textBox1.Invoke(settextbox, "发生急停，软件运行终止！");
                            MessageBox.Show("发生急停！即将关闭软件！！", "提示");
                            System.Diagnostics.Process.GetCurrentProcess().Kill();
                        }                        
                        C= "";
                    }
                    iData = 0;
                    ing = false;
                }
            }
        }
        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {

            Thread.Sleep(500);      //等待缓冲器满
            string dataRe;
            string[] data = new string[100];                           //修改
            try
            {
                byte[] byteRead = new byte[serialPort1.BytesToRead];
                DeleUpdateTextbox deleupdatetextbox = new DeleUpdateTextbox(UpdateTextbox);

                serialPort1.Read(byteRead, 0, byteRead.Length);

                for (int i = 0; i < byteRead.Length; i++)
                {
                    byte temp = byteRead[i];
                    dataRe = temp.ToString("X2") + " ";
                    textBox2.Invoke(deleupdatetextbox, dataRe);
                }
            }
            catch
            {

            }

        }


        public delegate void DeleCheckTextbox(string dataRe);
        private void CheckTextbox(string dataRe)
        {
            textBox1.AppendText(dataRe);
            Thread.Sleep(800);
            if (dataRe.Length > 150)
            {              
                if (dataRe.Substring(dataRe.LastIndexOf("\r\nCPLD") + 6, 5).Trim().Contains('V'))
                {
                    HWversion = dataRe.Substring(dataRe.LastIndexOf("\r\nCPLD") + 6, 5).Trim();
                    //if (HWversion.Trim().Contains(Properties.Settings.Default.HardwareSetting.Trim()) == false)
                    //{
                    //    MessageBox.Show("要求硬件版本为： " + Properties.Settings.Default.HardwareSetting + "\n\n" + "实际硬件版本为： " + HWversion);
                    //}
                }
                if (dataRe.Substring(dataRe.LastIndexOf("\r\nSoftware version S00x") + 23, 10).Trim().Contains('V'))
                {
                    SWversion = dataRe.Substring(dataRe.LastIndexOf("\r\nSoftware version S00x") + 23, 10).Trim();
                    //if (SWversion.Trim().Contains(Properties.Settings.Default.SoftwareSetting.Trim()) == false)
                    //{
                    //    MessageBox.Show("要求软件版本为： " + Properties.Settings.Default.SoftwareSetting + "\n\n" + "实际软件版本为： " + SWversion);
                    //}
                }
                version = true;
            }
        }
        private void serialPort2_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string dataRe;
            string[] data = new string[1000];
            byte[] byteRead = new byte[serialPort2.BytesToRead];

            DeleCheckTextbox checktextbox = new DeleCheckTextbox(CheckTextbox);

            serialPort2.Read(byteRead, 0, byteRead.Length);

            dataRe = Encoding.Default.GetString(byteRead);
            textBox1.Invoke(checktextbox, dataRe);
        }

        #endregion

        #region EnableButton
        private delegate void EnableButton();
        private void enablebutton()
        {
            this.button19.Enabled = true;
            AutoReport AutoReport = new AutoReport(NEXT, PLCCom);
            //AutoReport.Show();
            AutoReport.GetOrder(this);
            AutoReport.ShowDialog();
            if (closeit == true)
            {
                this.Close();
                //Thread.CurrentThread.Abort();
            }
            else
            {
                try
                {
                    T.Abort();
                    RE.Abort();
                    CheckRun.Abort();
                    Door.Abort();
                }
                catch { }
                R.Abort();
                NEXT = true;
                HWversion = "";
                SWversion = "";
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
            }
        }
        #endregion

        #region Close or Open the Door

        private delegate void MOVING(int x);
        private void move_l(int x)
        {
            this.button17.Location = new System.Drawing.Point(x, this.button17.Location.Y);
        }
        private void move_r(int x)
        {
            this.button18.Location = new System.Drawing.Point(x, this.button18.Location.Y);
        }
        private void OpenTheDoor()
        {
            MOVING moving_l = new MOVING(move_l);
            MOVING moving_r = new MOVING(move_r);
            int l = 949;
            int r = 997;
            while (true)
            {
                button17.Invoke(moving_l, l=l-2);
                button18.Invoke(moving_r, r=r+2);
                Thread.Sleep(100);
                if (l == 903) 
                {
                    Thread.CurrentThread.Abort();
                }
            }
            //Thread.CurrentThread.Abort();
            //Thread.CurrentThread.Abort();
        }
        private void CloseTheDoor()
        {
            MOVING moving_l = new MOVING(move_l);
            MOVING moving_r = new MOVING(move_r);
            int l = 904;
            int r = 1041;
            while (true)
            {
                button17.Invoke(moving_l, l=l+2);
                button18.Invoke(moving_r, r=r-2);
                Thread.Sleep(100);
                if (l == 948)
                {
                    Thread.CurrentThread.Abort();
                }
            }
            //Thread.CurrentThread.Abort();
            //Thread.CurrentThread.Abort();
        }

        #endregion

        #region Show Widgets
        private delegate void SHOWgroupbox2();
        private void ShowGroupBox2()
        {
            groupBox2.Visible = true;
            groupBox3.Visible = false;
        }

        private delegate void SHOWgroupbox3();
        private void ShowGroupBox3()
        {
            groupBox3.Visible = true;
            groupBox2.Visible = false;
        }

        private delegate void SHOWbutton7();
        private void ShowButton7()
        {
            button7.Visible = true;
            button16.Visible = false;
        }

        private delegate void SHOWbutton16();
        private void ShowButton16()
        {
            button16.Visible = true;
            button7.Visible = false;
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

        private void ReadEconder()                                                      //发送读取编码器脉冲数指令
        {
            while (true)
            {
                if (encoder)
                {
                    string a = ": 01 03 12 " + "00" + " 00 02";
                    string b = GetLRC(a);
                    byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
                    try
                    {
                        serialPort1.Write(message1, 0, b.Length);
                    }
                    catch
                    {

                    }
                    if (!encoder) break;
                    Thread.Sleep(500);
                }
            }
        }

        private void Tally()                                                            //提示灯专属线程
        {
            while (tally)
            {
                if ((trackbarValue >= 80800 && trackbarValue < 84000) || (trackbarValue >= 40400 && trackbarValue <= 43600) || (trackbarValue >= 0 && trackbarValue <= 3200))          //PHS
                {
                    label6.BackColor = Color.Green;                                                                //PHS
                }
                else label6.BackColor = Color.LightGray;
                if (trackbarValue >= 0 && trackbarValue <= 18266)                                                  //KSE_D
                {
                    label4.BackColor = Color.Green;
                }
                else label4.BackColor = Color.LightGray;
                if (trackbarValue >= 66500 && trackbarValue <= 67900)                                                  //KSE_U
                {
                    label2.BackColor = Color.Green;
                }
                else label2.BackColor = Color.LightGray;
                if (!tally) Thread.CurrentThread.Abort();
            }
        }

        private void SentToPLC(object plc)                                              //PLC命令发送线程
        {
            string c = plc.ToString();
            encoder = false;
            Thread.Sleep(500);
            string a = ": 01 05 09 " + c + " 00";
            string b = GetLRC(a);
            byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
            try
            {
                serialPort1.Write(message1, 0, b.Length);
            }
            catch
            {

            }
            //Thread.Sleep(500);    //修改201503062125
            encoder = true;
            Thread.Sleep(500);    //修改201503062125
            encoder = true;
            Thread.CurrentThread.Abort();
        }

        private void OpenCloseDoor()                                                    //开关门检测线程
        {
            while (true)
            {
                if ((trackbarValue > 0 && trackbarValue < 3200) && floor1 == true)
                {
                    int i = 10;
                    int j = 1;
                    int o = 1;
                    int c = 1;
                    Thread.Sleep(500);
                    encoder = false;
                    Thread.Sleep(500);
                    encoder = false;
                    button12.ForeColor = Color.Black;

                    while (i != 0)
                    {
                        string a = ": 01 02 08 96 00 01";// ": 01 02 0A E4 00 01";
                        string b = GetLRC(a);
                        byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
                        dataRE = "";
                        serialPort1.Write(message1, 0, b.Length);
                        Thread.Sleep(500);
                        if (dataRE.Contains("3A 30 31 30 32 30 31 39 37 36 35 0D 0A") && o == 1)
                        {
                            o = 0;
                            First = false;
                            opening = new Thread(OpenTheDoor);
                            opening.IsBackground = true;
                            opening.Start();                                          //显示开门

                            FirstDoor = true;

                            while (j != 0)
                            {
                                a = ": 01 02 08 96 00 01";// ": 01 02 0A E4 00 01";
                                b = GetLRC(a);
                                message1 = System.Text.Encoding.ASCII.GetBytes(b);
                                dataRE = "";
                                serialPort1.Write(message1, 0, b.Length);
                                Thread.Sleep(500);
                                if (dataRE.Contains("3A 30 31 30 32 30 31 39 36 36 36 0D 0A") && c == 1)
                                {
                                    c = 0;
                                    closeing = new Thread(CloseTheDoor);
                                    closeing.IsBackground = true;
                                    closeing.Start();                                         //显示关门
                                    j = 0;
                                }
                            }
                        }
                        i--;
                    }
                    floor1 = false;
                    encoder = true;
                }
                if ((trackbarValue > 40400 && trackbarValue < 43600) && floor2 == true)
                {
                    int i = 10;
                    int j = 1;
                    int o = 1;
                    int c = 1;
                    Thread.Sleep(500);
                    encoder = false;
                    Thread.Sleep(500);
                    encoder = false;
                    button11.ForeColor = Color.Black;

                    while (i != 0)
                    {
                        string a = ": 01 02 08 96 00 01";// ": 01 02 0A E4 00 01";
                        string b = GetLRC(a);
                        byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
                        dataRE = "";
                        serialPort1.Write(message1, 0, b.Length);
                        Thread.Sleep(500);
                        if (dataRE.Contains("3A 30 31 30 32 30 31 39 37 36 35 0D 0A") && o == 1)
                        {
                            o = 0;
                            Second = false;
                            opening = new Thread(OpenTheDoor);
                            opening.IsBackground = true;
                            opening.Start();                                          //显示开门

                            SecondDoor = true;

                            while (j != 0)
                            {
                                a = ": 01 02 08 96 00 01";// ": 01 02 0A E4 00 01";
                                b = GetLRC(a);
                                message1 = System.Text.Encoding.ASCII.GetBytes(b);
                                dataRE = "";
                                serialPort1.Write(message1, 0, b.Length);
                                Thread.Sleep(500);
                                if (dataRE.Contains("3A 30 31 30 32 30 31 39 36 36 36 0D 0A") && c == 1)
                                {
                                    c = 0;
                                    closeing = new Thread(CloseTheDoor);
                                    closeing.IsBackground = true;
                                    closeing.Start();                                         //显示关门
                                    j = 0;
                                }
                            }
                        }
                        i--;
                    }
                    floor2 = false;
                    encoder = true;
                }
                if ((trackbarValue > 80800 && trackbarValue < 84000) && floor3 == true)
                {
                    int i = 10;
                    int j = 1;
                    int o = 1;
                    int c = 1;
                    Thread.Sleep(500);
                    encoder = false;
                    Thread.Sleep(500);
                    encoder = false;
                    button10.ForeColor = Color.Black;

                    while (i != 0)
                    {
                        string a = ": 01 02 08 96 00 01";// ": 01 02 0A E4 00 01";
                        string b = GetLRC(a);
                        byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
                        dataRE = "";
                        serialPort1.Write(message1, 0, b.Length);
                        Thread.Sleep(500);
                        if (dataRE.Contains("3A 30 31 30 32 30 31 39 37 36 35 0D 0A") && o == 1)
                        {
                            o = 0;
                            Third = false;
                            opening = new Thread(OpenTheDoor);
                            opening.IsBackground = true;
                            opening.Start();                                          //显示开门

                            ThirdDoor = true;

                            while (j != 0)
                            {
                                a = ": 01 02 08 96 00 01";// ": 01 02 0A E4 00 01";
                                b = GetLRC(a);
                                message1 = System.Text.Encoding.ASCII.GetBytes(b);
                                dataRE = "";
                                serialPort1.Write(message1, 0, b.Length);
                                Thread.Sleep(500);
                                if (dataRE.Contains("3A 30 31 30 32 30 31 39 36 36 36 0D 0A") && c == 1)
                                {
                                    c = 0;
                                    closeing = new Thread(CloseTheDoor);
                                    closeing.IsBackground = true;
                                    closeing.Start();                                         //显示关门
                                    j = 0;
                                }
                            }
                        }
                        i--;
                    }
                    //while (i != 0)
                    //{
                    //    string a = ": 01 02 08 96 00 01";// ": 01 02 0A E4 00 01";
                    //    string b = GetLRC(a);
                    //    byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
                    //    dataRE = "";
                    //    serialPort1.Write(message1, 0, b.Length);
                    //    Thread.Sleep(1000);
                    //    if (dataRE.Contains("3A 30 31 30 32 30 31 39 37 36 35 0D 0A") && o == 1)
                    //    {
                    //        o = 0;
                    //        opening = new Thread(OpenTheDoor);
                    //        opening.IsBackground = true;
                    //        opening.Start();                                          //显示开门

                    //        button13.ForeColor = Color.Black;
                    //    }
                    //    else if (dataRE.Contains("3A 30 31 30 32 30 31 39 36 36 36 0D 0A") && c == 1)
                    //    {
                    //        c = 0;
                    //        closeing = new Thread(CloseTheDoor);
                    //        closeing.IsBackground = true;
                    //        closeing.Start();                                         //显示关门

                    //        button14.ForeColor = Color.Black;
                    //    }
                    //    i--;
                    //}
                    floor3 = false;
                    encoder = true;
                }
            }
        }

        private void CarCall()                                                          //楼层呼叫发送序列
        {
            while (true)
            {
                //waitCallCarHandler.WaitOne();
                if (one == true)
                {
                    one = false;
                    Send = new Thread(new ParameterizedThreadStart(SentToPLC));
                    Send.IsBackground = true;
                    Send.Start("1B FF");
                    //Thread.Sleep(500);
                }
                if (two == true)
                {
                    two = false;
                    Send = new Thread(new ParameterizedThreadStart(SentToPLC));
                    Send.IsBackground = true;
                    Send.Start("1C FF");
                    //Thread.Sleep(1000);
                }
                if (three == true)
                {
                    three = false;
                    Send = new Thread(new ParameterizedThreadStart(SentToPLC));
                    Send.IsBackground = true;
                    Send.Start("1D FF");
                    //Thread.Sleep(1000);
                }
            }
        }                                                       

        private void RightOrWrong()                                                     //运行状态检测线程
        {
            bool i = false;
            bool j = false;
            bool k = false;
            SetTextCallback settextbox = new SetTextCallback(SetText);
            while (true)
            {
                if (learningtrip)                                                     //Learning Trip Error
                {
                    Thread.Sleep(1000);
                    if (trackbarValue > 84000)
                    {
                        textBox1.Invoke(settextbox, "Learning Trip 故障 电梯到顶楼后继续上升");
                        learningtrip = false;
                        error1++;
                        NEXT = false;
                    }
                    else if (trackbarValue < 100)
                    {
                        textBox1.Invoke(settextbox, "Learning Trip 故障 电梯到底楼后继续下降");
                        learningtrip = false;
                        error1++;
                        NEXT = false;
                    }
                    else if (trackbarValue > 3200 || i == true)     //电梯↑找底楼KSE上边界
                    {
                        i = true;
                        if (trackbarValue > 18780 && j == false)    //电梯↑超过底楼KSE上边界
                        {
                            textBox1.Invoke(settextbox, "Learning Trip 故障 电梯没有检测到底楼KSE");
                            learningtrip = false;
                            error1++;
                            NEXT = false;
                        }
                        else if (trackbarValue < 3200 || j == true) //电梯↓回到底楼PHS
                        {
                            j = true;
                            if (trackbarValue > 18780 || k == true) //电梯↑离开底楼KSE找顶楼PHS
                            {
                                k = true;
                                if (trackbarValue < 1900 && trackbarValue > 900)           //电梯↓重新回到底楼PHS
                                {
                                    Thread.Sleep(1000);
                                    firstplace = trackbarValue;
                                    Thread.Sleep(3000);
                                    secondplace = trackbarValue;
                                    if (firstplace - secondplace == 0)
                                    {
                                        i = false;
                                        k = false;
                                        j = false;
                                        if (error1 == 0)
                                        {
                                            textBox1.Invoke(settextbox, "Learning Trip PASS");
                                        }
                                        learningtrip = false;
                                        error1 = 0;
                                    }
                                    else if (trackbarValue < 900)
                                    {
                                        textBox1.Invoke(settextbox, "Learning Trip 故障 自学习从三楼回到底楼后遇PHS不停");
                                        learningtrip = false;
                                        error1++;
                                        NEXT = false;
                                    }
                                }
                            }
                        }
                    }
                }
                if (inspectiontrip)                                                   //Inspection Trip Error
                {
                    Thread.Sleep(1000);
                    if (runup)
                    {
                        firstplace = trackbarValue;
                        Thread.Sleep(3000);
                        secondplace = trackbarValue;
                        if (firstplace - secondplace >= 0)
                        {
                            textBox1.Invoke(settextbox, "Inspection Trip 故障 按上行电梯没有上行");
                            inspectiontrip = false;
                            error2++;
                            NEXT = false;
                        }
                    }
                    if (rundown)
                    {
                        firstplace = trackbarValue;
                        Thread.Sleep(3000);
                        secondplace = trackbarValue;
                        if (secondplace - firstplace >= 0)
                        {
                            textBox1.Invoke(settextbox, "Inspection Trip 故障 按下行电梯没有下行");
                            inspectiontrip = false;
                            error2++;
                            NEXT = false;
                        }
                    }
                    if (runover)
                    {
                        Thread.Sleep(1000);
                        if (runover && (trackbarValue > 84000 || trackbarValue < 0))
                        {
                            textBox1.Invoke(settextbox, "Inspection Trip 故障 电梯位置越界");
                            inspectiontrip = false;
                            error2++;
                            NEXT = false;
                        }
                    }
                    if (stoprun)
                    {
                        Thread.Sleep(3000);
                        firstplace = trackbarValue;
                        Thread.Sleep(2000);
                        secondplace = trackbarValue;
                        if (secondplace - firstplace != 0 && runup != true && rundown != true)
                        {
                            textBox1.Invoke(settextbox, "Inspection Trip 故障 进入检修模式无操作但电梯没有静止");
                            inspectiontrip = false;
                            error2++;
                            NEXT = false;
                        }
                    }
                    if (goground)
                    {
                        Thread.Sleep(1000);
                        firstplace = trackbarValue;
                        Thread.Sleep(13000);
                        secondplace = trackbarValue;
                        if (secondplace - firstplace == 0)
                        {
                            if (trackbarValue <= 900 || trackbarValue >= 1900)
                            {
                                textBox1.Invoke(settextbox, "Synchronization Trip 故障 退出检修模式后电梯没有同步至底楼");
                                inspectiontrip = false;
                                NEXT = false;
                            }
                            else if (trackbarValue > 900 && trackbarValue < 1900)
                            {
                                textBox1.Invoke(settextbox, "Synchronization Trip PASS");
                                inspectiontrip = false;
                            }
                        }
                    }
                }
                if (carcalltrip)                                                   //Car Call Trip Error
                {
                    //if (Third == true)
                    //{
                    //    Thread.Sleep(5000);
                    //    firstplace = trackbarValue;
                    //    Thread.Sleep(2500);
                    //    secondplace = trackbarValue;
                    //    if (trackbarValue<40000 && (firstplace - secondplace == 0))
                    //    {

                    //    }
                    //}





                    //firstplace = trackbarValue;
                    //Thread.Sleep(2500);
                    //secondplace = trackbarValue;
                    //if (firstplace - secondplace == 0)
                    //{
                    //    if (First == false && Second == false && Third == true && (trackbarValue >= 83000 || trackbarValue <= 82000))
                    //    {
                    //        Third = false;
                    //        textBox1.Invoke(settextbox, "Car Call Trip 故障 选择三楼后电梯没运行到三楼指定位置停止");
                    //        carcalltrip = false;
                    //        error3++;
                    //        NEXT = false;
                    //    }
                    //    if (First == false && Second == true && Third == false && (trackbarValue >= 42300 || trackbarValue <= 41300))
                    //    {
                    //        Second = false;
                    //        textBox1.Invoke(settextbox, "Car Call Trip 故障 选择二楼后电梯没运行到二楼指定位置停止");
                    //        carcalltrip = false;
                    //        error3++;
                    //        NEXT = false;
                    //    }
                    //    if (First == true && Second == false && Third == false && (trackbarValue >= 1900 || trackbarValue <= 900))
                    //    {
                    //        First = false;
                    //        textBox1.Invoke(settextbox, "Car Call Trip 故障 选择一楼后电梯没运行到一楼指定位置停止");
                    //        carcalltrip = false;
                    //        error3++;
                    //        NEXT = false;
                    //    }
                    //}



                    //if (Second == true)
                    //{
                    //    if (trackbarValue >= 42300 && Third==false)
                    //    {
                    //        firstplace = trackbarValue;
                    //        Thread.Sleep(2500);
                    //        secondplace = trackbarValue;
                    //        if (firstplace - secondplace >= 0)
                    //        {
                    //            if (trackbarValue < 41300)
                    //            {
                    //                if (SecondDoor == false)
                    //                {
                    //                    Second = false;
                    //                    textBox1.Invoke(settextbox, "Car Call Trip 故障 选择二楼后电梯没运行到二楼指定位置停止");
                    //                    carcalltrip = false;
                    //                    error3++;
                    //                    NEXT = false;
                    //                }
                    //                else
                    //                {
                    //                    Second = false;
                    //                    SecondDoor = false;
                    //                }
                    //            }
                    //            else if (SecondDoor == true)
                    //            {
                    //                Second = false;
                    //                SecondDoor = false;
                    //            }
                    //        }
                    //        else
                    //        {
                    //            Second = false;
                    //            textBox1.Invoke(settextbox, "Car Call Trip 故障 选择二楼后电梯没运行到二楼指定位置停止");
                    //            carcalltrip = false;
                    //            error3++;
                    //            NEXT = false;
                    //        }
                    //    }
                    //    if (trackbarValue <= 41300 && First==false)
                    //    {
                    //        firstplace = trackbarValue;
                    //        Thread.Sleep(2500);
                    //        secondplace = trackbarValue;
                    //        if (secondplace - firstplace >= 0)
                    //        {
                    //            if (trackbarValue > 42300)
                    //            {
                    //                if (SecondDoor == false)
                    //                {
                    //                    Second = false;
                    //                    textBox1.Invoke(settextbox, "Car Call Trip 故障 选择二楼后电梯没运行到二楼指定位置停止");
                    //                    carcalltrip = false;
                    //                    error3++;
                    //                    NEXT = false;
                    //                }
                    //                else
                    //                {
                    //                    Second = false;
                    //                    SecondDoor = false;
                    //                }
                    //            }
                    //            else if(SecondDoor==true)
                    //            {
                    //                Second = false;
                    //                SecondDoor = false;
                    //            }
                    //        }
                    //        else
                    //        {
                    //            Second = false;
                    //            textBox1.Invoke(settextbox, "Car Call Trip 故障 选择二楼后电梯没运行到二楼指定位置停止");
                    //            carcalltrip = false;
                    //            error3++;
                    //            NEXT = false;
                    //        }
                    //    }
                    //}
                    //if (First == true)
                    //{
                    //    firstplace = trackbarValue;
                    //    Thread.Sleep(2500);
                    //    secondplace = trackbarValue;
                    //    if (firstplace - secondplace > 0)
                    //    {
                    //        if (trackbarValue < 600)
                    //        {
                    //            if(FirstDoor==false)
                    //            {
                    //                First = false;
                    //                textBox1.Invoke(settextbox, "Car Call Trip 故障 选择一楼后电梯没运行到一楼指定位置停止");
                    //                carcalltrip = false;
                    //                error3++;
                    //                NEXT = false;
                    //            }
                    //            else
                    //            {
                    //                First = false;
                    //                FirstDoor = false;
                    //            }
                    //        }
                    //        else if(FirstDoor==true)
                    //        {
                    //            First = false;
                    //            FirstDoor = false;
                    //        }
                    //    }
                    //    else
                    //    {
                    //        First = false;
                    //        textBox1.Invoke(settextbox, "Car Call Trip 故障 选择一楼后电梯没运行到一楼指定位置停止");
                    //        carcalltrip = false;
                    //        error3++;
                    //        NEXT = false;
                    //    }
                    //}
                    //if (Third == true)
                    //{
                    //    firstplace = trackbarValue;
                    //    Thread.Sleep(2500);
                    //    secondplace = trackbarValue;
                    //    if (secondplace - firstplace > 0)
                    //    {
                    //        if (trackbarValue >83000)
                    //        {
                    //            if (ThirdDoor == false)
                    //            {
                    //                Third = false;
                    //                textBox1.Invoke(settextbox, "Car Call Trip 故障 选择三楼后电梯没运行到三楼指定位置停止");
                    //                carcalltrip = false;
                    //                error3++;
                    //                NEXT = false;
                    //            }
                    //            else
                    //            {
                    //                Third = false;
                    //                ThirdDoor = false;
                    //            }
                    //        }
                    //        else if (ThirdDoor==true)
                    //        {
                    //            Third = false;
                    //            ThirdDoor = false;
                    //        }
                    //    }
                    //    else
                    //    {
                    //        Third = false;
                    //        textBox1.Invoke(settextbox, "Car Call Trip 故障 选择三楼后电梯没运行到三楼指定位置停止");
                    //        carcalltrip = false;
                    //        error3++;
                    //        NEXT = false;
                    //    }
                    //}
                }
            }
        }

        private void Running()                                                          //运行测试专属线程
        {
            SetTextCallback settextbox = new SetTextCallback(SetText);

            //if (PLCPrm[7] == 1)
            //{
            //    MessageShow messageshow = new MessageShow("请确认控制柜内所有的断路器在断开状态, ECB印板没有任何插件" + "\n\n" + "然后" + "\n" + "(1)请插上短接端子： XPSU_B、 XSPH、 XKNE、 XKBV、 XKTHMH、 XJH、 XTHMR" + "\n\n" + "(2)请插上从控制柜元器件来的端子： XMAIN、 XRKPH、 XPSU_E、 X24PS、 XJTHS" + "\n\n" + "(3)请插上测试设备红色线束的端子： XESE、 XVF、 XTC1、 XTC2、 XISPT、 XKV、 XCAN_EXT、RS232串口线" + "\n\n" + "(4)请接 JTHS 夹具" + "\n\n" + "(5)请把 JTHS 闭合");
            //    messageshow.ShowDialog();
            //}
            //if (PLCPrm[8] == 1)
            //{
            //    MessageShow messageshow = new MessageShow("请确认控制柜内所有的断路器在断开状态, ECB印板没有任何插件" + "\n\n" + "然后" + "\n" + "(1)请插上短接端子： XPSU_B、 XSPH、 XKNE、 XKBV、 XKTHMH、 XJH、 XTHMR、 XCTB、 XCTD" + "\n\n" + "(2)请插上从控制柜元器件来的端子： XMAIN、 XRKPH、 XPSU_E、 X24PS、 XJTHS、 XCT" + "\n\n" + "(3)请插上测试设备红色线束的端子： XESE、 XVF、 XTC1、 XTC2、 XISPT、 XKV、 XCAN_EXT、RS232串口线" + "\n\n" + "(4)请接 X1 夹具" + "\n\n" + "(5)请把 JTHS 闭合");
            //    messageshow.ShowDialog();
            //}
            
            int firstplace = 0;
            int secondplace = 0;
            SHOWgroupbox2 show2 = new SHOWgroupbox2(ShowGroupBox2);
            SHOWgroupbox3 show3 = new SHOWgroupbox3(ShowGroupBox3);
            SHOWbutton16 show16 = new SHOWbutton16(ShowButton16);
            SHOWbutton7 show7 = new SHOWbutton7(ShowButton7);


            //groupBox2.Visible = false;                                                  //PowerOn
            //groupBox3.Visible = false;
            button1.BackColor = Color.LightSeaGreen;

            string a = ": 01 05 08 13 FF 00";
            string b = GetLRC(a);
            byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
            serialPort1.Write(message1, 0, b.Length);

            Thread.Sleep(1000);
            a = ": 01 05 09 14 FF 00";
            b = GetLRC(a);
            message1 = System.Text.Encoding.ASCII.GetBytes(b);
            serialPort1.Write(message1, 0, b.Length);


            tally = true;  //开启信号灯线程
            T = new Thread(Tally);
            T.Start();

            encoder = true; //开启编码器读数线程
            RE = new Thread(ReadEconder);
            RE.Start();


            Thread.Sleep(25000);
            //MessageBox.Show("请等待液晶屏显示 [    53] 后" + "\n\n" + "然后按确认");
            MessageBox.Show("请等待液晶屏显示 [    53] 后" + "\n\n" + "再将 107 设为 1" + "\n" + "   将 106 设为 1" + "\n" + "   将 116 设为 1" + "\n\n" + "然后按确认");

            button1.BackColor = Color.FromKnownColor(KnownColor.Control);//Color.LightGray;                                //Check Version
            button13.BackColor = Color.LightSeaGreen;

            while ((HWversion.Contains(Properties.Settings.Default.HardwareSetting) == false) || (SWversion.Contains(Properties.Settings.Default.SoftwareSetting) == false))
            {
                a = "53 43 49 43 5F 49 44 45 4E 54 49 46 59 5F 48 57 3A 3D 31 0D";
                string[] aa1 = a.Split(' ');
                message1 = new byte[aa1.Length];
                int s = aa1.Length;
                for (int i = 0; i < aa1.Length; i++)
                {
                    message1[i] = Convert.ToByte(aa1[i], 16);
                }
                serialPort2.Write(message1, 0, s);
                Thread.Sleep(500);

                a = "53 43 49 43 5F 49 44 45 4E 54 49 46 59 5F 53 57 3A 3D 31 0D";
                aa1 = a.Split(' ');
                message1 = new byte[aa1.Length];
                s = aa1.Length;
                for (int i = 0; i < aa1.Length; i++)
                {
                    message1[i] = Convert.ToByte(aa1[i], 16);
                }
                serialPort2.Write(message1, 0, s);
                Thread.Sleep(1500);
                if (HWversion.Trim().Contains(Properties.Settings.Default.HardwareSetting.Trim()) == false)
                {
                    MessageBox.Show("要求硬件版本为： " + Properties.Settings.Default.HardwareSetting + "\n\n" + "实际硬件版本为： " + HWversion);
                }
                if (SWversion.Trim().Contains(Properties.Settings.Default.SoftwareSetting.Trim()) == false)
                {
                    MessageBox.Show("要求软件版本为： " + Properties.Settings.Default.SoftwareSetting + "\n\n" + "实际软件版本为： " + SWversion);
                }                
            }

            button13.BackColor = Color.FromKnownColor(KnownColor.Control);
            button3.BackColor = Color.LightSeaGreen;

           // MessageBox.Show("请等待液晶屏显示 [    53] 后" + "\n\n" + "再将 107 设为 1" + "\n" + "   将 106 设为 1" + "\n" + "   将 116 设为 1" + "\n\n" + "然后按确认");
            encoder = false;
            Thread.Sleep(500);
            encoder = false;
            Thread.Sleep(500);
            a = ": 01 05 09 16 00 00";                                                //Out of Insp
            b = GetLRC(a);
            message1 = System.Text.Encoding.ASCII.GetBytes(b);
            serialPort1.Write(message1, 0, b.Length);

            encoder = true;
            Thread.Sleep(500);
            encoder = true;
            Thread.Sleep(500);
            encoder = true;

            CheckRun = new Thread(RightOrWrong);
            CheckRun.IsBackground = true;
            CheckRun.Start();
            
            inspectiontrip = false;
            carcalltrip = false;

            Thread.Sleep(1000);
            learningtrip = true;


            Thread.Sleep(50000);


            //button1.BackColor = Color.LightGray;                                             //Check Version
            //button2.BackColor = Color.LightSeaGreen;
            //Thread.Sleep(5000);


            Thread.Sleep(76000);

            while (true)
            {
                if (MessageBox.Show("请确认 自学习 是否完毕", "提示", MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    break;
                }
                Thread.Sleep(3000);
            }
            learningtrip = false;


            button3.BackColor = Color.FromKnownColor(KnownColor.Control);
            button4.BackColor = Color.LightSeaGreen;
            groupBox2.Invoke(show2);
            groupBox3.Visible = false;
            encoder = false;
            Thread.Sleep(2000);
            a = ": 01 05 09 16 FF 00";                                           //Inspection Trip
            b = GetLRC(a);
            message1 = System.Text.Encoding.ASCII.GetBytes(b);
            serialPort1.Write(message1, 0, b.Length);
            button7.Invoke(show7);
            //button7.Visible = true;
            encoder = true;
            Thread.Sleep(500);
            encoder = true;
            Thread.Sleep(500);
            encoder = true;

            Thread.Sleep(1500);
            learningtrip = false;
            inspectiontrip = true;
            carcalltrip = false;


            Thread.Sleep(2000);


            encoder = false;
            Thread.Sleep(500);
            encoder = false;
            Thread.Sleep(500);
            a = ": 01 05 09 17 FF 00";                                           //Insp Up On  17
            b = GetLRC(a);
            message1 = System.Text.Encoding.ASCII.GetBytes(b);
            serialPort1.Write(message1, 0, b.Length);
            groupBox2.Invoke(show2);
            button8.ForeColor = Color.LightSeaGreen;
            Thread.Sleep(500);
            encoder = true;
            Thread.Sleep(500);
            encoder = true;

            Thread.Sleep(1500);
            runup = true;
            rundown = false;
            stoprun = false;
            goground = false;

            Thread.Sleep(14000);


            encoder = false;
            Thread.Sleep(500);
            encoder = false;
            Thread.Sleep(500);
            a = ": 01 05 09 17 00 00";                                           //Insp Up Off
            b = GetLRC(a);
            message1 = System.Text.Encoding.ASCII.GetBytes(b);
            serialPort1.Write(message1, 0, b.Length);
            button8.ForeColor = Color.Black;
            Thread.Sleep(500);
            encoder = true;
            Thread.Sleep(500);
            encoder = true;
            runup = false;
            Thread.Sleep(1500);
            //修改
            rundown = false;
            stoprun = true;
            goground = false;


            Thread.Sleep(2000);


            encoder = false;
            Thread.Sleep(500);
            encoder = false;
            Thread.Sleep(500);
            a = ": 01 05 09 18 FF 00";                                           //Insp Down On  18
            b = GetLRC(a);
            message1 = System.Text.Encoding.ASCII.GetBytes(b);
            serialPort1.Write(message1, 0, b.Length);
            button9.ForeColor = Color.LightSeaGreen;
            Thread.Sleep(500);
            encoder = true;
            Thread.Sleep(500);
            encoder = true;
            stoprun = false;

            Thread.Sleep(1500);
            runup = false;
            rundown = true;
            //stoprun = false;
            goground = false;


            Thread.Sleep(7000);


            encoder = false;
            Thread.Sleep(500);
            encoder = false;
            Thread.Sleep(500);
            a = ": 01 05 09 18 00 00";                                           //Insp Down Off
            b = GetLRC(a);
            message1 = System.Text.Encoding.ASCII.GetBytes(b);
            serialPort1.Write(message1, 0, b.Length);
            button9.ForeColor = Color.Black;
            Thread.Sleep(500);
            encoder = true;
            Thread.Sleep(500);
            encoder = true;

            rundown = false;
            Thread.Sleep(1500);
            runup = false;
            stoprun = true;
            goground = false;


            Thread.Sleep(4000);


            encoder = false;
            Thread.Sleep(500);
            a = ": 01 05 09 16 00 00";                                           //Out of Insp
            b = GetLRC(a);
            message1 = System.Text.Encoding.ASCII.GetBytes(b);
            serialPort1.Write(message1, 0, b.Length);
            button16.Invoke(show16);
            Thread.Sleep(500);
            encoder = true;
            Thread.Sleep(500);
            encoder = true;

            stoprun = false;

            Thread.Sleep(16000);
            firstplace = trackbarValue;
            Thread.Sleep(2000);
            secondplace = trackbarValue;
            if ((firstplace - secondplace) <= 0)
            {
                encoder = false;
                Thread.Sleep(500);
                encoder = false;
                Thread.Sleep(500);
                a = ": 01 05 09 16 FF 00";                                           //Inspection Trip
                b = GetLRC(a);
                message1 = System.Text.Encoding.ASCII.GetBytes(b);
                serialPort1.Write(message1, 0, b.Length);

                Thread.Sleep(2000);
                a = ": 01 05 09 16 00 00";                                           //Out of Insp
                b = GetLRC(a);
                message1 = System.Text.Encoding.ASCII.GetBytes(b);
                serialPort1.Write(message1, 0, b.Length);
                Thread.Sleep(500);
                encoder = true;
                Thread.Sleep(500);
                encoder = true;
                Thread.Sleep(15000);
            }

            runup = false;
            rundown = false;
            stoprun = false;
            goground = true;


            Thread.Sleep(23000);

            if (error2 == 0)
            {
                //SetTextCallback settextbox = new SetTextCallback(SetText);
                textBox1.Invoke(settextbox, "Inspection Trip PASS");
            }
            error2 = 0;
            inspectiontrip = false;


            button4.BackColor = Color.FromKnownColor(KnownColor.Control);
            button6.BackColor = Color.LightSeaGreen;
            //groupBox2.Visible = false;
            //groupBox3.Visible = true;
            groupBox3.Invoke(show3);
            encoder = false;
            Thread.Sleep(500);
            a = ": 01 05 09 1A FF 00";                                           //Car Call Trip
            b = GetLRC(a);
            message1 = System.Text.Encoding.ASCII.GetBytes(b);
            serialPort1.Write(message1, 0, b.Length);
            Thread.Sleep(500);
            encoder = true;
            Thread.Sleep(500);
            encoder = true;
            Door = new Thread(OpenCloseDoor);
            Door.IsBackground = true;
            Door.Start();
            //Call = new Thread(CarCall);
            //Call.IsBackground = true;
            //Call.Start();


            Thread.Sleep(5000);

            learningtrip = false;
            inspectiontrip = false;
            carcalltrip = true;
            goground = false;

            encoder = false;
            Thread.Sleep(500);
            a = ": 01 05 09 1D FF 00";                                           //3F  1D
            b = GetLRC(a);
            message1 = System.Text.Encoding.ASCII.GetBytes(b);
            serialPort1.Write(message1, 0, b.Length);
            button10.ForeColor = Color.LightSeaGreen;
            floor3 = true;
            encoder = true;
            Thread.Sleep(500);
            encoder = true;
            Thread.Sleep(500);
            encoder = true;
            First = false;
            Second = false;
            Thread.Sleep(2000);
            Third = true;


            Thread.Sleep(20000);
            firstplace = trackbarValue;
            Thread.Sleep(2500);
            secondplace = trackbarValue;
            if ((firstplace - secondplace == 0) || trackbarValue < 24000)
            {
                Thread.Sleep(3000);
                encoder = false;
                Thread.Sleep(500);
                a = ": 01 05 09 1D FF 00";                                           //3F  1D
                b = GetLRC(a);
                message1 = System.Text.Encoding.ASCII.GetBytes(b);
                serialPort1.Write(message1, 0, b.Length);
                //button10.ForeColor = Color.LightSeaGreen;
                floor3 = true;
                encoder = true;
                Thread.Sleep(500);
                encoder = true;
                Thread.Sleep(500);
                encoder = true;
                Thread.Sleep(12000);
            }
 
            Thread.Sleep(22000);
            if (trackbarValue > 84000)
            {
                Third = false;
                textBox1.Invoke(settextbox, "Car Call Trip 故障 电梯上行 越界");
                carcalltrip = false;
                error3++;
                NEXT = false;
            }
            else if (trackbarValue < 78000)
            {
                Third = false;
                textBox1.Invoke(settextbox, "Car Call Trip 故障 选择三楼后电梯没运行到三楼指定位置停止");
                carcalltrip = false;
                error3++;
                NEXT = false;
            }
            else if (ThirdDoor == true)
            {
                ThirdDoor = false;
                Third = false;
                carcalltrip = false;
            }



            //encoder = false;
            //Thread.Sleep(500);
            encoder = false;
            Thread.Sleep(500);
            a = ": 01 05 09 1B FF 00";                                           //1F
            b = GetLRC(a);
            message1 = System.Text.Encoding.ASCII.GetBytes(b);
            serialPort1.Write(message1, 0, b.Length);
            button12.ForeColor = Color.LightSeaGreen;
            floor1 = true;
            encoder = true;
            Thread.Sleep(500);
            encoder = true;
            Thread.Sleep(500);
            encoder = true;
            
            Second = false;
            Third = false;
            Thread.Sleep(2000);
            First = true;


            Thread.Sleep(38000);

            if (trackbarValue < 800)
            {
                First = false;
                textBox1.Invoke(settextbox, "Car Call Trip 故障 电梯下行 越界");
                carcalltrip = false;
                error3++;
                NEXT = false;
            }
            else if (trackbarValue > 2500)
            {
                First = false;
                textBox1.Invoke(settextbox, "Car Call Trip 故障 选择一楼后电梯没运行到一楼指定位置停止");
                carcalltrip = false;
                error3++;
                NEXT = false;
            }
            else if (FirstDoor == true)
            {
                FirstDoor = false;
                First = false;
                carcalltrip = false;
            }

            //encoder = false;
            //Thread.Sleep(500);
            //a = ": 01 05 09 1C FF 00";                                           //2F
            //b = GetLRC(a);
            //message1 = System.Text.Encoding.ASCII.GetBytes(b);
            //serialPort1.Write(message1, 0, b.Length);
            //button11.ForeColor = Color.LightSeaGreen;
            //floor2 = true;
            //Thread.Sleep(500);
            //encoder = true;
            //First = false;
            
            //Third = false;
            //Thread.Sleep(2000);
            //Second = true;
            //Thread.Sleep(28000);



            if (error3 == 0)
            {
                //SetTextCallback settextbox = new SetTextCallback(SetText);
                textBox1.Invoke(settextbox, "Car Call Trip PASS");
            }
            error3 = 0;
            First = false;
            Second = false;
            Third = false;

            button6.BackColor = Color.FromKnownColor(KnownColor.Control);                                //Erase EEPROM
            button20.BackColor = Color.LightSeaGreen;

            a = "47 43 5F 45 52 41 53 45 5F 45 45 3A 3D 31 0D";
            string[] aa = a.Split(' ');
            byte[] message = new byte[aa.Length];
            int s1 = aa.Length;
            for (int i = 0; i < aa.Length; i++)
            {
                message[i] = Convert.ToByte(aa[i], 16);
            }
            serialPort2.Write(message, 0, s1);     


            Thread.Sleep(5000);

            //MessageBox.Show("    测试结束");

            //Form AutoReport = new AutoReport(NEXT, PLCCom);
            //AutoReport.Show();     
            serialPort1.Close();

            button20.BackColor = Color.FromKnownColor(KnownColor.Control);                                //PowerOff
            button5.BackColor = Color.LightSeaGreen;

            EnableButton ebutton = new EnableButton(enablebutton);
            button19.Invoke(ebutton);

            //button20.BackColor = Color.LightGray;
            //button5.BackColor = Color.LightSeaGreen;
            //encoder = false;
            //Thread.Sleep(500);
            //a = ": 01 05 09 14 00 00";                                          //PowerOff
            //b = GetLRC(a);
            //message1 = System.Text.Encoding.ASCII.GetBytes(b);
            //serialPort1.Write(message1, 0, b.Length);
            //tally = false;
            //encoder = false;
            //this.Refresh();
            Thread.CurrentThread.Abort();
            Thread.Sleep(500);
            Thread.CurrentThread.Abort();
        }

        private void button19_Click(object sender, EventArgs e)                         //开始测试
        {
            button19.Enabled = false;
            try
            {
                T.Abort();
                RE.Abort();
                CheckRun.Abort();
                Door.Abort();
            }
            catch { }
            button1.BackColor = Color.FromKnownColor(KnownColor.Control);
            button13.BackColor = Color.FromKnownColor(KnownColor.Control);
            button3.BackColor = Color.FromKnownColor(KnownColor.Control);
            button4.BackColor = Color.FromKnownColor(KnownColor.Control);
            button6.BackColor = Color.FromKnownColor(KnownColor.Control);
            button20.BackColor = Color.FromKnownColor(KnownColor.Control);
            button5.BackColor = Color.FromKnownColor(KnownColor.Control);

            groupBox2.Visible = false;                                                 
            groupBox3.Visible = false;

            SetTextCallback settextbox = new SetTextCallback(SetText);
            textBox1.Invoke(settextbox, "—————————————————————");       
            R = new Thread(Running);
            R.Start(); 
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                T.Abort();
                RE.Abort();
                CheckRun.Abort();
                Door.Abort();
            }
            catch { }
            R.Abort();
            HWversion = "";
            SWversion = "";
            string a = ": 01 05 09 14 00 00";                                          //PowerOff
            string b = GetLRC(a);
            byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
            serialPort1.Write(message1, 0, b.Length);
            //serialPort1.Close();
            this.button19.Enabled = true;
            this.Refresh();
        }

        private void button17_Click(object sender, EventArgs e)
        {
            //opening = new Thread(OpenTheDoor);
            //opening.IsBackground = true;
            //opening.Start();
        }

        private void button18_Click(object sender, EventArgs e)
        {
            //closeing = new Thread(CloseTheDoor);
            //closeing.IsBackground = true;
            //closeing.Start();
        }

        private void button15_Click(object sender, EventArgs e)
        {
            tally = false;
            encoder = false;
            try
            {
                T.Abort();
                RE.Abort();
                CheckRun.Abort();
                Door.Abort();
            }
            catch { }
            if (MessageBox.Show("请确认是否关闭", "提示", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                Send = new Thread(new ParameterizedThreadStart(SentToPLC));
                Send.IsBackground = true;
                Send.Start("14 00");
                Thread.Sleep(500);
                this.Close();
            }
            //Send = new Thread(new ParameterizedThreadStart(SentToPLC));
            //Send.IsBackground = true;
            //Send.Start("14 00");
            //Thread.Sleep(1000);
            //this.Close();
        }

        #region button_Click
        //private void button1_Click(object sender, EventArgs e)                          //Power On
        //{
        //    groupBox2.Visible = false;
        //    groupBox3.Visible = false;
        //    //string a = ": 01 05 09 14 FF 00";
        //    //string b = GetLRC(a);
        //    //byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
        //    //serialPort1.Write(message1, 0, b.Length);

        //    Send = new Thread(new ParameterizedThreadStart(SentToPLC));
        //    Send.IsBackground = true;
        //    Send.Start("14 FF");

        //    tally = true;
        //    Thread T = new Thread(Tally);
        //    T.Start();

        //    encoder = true;
        //    Thread RE = new Thread(ReadEconder);
        //    RE.Start();
        //}

        //private void button5_Click(object sender, EventArgs e)                          //Power Off
        //{
        //    groupBox2.Visible = false;
        //    groupBox3.Visible = false;

        //    Send = new Thread(new ParameterizedThreadStart(SentToPLC));
        //    Send.IsBackground = true;
        //    Send.Start("14 00");

        //    tally = false;          
        //    encoder = false;
        //}

        //private void button2_Click(object sender, EventArgs e)                          //Version Check
        //{
        //    groupBox2.Visible = false;
        //    groupBox3.Visible = false;
        //}

        //private void button3_Click(object sender, EventArgs e)                          //Learning Trip
        //{
        //    groupBox2.Visible = false;
        //    groupBox3.Visible = false;

        //    Send = new Thread(new ParameterizedThreadStart(SentToPLC));
        //    Send.IsBackground = true;
        //    Send.Start("15 FF");
        //}                   

        //private void button4_Click(object sender, EventArgs e)                          //Inspection Trip
        //{
        //    groupBox2.Visible = true;
        //    groupBox3.Visible = false;
        //}

        //private void button7_Click(object sender, EventArgs e)                          //退出检修
        //{
        //    button7.Visible = false;
        //    button16.Visible = true;
        //    button8.Enabled = false;
        //    button9.Enabled = false;

        //    Send = new Thread(new ParameterizedThreadStart(SentToPLC));
        //    Send.IsBackground = true;
        //    Send.Start("16 00");
        //}

        //private void button16_Click(object sender, EventArgs e)                         //进入检修
        //{
        //    button7.Visible = true;
        //    button16.Visible = false;
        //    button8.Enabled = true;
        //    button9.Enabled = true;

        //    Send = new Thread(new ParameterizedThreadStart(SentToPLC));
        //    Send.IsBackground = true;
        //    Send.Start("16 FF");
        //}

        //private void button8_MouseDown(object sender, MouseEventArgs e)                 //检修模式上行按下
        //{
        //    Send = new Thread(new ParameterizedThreadStart(SentToPLC));
        //    Send.IsBackground = true;
        //    Send.Start("17 FF");
        //    button8.ForeColor = Color.LightSeaGreen;
        //}

        //private void button8_MouseUp(object sender, MouseEventArgs e)                   //检修模式上行松开
        //{
        //    Send = new Thread(new ParameterizedThreadStart(SentToPLC));
        //    Send.IsBackground = true;
        //    Send.Start("17 00");
        //    button8.ForeColor = Color.Black;
        //}

        //private void button9_MouseDown(object sender, MouseEventArgs e)                 //检修模式下行按下
        //{
        //    Send = new Thread(new ParameterizedThreadStart(SentToPLC));
        //    Send.IsBackground = true;
        //    Send.Start("18 FF");
        //    button9.ForeColor = Color.LightSeaGreen;
        //}

        //private void button9_MouseUp(object sender, MouseEventArgs e)                   //检修模式下行松开
        //{
        //    Send = new Thread(new ParameterizedThreadStart(SentToPLC));
        //    Send.IsBackground = true;
        //    Send.Start("18 00");
        //    button9.ForeColor = Color.White;
        //}

        //private void button6_Click(object sender, EventArgs e)                          //Car Call Trip
        //{
        //    groupBox2.Visible = false;
        //    groupBox3.Visible = true;
        //    Send = new Thread(new ParameterizedThreadStart(SentToPLC));
        //    Send.IsBackground = true;
        //    Send.Start("1A FF");

        //    Door = new Thread(OpenCloseDoor);
        //    Door.IsBackground = true;
        //    Door.Start();
        //}

        //private void button10_Click(object sender, EventArgs e)                         //楼层呼叫3楼
        //{
        //    Send = new Thread(new ParameterizedThreadStart(SentToPLC));
        //    Send.IsBackground = true;
        //    Send.Start("1D FF");
        //    button10.ForeColor = Color.LightSeaGreen;
        //    floor3 = true;
        //}

        //private void button11_Click(object sender, EventArgs e)                         //楼层呼叫2楼
        //{
        //    Send = new Thread(new ParameterizedThreadStart(SentToPLC));
        //    Send.IsBackground = true;
        //    Send.Start("1C FF");
        //    button11.ForeColor = Color.LightSeaGreen;
        //    floor2 = true;
        //}

        //private void button12_Click(object sender, EventArgs e)                         //楼层呼叫1楼
        //{
        //    Send = new Thread(new ParameterizedThreadStart(SentToPLC));
        //    Send.IsBackground = true;
        //    Send.Start("1B FF");
        //    button12.ForeColor = Color.LightSeaGreen;
        //    floor1 = true;
        //}

        //private void button13_Click(object sender, EventArgs e)                         //楼层呼叫开门
        //{
        //    button13.ForeColor = Color.LightSeaGreen;
        //    Send = new Thread(new ParameterizedThreadStart(SentToPLC));
        //    Send.IsBackground = true;
        //    Send.Start("1E FF");            
        //    floor1 = true;
        //    floor2 = true;
        //    floor3 = true;
        //}

        //private void button14_Click(object sender, EventArgs e)                         //楼层呼叫关门
        //{
        //    button14.ForeColor = Color.LightSeaGreen;
        //    Send = new Thread(new ParameterizedThreadStart(SentToPLC));
        //    Send.IsBackground = true;
        //    Send.Start("1F FF");
        //}

        //private void button15_Click(object sender, EventArgs e)                         //关闭
        //{
        //    tally = false;
        //    encoder = false;
        //    if (MessageBox.Show("请确认已按  PowerOff", "提示", MessageBoxButtons.OKCancel) == DialogResult.OK)
        //    {
        //        this.Close();
        //    }
        //    //Send = new Thread(new ParameterizedThreadStart(SentToPLC));
        //    //Send.IsBackground = true;
        //    //Send.Start("14 00");
        //}
        #endregion
    }
}
