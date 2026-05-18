using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;

namespace F1
{
	///	<summary>
	///	S98 エクスポート クラス
	///	</summary>
	public class F1ExportS98
	{
		private List<byte> m_s98DataList;
		private List<int> m_csDataList = new List<int>();

		/// <summary>
		///	S98 の生成
		/// </summary>
		public bool CreateS98(List<byte> s98DataList, F1TargetHardware targetHard, F1Header header, F1ImData imData)
		{
			m_s98DataList = s98DataList;
			m_s98DataList.Clear();

			CreateS98Header(header);
			if (!CreateS98DeviceInfo(targetHard))
			{
				return false;
			}
			if (!CreateS98Dump(imData))
			{
				return false;
			}
			return true;
		}

		/// <summary>
		//	S98 Dump の生成
		/// </summary>
		private bool CreateS98Dump(F1ImData imData)
		{
			bool isLoop = false;
			foreach(var playImData in imData.PlayImDataList)
			{
				if (playImData.m_imType == F1ImData.PlayImType.TWO_DATA)
				{
					if (playImData.m_A1 <= 1)
					{
						foreach(var cs in m_csDataList)
						{
							if (cs == playImData.m_chipSelect)
							{
								var cs0 = (playImData.m_chipSelect * 2) + playImData.m_A1;
								WriteAddData(DataSize.DB, (uint)cs0);
								WriteAddData(DataSize.DB, (uint)playImData.m_data0);
								WriteAddData(DataSize.DB, (uint)playImData.m_data1);
								break;
							}
						}
					}
				}
				else if  (playImData.m_imType == F1ImData.PlayImType.ONE_DATA)
				{
					if (playImData.m_A1 <= 1)
					{
						foreach(var cs in m_csDataList)
						{
							if (cs == playImData.m_chipSelect)
							{
								var cs0 = (playImData.m_chipSelect * 2) + playImData.m_A1;
								WriteAddData(DataSize.DB, (uint)cs0);
								WriteAddData(DataSize.DB, (uint)0x00);
								WriteAddData(DataSize.DB, (uint)playImData.m_data0);
								break;
							}
						}
					}
				}
				else if  (playImData.m_imType == F1ImData.PlayImType.CYCLE_WAIT)
				{
					var wait = playImData.m_cycleWait;
					if (wait == 1)
					{
						WriteAddData(DataSize.DB, (uint)0xFF);
					}
					else
					{
						wait -= 2;
						WriteAddData(DataSize.DB, (uint)0xFE);
						while(wait >=0x80)
						{
							WriteAddData(DataSize.DB, (uint)((wait & 0x7F) | 0x80) );
							wait = wait >> 7;
						}
						WriteAddData(DataSize.DB, (uint)wait);
					}
				}
				else if (playImData.m_imType == F1ImData.PlayImType.END_CODE)
				{
					WriteAddData(DataSize.DB, 0xFD);
					return true;
				}
				else if (playImData.m_imType == F1ImData.PlayImType.LOOP_POINT)
				{
					if (!isLoop)
					{
						isLoop = true;
						WriteData(0x18, DataSize.DL, (uint)(m_s98DataList.Count()));
					}
				}
				else
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>
		//	S98 DeviceInfo の生成
		/// </summary>
		private bool CreateS98DeviceInfo(F1TargetHardware targetHardware)
		{
			uint deviceCtr = 0;
			foreach(var targetChip in targetHardware.TargetChipList.Where(x => x.TargetActiveStatus == ActiveStatus.ACTIVE)) 
			{
				uint deviceType = 0;
				switch(targetChip.TargetChipType)
				{
					case ChipType.YM2149: deviceType = 1; break;
					case ChipType.YM2203: deviceType = 2; break;
					case ChipType.YM2612: deviceType = 3; break;
					case ChipType.YM2608: deviceType = 4; break;
					case ChipType.YM2151: deviceType = 5; break;
					case ChipType.YM2413: deviceType = 6; break;
					case ChipType.YM3526: deviceType = 7; break;
					case ChipType.YM3812: deviceType = 8; break;
					case ChipType.YMF262: deviceType = 9; break;
					case ChipType.AY_3_8910: deviceType = 15; break;
					case ChipType.SN76489: deviceType = 16; break;
					default:
						break;
				}
				if (deviceType != 0)
				{
					m_csDataList.Add(targetChip.ChipSelect);
					WriteAddData(DataSize.DL, deviceType);
					WriteAddData(DataSize.DL, (uint)targetChip.TargetChipClock);
					WriteAddData(DataSize.DL, 0);
					WriteAddData(DataSize.DL, 0);
					deviceCtr += 1;
				}
			}
			if (deviceCtr == 0)
			{
				return false;
			}
			WriteData(0x1C, DataSize.DL, deviceCtr);
			WriteData(0x14, DataSize.DL, (uint)(m_s98DataList.Count));
			return true;
		}

		/// <summary>
		///	S98 ヘッダーの生成
		/// </summary>
		private void CreateS98Header(F1Header header)
		{
			m_s98DataList.Add((byte)0x53);		//	0x00	's'	MAGIC
			m_s98DataList.Add((byte)0x39);		//	0x01	'9'
			m_s98DataList.Add((byte)0x38);		//	0x02	'8'	FORMAT VERSION
			m_s98DataList.Add((byte)0x33);		//	0x03	'3'
			for (int i = 0; i < 4; i++) m_s98DataList.Add((byte)0x00);	//	0x04
			for (int i = 0; i < 4; i++) m_s98DataList.Add((byte)0x00);	//	0x08
			for (int i = 0; i < 4; i++) m_s98DataList.Add((byte)0x00);	//	0x0C
			for (int i = 0; i < 4; i++) m_s98DataList.Add((byte)0x00);	//	0x10
			for (int i = 0; i < 4; i++) m_s98DataList.Add((byte)0x00);	//	0x14
			for (int i = 0; i < 4; i++) m_s98DataList.Add((byte)0x00);	//	0x18
			for (int i = 0; i < 4; i++) m_s98DataList.Add((byte)0x00);	//	0x1C
			var oneCycleNs = header.GetOneCycleNs();
			WriteData(0x04, DataSize.DL, oneCycleNs);
			WriteData(0x08, DataSize.DL, (uint)1000000000);
		}

		/// <summary>
		//	S98 バイナリデータの書き込み
		/// </summary>
		private void WriteData(int index, DataSize dataSize, uint data)
		{
			switch(dataSize)
			{
				case DataSize.DB:
					m_s98DataList[index] = (byte)(data & 0xFF);
					break;
				case DataSize.DW:
					m_s98DataList[index+0] = (byte)(data & 0xFF);
					m_s98DataList[index+1] = (byte)((data >> 8) & 0xFF);
					break;
				case DataSize.DL:
					m_s98DataList[index+0] = (byte)(data & 0xFF);
					m_s98DataList[index+1] = (byte)((data >>  8) & 0xFF);
					m_s98DataList[index+2] = (byte)((data >> 16) & 0xFF);
					m_s98DataList[index+3] = (byte)((data >> 24) & 0xFF);
					break;
			}
		}

		/// <summary>
		///	S98 バイナリデータの追加書き込み
		/// </summary>
		private void WriteAddData(DataSize dataSize, uint data)
		{
			switch(dataSize)
			{
				case DataSize.DB:
					m_s98DataList.Add((byte)(data & 0xFF));
					break;
				case DataSize.DW:
					m_s98DataList.Add((byte)(data & 0xFF));
					m_s98DataList.Add((byte)((data >> 8) & 0xFF));
					break;
				case DataSize.DL:
					m_s98DataList.Add((byte)(data & 0xFF));
					m_s98DataList.Add((byte)((data >>  8) & 0xFF));
					m_s98DataList.Add((byte)((data >> 16) & 0xFF));
					m_s98DataList.Add((byte)((data >> 24) & 0xFF));
					break;
			}
		}

	}
}
