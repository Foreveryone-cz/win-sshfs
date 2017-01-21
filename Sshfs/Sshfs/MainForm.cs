#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using Renci.SshNet;
using Sshfs.Properties;
using System.Threading;

#endregion

namespace Sshfs
{
    public partial class MainForm : Form
    {
        private readonly StringBuilder _balloonText = new StringBuilder(255);
        private readonly List<SftpDrive> _drives = new List<SftpDrive>();
        private readonly List<String> _configVars = new List<String>();
        private readonly Regex _regex = new Regex(@"^New Drive\s\d{1,2}$", RegexOptions.Compiled);
        private readonly Queue<SftpDrive> _suspendedDrives = new Queue<SftpDrive>();

        private VirtualDrive virtualDrive;
        //private char virtualDriveLetter;

        private bool _balloonTipVisible;

        private int _lastindex = -1;
        private int _namecount;
        private bool _suspend;
        private bool _dirty;

        private bool _updateLockvirtualDriveBox = false;
        private bool _updateLockLetterBox = false;
        

        public MainForm()
        {
            InitializeComponent();
            Opacity = 0;
            driveListView.Columns[0].Width = driveListView.ClientRectangle.Width - 1;
            contextMenu.Renderer = new ContextMenuStripThemedRenderer();
            proxyType.SelectedIndexChanged += proxyType_SelectedIndexChanged;
            proxyType.SelectedIndex = 0;
        }

        void proxyType_SelectedIndexChanged(object sender, EventArgs e) {
          if (proxyType.SelectedIndex == 0) {
            proxyHostBox.Enabled = false;
            proxyLoginBox.Enabled = false;
            proxyPassBox.Enabled = false;
          }
          else {
            proxyHostBox.Enabled = true;
            proxyLoginBox.Enabled = true;
            proxyPassBox.Enabled = true;
          }
        }

        private void tryMountVFS()
        {
            if (virtualDrive.Letter==' ')
            {
                return;
            }

            try
            {    
                virtualDrive.Mount();
            }
            catch (Exception ex)
            {
                if (Visible)
                {
                    BeginInvoke(
                        new MethodInvoker(
                            () =>
                            MessageBox.Show(this,
                                            String.Format("{0} could not connect:\n{1}",
                                                          "Virtual drive", ex.Message), Text)));
                }
                else
                {
                    ShowBallon(String.Format("{0} : {1}", "Virtual drive", ex.Message), true);
                }
            }
        }

        protected override void OnLoad(EventArgs e)
        {

          
            notifyIcon.Text = Text = String.Format("Sshfs Manager - WinSshFS Foreveryone - v. {0}", Assembly.GetEntryAssembly().GetName().Version);
            portBox.Minimum = IPEndPoint.MinPort;
            portBox.Maximum = IPEndPoint.MaxPort;




            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile,
                                                                            Environment.SpecialFolderOption.None);//Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile,Environment.SpecialFolderOption.DoNotVerify),".ssh");

           /* if (!Directory.Exists(openFileDialog.InitialDirectory))
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal,
                                                                            Environment.SpecialFolderOption.DoNotVerify);*/

            startupMenuItem.Checked = Utilities.IsAppRegistredForStarup();

            // _drives.Persist("config.xml",true);            

            try {
                virtualDrive = virtualDrive.Load("vfs.xml");
            }
            catch {
                MessageBox.Show("Unable to load virtual drive info xml file.");
            }
            if (virtualDrive == null)
            {
                virtualDrive = new VirtualDrive
                {
                    Letter = ' '
                };
            }
            virtualDrive.StatusChanged += drive_VFSStatusChanged;

            updateVirtualDriveCombo();
            this.tryMountVFS();
            buttonVFSupdate();


            try {
                _drives.Load("config.xml");
            }
            catch
            {
                MessageBox.Show("Unable to load config file.");
            }


            driveListView.BeginUpdate();
            for (int i = 0; i < _drives.Count; i++)
            {
                driveListView.Items.Add(
                    (
                        _drives[i].Tag = new ListViewItem(_drives[i].Name, 0) {Tag = _drives[i]}
                    ) as ListViewItem
                );
                _drives[i].StatusChanged += drive_StatusChanged;
                if (_drives[i].Name.StartsWith("New Drive")) _namecount++;

                virtualDrive.AddSubFS(_drives[i]);
            }


            if (driveListView.Items.Count != 0)
            {
                driveListView.SelectedIndices.Add(0);
            }

            driveListView.Sorting = SortOrder.Ascending;

            driveListView.EndUpdate();

            //just to remove HScroll
            if (driveListView.Items.Count > 10)
            {
                driveListView.Items[10].EnsureVisible();
                driveListView.Items[0].EnsureVisible();
            }

            SetupPanels();

            


            SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
            base.OnLoad(e);
        }

        private void updateVirtualDriveCombo()
        {
            if (_updateLockvirtualDriveBox)
                return;
            this.virtualDriveCombo.BeginUpdate();

            this.virtualDriveCombo.Items.Clear();

            this.virtualDriveCombo.Items.Add(" Off");
            this.virtualDriveCombo.Items.AddRange(
                Utilities.GetAvailableDrives()
                    .Except(_drives.Select(d => d.Letter))
                    .Except(new char[] { virtualDrive.Letter })
                    .Select(l => String.Format("{0} :", l))
                    .ToArray()
            );
            if (virtualDrive.Letter!=' ')
                this.virtualDriveCombo.Items.Add(String.Format("{0} :", virtualDrive.Letter));


            this.virtualDriveCombo.SelectedIndex = this.virtualDriveCombo.FindString(virtualDrive.Letter.ToString());

            this.virtualDriveCombo.EndUpdate();
        }

        private void updateLetterBoxCombo(SftpDrive drive)
        {
            if (_updateLockLetterBox)
                return;
            if (drive == null)
            {
                if (driveListView.SelectedItems.Count == 0)
                    return;
                drive = driveListView.SelectedItems[0].Tag as SftpDrive;
                if (drive == null)
                    return;
            }

            letterBox.BeginUpdate();

            letterBox.Items.Clear();

            letterBox.Items.Add(" None");

            letterBox.Items.AddRange(
                Utilities.GetAvailableDrives()
                    .Except(_drives.Select(d => d.Letter))
                    .Except(new char[] {virtualDrive.Letter})
                    .Select(l => String.Format("{0} :", l))
                    .ToArray());

                
            if (drive.Letter!=' ')
                letterBox.Items.Add(String.Format("{0} :", drive.Letter));
                
            letterBox.SelectedIndex = letterBox.FindString(drive.Letter.ToString());

            letterBox.EndUpdate();
        }


        private void startupMenuItem_CheckedChanged(object sender, EventArgs e)
        {
            if (startupMenuItem.Checked)
            {
                Utilities.RegisterForStartup();
            }
            else
            {
                Utilities.UnregisterForStarup();
            }
        }

        private void SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            Debug.WriteLine("PowerModeChange:{0}", e.Mode);
            _suspend = e.Mode == PowerModes.Suspend;

            if (e.Mode == PowerModes.Resume)
            {
                while (_suspendedDrives.Count > 0)
                {
                    MountDrive(_suspendedDrives.Dequeue());
                }
            }
        }

        private void SetupPanels()
        {
            buttonPanel.Enabled = removeButton.Enabled = fieldsPanel.Enabled = driveListView.Items.Count != 0;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_dirty)
            {
                _drives.Persist("config.xml");
                //virtualDrive.per
            }

            if (e.CloseReason == CloseReason.UserClosing)
            {
                Visible = false;
                e.Cancel = true;
            }
            else
            {
                Debug.WriteLine("FormCOveride");
                notifyIcon.Visible = false;
            }
            base.OnFormClosing(e);
        }

        private void addButton_Click(object sender, EventArgs e)
        {
            char letter;
            try
            {
                letter = Utilities.GetAvailableDrives().Except(_drives.Select(d => d.Letter)).First();
            }
            catch
            {
                MessageBox.Show("No more drive letters available", Text);
                return;
            }


            var drive = new SftpDrive
                            {
                                Name = String.Format("New Drive {0}", ++_namecount),
                                Port = 22,
                                Root = ".",
                                Letter = letter,
                                MountPoint = "",
                                KeepAliveInterval = 30
                            };
            

            drive.StatusChanged += drive_StatusChanged;
            _drives.Add(drive);
            this.virtualDrive.AddSubFS(drive);
            var item =
                (drive.Tag = new ListViewItem(drive.Name, 0) {Tag = drive, Selected = true}) as
                ListViewItem;

            driveListView.Items.Add(item
                );
            item.EnsureVisible();


            SetupPanels();
            _dirty = true;
            
        }

        private void drive_StatusChanged(object sender, EventArgs e)
        {
            var drive = sender as SftpDrive;

            Debug.WriteLine("Status Changed {0}:{1}", sender, drive.Status);

            if (_suspend && drive.Status == DriveStatus.Unmounted)
            {
                _suspendedDrives.Enqueue(drive);
            }

            if (!Visible)
            {
                ShowBallon(String.Format("{0} : {1}", drive.Name,
                                         drive.Status == DriveStatus.Mounted ? "Mounted" : "Unmounted"),false);
            }

            BeginInvoke(new MethodInvoker(() =>
                                              {
                                                  var item =
                                                      drive.Tag as ListViewItem;


                                                  if (item.Selected)
                                                  {
                                                      muButton.Text = drive.Status == DriveStatus.Mounted
                                                                          ? "Unmount"
                                                                          : "Mount";
                                                      muButton.Image = drive.Status == DriveStatus.Mounted
                                                                           ? Resources.unmount
                                                                           : Resources.mount;
                                                      muButton.Enabled = true;
                                                  }
                                                  item.ImageIndex = drive.Status == DriveStatus.Mounted ? 1 : 0;
                                              }));
        }

        private void removeButton_Click(object sender, EventArgs e)
        {
            if (driveListView.SelectedItems.Count != 0 &&
                MessageBox.Show("Do you want to delete this drive?", Text, MessageBoxButtons.YesNo) ==
                DialogResult.Yes)
            {
                var drive = driveListView.SelectedItems[0].Tag as SftpDrive;


                drive.StatusChanged -= drive_StatusChanged;
                drive.Unmount();
                virtualDrive.RemoveSubFS(drive);
                _drives.Remove(drive);


                int next = driveListView.SelectedIndices[0] == driveListView.Items.Count - 1
                               ? driveListView.SelectedIndices[0] - 1
                               : driveListView.SelectedIndices[0];

                driveListView.Items.Remove(driveListView.SelectedItems[0]);

                if (next != -1)
                {
                    _lastindex = -1;
                    driveListView.SelectedIndices.Add(next);
                    driveListView.Items[next].EnsureVisible();
                }

                SetupPanels();
                _dirty = true;
            }
        }

        private void listView_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (e.IsSelected && _lastindex != e.ItemIndex)
            {
                _lastindex = e.ItemIndex;

                var drive = e.Item.Tag as SftpDrive;

                nameBox.Text = drive.Name;
                hostBox.Text = drive.Host;
                portBox.Value = drive.Port;
                userBox.Text = drive.Username;
                switch (drive.ConnectionType)
                {
                    case ConnectionType.Pageant: authCombo.SelectedIndex = 2; break;
                    case ConnectionType.PrivateKey: authCombo.SelectedIndex = 1; break;
                    default: authCombo.SelectedIndex=0; break;
                }

                if(drive.KeepAliveInterval <= 0)
                {
                    drive.KeepAliveInterval = 1;
                }

                updateLetterBoxCombo(drive);

                passwordBox.Text = drive.Password;
                directoryBox.Text = drive.Root;
                mountCheck.Checked = drive.Automount;
                passwordBox.Text = drive.Password;
                privateKeyBox.Text = drive.PrivateKey;
                passphraseBox.Text = drive.Passphrase;
                proxyType.SelectedIndex = drive.ProxyType;
                proxyHostBox.Text = drive.ProxyHost;
                proxyLoginBox.Text = drive.ProxyUser;
                proxyPassBox.Text = drive.ProxyPass;
                keepAliveIntervalBox.Value = drive.KeepAliveInterval;
                muButton.Text = drive.Status == DriveStatus.Mounted ? "Unmount" : "Mount";
                muButton.Image = drive.Status == DriveStatus.Mounted ? Resources.unmount : Resources.mount;
                muButton.Enabled = (drive.Status == DriveStatus.Unmounted || drive.Status == DriveStatus.Mounted);
                mountPointBox.Text = drive.MountPoint.Replace("/", "\\");//fix unix / to Windows standard
            }
        }

        private void authBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            authLabel.Text = String.Format("{0}:", authCombo.Text);
            passwordBox.Visible = authCombo.SelectedIndex == 0;
            privateKeyButton.Visible = passphraseBox.Visible = privateKeyBox.Visible = authCombo.SelectedIndex == 1;
        }

        private void keyButton_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                privateKeyBox.Text = openFileDialog.FileName;
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(nameBox.Text))
            {
                MessageBox.Show("Drive name connot be empty", Text);
                nameBox.Focus();
                return;
            }
            SftpDrive drive = driveListView.SelectedItems[0].Tag as SftpDrive;

            if ((_regex.IsMatch(nameBox.Text) || nameBox.Text == String.Format("{0}@'{1}'", drive.Username, drive.Host)) &&
                !String.IsNullOrEmpty(userBox.Text) && !String.IsNullOrEmpty(hostBox.Text))
            {
                nameBox.Text = String.Format("{0}@'{1}'", userBox.Text, hostBox.Text);
            }

            driveListView.SelectedItems[0].Text = drive.Name = nameBox.Text;
            driveListView.SelectedItems[0].EnsureVisible();
            driveListView.Sorting = SortOrder.None;
            driveListView.Sorting = SortOrder.Ascending;

            drive.Host = hostBox.Text;
            drive.Port = (int) portBox.Value;
            drive.Username = userBox.Text;
            switch (authCombo.SelectedIndex){
                case 2: drive.ConnectionType = ConnectionType.Pageant; break;
                case 1: drive.ConnectionType = ConnectionType.PrivateKey; break;
                default: drive.ConnectionType = ConnectionType.Password; break;
            }
            drive.Letter = letterBox.Text[0];
            drive.Root = directoryBox.Text.Trim();
            drive.Automount = mountCheck.Checked;
            drive.Password = passwordBox.Text;
            drive.PrivateKey = privateKeyBox.Text;
            drive.Passphrase = passphraseBox.Text;
            drive.MountPoint = mountPointBox.Text;
            drive.ProxyType = proxyType.SelectedIndex;
            drive.ProxyHost = proxyHostBox.Text;
            drive.ProxyUser = proxyLoginBox.Text;
            drive.ProxyPass = proxyPassBox.Text;
            drive.KeepAliveInterval = (int) keepAliveIntervalBox.Value;
            _dirty = true;
        }

        private void MountDrive(SftpDrive drive)
        {
            Task.Factory.StartNew(() =>
                                      {
                                          try
                                          {
                                              drive.Mount();
                                          }
                                          catch (Exception e)
                                          {
                                              BeginInvoke(new MethodInvoker(() =>
                                                                                {
                                                                                    if (
                                                                                        (drive.Tag as ListViewItem)
                                                                                            .Selected)
                                                                                    {
                                                                                        muButton.Enabled
                                                                                            = true;
                                                                                    }
                                                                                }));


                                              if (Visible)
                                              {
                                                  BeginInvoke(
                                                      new MethodInvoker(
                                                          () =>
                                                          MessageBox.Show(this,
                                                                          String.Format("{0} could not connect:\n{1}",
                                                                                        drive.Name, e.Message), Text)));
                                              }
                                              else
                                              {
                                                  ShowBallon(String.Format("{0} : {1}", drive.Name, e.Message),true);
                                              }
                                          }
                                      });
        }

        private void muButton_Click(object sender, EventArgs e)
        {
            var drive = driveListView.SelectedItems[0].Tag as SftpDrive;

            if (drive.Status == DriveStatus.Unmounted)
            {
                MountDrive(drive);
                muButton.Enabled = false;
            }
            else
            {
                drive.Unmount();
                muButton.Enabled = false;
            }
        }


        private void driveListView_MouseUpDown(object sender, MouseEventArgs e)
        {
            if (driveListView.HitTest(e.X, e.Y).Item == null && driveListView.Items.Count != 0)
            {
                driveListView.SelectedIndices.Add(_lastindex);
            }
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            Visible = false;
            Opacity = 1;
            Shown -= MainForm_Shown;

            foreach (var drive in _drives.Where(d => d.Automount))
            {
                MountDrive(drive);
                //no parallel mounting on startup fix:
                while (drive.Status == DriveStatus.Mounting)
                {
                    Thread.Sleep(100);
                }
            }
            if (_drives.Count != 0 && _drives[0].Automount)
                muButton.Enabled = false;
            ;
        }

        private void openFileDialog_FileOk(object sender, CancelEventArgs e)
        {
            //don't check larger files
            if (new FileInfo(openFileDialog.FileName).Length>4*4*1024/*||!PrivateKeyFile.IsValid(openFileDialog.FileName) not supported in current version, solved on open*/)
            {
                
                MessageBox.Show(this,
                                "File doesn't seem to be a valid private key file",Text);
                e.Cancel = true;
            }
        
        }

        private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
        {
            _balloonTipVisible = false;
            if (e.Button == MouseButtons.Left)
            {
                ReShow();
            }
        }

        private void exitMenuItem_Click(object sender, EventArgs e)
        {
            if(_drives.Any(i=>i.Status==DriveStatus.Mounted)&&MessageBox.Show(this,"Remote drives that are still connected will be closed on application exit.\nDo you wish to continue?",Text,MessageBoxButtons.YesNo)==DialogResult.No)
            {
                return;
            }


            Application.Exit();
        }

        private void aboutMenuItem_Click(object sender, EventArgs e)
        {
            var about = Application.OpenForms.OfType<AboutForm>().FirstOrDefault();
            if (about == null)
            {
                new AboutForm().ShowDialog(this);
            }
            else
            {
                about.Focus();
            }
        }

        private void showMenuItem_Click(object sender, EventArgs e)
        {
            ReShow();
        }

        public void ReShow()
        {
            
            TopMost = true;
            Visible = true;
            TopMost = false;
           
        }


        private void notifyIcon_BalloonTipClosed(object sender, EventArgs e)
        {
            _balloonTipVisible = false;
        }

        private void notifyIcon_BalloonTipShown(object sender, EventArgs e)
        {
            _balloonTipVisible = true;
        }

        private void ShowBallon(string text,bool error)
        {
            if (!_balloonTipVisible || (_balloonText.Length + text.Length) > 255)
            {
                _balloonText.Clear();
            }

            _balloonText.AppendLine(text);


            notifyIcon.ShowBalloonTip(0, Text, _balloonText.ToString().TrimEnd(),error?ToolTipIcon.Warning: ToolTipIcon.Info);
        }

        private void driveListView_ClientSizeChanged(object sender, EventArgs e)
        {
            Debug.WriteLine("CLIENT SIZE" + driveListView.ClientRectangle + driveListView.Columns[0].Width);

            //  driveListView.Scrollable = false;
            // driveListView.Refresh();
            driveListView.Columns[0].Width = driveListView.ClientRectangle.Width - 1;
        }


       
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
            
            _drives.Persist("config.xml");

            Parallel.ForEach(_drives.Where(d => d.Status != DriveStatus.Unmounted), d =>
                                                                                        {
                                                                                            d.StatusChanged -=
                                                                                                drive_StatusChanged;
                                                                                            d.Unmount();
                                                                                        });
            virtualDrive.Unmount();
            base.OnFormClosed(e);
        }

        private  void box_Leave(object sender, EventArgs e)
        {
            var box = sender as TextBox;
            box.Text = box.Text.Trim();
        }

        private void contextMenu_Opening(object sender, CancelEventArgs e)
        {
            mountMenuItem.Enabled = _drives.Any(drive => drive.Status == DriveStatus.Unmounted);
            unmountMenuItem.Enabled = _drives.Any(drive => drive.Status == DriveStatus.Mounted);
        }

        private void unmountMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            foreach (var drive in _drives.Where(d => d.Status == DriveStatus.Mounted))
            {
                var umitem = unmountMenuItem.DropDownItems.Add(drive.Name);
                umitem.Tag = drive;
                umitem.Click += umitem_Click;
            }
        }

        private void umitem_Click(object sender, EventArgs e)
        {
            ((sender as ToolStripItem).Tag as SftpDrive).Unmount();
        }


        private void unmountMenuItem_DropDownClosed(object sender, EventArgs e)
        {
            unmountMenuItem.DropDownItems.Clear();
        }

        private void mountMenuItem_DropDownClosed(object sender, EventArgs e)
        {
            mountMenuItem.DropDownItems.Clear();
        }

        private void mountMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            foreach (var drive in _drives.Where(d => d.Status == DriveStatus.Unmounted))
            {
                var mitem = mountMenuItem.DropDownItems.Add(drive.Name);
                mitem.Tag = drive;
                mitem.Click += mitem_Click;
            }
        }

        private void mitem_Click(object sender, EventArgs e)
        {
            var drive = (sender as ToolStripItem).Tag as SftpDrive;
            if (driveListView.SelectedItems[0].Tag == drive)
                muButton.Enabled = false;
            MountDrive(drive);
        }

        private void virtualDriveCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_updateLockvirtualDriveBox)
                return;

            virtualDrive.Letter = virtualDriveCombo.Text[0];
            virtualDrive.Persist("vfs.xml");

            _updateLockvirtualDriveBox = true; ;

            updateLetterBoxCombo(null);

            _updateLockvirtualDriveBox = false;

            this.buttonVFSupdate();
        }

        private void letterBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            _updateLockLetterBox = true;

            SftpDrive drive = driveListView.SelectedItems[0].Tag as SftpDrive;
            drive.Letter = letterBox.Text[0];
            
            this.updateVirtualDriveCombo();
            _updateLockLetterBox = false;
        }

        private void buttonVFSMount_Click(object sender, EventArgs e)
        {
            if (virtualDrive == null) return;//hmm

            if (virtualDrive.Status == DriveStatus.Unmounted)
            {
                this.tryMountVFS();
                
            }else if (virtualDrive.Status == DriveStatus.Mounted)
            {
                virtualDrive.Unmount();
            }

            buttonVFSMount.Enabled = false;
        }

        private void buttonVFSupdate()
        {
            this.BeginInvoke(new MethodInvoker(() =>
            {
                buttonVFSMount.Text = virtualDrive.Status == DriveStatus.Mounted
                                        ? "Unmount"
                                        : "Mount";
                buttonVFSMount.Image = virtualDrive.Status == DriveStatus.Mounted
                                            ? Resources.unmount
                                            : Resources.mount;
                buttonVFSMount.Enabled = (virtualDrive.Letter != ' ') || virtualDrive.Status == DriveStatus.Mounted;
            }));
        }

        private void drive_VFSStatusChanged(object sender, EventArgs e)
        {
            var drive = sender as SftpDrive;
            buttonVFSupdate();
        }

        private void contextMenu_PreviewKeyDown(object sender, System.Windows.Forms.PreviewKeyDownEventArgs e)
        {
            if (mountMenuItem.Selected && (e.KeyCode == Keys.Right || e.KeyCode == Keys.Enter))
            {
                mountMenuItem.ShowDropDown();
            }
            else
            {
                mountMenuItem.HideDropDown();
            }
            if (unmountMenuItem.Selected && (e.KeyCode == Keys.Right || e.KeyCode == Keys.Enter))
            {
                unmountMenuItem.ShowDropDown();
            }
            else
            {
                unmountMenuItem.HideDropDown();
            }
        }

        private void mountMenuItem_PreviewKeyDown(object sender, System.Windows.Forms.PreviewKeyDownEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Right:
                    contextMenu.AutoClose = false;
                    mountMenuItem.DropDown.AutoClose = false;
                    break;
                default:
                    contextMenu.AutoClose = true;
                    mountMenuItem.DropDown.AutoClose = true;
                    break;

            }
        }

        private void unmountMenuItem_PreviewKeyDown(object sender, System.Windows.Forms.PreviewKeyDownEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Right:
                    contextMenu.AutoClose = false;
                    unmountMenuItem.DropDown.AutoClose = false;
                    break;
                default:
                    contextMenu.AutoClose = true;
                    unmountMenuItem.DropDown.AutoClose = true;
                    break;

            }
        }

    }
}