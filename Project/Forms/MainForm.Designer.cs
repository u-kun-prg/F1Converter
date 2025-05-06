namespace F1Converter
{
	partial class MainForm
	{
		/// <summary>
		/// 必要なデザイナー変数です。
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// 使用中のリソースをすべてクリーンアップします。
		/// </summary>
		/// <param name="disposing">マネージド リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows フォーム デザイナーで生成されたコード

		/// <summary>
		/// デザイナー サポートに必要なメソッドです。このメソッドの内容を
		/// コード エディターで変更しないでください。
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.chkBoxNoPCMBlk = new System.Windows.Forms.CheckBox();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.btnReset = new System.Windows.Forms.Button();
			this.cmbBoxSerialPort = new System.Windows.Forms.ComboBox();
			this.labelUploadOffset = new System.Windows.Forms.Label();
			this.numUDOffset = new System.Windows.Forms.NumericUpDown();
			this.labelSerialPort = new System.Windows.Forms.Label();
			this.chkBoxS98Out = new System.Windows.Forms.CheckBox();
			this.chkBoxDumpOut = new System.Windows.Forms.CheckBox();
			this.chkBoxTextOut = new System.Windows.Forms.CheckBox();
			this.chkBoxUpload = new System.Windows.Forms.CheckBox();
			this.chkBoxF1TOut = new System.Windows.Forms.CheckBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.label2 = new System.Windows.Forms.Label();
			this.numUDTempo = new System.Windows.Forms.NumericUpDown();
			this.labelTempo = new System.Windows.Forms.Label();
			this.chkBoxYM2612RL = new System.Windows.Forms.CheckBox();
			this.numUDLoop = new System.Windows.Forms.NumericUpDown();
			this.labelLoop = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.labelFMVolPer = new System.Windows.Forms.Label();
			this.numUDSSGVol = new System.Windows.Forms.NumericUpDown();
			this.labelSSGVolAdd = new System.Windows.Forms.Label();
			this.numUDFMVol = new System.Windows.Forms.NumericUpDown();
			this.labelFMVolDown = new System.Windows.Forms.Label();
			this.chkBoxClockAdjust = new System.Windows.Forms.CheckBox();
			this.chkBoxPCM = new System.Windows.Forms.CheckBox();
			this.chkBoxTimerReg = new System.Windows.Forms.CheckBox();
			this.chkBoxDual2nd = new System.Windows.Forms.CheckBox();
			this.chkBoxShrink = new System.Windows.Forms.CheckBox();
			this.groupBoxTarget = new System.Windows.Forms.GroupBox();
			this.cmbBoxChip2Clock = new System.Windows.Forms.ComboBox();
			this.cmbBoxChip1Clock = new System.Windows.Forms.ComboBox();
			this.labelChip2 = new System.Windows.Forms.Label();
			this.labelChip1 = new System.Windows.Forms.Label();
			this.cmbBoxTarget = new System.Windows.Forms.ComboBox();
			this.chkBoxF1Out = new System.Windows.Forms.CheckBox();
			this.btnConvert = new System.Windows.Forms.Button();
			this.btnFileRef = new System.Windows.Forms.Button();
			this.chkBoxVerify = new System.Windows.Forms.CheckBox();
			this.tBoxUploadFile = new System.Windows.Forms.TextBox();
			this.labelUploadFIle = new System.Windows.Forms.Label();
			this.textBoxString = new System.Windows.Forms.TextBox();
			this.statusStrip1 = new System.Windows.Forms.StatusStrip();
			this.tsslblProtcol = new System.Windows.Forms.ToolStripStatusLabel();
			this.tspbarLoad = new System.Windows.Forms.ToolStripProgressBar();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.fileFToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.exitXToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolTToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.rescanSerialPortRToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.portBToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.helpHToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.aboutThisProgramAToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numUDOffset)).BeginInit();
			this.groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numUDTempo)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numUDLoop)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numUDSSGVol)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numUDFMVol)).BeginInit();
			this.groupBoxTarget.SuspendLayout();
			this.statusStrip1.SuspendLayout();
			this.menuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// splitContainer1
			// 
			this.splitContainer1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			resources.ApplyResources(this.splitContainer1, "splitContainer1");
			this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.chkBoxNoPCMBlk);
			this.splitContainer1.Panel1.Controls.Add(this.groupBox2);
			this.splitContainer1.Panel1.Controls.Add(this.chkBoxS98Out);
			this.splitContainer1.Panel1.Controls.Add(this.chkBoxDumpOut);
			this.splitContainer1.Panel1.Controls.Add(this.chkBoxTextOut);
			this.splitContainer1.Panel1.Controls.Add(this.chkBoxUpload);
			this.splitContainer1.Panel1.Controls.Add(this.chkBoxF1TOut);
			this.splitContainer1.Panel1.Controls.Add(this.groupBox1);
			this.splitContainer1.Panel1.Controls.Add(this.groupBoxTarget);
			this.splitContainer1.Panel1.Controls.Add(this.chkBoxF1Out);
			this.splitContainer1.Panel1.Controls.Add(this.btnConvert);
			this.splitContainer1.Panel1.Controls.Add(this.btnFileRef);
			this.splitContainer1.Panel1.Controls.Add(this.chkBoxVerify);
			this.splitContainer1.Panel1.Controls.Add(this.tBoxUploadFile);
			this.splitContainer1.Panel1.Controls.Add(this.labelUploadFIle);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.textBoxString);
			this.splitContainer1.Panel2.Controls.Add(this.statusStrip1);
			// 
			// chkBoxNoPCMBlk
			// 
			resources.ApplyResources(this.chkBoxNoPCMBlk, "chkBoxNoPCMBlk");
			this.chkBoxNoPCMBlk.Name = "chkBoxNoPCMBlk";
			this.chkBoxNoPCMBlk.UseVisualStyleBackColor = true;
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.btnReset);
			this.groupBox2.Controls.Add(this.cmbBoxSerialPort);
			this.groupBox2.Controls.Add(this.labelUploadOffset);
			this.groupBox2.Controls.Add(this.numUDOffset);
			this.groupBox2.Controls.Add(this.labelSerialPort);
			resources.ApplyResources(this.groupBox2, "groupBox2");
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.TabStop = false;
			// 
			// btnReset
			// 
			resources.ApplyResources(this.btnReset, "btnReset");
			this.btnReset.Name = "btnReset";
			this.btnReset.UseVisualStyleBackColor = true;
			this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
			// 
			// cmbBoxSerialPort
			// 
			this.cmbBoxSerialPort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbBoxSerialPort.DropDownWidth = 94;
			this.cmbBoxSerialPort.FormattingEnabled = true;
			resources.ApplyResources(this.cmbBoxSerialPort, "cmbBoxSerialPort");
			this.cmbBoxSerialPort.Name = "cmbBoxSerialPort";
			// 
			// labelUploadOffset
			// 
			resources.ApplyResources(this.labelUploadOffset, "labelUploadOffset");
			this.labelUploadOffset.Name = "labelUploadOffset";
			// 
			// numUDOffset
			// 
			this.numUDOffset.Hexadecimal = true;
			this.numUDOffset.Increment = new decimal(new int[] {
            1024,
            0,
            0,
            0});
			resources.ApplyResources(this.numUDOffset, "numUDOffset");
			this.numUDOffset.Maximum = new decimal(new int[] {
            32768,
            0,
            0,
            0});
			this.numUDOffset.Name = "numUDOffset";
			this.numUDOffset.ReadOnly = true;
			this.numUDOffset.Value = new decimal(new int[] {
            4096,
            0,
            0,
            0});
			// 
			// labelSerialPort
			// 
			resources.ApplyResources(this.labelSerialPort, "labelSerialPort");
			this.labelSerialPort.Name = "labelSerialPort";
			// 
			// chkBoxS98Out
			// 
			resources.ApplyResources(this.chkBoxS98Out, "chkBoxS98Out");
			this.chkBoxS98Out.Name = "chkBoxS98Out";
			this.chkBoxS98Out.UseVisualStyleBackColor = true;
			// 
			// chkBoxDumpOut
			// 
			resources.ApplyResources(this.chkBoxDumpOut, "chkBoxDumpOut");
			this.chkBoxDumpOut.Name = "chkBoxDumpOut";
			this.chkBoxDumpOut.UseVisualStyleBackColor = true;
			// 
			// chkBoxTextOut
			// 
			resources.ApplyResources(this.chkBoxTextOut, "chkBoxTextOut");
			this.chkBoxTextOut.Name = "chkBoxTextOut";
			this.chkBoxTextOut.UseVisualStyleBackColor = true;
			// 
			// chkBoxUpload
			// 
			resources.ApplyResources(this.chkBoxUpload, "chkBoxUpload");
			this.chkBoxUpload.Name = "chkBoxUpload";
			this.chkBoxUpload.UseVisualStyleBackColor = true;
			// 
			// chkBoxF1TOut
			// 
			resources.ApplyResources(this.chkBoxF1TOut, "chkBoxF1TOut");
			this.chkBoxF1TOut.Name = "chkBoxF1TOut";
			this.chkBoxF1TOut.UseVisualStyleBackColor = true;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.label2);
			this.groupBox1.Controls.Add(this.numUDTempo);
			this.groupBox1.Controls.Add(this.labelTempo);
			this.groupBox1.Controls.Add(this.chkBoxYM2612RL);
			this.groupBox1.Controls.Add(this.numUDLoop);
			this.groupBox1.Controls.Add(this.labelLoop);
			this.groupBox1.Controls.Add(this.label1);
			this.groupBox1.Controls.Add(this.labelFMVolPer);
			this.groupBox1.Controls.Add(this.numUDSSGVol);
			this.groupBox1.Controls.Add(this.labelSSGVolAdd);
			this.groupBox1.Controls.Add(this.numUDFMVol);
			this.groupBox1.Controls.Add(this.labelFMVolDown);
			this.groupBox1.Controls.Add(this.chkBoxClockAdjust);
			this.groupBox1.Controls.Add(this.chkBoxPCM);
			this.groupBox1.Controls.Add(this.chkBoxTimerReg);
			this.groupBox1.Controls.Add(this.chkBoxDual2nd);
			this.groupBox1.Controls.Add(this.chkBoxShrink);
			resources.ApplyResources(this.groupBox1, "groupBox1");
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.TabStop = false;
			// 
			// label2
			// 
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			// 
			// numUDTempo
			// 
			resources.ApplyResources(this.numUDTempo, "numUDTempo");
			this.numUDTempo.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numUDTempo.Name = "numUDTempo";
			this.numUDTempo.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
			// 
			// labelTempo
			// 
			resources.ApplyResources(this.labelTempo, "labelTempo");
			this.labelTempo.Name = "labelTempo";
			// 
			// chkBoxYM2612RL
			// 
			resources.ApplyResources(this.chkBoxYM2612RL, "chkBoxYM2612RL");
			this.chkBoxYM2612RL.Name = "chkBoxYM2612RL";
			this.chkBoxYM2612RL.UseVisualStyleBackColor = true;
			// 
			// numUDLoop
			// 
			resources.ApplyResources(this.numUDLoop, "numUDLoop");
			this.numUDLoop.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
			this.numUDLoop.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numUDLoop.Name = "numUDLoop";
			this.numUDLoop.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
			// 
			// labelLoop
			// 
			resources.ApplyResources(this.labelLoop, "labelLoop");
			this.labelLoop.Name = "labelLoop";
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// labelFMVolPer
			// 
			resources.ApplyResources(this.labelFMVolPer, "labelFMVolPer");
			this.labelFMVolPer.Name = "labelFMVolPer";
			// 
			// numUDSSGVol
			// 
			this.numUDSSGVol.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
			resources.ApplyResources(this.numUDSSGVol, "numUDSSGVol");
			this.numUDSSGVol.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            -2147483648});
			this.numUDSSGVol.Name = "numUDSSGVol";
			// 
			// labelSSGVolAdd
			// 
			resources.ApplyResources(this.labelSSGVolAdd, "labelSSGVolAdd");
			this.labelSSGVolAdd.Name = "labelSSGVolAdd";
			// 
			// numUDFMVol
			// 
			this.numUDFMVol.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
			resources.ApplyResources(this.numUDFMVol, "numUDFMVol");
			this.numUDFMVol.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            -2147483648});
			this.numUDFMVol.Name = "numUDFMVol";
			// 
			// labelFMVolDown
			// 
			resources.ApplyResources(this.labelFMVolDown, "labelFMVolDown");
			this.labelFMVolDown.Name = "labelFMVolDown";
			// 
			// chkBoxClockAdjust
			// 
			resources.ApplyResources(this.chkBoxClockAdjust, "chkBoxClockAdjust");
			this.chkBoxClockAdjust.Checked = true;
			this.chkBoxClockAdjust.CheckState = System.Windows.Forms.CheckState.Checked;
			this.chkBoxClockAdjust.Name = "chkBoxClockAdjust";
			this.chkBoxClockAdjust.UseVisualStyleBackColor = true;
			// 
			// chkBoxPCM
			// 
			resources.ApplyResources(this.chkBoxPCM, "chkBoxPCM");
			this.chkBoxPCM.Name = "chkBoxPCM";
			this.chkBoxPCM.UseVisualStyleBackColor = true;
			// 
			// chkBoxTimerReg
			// 
			resources.ApplyResources(this.chkBoxTimerReg, "chkBoxTimerReg");
			this.chkBoxTimerReg.Name = "chkBoxTimerReg";
			this.chkBoxTimerReg.UseVisualStyleBackColor = true;
			// 
			// chkBoxDual2nd
			// 
			resources.ApplyResources(this.chkBoxDual2nd, "chkBoxDual2nd");
			this.chkBoxDual2nd.Name = "chkBoxDual2nd";
			this.chkBoxDual2nd.UseVisualStyleBackColor = true;
			// 
			// chkBoxShrink
			// 
			resources.ApplyResources(this.chkBoxShrink, "chkBoxShrink");
			this.chkBoxShrink.Name = "chkBoxShrink";
			this.chkBoxShrink.UseVisualStyleBackColor = true;
			// 
			// groupBoxTarget
			// 
			this.groupBoxTarget.Controls.Add(this.cmbBoxChip2Clock);
			this.groupBoxTarget.Controls.Add(this.cmbBoxChip1Clock);
			this.groupBoxTarget.Controls.Add(this.labelChip2);
			this.groupBoxTarget.Controls.Add(this.labelChip1);
			this.groupBoxTarget.Controls.Add(this.cmbBoxTarget);
			resources.ApplyResources(this.groupBoxTarget, "groupBoxTarget");
			this.groupBoxTarget.Name = "groupBoxTarget";
			this.groupBoxTarget.TabStop = false;
			// 
			// cmbBoxChip2Clock
			// 
			this.cmbBoxChip2Clock.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbBoxChip2Clock.DropDownWidth = 92;
			this.cmbBoxChip2Clock.FormattingEnabled = true;
			resources.ApplyResources(this.cmbBoxChip2Clock, "cmbBoxChip2Clock");
			this.cmbBoxChip2Clock.Name = "cmbBoxChip2Clock";
			this.cmbBoxChip2Clock.SelectedIndexChanged += new System.EventHandler(this.cmbBoxChip2Clock_SelectedIndexChanged);
			// 
			// cmbBoxChip1Clock
			// 
			this.cmbBoxChip1Clock.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbBoxChip1Clock.DropDownWidth = 92;
			this.cmbBoxChip1Clock.FormattingEnabled = true;
			resources.ApplyResources(this.cmbBoxChip1Clock, "cmbBoxChip1Clock");
			this.cmbBoxChip1Clock.Name = "cmbBoxChip1Clock";
			this.cmbBoxChip1Clock.SelectedIndexChanged += new System.EventHandler(this.cmbBoxChip1Clock_SelectedIndexChanged);
			// 
			// labelChip2
			// 
			resources.ApplyResources(this.labelChip2, "labelChip2");
			this.labelChip2.Name = "labelChip2";
			// 
			// labelChip1
			// 
			resources.ApplyResources(this.labelChip1, "labelChip1");
			this.labelChip1.Name = "labelChip1";
			// 
			// cmbBoxTarget
			// 
			this.cmbBoxTarget.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cmbBoxTarget.DropDownWidth = 92;
			this.cmbBoxTarget.FormattingEnabled = true;
			resources.ApplyResources(this.cmbBoxTarget, "cmbBoxTarget");
			this.cmbBoxTarget.Name = "cmbBoxTarget";
			this.cmbBoxTarget.SelectedIndexChanged += new System.EventHandler(this.cmbBoxTarget_SelectedIndexChanged);
			// 
			// chkBoxF1Out
			// 
			resources.ApplyResources(this.chkBoxF1Out, "chkBoxF1Out");
			this.chkBoxF1Out.Name = "chkBoxF1Out";
			this.chkBoxF1Out.UseVisualStyleBackColor = true;
			// 
			// btnConvert
			// 
			resources.ApplyResources(this.btnConvert, "btnConvert");
			this.btnConvert.Name = "btnConvert";
			this.btnConvert.UseVisualStyleBackColor = true;
			this.btnConvert.Click += new System.EventHandler(this.btnConvert_Click);
			// 
			// btnFileRef
			// 
			resources.ApplyResources(this.btnFileRef, "btnFileRef");
			this.btnFileRef.Name = "btnFileRef";
			this.btnFileRef.UseVisualStyleBackColor = true;
			this.btnFileRef.Click += new System.EventHandler(this.btnFileRef_Click);
			// 
			// chkBoxVerify
			// 
			resources.ApplyResources(this.chkBoxVerify, "chkBoxVerify");
			this.chkBoxVerify.Name = "chkBoxVerify";
			this.chkBoxVerify.UseVisualStyleBackColor = true;
			// 
			// tBoxUploadFile
			// 
			resources.ApplyResources(this.tBoxUploadFile, "tBoxUploadFile");
			this.tBoxUploadFile.Name = "tBoxUploadFile";
			// 
			// labelUploadFIle
			// 
			resources.ApplyResources(this.labelUploadFIle, "labelUploadFIle");
			this.labelUploadFIle.Name = "labelUploadFIle";
			// 
			// textBoxString
			// 
			this.textBoxString.BackColor = System.Drawing.SystemColors.ControlLight;
			resources.ApplyResources(this.textBoxString, "textBoxString");
			this.textBoxString.Name = "textBoxString";
			this.textBoxString.ReadOnly = true;
			// 
			// statusStrip1
			// 
			this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsslblProtcol,
            this.tspbarLoad});
			resources.ApplyResources(this.statusStrip1, "statusStrip1");
			this.statusStrip1.Name = "statusStrip1";
			// 
			// tsslblProtcol
			// 
			resources.ApplyResources(this.tsslblProtcol, "tsslblProtcol");
			this.tsslblProtcol.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Top) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right) 
            | System.Windows.Forms.ToolStripStatusLabelBorderSides.Bottom)));
			this.tsslblProtcol.Name = "tsslblProtcol";
			// 
			// tspbarLoad
			// 
			this.tspbarLoad.Name = "tspbarLoad";
			resources.ApplyResources(this.tspbarLoad, "tspbarLoad");
			// 
			// menuStrip1
			// 
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileFToolStripMenuItem,
            this.toolTToolStripMenuItem,
            this.helpHToolStripMenuItem});
			resources.ApplyResources(this.menuStrip1, "menuStrip1");
			this.menuStrip1.Name = "menuStrip1";
			// 
			// fileFToolStripMenuItem
			// 
			this.fileFToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitXToolStripMenuItem});
			this.fileFToolStripMenuItem.Name = "fileFToolStripMenuItem";
			resources.ApplyResources(this.fileFToolStripMenuItem, "fileFToolStripMenuItem");
			// 
			// exitXToolStripMenuItem
			// 
			this.exitXToolStripMenuItem.Name = "exitXToolStripMenuItem";
			resources.ApplyResources(this.exitXToolStripMenuItem, "exitXToolStripMenuItem");
			this.exitXToolStripMenuItem.Click += new System.EventHandler(this.exitXToolStripMenuItem_Click);
			// 
			// toolTToolStripMenuItem
			// 
			this.toolTToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.rescanSerialPortRToolStripMenuItem,
            this.portBToolStripMenuItem});
			this.toolTToolStripMenuItem.Name = "toolTToolStripMenuItem";
			resources.ApplyResources(this.toolTToolStripMenuItem, "toolTToolStripMenuItem");
			// 
			// rescanSerialPortRToolStripMenuItem
			// 
			this.rescanSerialPortRToolStripMenuItem.Name = "rescanSerialPortRToolStripMenuItem";
			resources.ApplyResources(this.rescanSerialPortRToolStripMenuItem, "rescanSerialPortRToolStripMenuItem");
			this.rescanSerialPortRToolStripMenuItem.Click += new System.EventHandler(this.rescanSerialPortRToolStripMenuItem_Click);
			// 
			// portBToolStripMenuItem
			// 
			this.portBToolStripMenuItem.Name = "portBToolStripMenuItem";
			resources.ApplyResources(this.portBToolStripMenuItem, "portBToolStripMenuItem");
			// 
			// helpHToolStripMenuItem
			// 
			this.helpHToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutThisProgramAToolStripMenuItem});
			this.helpHToolStripMenuItem.Name = "helpHToolStripMenuItem";
			resources.ApplyResources(this.helpHToolStripMenuItem, "helpHToolStripMenuItem");
			// 
			// aboutThisProgramAToolStripMenuItem
			// 
			this.aboutThisProgramAToolStripMenuItem.Name = "aboutThisProgramAToolStripMenuItem";
			resources.ApplyResources(this.aboutThisProgramAToolStripMenuItem, "aboutThisProgramAToolStripMenuItem");
			this.aboutThisProgramAToolStripMenuItem.Click += new System.EventHandler(this.aboutThisProgramAToolStripMenuItem_Click);
			// 
			// MainForm
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.splitContainer1);
			this.Controls.Add(this.menuStrip1);
			this.MainMenuStrip = this.menuStrip1;
			this.MaximizeBox = false;
			this.Name = "MainForm";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel1.PerformLayout();
			this.splitContainer1.Panel2.ResumeLayout(false);
			this.splitContainer1.Panel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.numUDOffset)).EndInit();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.numUDTempo)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numUDLoop)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numUDSSGVol)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numUDFMVol)).EndInit();
			this.groupBoxTarget.ResumeLayout(false);
			this.groupBoxTarget.PerformLayout();
			this.statusStrip1.ResumeLayout(false);
			this.statusStrip1.PerformLayout();
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem fileFToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem exitXToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem toolTToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem rescanSerialPortRToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem portBToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem helpHToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem aboutThisProgramAToolStripMenuItem;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.StatusStrip statusStrip1;
		private System.Windows.Forms.ToolStripStatusLabel tsslblProtcol;
		private System.Windows.Forms.ToolStripProgressBar tspbarLoad;
		private System.Windows.Forms.NumericUpDown numUDSSGVol;
		private System.Windows.Forms.Label labelSSGVolAdd;
		private System.Windows.Forms.NumericUpDown numUDFMVol;
		private System.Windows.Forms.Label labelFMVolDown;
		private System.Windows.Forms.CheckBox chkBoxClockAdjust;
		private System.Windows.Forms.CheckBox chkBoxPCM;
		private System.Windows.Forms.CheckBox chkBoxTimerReg;
		private System.Windows.Forms.CheckBox chkBoxDual2nd;
		private System.Windows.Forms.CheckBox chkBoxShrink;
		private System.Windows.Forms.CheckBox chkBoxF1Out;
		private System.Windows.Forms.ComboBox cmbBoxTarget;
		private System.Windows.Forms.Button btnConvert;
		private System.Windows.Forms.Button btnFileRef;
		private System.Windows.Forms.CheckBox chkBoxVerify;
		private System.Windows.Forms.TextBox tBoxUploadFile;
		private System.Windows.Forms.Label labelUploadFIle;
		private System.Windows.Forms.Button btnReset;
		private System.Windows.Forms.ComboBox cmbBoxSerialPort;
		private System.Windows.Forms.Label labelSerialPort;
		private System.Windows.Forms.TextBox textBoxString;
		private System.Windows.Forms.NumericUpDown numUDOffset;
		private System.Windows.Forms.Label labelUploadOffset;
		private System.Windows.Forms.GroupBox groupBoxTarget;
		private System.Windows.Forms.Label labelChip2;
		private System.Windows.Forms.Label labelChip1;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.ComboBox cmbBoxChip1Clock;
		private System.Windows.Forms.ComboBox cmbBoxChip2Clock;
		private System.Windows.Forms.NumericUpDown numUDLoop;
		private System.Windows.Forms.Label labelLoop;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label labelFMVolPer;
		private System.Windows.Forms.CheckBox chkBoxF1TOut;
		private System.Windows.Forms.CheckBox chkBoxUpload;
		private System.Windows.Forms.CheckBox chkBoxTextOut;
		private System.Windows.Forms.CheckBox chkBoxDumpOut;
		private System.Windows.Forms.CheckBox chkBoxS98Out;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.CheckBox chkBoxNoPCMBlk;
		private System.Windows.Forms.CheckBox chkBoxYM2612RL;
		private System.Windows.Forms.NumericUpDown numUDTempo;
		private System.Windows.Forms.Label labelTempo;
		private System.Windows.Forms.Label label2;
	}
}

