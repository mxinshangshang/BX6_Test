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
    public partial class ManuF : Form
    {
        public int iTextbox1 = 0;
        FileStream myFs;
        StreamWriter mySw;
        string file;
        string PLCCom;
        string JobNum;

        string[,] PLCPrm1;

        string dataRE = null;
        string[] datare = new string[60];
        int iData = 0;

        protected override void WndProc(ref   Message m)
        {
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_CLOSE = 0xF060;
            if (m.Msg == WM_SYSCOMMAND && (int)m.WParam == SC_CLOSE)
            {
                return;
            }
            base.WndProc(ref m);
        }

        public ManuF(string file, string PLCCom, string[,] PLCPrm1,string jobnum)
        {
            InitializeComponent();

            this.file = file;
            this.PLCCom = PLCCom;
            this.PLCPrm1 = PLCPrm1;
            this.JobNum = jobnum;

            //ch372.Init();
            //ch372.OpenDevice(0);
            //ch372.SetTimeout(0, 3000, 3000);

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

            for (int i = 0; i < (PLCPrm1.Length / 16); i++)
            {
                ((CheckBox)this.Controls.Find("checkBox" + (i + 1), true)[0]).Checked = false;
            }

            int k = 0;
            for (int i = 0; i < ( PLCPrm1.Length / 16); i++)
            {
                ((CheckBox)this.Controls.Find("checkBox" + (i + 1), true)[0]).Text = PLCPrm1[k++, 0];
            }
        }

        #region txt & USB communication

        private delegate void SetTextCallback(string text);
        private void SetText(string text)
        {
            this.textBox1.Text += Environment.NewLine + text + "\r\n";//"\r\n";// "\r\n" + text;
            this.textBox1.SelectionStart = this.textBox1.Text.Length;
            this.textBox1.ScrollToCaret();

            string content = DateTime.Now.ToString() + " " + JobNum + " " + "手动功能测试 " + text + "\r\n";
            myFs = new FileStream(file, FileMode.Append, FileAccess.Write);
            mySw = new StreamWriter(myFs);
            mySw.Write(content);
            mySw.Close();
            myFs.Close();
        }
        //private void StartListen(object arry)
        //{
        //    int l = 0;
        //    int error = 0;
        //    string[,] Arry = arry as string[,];
        //    while (true)
        //    {
        //        byte[] buf = new byte[128];
        //        int len = 100;
        //        SetTextCallback settextbox = new SetTextCallback(SetText);
        //        System.Text.ASCIIEncoding asciiEncoding = new System.Text.ASCIIEncoding();
        //        if (ch372.ReadData(0, buf, ref len) == true && len != 0)
        //        {
        //            string message = asciiEncoding.GetString(buf);
        //            if (!this.IsHandleCreated) return;

        //            for (int k = 0; k < (Arry.Length / 3); k++)
        //            {
        //                if (message.Contains("OPEN") && message.Contains(Arry[k, 0]) && message.Contains(Arry[k, 1]))
        //                {
        //                    this.Invoke(new EventHandler(delegate
        //                    {
        //                        this.Controls["label" + (l + 1).ToString()].Text = Arry[k, 2];
        //                        l++;
        //                    }));
        //                    if (l == 32) l = 0;

        //                    textBox1.Invoke(settextbox, Arry[k, 2] + "开路");
        //                    error++;
        //                }
        //                else if (message.Contains("SHORT") && (message.Contains(Arry[k, 0]) || message.Contains(Arry[k, 1])))
        //                {
        //                    textBox1.Invoke(settextbox, Arry[k, 2] + "短路");
        //                    error++;
        //                }
        //            }
        //            if (error == 0) textBox1.Invoke(settextbox, PLCPrm1[num++, 1] + "Pass");
        //            textBox1.Invoke(settextbox, message);
        //        }
        //    }
        //}

        #endregion

        #region Port communication

        public delegate void DeleUpdateTextbox(string dataRe);
        private void UpdateTextbox(string dataRe)
        {
            datare[iData++] = dataRe;
            if (datare[iData - 1] == "0A " && datare[iData - 2] == "0D ")
            {
                dataRE = string.Join("", datare);
                textBox2.Text = dataRE;
                iData = 0;//0123修改
            }
        }

        //public delegate void DeleUpdateTextbox(byte dataRe);
        //private void UpdateTextbox(byte dataRe)
        //{
        //    datare[iData++] = dataRe;
        //    if (datare[iData - 1] == 10 && datare[iData - 2] == 13)
        //    {
        //        dataRE10 = new byte[iData];
        //        for (int i = 0; i < iData; i++)
        //        {
        //            dataRE10[i] = datare[i];
        //        }

        //        dataRE = string.Join("", datare);
        //        textBox2.Text = dataRE;
        //        iData = 0;//0123修改
        //    }
        //}

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
                dataRe = temp.ToString("X2")+" ";
                textBox2.Invoke(deleupdatetextbox, dataRe);
                //textBox2.Invoke(deleupdatetextbox, temp);
            }
        }

        #endregion

        #region EnableButton
        private delegate void EnableButton();
        private void enablebutton1()
        {
            this.button1.Enabled = true;
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

        private void Function()                                                         //功能测试专属线程
        {
            SetTextCallback settextbox = new SetTextCallback(SetText);

            #region Attention
            int A = 0;
            for (int i = 0; i < PLCPrm1.Length / 16; i++)
            {
                if (((CheckBox)this.Controls.Find("checkbox" + (i + 1), true)[0]).Checked == true)
                {
                    A++;
                }
            }
            string[] AttentionW = new string[A + 1];
            string[] AttentionB = new string[A + 1];

            if (checkBox11.Checked && checkBox12.Checked)
            {
                AttentionW[0] = "请把XCT线束连接至各模块";
                AttentionB[0] = "";
            }
            else if (checkBox11.Checked && checkBox12.Checked == false)
            {
                AttentionW[0] = "请把XCT、XCTD、XCTB线束连接至各模块";
                AttentionB[0] = "";
            }

            A = 1;
            for (int i = 0; i < PLCPrm1.Length / 16; i++)
            {
                if (((CheckBox)this.Controls.Find("checkbox" + (i + 1), true)[0]).Checked == true)
                {
                    AttentionW[A] = PLCPrm1[i, 14];
                    AttentionB[A++] = PLCPrm1[i, 15];
                }
            }
            //MessageBox.Show(string.Join("\n", AttentionW) + "\n\n" + string.Join("\n", AttentionB));
            MessageShow messageshow = new MessageShow(string.Join("\n\n", AttentionW) + "\n\n" + string.Join("\n\n", AttentionB));
            messageshow.ShowDialog();
            #endregion

            string a = ": 01 05 08 " + "25" + " FF 00";                 //发送直流矫正PLC指令
            string b = GetLRC(a);
            byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
            serialPort1.Write(message1, 0, b.Length);
            Thread.Sleep(8000);

            #region  foreach checkbox

            //foreach (Control o in Controls)
            //{
            //    if (o is CheckBox)
            //    {
            //        if (((CheckBox)o).Checked == true)
            //        {
            for (int I = 1; I <= 10; I++)
            {
                if (((CheckBox)this.Controls.Find("checkBox" + I, true)[0]).Checked == true)
                {
                        //int num = Convert.ToInt32(o.Name.Substring(8), 10) - 1;
                        int num = I - 1;
                        for (int i = 0; i < 6; i++)
                        {
                            if ((PLCPrm1[num, 6 + i]).Length != 0)
                            {
                                a = ": 01 05 08 " + PLCPrm1[num, 6 + i].Split(' ')[3] + " FF 00";       //发送PLC指令
                                b = GetLRC(a);
                                message1 = System.Text.Encoding.ASCII.GetBytes(b);
                                serialPort1.Write(message1, 0, b.Length);
                                Thread.Sleep(6500);
                                dataRE = "";

                                a = ": 01 03 12 " + PLCPrm1[num, 6 + i].Split(' ')[3] + " 00" + " 01";        //读取所需读取的电压的值
                                b = GetLRC(a);
                                message1 = System.Text.Encoding.ASCII.GetBytes(b);
                                serialPort1.Write(message1, 0, b.Length);
                                Thread.Sleep(1500);

                                string c = dataRE;// "3A 30 31 30 33 30 43 30 30 36 34 30 30 43 38 30 31 32 43 30 31 39 30 30 31 46 34 30 32 35 38 42 37 0D 0A ";
                                string ascii = c.Substring(21, 12 * 1).Replace(" ", "");
                                List<byte> buffer = new List<byte>();
                                for (int j = 0; j < ascii.Length; j += 2)
                                {
                                    string temp = ascii.Substring(j, 2);
                                    byte value = Convert.ToByte(temp, 16);
                                    buffer.Add(value);
                                }
                                string str = System.Text.Encoding.ASCII.GetString(buffer.ToArray());
                                string[] result = Regex.Split(str, @"(?<=\G.{4})(?!$)");//@"(?<=\G.{4})(?!$)"//(?<=\\G.{4})

                                if (Convert.ToInt32(result[0], 16) == 65436)
                                {
                                    textBox1.Invoke(settextbox, "发生急停，软件运行终止！");
                                    MessageBox.Show("发生急停！即将关闭软件！！", "提示");
                                    System.Diagnostics.Process.GetCurrentProcess().Kill();
                                    //if (MessageBox.Show("发生急停！即将关闭软件！！", "提示", MessageBoxButtons.OKCancel) == DialogResult.OK)
                                    //{
                                    //    System.Diagnostics.Process.GetCurrentProcess().Kill();
                                    //}
                                    //if (MessageBox.Show("发生急停！即将关闭软件！！", "提示", MessageBoxButtons.OKCancel) == DialogResult.Cancel)
                                    //{
                                    //    System.Diagnostics.Process.GetCurrentProcess().Kill();
                                    //}
                                }

                                for (int j = 0; j < result.Length; j++)
                                {
                                    if (Convert.ToDouble((PLCPrm1[num, 6 + i]).Split(' ')[0]) <= (Convert.ToDouble(Convert.ToInt32(result[j], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[num, 6 + i].Split(' ')[4])) && (Convert.ToInt32(Convert.ToInt32(result[j], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[num, 6 + i].Split(' ')[4])) <= Convert.ToDouble((PLCPrm1[num, 6 + i]).Split(' ')[1]))
                                    {
                                        textBox1.Invoke(settextbox, (PLCPrm1[num, 6 + i]).Split(' ')[2] + "合格 " + (Convert.ToDouble(Convert.ToInt32(result[j], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[num, 6 + i].Split(' ')[4])).ToString("0.0") + "V");
                                    }
                                    //textBox2.AppendText((PLCPrm1[3, 6 + i]).Split(' ')[2] + "合格 " + Convert.ToInt32(result[i], 16));if (Convert.ToInt32((PLCPrm1[num, 6 + j]).Split(' ')[0], 10) <= (Convert.ToInt32(Convert.ToInt32(result[j], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[num, 12])) && (Convert.ToInt32(Convert.ToInt32(result[j], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[num, 12])) <= Convert.ToInt32((PLCPrm1[num, 6 + j]).Split(' ')[1], 10))
                                    else textBox1.Invoke(settextbox, (PLCPrm1[num, 6 + i]).Split(' ')[2] + "不合格 " + (Convert.ToDouble(Convert.ToInt32(result[j], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[num, 6 + i].Split(' ')[4])).ToString("0.0") + "V");
                                    //textBox2.AppendText((PLCPrm1[3, 6 + i]).Split(' ')[2] + "不合格 " + Convert.ToInt32(result[i], 16));
                                }
                                Thread.Sleep(5000);
                            }
                        }
                    //}
                }
            }
            #endregion

            MessageBox.Show("测试结束");
            EnableButton ebutton1 = new EnableButton(enablebutton1);
            button1.Invoke(ebutton1);
            Thread.CurrentThread.Abort();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            SetTextCallback settextbox = new SetTextCallback(SetText);
            textBox1.Invoke(settextbox, "——————————————————————");
            Thread F = new Thread(Function);
            F.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void checkBox11_CheckedChanged(object sender, EventArgs e)
        {
            //if (checkBox11.Checked == true)
            //{
            //    for (int i = 0; i < PLCPrm1.Length / 16; i++)
            //    {
            //        ((CheckBox)this.Controls.Find("checkBox" + (i + 1), true)[0]).Checked = true;
            //    }
            //}
            //if (checkBox11.Checked != true)
            //{
            //    for (int i = 0; i < PLCPrm1.Length / 16; i++)
            //    {
            //        ((CheckBox)this.Controls.Find("checkBox" + (i + 1), true)[0]).Checked = false;
            //    }
            //}
        }

        //private void button3_Click(object sender, EventArgs e)
        //{
        //    SetTextCallback settextbox = new SetTextCallback(SetText);

        //    string a = ": 01 03 12 " + "27" + " 00 01";        //读取所需读取的电压的值
        //    string b = GetLRC(a);
        //    byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
        //    serialPort1.Write(message1, 0, b.Length);
        //    Thread.Sleep(3500);

        //    string c = dataRE;// "3A 30 31 30 33 30 32 30 39 42 30 34 31 0D 0A ";
        //    string ascii = c.Substring(21, 12 * 1).Replace(" ", "");
        //    List<byte> buffer = new List<byte>();
        //    for (int j = 0; j < ascii.Length; j += 2)
        //    {
        //        string temp = ascii.Substring(j, 2);
        //        byte value = Convert.ToByte(temp, 16);
        //        buffer.Add(value);
        //    }
        //    string str = System.Text.Encoding.ASCII.GetString(buffer.ToArray());
        //    string[] result = Regex.Split(str, @"(?<=\G.{4})(?!$)");//@"(?<=\G.{4})(?!$)"//(?<=\\G.{4})

        //    for (int j = 0; j < result.Length; j++)
        //    {
        //        textBox1.Invoke(settextbox, Convert.ToInt32(result[j], 16).ToString());
        //    }
        //}
    }
}
