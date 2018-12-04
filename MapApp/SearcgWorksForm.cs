using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Eternal.MapApp
{
    public partial class SearcgWorksForm : Form
    {
        public String SearchWords;
        public SearcgWorksForm()
        {
            InitializeComponent();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(tbWords.Text.Trim()))
            {
                MessageBox.Show("请输入关键字！");
                return;
            }
            else
            {
                SearchWords = tbWords.Text;
                this.DialogResult = DialogResult.OK;
            }
        }
    }
}