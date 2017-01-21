namespace Sshfs
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.fieldsPanel = new System.Windows.Forms.TableLayoutPanel();
            this.proxyHostBox = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.proxyPassBox = new System.Windows.Forms.TextBox();
            this.proxyLoginBox = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.nameBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.hostBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.portBox = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.userBox = new System.Windows.Forms.TextBox();
            this.authCombo = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.letterBox = new System.Windows.Forms.ComboBox();
            this.mountCheck = new System.Windows.Forms.CheckBox();
            this.directoryBox = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.passphraseBox = new System.Windows.Forms.TextBox();
            this.privateKeyBox = new System.Windows.Forms.TextBox();
            this.privateKeyButton = new System.Windows.Forms.Button();
            this.passwordBox = new System.Windows.Forms.TextBox();
            this.authLabel = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.mountPointBox = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.proxyType = new System.Windows.Forms.ComboBox();
            this.labelKeepAlive = new System.Windows.Forms.Label();
            this.keepAliveIntervalBox = new System.Windows.Forms.NumericUpDown();
            this.driveListView = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.imageList = new System.Windows.Forms.ImageList(this.components);
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.removeButton = new System.Windows.Forms.Button();
            this.addButton = new System.Windows.Forms.Button();
            this.buttonPanel = new System.Windows.Forms.TableLayoutPanel();
            this.muButton = new System.Windows.Forms.Button();
            this.saveButton = new System.Windows.Forms.Button();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.buttonVFSMount = new System.Windows.Forms.Button();
            this.virtualDriveCombo = new System.Windows.Forms.ComboBox();
            this.label9 = new System.Windows.Forms.Label();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.showMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.mountMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.unmountMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.startupMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tableLayoutPanel1.SuspendLayout();
            this.fieldsPanel.SuspendLayout();
            this.panel3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.portBox)).BeginInit();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.keepAliveIntervalBox)).BeginInit();
            this.tableLayoutPanel2.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.contextMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 42.88703F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 57.11297F));
            this.tableLayoutPanel1.Controls.Add(this.fieldsPanel, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.driveListView, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.buttonPanel, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel3, 0, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(0, 2, 0, 0);
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 42F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 41F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(583, 469);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // fieldsPanel
            // 
            this.fieldsPanel.ColumnCount = 3;
            this.fieldsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 26.10063F));
            this.fieldsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 73.89937F));
            this.fieldsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.fieldsPanel.Controls.Add(this.proxyHostBox, 1, 10);
            this.fieldsPanel.Controls.Add(this.label12, 0, 10);
            this.fieldsPanel.Controls.Add(this.panel3, 1, 11);
            this.fieldsPanel.Controls.Add(this.label11, 0, 11);
            this.fieldsPanel.Controls.Add(this.nameBox, 1, 0);
            this.fieldsPanel.Controls.Add(this.label1, 0, 0);
            this.fieldsPanel.Controls.Add(this.hostBox, 1, 1);
            this.fieldsPanel.Controls.Add(this.label2, 0, 1);
            this.fieldsPanel.Controls.Add(this.portBox, 1, 2);
            this.fieldsPanel.Controls.Add(this.label3, 0, 2);
            this.fieldsPanel.Controls.Add(this.label4, 0, 3);
            this.fieldsPanel.Controls.Add(this.userBox, 1, 3);
            this.fieldsPanel.Controls.Add(this.authCombo, 1, 4);
            this.fieldsPanel.Controls.Add(this.label5, 0, 4);
            this.fieldsPanel.Controls.Add(this.label6, 0, 7);
            this.fieldsPanel.Controls.Add(this.panel1, 1, 7);
            this.fieldsPanel.Controls.Add(this.directoryBox, 1, 6);
            this.fieldsPanel.Controls.Add(this.label7, 0, 6);
            this.fieldsPanel.Controls.Add(this.panel2, 1, 5);
            this.fieldsPanel.Controls.Add(this.authLabel, 0, 5);
            this.fieldsPanel.Controls.Add(this.label8, 0, 8);
            this.fieldsPanel.Controls.Add(this.mountPointBox, 1, 8);
            this.fieldsPanel.Controls.Add(this.label10, 0, 9);
            this.fieldsPanel.Controls.Add(this.proxyType, 1, 9);
            this.fieldsPanel.Controls.Add(this.labelKeepAlive, 0, 12);
            this.fieldsPanel.Controls.Add(this.keepAliveIntervalBox, 1, 12);
            this.fieldsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.fieldsPanel.Location = new System.Drawing.Point(253, 5);
            this.fieldsPanel.Name = "fieldsPanel";
            this.fieldsPanel.RowCount = 14;
            this.tableLayoutPanel1.SetRowSpan(this.fieldsPanel, 2);
            this.fieldsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 29F));
            this.fieldsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 29F));
            this.fieldsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 29F));
            this.fieldsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 29F));
            this.fieldsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 29F));
            this.fieldsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 58F));
            this.fieldsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 29F));
            this.fieldsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 29F));
            this.fieldsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 29F));
            this.fieldsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 29F));
            this.fieldsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 29F));
            this.fieldsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 29F));
            this.fieldsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 29F));
            this.fieldsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.fieldsPanel.Size = new System.Drawing.Size(327, 420);
            this.fieldsPanel.TabIndex = 1;
            // 
            // proxyHostBox
            // 
            this.proxyHostBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.proxyHostBox.Location = new System.Drawing.Point(81, 322);
            this.proxyHostBox.Name = "proxyHostBox";
            this.proxyHostBox.Size = new System.Drawing.Size(216, 20);
            this.proxyHostBox.TabIndex = 10;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Dock = System.Windows.Forms.DockStyle.Left;
            this.label12.Location = new System.Drawing.Point(3, 319);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(50, 29);
            this.label12.TabIndex = 25;
            this.label12.Text = "Host:port";
            this.label12.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.proxyPassBox);
            this.panel3.Controls.Add(this.proxyLoginBox);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(81, 351);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(216, 23);
            this.panel3.TabIndex = 11;
            // 
            // proxyPassBox
            // 
            this.proxyPassBox.Dock = System.Windows.Forms.DockStyle.Right;
            this.proxyPassBox.Location = new System.Drawing.Point(111, 0);
            this.proxyPassBox.Name = "proxyPassBox";
            this.proxyPassBox.Size = new System.Drawing.Size(105, 20);
            this.proxyPassBox.TabIndex = 1;
            this.proxyPassBox.UseSystemPasswordChar = true;
            // 
            // proxyLoginBox
            // 
            this.proxyLoginBox.Dock = System.Windows.Forms.DockStyle.Left;
            this.proxyLoginBox.Location = new System.Drawing.Point(0, 0);
            this.proxyLoginBox.Name = "proxyLoginBox";
            this.proxyLoginBox.Size = new System.Drawing.Size(112, 20);
            this.proxyLoginBox.TabIndex = 0;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Dock = System.Windows.Forms.DockStyle.Left;
            this.label11.Location = new System.Drawing.Point(3, 348);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(64, 29);
            this.label11.TabIndex = 23;
            this.label11.Text = "Login/Pass:";
            this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // nameBox
            // 
            this.nameBox.Location = new System.Drawing.Point(81, 3);
            this.nameBox.Name = "nameBox";
            this.nameBox.Size = new System.Drawing.Size(216, 20);
            this.nameBox.TabIndex = 0;
            this.nameBox.Leave += new System.EventHandler(this.box_Leave);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Left;
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(66, 29);
            this.label1.TabIndex = 1;
            this.label1.Text = "Drive Name:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // hostBox
            // 
            this.hostBox.Location = new System.Drawing.Point(81, 32);
            this.hostBox.Name = "hostBox";
            this.hostBox.Size = new System.Drawing.Size(216, 20);
            this.hostBox.TabIndex = 1;
            this.hostBox.Leave += new System.EventHandler(this.box_Leave);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Left;
            this.label2.Location = new System.Drawing.Point(3, 29);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(32, 29);
            this.label2.TabIndex = 0;
            this.label2.Text = "Host:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // portBox
            // 
            this.portBox.Location = new System.Drawing.Point(81, 61);
            this.portBox.Name = "portBox";
            this.portBox.Size = new System.Drawing.Size(68, 20);
            this.portBox.TabIndex = 2;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Dock = System.Windows.Forms.DockStyle.Left;
            this.label3.Location = new System.Drawing.Point(3, 58);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(29, 29);
            this.label3.TabIndex = 5;
            this.label3.Text = "Port:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Dock = System.Windows.Forms.DockStyle.Left;
            this.label4.Location = new System.Drawing.Point(3, 87);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(58, 29);
            this.label4.TabIndex = 6;
            this.label4.Text = "Username:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // userBox
            // 
            this.userBox.Location = new System.Drawing.Point(81, 90);
            this.userBox.Name = "userBox";
            this.userBox.Size = new System.Drawing.Size(216, 20);
            this.userBox.TabIndex = 3;
            this.userBox.Leave += new System.EventHandler(this.box_Leave);
            // 
            // authCombo
            // 
            this.authCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.authCombo.FormattingEnabled = true;
            this.authCombo.Items.AddRange(new object[] {
            "Password",
            "PrivateKey",
            "Pageant"});
            this.authCombo.Location = new System.Drawing.Point(81, 119);
            this.authCombo.Name = "authCombo";
            this.authCombo.Size = new System.Drawing.Size(216, 21);
            this.authCombo.TabIndex = 4;
            this.authCombo.SelectedIndexChanged += new System.EventHandler(this.authBox_SelectedIndexChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Dock = System.Windows.Forms.DockStyle.Left;
            this.label5.Location = new System.Drawing.Point(3, 116);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(69, 29);
            this.label5.TabIndex = 9;
            this.label5.Text = "Authentication method:";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Dock = System.Windows.Forms.DockStyle.Left;
            this.label6.Location = new System.Drawing.Point(3, 232);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(65, 29);
            this.label6.TabIndex = 11;
            this.label6.Text = "Drive Letter:";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.letterBox);
            this.panel1.Controls.Add(this.mountCheck);
            this.panel1.Location = new System.Drawing.Point(81, 235);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(182, 23);
            this.panel1.TabIndex = 7;
            // 
            // letterBox
            // 
            this.letterBox.Dock = System.Windows.Forms.DockStyle.Left;
            this.letterBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.letterBox.FormattingEnabled = true;
            this.letterBox.Location = new System.Drawing.Point(0, 0);
            this.letterBox.Name = "letterBox";
            this.letterBox.Size = new System.Drawing.Size(68, 21);
            this.letterBox.Sorted = true;
            this.letterBox.TabIndex = 0;
            this.letterBox.SelectedIndexChanged += new System.EventHandler(this.letterBox_SelectedIndexChanged);
            // 
            // mountCheck
            // 
            this.mountCheck.AutoSize = true;
            this.mountCheck.Dock = System.Windows.Forms.DockStyle.Right;
            this.mountCheck.Location = new System.Drawing.Point(89, 0);
            this.mountCheck.Name = "mountCheck";
            this.mountCheck.Size = new System.Drawing.Size(93, 23);
            this.mountCheck.TabIndex = 1;
            this.mountCheck.Text = "Mount at login";
            this.mountCheck.UseVisualStyleBackColor = true;
            // 
            // directoryBox
            // 
            this.directoryBox.FormattingEnabled = true;
            this.directoryBox.Items.AddRange(new object[] {
            ".",
            "/"});
            this.directoryBox.Location = new System.Drawing.Point(81, 206);
            this.directoryBox.Name = "directoryBox";
            this.directoryBox.Size = new System.Drawing.Size(216, 21);
            this.directoryBox.TabIndex = 6;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Dock = System.Windows.Forms.DockStyle.Left;
            this.label7.Location = new System.Drawing.Point(3, 203);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(52, 29);
            this.label7.TabIndex = 14;
            this.label7.Text = "Directory:";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.passphraseBox);
            this.panel2.Controls.Add(this.privateKeyBox);
            this.panel2.Controls.Add(this.privateKeyButton);
            this.panel2.Controls.Add(this.passwordBox);
            this.panel2.Location = new System.Drawing.Point(81, 148);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(216, 52);
            this.panel2.TabIndex = 5;
            // 
            // passphraseBox
            // 
            this.passphraseBox.Location = new System.Drawing.Point(0, 26);
            this.passphraseBox.Name = "passphraseBox";
            this.passphraseBox.PasswordChar = '*';
            this.passphraseBox.Size = new System.Drawing.Size(220, 20);
            this.passphraseBox.TabIndex = 2;
            this.passphraseBox.Leave += new System.EventHandler(this.box_Leave);
            // 
            // privateKeyBox
            // 
            this.privateKeyBox.Location = new System.Drawing.Point(0, 0);
            this.privateKeyBox.Name = "privateKeyBox";
            this.privateKeyBox.Size = new System.Drawing.Size(186, 20);
            this.privateKeyBox.TabIndex = 0;
            this.privateKeyBox.Leave += new System.EventHandler(this.box_Leave);
            // 
            // privateKeyButton
            // 
            this.privateKeyButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.privateKeyButton.Location = new System.Drawing.Point(192, -1);
            this.privateKeyButton.Name = "privateKeyButton";
            this.privateKeyButton.Size = new System.Drawing.Size(28, 21);
            this.privateKeyButton.TabIndex = 1;
            this.privateKeyButton.Text = "...";
            this.privateKeyButton.UseVisualStyleBackColor = true;
            this.privateKeyButton.Click += new System.EventHandler(this.keyButton_Click);
            // 
            // passwordBox
            // 
            this.passwordBox.Location = new System.Drawing.Point(0, 0);
            this.passwordBox.Name = "passwordBox";
            this.passwordBox.PasswordChar = '*';
            this.passwordBox.Size = new System.Drawing.Size(220, 20);
            this.passwordBox.TabIndex = 7;
            // 
            // authLabel
            // 
            this.authLabel.AutoSize = true;
            this.authLabel.Dock = System.Windows.Forms.DockStyle.Left;
            this.authLabel.Location = new System.Drawing.Point(3, 145);
            this.authLabel.Name = "authLabel";
            this.authLabel.Padding = new System.Windows.Forms.Padding(0, 9, 0, 0);
            this.authLabel.Size = new System.Drawing.Size(43, 58);
            this.authLabel.TabIndex = 16;
            this.authLabel.Text = "______";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Dock = System.Windows.Forms.DockStyle.Left;
            this.label8.Location = new System.Drawing.Point(3, 261);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(69, 29);
            this.label8.TabIndex = 17;
            this.label8.Text = "Mount folder:";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // mountPointBox
            // 
            this.mountPointBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mountPointBox.Location = new System.Drawing.Point(81, 264);
            this.mountPointBox.Name = "mountPointBox";
            this.mountPointBox.Size = new System.Drawing.Size(216, 20);
            this.mountPointBox.TabIndex = 8;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Dock = System.Windows.Forms.DockStyle.Left;
            this.label10.Location = new System.Drawing.Point(3, 290);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(63, 29);
            this.label10.TabIndex = 19;
            this.label10.Text = "Proxy Type:";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // proxyType
            // 
            this.proxyType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.proxyType.FormattingEnabled = true;
            this.proxyType.Items.AddRange(new object[] {
            "None",
            "HTTP",
            "SOCKS4",
            "SOCKS5"});
            this.proxyType.Location = new System.Drawing.Point(81, 293);
            this.proxyType.Name = "proxyType";
            this.proxyType.Size = new System.Drawing.Size(216, 21);
            this.proxyType.TabIndex = 9;
            // 
            // labelKeepAlive
            // 
            this.labelKeepAlive.AutoSize = true;
            this.labelKeepAlive.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelKeepAlive.Location = new System.Drawing.Point(3, 377);
            this.labelKeepAlive.Name = "labelKeepAlive";
            this.labelKeepAlive.Size = new System.Drawing.Size(72, 29);
            this.labelKeepAlive.TabIndex = 26;
            this.labelKeepAlive.Text = "KeepAlive (s)";
            this.labelKeepAlive.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // keepAliveIntervalBox
            // 
            this.keepAliveIntervalBox.Location = new System.Drawing.Point(81, 380);
            this.keepAliveIntervalBox.Maximum = new decimal(new int[] {
            999,
            0,
            0,
            0});
            this.keepAliveIntervalBox.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.keepAliveIntervalBox.Name = "keepAliveIntervalBox";
            this.keepAliveIntervalBox.Size = new System.Drawing.Size(120, 20);
            this.keepAliveIntervalBox.TabIndex = 27;
            // 
            // driveListView
            // 
            this.driveListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.driveListView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.driveListView.FullRowSelect = true;
            this.driveListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.driveListView.HideSelection = false;
            this.driveListView.LabelWrap = false;
            this.driveListView.Location = new System.Drawing.Point(3, 5);
            this.driveListView.MultiSelect = false;
            this.driveListView.Name = "driveListView";
            this.driveListView.Size = new System.Drawing.Size(244, 378);
            this.driveListView.SmallImageList = this.imageList;
            this.driveListView.TabIndex = 0;
            this.driveListView.UseCompatibleStateImageBehavior = false;
            this.driveListView.View = System.Windows.Forms.View.Details;
            this.driveListView.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.listView_ItemSelectionChanged);
            this.driveListView.ClientSizeChanged += new System.EventHandler(this.driveListView_ClientSizeChanged);
            this.driveListView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.driveListView_MouseUpDown);
            this.driveListView.MouseUp += new System.Windows.Forms.MouseEventHandler(this.driveListView_MouseUpDown);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Width = 200;
            // 
            // imageList
            // 
            this.imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList.ImageStream")));
            this.imageList.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList.Images.SetKeyName(0, "network-offline.png");
            this.imageList.Images.SetKeyName(1, "connect_creating.png");
            this.imageList.Images.SetKeyName(2, "network-offline.png");
            this.imageList.Images.SetKeyName(3, "network-idle.png");
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Controls.Add(this.removeButton, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.addButton, 0, 0);
            this.tableLayoutPanel2.Location = new System.Drawing.Point(3, 389);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(200, 35);
            this.tableLayoutPanel2.TabIndex = 3;
            // 
            // removeButton
            // 
            this.removeButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.removeButton.Image = ((System.Drawing.Image)(resources.GetObject("removeButton.Image")));
            this.removeButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.removeButton.Location = new System.Drawing.Point(103, 3);
            this.removeButton.Name = "removeButton";
            this.removeButton.Size = new System.Drawing.Size(94, 29);
            this.removeButton.TabIndex = 1;
            this.removeButton.Text = "Remove";
            this.removeButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.removeButton.UseVisualStyleBackColor = true;
            this.removeButton.Click += new System.EventHandler(this.removeButton_Click);
            // 
            // addButton
            // 
            this.addButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.addButton.Image = ((System.Drawing.Image)(resources.GetObject("addButton.Image")));
            this.addButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.addButton.Location = new System.Drawing.Point(3, 3);
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size(94, 29);
            this.addButton.TabIndex = 0;
            this.addButton.Text = "Add";
            this.addButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.addButton.UseVisualStyleBackColor = true;
            this.addButton.Click += new System.EventHandler(this.addButton_Click);
            // 
            // buttonPanel
            // 
            this.buttonPanel.ColumnCount = 3;
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.buttonPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.buttonPanel.Controls.Add(this.muButton, 1, 0);
            this.buttonPanel.Controls.Add(this.saveButton, 1, 0);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonPanel.Location = new System.Drawing.Point(253, 431);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Padding = new System.Windows.Forms.Padding(0, 0, 15, 0);
            this.buttonPanel.RowCount = 1;
            this.buttonPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.buttonPanel.Size = new System.Drawing.Size(327, 35);
            this.buttonPanel.TabIndex = 2;
            // 
            // muButton
            // 
            this.muButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.muButton.Image = global::Sshfs.Properties.Resources.mount;
            this.muButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.muButton.Location = new System.Drawing.Point(199, 3);
            this.muButton.Name = "muButton";
            this.muButton.Size = new System.Drawing.Size(110, 29);
            this.muButton.TabIndex = 1;
            this.muButton.Text = "Mount";
            this.muButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.muButton.UseVisualStyleBackColor = true;
            this.muButton.Click += new System.EventHandler(this.muButton_Click);
            // 
            // saveButton
            // 
            this.saveButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.saveButton.Image = global::Sshfs.Properties.Resources.save;
            this.saveButton.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.saveButton.Location = new System.Drawing.Point(83, 3);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(110, 29);
            this.saveButton.TabIndex = 0;
            this.saveButton.Text = "Save";
            this.saveButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.ColumnCount = 3;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 70F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 94F));
            this.tableLayoutPanel3.Controls.Add(this.buttonVFSMount, 2, 0);
            this.tableLayoutPanel3.Controls.Add(this.virtualDriveCombo, 1, 0);
            this.tableLayoutPanel3.Controls.Add(this.label9, 0, 0);
            this.tableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel3.Location = new System.Drawing.Point(3, 431);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 1;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(244, 35);
            this.tableLayoutPanel3.TabIndex = 4;
            // 
            // buttonVFSMount
            // 
            this.buttonVFSMount.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonVFSMount.Image = global::Sshfs.Properties.Resources.mount;
            this.buttonVFSMount.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.buttonVFSMount.Location = new System.Drawing.Point(153, 3);
            this.buttonVFSMount.Name = "buttonVFSMount";
            this.buttonVFSMount.Size = new System.Drawing.Size(88, 29);
            this.buttonVFSMount.TabIndex = 1;
            this.buttonVFSMount.Text = "Mount";
            this.buttonVFSMount.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.buttonVFSMount.UseVisualStyleBackColor = true;
            this.buttonVFSMount.Click += new System.EventHandler(this.buttonVFSMount_Click);
            // 
            // virtualDriveCombo
            // 
            this.virtualDriveCombo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.virtualDriveCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.virtualDriveCombo.DropDownWidth = 43;
            this.virtualDriveCombo.FormattingEnabled = true;
            this.virtualDriveCombo.Items.AddRange(new object[] {
            "off"});
            this.virtualDriveCombo.Location = new System.Drawing.Point(83, 3);
            this.virtualDriveCombo.Name = "virtualDriveCombo";
            this.virtualDriveCombo.Size = new System.Drawing.Size(64, 21);
            this.virtualDriveCombo.Sorted = true;
            this.virtualDriveCombo.TabIndex = 0;
            this.virtualDriveCombo.SelectedIndexChanged += new System.EventHandler(this.virtualDriveCombo_SelectedIndexChanged);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Dock = System.Windows.Forms.DockStyle.Left;
            this.label9.Location = new System.Drawing.Point(3, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(65, 35);
            this.label9.TabIndex = 6;
            this.label9.Text = "Virtual drive:";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // openFileDialog
            // 
            this.openFileDialog.FileName = "id_rsa";
            this.openFileDialog.Filter = "Pivate Key Files (*.*)|*";
            this.openFileDialog.Title = "Open Private Key";
            this.openFileDialog.FileOk += new System.ComponentModel.CancelEventHandler(this.openFileDialog_FileOk);
            // 
            // notifyIcon
            // 
            this.notifyIcon.ContextMenuStrip = this.contextMenu;
            this.notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon.Icon")));
            this.notifyIcon.Text = "Sshfs";
            this.notifyIcon.Visible = true;
            this.notifyIcon.BalloonTipClosed += new System.EventHandler(this.notifyIcon_BalloonTipClosed);
            this.notifyIcon.BalloonTipShown += new System.EventHandler(this.notifyIcon_BalloonTipShown);
            this.notifyIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon_MouseClick);
            this.notifyIcon.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon_MouseClick);
            // 
            // contextMenu
            // 
            this.contextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.showMenuItem,
            this.toolStripSeparator1,
            this.mountMenuItem,
            this.unmountMenuItem,
            this.toolStripSeparator2,
            this.startupMenuItem,
            this.aboutMenuItem,
            this.exitMenuItem});
            this.contextMenu.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.Table;
            this.contextMenu.MaximumSize = new System.Drawing.Size(135, 0);
            this.contextMenu.Name = "contextMenuStrip1";
            this.contextMenu.ShowCheckMargin = true;
            this.contextMenu.ShowImageMargin = false;
            this.contextMenu.Size = new System.Drawing.Size(135, 148);
            this.contextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenu_Opening);
            this.contextMenu.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.contextMenu_PreviewKeyDown);
            // 
            // showMenuItem
            // 
            this.showMenuItem.AutoSize = false;
            this.showMenuItem.Name = "showMenuItem";
            this.showMenuItem.Size = new System.Drawing.Size(134, 22);
            this.showMenuItem.Text = "Show Manager";
            this.showMenuItem.Click += new System.EventHandler(this.showMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.AutoSize = false;
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(131, 6);
            // 
            // mountMenuItem
            // 
            this.mountMenuItem.AutoSize = false;
            this.mountMenuItem.Name = "mountMenuItem";
            this.mountMenuItem.Size = new System.Drawing.Size(134, 22);
            this.mountMenuItem.Text = "Mount";
            this.mountMenuItem.DropDownClosed += new System.EventHandler(this.mountMenuItem_DropDownClosed);
            this.mountMenuItem.DropDownOpening += new System.EventHandler(this.mountMenuItem_DropDownOpening);
            this.mountMenuItem.DropDown.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.mountMenuItem_PreviewKeyDown);
            // 
            // unmountMenuItem
            // 
            this.unmountMenuItem.AutoSize = false;
            this.unmountMenuItem.Name = "unmountMenuItem";
            this.unmountMenuItem.Size = new System.Drawing.Size(134, 22);
            this.unmountMenuItem.Text = "Unmount";
            this.unmountMenuItem.DropDownClosed += new System.EventHandler(this.unmountMenuItem_DropDownClosed);
            this.unmountMenuItem.DropDownOpening += new System.EventHandler(this.unmountMenuItem_DropDownOpening);
            this.unmountMenuItem.DropDown.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.unmountMenuItem_PreviewKeyDown);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(131, 6);
            // 
            // startupMenuItem
            // 
            this.startupMenuItem.AutoSize = false;
            this.startupMenuItem.CheckOnClick = true;
            this.startupMenuItem.Name = "startupMenuItem";
            this.startupMenuItem.Size = new System.Drawing.Size(134, 22);
            this.startupMenuItem.Text = "Run at startup";
            this.startupMenuItem.CheckedChanged += new System.EventHandler(this.startupMenuItem_CheckedChanged);
            // 
            // aboutMenuItem
            // 
            this.aboutMenuItem.AutoSize = false;
            this.aboutMenuItem.Name = "aboutMenuItem";
            this.aboutMenuItem.Size = new System.Drawing.Size(134, 22);
            this.aboutMenuItem.Text = "About";
            this.aboutMenuItem.Click += new System.EventHandler(this.aboutMenuItem_Click);
            // 
            // exitMenuItem
            // 
            this.exitMenuItem.AutoSize = false;
            this.exitMenuItem.Name = "exitMenuItem";
            this.exitMenuItem.Size = new System.Drawing.Size(134, 22);
            this.exitMenuItem.Text = "Exit";
            this.exitMenuItem.Click += new System.EventHandler(this.exitMenuItem_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(583, 469);
            this.Controls.Add(this.tableLayoutPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MainForm";
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.fieldsPanel.ResumeLayout(false);
            this.fieldsPanel.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.portBox)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.keepAliveIntervalBox)).EndInit();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.buttonPanel.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            this.contextMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.ImageList imageList;
        private System.Windows.Forms.ListView driveListView;
        private System.Windows.Forms.Button addButton;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Button removeButton;
        private System.Windows.Forms.TableLayoutPanel fieldsPanel;
        private System.Windows.Forms.TextBox nameBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox hostBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown portBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox userBox;
        private System.Windows.Forms.ComboBox authCombo;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ComboBox letterBox;
        private System.Windows.Forms.CheckBox mountCheck;
        private System.Windows.Forms.ComboBox directoryBox;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TableLayoutPanel buttonPanel;
        private System.Windows.Forms.Button muButton;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.TextBox passwordBox;
        private System.Windows.Forms.Label authLabel;
        private System.Windows.Forms.TextBox privateKeyBox;
        private System.Windows.Forms.Button privateKeyButton;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.TextBox passphraseBox;
        private System.Windows.Forms.ContextMenuStrip contextMenu;
        private System.Windows.Forms.ToolStripMenuItem exitMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aboutMenuItem;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ToolStripMenuItem startupMenuItem;
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ToolStripMenuItem showMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem mountMenuItem;
        private System.Windows.Forms.ToolStripMenuItem unmountMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox mountPointBox;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.ComboBox virtualDriveCombo;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Button buttonVFSMount;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ComboBox proxyType;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox proxyHostBox;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.TextBox proxyPassBox;
        private System.Windows.Forms.TextBox proxyLoginBox;
        private System.Windows.Forms.Label labelKeepAlive;
        private System.Windows.Forms.NumericUpDown keepAliveIntervalBox;
    }
}