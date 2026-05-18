using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;

namespace F1
{
	///	<summary>
	///	F1 Text エクスポート クラス
	///	</summary>
	public class F1ExportText
	{
		private int m_address;
		private List<string> m_textDataList;

		/// <summary>
		/// F1 Text の生成
		/// </summary>
		public void CreateText(List<string> textDataList, F1Header header, F1ImData imData, bool isNoOutputPcmBlock)
		{
			m_address = 0;
			m_textDataList = textDataList;

			CreataHeader(header);
			ExportPlayData(header, imData);
			if (!isNoOutputPcmBlock)
			{
				ExportPcmData(imData);
			}
		}

		/// <summary>
		/// PCM中間データを F1 Text にエクスポート
		/// </summary>
		private void ExportPcmData(F1ImData imData)
		{
			if (imData.PcmImDataList.Count != 0)
			{
				int nextAddress = 0;
				int keepIndex = -1;
				int keepAddress = 0;
				uint d0 = 0;
				uint d1 = 0;
				uint d2 = 0;
				uint d3 = 0;
				m_textDataList.Add("F1 PcmData:");
				foreach(var pcmImData in imData.PcmImDataList)
				{
					AddTextData($"{pcmImData.m_chipSelect:X2}", cmdStr:"ChipSelext", dataStr:$"{pcmImData.m_chipSelect}");
					m_address += 1;

					d0 = (uint)((pcmImData.m_pcmStart >> 24) & 0xFF);
					d1 = (uint)((pcmImData.m_pcmStart >> 16) & 0xFF);
					d2 = (uint)((pcmImData.m_pcmStart >>  8) & 0xFF);
					d3 = (uint) (pcmImData.m_pcmStart		   & 0xFF);
					AddTextData($"{d0:X2} {d1:X2} {d2:X2} {d3:X2}", cmdStr:"StartAddress", dataStr:$"0x{pcmImData.m_pcmStart:X8}");
					m_address += 4;

					d0 = (uint)((pcmImData.m_pcmSize >> 24) & 0xFF);
					d1 = (uint)((pcmImData.m_pcmSize >> 16) & 0xFF);
					d2 = (uint)((pcmImData.m_pcmSize >>  8) & 0xFF);
					d3 = (uint) (pcmImData.m_pcmSize		  & 0xFF);
					AddTextData($"{d0:X2} {d1:X2} {d2:X2} {d3:X2}", cmdStr:"Size", dataStr:$"0x{pcmImData.m_pcmSize:X8}");
					m_address += 4;
					if (keepIndex >= 0)
					{
						d0 = (uint)((nextAddress >> 24) & 0xFF);
						d1 = (uint)((nextAddress >> 16) & 0xFF);
						d2 = (uint)((nextAddress >>  8) & 0xFF);
						d3 = (uint)(nextAddress & 0xFF);
						SetTextData(keepAddress, keepIndex, $"{d0:X2} {d1:X2} {d2:X2} {d3:X2}", cmdStr:"NextPcmData", dataStr:$"{nextAddress:X8}");
					}
					keepIndex = m_textDataList.Count;
					keepAddress = m_address;
					AddTextData($"FF FF FF FF", cmdStr:"NextPcmData", dataStr:"--------");
					m_address += 4;
					AddTextData($"Binary", cmdStr:$"Size:{pcmImData.m_pcmBinaryArray.Length:X8}");
					m_textDataList.Add("");
					m_address += pcmImData.m_pcmBinaryArray.Length;
					nextAddress = m_address;
				}
			}
			m_textDataList.Add("");
		}

		/// <summary>
		/// PLAY中間データを F1 Text にエクスポート
		/// </summary>
		private void ExportPlayData(F1Header header, F1ImData imData)
		{
			uint d0;
			uint d1;
			int chipSelect = 0;
			var a1s = new int[256];
			var is3CS = imData.IsPlayImDataList3CS();
			var is3A1 = imData.IsPlayImDataList3A1();
			int wait = 0;
			string tmpStr = "";

			m_textDataList.Add("F1 PlayData:");
			foreach(var playImData in imData.PlayImDataList)
			{
				switch(playImData.m_imType)
				{
					case F1ImData.PlayImType.TWO_DATA:
						if (chipSelect != playImData.m_chipSelect)
						{
							chipSelect = playImData.m_chipSelect;
							d0 = (uint)header.GetCmdCodeCS();
							if (!is3CS)
							{
								AddTextData($"{d0:X2}", cmdStr:"ChangeCS", dataStr:$"{chipSelect}");
								m_address += 1;
							}
							else
							{
								AddTextData($"{d0:X2} {chipSelect:X2}", cmdStr:"ChangeCS", dataStr:$"{d0}");
								m_address += 2;
							}
						}
						if (a1s[chipSelect] != playImData.m_A1)
						{
							a1s[chipSelect] = playImData.m_A1;
							d0 = (uint)header.GetCmdCodeA1();
							if (!is3A1)
							{
								AddTextData($"{d0:X2}", cmdStr:"ChangeA1", dataStr:$"{a1s[chipSelect]}");
								m_address += 1;
							}
							else
							{
								AddTextData($"{d0:X2} {a1s[chipSelect]:X2}", cmdStr:"ChangeA1", dataStr:$"{a1s[chipSelect]}");
								m_address += 2;
							}
						}

						if (imData.TopCodeCSList[chipSelect] < 0x100)
						{
							AddTextData($"{(imData.TopCodeCSList[chipSelect] & 0xFF):X2} {playImData.m_data0:X2} {playImData.m_data1:X2}");
							m_address += 3;
						}
						else
						{
							AddTextData($"{playImData.m_data0:X2} {playImData.m_data1:X2}");
							m_address += 2;
						}
						break;
					case F1ImData.PlayImType.ONE_DATA:
						if (chipSelect != playImData.m_chipSelect)
						{
							chipSelect = playImData.m_chipSelect;
							if (!is3CS)
							{
								d0 = (uint)header.GetCmdCodeCS();
								AddTextData($"{d0:X2}", cmdStr:"ChangeCS", dataStr:$"{chipSelect}");
								m_address += 1;
							}
							else
							{
								d0 = (uint)header.GetCmdCodeCS();
								AddTextData($"{d0:X2} {chipSelect:X2}", cmdStr:"ChangeCS", dataStr:$"{chipSelect:X2}");
								m_address += 2;
							}
						}
						if (a1s[chipSelect] != playImData.m_A1)
						{
							a1s[chipSelect] = playImData.m_A1;
							d0 = (uint)header.GetCmdCodeA1();
							if (!is3A1)
							{
								AddTextData($"{d0:X2}", cmdStr:"ChangeA1", dataStr:$"{a1s[chipSelect]}");
								m_address += 1;
							}
							else
							{
								AddTextData($"{d0:X2} {a1s[chipSelect]:X2}", "ChangeA1", $"{a1s[chipSelect]}");
								m_address += 2;
							}
						}
						if (imData.TopCodeCSList[chipSelect] < 0x100)
						{
							AddTextData($"{(imData.TopCodeCSList[chipSelect] & 0xFF):X2} {playImData.m_data0:X2}");
							m_address += 2;
						}
						else
						{
							AddTextData($"{playImData.m_data0:X2}");
							m_address += 1;
						}
						break;
					case F1ImData.PlayImType.VSTRM_DATA:
						if (chipSelect != playImData.m_chipSelect)
						{
							chipSelect = playImData.m_chipSelect;
							if (!is3CS)
							{
								d0 = (uint)header.GetCmdCodeCS();
								AddTextData($"{d0:X2}", cmdStr:"ChangeCS", dataStr:$"{chipSelect}");
								m_address += 1;
							}
							else
							{
								d0 = (uint)header.GetCmdCodeCS();
								AddTextData($"{d0:X2} {chipSelect:X2}", cmdStr:"ChangeCS", dataStr:$"{chipSelect:X2}");
								m_address += 2;
							}
						}
						if (a1s[chipSelect] != playImData.m_A1)
						{
							a1s[chipSelect] = playImData.m_A1;
							d0 = (uint)header.GetCmdCodeA1();
							if (!is3A1)
							{
								AddTextData($"{d0:X2}", cmdStr:"ChangeA1", dataStr:$"{a1s[chipSelect]}");
								m_address += 1;
							}
							else
							{
								AddTextData($"{d0:X2} {a1s[chipSelect]:X2}", cmdStr:"ChangeA1", dataStr:$"{a1s[chipSelect]}");
								m_address += 2;
							}
						}
						if (imData.TopCodeCSList[chipSelect] < 0x100)
						{
							AddTextData($"{(imData.TopCodeCSList[chipSelect] & 0xFF):X2} {playImData.m_data0:X2} {playImData.m_data1:X2} {playImData.m_vStrmStepBase:X2} {playImData.m_vStrmStepSize:X2}");
							m_address += 5;
						}
						else
						{
							AddTextData($"{playImData.m_data0:X2} {playImData.m_data1:X2} {playImData.m_vStrmStepBase:X2} {playImData.m_vStrmStepSize:X2}");
							m_address += 4;
						}
						break;
					case F1ImData.PlayImType.VSTRM_SAMPLING_RATE:
						if (chipSelect != playImData.m_chipSelect)
						{
							chipSelect = playImData.m_chipSelect;
							if (!is3CS)
							{
								d0 = (uint)header.GetCmdCodeCS();
								AddTextData($"{d0:X2}", cmdStr:"ChangeCS", dataStr:$"{chipSelect}");
								m_address += 1;
							}
							else
							{
								d0 = (uint)header.GetCmdCodeCS();
								AddTextData($"{d0:X2} {chipSelect:X2}", cmdStr:"ChangeCS", dataStr:$"{chipSelect:X2}");
								m_address += 2;
							}
						}
						if (a1s[chipSelect] != playImData.m_A1)
						{
							a1s[chipSelect] = playImData.m_A1;
							d0 = (uint)header.GetCmdCodeA1();
							if (!is3A1)
							{
								AddTextData($"{d0:X2}", cmdStr:"ChangeA1", dataStr:$"{a1s[chipSelect]}");
								m_address += 1;
							}
							else
							{
								AddTextData($"{d0:X2} {a1s[chipSelect]:X2}", cmdStr:"ChangeA1", dataStr:$"{a1s[chipSelect]}");
								m_address += 2;
							}
						}
						if (imData.TopCodeCSList[chipSelect] < 0x100)
						{
							AddTextData($"{(imData.TopCodeCSList[chipSelect] & 0xFF):X2} {playImData.m_data0:X2} {playImData.m_data1:X2} {playImData.m_vStrmSamplingRate:X8}");
							m_address += 7;
						}
						else
						{
							AddTextData($"{playImData.m_data0:X2} {playImData.m_data1:X2} {playImData.m_vStrmSamplingRate:X8}");
							m_address += 6;
						}
						break;
					case F1ImData.PlayImType.VSTRM_START_SIZE:
						if (chipSelect != playImData.m_chipSelect)
						{
							chipSelect = playImData.m_chipSelect;
							if (!is3CS)
							{
								d0 = (uint)header.GetCmdCodeCS();
								AddTextData($"{d0:X2}", cmdStr:"ChangeCS", dataStr:$"{chipSelect}");
								m_address += 1;
							}
							else
							{
								d0 = (uint)header.GetCmdCodeCS();
								AddTextData($"{d0:X2} {chipSelect:X2}", cmdStr:"ChangeCS", dataStr:$"{chipSelect:X2}");
								m_address += 2;
							}
						}
						if (a1s[chipSelect] != playImData.m_A1)
						{
							a1s[chipSelect] = playImData.m_A1;
							d0 = (uint)header.GetCmdCodeA1();
							if (!is3A1)
							{
								AddTextData($"{d0:X2}", cmdStr:"ChangeA1", dataStr:$"{a1s[chipSelect]}");
								m_address += 1;
							}
							else
							{
								AddTextData($"{d0:X2} {a1s[chipSelect]:X2}", cmdStr:"ChangeA1", dataStr:$"{a1s[chipSelect]}");
								m_address += 2;
							}
						}
						if (imData.TopCodeCSList[chipSelect] < 0x100)
						{
							AddTextData($"{(imData.TopCodeCSList[chipSelect] & 0xFF):X2} {playImData.m_data0:X2} {playImData.m_data1:X2} {playImData.m_vStrmFlagMode:X2} {playImData.m_vStrmStart:X8} {playImData.m_vStrmSize:X8}");
							m_address += 12;
						}
						else
						{
							AddTextData($"{playImData.m_data0:X2} {playImData.m_data1:X2} {playImData.m_vStrmFlagMode:X2} {playImData.m_vStrmStart:X8} {playImData.m_vStrmSize:X8}");
							m_address += 11;
						}
						break;
					case F1ImData.PlayImType.VSTRM_STOP_STREAM:
						if (chipSelect != playImData.m_chipSelect)
						{
							chipSelect = playImData.m_chipSelect;
							if (!is3CS)
							{
								d0 = (uint)header.GetCmdCodeCS();
								AddTextData($"{d0:X2}", cmdStr:"ChangeCS", dataStr:$"{chipSelect}");
								m_address += 1;
							}
							else
							{
								d0 = (uint)header.GetCmdCodeCS();
								AddTextData($"{d0:X2} {chipSelect:X2}", cmdStr:"ChangeCS", dataStr:"{chipSelect:X2}");
								m_address += 2;
							}
						}
						if (a1s[chipSelect] != playImData.m_A1)
						{
							a1s[chipSelect] = playImData.m_A1;
							d0 = (uint)header.GetCmdCodeA1();
							if (!is3A1)
							{
								AddTextData($"{d0:X2}", cmdStr:"ChangeA1", dataStr:$"{a1s[chipSelect]}");
								m_address += 1;
							}
							else
							{
								AddTextData($"{d0:X2} {a1s[chipSelect]:X2}", cmdStr:"ChangeA1", dataStr:$"{a1s[chipSelect]}");
								m_address += 2;
							}
						}
						if (imData.TopCodeCSList[chipSelect] < 0x100)
						{
							AddTextData($"{(imData.TopCodeCSList[chipSelect] & 0xFF):X2} {playImData.m_data0:X2} {playImData.m_data1:X2}");
							m_address += 3;
						}
						else
						{
							AddTextData($"{playImData.m_data0:X2} {playImData.m_data1:X2}");
							m_address += 2;
						}
						break;
					case F1ImData.PlayImType.VSTRM_START_SIZE_FAST:
						if (chipSelect != playImData.m_chipSelect)
						{
							chipSelect = playImData.m_chipSelect;
							if (!is3CS)
							{
								d0 = (uint)header.GetCmdCodeCS();
								AddTextData($"{d0:X2}", cmdStr:"ChangeCS", dataStr:$"{chipSelect}");
								m_address += 1;
							}
							else
							{
								d0 = (uint)header.GetCmdCodeCS();
								AddTextData($"{d0:X2} {chipSelect:X2}", cmdStr:"ChangeCS", dataStr:$"{chipSelect:X2}");
								m_address += 2;
							}
						}
						if (a1s[chipSelect] != playImData.m_A1)
						{
							a1s[chipSelect] = playImData.m_A1;
							d0 = (uint)header.GetCmdCodeA1();
							if (!is3A1)
							{
								AddTextData($"{d0:X2}", cmdStr:"ChangeA1", dataStr:$"{a1s[chipSelect]}");
								m_address += 1;
							}
							else
							{
								AddTextData($"{d0:X2} {a1s[chipSelect]:X2}", cmdStr:"ChangeA1", dataStr:$"{a1s[chipSelect]}");
								m_address += 2;
							}
						}
						if (imData.TopCodeCSList[chipSelect] < 0x100)
						{
							AddTextData($"{(imData.TopCodeCSList[chipSelect] & 0xFF):X2} {playImData.m_data0:X2} {playImData.m_data1:X2} {playImData.m_vStrmBlockId:X4} {playImData.m_vStrmFlagMode:X2} {playImData.m_vStrmStart:X8} {playImData.m_vStrmSize:X8}");
							m_address += 14;
						}
						else
						{
							AddTextData($"{playImData.m_data0:X2} {playImData.m_data1:X2} {playImData.m_vStrmBlockId:X4} {playImData.m_vStrmFlagMode:X2} {playImData.m_vStrmStart:X8} {playImData.m_vStrmSize:X8}");
							m_address += 13;
						}
						break;
					case F1ImData.PlayImType.CYCLE_WAIT:
						wait = playImData.m_cycleWait;
						while(wait != 0)
						{
							if (wait <=6)
							{
								tmpStr = "";
								switch(wait)
								{
									case 1: d1 = (uint)header.GetCmdCodeCycle1Wait(); tmpStr = "CycleWait1"; break;
									case 2: d1 = (uint)header.GetCmdCodeCycle2Wait(); tmpStr = "CycleWait2"; break;
									case 3: d1 = (uint)header.GetCmdCodeCycle3Wait(); tmpStr = "CycleWait3"; break;
									case 4: d1 = (uint)header.GetCmdCodeCycle4Wait(); tmpStr = "CycleWait4"; break;
									case 5: d1 = (uint)header.GetCmdCodeCycle5Wait(); tmpStr = "CycleWait5"; break;
									case 6: d1 = (uint)header.GetCmdCodeCycle5Wait(); tmpStr = "CycleWait6"; break;
									default: d1 = 0; break;
								}
								AddTextData($"{d1:X2}", cmdStr:tmpStr, dataStr:$"{wait}", waitCountStr:$"WaitCount:{playImData.m_cycleWaitCount}");
								wait = 0;
								m_address += 1;
							}
							else if (wait <= 255)
							{
								d1 = (uint)header.GetCmdCodeCycleWaitByte();
								AddTextData($"{d1:X2} {wait:X2}", cmdStr:$"CycleWaitByte", dataStr:$"{wait}", waitCountStr:$"WaitCount:{playImData.m_cycleWaitCount}");
								wait = 0;
								m_address += 2;
							}
							else
							{
								int div = wait / 0xFFFF;
								int mod = wait % 0xFFFF;
								div += 1;
								d1 = (uint)header.GetCmdCodeCycleWaitWord();
								while(div > 0)
								{
									div -= 1;
									if (div != 0)
									{
										AddTextData($"{d1:X2} FF FF", cmdStr:"CycleWaitWord", dataStr:"65535", waitCountStr:$"WaitCount:{playImData.m_cycleWaitCount}");
										m_address += 3;
										wait -= 0xFFFF;
									}
									else
									{
										AddTextData($"{d1:X2} {((wait >>8) & 0xFF):X2} {(wait & 0xFF):X2}", cmdStr:"CycleWaitWord", dataStr:$"{wait}", waitCountStr:$"WaitCount:{playImData.m_cycleWaitCount}");
										m_address += 3;
										wait = 0;
									}
								}
							}
						}
						break;
					case F1ImData.PlayImType.WRITE_WAIT:
						AddTextData($"{header.GetCmdCodeWriteWait():X2} {playImData.m_writeWait:X2}", cmdStr:"WriteWait", dataStr:$"{playImData.m_writeWait}");
						m_address += 2;
						break;
					case F1ImData.PlayImType.WRITE_WAIT_RL:
						AddTextData($"{header.GetCmdCodeWriteWaitRunLength():X2} {playImData.m_writeWaitRL:X2} {playImData.m_writeRunLength:X2}", cmdStr:"WriteWaitRL", dataStr:$"{playImData.m_writeWaitRL}, {playImData.m_writeRunLength}");
						m_address += 3;
						break;
					case F1ImData.PlayImType.WRITE_SEEK:
						AddTextData($"{header.GetCmdCodeWriteSeek():X2} {((playImData.m_seekAddress >> 24) & 0xFF):X2} {((playImData.m_seekAddress >> 16) & 0xFF):X2} {((playImData.m_seekAddress >> 8) & 0xFF):X2} {(playImData.m_seekAddress  & 0xFF):X2}", cmdStr:"WriteSeek", dataStr:$"{playImData.m_seekAddress:X08}");
						m_address += 5;
						break;
					case F1ImData.PlayImType.END_CODE:
						AddTextData($"{header.GetCmdCodeEnd():X2}", cmdStr: "EndCode");
						m_address += 1;
						break;
					case F1ImData.PlayImType.LOOP_POINT:
						AddTextData($"{header.GetCmdCodeLoop():X2}", cmdStr:"LoopPoint");
						m_address += 1;
						break;
					default:
						break;
				}
			}
			m_textDataList.Add("");
		}

		/// <summary>
		/// F1 Text ヘッダーを生成
		/// </summary>
		private void CreataHeader(F1Header header)
		{
			uint d0 = 0;
			string byteStr = "";

			m_textDataList.Add("F1 Header:");

			header.GetDataTextDump(m_address, DataSize.DW, out byteStr);
			AddTextData(byteStr, cmdStr:"HeaderID", dataStr:header.GetIDString());
			m_address += 2;
			header.GetDataTextDump(m_address, DataSize.DB, out byteStr);
			AddTextData(byteStr, cmdStr:"F1 Version", dataStr:header.GetVersionString());
			m_address += 1;
			d0 = header.GetDataTextDump(m_address, DataSize.DB, out byteStr);
			AddTextData(byteStr, cmdStr:"Loop Count", dataStr:d0.ToString());
			m_address += 1;
			d0 = header.GetDataTextDump(m_address, DataSize.DL, out byteStr);
			AddTextData(byteStr, cmdStr:"OneWaitTime", dataStr:$"{d0}[ns]");
			m_address += 4;
			d0 = header.GetDataTextDump(m_address, DataSize.DL, out byteStr);
			AddTextData(byteStr, cmdStr:"PCM Data", dataStr:$"0x{d0:X8}");
			m_address += 4;
			d0 = header.GetDataTextDump(m_address, DataSize.DB, out byteStr);
			AddTextData(byteStr, cmdStr:"Cmd:EndCode", dataStr:d0.ToString("X2"));
			m_address += 1;
			d0 = header.GetDataTextDump(m_address, DataSize.DB, out byteStr);
			AddTextData(byteStr, cmdStr:"Cmd:ChangeA1", dataStr:d0.ToString("X2"));
			m_address += 1;
			d0 = header.GetDataTextDump(m_address, DataSize.DB, out byteStr);
			AddTextData(byteStr, cmdStr:"Cmd:ChangeCS", dataStr:d0.ToString("X2"));
			m_address += 1;
			d0 = header.GetDataTextDump(m_address, DataSize.DB, out byteStr);
			AddTextData(byteStr, cmdStr:"Cmd:LoopPoint", dataStr:d0.ToString("X2"));
			m_address += 1;
			d0 = header.GetDataTextDump(m_address, DataSize.DB, out byteStr);
			AddTextData(byteStr, cmdStr:"Cmd:Wait1Byte", dataStr:d0.ToString("X2"));
			m_address += 1;
			d0 = header.GetDataTextDump(m_address, DataSize.DB, out byteStr);
			AddTextData(byteStr, cmdStr:"Cmd:Wait2Bytes", dataStr:d0.ToString("X2"));
			m_address += 1;
			d0 = header.GetDataTextDump(m_address, DataSize.DB, out byteStr);
			AddTextData(byteStr, cmdStr:"Cmd:1Wait", dataStr:d0.ToString("X2"));
			m_address += 1;
			d0 = header.GetDataTextDump(m_address, DataSize.DB, out byteStr);
			AddTextData(byteStr, cmdStr:"Cmd:2Wait", dataStr:d0.ToString("X2"));
			m_address += 1;
			d0 = header.GetDataTextDump(m_address, DataSize.DB, out byteStr);
			AddTextData(byteStr, cmdStr:"Cmd:3Wait", dataStr:d0.ToString("X2"));
			m_address += 1;
			d0 = header.GetDataTextDump(m_address, DataSize.DB, out byteStr);
			AddTextData(byteStr, cmdStr:"Cmd:4Wait", dataStr:d0.ToString("X2"));
			m_address += 1;
			d0 = header.GetDataTextDump(m_address, DataSize.DB, out byteStr);
			AddTextData(byteStr, cmdStr:"Cmd:5Wait", dataStr:d0.ToString("X2"));
			m_address += 1;
			d0 = header.GetDataTextDump(m_address, DataSize.DB, out byteStr);
			AddTextData(byteStr, cmdStr:"Cmd:6Wait", dataStr:d0.ToString("X2"));
			m_address += 1;
			d0 = header.GetDataTextDump(m_address, DataSize.DB, out byteStr);
			AddTextData(byteStr, cmdStr:"Cmd:WrWait", dataStr:d0.ToString("X2"));
			m_address += 1;
			d0 = header.GetDataTextDump(m_address, DataSize.DB, out byteStr);
			AddTextData(byteStr, cmdStr:"Cmd:WrWaitRL", dataStr:d0.ToString("X2"));
			m_address += 1;
			d0 = header.GetDataTextDump(m_address, DataSize.DB, out byteStr);
			AddTextData(byteStr, cmdStr:"Cmd:WrSeek", dataStr:d0.ToString("X2"));
			m_address += 1;
			d0 = header.GetDataTextDump(m_address, DataSize.DB, out byteStr);
			AddTextData(byteStr, cmdStr:"Cmd:Free0", dataStr:d0.ToString("X2"));
			m_address += 1;
			d0 = header.GetDataTextDump(m_address, DataSize.DB, out byteStr);
			AddTextData(byteStr, cmdStr:"Cmd:Free1", dataStr:d0.ToString("X2"));
			m_address += 1;
			d0 = header.GetDataTextDump(m_address, DataSize.DB, out byteStr);
			AddTextData(byteStr, cmdStr:"Cmd:Free2", dataStr:d0.ToString("X2"));
			m_address += 1;
			d0 = header.GetDataTextDump(m_address, DataSize.DB, out byteStr);
			AddTextData(byteStr, cmdStr:"Cmd:Free3", dataStr:d0.ToString("X2"));
			m_address += 1;
			d0 = header.GetDataTextDump(m_address, DataSize.DB, out byteStr);
			AddTextData(byteStr, cmdStr:"Cmd:Free4", dataStr:d0.ToString("X2"));
			m_address += 1;
			m_textDataList.Add("");
		}

		/// <summary>
		/// F1 Text への追加
		/// </summary>
		private void AddTextData(string byteStr, string cmdStr="", string dataStr="",string waitCountStr="")
		{
			if (cmdStr=="")
			{
				if (waitCountStr=="")
				{
					m_textDataList.Add($"0x{m_address:X8}: {byteStr,-16}");
				}
				else
				{
					m_textDataList.Add($"0x{m_address:X8}: {byteStr,-16}\t{waitCountStr,-16}");
				}
				return;
			}
			else if (dataStr=="")
			{
				if (waitCountStr=="")
				{
					m_textDataList.Add($"0x{m_address:X8}: {byteStr,-16}{cmdStr,-16}");
				}
				else
				{
					m_textDataList.Add($"0x{m_address:X8}: {byteStr,-16}{cmdStr,-16}\t{waitCountStr,-16}");
				}
				return;
			}
			if (waitCountStr == "")
			{
				m_textDataList.Add($"0x{m_address:X8}: {byteStr,-16}{cmdStr,-16}:\t{dataStr}");
			}
			else
			{
				m_textDataList.Add($"0x{m_address:X8}: {byteStr,-16}{cmdStr,-16}:\t{dataStr}\t{waitCountStr,-16}");
			}
		}

		/// <summary>
		///	テキストデータのセット
		/// </summary>
		private void SetTextData(int address, int index, string byteStr, string cmdStr="", string dataStr="")
		{
			if (cmdStr=="")
			{
				m_textDataList[index] = $"0x{address:X8}: {byteStr,-16}";
				return;
			}
			else if (dataStr=="")
			{
				m_textDataList[index] = $"0x{address:X8}: {byteStr,-16}{cmdStr,-16}";
				return;
			}
			m_textDataList[index] = $"0x{address:X8}: {byteStr,-16}{cmdStr,-16}:\t{dataStr}";
		}

	}
}
