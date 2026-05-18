using System;
using System.Collections.Generic;
using System.Linq;

namespace F1
{
	/// <summary>
	///	S98 フォーマット パーサー クラス
	/// </summary>
	public class S98Parser : Parser
	{
		/// <summary>
		///	S98 フォーマット パース
		/// </summary>
		public override bool Parse()
		{
			int source_address = 0;
			int loopPoint_address = 0;
			uint tmp_d0 = 0;
			uint tmp_d1 = 0;
			uint tmp_d2 = 0;
			uint device_ctr = 0;

			//	ヘッダー	サンプル時間
			if (!GetSourceData(0x0004, DataSize.DL, false, out tmp_d0)) return false;
			if (!GetSourceData(0x0008, DataSize.DL, false, out tmp_d1)) return false;
			if (tmp_d0 == 0) tmp_d0 = 10;
			if (tmp_d1 == 0) tmp_d1 = 1000;
			Header.SetOneCycleNs((uint)( ((float)tmp_d0) / ((float)tmp_d1) * 1000000000f));

			//	デバイスヘッダーを解析
			if (!GetSourceData(0x1C, DataSize.DL, false, out device_ctr)) return false;

			if (device_ctr == 0)
			{	//	デバイスがない場合、パース CHIP は、YM2608 7.9872Mhz １つ
				var parseChip = new ParseChip(ChipType.YM2608, 7987200, 0, 1);
				m_parseChipList.Add(parseChip);
				device_ctr = 1;
			}
			else
			{	//	デバイスがある場合、パース CHIP は、デバイスから生成する
				source_address = 0x20;
				for (int i = 0; i < (int)device_ctr; i ++)
				{
					if (!GetSourceData(source_address,   DataSize.DL, false, out tmp_d0)) return false;
					if (!GetSourceData(source_address+4, DataSize.DL, false, out tmp_d1)) return false;
					ChipType chipType = ChipType.NONE;
					switch(tmp_d0)
					{
						case 0x01: chipType = ChipType.YM2149; break;
						case 0x02: chipType = ChipType.YM2203; break;
						case 0x03: chipType = ChipType.YM2612; break;
						case 0x04: chipType = ChipType.YM2608; break;
						case 0x05: chipType = ChipType.YM2151; break;
						case 0x06: chipType = ChipType.YM2413; break;
						case 0x07: chipType = ChipType.YM3526; break;
						case 0x08: chipType = ChipType.YM3812; break;
						case 0x09: chipType = ChipType.YMF262; break;
						case 0x0F: chipType = ChipType.AY_3_8910; break;
						case 0x10: chipType = ChipType.SN76489; break;
						default: break;
					}
					if (chipType != ChipType.NONE)
					{
						var addParseChip = new ParseChip(chipType, (int)tmp_d1, (uint)(i * 2), (uint)((i * 2)+1));
						var count = m_parseChipList.Count;
						for (int ci = 0; ci < count; ci ++)
						{	//	CHIP タイプとクロックが同じ２つのパース CHIP は、デュアルとする
							var extParseChip = m_parseChipList[ci];
							if (extParseChip.ChipDualNumber < 0 && extParseChip.ChipType == addParseChip.ChipType && extParseChip.ChipClock == addParseChip.ChipClock)
							{
								extParseChip.SetChipDualNumber(DualNumber.Dual1st);
								addParseChip.SetChipDualNumber(DualNumber.Dual2nd);
								extParseChip.SetChipDualIndex(count);
							}
						}
						m_parseChipList.Add(addParseChip);
					}
					source_address += 0x10;
				}
			}
			if (m_parseChipList.Count == 0)
			{	//	未対応の S98 ファイル
				SetNoSupportedMessage();
				return false;
			}
			//	パース CHIP をターゲット CHIP に反映
			ReflectParseChipToTargetChip();
			if (!TargetHardware.IsActiveTarget())
			{
				SetNoSupportedMessage();
				return false;
			}

			//	データブロック
			if (!GetSourceData( 0x14, DataSize.DL, false, out tmp_d0)) return false;
			source_address = (int)tmp_d0;

			//	ループポイント
			if (!GetSourceData( 0x18, DataSize.DL, false, out tmp_d0)) return false;
			loopPoint_address = (int)tmp_d0;

			bool is_data_end = false;
			while(!is_data_end)
			{
				if (!GetSourceData(source_address, DataSize.DB, false, out tmp_d0))
				{
					ErrorString = null;
					AddWarningString("WARNING : S98 End mark not found.");
					AddEndCodeToPlayImData();
					is_data_end = true;
					continue;
				}
				if (source_address == loopPoint_address)
				{
					AddLoopPointToPlayImData();
				}
				source_address += 1;
				switch(tmp_d0)
				{
					case 0xFF:	//	s98 1Sync
						{
							AddCycleWaitToPlayImData(1);
						}
						break;
					case 0xFE:	//	s98	nSync
						{
							int fe_ctr = 0;
							uint fe_wait_count = 0;
							int	fe_shift = 0;
							bool fe_ext = true;
							while(fe_ext)
							{
								if (!GetSourceData(source_address, DataSize.DB, false, out tmp_d0))
								{
									return false;
								}
								source_address += 1;
								fe_ctr ++;
								if (((tmp_d0 & 0x80)==0) || (fe_ctr == 4)) fe_ext = false;
								tmp_d0 &= 0x7F;
								tmp_d0 = tmp_d0 << fe_shift;
								fe_wait_count |= tmp_d0;
								fe_shift += 7;
							}
							fe_wait_count += 2;
							AddCycleWaitToPlayImData((int)fe_wait_count);
						}
						break;
					case 0xFD:	//	End/Loop
						{
							AddEndCodeToPlayImData();
							is_data_end = true;
						}
						break;
					default:
						{
							source_address += 2;
							foreach(var parseChip in m_parseChipList.Where(x => x.ChipActiveStatus == ActiveStatus.ACTIVE))
							{
								if (tmp_d0 == parseChip.ChipIdCode0 || tmp_d0 == parseChip.ChipIdCode1)
								{
									int a1 = (tmp_d0 == parseChip.ChipIdCode0) ? 0 : 1;
									if (!GetSourceData(source_address-2, DataSize.DB, false, out tmp_d1)) return false;
									if (!GetSourceData(source_address-1, DataSize.DB, false, out tmp_d2)) return false;
									if (parseChip.ChipType == ChipType.SN76489)
									{
										AddOneDataToPlayImData(parseChip.ChipSelect, a1:a1, data0:(byte)tmp_d2);
									}
									else
									{
										AddTwoDataToPlayImData(parseChip.ChipSelect, a1:a1, data0:(byte)tmp_d1, data1:(byte)tmp_d2);
									}
								}
							}
						}
						break;
					}
			}
			CreateResultMessage();
			return true;
		}
	}
}
