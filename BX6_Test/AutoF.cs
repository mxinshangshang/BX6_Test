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
    public partial class AutoF : Form
    {
        public int iTextbox1 = 0;
        FileStream myFs;
        StreamWriter mySw;
        string file;
        string PLCCom;
        string TELECom;
        string Contract;
        string JobNum;

        string[,] PLCPrm1;
        string[,] PLCPrm2;
        int[] PLCPrm = new int[13];

        string dataRE = null;
        string[] datare = new string[60];
        int iData = 0;
        bool NEXT = true;

        protected override void WndProc(ref   Message m)            //禁用右上角关闭按钮
        {
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_CLOSE = 0xF060;
            if (m.Msg == WM_SYSCOMMAND && (int)m.WParam == SC_CLOSE)
            {
                return;
            }
            base.WndProc(ref m);
        }

        public AutoF(string file, string PLCCom, string[,] PLCPrm1, string[,] PLCPrm2,string contract,string jobnum,int[] PLCPrm,string TELECom)
        {
            InitializeComponent();

            this.file = file;
            this.PLCCom = PLCCom;
            this.TELECom = TELECom;
            this.PLCPrm1 = PLCPrm1;
            this.PLCPrm2 = PLCPrm2;
            this.Contract = contract;
            this.JobNum = jobnum;
            this.PLCPrm = PLCPrm;

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

            for (int i = 0; i < 5; i++)
            {
                ((Label)this.Controls.Find("label" + (i + 1), true)[0]).Visible = false;
            }

            int l = 0;
            for (int i = 0; i < ( PLCPrm1.Length / 16); i++)
            {
                ((Label)this.Controls.Find("label" + (i + 1), true)[0]).Visible = true;
                ((Label)this.Controls.Find("label" + (i + 1), true)[0]).Text = PLCPrm1[l++, 0];
            }
        }

        #region txt & USB communication

        private delegate void SetTextCallback(string text);
        private void SetText(string text)
        {
            //if (iTextbox1 == 0)
            //{
            //    this.textBox1.Text = text;
            //    iTextbox1++;
            //}
            //else
            //{
                //textBox1.AppendText(text);

                this.textBox1.Text += Environment.NewLine + text + "\r\n";//"\r\n";// "\r\n" + text;
                this.textBox1.SelectionStart = this.textBox1.Text.Length;
                this.textBox1.ScrollToCaret();

                string content = DateTime.Now.ToString() + " " + Contract + " " + JobNum + " " + "自动功能测试 " + text + "\r\n";
                myFs = new FileStream(file, FileMode.Append, FileAccess.Write);
                mySw = new StreamWriter(myFs);
                mySw.Write(content);
                mySw.Close();
                myFs.Close();
            //}
        }

        #endregion

        #region Port communication

        public delegate void DeleUpdateTextbox(string dataRe);
        private void UpdateTextbox(string dataRe)
        {
            //dataRE = "";
            //textBox2.AppendText(dataRe);
            datare[iData++] = dataRe;
            if (datare[iData - 1] == "0A " && datare[iData - 2] == "0D ")
            {
                dataRE = string.Join("", datare);
                textBox2.Text = dataRE;
                iData = 0;//0123修改
            }

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
                textBox2.Invoke(deleupdatetextbox, dataRe);
                //data[j++] = dataRe;
                //if (dataRe == "0A ")
                //{
                //    textBox1.Invoke(deleupdatetextbox, data);
                //    j = 0;
                //}
            }
        }

        #endregion

        #region EnableButton
        private delegate void EnableButton1();
        private void enablebutton1()
        {
            this.button1.Enabled = true;
            if (NEXT == true)
            {
                serialPort1.Close();
                label7.ForeColor = Color.Green;
                label7.Text = "功能测试 PASS"; 
                Form AutoRun = new AutoR(file, PLCCom, PLCPrm2,Contract,JobNum,PLCPrm,TELECom);
                AutoRun.Show();                
                this.Close();
            }
            else
            {
                label7.ForeColor = Color.Red;
                label7.Text = "功能测试 FAIL！";
            }
        }
        private delegate void EnableButton();
        private void enablebutton()
        {
            this.button1.Enabled = true;
            label7.ForeColor = Color.Green;
            label7.Text = "等待功能测试"; 
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
                //if (((CheckBox)this.Controls.Find("checkbox" + (i + 1), true)[0]).Checked == true)
                //{
                A++;
                //}
            }
            string[] AttentionW = new string[A + 1];
            string[] AttentionB = new string[A + 1];

            for (int i = 0; i < PLCPrm1.Length / 16; i++)
            {
                if (PLCPrm1[i, 1] == "57814139")
                {
                    for (int J = 0; J < PLCPrm1.Length / 16; J++)
                    {
                        if (PLCPrm1[J, 1] == "57814165")
                        {
                            AttentionW[0] = "请把XCT线束连接至模块（线束为黑色）";
                            AttentionB[0] = "";
                        }
                        else if (J == PLCPrm1.Length / 16 - 1)
                        {
                            AttentionW[0] = "请把XCT、XCTD、XCTB线束连接至模块（线束为黑色）";
                            AttentionB[0] = "";
                        }
                    }
                }
            }

            A = 1;
            for (int i = 0; i < PLCPrm1.Length / 16; i++)
            {
                //if (((CheckBox)this.Controls.Find("checkbox" + (i + 1), true)[0]).Checked == true)
                //{
                AttentionW[A] = PLCPrm1[i, 14];
                AttentionB[A++] = PLCPrm1[i, 15];
                //}
            }
            //MessageBox.Show(string.Join("\n", AttentionW) + "\n\n" + string.Join("\n", AttentionB));
            MessageShow messageshow = new MessageShow(string.Join("\n\n", AttentionW) + "\n\n" + string.Join("\n\n", AttentionB));
            messageshow.ShowDialog();
            #endregion

            string a = ": 01 05 08 12 FF 00";
            string b = GetLRC(a);
            byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
            serialPort1.Write(message1, 0, b.Length);

            Thread.Sleep(1000);


            a = ": 01 05 08 " + "25" + " FF 00";                 //发送直流矫正PLC指令
            b = GetLRC(a);
            message1 = System.Text.Encoding.ASCII.GetBytes(b);
            serialPort1.Write(message1, 0, b.Length);
            Thread.Sleep(8000);

            #region  foreach label
            //foreach (Control o in Controls)
            //{
            //    if (o is Label && Convert.ToInt32(o.Name.Substring(6), 10) < 6 && Convert.ToInt32(o.Name.Substring(6), 10) > 0)
            //    {
            //        int num = Convert.ToInt32(o.Name.Substring(8), 10) - 1;
            for (int I = 1; I <= PLCPrm1.Length / 16; I++)
            {
                if (((Label)this.Controls.Find("label" + I, true)[0]).Visible == true)
                {
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
                                    textBox1.Invoke(settextbox, (PLCPrm1[num, 6 + i]).Split(' ')[2] + " 合格 " + (Convert.ToDouble(Convert.ToInt32(result[j], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[num, 6 + i].Split(' ')[4])).ToString("0.0") + "V");
                                }
                                //textBox2.AppendText((PLCPrm1[3, 6 + i]).Split(' ')[2] + "合格 " + Convert.ToInt32(result[i], 16));if (Convert.ToInt32((PLCPrm1[num, 6 + j]).Split(' ')[0], 10) <= (Convert.ToInt32(Convert.ToInt32(result[j], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[num, 12])) && (Convert.ToInt32(Convert.ToInt32(result[j], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[num, 12])) <= Convert.ToInt32((PLCPrm1[num, 6 + j]).Split(' ')[1], 10))
                                else
                                {
                                    textBox1.Invoke(settextbox, (PLCPrm1[num, 6 + i]).Split(' ')[2] + " 不合格 " + (Convert.ToDouble(Convert.ToInt32(result[j], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[num, 6 + i].Split(' ')[4])).ToString("0.0") + "V");
                                    NEXT = false;
                                }
                                //textBox2.AppendText((PLCPrm1[3, 6 + i]).Split(' ')[2] + "不合格 " + Convert.ToInt32(result[i], 16));
                            }
                            Thread.Sleep(5000);
                        }
                    }
                }
            }
            #endregion

            #region label1

            //    string a = ": 01 05 08 " + PLCPrm1[0, 2] + " FF 00";                       //发送PLC指令
            //    string b = GetLRC(a);
            //    byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
            //    serialPort1.Write(message1, 0, b.Length);
            //    int m = 3;
            //    while (m != 0)
            //    {
            //        Thread.Sleep(2000);
            //        if (m == 3)
            //        {
            //            a = ": 01 02 08 " + PLCPrm1[0, 5] + " 00 01";                          //读取PLC完成测试标志
            //            b = GetLRC(a);
            //            message1 = System.Text.Encoding.ASCII.GetBytes(b);
            //            serialPort1.Write(message1, 0, b.Length);
            //        }
            //        Thread.Sleep(1000);

            //        if (dataRE.Contains(PLCPrm1[0, 13]))
            //        {
            //            a = ": 01 03 11 " + PLCPrm1[0, 3] + " 00 " + PLCPrm1[0, 4];        //读取所需读取的电压的值
            //            b = GetLRC(a);
            //            message1 = System.Text.Encoding.ASCII.GetBytes(b);
            //            serialPort1.Write(message1, 0, b.Length);
            //            Thread.Sleep(2000);

            //            string c = dataRE;// "3A 30 31 30 33 30 43 30 30 36 34 30 30 43 38 30 31 32 43 30 31 39 30 30 31 46 34 30 32 35 38 42 37 0D 0A ";
            //            string ascii = c.Substring(21, 12 * (Convert.ToInt32(PLCPrm1[0, 4], 10))).Replace(" ", "");
            //            List<byte> buffer = new List<byte>();
            //            for (int i = 0; i < ascii.Length; i += 2)
            //            {
            //                string temp = ascii.Substring(i, 2);
            //                byte value = Convert.ToByte(temp, 16);
            //                buffer.Add(value);
            //            }
            //            string str = System.Text.Encoding.ASCII.GetString(buffer.ToArray());
            //            string[] result = Regex.Split(str, @"(?<=\G.{4})(?!$)");//@"(?<=\G.{4})(?!$)"//(?<=\\G.{4})

            //            for (int i = 0; i < result.Length; i++)
            //            {
            //                if (Convert.ToInt32((PLCPrm1[0, 6 + i]).Split(' ')[0], 10) <= (Convert.ToInt32(Convert.ToInt32(result[i], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[0, 12])) && (Convert.ToInt32(Convert.ToInt32(result[i], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[0, 12])) <= Convert.ToInt32((PLCPrm1[0, 6 + i]).Split(' ')[1], 10))
            //                    textBox1.Invoke(settextbox, (PLCPrm1[0, 6 + i]).Split(' ')[2] + "合格 " + (Convert.ToInt32(Convert.ToInt32(result[i], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[0, 12])).ToString() + "V");
            //                //textBox2.AppendText((PLCPrm1[3, 6 + i]).Split(' ')[2] + "合格 " + Convert.ToInt32(result[i], 16));
            //                else textBox1.Invoke(settextbox, (PLCPrm1[0, 6 + i]).Split(' ')[2] + "不合格 " + (Convert.ToInt32(Convert.ToInt32(result[i], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[0, 12])).ToString() + "V");
            //                //textBox2.AppendText((PLCPrm1[3, 6 + i]).Split(' ')[2] + "不合格 " + Convert.ToInt32(result[i], 16));
            //            }


            //            break;
            //        }
            //        m--;
            //        if (m == 0) break;
            //    }
            //#endregion
            //#region label2

            //    a = ": 01 05 08 " + PLCPrm1[1, 2] + " FF 00";                       //发送PLC指令
            //    b = GetLRC(a);
            //     message1 = System.Text.Encoding.ASCII.GetBytes(b);
            //    serialPort1.Write(message1, 0, b.Length);
            //    m = 3;
            //    while (m != 0)
            //    {
            //        Thread.Sleep(2000);
            //        if (m == 3)
            //        {
            //            a = ": 01 02 08 " + PLCPrm1[1, 5] + " 00 01";                          //读取PLC完成测试标志
            //            b = GetLRC(a);
            //            message1 = System.Text.Encoding.ASCII.GetBytes(b);
            //            serialPort1.Write(message1, 0, b.Length);
            //        }
            //        Thread.Sleep(1000);

            //        if (dataRE.Contains(PLCPrm1[1, 13]))
            //        {
            //            a = ": 01 03 11 " + PLCPrm1[1, 3] + " 00 " + PLCPrm1[1, 4];            //读取所需读取的电压的值
            //            b = GetLRC(a);
            //            message1 = System.Text.Encoding.ASCII.GetBytes(b);
            //            serialPort1.Write(message1, 0, b.Length);
            //            Thread.Sleep(2000);

            //            string c = dataRE;// "3A 30 31 30 33 30 43 30 30 36 34 30 30 43 38 30 31 32 43 30 31 39 30 30 31 46 34 30 32 35 38 42 37 0D 0A ";
            //            string ascii = c.Substring(21, 12 * (Convert.ToInt32(PLCPrm1[1, 4], 10))).Replace(" ", "");
            //            List<byte> buffer = new List<byte>();
            //            for (int i = 0; i < ascii.Length; i += 2)
            //            {
            //                string temp = ascii.Substring(i, 2);
            //                byte value = Convert.ToByte(temp, 16);
            //                buffer.Add(value);
            //            }
            //            string str = System.Text.Encoding.ASCII.GetString(buffer.ToArray());
            //            string[] result = Regex.Split(str, @"(?<=\G.{4})(?!$)");//@"(?<=\G.{4})(?!$)"//(?<=\\G.{4})

            //            for (int i = 0; i < result.Length; i++)
            //            {
            //                if (Convert.ToInt32((PLCPrm1[1, 6 + i]).Split(' ')[0], 10) <= (Convert.ToInt32(Convert.ToInt32(result[i], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[1, 12])) && (Convert.ToInt32(Convert.ToInt32(result[i], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[1, 12])) <= Convert.ToInt32((PLCPrm1[1, 6 + i]).Split(' ')[1], 10))
            //                    textBox1.Invoke(settextbox, (PLCPrm1[1, 6 + i]).Split(' ')[2] + "合格 " + (Convert.ToInt32(Convert.ToInt32(result[i], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[1, 12])).ToString() + "V");
            //                //textBox2.AppendText((PLCPrm1[3, 6 + i]).Split(' ')[2] + "合格 " + Convert.ToInt32(result[i], 16));
            //                else textBox1.Invoke(settextbox, (PLCPrm1[1, 6 + i]).Split(' ')[2] + "不合格 " + (Convert.ToInt32(Convert.ToInt32(result[i], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[1, 12])).ToString() + "V");
            //                //textBox2.AppendText((PLCPrm1[3, 6 + i]).Split(' ')[2] + "不合格 " + Convert.ToInt32(result[i], 16));
            //            }

            //            break;
            //        }
            //        m--;
            //        if (m == 0) break;
            //    }
            //#endregion
            //#region label3

            //    a = ": 01 05 08 " + PLCPrm1[2, 2] + " FF 00";                       //发送PLC指令
            //    b = GetLRC(a);
            //    message1 = System.Text.Encoding.ASCII.GetBytes(b);
            //    serialPort1.Write(message1, 0, b.Length);
            //    m = 3;
            //    while (m != 0)
            //    {
            //        if (m == 3)
            //        {
            //            Thread.Sleep(2000);
            //            a = ": 01 02 08 " + PLCPrm1[2, 5] + " 00 01";                          //读取PLC完成测试标志
            //            b = GetLRC(a);
            //            message1 = System.Text.Encoding.ASCII.GetBytes(b);
            //            serialPort1.Write(message1, 0, b.Length);
            //        }
            //        Thread.Sleep(1000);

            //        if (dataRE.Contains(PLCPrm1[2, 13]))
            //        {
            //            a = ": 01 03 11 " + PLCPrm1[2, 3] + " 00 " + PLCPrm1[2, 4];            //读取所需读取的电压的值
            //            b = GetLRC(a);
            //            message1 = System.Text.Encoding.ASCII.GetBytes(b);
            //            serialPort1.Write(message1, 0, b.Length);
            //            Thread.Sleep(2000);

            //            string c = dataRE;// "3A 30 31 30 33 30 43 30 30 36 34 30 30 43 38 30 31 32 43 30 31 39 30 30 31 46 34 30 32 35 38 42 37 0D 0A ";
            //            string ascii = c.Substring(21, 12 * (Convert.ToInt32(PLCPrm1[2, 4], 10))).Replace(" ", "");
            //            List<byte> buffer = new List<byte>();
            //            for (int i = 0; i < ascii.Length; i += 2)
            //            {
            //                string temp = ascii.Substring(i, 2);
            //                byte value = Convert.ToByte(temp, 16);
            //                buffer.Add(value);
            //            }
            //            string str = System.Text.Encoding.ASCII.GetString(buffer.ToArray());
            //            string[] result = Regex.Split(str, @"(?<=\G.{4})(?!$)");//@"(?<=\G.{4})(?!$)"//(?<=\\G.{4})

            //            for (int i = 0; i < result.Length; i++)
            //            {
            //                if (Convert.ToInt32((PLCPrm1[2, 6 + i]).Split(' ')[0], 10) <= (Convert.ToInt32(Convert.ToInt32(result[i], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[2, 12])) && (Convert.ToInt32(Convert.ToInt32(result[i], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[2, 12])) <= Convert.ToInt32((PLCPrm1[2, 6 + i]).Split(' ')[1], 10))
            //                    textBox1.Invoke(settextbox, (PLCPrm1[2, 6 + i]).Split(' ')[2] + "合格 " + (Convert.ToInt32(Convert.ToInt32(result[i], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[2, 12])).ToString() + "V");
            //                //textBox2.AppendText((PLCPrm1[3, 6 + i]).Split(' ')[2] + "合格 " + Convert.ToInt32(result[i], 16));
            //                else textBox1.Invoke(settextbox, (PLCPrm1[2, 6 + i]).Split(' ')[2] + "不合格 " + (Convert.ToInt32(Convert.ToInt32(result[i], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[2, 12])).ToString() + "V");
            //                //textBox2.AppendText((PLCPrm1[3, 6 + i]).Split(' ')[2] + "不合格 " + Convert.ToInt32(result[i], 16));
            //            }
            //            break;
            //        }
            //        m--;
            //        if (m == 0) break;
            //    }

            //#endregion
            //#region label4

            //    a = ": 01 05 08 " + PLCPrm1[3, 2] + " FF 00";                       //发送PLC指令
            //    b = GetLRC(a);
            //    message1 = System.Text.Encoding.ASCII.GetBytes(b);
            //    serialPort1.Write(message1, 0, b.Length);
            //    m = 3;
            //    while (m != 0)
            //    {
            //        if (m == 3)
            //        {
            //            Thread.Sleep(2000);
            //            a = ": 01 02 08 " + PLCPrm1[3, 5] + " 00 01";                          //读取PLC完成测试标志
            //            b = GetLRC(a);
            //            message1 = System.Text.Encoding.ASCII.GetBytes(b);
            //            serialPort1.Write(message1, 0, b.Length);
            //            dataRE = "";
            //        }
            //        Thread.Sleep(1000);
            //        if (dataRE.Contains(PLCPrm1[3, 13]))//("58484948504849536665491310"))
            //        {
            //            a = ": 01 03 11 " + PLCPrm1[3, 3] + " 00 " + PLCPrm1[3, 4];        //读取所需读取的电压的值
            //            b = GetLRC(a);
            //            message1 = System.Text.Encoding.ASCII.GetBytes(b);
            //            serialPort1.Write(message1, 0, b.Length);
            //            Thread.Sleep(2000);


            //            string c = dataRE;// "3A 30 31 30 33 30 43 30 30 36 34 30 30 43 38 30 31 32 43 30 31 39 30 30 31 46 34 30 32 35 38 42 37 0D 0A ";
            //            string ascii = c.Substring(21, 12 * (Convert.ToInt32(PLCPrm1[3, 4], 10))).Replace(" ", "");
            //            List<byte> buffer = new List<byte>();
            //            for (int i = 0; i < ascii.Length; i += 2)
            //            {
            //                string temp = ascii.Substring(i, 2);
            //                byte value = Convert.ToByte(temp, 16);
            //                buffer.Add(value);
            //            }
            //            string str = System.Text.Encoding.ASCII.GetString(buffer.ToArray());
            //            string[] result = Regex.Split(str, @"(?<=\G.{4})(?!$)");//@"(?<=\G.{4})(?!$)"//(?<=\\G.{4})

            //            for (int i = 0; i < result.Length; i++)
            //            {
            //                if (Convert.ToInt32((PLCPrm1[3, 6 + i]).Split(' ')[0], 10) <= (Convert.ToInt32(Convert.ToInt32(result[i], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[3, 12])) && (Convert.ToInt32(Convert.ToInt32(result[i], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[3, 12])) <= Convert.ToInt32((PLCPrm1[3, 6 + i]).Split(' ')[1], 10))
            //                    textBox1.Invoke(settextbox, (PLCPrm1[3, 6 + i]).Split(' ')[2] + "合格 " + (Convert.ToInt32(Convert.ToInt32(result[i], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[3, 12])).ToString() + "V");
            //                //textBox2.AppendText((PLCPrm1[3, 6 + i]).Split(' ')[2] + "合格 " + Convert.ToInt32(result[i], 16));
            //                else textBox1.Invoke(settextbox, (PLCPrm1[3, 6 + i]).Split(' ')[2] + "不合格 " + (Convert.ToInt32(Convert.ToInt32(result[i], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[3, 12])).ToString() + "V");
            //                //textBox2.AppendText((PLCPrm1[3, 6 + i]).Split(' ')[2] + "不合格 " + Convert.ToInt32(result[i], 16));
            //            }
            //            break;
            //        }
            //        m--;
            //        if (m == 0) break;
            //    }
            //#endregion
            //#region label5

            //    a = ": 01 05 08 " + PLCPrm1[4, 2] + " FF 00";                       //发送PLC指令
            //    b = GetLRC(a);
            //    message1 = System.Text.Encoding.ASCII.GetBytes(b);
            //    serialPort1.Write(message1, 0, b.Length);
            //    m = 3;
            //    while (m != 0)
            //    {
            //        if (m == 3)
            //        {
            //            Thread.Sleep(2000);
            //            a = ": 01 02 08 " + PLCPrm1[4, 5] + " 00 01";                          //读取PLC完成测试标志
            //            b = GetLRC(a);
            //            message1 = System.Text.Encoding.ASCII.GetBytes(b);
            //            serialPort1.Write(message1, 0, b.Length);
            //            dataRE = "";
            //        }
            //        Thread.Sleep(1000);
            //        if (dataRE.Contains(PLCPrm1[4, 13]))//("58484948504849536665491310"))
            //        {
            //            a = ": 01 03 11 " + PLCPrm1[4, 3] + " 00 " + PLCPrm1[4, 4];        //读取所需读取的电压的值
            //            b = GetLRC(a);
            //            message1 = System.Text.Encoding.ASCII.GetBytes(b);
            //            serialPort1.Write(message1, 0, b.Length);
            //            Thread.Sleep(2000);


            //            string c = dataRE;// "3A 30 31 30 33 30 43 30 30 36 34 30 30 43 38 30 31 32 43 30 31 39 30 30 31 46 34 30 32 35 38 42 37 0D 0A ";
            //            string ascii = c.Substring(21, 12 * (Convert.ToInt32(PLCPrm1[4, 4], 10))).Replace(" ", "");
            //            List<byte> buffer = new List<byte>();
            //            for (int i = 0; i < ascii.Length; i += 2)
            //            {
            //                string temp = ascii.Substring(i, 2);
            //                byte value = Convert.ToByte(temp, 16);
            //                buffer.Add(value);
            //            }
            //            string str = System.Text.Encoding.ASCII.GetString(buffer.ToArray());
            //            string[] result = Regex.Split(str, @"(?<=\G.{4})(?!$)");//@"(?<=\G.{4})(?!$)"//(?<=\\G.{4})

            //            for (int i = 0; i < result.Length; i++)
            //            {
            //                if (Convert.ToInt32((PLCPrm1[4, 6 + i]).Split(' ')[0], 10) <= (Convert.ToInt32(Convert.ToInt32(result[i], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[4, 12])) && (Convert.ToInt32(Convert.ToInt32(result[i], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[4, 12])) <= Convert.ToInt32((PLCPrm1[4, 6 + i]).Split(' ')[1], 10))
            //                    textBox1.Invoke(settextbox, (PLCPrm1[4, 6 + i]).Split(' ')[2] + "合格 " + (Convert.ToInt32(Convert.ToInt32(result[i], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[4, 12])).ToString() + "V");
            //                //textBox2.AppendText((PLCPrm1[3, 6 + i]).Split(' ')[2] + "合格 " + Convert.ToInt32(result[i], 16));
            //                else textBox1.Invoke(settextbox, (PLCPrm1[4, 6 + i]).Split(' ')[2] + "不合格 " + (Convert.ToInt32(Convert.ToInt32(result[i], 16).ToString()) / Convert.ToDouble("32000") * Convert.ToDouble(PLCPrm1[4, 12])).ToString() + "V");
            //                //textBox2.AppendText((PLCPrm1[3, 6 + i]).Split(' ')[2] + "不合格 " + Convert.ToInt32(result[i], 16));
            //            }

            //            break;
            //        }
            //        m--;
            //        if (m == 0) break;
            //    }

            #endregion


            if (NEXT == true)
            {
                //MessageBox.Show("功能测试 PASS" + "\n\n" + "请拔下所有接插件，并断开所有断路器");
                ShowResult showresult = new ShowResult("功能测试 PASS");
                showresult.ShowDialog();
                EnableButton1 ebutton1 = new EnableButton1(enablebutton1);
                button1.Invoke(ebutton1);
                Thread.CurrentThread.Abort();
            }
            else
            {
                NEXT = true;
                //MessageBox.Show("功能测试 FAIL" + "\n\n" + "请拔下所有接插件，并断开所有断路器");
                ShowResult showresult = new ShowResult("功能测试 FAIL");
                showresult.ShowDialog();
                EnableButton ebutton = new EnableButton(enablebutton);
                button1.Invoke(ebutton);
                Thread.CurrentThread.Abort();
            }
            //MessageBox.Show("测试结束" + "\n\n" + "请拔下所有接插件，并断开所有断路器");
            //EnableButton ebutton1 = new EnableButton(enablebutton1);
            //button1.Invoke(ebutton1);
            //Thread.CurrentThread.Abort();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            label7.ForeColor = Color.Red;
            label7.Text = "功能测试运行中...";
            SetTextCallback settextbox = new SetTextCallback(SetText);
            textBox1.Invoke(settextbox, "——————————————————————");
            Thread F = new Thread(Function);
            F.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            serialPort1.Close();
            this.Close();            
        }
    }
}
