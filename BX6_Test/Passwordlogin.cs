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
    public partial class Passwordlogin : Form
    {
        public string password = null;
        public LoginForm form = null;
        public Passwordlogin()
        {
            InitializeComponent();
            //textBox1.Text = "123456";
        }

        public void GetForm(LoginForm theform)
        {
            form = theform;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            form.password = textBox1.Text;
            this.Close();
        }
    }
}
