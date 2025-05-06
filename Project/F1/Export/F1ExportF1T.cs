using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;

namespace F1
{
	///	<summary>
	///	F1T エクスポート クラス
	///	</summary>
	public class F1ExportF1T
	{
		private List<string> m_f1tDataList;

		/// <summary>
		/// F1T の生成
		/// </summary>
		public void CreateF1T(List<string> textDataList, F1Header header, F1ImData imData, bool isNoOutputPcmBlock)
		{
			m_f1tDataList = textDataList;

			CreateHeader(header);
			ExportPlayData(header, imData);
			if (!isNoOutputPcmBlock)
			{
				ExportPcmData(imData);
			}
		}

		/// <summary>
		///	PCM中間データを F1T にエクスポート
		/// </summary>
		private void ExportPcmData(F1ImData imData)
		{
			if (imData.PcmImDataList.Count != 0)
			{
				AddTextData(F1TReservedWord.F1TLabelStrings[(int)F1TReservedWord.F1TLabel.PCM_DATA],"","");
				for (int i = 0;i <imData.PcmImDataList.Count; i ++ )
				{
					AddTextData("","//------","");
					var pcmImData = imData.PcmImDataList[i];
					AddTextData("", F1TReservedWord.F1TPcmDataOpecodeStrings[(int)F1TReservedWord.F1TPcmDataOpecode.PCMHEADER], $"{pcmImData.m_chipSelect}, 0x{(int)(pcmImData.m_pcmDataType):X2}, 0x{pcmImData.m_pcmStart:X8}");
					AddTextData("","//------","");
					int ix = 0;
					var size = pcmImData.m_pcmSize / 32;
					var modSize = pcmImData.m_pcmSize & 0x1F;
					var pcm = pcmImData.m_pcmBinaryArray;
					for (int j=0; j < size; j++)
					{
						AddTextData("", F1TReservedWord.F1TPcmDataOpecodeStrings[(int)F1TReservedWord.F1TPcmDataOpecode.DATA], $"0x{pcm[ix+0x00]:X2}, 0x{pcm[ix+0x01]:X2}, 0x{pcm[ix+0x02]:X2}, 0x{pcm[ix+0x03]:X2}, 0x{pcm[ix+0x04]:X2}, 0x{pcm[ix+0x05]:X2}, 0x{pcm[ix+0x06]:X2}, 0x{pcm[ix+0x07]:X2}, 0x{pcm[ix+0x08]:X2}, 0x{pcm[ix+0x09]:X2}, 0x{pcm[ix+0x0A]:X2}, 0x{pcm[ix+0x0B]:X2}, 0x{pcm[ix+0x0C]:X2}, 0x{pcm[ix+0x0D]:X2}, 0x{pcm[ix+0x0E]:X2}, 0x{pcm[ix+0x0F]:X2}, 0x{pcm[ix+0x10]:X2}, 0x{pcm[ix+0x11]:X2}, 0x{pcm[ix+0x12]:X2}, 0x{pcm[ix+0x13]:X2}, 0x{pcm[ix+0x14]:X2}, 0x{pcm[ix+0x15]:X2}, 0x{pcm[ix+0x16]:X2}, 0x{pcm[ix+0x17]:X2}, 0x{pcm[ix+0x18]:X2}, 0x{pcm[ix+0x19]:X2}, 0x{pcm[ix+0x1A]:X2}, 0x{pcm[ix+0x1B]:X2}, 0x{pcm[ix+0x1C]:X2}, 0x{pcm[ix+0x1D]:X2}, 0x{pcm[ix+0x1E]:X2}, 0x{pcm[ix+0x1F]:X2},");
						ix += 32;
					}
					if (modSize != 0)
					{
						StringBuilder sb = new StringBuilder("");
						for (int j=0; j < modSize; j++)
						{
							sb.Append($"0x{pcm[ix]:X2}, ");
							ix += 1;
						}
						AddTextData("", F1TReservedWord.F1TPcmDataOpecodeStrings[(int)F1TReservedWord.F1TPcmDataOpecode.DATA], sb.ToString());
					}
				}
			}
		}

		/// <summary>
		///	PLAY中間データを F1T にエクスポート
		/// </summary>
		private void ExportPlayData(F1Header header, F1ImData imData)
		{
			int chipSelect = 0;
			var a1s = new int[256];
			bool is3CS = imData.IsPlayImDataList3CS();
			bool is3A1 = imData.IsPlayImDataList3A1();

			AddTextData(F1TReservedWord.F1TLabelStrings[(int)F1TReservedWord.F1TLabel.PLAY_DATA],"","");
			foreach(var playImData in imData.PlayImDataList)
			{
				switch(playImData.m_imType)
				{
					case F1ImData.PlayImType.TWO_DATA:
						if (chipSelect != playImData.m_chipSelect)
						{
							chipSelect = playImData.m_chipSelect;
							if (!is3CS)
							{
								AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.CS], $"{chipSelect}");
							}
							else
							{
								AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.CS], $"{chipSelect}");
							}
						}
						if (a1s[chipSelect] != playImData.m_A1)
						{
							a1s[chipSelect] = playImData.m_A1;
							if (!is3A1)
							{
								AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.A1], $"{a1s[chipSelect]}");
							}
							else
							{
								AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.A1], $"{a1s[chipSelect]}");
							}
						}
						if (imData.TopCodeCSList[chipSelect] < 0x100)
						{
							AddTextData("",$"\t0x{(imData.TopCodeCSList[chipSelect] & 0xFF):X2}, 0x{playImData.m_data0:X2}, 0x{playImData.m_data1:X2},","");
						}
						else
						{
							AddTextData("",$"\t0x{playImData.m_data0:X2}, 0x{playImData.m_data1:X2},","");
						}
						break;
					case F1ImData.PlayImType.ONE_DATA:
						if (chipSelect != playImData.m_chipSelect)
						{
							chipSelect = playImData.m_chipSelect;
							if (!is3CS)
							{
								AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.CS], $"{chipSelect}");
							}
							else
							{
								AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.CS], $"{chipSelect}");
							}
						}
						if (a1s[chipSelect] != playImData.m_A1)
						{
							a1s[chipSelect] = playImData.m_A1;
							if (!is3A1)
							{
								AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.A1],$"{a1s[chipSelect]}");
							}
							else
							{
								AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.A1],$"{a1s[chipSelect]}");
							}
						}
						if (imData.TopCodeCSList[chipSelect] < 0x100)
						{
							AddTextData("",$"\t0x{(imData.TopCodeCSList[chipSelect] & 0xFF):X2}, 0x{playImData.m_data0:X2},","");
						}
						else
						{
							AddTextData("",$"\t0x{playImData.m_data0:X2},","");
						}
						break;
					case F1ImData.PlayImType.VSTRM_DATA:
						if (chipSelect != playImData.m_chipSelect)
						{
							chipSelect = playImData.m_chipSelect;
							if (!is3CS)
							{
								AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.CS], $"{chipSelect}");
							}
							else
							{
								AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.CS], $"{chipSelect}");
							}
						}
						if (a1s[chipSelect] != playImData.m_A1)
						{
							a1s[chipSelect] = playImData.m_A1;
							if (!is3A1)
							{
								AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.A1],$"{a1s[chipSelect]}");
							}
							else
							{
								AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.A1],$"{a1s[chipSelect]}");
							}
						}
						if (imData.TopCodeCSList[chipSelect] < 0x100)
						{
							AddTextData("",$"\t0x{(imData.TopCodeCSList[chipSelect] & 0xFF):X2}, 0x{playImData.m_data0:X2}, 0x{playImData.m_data1:X2}, 0x{playImData.m_vStrmStepBase:X2}, 0x{playImData.m_vStrmStepSize:X2},","");
						}
						else
						{
							AddTextData("",$"\t0x{playImData.m_data0:X2}, 0x{playImData.m_data1:X2}, 0x{playImData.m_vStrmStepBase:X2}, 0x{playImData.m_vStrmStepSize:X2},","");
						}
						break;
					case F1ImData.PlayImType.VSTRM_SAMPLING_RATE:
						if (chipSelect != playImData.m_chipSelect)
						{
							chipSelect = playImData.m_chipSelect;
							if (!is3CS)
							{
								AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.CS], $"{chipSelect}");
							}
							else
							{
								AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.CS], $"{chipSelect}");
							}
						}
						if (a1s[chipSelect] != playImData.m_A1)
						{
							a1s[chipSelect] = playImData.m_A1;
							if (!is3A1)
							{
								AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.A1],$"{a1s[chipSelect]}");
							}
							else
							{
								AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.A1],$"{a1s[chipSelect]}");
							}
						}
						var f0 = (playImData.m_vStrmSamplingRate >> 24) & 0xFF;
						var f1 = (playImData.m_vStrmSamplingRate >> 16) & 0xFF;
						var f2 = (playImData.m_vStrmSamplingRate >>  8) & 0xFF;
						var f3 = (playImData.m_vStrmSamplingRate      ) & 0xFF;
						if (imData.TopCodeCSList[chipSelect] < 0x100)
						{
							AddTextData("",$"\t0x{(imData.TopCodeCSList[chipSelect] & 0xFF):X2}, 0x{playImData.m_data0:X2}, 0x{playImData.m_data1:X2}, 0x{f0:X2}, 0x{f1:X2}, 0x{f2:X2}, 0x{f3:X2},","");
						}
						else
						{
							AddTextData("",$"\t0x{playImData.m_data0:X2}, 0x{playImData.m_data1:X2}, 0x{f0:X2}, 0x{f1:X2}, 0x{f2:X2}, 0x{f3:X2},","");
						}
						break;
					case F1ImData.PlayImType.VSTRM_START_SIZE:
						if (chipSelect != playImData.m_chipSelect)
						{
							chipSelect = playImData.m_chipSelect;
							if (!is3CS)
							{
								AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.CS], $"{chipSelect}");
							}
							else
							{
								AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.CS], $"{chipSelect}");
							}
						}
						if (a1s[chipSelect] != playImData.m_A1)
						{
							a1s[chipSelect] = playImData.m_A1;
							if (!is3A1)
							{
								AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.A1],$"{a1s[chipSelect]}");
							}
							else
							{
								AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.A1],$"{a1s[chipSelect]}");
							}
						}
						var s0 = (playImData.m_vStrmStart >> 24) & 0xFF;
						var s1 = (playImData.m_vStrmStart >> 16) & 0xFF;
						var s2 = (playImData.m_vStrmStart >>  8) & 0xFF;
						var s3 = (playImData.m_vStrmStart      ) & 0xFF;
						var z0 = (playImData.m_vStrmSize >> 24) & 0xFF;
						var z1 = (playImData.m_vStrmSize >> 16) & 0xFF;
						var z2 = (playImData.m_vStrmSize >>  8) & 0xFF;
						var z3 = (playImData.m_vStrmSize      ) & 0xFF;
						if (imData.TopCodeCSList[chipSelect] < 0x100)
						{
							AddTextData("",$"\t0x{(imData.TopCodeCSList[chipSelect] & 0xFF):X2}, 0x{playImData.m_data0:X2}, 0x{playImData.m_data1:X2}, 0x{playImData.m_vStrmFlagMode:X2}, 0x{s0:X2}, 0x{s1:X2}, 0x{s2:X2}, 0x{s3:X2}, 0x{z0:X2}, 0x{z1:X2}, 0x{z2:X2}, 0x{z3:X2},","");
						}
						else
						{
							AddTextData("",$"\t0x{playImData.m_data0:X2}, 0x{playImData.m_data1:X2}, 0x{playImData.m_vStrmFlagMode:X2}, 0x{s0:X2}, 0x{s1:X2}, 0x{s2:X2}, 0x{s3:X2}, 0x{z0:X2}, 0x{z1:X2}, 0x{z2:X2}, 0x{z3:X2},","");
						}
						break;
					case F1ImData.PlayImType.VSTRM_STOP_STREAM:
						if (chipSelect != playImData.m_chipSelect)
						{
							chipSelect = playImData.m_chipSelect;
							if (!is3CS)
							{
								AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.CS], $"{chipSelect}");
							}
							else
							{
								AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.CS], $"{chipSelect}");
							}
						}
						if (a1s[chipSelect] != playImData.m_A1)
						{
							a1s[chipSelect] = playImData.m_A1;
							if (!is3A1)
							{
								AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.A1],$"{a1s[chipSelect]}");
							}
							else
							{
								AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.A1],$"{a1s[chipSelect]}");
							}
						}
						if (imData.TopCodeCSList[chipSelect] < 0x100)
						{
							AddTextData("",$"\t0x{(imData.TopCodeCSList[chipSelect] & 0xFF):X2}, 0x{playImData.m_data0:X2}, 0x{playImData.m_data1:X2},","");
						}
						else
						{
							AddTextData("",$"\t0x{playImData.m_data0:X2}, 0x{playImData.m_data1:X2},,","");
						}
						break;
					case F1ImData.PlayImType.VSTRM_START_SIZE_FAST:
						if (chipSelect != playImData.m_chipSelect)
						{
							chipSelect = playImData.m_chipSelect;
							if (!is3CS)
							{
								AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.CS], $"{chipSelect}");
							}
							else
							{
								AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.CS], $"{chipSelect}");
							}
						}
						if (a1s[chipSelect] != playImData.m_A1)
						{
							a1s[chipSelect] = playImData.m_A1;
							if (!is3A1)
							{
								AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.A1],$"{a1s[chipSelect]}");
							}
							else
							{
								AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.A1],$"{a1s[chipSelect]}");
							}
						}
						var b0 = (playImData.m_vStrmBlockId >>  8) & 0xFF;
						var b1 = (playImData.m_vStrmBlockId      ) & 0xFF;
						var s00 = (playImData.m_vStrmStart >> 24) & 0xFF;
						var s01 = (playImData.m_vStrmStart >> 16) & 0xFF;
						var s02 = (playImData.m_vStrmStart >>  8) & 0xFF;
						var s03 = (playImData.m_vStrmStart      ) & 0xFF;
						var z00 = (playImData.m_vStrmSize >> 24) & 0xFF;
						var z01 = (playImData.m_vStrmSize >> 16) & 0xFF;
						var z02 = (playImData.m_vStrmSize >>  8) & 0xFF;
						var z03 = (playImData.m_vStrmSize      ) & 0xFF;
						if (imData.TopCodeCSList[chipSelect] < 0x100)
						{
							AddTextData("",$"\t0x{(imData.TopCodeCSList[chipSelect] & 0xFF):X2}, 0x{playImData.m_data0:X2}, 0x{playImData.m_data1:X2}, 0x{b0:X2}, 0x{b1:X2}, 0x{playImData.m_vStrmFlagMode:X2}, 0x{s00:X2}, 0x{s01:X2}, 0x{s02:X2}, 0x{s03:X2}, 0x{z00:X2}, 0x{z01:X2}, 0x{z02:X2}, 0x{z03:X2},","");
						}
						else
						{
							AddTextData("",$"\t0x{playImData.m_data0:X2}, 0x{playImData.m_data1:X2}, 0x{b0:X2}, 0x{b1:X2}, 0x{playImData.m_vStrmFlagMode:X2}, 0x{s00:X2}, 0x{s01:X2}, 0x{s02:X2}, 0x{s03:X2}, 0x{z00:X2}, 0x{z01:X2}, 0x{z02:X2}, 0x{z03:X2},","");
						}
						break;
					case F1ImData.PlayImType.CYCLE_WAIT:
						AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.WAIT], $"{playImData.m_cycleWait}");
						break;
					case F1ImData.PlayImType.WRITE_WAIT:
						AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.WRWAIT], $"{playImData.m_writeWait}");
						break;
					case F1ImData.PlayImType.WRITE_WAIT_RL:
						AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.WRWAITRL], $"{playImData.m_writeWaitRL}, {playImData.m_writeRunLength}");
						break;
					case F1ImData.PlayImType.WRITE_SEEK:
						AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.WRSEEK], $"{playImData.m_seekAddress}");
						break;
					case F1ImData.PlayImType.END_CODE:
						AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.END],"");
						break;
					case F1ImData.PlayImType.LOOP_POINT:
						AddTextData("", F1TReservedWord.F1TPlayDataOpecodeStrings[(int)F1TReservedWord.F1TPlayDataOpecode.LP],"");
						break;
					default:
						break;
				}
			}
		}

		/// <summary>
		///	F1T ヘッダーの生成
		/// </summary>
		private void CreateHeader(F1Header header)
		{
			AddTextData(F1TReservedWord.F1TLabelStrings[(int)F1TReservedWord.F1TLabel.HEADER],"","");
			AddTextData("",F1TReservedWord.F1THeaderOpecodeStrings[(int)F1TReservedWord.F1THeaderOpecode.VERSION],$"{header.GetVersionString()}");
			AddTextData("",F1TReservedWord.F1THeaderOpecodeStrings[(int)F1TReservedWord.F1THeaderOpecode.LOOP_COUNT],$"{header.GetLoopCount()}");
			AddTextData("",F1TReservedWord.F1THeaderOpecodeStrings[(int)F1TReservedWord.F1THeaderOpecode.ONE_WAIT_NS],$"{header.GetOneCycleNs()}");
			AddTextData("",F1TReservedWord.F1THeaderOpecodeStrings[(int)F1TReservedWord.F1THeaderOpecode.CMD_END],$"0x{header.GetCmdCodeEnd():X2}");
			AddTextData("",F1TReservedWord.F1THeaderOpecodeStrings[(int)F1TReservedWord.F1THeaderOpecode.CMD_A1],$"0x{header.GetCmdCodeA1():X2}");
			AddTextData("",F1TReservedWord.F1THeaderOpecodeStrings[(int)F1TReservedWord.F1THeaderOpecode.CMD_CS],$"0x{header.GetCmdCodeCS():X2}");
			AddTextData("",F1TReservedWord.F1THeaderOpecodeStrings[(int)F1TReservedWord.F1THeaderOpecode.CMD_LP],$"0x{header.GetCmdCodeLoop():X2}");
			AddTextData("",F1TReservedWord.F1THeaderOpecodeStrings[(int)F1TReservedWord.F1THeaderOpecode.CMD_BYTE_W],$"0x{header.GetCmdCodeCycleWaitByte():X2}");
			AddTextData("",F1TReservedWord.F1THeaderOpecodeStrings[(int)F1TReservedWord.F1THeaderOpecode.CMD_WORD_W],$"0x{header.GetCmdCodeCycleWaitWord():X2}");
			AddTextData("",F1TReservedWord.F1THeaderOpecodeStrings[(int)F1TReservedWord.F1THeaderOpecode.CMD_W1],$"0x{header.GetCmdCodeCycle1Wait():X2}");
			AddTextData("",F1TReservedWord.F1THeaderOpecodeStrings[(int)F1TReservedWord.F1THeaderOpecode.CMD_W2],$"0x{header.GetCmdCodeCycle2Wait():X2}");
			AddTextData("",F1TReservedWord.F1THeaderOpecodeStrings[(int)F1TReservedWord.F1THeaderOpecode.CMD_W3],$"0x{header.GetCmdCodeCycle3Wait():X2}");
			AddTextData("",F1TReservedWord.F1THeaderOpecodeStrings[(int)F1TReservedWord.F1THeaderOpecode.CMD_W4],$"0x{header.GetCmdCodeCycle4Wait():X2}");
			AddTextData("",F1TReservedWord.F1THeaderOpecodeStrings[(int)F1TReservedWord.F1THeaderOpecode.CMD_W5],$"0x{header.GetCmdCodeCycle5Wait():X2}");
			AddTextData("",F1TReservedWord.F1THeaderOpecodeStrings[(int)F1TReservedWord.F1THeaderOpecode.CMD_W6],$"0x{header.GetCmdCodeCycle6Wait():X2}");
			AddTextData("",F1TReservedWord.F1THeaderOpecodeStrings[(int)F1TReservedWord.F1THeaderOpecode.CMD_WR_WAIT],$"0x{header.GetCmdCodeWriteWait():X2}");
			AddTextData("",F1TReservedWord.F1THeaderOpecodeStrings[(int)F1TReservedWord.F1THeaderOpecode.CMD_WR_WAIT_RL],$"0x{header.GetCmdCodeWriteWaitRunLength():X2}");
			AddTextData("",F1TReservedWord.F1THeaderOpecodeStrings[(int)F1TReservedWord.F1THeaderOpecode.CMD_WR_SEEK],$"0x{header.GetCmdCodeWriteSeek():X2}");
			AddTextData("",F1TReservedWord.F1THeaderOpecodeStrings[(int)F1TReservedWord.F1THeaderOpecode.CMD_F0],$"0x{header.GetCmdCodeFree0():X2}");
			AddTextData("",F1TReservedWord.F1THeaderOpecodeStrings[(int)F1TReservedWord.F1THeaderOpecode.CMD_F1],$"0x{header.GetCmdCodeFree1():X2}");
			AddTextData("",F1TReservedWord.F1THeaderOpecodeStrings[(int)F1TReservedWord.F1THeaderOpecode.CMD_F2],$"0x{header.GetCmdCodeFree2():X2}");
			AddTextData("",F1TReservedWord.F1THeaderOpecodeStrings[(int)F1TReservedWord.F1THeaderOpecode.CMD_F3],$"0x{header.GetCmdCodeFree3():X2}");
			AddTextData("",F1TReservedWord.F1THeaderOpecodeStrings[(int)F1TReservedWord.F1THeaderOpecode.CMD_F4],$"0x{header.GetCmdCodeFree4():X2}");
		}

		/// <summary>
		///	F1T データに追加
		/// </summary>
		private void AddTextData(string label, string opecode, string param)
		{
			if (string.IsNullOrEmpty(label))
			{
				string t =(opecode.Length >= 8) ? "\t" : "\t\t";
				m_f1tDataList.Add($"\t{opecode}{t}{param}");
			}
			else
			{
				m_f1tDataList.Add(label);
			}
		}

	}
}
