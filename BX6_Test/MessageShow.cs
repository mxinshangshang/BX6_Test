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
    public partial class MessageShow : Form
    {
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

        public MessageShow(string w)
        {
            InitializeComponent();
            label1.Text = w;
            //label2.Text = b;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
