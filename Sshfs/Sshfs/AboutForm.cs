using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Renci.SshNet;

namespace Sshfs
{
    public partial class AboutForm : Form
    {
        private static readonly string _sshfsText = String.Format("Sshfs {0}", Assembly.GetEntryAssembly().GetName().Version);
        private static readonly string _dokanText =
                        (DokanNet.Dokan.Version < 600)
                                ? String.Format("Dokan {0}.{1}.{2}",     DokanNet.Dokan.Version / 100, (DokanNet.Dokan.Version % 100) / 10, DokanNet.Dokan.Version % 10)
                                : String.Format("Dokan {0}.{1}.{2}.{3}", DokanNet.Dokan.Version / 1000, (DokanNet.Dokan.Version % 1000) / 100, (DokanNet.Dokan.Version % 100) / 10, DokanNet.Dokan.Version % 10);
        private static readonly string _sshnetText = String.Format("SSH.NET {0}", Assembly.GetAssembly(typeof (SshClient)).GetName().Version);

        public AboutForm()
        {
            InitializeComponent();
            label1.Text = _sshfsText;
            label2.Text = _dokanText;
            label3.Text = _sshnetText;
        }

        private void ok_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void linkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(String.Format("http:\\{0}", (sender as LinkLabel).Text));
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
          // ControlPaint.DrawBorder3D(e.Graphics,0,0,panel1.Width,panel1.Height,Border3DStyle.);
            e.Graphics.DrawLine(new Pen(SystemColors.ActiveBorder,3), 0, panel1.Height, panel1.Width, panel1.Height);

           // ControlPaint.DrawBorder(e.Graphics, new Rectangle(0, panel1.Height-1, panel1.Width, panel1.Height-1), SystemColors.ActiveBorder, ButtonBorderStyle.Dashed);
        }
    }
}
