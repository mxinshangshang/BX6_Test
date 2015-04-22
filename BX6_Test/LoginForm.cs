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
using System.Runtime.InteropServices;
using System.Data.OleDb;
using System.Threading.Tasks;
using System.Threading;

namespace BX6_Test
{
    public partial class LoginForm : Form
    {
        string LogPath;
        string SamplePath;
        string folderPath;

        string file;

        public string password = null;

        int index;
        private System.Data.DataSet myDataSet;
        public int iTextbox5 = 0;

        string[,] Parameters;
        string[,] PLCPrm1;
        string[,] PLCPrm2;
        string[,] PLCPrm3;
        string[,] PLCPrm4;
        int[] PLCPrm = new int[13];

        short[] PLCPrm_16 = new short[3];
        short[] M = new short[3];

        string dataRE = "";
        string[] datare = new string[60];
        int iData = 0;

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

        public LoginForm()
        {
            InitializeComponent();

            this.StartPosition = FormStartPosition.Manual;
            int xWidth = SystemInformation.PrimaryMonitorSize.Width;//获取显示器屏幕宽度
            this.Location = new Point(xWidth/2-this.Width/2, 0);
            System.Text.ASCIIEncoding asciiEncoding = new System.Text.ASCIIEncoding();

            //textBox2.Text = "68888888";                          //修改
            //textBox3.Text = "123456";                          //修改
            textBox9.Text = Properties.Settings.Default.HardwareSetting;
            textBox10.Text = Properties.Settings.Default.SoftwareSetting;

            try
            {
                if (ch372.OpenDevice(0) == 0)
                {
                    ch372.CloseDevice(0);
                }
                ch372.Init();
                ch372.OpenDevice(0);
                string str2;
                str2 = "USB::IDN?/n";
                byte[] data = Encoding.Default.GetBytes(str2);
                int s = data.Length;
                if (ch372.WriteData(0, data, ref s) == false) MessageBox.Show("未检测到PC连接线材仪", "Error");
                else textBox1.Text = "连接中…";
                byte[] buf = new byte[300];
                int len = 30;
                if (ch372.ReadData(0, buf, ref len) == false) textBox1.Text = "线材仪连接超时";
                else
                    textBox1.Text = asciiEncoding.GetString(buf);                //待修改
            }
            catch //(Exception er) 
            {
                MessageBox.Show("未检测到连接USB设备", "Error");
            }

            string[] ports1 = SerialPort.GetPortNames();                         //读取本机串口
            if (ports1 == null)
            {
                MessageBox.Show("本机没有串口！", "Error");
                return;
            }
            // Add all port names to the PLCCom:
            foreach (string port in ports1)
            {
                PLCCom.Items.Add(port);
                TELECom.Items.Add(port);
            }
            try
            {
                PLCCom.SelectedIndex = 0;
                TELECom.SelectedIndex = 1;
            }
            catch //(Exception er) 
            {
                MessageBox.Show("未检测到PC串口有连接", "Error");
            }
        }

        #region Path configuration
        private void 配置文件路径设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dilog = new FolderBrowserDialog();
            dilog.Description = "请选择物料文件路径";
            DialogResult result = dilog.ShowDialog();
            if (result == DialogResult.OK || result == DialogResult.Yes)
            {
                LogPath = dilog.SelectedPath;
            }
            Properties.Settings.Default.MaterialsSetting = LogPath;
            Properties.Settings.Default.Save();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dilog = new FolderBrowserDialog();
            dilog.Description = "请选择配置文件路径";
            DialogResult result = dilog.ShowDialog();
            if (result == DialogResult.OK || result == DialogResult.Yes)//if (dilog.ShowDialog() == DialogResult.OK || dilog.ShowDialog() == DialogResult.Yes)
            {
                SamplePath = dilog.SelectedPath;
            }
            Properties.Settings.Default.SampleSetting = SamplePath;
            Properties.Settings.Default.Save();
        }

        private void 测试记录文件保存路径ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "请选择测试记录文件保存路径";
            DialogResult result = folderBrowserDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                folderPath = folderBrowserDialog.SelectedPath;
            }
            Properties.Settings.Default.TestResultPathSetting = folderPath;
            Properties.Settings.Default.Save();
        }

        private void 密码设置ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form Password = new Password(Properties.Settings.Default.PasswordSetting);
            Password.Show();
        }

        private void 软硬件版本ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Passwordlogin PASSWORD = new Passwordlogin();
            PASSWORD.GetForm(this);
            PASSWORD.ShowDialog();
            if (password == Properties.Settings.Default.PasswordSetting)
            {
                Form HSVersion = new HSVersion();
                HSVersion.Show();
            }
            else
            {
                MessageBox.Show("此工号没有修改权限！！", "Error");
            }    
        }

        #endregion

        #region Port communication

        public delegate void DeleUpdateTextbox(string dataRe);

        private void UpdateTextbox(string dataRe)
        {
            datare[iData++] = dataRe;
            if (datare[iData - 1] == "0A " && datare[iData - 2] == "0D ")
            {
                dataRE = string.Join("", datare);
                textBox8.Text = dataRE;
                iData = 0;//0123修改
            }
        }

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string dataRe;

            byte[] byteRead = new byte[serialPort1.BytesToRead];

            DeleUpdateTextbox deleupdatetextbox = new DeleUpdateTextbox(UpdateTextbox);

            serialPort1.Read(byteRead, 0, byteRead.Length);


            System.Threading.Thread.Sleep(100);      //等待缓冲器满

            for (int i = 0; i < byteRead.Length; i++)
            {
                byte temp = byteRead[i];
                dataRe = temp.ToString("X2") + " ";
                textBox8.Invoke(deleupdatetextbox, dataRe);
            }
        }

        #endregion
        
        private string GetLRC(string a)                                         //计算LRC校验位
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

        public void GetFile(string fileDirectory)                               //读取.odl文件
        {
            index = 0;
            int i = 0;
            string[] words = new string[2];
            LogPath = Properties.Settings.Default.MaterialsSetting;
            string path = LogPath + "/" + fileDirectory;
            //try
            //{
                string[] text = File.ReadAllLines(path + "/" + fileDirectory + ".odl");
                for (int u = 0; u < text.Length; u++)
                {
                    if (text[u].Contains("                             "))
                    {
                        index++;                   
                    }
                    else continue;
                }
                Parameters = new string[index, 2];
                for (int u = 0; u < text.Length; u++)
                {
                    if (text[u].Contains("                             "))
                    {
                        words[0] = text[u].Substring(21, 9).Trim();
                        words[1] = text[u].Substring(58);
                        textBox6.AppendText(Environment.NewLine + words[0]);                              
                        textBox7.AppendText(Environment.NewLine + words[1]);
                        Parameters[i, 0] = words[0];
                        Parameters[i++, 1] = words[1];
                    }
                    else continue;
                }
            //}
            //catch (Exception e)
            //{
            //    MessageBox.Show(e.Message);
            //}
        }

        private void GetExcelAll()                                              //读入Excel全部内容
        {
            SamplePath = Properties.Settings.Default.SampleSetting;
            string path = SamplePath + "/";
            string strCon = " Provider = Microsoft.Jet.OLEDB.4.0 ; Data Source = " + path + "PLC.xls;Extended Properties='Excel 8.0;HDR=NO;IMEX=1;'";    //创建一个数据链接
            OleDbConnection myConn = new OleDbConnection(strCon);
            string strCom = " SELECT * FROM [Sheet1$] ";
            //try
            //{
                myConn.Open();
                OleDbDataAdapter myCommand = new OleDbDataAdapter(strCom, myConn);                 //打开数据链接，得到一个数据集
                myDataSet = new DataSet();                                                         //创建一个 DataSet对象
                myCommand.Fill(myDataSet, "[Sheet1$]");                                            //得到自己的DataSet对象
                myConn.Close();                                                                    //关闭此数据链接

                PLCPrm1 = new string[myDataSet.Tables[0].Rows.Count, 8];

                for (int i = 0; i < myDataSet.Tables[0].Rows.Count; i++)                           //读取Sheet1里的配置信息
                {
                    PLCPrm1[i, 0] = myDataSet.Tables[0].Rows[i].ItemArray[2].ToString();            //配置物料的名称
                    PLCPrm1[i, 1] = myDataSet.Tables[0].Rows[i].ItemArray[1].ToString();            //配置物料的ID号
                    PLCPrm1[i, 2] = myDataSet.Tables[0].Rows[i].ItemArray[3].ToString();            //配置物料第一命令地址
                    PLCPrm1[i, 3] = myDataSet.Tables[0].Rows[i].ItemArray[4].ToString();            //配置物料第二命令地址
                    PLCPrm1[i, 4] = myDataSet.Tables[0].Rows[i].ItemArray[5].ToString();            //配置物料第三命令地址
                    PLCPrm1[i, 5] = myDataSet.Tables[0].Rows[i].ItemArray[6].ToString();            //配置物料测试完成确认地址
                    PLCPrm1[i, 6] = myDataSet.Tables[0].Rows[i].ItemArray[7].ToString();            //配置物料线束连接提示
                    PLCPrm1[i, 7] = myDataSet.Tables[0].Rows[i].ItemArray[8].ToString();            //配置物料断路器闭合提示
                }
            //}
            //catch (Exception e)
            //{
            //    MessageBox.Show(e.Message);
            //}


            SamplePath = Properties.Settings.Default.SampleSetting;
            path = SamplePath + "/";
            strCon = " Provider = Microsoft.Jet.OLEDB.4.0 ; Data Source = " + path + "PLC.xls;Extended Properties='Excel 8.0;HDR=NO;IMEX=1;'";    //创建一个数据链接
            OleDbConnection myConn1 = new OleDbConnection(strCon);
            strCom = " SELECT * FROM [Sheet2$] ";
            //try
            //{
                myConn1.Open();
                myCommand = new OleDbDataAdapter(strCom, myConn1);                //打开数据链接，得到一个数据集
                myDataSet = new DataSet();                                                         //创建一个 DataSet对象
                myCommand.Fill(myDataSet, "[Sheet2$]");                                            //得到自己的DataSet对象
                myConn1.Close();                                                                   //关闭此数据链接

                PLCPrm2 = new string[myDataSet.Tables[0].Rows.Count, 16];

                for (int i = 0; i < myDataSet.Tables[0].Rows.Count; i++)                            //读取Sheet2里的配置信息
                {
                    PLCPrm2[i, 0] = myDataSet.Tables[0].Rows[i].ItemArray[2].ToString();            //配置物料的名称
                    PLCPrm2[i, 1] = myDataSet.Tables[0].Rows[i].ItemArray[1].ToString();            //配置物料的ID号
                    PLCPrm2[i, 2] = myDataSet.Tables[0].Rows[i].ItemArray[3].ToString();            //配置物料第一命令地址
                    PLCPrm2[i, 3] = myDataSet.Tables[0].Rows[i].ItemArray[4].ToString();            //配置物料电压值寄存器首地址
                    PLCPrm2[i, 4] = myDataSet.Tables[0].Rows[i].ItemArray[5].ToString();            //配置物料所需测电压值个数
                    PLCPrm2[i, 5] = myDataSet.Tables[0].Rows[i].ItemArray[6].ToString();            //配置物料对应PLC完成动作确认信号地址
                    PLCPrm2[i, 6] = myDataSet.Tables[0].Rows[i].ItemArray[7].ToString();            //配置物料第一电压值要求信息
                    PLCPrm2[i, 7] = myDataSet.Tables[0].Rows[i].ItemArray[8].ToString();            //配置物料第二电压值要求信息
                    PLCPrm2[i, 8] = myDataSet.Tables[0].Rows[i].ItemArray[9].ToString();            //配置物料第三电压值要求信息
                    PLCPrm2[i, 9] = myDataSet.Tables[0].Rows[i].ItemArray[10].ToString();           //配置物料第四电压值要求信息
                    PLCPrm2[i, 10] = myDataSet.Tables[0].Rows[i].ItemArray[11].ToString();          //配置物料第五电压值要求信息
                    PLCPrm2[i, 11] = myDataSet.Tables[0].Rows[i].ItemArray[12].ToString();          //配置物料第六电压值要求信息
                    PLCPrm2[i, 12] = myDataSet.Tables[0].Rows[i].ItemArray[13].ToString();          //配置物料满量程电压值
                    PLCPrm2[i, 13] = myDataSet.Tables[0].Rows[i].ItemArray[14].ToString();          //配置物料对应PLC完成动作确认信号信息
                    PLCPrm2[i, 14] = myDataSet.Tables[0].Rows[i].ItemArray[15].ToString();
                    PLCPrm2[i, 15] = myDataSet.Tables[0].Rows[i].ItemArray[16].ToString();
                }
            //}
            //catch (Exception e)
            //{
            //    MessageBox.Show(e.Message);
            //    return;
            //}
        }

        private void GetExcelPart()                                             //根据odl读入Excel部分内容
        {
            int sum = 0;
            SamplePath = Properties.Settings.Default.SampleSetting;
            string path = SamplePath + "/";
            string strCon = " Provider = Microsoft.Jet.OLEDB.4.0 ; Data Source = " + path + "PLC.xls;Extended Properties='Excel 8.0;HDR=NO;IMEX=1;'";    //创建一个数据链接
            OleDbConnection myConn = new OleDbConnection(strCon);
            string strCom = " SELECT * FROM [Sheet1$] ";
            //try
            //{
                myConn.Open();
                OleDbDataAdapter myCommand = new OleDbDataAdapter(strCom, myConn);                  //打开数据链接，得到一个数据集
                myDataSet = new DataSet();                                                          //创建一个 DataSet对象
                myCommand.Fill(myDataSet, "[Sheet1$]");                                             //得到自己的DataSet对象
                myConn.Close();                                                                     //关闭此数据链接

                PLCPrm1 = new string[myDataSet.Tables[0].Rows.Count, 8];

                for (int i = 0; i < myDataSet.Tables[0].Rows.Count; i++)                            //读取Sheet1里的配置信息
                {
                    PLCPrm1[i, 0] = myDataSet.Tables[0].Rows[i].ItemArray[2].ToString();            //配置物料的名称
                    PLCPrm1[i, 1] = myDataSet.Tables[0].Rows[i].ItemArray[1].ToString();            //配置物料的ID号
                    PLCPrm1[i, 2] = myDataSet.Tables[0].Rows[i].ItemArray[3].ToString();            //配置物料第一命令地址
                    PLCPrm1[i, 3] = myDataSet.Tables[0].Rows[i].ItemArray[4].ToString();            //配置物料第二命令地址
                    PLCPrm1[i, 4] = myDataSet.Tables[0].Rows[i].ItemArray[5].ToString();            //配置物料第三命令地址
                    PLCPrm1[i, 5] = myDataSet.Tables[0].Rows[i].ItemArray[6].ToString();            //配置物料测试完成确认地址
                    PLCPrm1[i, 6] = myDataSet.Tables[0].Rows[i].ItemArray[7].ToString();            //配置物料线束连接提示
                    PLCPrm1[i, 7] = myDataSet.Tables[0].Rows[i].ItemArray[8].ToString();            //配置物料断路器闭合提示
                }

                int k = 0;

                for (int j = 0; j < (PLCPrm1.Length / 8); j++)
                {
                    for (int i = 0; i < index; i++)
                    {
                        if (PLCPrm1[j, 1] == Parameters[i, 0])                                       //arry是Excel中的配置，Parameters是odl文件中提及的配置物料号
                        {
                            sum++;
                        }
                        else continue;
                    }
                }
                PLCPrm3 = new string[sum, 8];                                                        //待修改
                for (int j = 0; j < (PLCPrm1.Length / 8); j++)
                {
                    for (int i = 0; i < index; i++)
                    {
                        if (PLCPrm1[j, 1] == Parameters[i, 0])                                        //arry是Excel中的配置，Parameters是odl文件中提及的配置物料号
                        {
                            PLCPrm[j] = 1;
                            PLCPrm3[k, 0] = PLCPrm1[j, 0];                                            //配置物料的名称
                            PLCPrm3[k, 1] = PLCPrm1[j, 1];                                            //配置物料的ID号
                            PLCPrm3[k, 2] = PLCPrm1[j, 2];                                            //配置物料第一命令地址
                            PLCPrm3[k, 3] = PLCPrm1[j, 3];                                            //配置物料第二命令地址
                            PLCPrm3[k, 4] = PLCPrm1[j, 4];                                            //配置物料第三命令地址
                            PLCPrm3[k, 5] = PLCPrm1[j, 5];                                            //配置物料测试完成信号地址
                            PLCPrm3[k, 6] = PLCPrm1[j, 6];                                            //配置物料线束连接提示
                            PLCPrm3[k++, 7] = PLCPrm1[j, 7];                                          //配置物料断路器开断提示
                        }
                        else continue;
                    }
                }
            //}
            //catch (Exception e)
            //{
            //    MessageBox.Show(e.Message);
            //}

            sum = 0;
            SamplePath = Properties.Settings.Default.SampleSetting;
            path = SamplePath + "/";
            strCon = " Provider = Microsoft.Jet.OLEDB.4.0 ; Data Source = " + path + "PLC.xls;Extended Properties='Excel 8.0;HDR=NO;IMEX=1;'";    //创建一个数据链接
            OleDbConnection myConn1 = new OleDbConnection(strCon);
            strCom = " SELECT * FROM [Sheet2$] ";
            //try
            //{
                myConn1.Open();
                myCommand = new OleDbDataAdapter(strCom, myConn1);               //打开数据链接，得到一个数据集
                myDataSet = new DataSet();                                                        //创建一个 DataSet对象
                myCommand.Fill(myDataSet, "[Sheet2$]");                                           //得到自己的DataSet对象
                myConn1.Close();                                                                  //关闭此数据链接

                PLCPrm2 = new string[myDataSet.Tables[0].Rows.Count, 16];

                for (int i = 0; i < myDataSet.Tables[0].Rows.Count; i++)                          //读取Sheet2里的配置信息
                {
                    PLCPrm2[i, 0] = myDataSet.Tables[0].Rows[i].ItemArray[2].ToString();            //配置物料的名称
                    PLCPrm2[i, 1] = myDataSet.Tables[0].Rows[i].ItemArray[1].ToString();            //配置物料的ID号
                    PLCPrm2[i, 2] = myDataSet.Tables[0].Rows[i].ItemArray[3].ToString();            //配置物料第一命令地址
                    PLCPrm2[i, 3] = myDataSet.Tables[0].Rows[i].ItemArray[4].ToString();            //配置物料电压值寄存器首地址
                    PLCPrm2[i, 4] = myDataSet.Tables[0].Rows[i].ItemArray[5].ToString();            //配置物料所需测电压值个数
                    PLCPrm2[i, 5] = myDataSet.Tables[0].Rows[i].ItemArray[6].ToString();            //配置物料对应PLC完成动作确认信号地址
                    PLCPrm2[i, 6] = myDataSet.Tables[0].Rows[i].ItemArray[7].ToString();            //配置物料第一电压值要求信息
                    PLCPrm2[i, 7] = myDataSet.Tables[0].Rows[i].ItemArray[8].ToString();            //配置物料第二电压值要求信息
                    PLCPrm2[i, 8] = myDataSet.Tables[0].Rows[i].ItemArray[9].ToString();            //配置物料第三电压值要求信息
                    PLCPrm2[i, 9] = myDataSet.Tables[0].Rows[i].ItemArray[10].ToString();           //配置物料第四电压值要求信息
                    PLCPrm2[i, 10] = myDataSet.Tables[0].Rows[i].ItemArray[11].ToString();          //配置物料第五电压值要求信息
                    PLCPrm2[i, 11] = myDataSet.Tables[0].Rows[i].ItemArray[12].ToString();          //配置物料第六电压值要求信息
                    PLCPrm2[i, 12] = myDataSet.Tables[0].Rows[i].ItemArray[13].ToString();          //配置物料满量程电压值
                    PLCPrm2[i, 13] = myDataSet.Tables[0].Rows[i].ItemArray[14].ToString();          //配置物料对应PLC完成动作确认信号信息
                    PLCPrm2[i, 14] = myDataSet.Tables[0].Rows[i].ItemArray[15].ToString();
                    PLCPrm2[i, 15] = myDataSet.Tables[0].Rows[i].ItemArray[16].ToString();
                }

                k = 0;

                for (int j = 0; j < (PLCPrm2.Length / 16); j++)
                {
                    for (int i = 0; i < index; i++)
                    {
                        if (PLCPrm2[j, 1] == Parameters[i, 0])     //arry是Excel中的配置，Parameters是odl文件中提及的配置物料号
                        {
                            sum++;
                        }
                        else continue;
                    }
                }
                PLCPrm4 = new string[sum, 16];                    //待修改
                for (int j = 0; j < (PLCPrm2.Length / 16); j++)
                {
                    for (int i = 0; i < index; i++)
                    {
                        if (PLCPrm2[j, 1] == Parameters[i, 0])     //arry是Excel中的配置，Parameters是odl文件中提及的配置物料号
                        {
                            PLCPrm4[k, 0] = PLCPrm2[j, 0];            //配置物料的名称
                            PLCPrm4[k, 1] = PLCPrm2[j, 1];            //配置物料的ID号
                            PLCPrm4[k, 2] = PLCPrm2[j, 2];            //配置物料第一命令地址
                            PLCPrm4[k, 3] = PLCPrm2[j, 3];            //配置物料保存电压值寄存器首地址
                            PLCPrm4[k, 4] = PLCPrm2[j, 4];            //配置物料连读寄存器个数
                            PLCPrm4[k, 5] = PLCPrm2[j, 5];            //配置物料测试完成信号地址
                            PLCPrm4[k, 6] = PLCPrm2[j, 6];            //配置物料报错信息
                            PLCPrm4[k, 7] = PLCPrm2[j, 7];            //配置物料报错信息
                            PLCPrm4[k, 8] = PLCPrm2[j, 8];            //配置物料报错信息
                            PLCPrm4[k, 9] = PLCPrm2[j, 9];            //配置物料报错信息
                            PLCPrm4[k, 10] = PLCPrm2[j, 10];          //配置物料报错信息
                            PLCPrm4[k, 11] = PLCPrm2[j, 11];          //配置物料报错信息
                            PLCPrm4[k, 12] = PLCPrm2[j, 12];          //配置物料满量程电压值
                            PLCPrm4[k, 13] = PLCPrm2[j, 13];          //配置物料测试完成信号信息
                            PLCPrm4[k, 14] = PLCPrm2[j, 14];
                            PLCPrm4[k++, 15] = PLCPrm2[j, 15];
                        }
                        else continue;
                    }
                }
            //}
            //catch (Exception e)
            //{
            //    MessageBox.Show(e.Message);
            //    return;
            //}
        }

        private void button1_Click(object sender, EventArgs e)                  //自动测试
        {
            folderPath = Properties.Settings.Default.TestResultPathSetting;
            file = folderPath + "/" + textBox2.Text + " Test Result " + DateTime.Now.ToString("yyyy-MM-dd HHmmss") + ".txt";
            if (textBox2.Text.Trim() == "")
            {
                MessageBox.Show("未输入合同号", "Error");
            }
            else
            {
                try
                {
                    GetFile(textBox2.Text);        //调用GetFile函数，读取.odl文件
                    GetExcelPart();
                }
                catch (Exception er)
                {
                    MessageBox.Show("Error:" + er.Message, "Error");
                    return;
                }
            }
            if (textBox3.Text.Trim() == "")
            {
                MessageBox.Show("未输入工号", "Error");
            }
            else
            {
                try
                {
                    if (textBox1.Text.Contains("LX-560A"))// && serialPort1.IsOpen == true)              //修改
                    {
                        MessageBox.Show("PLC串口打开成功！" + "\r\n" + "线材仪连接成功", "提示");

                        serialPort1.Close();
                        textBox5.Clear();
                        Form AutoWire = new AutoW(file, PLCCom.Text, PLCPrm3, PLCPrm4, textBox2.Text, textBox3.Text, PLCPrm, TELECom.Text);
                        AutoWire.Show();

                        //Form AutoFun = new AutoF(file, PLCCom.Text, PLCPrm4, PLCPrm3, textBox2.Text, textBox3.Text, PLCPrm, TELECom.Text);//功能测试单项
                        //AutoFun.Show();

                        //Form AutoRun = new AutoR(file, PLCCom.Text, PLCPrm4, textBox2.Text, textBox3.Text, PLCPrm,TELECom.Text);//运行测试单项
                        //AutoRun.Show();
                    }
                    else
                    {
                        MessageBox.Show("线材仪连接失败请确保线材仪连接正确" + "\r\n" + "并复位线材仪后重启程序", "提示");
                        serialPort1.Close();
                    }
                }
                catch
                {
                    MessageBox.Show("串口打开异常 或正在被使用！！", "Error");
                }

            }
        }

        private void button2_Click(object sender, EventArgs e)                  //手动测试
        {
            Passwordlogin PASSWORD = new Passwordlogin();
            PASSWORD.GetForm(this);
            PASSWORD.ShowDialog();
            if (password == Properties.Settings.Default.PasswordSetting)
            {
                folderPath = Properties.Settings.Default.TestResultPathSetting;
                file = folderPath + "/" + textBox2.Text + " test result " + DateTime.Now.ToString("yyyy-MM-dd HHmmss") + ".txt";

                try
                {
                    GetExcelAll();
                }
                catch (Exception er)
                {
                    MessageBox.Show("Error:" + er.Message, "Error");
                    return;
                }
                try
                {
                    if (textBox1.Text.Contains("LX-560A"))// && serialPort1.IsOpen == true)                        //修改   
                    {
                        MessageBox.Show("PLC串口打开成功！" + "\r\n" + "线材仪连接成功", "提示");
                        serialPort1.Close();

                        Form Manu = new Manu(file, PLCCom.Text, PLCPrm1, PLCPrm2, textBox3.Text, TELECom.Text);
                        Manu.Show();
                    }
                    else
                    {
                        MessageBox.Show("线材仪连接失败请确保线材仪连接正确" + "\r\n" + "并复位线材仪后重启程序", "提示");
                        serialPort1.Close();
                    }
                }
                catch
                {
                    MessageBox.Show("串口打开异常 或正在被使用！！", "Error");
                }

            }
            else
            {
                MessageBox.Show("此工号没有手动测试权限！！", "Error");
            }        
        }

        private void button3_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.GetCurrentProcess().Kill();
            this.Close();
        }

        private void label6_Click(object sender, EventArgs e)                   //手动线材仪复位
        {
            System.Text.ASCIIEncoding asciiEncoding = new System.Text.ASCIIEncoding();
            try
            {
                String str1 = PLCCom.Text;
                if (str1 == null)
                {
                    MessageBox.Show("请先选择PLC串口！" + "/r/n" + "", "Error");
                    return;
                }

                serialPort1.PortName = str1;
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
            catch (Exception er)
            {
                MessageBox.Show("Error:" + er.Message, "Error");
                return;
            }

            string a = ": 01 05 08 28 FF 00";
            string b = GetLRC(a);
            byte[] message1 = System.Text.Encoding.ASCII.GetBytes(b);
            serialPort1.Write(message1, 0, b.Length);
            serialPort1.Close();

            Thread.Sleep(3000);
            try
            {
                if (ch372.OpenDevice(0) == 0)
                {
                    ch372.CloseDevice(0);
                }
                ch372.Init();
                ch372.OpenDevice(0);
                string str2;
                str2 = "USB::IDN?/n";
                byte[] data = Encoding.Default.GetBytes(str2);
                int s = data.Length;
                if (ch372.WriteData(0, data, ref s) == false) MessageBox.Show("未检测到PC连接线材仪", "Error");
                else textBox1.Text = "连接中…";
                byte[] buf = new byte[300];
                int len = 30;
                if (ch372.ReadData(0, buf, ref len) == false) textBox1.Text = "线材仪连接超时";
                else
                    textBox1.Text = asciiEncoding.GetString(buf);                //待修改
            }
            catch //(Exception er) 
            {
                MessageBox.Show("未检测到连接USB设备", "Error");
            }
        }
    }

    public static class ch372                                                   //USB_ch375协议封装   
    {
        public delegate void deleSetNotify(IntPtr iBuffer);
        public delegate void deleSetIntRoutine(IntPtr iBuffer);

        public const int DEVICE_ARRIVAL = 3;                          //设备插入事件,已经插入
        public const int DEVICE_REMOVE_PEND = 1;                      //设备将要拔出
        public const int DEVICE_REMOVE = 0;                           //设备拔出事件,已经拔出 

        //初始化
        public static void Init()
        {
            ReadLength = new int[1];
            ReadLength[0] = 0;
        }

        //打开设备
        public static int OpenDevice(int index)
        {
            return (int)CH375OpenDevice(index);
        }

        //复位设备
        public static bool ResetDevice(int index)
        {
            return CH375ResetDevice(index);
        }

        //关闭设备
        public static void CloseDevice(int index)
        {
            CH375CloseDevice(index);
        }

        //设置超时
        public static bool SetTimeout(int index, int writeTimeout, int redTimeout)
        {
            return CH375SetTimeout(index, writeTimeout, redTimeout);
        }

        //设置设备改动通知程序
        public static void SetDeviceNotify(int index, deleSetNotify iNotifyRoutine)
        {
            CH375SetDeviceNotify(index, null, iNotifyRoutine);
        }

        //设置设备中断上传通知程序
        public static void SetIntRoutine(int index, deleSetIntRoutine iNotifyRoutine)
        {
            CH375SetIntRoutine(index, iNotifyRoutine);
        }

        //读取数据
        public static bool ReadData(int index, byte[] buffer, ref int length)
        {
            ReadLength[0] = length;
            bool r = CH375ReadData(0, buffer, ReadLength);
            length = ReadLength[0];
            return r;
        }

        //写入数据
        public static bool WriteData(int index, byte[] buffer, ref int length)
        {
            ReadLength[0] = length;
            bool r = CH375WriteData(0, buffer, ReadLength);
            length = ReadLength[0];
            return r;
        }

        static int[] ReadLength;

        [DllImport("CH375DLL.dll")]
        static extern IntPtr CH375OpenDevice(int iIndex);

        //复位USB设备
        [DllImport("CH375DLL.dll", EntryPoint = "CH375ResetDevice")]
        static extern bool CH375ResetDevice(int iIndex);

        //关闭CH375设备
        [DllImport("CH375DLL.dll", EntryPoint = "CH375CloseDevice")]
        static extern void CH375CloseDevice(int iIndex);

        //设置超时
        [DllImport("CH375DLL.DLL")]
        static extern bool CH375SetTimeout(
                int iIndex, int iWriteTimeout, int iReadTimeout);

        //取消USB操作
        [DllImport("CH375DLL.DLL")]
        static extern bool CH375Abort(int iIndex);

        //读取数据
        [DllImport("CH375DLL.DLL", EntryPoint = "CH375ReadData", ExactSpelling = false, SetLastError = true)]
        static extern bool CH375ReadData(
        Int16 iIndex, [MarshalAs(UnmanagedType.LPArray)] byte[] oBuffer, [MarshalAs(UnmanagedType.LPArray)]  Int32[] ioLength);//读单片机缓存

        //写入数据
        [DllImport("CH375DLL.DLL", EntryPoint = "CH375WriteData", ExactSpelling = false, SetLastError = true)]
        static extern bool CH375WriteData(
        Int16 iIndex, [MarshalAs(UnmanagedType.LPArray)]  byte[] iBuffer, [MarshalAs(UnmanagedType.LPArray)]  Int32[] ioLength);   //写单片机缓存

        //设置中断服务函数
        [DllImport("CH375DLL.DLL ")]
        static extern bool CH375SetIntRoutine(
        int iIndex, deleSetIntRoutine iIntRoutine);

        //设定设备事件通知程序
        //可选参数,指向字符串,指定被监控的设备的ID,字符串以\0终止
        //指定设备事件回调程序,为NULL则取消事件通知
        [DllImport("CH375DLL.DLL ")]
        static extern bool CH375SetDeviceNotify(
        int iIndex, string iDeviceID, deleSetNotify iNotifyRoutine);
    }
}
