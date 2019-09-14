using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ILMergeWin
{
    public partial class FormAbout : Form
    {
        string _txt = @"
v.1.0
    Merge assemblies into one assembly.
    - [x] Select assemblies (Add, Delete, Clear).
    - [x] Merge assemblies.
    - [x] Show console output.
    - [x] Run in thread.
";

        public FormAbout()
        {
            InitializeComponent();
            this.tbHistory.Text = _txt.Trim();
        }

        private void FormAbout_Load(object sender, EventArgs e)
        {
            this.lblVersion.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void lnk_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(this.lnk.Text);
        }
    }
}
