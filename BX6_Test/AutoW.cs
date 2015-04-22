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
    public partial class AutoW : Form
    {
        #region Global Variables
        private Thread Listen;
        private Thread Check;
        private Thread Send;
        public int iTextbox1 = 0;
        FileStream myFs;
        StreamWriter mySw;
        string file;
        string PLCCom;
        string TELECom;
        string Contract;
        string JobNum;

        //bool next;
        //bool check;
        int next = 2;
        int check = 0;
        bool ing = false;
        bool NEXT = true;

        string SamplePath;
        private System.Data.DataSet myDataSet;
        public int iTextbox5 = 0;
        string[] words = new string[2];
        string[,] PLCPrm1;
        string[,] PLCPrm2;
        int[] PLCPrm = new int[13];

        short[] M = new short[3];

        string dataRE = null;
        string[] datare = new string[60];
        int iData = 0;

        string[] messages = new string[100];
        string[] Messages = new string[100];
        int mn = 0;
        #endregion

        protected override void WndProc(ref   Message m)                                //禁用右上角关闭按钮
        {
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_CLOSE = 0xF060;
            if (m.Msg == WM_SYSCOMMAND && (int)m.WParam == SC_CLOSE)
            {
                return;
            }
            base.WndProc(ref m);
        }

        public AutoW(string file, string PLCCom, string[,] PLCPrm1, string[,] PLCPrm2, string contract, string jobnum, int[] PLCPrm, string TELECom)
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

            for (int i = 0; i < 4; i++)
            {
                ((Label)this.Controls.Find("label" + (i + 1), true)[0]).Visible = false;
            }

            for (int i = 0; i < PLCPrm1.Length / 8; i++)
            {
                ((Label)this.Controls.Find("label" + (i + 1), true)[0]).Visible = true;
                ((Label)this.Controls.Find("label" + (i + 1), true)[0]).Text = PLCPrm1[i, 0];
            }

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

            string a = ": 01 05 08 11 FF 00";
            string b = GetLRC(a);
            byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
            serialPort1.Write(message1, 0, b.Length);

            this.button1.Enabled = false;
            Send = new Thread(SentToPLC);
            Send.IsBackground = true;
            Send.Start();

            Listen = new Thread(new ThreadStart(StartListen));
            Listen.IsBackground = true;
            Listen.Start();
        }

        #region txt & USB communication

        private delegate void SetTextCallback(string text);
        private void SetText(string text)
        {
            this.textBox1.Text += Environment.NewLine + text + "\r\n";// "\r\n" + text;
            this.textBox1.SelectionStart = this.textBox1.Text.Length;
            this.textBox1.ScrollToCaret();

            string content = DateTime.Now.ToString() + " " + Contract + " " + JobNum + " " + "自动连线测试 " + text + "\r\n";
            myFs = new FileStream(file, FileMode.Append, FileAccess.Write);
            mySw = new StreamWriter(myFs);
            mySw.Write(content);
            mySw.Close();
            myFs.Close();
        }
        private void StartCheck(object arry)
        {
            string[,] Arry = arry as string[,];
            string[] report = new string[Arry.Length / 7];
            int r = 0;
            int error = 0;
            int count = 0;
            SetTextCallback settextbox = new SetTextCallback(SetText);
            while (true)
            {
                if (check == 2)
                {
                    //NEXT = true;
                    count = 0;
                    check = 0;                     //修改201503040945
                    for (int i = 0; i < (Arry.Length / 7); i++)
                    {
                        for (int k = 0; k < Messages.Length; k++)
                        {
                            if (Messages[k] != null)
                            {
                                if (Messages[k].Contains("OPEN") && Messages[k].Contains(Arry[i, 0]) && Messages[k].Contains(Arry[i, 1]))
                                {
                                    report[r] = Arry[i, 2] + " 存在一处开路";
                                    error++;
                                    r++;
                                }
                                else if (Messages[k].Contains("SHORT") && Messages[k].Contains(Arry[i, 0]))
                                {
                                    for (int j = 0; j < Arry.Length / 7 - 1; j++)
                                    {
                                        if (Messages[k].Contains(Arry[j, 0]) && (Arry[j, 0] != Arry[i, 0]))
                                        {
                                            report[r] = Arry[i, 2] + " 存在一处短路";
                                            error++;
                                            r++;
                                        }
                                    }
                                }
                            }
                        }
                        if (error == 0)
                        {
                            count++;
                            report[r] = Arry[i, 2] + " Pass";
                            r++;
                        }
                        error = 0;
                    }
                    //check = 0;                     //修改201503040945
                    if (count != (Arry.Length / 7))
                    {
                        next--;
                        error = 0;
                        count = 0;
                        if (next == 0)
                        {
                            NEXT = false;
                            for (int i = 0; i < Arry.Length / 7; i++)
                            {
                                textBox1.Invoke(settextbox, report[i]);
                                count = 0;
                            }
                        }
                    }
                    else if (count == (Arry.Length / 7))
                    {
                        next = 0;
                        for (int i = 0; i < Arry.Length / 7; i++)
                        {
                            textBox1.Invoke(settextbox,report[i]);
                            count = 0;
                        }
                    }
                    Thread.CurrentThread.Abort();
                }
            }
        }
        private void StartListen()
        {
            SetTextCallback settextbox = new SetTextCallback(SetText);
            System.Text.ASCIIEncoding asciiEncoding = new System.Text.ASCIIEncoding();

            while (true)
            {
                byte[] buf = new byte[400];
                int len = 100;
                if (ch372.ReadData(0, buf, ref len) == true && len != 0)
                {
                    string message = asciiEncoding.GetString(buf);
                    //if (!this.IsHandleCreated) return;
                    if (message.Contains("Start") || ing == true)
                    {
                        ing = true;
                        messages[mn++] = message;
                        if (message.Contains("End"))
                        {
                            ing = false;
                            mn = 0;
                            for (int i = 0; i < messages.Length; i++)
                            {
                                Messages[i] = messages[i];
                            }
                            for (int i = 0; i < messages.Length; i++)
                            {
                                messages[i] = null;
                            }
                            check++;
                        }
                    }
                }
            }
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
                label7.Text = "连线测试 PASS";
                Listen.Abort();
                Form AutoFun = new AutoF(file, PLCCom, PLCPrm2, PLCPrm1,Contract,JobNum,PLCPrm,TELECom);
                AutoFun.Show();
                //this.Close();
                this.Dispose();
            }
            else
            {
                label7.ForeColor = Color.Red;
                label7.Text = "连线测试 FAIL！";
            }
        }

        private delegate void EnableButton();
        private void enablebutton()
        {
            this.button1.Enabled = true;
            label7.ForeColor = Color.Green;
            label7.Text = "等待连线测试";
        }
        #endregion

        private string GetLRC(string a)                                                 //计算LRC校验位
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
            //string a = ": 01 02 08 96 00 01";
            //string b = GetLRC(a);
            //byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
            dataRE = "";
            //serialPort1.Write(message1, 0, b.Length);

            Thread.Sleep(1000);
            if (dataRE.Contains("3A 30 31 30 35 30 38"))
            {
                EnableButton1 ebutton1 = new EnableButton1(enablebutton);
                button1.Invoke(ebutton1);
                return;
            }
            else
            {
                MessageBox.Show("PLC串口未监测到 PLC 在线" + "\n\n" + "请关闭软件确认好 PLC 在线 并且与 串口 连线正确后重试", "Error");
                System.Diagnostics.Process.GetCurrentProcess().Kill();
            }
            Thread.CurrentThread.Abort();
        }

        private void Connection()                                                       //连线测试专属线程
        {
            int sum = 0;
            int sum1 = 0;
            int Sum = 0;
            int l = 0;
            int n = 0;
            int m = 0;
            string[,] Arry;
            string[,] Arry1;
            string[,] Arry2;
            object arry;
            object arry1;
            object arry2;

            SamplePath = Properties.Settings.Default.SampleSetting;
            string path = SamplePath + "/";
            string strCon = " Provider = Microsoft.Jet.OLEDB.4.0 ; Data Source = " + path + "PLC.xls;Extended Properties='Excel 8.0;HDR=NO;IMEX=1;'";    //创建一个数据链接
            //string strCon = "Provider=Microsoft.Jet.OLEDB.4.0;" + "Data Source=" + path + "PLC.xls;" + "Extended Properties='Excel 8.0;HDR=YES;IMEX=1';";
            OleDbConnection myConn = new OleDbConnection(strCon);
            myConn.Open();

            OleDbDataAdapter myCommand = new OleDbDataAdapter();                 //打开数据链接，得到一个数据集
            myDataSet = new DataSet();                                           //创建一个 DataSet对象

            DataTable schemaTable = myConn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
            foreach (DataRow dr in schemaTable.Rows)                            // 遍历工作表取得数据并存入Dataset
            {
                string strSql = "Select * From [" + dr[2].ToString().Trim() + "]";
                OleDbCommand objCmd = new OleDbCommand(strSql, myConn);
                myCommand.SelectCommand = objCmd;
                myCommand.Fill(myDataSet, dr[2].ToString().Trim());
                Sum++;
            }
            myConn.Close();                                                     //修改

            #region Attention
            int A = 0;
            for (int i = 0; i < PLCPrm1.Length / 8; i++)
            {
                A++;
            }
            for (int i = 0; i < PLCPrm2.Length / 16; i++)
            {
                if (PLCPrm2[i, 14].Trim() != "")
                {
                    A++;
                }
            }
            string[] AttentionW = new string[A + 1];
            string[] AttentionB = new string[A + 1];

            for (int i = 0; i < PLCPrm1.Length / 8; i++)
            {
                if (PLCPrm1[i, 1] == "57814139")
                {
                    for (int J = 0; J < PLCPrm1.Length / 8; J++)
                    {
                        if (PLCPrm1[J, 1] == "57814165")
                        {
                            AttentionW[0] = "请把XCT线束连接至模块（线束为黑色）";
                            AttentionB[0] = "";
                        }
                        else if (J == PLCPrm1.Length / 8 - 1)
                        {
                            AttentionW[0] = "请把XCT、XCTD、XCTB线束连接至模块（线束为黑色）";
                            AttentionB[0] = "";
                        }
                    }
                }
            }

            A = 1;
            for (int i = 0; i < PLCPrm1.Length / 8; i++)
            {
                AttentionW[A] = PLCPrm1[i, 6];
                AttentionB[A++] = PLCPrm1[i, 7];
            }
            //for (int i = 0; i < PLCPrm2.Length / 16; i++)
            //{
            //    if (PLCPrm2[i, 14].Trim() != "")
            //    {
            //        AttentionW[A] = PLCPrm2[i, 14];
            //        AttentionB[A++] = PLCPrm2[i, 15];
            //    }
            //}
            //MessageBox.Show(string.Join("\n", AttentionW) + "\n\n" + string.Join("\n", AttentionB));
            MessageShow messageshow = new MessageShow(string.Join("\n\n", AttentionW) + "\n\n" + string.Join("\n\n", AttentionB));
            messageshow.ShowDialog();
            #endregion

            #region 139/165
            for (int i = 0; i < PLCPrm1.Length / 8; i++)
            {
                if (PLCPrm1[i, 1] == "57814139")
                {
                    for (int J = 0; J < PLCPrm1.Length / 8; J++)
                    {
                        if (PLCPrm1[J, 1] == "57814165")
                        {
                            Arry = null;
                            arry = null;
                            sum = 0;
                            for (int j = 0; j < Sum; j++)
                            {
                                if ("2" == myDataSet.Tables[j].TableName.ToString().Trim().Substring(1, myDataSet.Tables[j].TableName.ToString().Trim().Length - 3))
                                {
                                    for (int k = 0; k < myDataSet.Tables[j].Rows.Count; k++)
                                    {
                                        if (myDataSet.Tables[j].Rows[k].ItemArray[3].ToString() == "1")
                                        {
                                            sum++;
                                        }
                                    }
                                    Arry = new string[sum, 7];
                                    Arry1 = new string[myDataSet.Tables[j].Rows.Count - sum, 7];
                                    for (int k = 0; k < myDataSet.Tables[j].Rows.Count; k++)
                                    {
                                        if (myDataSet.Tables[j].Rows[k].ItemArray[3].ToString() == "1")
                                        {
                                            Arry[l, 0] = myDataSet.Tables[j].Rows[k].ItemArray[0].ToString();           //待测物料报警文字汇总
                                            Arry[l, 1] = myDataSet.Tables[j].Rows[k].ItemArray[1].ToString();
                                            Arry[l, 2] = myDataSet.Tables[j].Rows[k].ItemArray[2].ToString();
                                            Arry[l, 3] = myDataSet.Tables[j].Rows[k].ItemArray[3].ToString();
                                            Arry[l, 4] = myDataSet.Tables[j].Rows[k].ItemArray[4].ToString();
                                            Arry[l, 5] = myDataSet.Tables[j].Rows[k].ItemArray[5].ToString();
                                            Arry[l++, 6] = myDataSet.Tables[j].Rows[k].ItemArray[6].ToString();
                                        }
                                    }
                                    arry = Arry;
                                    l = 0;
                                }
                            }
                            if (Arry.Length != 0)
                            {
                                Check = new Thread(new ParameterizedThreadStart(StartCheck));
                                Check.IsBackground = true;
                                string a = ": 01 05 08 " + "14" + " FF 00";           //发送PLC指令
                                string b = GetLRC(a);
                                byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
                                serialPort1.Write(message1, 0, b.Length);

                                Check.Start(arry);
                                Thread.Sleep(4500);
                                dataRE = "";
                                //等待线材仪测试完毕

                                int ot = 0;
                                while (next != 0)
                                {
                                    ot++;
                                    //不要着急，休息一会儿 
                                    Thread.Sleep(500);
                                    if (next == 1) TryAgain("14", arry);
                                    if (ot == 30)
                                    {
                                        Listen.Abort();
                                        Check.Abort();
                                        OverTime("14", arry);
                                        break;
                                    }
                                }
                                next = 2;
                                Thread.Sleep(6000);
                                dataRE = "";
                            }
                        }
                        else if (J == PLCPrm1.Length / 8 - 1)
                        {
                            Arry = null;
                            arry = null;
                            sum = 0;
                            for (int j = 0; j < Sum; j++)
                            {
                                if ("1" == myDataSet.Tables[j].TableName.ToString().Trim().Substring(1, myDataSet.Tables[j].TableName.ToString().Trim().Length - 3))
                                {
                                    //Arry = new string[myDataSet.Tables[j].Rows.Count, 3];
                                    for (int k = 0; k < myDataSet.Tables[j].Rows.Count; k++)
                                    {
                                        if (myDataSet.Tables[j].Rows[k].ItemArray[3].ToString() == "1")
                                        {
                                            sum++;
                                        }
                                    }
                                    Arry = new string[sum, 7];
                                    Arry1 = new string[myDataSet.Tables[j].Rows.Count - sum, 7];
                                    for (int k = 0; k < myDataSet.Tables[j].Rows.Count; k++)
                                    {
                                        if (myDataSet.Tables[j].Rows[k].ItemArray[3].ToString() == "1")
                                        {
                                            Arry[l, 0] = myDataSet.Tables[j].Rows[k].ItemArray[0].ToString();           //待测物料报警文字汇总
                                            Arry[l, 1] = myDataSet.Tables[j].Rows[k].ItemArray[1].ToString();
                                            Arry[l, 2] = myDataSet.Tables[j].Rows[k].ItemArray[2].ToString();
                                            Arry[l, 3] = myDataSet.Tables[j].Rows[k].ItemArray[3].ToString();
                                            Arry[l, 4] = myDataSet.Tables[j].Rows[k].ItemArray[4].ToString();
                                            Arry[l, 5] = myDataSet.Tables[j].Rows[k].ItemArray[5].ToString();
                                            Arry[l++, 6] = myDataSet.Tables[j].Rows[k].ItemArray[6].ToString();
                                        }
                                    }
                                    arry = Arry;
                                    l = 0;
                                }
                            }
                            if (Arry.Length != 0)
                            {
                                Check = new Thread(new ParameterizedThreadStart(StartCheck));
                                Check.IsBackground = true;
                                string a = ": 01 05 08 " + "14" + " FF 00";           //发送PLC指令
                                string b = GetLRC(a);
                                byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
                                serialPort1.Write(message1, 0, b.Length);

                                Check.Start(arry);
                                Thread.Sleep(4500);
                                dataRE = "";
                                //等待线材仪测试完毕
                                int ot = 0;
                                while (next != 0)
                                {
                                    ot++;
                                    //不要着急，休息一会儿 
                                    Thread.Sleep(500);
                                    if (next == 1) TryAgain("14", arry);
                                    if (ot == 30)
                                    {
                                        Listen.Abort();
                                        Check.Abort();
                                        OverTime("14", arry);
                                        break;
                                    }
                                }
                                next = 2;
                                Thread.Sleep(6000);
                                dataRE = "";
                            }
                        }
                    }
                }
            }
            #endregion

            #region foreach label
            //foreach (Control o in Controls)
            //{
            //    if (o is Label && Convert.ToInt32(o.Name.Substring(5), 10) < 6 && Convert.ToInt32(o.Name.Substring(5), 10) > 0)
            //    {
            //        int num = Convert.ToInt32(o.Name.Substring(5), 10) - 1;
            for (int i = 1; i <= PLCPrm1.Length / 8; i++)
            {
                if (((Label)this.Controls.Find("label" + i, true)[0]).Visible == true)
                {
                    int num = i - 1;
                    Arry = null;
                    Arry1 = null;
                    Arry2 = null;
                    arry = null;
                    arry1 = null;
                    arry2 = null;
                    sum = 0;
                    sum1 = 0;

                    for (int j = 0; j < Sum; j++)
                    {
                        if (PLCPrm1[num, 1].ToString().Trim() == myDataSet.Tables[j].TableName.ToString().Trim().Substring(1, myDataSet.Tables[j].TableName.ToString().Trim().Length - 3))
                        {
                            for (int k = 0; k < myDataSet.Tables[j].Rows.Count; k++)
                            {
                                if (myDataSet.Tables[j].Rows[k].ItemArray[3].ToString() == "1")
                                {
                                    sum++;
                                }
                            }
                            for (int k = 0; k < myDataSet.Tables[j].Rows.Count; k++)
                            {
                                if (myDataSet.Tables[j].Rows[k].ItemArray[3].ToString() == "2")
                                {
                                    sum1++;
                                }
                            }
                            Arry = new string[sum, 7];
                            Arry1 = new string[sum1, 7];
                            Arry2 = new string[myDataSet.Tables[j].Rows.Count - sum - sum1, 7];
                            for (int k = 0; k < myDataSet.Tables[j].Rows.Count; k++)
                            {
                                if (myDataSet.Tables[j].Rows[k].ItemArray[3].ToString() == "1")
                                {
                                    Arry[l, 0] = myDataSet.Tables[j].Rows[k].ItemArray[0].ToString();           //待测物料报警文字汇总
                                    Arry[l, 1] = myDataSet.Tables[j].Rows[k].ItemArray[1].ToString();
                                    Arry[l, 2] = myDataSet.Tables[j].Rows[k].ItemArray[2].ToString();
                                    Arry[l, 3] = myDataSet.Tables[j].Rows[k].ItemArray[3].ToString();
                                    Arry[l, 4] = myDataSet.Tables[j].Rows[k].ItemArray[4].ToString();
                                    Arry[l, 5] = myDataSet.Tables[j].Rows[k].ItemArray[5].ToString();
                                    Arry[l++, 6] = myDataSet.Tables[j].Rows[k].ItemArray[6].ToString();
                                }
                                else if (myDataSet.Tables[j].Rows[k].ItemArray[3].ToString() == "2")
                                {
                                    Arry1[n, 0] = myDataSet.Tables[j].Rows[k].ItemArray[0].ToString();           //待测物料报警文字汇总
                                    Arry1[n, 1] = myDataSet.Tables[j].Rows[k].ItemArray[1].ToString();
                                    Arry1[n, 2] = myDataSet.Tables[j].Rows[k].ItemArray[2].ToString();
                                    Arry1[n, 3] = myDataSet.Tables[j].Rows[k].ItemArray[3].ToString();
                                    Arry1[n, 4] = myDataSet.Tables[j].Rows[k].ItemArray[4].ToString();
                                    Arry1[n, 5] = myDataSet.Tables[j].Rows[k].ItemArray[5].ToString();
                                    Arry1[n++, 6] = myDataSet.Tables[j].Rows[k].ItemArray[6].ToString();
                                }
                                else if (myDataSet.Tables[j].Rows[k].ItemArray[3].ToString() == "3")
                                {
                                    Arry2[m, 0] = myDataSet.Tables[j].Rows[k].ItemArray[0].ToString();           //待测物料报警文字汇总
                                    Arry2[m, 1] = myDataSet.Tables[j].Rows[k].ItemArray[1].ToString();
                                    Arry2[m, 2] = myDataSet.Tables[j].Rows[k].ItemArray[2].ToString();
                                    Arry2[m, 3] = myDataSet.Tables[j].Rows[k].ItemArray[3].ToString();
                                    Arry2[m, 4] = myDataSet.Tables[j].Rows[k].ItemArray[4].ToString();
                                    Arry2[m, 5] = myDataSet.Tables[j].Rows[k].ItemArray[5].ToString();
                                    Arry2[m++, 6] = myDataSet.Tables[j].Rows[k].ItemArray[6].ToString();
                                }
                            }
                            arry = Arry;
                            arry1 = Arry1;
                            arry2 = Arry2;
                            l = 0;
                            n = 0;
                            m = 0;
                        }
                    }
                    if (Arry != null && Arry.Length != 0)
                    {
                        Check = new Thread(new ParameterizedThreadStart(StartCheck));
                        Check.IsBackground = true;
                        string a = ": 01 05 08 " + PLCPrm1[num, 2] + " FF 00";           //发送PLC指令
                        string b = GetLRC(a);
                        byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
                        serialPort1.Write(message1, 0, b.Length);

                        Check.Start(arry);
                        Thread.Sleep(3500);
                        dataRE = "";
                        //等待线材仪测试完毕

                        int ot = 0;
                        while (next != 0)
                        {
                            ot++;
                            //不要着急，休息一会儿 
                            Thread.Sleep(500);
                            if (next == 1) TryAgain(PLCPrm1[num, 2], arry);
                            if (ot == 40)
                            {
                                Listen.Abort();
                                Check.Abort();
                                OverTime(PLCPrm1[num, 2], arry);
                                break;
                            }
                        }
                        next = 2;
                        Thread.Sleep(6000);
                        dataRE = "";

                    }
                    if (Arry1 != null && Arry1.Length != 0)
                    {
                        Check = new Thread(new ParameterizedThreadStart(StartCheck));
                        Check.IsBackground = true;
                        string a = ": 01 05 08 " + PLCPrm1[num, 3] + " FF 00";          //发送PLC指令
                        string b = GetLRC(a);
                        byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
                        serialPort1.Write(message1, 0, b.Length);

                        Check.Start(arry1);
                        Thread.Sleep(3500);
                        dataRE = "";
                        //等待线材仪测试完毕

                        int ot = 0;
                        while (next != 0)
                        {
                            ot++;
                            // 不要着急，休息一会儿 
                            Thread.Sleep(500);
                            if (next == 1) TryAgain(PLCPrm1[num, 3], arry1);
                            if (ot == 40)
                            {
                                Listen.Abort();
                                Check.Abort();
                                Thread.Sleep(510);
                                OverTime(PLCPrm1[num, 3], arry1);
                                break;
                            }
                        }
                        next = 2;
                        Thread.Sleep(6000);
                        dataRE = "";
                    }
                    if (Arry2 != null && Arry2.Length != 0)
                    {
                        Check = new Thread(new ParameterizedThreadStart(StartCheck));
                        Check.IsBackground = true;
                        string a = ": 01 05 08 " + PLCPrm1[num, 4] + " FF 00";           //发送PLC指令
                        string b = GetLRC(a);
                        byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
                        serialPort1.Write(message1, 0, b.Length);

                        Check.Start(arry2);
                        Thread.Sleep(3500);
                        dataRE = "";
                        //等待线材仪测试完毕

                        int ot = 0;
                        while (next != 0)
                        {
                            ot++;
                            // 不要着急，休息一会儿  
                            Thread.Sleep(500);
                            if (next == 1) TryAgain(PLCPrm1[num, 4], arry2);
                            if (ot == 40)
                            {
                                Listen.Abort();
                                Check.Abort();
                                OverTime(PLCPrm1[num, 4], arry2);
                                break;
                            }
                        }
                        next = 2;
                        Thread.Sleep(6000);
                        dataRE = "";
                    }
                }
            }
            #endregion

            if (NEXT == true)
            {
                //MessageBox.Show("连线测试 PASS" + "\n\n" + "请拔下所有接插件，并断开所有断路器");
                ShowResult showresult = new ShowResult("连线测试 PASS");
                showresult.ShowDialog();
                EnableButton1 ebutton1 = new EnableButton1(enablebutton1);
                button1.Invoke(ebutton1);
                Thread.CurrentThread.Abort();
            }
            else
            {
                //MessageBox.Show("连线测试 FAIL" + "\n\n" + "请拔下所有接插件，并断开所有断路器");
                ShowResult showresult = new ShowResult("连线测试 FAIL");
                showresult.ShowDialog();
                EnableButton ebutton = new EnableButton(enablebutton);
                button1.Invoke(ebutton);
                Thread.CurrentThread.Abort();
            }
            //MessageBox.Show("测试结束");
            //EnableButton ebutton1 = new EnableButton(enablebutton1);
            //button1.Invoke(ebutton1);

            Thread.CurrentThread.Abort();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            check = 0;
            NEXT = true;
            button1.Enabled = false;
            label7.ForeColor = Color.Red;
            label7.Text = "连线测试运行中...";
            SetTextCallback settextbox = new SetTextCallback(SetText);
            textBox1.Invoke(settextbox, "——————————————————————");
            Thread C = new Thread(Connection);
            C.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            serialPort1.Close();
            this.Close();
        }

        private void OverTime(string c, object arry)                                    //超时响应
        {
            System.Text.ASCIIEncoding asciiEncoding = new System.Text.ASCIIEncoding();

            string a = ": 01 05 08 28 FF 00";
            string b = GetLRC(a);
            byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
            serialPort1.Write(message1, 0, b.Length);

            Thread.Sleep(2000);
            try
            {
                if (ch372.OpenDevice(0) == 0)
                {
                    ch372.CloseDevice(0);
                }
                ch372.Init();
                ch372.OpenDevice(0);
            }
            catch //(Exception er) 
            {
                //MessageBox.Show("未检测到线材仪", "Error");
            }

            Listen = new Thread(new ThreadStart(StartListen));
            Listen.IsBackground = true;
            Listen.Start();

            Thread.Sleep(5000);
            Check = new Thread(new ParameterizedThreadStart(StartCheck));
            Check.IsBackground = true;
            a = ": 01 05 08 " + c + " FF 00";          //发送PLC指令
            b = GetLRC(a);
            message1 = System.Text.Encoding.ASCII.GetBytes(b);
            serialPort1.Write(message1, 0, b.Length);

            check = 0;

            Check.Start(arry);
            Thread.Sleep(3500);
            dataRE = "";
            //等待线材仪测试完毕

            while (next != 0)
            {
                // 不要着急，休息，休息一会儿  
                Thread.Sleep(500);
                if (next == 1) TryAgain(c, arry);
            }
            Thread.Sleep(6000);
            next = 2;
            //Check.Abort(); 
            dataRE = "";
        }

        private void TryAgain(string c, object arry)                                    //出错自我重检
        {
            Check = new Thread(new ParameterizedThreadStart(StartCheck));
            Check.IsBackground = true;
            Thread.Sleep(4000);

            string a = ": 01 05 08 " + c + " FF 00";          //发送PLC指令
            string b = GetLRC(a);
            byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
            serialPort1.Write(message1, 0, b.Length);

            Check.Start(arry);
            Thread.Sleep(3500);
            dataRE = "";
            //等待线材仪测试完毕

            int ot = 0;
            while (next != 0)
            {
                ot++;
                // 不要着急，休息，休息一会儿  
                Thread.Sleep(500);
                if (ot == 30)
                {
                    Listen.Abort();
                    Check.Abort();
                    OverTime(c, arry);
                    break;
                }
            }
            dataRE = "";
            Thread.Sleep(2500);    //修改
        }

    }
}
