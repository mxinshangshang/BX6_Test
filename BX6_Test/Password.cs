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
    public partial class Password : Form
    {
        string password;
        public Password(string password)
        {
            InitializeComponent();
            this.password = password;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox1.Text != password)
            {
                MessageBox.Show("原始密码错误请重新输入", "提示");
            }
            else if (textBox2.Text.Trim().Length == 0)
            {
                MessageBox.Show("新密码无效请重新输入", "提示");
            }
            else if (textBox1.Text == password && textBox2.Text.Trim() != null)
            {
                if (MessageBox.Show("确认修改密码？", "提示", MessageBoxButtons.OKCancel) == DialogResult.OK)
                {
                    Properties.Settings.Default.PasswordSetting = textBox2.Text;
                    Properties.Settings.Default.Save();
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
