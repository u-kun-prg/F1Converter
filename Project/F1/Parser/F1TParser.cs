using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace F1
{
	/// <summary>
	///	F1T フォーマット パーサークラス
	/// </summary>
	public class F1TParser : Parser
	{
		private class F1TLine
		{
			public int LineNo;
			public string SourceStr;
			public string Label;
			public string Opecode;
			public List<uint> DataList = new List<uint>();
		}
		private List<F1TLine> m_F1TLines = new List<F1TLine>();

		private int m_headerIndex;
		private int m_playDataIndex;
		private int m_pcmDataIndex;

		/// <summary>
		///	F1T	フォーマットパース
		/// </summary>
		public override bool Parse()
		{
			if (!CreateF1TLines())
			{
				return false;
			}
			if (!CheckLabelString())
			{
				return false;
			}

			if (!ParseHeader()) 
			{
				return false;
			}
			if (!ParsePlayData())
			{
				return false;
			}
			if (!ParsePcmData())
			{
				return false;
			}
			return true;
		}

		/// <summary>
		///	F1T	PCM Data をパース
		/// </summary>
		private bool ParsePcmData()
		{
			if (m_pcmDataIndex < 0) 
			{
				return  true;
			}
			while(true)
			{
				var f1TLine = m_F1TLines[m_pcmDataIndex];
				if (string.IsNullOrEmpty(f1TLine.Opecode))
				{
					return SetSyntaxErrorLine(f1TLine);
				}
				if (f1TLine.Opecode != F1TReservedWord.F1TPcmDataOpecodeStrings[(int)F1TReservedWord.F1TPcmDataOpecode.PCMHEADER])
				{
					return SetSyntaxErrorLine(f1TLine);
				}
				if (f1TLine.DataList.Count != 3)
				{
					return SetDataErrorLine(f1TLine);
				}
				bool isEnd = false;
				var pcmChipSelect = (int)f1TLine.DataList[0];
				var dataType = (int)f1TLine.DataList[1];
				var pcmSourceAddress = f1TLine.DataList[2];
				var pcmDataList = new List<byte>();
				m_pcmDataIndex += 1;
				while(true)
				{
					var f1PcmTLine = m_F1TLines[m_pcmDataIndex];
					if (!string.IsNullOrEmpty(f1PcmTLine.Label))
					{
						break;
					}
					if (string.IsNullOrEmpty(f1PcmTLine.Opecode))
					{
						return SetSyntaxErrorLine(f1PcmTLine);
					}
					if (f1PcmTLine.Opecode == F1TReservedWord.F1TPcmDataOpecodeStrings[(int)F1TReservedWord.F1TPcmDataOpecode.DATA])
					{
						for (int i = 0; i < f1PcmTLine.DataList.Count; i ++)
						{
							pcmDataList.Add((byte)f1PcmTLine.DataList[i]);
						}
						m_pcmDataIndex += 1;
						if (m_pcmDataIndex >= m_F1TLines.Count)
						{
							isEnd = true;
							break;
						}
					}
					else
					{
						break;
					}
				}
				ImData.AddPcmImDataList(pcmChipSelect, (PcmDataType)dataType, (int)pcmSourceAddress, pcmDataList.Count, pcmDataList.ToArray());
				if (isEnd)
				{
					return true;
				}
			}
		}

		/// <summary>
		///	F1T	Play Data をパース
		/// </summary>
		private bool ParsePlayData()
		{
			int cs = 0;
			var a1Array = new int[256];
			while(true)
			{
				var f1TLine = m_F1TLines[m_playDataIndex];
				if (string.IsNullOrEmpty(f1TLine.Opecode))
				{
					if (f1TLine.DataList.Count <=0)
					{
						return SetSyntaxErrorLine(f1TLine);
					}
					else
					{
						for (int i = 0; i < f1TLine.DataList.Count; i ++)
						{
							if (f1TLine.DataList[i] >= 0x100 || f1TLine.DataList[i] < 0)
							{
								return SetDataErrorLine(f1TLine);
							}
							var data = (byte)f1TLine.DataList[i];
							AddOneDataToPlayImData(cs, a1:a1Array[cs], data0:data);
						}
					}
				}
				else
				{
					if (f1TLine.Opecode == F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.END])
					{
						AddEndCodeToPlayImData();
						return true;
					}
					else if (f1TLine.Opecode == F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.CS])
					{
						if (f1TLine.DataList.Count != 1)
						{
							return SetSyntaxErrorLine(f1TLine);
						}
						if (f1TLine.DataList[0] >= 0x100 || f1TLine.DataList[0] < 0)
						{
							return SetSyntaxErrorLine(f1TLine);
						}
						cs = (int)f1TLine.DataList[0];
					}
					else if (f1TLine.Opecode == F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.A1])
					{
						if (f1TLine.DataList.Count != 1)
						{
							return SetSyntaxErrorLine(f1TLine);
						}
						if (f1TLine.DataList[0] >= 0x100 || f1TLine.DataList[0] < 0)
						{
							return SetSyntaxErrorLine(f1TLine);
						}
						a1Array[cs] = (int)f1TLine.DataList[0];
					}
					else if (f1TLine.Opecode == F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.LP])
					{
						AddLoopPointToPlayImData();
					}
					else if (f1TLine.Opecode == F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.WAIT])
					{
						if (f1TLine.DataList.Count != 1)
						{
							return SetSyntaxErrorLine(f1TLine);
						}
						AddCycleWaitToPlayImData((int)f1TLine.DataList[0]);
					}
					else if (f1TLine.Opecode == F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.WRWAIT])
					{
						if (f1TLine.DataList.Count != 1)
						{
							return SetSyntaxErrorLine(f1TLine);
						}
						AddWriteWaitToPlayImData((int)f1TLine.DataList[0], isUseRunLength:false);
					}
					else if (f1TLine.Opecode == F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.WRWAITRL])
					{
						if (f1TLine.DataList.Count != 2)
						{
							return SetSyntaxErrorLine(f1TLine);
						}
						AddWriteWaitRunLengthToPlayImData((int)f1TLine.DataList[0], (int)f1TLine.DataList[1]);
					}
					else if (f1TLine.Opecode == F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.WRSEEK])
					{
						if (f1TLine.DataList.Count != 1)
						{
							return SetSyntaxErrorLine(f1TLine);
						}
						AddWriteSeekToPlayImData(((int)f1TLine.DataList[0]));
					}
					else
					{
						return SetErrorLine(f1TLine, "Play data has no End.");
					}
				}
				m_playDataIndex += 1;
				if (m_playDataIndex >= m_F1TLines.Count)
				{
					return SetError("Unexpected end of file.");
				}
			}
		}

		/// <summary>
		///	F1T	ヘッダーをパース
		/// </summary>
		private bool ParseHeader()
		{
			var isHheaderDataArray = new bool[(int)(F1TReservedWord.F1THeaderOpecode.MAX)];
			while(true)
			{
				var f1TLine = m_F1TLines[m_headerIndex];
				if (!string.IsNullOrEmpty(f1TLine.Label))
				{
					for (int i = 0;i < (int)(F1TReservedWord.F1THeaderOpecode.MAX); i++)
					{
						if (!isHheaderDataArray[i])
						{
							return SetErrorLine(f1TLine, $"Header has no {F1TReservedWord.F1THeaderOpecodeStrings[i]}");
						}
					}
					m_headerIndex -= 1;
					return true;
				}
				for (int i = 0;i < (int)(F1TReservedWord.F1THeaderOpecode.MAX); i++)
				{
					if (f1TLine.Opecode == F1TReservedWord.F1THeaderOpecodeStrings[i])
					{
						if (isHheaderDataArray[i])
						{
							return SetErrorLine(f1TLine, $"Multiple [{F1TReservedWord.F1THeaderOpecodeStrings[i]}] are defined.");
						}
						if (f1TLine.DataList.Count !=1)
						{
							return SetErrorLine(f1TLine, $"Parameter error.");
						}
						if (i != 2 && f1TLine.DataList[0] > 0xFF)
						{
							return SetErrorLine(f1TLine, $"Parameter error.");
						}
						isHheaderDataArray[i] = true;
						switch(i)
						{
							case 0:		//	"Version",
								Header.SetVersion(f1TLine.DataList[0]);
								break;
							case 1:		//	"LoopCount",
								Header.SetLoopCount(f1TLine.DataList[0]);
								break;
							case 2:		//	"OneWaitNs",
								Header.SetOneCycleNs(f1TLine.DataList[0]);
								break;
							case 3:		//	"CmdEnd",
								Header.SetCmdCodeEnd(f1TLine.DataList[0]);
								break;
							case 4:		//	"CmdA1",
								Header.SetCmdCodeA1(f1TLine.DataList[0]);
								break;
							case 5:		//	"CmdCS",
								Header.SetCmdCodeCS(f1TLine.DataList[0]);
								break;
							case 6:		//	"CmdLp",
								Header.SetCmdCodeLoop(f1TLine.DataList[0]);
								break;
							case 7:		//	"CmdByteW",
								Header.SetCmdCodeCycleWaitByte(f1TLine.DataList[0]);
								break;
							case 8:		//	"CmdWordW",
								Header.SetCmdCodeCycleWaitWord(f1TLine.DataList[0]);
								break;
							case 9:		//	"CmdW1",
								Header.SetCmdCode1Wait(f1TLine.DataList[0]);
								break;
							case 10:	//	"CmdW2",
								Header.SetCmdCode2Wait(f1TLine.DataList[0]);
								break;
							case 11:	//	"CmdW3",
								Header.SetCmdCode3Wait(f1TLine.DataList[0]);
								break;
							case 12:	//	"CmdW4",
								Header.SetCmdCode4Wait(f1TLine.DataList[0]);
								break;
							case 13:	//	"CmdW5",
								Header.SetCmdCode5Wait(f1TLine.DataList[0]);
								break;
							case 14:	//	"CmdW6",
								Header.SetCmdCode6Wait(f1TLine.DataList[0]);
								break;
							case 15:	//	"CmdWriteWait",
								Header.SetCmdCodeWriteWait(f1TLine.DataList[0]);
								break;
							case 16:	//	"CmdWriteWaitRunLength",
								Header.SetCmdCodeWriteWaitRunLength(f1TLine.DataList[0]);
								break;
							case 17:	//	"CmdWriteKeep",
								Header.SetCmdCodeWriteSeek(f1TLine.DataList[0]);
								break;
							case 18:	//	"CmdF0",
								Header.SetCmdCodeFree0(f1TLine.DataList[0]);
								break;
							case 19:	//	"CmdF1",
								Header.SetCmdCodeFree1(f1TLine.DataList[0]);
								break;
							case 20:	//	"CmdF2",
								Header.SetCmdCodeFree2(f1TLine.DataList[0]);
								break;
							case 21:	//	"CmdF3",
								Header.SetCmdCodeFree3(f1TLine.DataList[0]);
								break;
							case 22:	//	"CmdF4",
								Header.SetCmdCodeFree4(f1TLine.DataList[0]);
								break;
						}
						break;
					}
				}
				m_headerIndex += 1;
				if (m_headerIndex >= m_F1TLines.Count)
				{
					return SetError("Unexpected end of file.");
				}
			}
		}

		/// <summary>
		///	F1T	Lineのエラー
		/// </summary>
		private bool SetError(string errStr)
		{
			ErrorString = errStr;
			return false;
		}
		private bool SetErrorLine(F1TLine f1TLine, string errStr)
		{
			ErrorString = $"F1T {errStr}\r\nLine {f1TLine.LineNo}: {f1TLine.SourceStr}";
			return false;
		}
		private bool SetSyntaxErrorLine(F1TLine f1TLine)
		{
			ErrorString = $"F1T Syntax error.\r\nLine {f1TLine.LineNo}: {f1TLine.SourceStr}";
			return false;
		}
		private bool SetDataErrorLine(F1TLine f1TLine)
		{
			ErrorString = $"F1T Data error.\r\nLine {f1TLine.LineNo}: {f1TLine.SourceStr}";
			return false;
		}

		/// <summary>
		///	ラベル文字列のチェック
		/// </summary>
		private bool CheckLabelString()
		{
			m_headerIndex = -1;
			m_playDataIndex = -1;
			m_pcmDataIndex = -1;

			int index = 0;
			while(true)
			{
				var f1TLine = m_F1TLines[index];
				if (m_headerIndex < 0)
				{
					if (f1TLine.Label == F1TReservedWord.F1TLabelStrings[(int)F1TReservedWord.F1TLabel.HEADER])
					{
						m_headerIndex = index + 1;
					}
				}
				if (m_headerIndex >= 0)
				{
					if (m_playDataIndex < 0)
					{
						if (f1TLine.Label == F1TReservedWord.F1TLabelStrings[(int)F1TReservedWord.F1TLabel.PLAY_DATA])
						{
							m_playDataIndex = index + 1;
						}
					}
					if (m_pcmDataIndex < 0)
					{
						if (f1TLine.Label == F1TReservedWord.F1TLabelStrings[(int)F1TReservedWord.F1TLabel.PCM_DATA])
						{
							m_pcmDataIndex = index + 1;
						}
					}
				}
				index += 1;
				if (index >= m_F1TLines.Count)
				{
					break;
				}
			}
			if (m_headerIndex < 0)
			{
				return SetError("Header not found.");
			}
			if (m_playDataIndex < 0)
			{
				return SetError("Play Data not found.");
			}
			var count = m_F1TLines.Count;
			if (m_headerIndex >= count || m_playDataIndex >= count || m_pcmDataIndex >= count)
			{
				return SetError("Unexpected end of file.");
			}
			return true;
		}

		/// <summary>
		///	ソースからC#風のコメントを撤去してF1Tリストに収納する
		/// </summary>
		private bool CreateF1TLines()
		{
			for (int i = 0; i < TextArray.Length; i ++)
			{
				var tmpStr = TextArray[i];
				//	ダブルスラッシュ以降を撤去
                tmpStr = Regex.Replace(tmpStr, @"//.*", "");
				//	タブを１スペースに置き換え
				tmpStr = tmpStr.Replace("\t"," ");
				//	行頭と行末のスペースを撤去
				tmpStr = tmpStr.Trim();
				//	連続複数スペースを１スペースに置き換え
				tmpStr = Regex.Replace(tmpStr, @"\s\s+", " ");
				//	空行以外を確保する
				if (tmpStr != "")
				{
					var f1tLine = new F1TLine();
					f1tLine.LineNo = (i+1);
					f1tLine.SourceStr = tmpStr;
					m_F1TLines.Add(f1tLine);
				}
			}
			//	コメント行を撤去する
			bool isRem = false;
			foreach(var f1tLine in m_F1TLines)
			{
				var tmpStr = f1tLine.SourceStr;
				tmpStr = Regex.Replace(tmpStr, @"/\*(?s:.*?)\*/", "");
				if (isRem)
				{
					var p1 = tmpStr.IndexOf("*/");
					if (p1 >=0)
					{
						tmpStr = Regex.Replace(tmpStr, @".*\*/", "");
						isRem = false;
					}
					else
					{
						tmpStr ="";
					}
				}
				else
				{
					var p1 = tmpStr.IndexOf("/*");
					if (p1 >= 0)
					{
		                tmpStr = Regex.Replace(tmpStr, @"/\*.*", "");
						isRem = true;
					}
				}
				tmpStr = tmpStr.Trim();
				tmpStr = Regex.Replace(tmpStr, @"\s\s+", " ");
				f1tLine.SourceStr = tmpStr;
			}
			m_F1TLines.RemoveAll(p => p.SourceStr == "");

			//	ラインをカラムで分解する
			foreach(var f1tLine in m_F1TLines)
			{
				var tmpSplitSpace = f1tLine.SourceStr.Split(' ');
				f1tLine.Label = null;
				f1tLine.Opecode = null;
				f1tLine.DataList.Clear();
				int index = 0;
				for (int i = 0; i < (int)(F1TReservedWord.F1TLabel.MAX); i ++ )
				{
					if (tmpSplitSpace[index] == F1TReservedWord.F1TLabelStrings[i])
					{
						f1tLine.Label = F1TReservedWord.F1TLabelStrings[i];
						index += 1;
						break;
					}
				}
				if (index >= tmpSplitSpace.Length)
				{
					continue;
				}
				bool isOpecodeOk = false;
				for (int i = 0; i < (int)(F1TReservedWord.F1THeaderOpecode.MAX); i ++ )
				{
					if (tmpSplitSpace[index] == F1TReservedWord.F1THeaderOpecodeStrings[i])
					{
						f1tLine.Opecode = F1TReservedWord.F1THeaderOpecodeStrings[i];
						index += 1;
						isOpecodeOk = true;
						break;
					}
				}
				if (!isOpecodeOk)
				{
					for (int i = 0; i < (int)(F1TReservedWord.F1TPlayDataOpecode.MAX); i ++ )
					{
						if (tmpSplitSpace[index] == F1TReservedWord.F1TPlayDataOpecodeStrings[i])
						{
							f1tLine.Opecode = F1TReservedWord.F1TPlayDataOpecodeStrings[i];
							index += 1;
							isOpecodeOk = true;
							break;
						}
					}
				}
				if (!isOpecodeOk)
				{
					for (int i = 0; i < (int)(F1TReservedWord.F1TPcmDataOpecode.MAX); i ++ )
					{
						if (tmpSplitSpace[index] == F1TReservedWord.F1TPcmDataOpecodeStrings[i])
						{
							f1tLine.Opecode = F1TReservedWord.F1TPcmDataOpecodeStrings[i];
							index += 1;
							isOpecodeOk = true;
							break;
						}
					}
				}
				if (index >= tmpSplitSpace.Length)
				{
					continue;
				}

				StringBuilder sb = new StringBuilder("");
				for (int i = index; i < tmpSplitSpace.Length; i ++)
				{
					sb.Append($"{tmpSplitSpace[i]}");
				}
				var tmpSplitComma = sb.ToString().Split(',');
				int indexAdder = 0;
				for (int i = 0; i < tmpSplitComma.Length; i++)
				{
					uint data = 0;
					if (!string.IsNullOrEmpty(tmpSplitComma[i]))
					{
						indexAdder = 1;
						if (!StrToInt(tmpSplitComma[i], out data))
						{
							return SetSyntaxErrorLine(f1tLine);
						}
						f1tLine.DataList.Add(data);
					}
				}
				index += indexAdder;
				if (index == 0)
				{
					return SetSyntaxErrorLine(f1tLine);
				}
			}
			return true;
		}

		/// <summary>
		///	文字列の数値化
		/// </summary>
		private bool StrToInt(string str, out uint data)
		{
			data = 0;
			try {
				if (str.IndexOf(".") >= 0)
				{
					var tmpSplit = str.Split('.');
					if (tmpSplit.Length != 2) return false;
					uint hi = Convert.ToUInt32(tmpSplit[0],10);
					if (hi > 15) return false;
					uint lo = Convert.ToUInt32(tmpSplit[1],10);
					if (lo > 15) return false;
					data = ((hi << 4)&0xF0) | (lo & 0xF);
				}
				else if (str.StartsWith("0x"))
				{
					data = Convert.ToUInt32(str.Replace("0x",""), 16);
				}
				else if (str.StartsWith("0b"))
				{
					str = str.Replace("x","0");
					str = str.Replace("_","");
					data = Convert.ToUInt32(str.Replace("0b",""), 2);
				}
				else
				{
					str.Replace(".","");
					if (!uint.TryParse(str, out data))
					{
						return false;
					}
				}
			}
			catch
			{
				return false;
			}
			return true;
		}

	}
}
