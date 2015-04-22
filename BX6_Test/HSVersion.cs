using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BX6_Test
{
    public partial class HSVersion : Form
    {
        protected override void WndProc(ref   Message m)     //禁用右上角关闭按钮
        {
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_CLOSE = 0xF060;
            if (m.Msg == WM_SYSCOMMAND && (int)m.WParam == SC_CLOSE)
            {
                return;
            }
            base.WndProc(ref m);
        }

        public HSVersion()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Trim().Length == 0)
            {
                MessageBox.Show("硬件版本输入为空请重新输入", "提示");
            }
            else if (textBox2.Text.Trim().Length == 0)
            {
                MessageBox.Show("软件版本输入为空请重新输入", "提示");
            }
            else if (textBox1.Text.Trim() != null && textBox2.Text.Trim() != null)
            {
                if (MessageBox.Show("HW Version: "+textBox1.Text+"\r\n"+"SW Version: "+textBox2.Text+"\r\n"+"确认修改？", "提示", MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    Properties.Settings.Default.HardwareSetting = textBox1.Text;
                    Properties.Settings.Default.SoftwareSetting = textBox2.Text;
                    Properties.Settings.Default.Save();
                    MessageBox.Show("设置完毕软件即将关闭"+"\r\n"+"请重启软件", "提示");
                    System.Diagnostics.Process.GetCurrentProcess().Kill();
                    this.Close();
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
