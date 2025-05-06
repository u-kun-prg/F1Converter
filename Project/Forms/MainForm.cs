using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using F1;

namespace F1Converter
{
	public partial class MainForm : Form
	{
		Protcol ProtcolCtrl = new Protcol();
		List<Board> BoardList = new List<Board>();
		List<FormTarget> FormTargetList = new List<FormTarget>();
		Board CurrentBord = null;
		List<ToolStripMenuItem> BoardListMenuItem = new List<ToolStripMenuItem>();

		public MainForm()
		{
			FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			LoadBoardList();
			LoadFormTargetList();

			InitializeComponent();

			scanningSerialPort();
			careateBoardListMenuItem();
			careateFormTargetListItem();

			CurrentBord = BoardList[0];
			updateStatus();

			cmbBoxTarget.SelectedIndex = 0;

			ProtcolCtrl.StateCallback += new Protcol.StateCallbackMethod(updateStatus);
#if DEVELOP
			chkBoxUpload.Checked = true;
#else
			chkBoxS98Out.Visible = false;
#endif 
		}

		delegate void UpdateStatusDelegate();

		private void updateStatus()
		{
			if (tsslblProtcol.Owner.InvokeRequired)
			{
				tsslblProtcol.Owner.Invoke(new UpdateStatusDelegate(updateStatus));
				return;
			}
			if (ProtcolCtrl.IsRunningCritical)
			{
				if (btnConvert.Enabled) btnConvert.Enabled = false;
				if (btnReset.Enabled) btnReset.Enabled = false;
			}
			else
			{
				if (!btnConvert.Enabled)	btnConvert.Enabled = true;
				if (!btnReset.Enabled) btnReset.Enabled = true;
			}
			switch (ProtcolCtrl.MainState)
			{
				case Protcol.MainStateEnum.RESET:
					tsslblProtcol.Text = ProtcolCtrl.MainState.ToString();
					tspbarLoad.Value = 0;
					break;
				case Protcol.MainStateEnum.SEND:
					tsslblProtcol.Text = $"{ProtcolCtrl.MainState}: {(ProtcolCtrl.RecordIndex+1)}/{ProtcolCtrl.RecordCount}";
					tspbarLoad.Value = (int)(ProtcolCtrl.RecordProcessing * 100f);
					break;
				case Protcol.MainStateEnum.VERIFY:
					tsslblProtcol.Text = $"{ProtcolCtrl.MainState}: {(ProtcolCtrl.RecordIndex+1)}/{ProtcolCtrl.RecordCount}";
					tspbarLoad.Value = (int)(ProtcolCtrl.RecordProcessing * 100f);
					break;
				default:
					tsslblProtcol.Text = $"{ProtcolCtrl.MainState}: {ProtcolCtrl.ErrorState}";
					tspbarLoad.Value = 0;
					break;
			}
			foreach (ToolStripMenuItem item in BoardListMenuItem)
			{
				item.Checked = (item.Tag == (object)CurrentBord) ? true : false;
			}
			statusStrip1.Invalidate();
		}

		private void scanningSerialPort()
		{
			cmbBoxSerialPort.Items.Clear();
			string[] portnames = System.IO.Ports.SerialPort.GetPortNames();
			foreach (string str in portnames)
			{
				cmbBoxSerialPort.Items.Add(str);
			}
			if (portnames == null || portnames.Length ==0)
			{
				cmbBoxSerialPort.Enabled = false;
			}
			else
			{
				cmbBoxSerialPort.Enabled = true;
				cmbBoxSerialPort.SelectedIndex = 0;
			}
		}

		private void careateBoardListMenuItem()
		{
			foreach (Board board in BoardList)
			{
				ToolStripMenuItem menuitem = new ToolStripMenuItem(board.Name);
				menuitem.Tag = (object)board;
				menuitem.Click += new EventHandler(BoardTypeMenuItem_Click);
				portBToolStripMenuItem.DropDown.Items.Add(menuitem);
				BoardListMenuItem.Add(menuitem);
			}
		}

		private void careateFormTargetListItem()
		{
			foreach (FormTarget formTarget in FormTargetList)
			{
				cmbBoxTarget.Items.Add(formTarget.name);
			}
			careateFormTargetChipItem(0);
		}

		private void careateFormTargetChipItem(int index)
		{
			var chipNum = FormTargetList[index].formTargetChips.Count;
			labelChip1.Text = "None";
			labelChip2.Text = "None";
			cmbBoxChip1Clock.BeginUpdate();
			cmbBoxChip1Clock.Items.Clear();
			cmbBoxChip1Clock.Items.Add("---");
			cmbBoxChip1Clock.SelectedIndex = 0;
			cmbBoxChip1Clock.EndUpdate();
			cmbBoxChip2Clock.BeginUpdate();
			cmbBoxChip2Clock.Items.Clear();
			cmbBoxChip2Clock.Items.Add("---");
			cmbBoxChip2Clock.SelectedIndex = 0;
			cmbBoxChip2Clock.EndUpdate();
			for (int i=0; i<chipNum; i++)
			{
				var formTargetChip = FormTargetList[index].formTargetChips[i];
				var clockNum = formTargetChip.usableClocks.Count();
				switch(i)
				{
					case 0:
						labelChip1.Text = formTargetChip.chipName;
						cmbBoxChip1Clock.BeginUpdate();
						cmbBoxChip1Clock.Items.Clear();
						for (int j=0; j<clockNum; j++)
						{
							float clk = (float)formTargetChip.usableClocks[j];
							clk /= 1000000f;
							cmbBoxChip1Clock.Items.Add($"{clk}Mhz");
						}
						cmbBoxChip1Clock.SelectedIndex = formTargetChip.selectedIndex;
						cmbBoxChip1Clock.EndUpdate();
						break;
					case 1:
						labelChip2.Text = formTargetChip.chipName;
						cmbBoxChip2Clock.BeginUpdate();
						cmbBoxChip2Clock.Items.Clear();
						for (int j=0; j<clockNum; j++)
						{
							float clk = (float)formTargetChip.usableClocks[j];
							clk /= 1000000f;
							cmbBoxChip2Clock.Items.Add($"{clk}Mhz");
						}
						cmbBoxChip2Clock.SelectedIndex = formTargetChip.selectedIndex;
						cmbBoxChip2Clock.EndUpdate();
						break;
					default:
						break;
				}
			}
		}

		private void cmbBoxTarget_SelectedIndexChanged(object sender, EventArgs e)
		{
			careateFormTargetChipItem(cmbBoxTarget.SelectedIndex);
		}

		private void cmbBoxChip1Clock_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (labelChip1.Text != "None" && cmbBoxTarget.SelectedIndex != -1)
			{
				var formTargetChip = FormTargetList[cmbBoxTarget.SelectedIndex].formTargetChips[0];
				formTargetChip.selectedIndex = cmbBoxChip1Clock.SelectedIndex;
				formTargetChip.chipClock = formTargetChip.usableClocks[formTargetChip.selectedIndex];
			}
		}

		private void cmbBoxChip2Clock_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (labelChip2.Text != "None" && cmbBoxTarget.SelectedIndex != -1)
			{
				var formTargetChip = FormTargetList[cmbBoxTarget.SelectedIndex].formTargetChips[1];
				formTargetChip.selectedIndex = cmbBoxChip2Clock.SelectedIndex;
				formTargetChip.chipClock = formTargetChip.usableClocks[formTargetChip.selectedIndex];
			}
		}

		private bool ConvertSource(string sourceFilename)
		{
			if (chkBoxUpload.Checked)
			{
				if (CurrentBord==null)
				{
					MessageBox.Show("Not specified target board.");
					return false;
				}
				if (ProtcolCtrl.IsRunningCritical || ProtcolCtrl.CountMessage() > 0)
				{
					MessageBox.Show("Running Worker Thread.");
					return false;
				}
			}

			btnConvert.Enabled = false;
			btnReset.Enabled = false;
			Protcol.DataRecord data_recoed = null;
			if (String.IsNullOrEmpty(sourceFilename) || !System.IO.File.Exists(sourceFilename))
			{
				MessageBox.Show("File not found.");
				btnConvert.Enabled = true;
				btnReset.Enabled = true;
				return false;
			}
			var chipNameList = new List<string>();
			var chipClockList = new List<int>();
			var targetTopCodeList = new List<int>();
			{
				for (int i=0, l = FormTargetList[cmbBoxTarget.SelectedIndex].formTargetChips.Count; i<l; i++)
				{
					chipNameList.Add(FormTargetList[cmbBoxTarget.SelectedIndex].formTargetChips[i].chipName);
					chipClockList.Add(FormTargetList[cmbBoxTarget.SelectedIndex].formTargetChips[i].chipClock);
					targetTopCodeList.Add(FormTargetList[cmbBoxTarget.SelectedIndex].formTargetChips[i].chipTopCode);
				}
			}
			F1Convert f1_convert = new F1Convert();
			List<byte> f1DataList = new List<byte>();
			List<string> textDataList = new List<string>();
			List<string> dumpDataList = new List<string>();
			List<string> f1tDataList = new List<string>();
			List<byte> s98DataList = new List<byte>();
			bool isF1 = false;
			bool isF1T = false;
			bool isS98 = false;
			if (!f1_convert.F1Converter(
									sourceFilename, 
									f1DataList, 
									textDataList, 
									dumpDataList, 
									f1tDataList,
									s98DataList,
									FormTargetList[cmbBoxTarget.SelectedIndex].name,
									FormTargetList[cmbBoxTarget.SelectedIndex].commandArray,
									chipNameList,
									chipClockList,
									targetTopCodeList,
									chkBoxClockAdjust.Checked, 
									chkBoxShrink.Checked, 
									chkBoxDual2nd.Checked, 
									chkBoxTimerReg.Checked, 
									chkBoxPCM.Checked, 
									chkBoxYM2612RL.Checked,
									(int)numUDLoop.Value,
									(int)numUDTempo.Value,
									(int)numUDFMVol.Value, 
									(int)numUDSSGVol.Value, 
									chkBoxTextOut.Checked,
									chkBoxDumpOut.Checked,
									chkBoxF1TOut.Checked,
									chkBoxS98Out.Checked,
									chkBoxNoPCMBlk.Checked,
									textBoxString, 
									out isF1,
									out isF1T,
									out isS98))
			{
				btnConvert.Enabled = true;
				btnReset.Enabled = true;
				return false;
			}

			if (!isF1)
			{
				if (chkBoxTextOut.Checked)
				{
					string savefilename = System.IO.Path.ChangeExtension(sourceFilename, "txt");
					SaveFileDialog sa = new SaveFileDialog();
					sa.Title = "F1 Text File Save";
					sa.FileName = @savefilename;
					sa.Filter = "Text file(*.txt)|*.TXT|All file(*.*)|*.*";
					sa.FilterIndex = 1;

					DialogResult result = sa.ShowDialog();
					if (result == DialogResult.OK)
					{
						var Writer = new System.IO.StreamWriter(sa.FileName, false, Encoding.GetEncoding("utf-8"));
						foreach(var str in textDataList)
						{
							Writer.WriteLine(str);
						}
						Writer.Close();
					}
				}

				if (chkBoxS98Out.Checked && isS98)
				{
					byte[] save_image = s98DataList.ToArray();
					string savefilename = System.IO.Path.ChangeExtension(sourceFilename, "s98");
					SaveFileDialog sa = new SaveFileDialog();
					sa.Title = "s98 File Save";
					sa.FileName = @savefilename;
					sa.Filter = "s98 file(*.s98)|*.S98|All file(*.*)|*.*";
					sa.FilterIndex = 1;
					DialogResult result = sa.ShowDialog();
					if (result == DialogResult.OK)
					{
						System.IO.FileStream wfs = new System.IO.FileStream(sa.FileName, System.IO.FileMode.Create, System.IO.FileAccess.Write);
						wfs.Write(save_image, 0, save_image.Length);
						wfs.Close();
					}
				}

				if (chkBoxF1TOut.Checked && !isF1T)
				{
					string savefilename = System.IO.Path.ChangeExtension(sourceFilename, "f1T");
					SaveFileDialog sa = new SaveFileDialog();
					sa.Title = "F1T File Save";
					sa.FileName = @savefilename;
					sa.Filter = "F1T file(*.f1t)|*.F1T|All file(*.*)|*.*";
					sa.FilterIndex = 1;
					DialogResult result = sa.ShowDialog();
					if (result == DialogResult.OK)
					{
						var Writer = new System.IO.StreamWriter(sa.FileName, false, Encoding.GetEncoding("utf-8"));
						foreach(var str in f1tDataList)
						{
							Writer.WriteLine(str);
						}
						Writer.Close();
					}
				}
			}

			if (chkBoxDumpOut.Checked)
			{
				string savefilename = System.IO.Path.ChangeExtension(sourceFilename, ".dmp");
				SaveFileDialog sa = new SaveFileDialog();
				sa.Title = "F1T File Save";
				sa.FileName = @savefilename;
				sa.Filter = "F1T file(*.dmp)|*.DMP|All file(*.*)|*.*";
				sa.FilterIndex = 1;
				DialogResult result = sa.ShowDialog();
				if (result == DialogResult.OK)
				{
					var Writer = new System.IO.StreamWriter(sa.FileName, false, Encoding.GetEncoding("utf-8"));
					foreach(var str in dumpDataList)
					{
						Writer.WriteLine(str);
					}
					Writer.Close();
				}
			}

			if (chkBoxF1Out.Checked && !isF1)
			{
				byte[] save_image = f1DataList.ToArray();
				string savefilename = System.IO.Path.ChangeExtension(sourceFilename, "F1");
				SaveFileDialog sa = new SaveFileDialog();
				sa.Title = "F1 File Save";
				sa.FileName = @savefilename;
				sa.Filter = "f1 file(*.f1)|*.F1|All file(*.*)|*.*";
				sa.FilterIndex = 1;
				DialogResult result = sa.ShowDialog();
				if (result == DialogResult.OK)
				{
					System.IO.FileStream wfs = new System.IO.FileStream(sa.FileName, System.IO.FileMode.Create, System.IO.FileAccess.Write);
					wfs.Write(save_image, 0, save_image.Length);
					wfs.Close();
				}
			}

			if (chkBoxUpload.Checked)
			{
				int load_offset = (int)numUDOffset.Value;
				int upload_size = f1DataList.Count;
				int memory_size = CurrentBord.FlashEndAddress - CurrentBord.FlashStartAddress - load_offset;
				int load_address = load_offset + CurrentBord.FlashStartAddress;
				textBoxString.AppendText($"\r\n{CurrentBord.Name} MemorySize:{memory_size:X}H ({load_address:X}H - {(CurrentBord.FlashEndAddress-1):X}H)\r\n\r\n");
				textBoxString.AppendText($"F1 Data Size:{upload_size:X}H\r\n");
				if (memory_size < upload_size)
				{
					upload_size = memory_size - 1;
					textBoxString.AppendText($"\r\nSIZE OVER!! Cut the F1-Data to memory size.:{upload_size:X}\r\n");
				}
				var uploadImage = new byte[upload_size];
				for (int i = 0; i < upload_size; i ++)
				{
					uploadImage[i] = f1DataList[i];
				}
				data_recoed = new Protcol.DataRecord();
				data_recoed.Address = load_address;
				data_recoed.Data = uploadImage;
				data_recoed.Verify = chkBoxVerify.Checked;

				if (data_recoed == null)
				{
					MessageBox.Show("Send data does not exist.");
					btnConvert.Enabled = true;
					btnReset.Enabled = true;
					return false;
				}
				if (cmbBoxSerialPort.Enabled)
				{
					pushSerialSetup();
					if (data_recoed != null)
					{
						ProtcolCtrl.PushMessage(new Protcol.MessageRecord(Protcol.MessageRecord.MessageTypeEnum.SetRecord, (object)data_recoed));
					}
					ProtcolCtrl.PushMessage(new Protcol.MessageRecord(Protcol.MessageRecord.MessageTypeEnum.Start));
					ProtcolCtrl.AwakeWorker();
					updateStatus();
				}
			}
			return true;
		}

		private void pushSerialSetup()
		{
			ProtcolCtrl.PushMessage(new Protcol.MessageRecord(Protcol.MessageRecord.MessageTypeEnum.Clear));
			ProtcolCtrl.PushMessage(new Protcol.MessageRecord(Protcol.MessageRecord.MessageTypeEnum.SetPortName, (object)cmbBoxSerialPort.SelectedItem.ToString()));
			ProtcolCtrl.PushMessage(new Protcol.MessageRecord(Protcol.MessageRecord.MessageTypeEnum.SetDevice, (object)CurrentBord));
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			ProtcolCtrl.AbortThread();
		}

		private void exitXToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		private void btnFileRef_Click(object sender, EventArgs e)
		{
			System.Windows.Forms.OpenFileDialog fd = new System.Windows.Forms.OpenFileDialog();
			fd.CheckFileExists = true;
			fd.Filter = "Upload file (*.*)|*.*";
			if (fd.ShowDialog() == DialogResult.OK && fd.CheckFileExists)
			{
				tBoxUploadFile.Text = fd.FileName;
			}
		}

		private void btnConvert_Click(object sender, EventArgs e)
		{
			string sourceFilename = null;
			if (tBoxUploadFile.Text.Length > 0)
			{
				sourceFilename = tBoxUploadFile.Text;
			}
			if (sourceFilename == null)
			{
				MessageBox.Show("File not found.");
			}
			else 
			{
				textBoxString.ResetText();
				if (ConvertSource(sourceFilename))
				{
				    updateStatus();
				}
			}
		}

		private void btnReset_Click(object sender, EventArgs e)
		{
			if (cmbBoxSerialPort.Enabled)
			{
				pushSerialSetup();
				ProtcolCtrl.PushMessage(new Protcol.MessageRecord(Protcol.MessageRecord.MessageTypeEnum.Start));
				ProtcolCtrl.AwakeWorker();
				updateStatus();
			}
		}

		private void rescanSerialPortRToolStripMenuItem_Click(object sender, EventArgs e)
		{
			scanningSerialPort();
		}

		private void BoardTypeMenuItem_Click(object sender, EventArgs e)
		{
			ToolStripMenuItem item = (ToolStripMenuItem)sender;
			CurrentBord = (Board)item.Tag;
			updateStatus();
		}

		private void aboutThisProgramAToolStripMenuItem_Click(object sender, EventArgs e)
		{
			AboutThisProgramForm form = new AboutThisProgramForm();
			form.ShowDialog();
		}

		public void LoadBoardList()
		{
			System.IO.StreamReader sr;

			if (System.IO.File.Exists("boards.xml"))
			{
			    sr = new System.IO.StreamReader("boards.xml");
			}
			else
			{
				System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
				sr = new System.IO.StreamReader( asm.GetManifestResourceStream("F1Converter.Resources.boards.xml") );
			}
			System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader(sr.BaseStream);

			string lasttext = null;
			Board lastboard = null;
			while (reader.Read())
			{
				switch (reader.NodeType)
				{
					case System.Xml.XmlNodeType.Element:
						if (reader.Name == "board")
						{
							lastboard = new Board();
						}
						lasttext = null;
						break;
					case System.Xml.XmlNodeType.Text:
						lasttext = reader.Value;
						break;
					case System.Xml.XmlNodeType.EndElement:
						if (reader.Name == "name")
						{
							lastboard.Name = lasttext;
						}
						else if (reader.Name == "protcol")
						{
							if (lasttext == "stk500")
							{
								lastboard.ProtcolType = Board.ProtcolTypeEnum.STK500;
							}
							else if (lasttext == "stk500v2")
							{
								lastboard.ProtcolType = Board.ProtcolTypeEnum.STK500V2;
							}
							else if (lasttext == "SAM_BA")
							{
								lastboard.ProtcolType = Board.ProtcolTypeEnum.SAM_BA;
							}
							else
							{
								lastboard.ProtcolType = Board.ProtcolTypeEnum.NONE;
							}
						}
						else if (reader.Name == "flash_start_address")
						{
							lastboard.FlashStartAddress = int.Parse(lasttext);
						}
						else if (reader.Name == "flash_end_address")
						{
							lastboard.FlashEndAddress = int.Parse(lasttext);
						}
						else if (reader.Name == "upload_speed")
						{
							lastboard.UpLoadSpeed = int.Parse(lasttext);
						}
						else if (reader.Name == "dtr_control")
						{
							if (lasttext == "EnableAndThrough")
							{
								lastboard.DTRControl = Board.DTRControlTypeEnum.ENABLE_AND_THROUGH;
							}
							else if (lasttext == "EnableAndThroughDisable")
							{
								lastboard.DTRControl = Board.DTRControlTypeEnum.ENABLE_AND_THROUGH_DISABLE;
							}
							else
							{	// perse error
								lastboard.DTRControl = Board.DTRControlTypeEnum.UNKNOWN;
							}
						}
						else if (reader.Name == "board")
						{
							BoardList.Add(lastboard);
						}
						else
						{
							// perse error
						}
						break;
				}
			}
		}

		public void LoadFormTargetList()
		{
			System.IO.StreamReader sr;

			if (System.IO.File.Exists("targets.xml"))
			{
			    sr = new System.IO.StreamReader("targets.xml");
			}
			else
			{
				System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
				sr = new System.IO.StreamReader( asm.GetManifestResourceStream("F1Converter.Resources.targets.xml") );
			}
			System.Xml.XmlTextReader reader = new System.Xml.XmlTextReader(sr.BaseStream);
			
			string lasttext = null;
			FormTarget lastFormTarget = null;
			FormTargetChip lastFormTargetChip = null;
			while (reader.Read())
			{
				switch (reader.NodeType)
				{
					case System.Xml.XmlNodeType.Element:
						if (reader.Name == "target")
						{
							lastFormTarget = new FormTarget();
						}
						if (reader.Name == "chips")
						{
							lastFormTargetChip = new FormTargetChip();
							lastFormTargetChip.selectedIndex = 0;
						}
						lasttext = null;
						break;
					case System.Xml.XmlNodeType.Text:
						lasttext = reader.Value;
						break;
					case System.Xml.XmlNodeType.EndElement:
						if (reader.Name == "name")
						{
							lastFormTarget.name = lasttext;
						}
						else if (reader.Name == "commands")
						{
							string[] cmdStrings = lasttext.Split(' ');
							lastFormTarget.commandArray =new byte[cmdStrings.Length];
							for (int i=0,l = cmdStrings.Length; i<l; i++)
							{
								lastFormTarget.commandArray[i] = Convert.ToByte(cmdStrings[i], 16);
							}
						}
						else if (reader.Name == "targetchip")
						{
							lastFormTargetChip.chipName = lasttext;
						}
						else if (reader.Name == "targetclock")
						{
							string[] clkStrings = lasttext.Split(' ');
							for (int i=0,l = clkStrings.Length; i<l; i++)
							{
								lastFormTargetChip.usableClocks.Add(Convert.ToInt32(clkStrings[i]));
							}
						}
						else if (reader.Name == "targettopcode")
						{
							lastFormTargetChip.chipTopCode = Convert.ToInt32(lasttext, 16);
						}
						else if (reader.Name == "chips")
						{
							lastFormTarget.formTargetChips.Add(lastFormTargetChip);
						}
						else if (reader.Name == "target")
						{
							FormTargetList.Add(lastFormTarget);
						}
						//else
						//{
						//	perse error
						//}
						break;
				}
			}
		}

	}
}
