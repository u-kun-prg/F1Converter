using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;

namespace F1
{
	///	<summary>
	///	F1 エクスポート クラス
	///	</summary>
	public class F1Export
	{
		///	<summary>
		///	F1 の生成
		///	</summary>
		public bool CreateF1(List<byte> f1DataList, F1Header header, F1ImData imData, uint loopNum, bool isNoOutputPcmBlock)
		{
			var offset = F1Header.F1_HeaderSize;
			var a1s = new int[256];
			var playDataList = new List<byte>();
			var pcmDataList = new List<byte>();
			{
				for(int i = 0, l = a1s.Length; i < l; i++) 
				{
					a1s[i] = 0;
				}
				//	PLAY中間データ
				var is3CS = imData.IsPlayImDataList3CS();
				var is3A1 = imData.IsPlayImDataList3A1();
				int chipSelect = 0;
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
									playDataList.Add(header.GetCmdCodeCS());
								}
								else
								{
									playDataList.Add(header.GetCmdCodeCS());
									playDataList.Add((byte)chipSelect);
								}
							}
							if (a1s[chipSelect] != playImData.m_A1)
							{
								a1s[chipSelect] = playImData.m_A1;
								if (!is3A1)
								{
									playDataList.Add(header.GetCmdCodeA1());
								}
								else
								{
									playDataList.Add(header.GetCmdCodeA1());
									playDataList.Add((byte)a1s[chipSelect]);
								}
							}
							if (imData.TopCodeCSList[chipSelect] < 0x100)
							{
								playDataList.Add((byte)(imData.TopCodeCSList[chipSelect] & 0xFF));
							}
							playDataList.Add(playImData.m_data0);
							playDataList.Add(playImData.m_data1);
							break;
						case F1ImData.PlayImType.ONE_DATA:
							if (chipSelect != playImData.m_chipSelect)
							{
								chipSelect = playImData.m_chipSelect;
								if (!is3CS)
								{
									playDataList.Add(header.GetCmdCodeCS());
								}
								else
								{
									playDataList.Add(header.GetCmdCodeCS());
									playDataList.Add((byte)chipSelect);
								}
							}
							if (a1s[chipSelect] != playImData.m_A1)
							{
								a1s[chipSelect] = playImData.m_A1;
								if (!is3A1)
								{
									playDataList.Add(header.GetCmdCodeA1());
								}
								else
								{
									playDataList.Add(header.GetCmdCodeA1());
									playDataList.Add((byte)a1s[chipSelect]);
								}
							}
							if (imData.TopCodeCSList[chipSelect] < 0x100)
							{
								playDataList.Add((byte)(imData.TopCodeCSList[chipSelect] & 0xFF));
							}
							playDataList.Add(playImData.m_data0);
							break;
						case F1ImData.PlayImType.VSTRM_DATA:
							if (chipSelect != playImData.m_chipSelect)
							{
								chipSelect = playImData.m_chipSelect;
								if (!is3CS)
								{
									playDataList.Add(header.GetCmdCodeCS());
								}
								else
								{
									playDataList.Add(header.GetCmdCodeCS());
									playDataList.Add((byte)chipSelect);
								}
							}
							if (imData.TopCodeCSList[chipSelect] < 0x100)
							{
								playDataList.Add((byte)(imData.TopCodeCSList[chipSelect] & 0xFF));
							}
							playDataList.Add(playImData.m_data0);
							playDataList.Add(playImData.m_data1);
							playDataList.Add(playImData.m_vStrmStepBase);
							playDataList.Add(playImData.m_vStrmStepSize);
							break;
						case F1ImData.PlayImType.VSTRM_SAMPLING_RATE:
							if (chipSelect != playImData.m_chipSelect)
							{
								chipSelect = playImData.m_chipSelect;
								if (!is3CS)
								{
									playDataList.Add(header.GetCmdCodeCS());
								}
								else
								{
									playDataList.Add(header.GetCmdCodeCS());
									playDataList.Add((byte)chipSelect);
								}
							}
							if (imData.TopCodeCSList[chipSelect] < 0x100)
							{
								playDataList.Add((byte)(imData.TopCodeCSList[chipSelect] & 0xFF));
							}
							playDataList.Add(playImData.m_data0);
							playDataList.Add(playImData.m_data1);
							playDataList.Add((byte)(((playImData.m_vStrmSamplingRate & 0xFF000000) >> 24) & 0xFF));
							playDataList.Add((byte)(((playImData.m_vStrmSamplingRate & 0x00FF0000) >> 16) & 0xFF));
							playDataList.Add((byte)(((playImData.m_vStrmSamplingRate & 0x0000FF00) >>  8) & 0xFF));
							playDataList.Add((byte) ((playImData.m_vStrmSamplingRate & 0x000000FF)        & 0xFF));
							break;
						case F1ImData.PlayImType.VSTRM_START_SIZE:
							if (chipSelect != playImData.m_chipSelect)
							{
								chipSelect = playImData.m_chipSelect;
								if (!is3CS)
								{
									playDataList.Add(header.GetCmdCodeCS());
								}
								else
								{
									playDataList.Add(header.GetCmdCodeCS());
									playDataList.Add((byte)chipSelect);
								}
							}
							if (imData.TopCodeCSList[chipSelect] < 0x100)
							{
								playDataList.Add((byte)(imData.TopCodeCSList[chipSelect] & 0xFF));
							}
							playDataList.Add(playImData.m_data0);
							playDataList.Add(playImData.m_data1);
							playDataList.Add(playImData.m_vStrmFlagMode);
							playDataList.Add((byte)(((playImData.m_vStrmStart & 0xFF000000) >> 24) & 0xFF));
							playDataList.Add((byte)(((playImData.m_vStrmStart & 0x00FF0000) >> 16) & 0xFF));
							playDataList.Add((byte)(((playImData.m_vStrmStart & 0x0000FF00) >>  8) & 0xFF));
							playDataList.Add((byte) ((playImData.m_vStrmStart & 0x000000FF)        & 0xFF));
							playDataList.Add((byte)(((playImData.m_vStrmSize  & 0xFF000000) >> 24) & 0xFF));
							playDataList.Add((byte)(((playImData.m_vStrmSize  & 0x00FF0000) >> 16) & 0xFF));
							playDataList.Add((byte)(((playImData.m_vStrmSize  & 0x0000FF00) >>  8) & 0xFF));
							playDataList.Add((byte) ((playImData.m_vStrmSize  & 0x000000FF)        & 0xFF));
							break;
						case F1ImData.PlayImType.VSTRM_STOP_STREAM:
							if (chipSelect != playImData.m_chipSelect)
							{
								chipSelect = playImData.m_chipSelect;
								if (!is3CS)
								{
									playDataList.Add(header.GetCmdCodeCS());
								}
								else
								{
									playDataList.Add(header.GetCmdCodeCS());
									playDataList.Add((byte)chipSelect);
								}
							}
							if (imData.TopCodeCSList[chipSelect] < 0x100)
							{
								playDataList.Add((byte)(imData.TopCodeCSList[chipSelect] & 0xFF));
							}
							playDataList.Add(playImData.m_data0);
							playDataList.Add(playImData.m_data1);
							break;
						case F1ImData.PlayImType.VSTRM_START_SIZE_FAST:
							if (chipSelect != playImData.m_chipSelect)
							{
								chipSelect = playImData.m_chipSelect;
								if (!is3CS)
								{
									playDataList.Add(header.GetCmdCodeCS());
								}
								else
								{
									playDataList.Add(header.GetCmdCodeCS());
									playDataList.Add((byte)chipSelect);
								}
							}
							if (imData.TopCodeCSList[chipSelect] < 0x100)
							{
								playDataList.Add((byte)(imData.TopCodeCSList[chipSelect] & 0xFF));
							}
							playDataList.Add(playImData.m_data0);
							playDataList.Add(playImData.m_data1);
							playDataList.Add((byte)(((playImData.m_vStrmBlockId & 0x0000FF00) >>  8) & 0xFF));
							playDataList.Add((byte) ((playImData.m_vStrmBlockId & 0x000000FF)        & 0xFF));
							playDataList.Add(playImData.m_vStrmFlagMode);
							playDataList.Add((byte)(((playImData.m_vStrmStart & 0xFF000000) >> 24) & 0xFF));
							playDataList.Add((byte)(((playImData.m_vStrmStart & 0x00FF0000) >> 16) & 0xFF));
							playDataList.Add((byte)(((playImData.m_vStrmStart & 0x0000FF00) >>  8) & 0xFF));
							playDataList.Add((byte) ((playImData.m_vStrmStart & 0x000000FF)        & 0xFF));
							playDataList.Add((byte)(((playImData.m_vStrmSize  & 0xFF000000) >> 24) & 0xFF));
							playDataList.Add((byte)(((playImData.m_vStrmSize  & 0x00FF0000) >> 16) & 0xFF));
							playDataList.Add((byte)(((playImData.m_vStrmSize  & 0x0000FF00) >>  8) & 0xFF));
							playDataList.Add((byte) ((playImData.m_vStrmSize  & 0x000000FF)        & 0xFF));
							break;
						case F1ImData.PlayImType.CYCLE_WAIT:
							List<Byte> cycleWaitCodes = CreateCycleWaitCodes(playImData.m_cycleWait, header);
							for (int j = 0, wl = cycleWaitCodes.Count; j < wl; j ++)
							{
								playDataList.Add(cycleWaitCodes[j]);
							}
							break;
						case F1ImData.PlayImType.WRITE_WAIT:
							playDataList.Add(header.GetCmdCodeWriteWait());
							playDataList.Add((byte)(playImData.m_writeWait & 0xFF));
							break;
						case F1ImData.PlayImType.WRITE_WAIT_RL:
							playDataList.Add(header.GetCmdCodeWriteWaitRunLength());
							playDataList.Add((byte)(playImData.m_writeWaitRL & 0xFF));
							playDataList.Add((byte)(playImData.m_writeRunLength & 0xFF));
							break;
						case F1ImData.PlayImType.WRITE_SEEK:
							playDataList.Add(header.GetCmdCodeWriteSeek());
							playDataList.Add((byte)(((playImData.m_seekAddress) >> 24) & 0xFF));
							playDataList.Add((byte)(((playImData.m_seekAddress) >> 16) & 0xFF));
							playDataList.Add((byte)(((playImData.m_seekAddress) >>  8) & 0xFF));
							playDataList.Add((byte)(playImData.m_seekAddress & 0xFF));
							break;
						case F1ImData.PlayImType.END_CODE:
							playDataList.Add(header.GetCmdCodeEnd());
							break;
						case F1ImData.PlayImType.LOOP_POINT:
							playDataList.Add(header.GetCmdCodeLoop());
							break;
						default:
							break;
					}
				}
			}
			offset += playDataList.Count;

			//	PCM中間データ
			if (imData.PcmImDataList.Count != 0)
			{
				header.SetPCMDataOffset((uint)offset);
				uint nextAddress = 0xFFFFFFFF;
				int keepIndex = -1;
				foreach(var pcmImData in imData.PcmImDataList)
				{
					pcmDataList.Add((byte)pcmImData.m_chipSelect);	offset ++;
					pcmDataList.Add((byte)pcmImData.m_pcmDataType);	offset ++;
					pcmDataList.Add((byte)(((pcmImData.m_pcmStart) >> 24) & 0xFF)); offset ++;
					pcmDataList.Add((byte)(((pcmImData.m_pcmStart) >> 16) & 0xFF)); offset ++;
					pcmDataList.Add((byte)(((pcmImData.m_pcmStart) >>  8) & 0xFF)); offset ++;
					pcmDataList.Add((byte)  (pcmImData.m_pcmStart		   & 0xFF)); offset ++;
					pcmDataList.Add((byte)(((pcmImData.m_pcmSize)  >> 24) & 0xFF)); offset ++;
					pcmDataList.Add((byte)(((pcmImData.m_pcmSize)  >> 16) & 0xFF)); offset ++;
					pcmDataList.Add((byte)(((pcmImData.m_pcmSize)  >>  8) & 0xFF)); offset ++;
					pcmDataList.Add((byte)  (pcmImData.m_pcmSize		   & 0xFF)); offset ++;
					if (keepIndex >=0)
					{
						pcmDataList[keepIndex] = (byte)((nextAddress >> 24) & 0xFF);
						pcmDataList[keepIndex+1] = (byte)((nextAddress >> 16) & 0xFF);
						pcmDataList[keepIndex+2] = (byte)((nextAddress >>  8) & 0xFF);
						pcmDataList[keepIndex+3] = (byte)(nextAddress & 0xFF);
					}
					keepIndex = pcmDataList.Count;
					pcmDataList.Add(0xFF); offset ++;
					pcmDataList.Add(0xFF); offset ++;
					pcmDataList.Add(0xFF); offset ++;
					pcmDataList.Add(0xFF); offset ++;
					for (int j = 0, lp = pcmImData.m_pcmBinaryArray.Length; j < lp; j++)
					{
						pcmDataList.Add(pcmImData.m_pcmBinaryArray[j]);
						offset ++;
					}
					nextAddress = (uint)offset;
				}
			}

			header.WriteHeader(f1DataList);
			for (int i = 0, l = playDataList.Count; i < l; i ++)
			{
				f1DataList.Add(playDataList[i]);
			}
			if (!isNoOutputPcmBlock)
			{
				if (pcmDataList.Count != 0)
				{
					for (int i = 0, l = pcmDataList.Count; i < l; i ++)
					{
						f1DataList.Add(pcmDataList[i]);
					}
				}
			}
			return true;
		}

		///	<summary>
		///	サイクル WAIT のバイナリコードを生成する
		///	</summary>
		private List<byte> CreateCycleWaitCodes(int cycleWait, F1Header header)
		{
			var resList = new List<byte>();
			if (cycleWait <= 6)
			{
				resList.Add(header.GetCmdCodeCycleNWait(cycleWait));
				return resList;
			}
			if (cycleWait < 0x100)
			{
				resList.Add(header.GetCmdCodeCycleWaitByte());
				resList.Add((byte)(cycleWait));
				return resList;
			}
			int div = cycleWait / 0xFFFF;
			int mod = cycleWait % 0xFFFF;
			div += 1;
			while(div > 0)
			{
				div -= 1;
				if (div != 0)
				{
					resList.Add(header.GetCmdCodeCycleWaitWord());
					resList.Add((byte)0xFF);
					resList.Add((byte)0xFF);
				}
				else
				{
					if (mod > 0x100)
					{
						resList.Add(header.GetCmdCodeCycleWaitWord());
						resList.Add((byte)((mod >> 8) & 0xFF));
						resList.Add((byte)(mod & 0xFF));
					}
					else
					{
						if (mod > 6)
						{
							resList.Add(header.GetCmdCodeCycleWaitByte());
							resList.Add((byte)mod);
						}
						else
						{
							resList.Add(header.GetCmdCodeCycleNWait(mod));
						}
					}
				}
			}
			return resList;
		}

	}
}
