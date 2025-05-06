using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace F1
{
	/// <summary>
	///	CHIP OPL クラス
	/// </summary>
	public class Chip_OPL : SoundChip
	{
		protected int OPLs_OPERATOR { get; } = 2;
		protected int OPL2_FM_CH { get; } = 9;
		protected int OPL2_SSG_CH { get; } = 0;
		protected int OPL3_OPERATOR { get; } = 4;
		protected int OPL3_FM_CH { get; } = 18;
		protected int OPL3_SSG_CH { get; } = 0;
		protected int OPLL_FM_CH { get; } = 9;
		protected int OPLL_SSG_CH { get; } = 0;

		private ConvertWordToneData[] m_fmToneCvDataArray;

		/// <summary>
		/// レジスタ操作にプレイ CHIP にあわせた制御を入れる
		/// </summary>
		public override void ControlPlayChipRegiser()
		{
			bool isNewBit = false;
			foreach(var playImData in m_imData.PlayImDataList.Where(x => x.m_chipSelect == m_targetChip.ChipSelect && x.m_imType == F1ImData.PlayImType.TWO_DATA))
			{
				switch(m_targetChip.TargetChipType)
				{
					case ChipType.YM3526:
						switch(m_targetChip.SourceChipType)
						{
							case ChipType.YM3526:
								break;
							case ChipType.YM3812:
								if (!CheckRegister_YM3812(playImData)) continue;
								break;
							case ChipType.Y8950:
								if (!CheckRegister_Y8950(playImData)) continue;
								if (playImData.m_data0 == 0x08)
								{
									playImData.m_data1 &= 0xC0;
								}
								break;
							default:
								playImData.m_imType = F1ImData.PlayImType.NONE;
								continue;
						}
						if (!ControlPlayChipReg_CheckTimer(playImData)) continue;
						if (!CheckRegister_YM3526(playImData)) continue;
						break;

					case ChipType.YM3812:
						switch(m_targetChip.SourceChipType)
						{
							case ChipType.YM3526:
								if (!CheckRegister_YM3526(playImData)) continue;
								break;
							case ChipType.YM3812:
								break;
							case ChipType.Y8950:
								if (!CheckRegister_Y8950(playImData)) continue;
								if (playImData.m_data0 == 0x08)
								{
									playImData.m_data1 &= 0xC0;
								}
								break;
							default:
								playImData.m_imType = F1ImData.PlayImType.NONE;
								continue;
						}
						if (!ControlPlayChipReg_CheckTimer(playImData)) continue;
						if (!CheckRegister_YM3812(playImData)) continue;
						break;

					case ChipType.YM2413:
						if (!CheckRegister_YM2413(playImData)) continue;
						break;

					case ChipType.YMF262:
						switch(m_targetChip.SourceChipType)
						{
							case ChipType.YM3526:
								if (!CheckRegister_YM3526(playImData)) continue;
								break;
							case ChipType.YM3812:
								if (!CheckRegister_YM3812(playImData)) continue;
								break;
							case ChipType.Y8950:
								if (!CheckRegister_Y8950(playImData)) continue;
								if (playImData.m_data0 == 0x08)
								{
									playImData.m_data1 &= 0xC0;
								}
								break;
							case ChipType.YMF262:
								if (playImData.m_data0 == 0x05)
								{
									isNewBit = ((((int)playImData.m_data1) & 0x01) != 0) ? true : false;
								}
								else
								{
									if (!isNewBit && playImData.m_A1 == 1) 
									{
										playImData.m_imType = F1ImData.PlayImType.NONE;
										continue;
									}
								}
								break;
							default:
								playImData.m_imType = F1ImData.PlayImType.NONE;
								continue;
						}
						if (!ControlPlayChipReg_CheckTimer(playImData)) continue;
						if (!CheckRegister_YMF262(playImData)) continue;
						break;
				}
			}
			m_imData.CleanupPlayImDataList();
		}
		/// <summary>
		/// レジスタ操作にプレイ CHIP にあわせた制御を入れる	タイマレジスタの制御
		/// </summary>
		private bool ControlPlayChipReg_CheckTimer(F1ImData.PlayImData playImData)
		{
			if (m_targetChip.TargetChipType != ChipType.YM2413)
			{
				if (!m_imData.IsTimerReg)
				{
					if (playImData.m_A1 == 0)
					{
						if (playImData.m_data0 == 0x02 || playImData.m_data0 == 0x03 || playImData.m_data0 == 0x04) 
						{	//	TimerA.TimerB
							playImData.m_imType = F1ImData.PlayImType.NONE;
							return false;
						}
					}
				}
			}
			return true;
		}

		/// <summary>
		/// 重複するレジスタ操作の撤去などでサイズを抑える
		/// </summary>
		public override void ShrinkPlayChipRegiser()
		{
			if (m_imData.IsShrink)
			{
				Dictionary<byte, byte> SameReg = new Dictionary<byte, byte>();
				foreach(var playImData in m_imData.PlayImDataList.Where(x => x.m_chipSelect == m_targetChip.ChipSelect && x.m_imType == F1ImData.PlayImType.TWO_DATA))
				{
					bool sameRegFlag = false;
					if (m_targetChip.TargetChipType == ChipType.YM2413)
					{
						sameRegFlag = (playImData.m_data0 >= 0x00 && playImData.m_data0 <= 0x38);			//	All Register.
					}
					else
					{
						sameRegFlag =((playImData.m_data0 >= 0x40 && playImData.m_data0 <= 0x55) ||		//	KSL/TL
									  (playImData.m_data0 >= 0xA0 && playImData.m_data0 <= 0xA8) ||		//	F-Fumber
									  (playImData.m_data0 >= 0xB0 && playImData.m_data0 <= 0xB8));		//	TL
					}
					if (sameRegFlag)
					{
						if (SameReg.ContainsKey(playImData.m_data0))
						{
							if (SameReg[playImData.m_data0] == playImData.m_data1)
							{
								playImData.m_imType = F1ImData.PlayImType.NONE;
							}
							else
							{
								SameReg[playImData.m_data0] = playImData.m_data1;
							}
						}
					}
					else
					{
						SameReg.Add(playImData.m_data0, playImData.m_data1);
					}
				}
			}
			m_imData.CleanupPlayImDataList();
		}

		/// <summary>
		/// ボリュームと音程変換の初期化
		/// </summary>
		protected override void InitializeForToneAndVolume()
		{
			switch(m_targetChip.TargetChipType)
			{
				case ChipType.YM3526:
				case ChipType.YM3812:
					m_fmOperatorNum = OPLs_OPERATOR;
					m_fmCannelNum = OPL2_FM_CH;
					m_ssgCannelNum = OPL2_SSG_CH;
					break;
				case ChipType.YMF262:
					m_fmOperatorNum = OPL3_OPERATOR;
					m_fmCannelNum = OPL3_FM_CH;
					m_ssgCannelNum = OPL3_SSG_CH;
					break;
				case ChipType.YM2413:
					m_fmOperatorNum = OPLs_OPERATOR;
					m_fmCannelNum = OPLL_FM_CH;
					m_ssgCannelNum = OPLL_SSG_CH;
					break;
				default:
					return;
			}
			if (m_imData.IsToneAdjust && CheckAdjustClock())
			{
				if (m_fmCannelNum != 0)
				{
					m_fmToneCvDataArray = new ConvertWordToneData[m_fmCannelNum];
					for (int i = 0; i < m_fmCannelNum; i++) m_fmToneCvDataArray[i] = new ConvertWordToneData();
				}
			}
		}

		/// <summary>
		/// レジスタ操作にクロックにあわせた音程制御を入れる
		/// </summary>
		public override void ToneConvert()
		{
			if (m_imData.IsToneAdjust && CheckAdjustClock())
			{
				int channel = 0;
				byte fnumRegLimit;
				byte blkRegLimit;
				byte fnumRegBase = (m_targetChip.TargetChipType == ChipType.YM2413) ? (byte)0x10 : (byte)0xA0;
				byte blkRegBase = (m_targetChip.TargetChipType == ChipType.YM2413) ? (byte)0x20 : (byte)0xB0;
				byte rhythmReg = (m_targetChip.TargetChipType == ChipType.YM2413) ? (byte)0x0E : (byte)0xBD;
				byte keyOnBit = (m_targetChip.TargetChipType == ChipType.YM2413) ? (byte)0x10 : (byte)0x20;
				bool isRhythm = false;
				for (int playImDataIndex = 0, l = m_imData.PlayImDataList.Count; playImDataIndex < l; playImDataIndex++)
				{
					var playImData = m_imData.PlayImDataList[playImDataIndex];
					if (playImData.m_chipSelect == m_targetChip.ChipSelect && playImData.m_imType == F1ImData.PlayImType.TWO_DATA)
					{
						byte reg = playImData.m_data0;
						byte data = playImData.m_data1;
						if (reg == rhythmReg)
						{
							isRhythm = ((((int)data) & 0x20) != 0) ? true : false;
						}
						fnumRegLimit = (isRhythm && playImData.m_A1 == 0) ? (byte)(fnumRegBase+5) : (byte)(fnumRegBase+8);
						blkRegLimit = (isRhythm && playImData.m_A1 == 0) ? (byte)(blkRegBase+5) : (byte)(blkRegBase+8);
						if (reg >= fnumRegBase && reg <= fnumRegLimit)
						{
							byte fnum = 0;
							byte blk = 0;
							channel = (int)(reg - fnumRegBase);
							if (playImData.m_A1 == 1) channel += 9;
							if (m_fmToneCvDataArray[channel].m_lowPlayImDataIndex >= 0)
							{	//	Block を変更せずに、Fnum の変更音程制御している場合の対応
								//	Fnumが連続した場合に Block 以前のブロックで再計算する
								OPLs_FM_ToneConvert(m_fmToneCvDataArray[channel], out fnum, out blk);
								m_imData.PlayImDataList[m_fmToneCvDataArray[channel].m_lowPlayImDataIndex].m_data1 = fnum;
								if (m_fmToneCvDataArray[channel].m_hiCovertedData != blk)
								{
									m_imData.SetInsertPlayImData(	m_fmToneCvDataArray[channel].m_lowPlayImDataIndex+1, 
																	F1ImData.PlayImType.TWO_DATA, 
																	chipSelect:m_targetChip.ChipSelect, 
																	a1:playImData.m_A1, 
																	data0:(byte)(reg+0x10), 
																	data1:blk);
									m_fmToneCvDataArray[channel].m_hiCovertedData = blk;
								}
							}
							m_fmToneCvDataArray[channel].m_lowData = (int)data;
							m_fmToneCvDataArray[channel].m_lowPlayImDataIndex = playImDataIndex;
						}
						else if (reg >= blkRegBase && reg <= blkRegLimit)
						{
							byte fnum = 0;
							byte blk = 0;
							if ((data & keyOnBit) != 0)
							{
								channel = (int)(reg - blkRegBase);
								if (playImData.m_A1 == 1) channel += 9;
								m_fmToneCvDataArray[channel].m_hiData = (int)data;
								OPLs_FM_ToneConvert(m_fmToneCvDataArray[channel], out fnum, out blk);
								m_fmToneCvDataArray[channel].m_hiCovertedData = blk;
								if (m_fmToneCvDataArray[channel].m_lowPlayImDataIndex >= 0)
								{
									m_imData.PlayImDataList[m_fmToneCvDataArray[channel].m_lowPlayImDataIndex].m_data1 = fnum;
									m_fmToneCvDataArray[channel].m_lowPlayImDataIndex = -1;
								}
								playImData.m_data1 = blk;
							}
						}
					}
				}
				for (int ch = 0; ch < m_fmCannelNum; ch++)
				{
					if (m_fmToneCvDataArray[ch].m_lowPlayImDataIndex >= 0)
					{
						byte lo = 0;
						byte hi = 0;
						OPLs_FM_ToneConvert(m_fmToneCvDataArray[ch], out lo, out hi);
						m_imData.PlayImDataList[m_fmToneCvDataArray[ch].m_lowPlayImDataIndex].m_data1 = lo;
					}
				}
				m_imData.InsertPlayImDataList();
				m_imData.CleanupPlayImDataList();
			}
		}

		/// <summary>
		/// レジスタ操作に音量設定にあわせた音量制御を入れる
		/// </summary>
		public override void VolumeConvert()
		{
			if (m_imData.FMVol != 0)
			{
				bool isRhythm = false;
				bool isNewBit = false;
				int connectSelBits = 0;
				byte rhythmReg = (m_targetChip.TargetChipType == ChipType.YM2413) ? (byte)0x0E : (byte)0xBD;
				for (int playImDataIndex = 0, l = m_imData.PlayImDataList.Count; playImDataIndex < l; playImDataIndex++)
				{
					var playImData = m_imData.PlayImDataList[playImDataIndex];
					if (playImData.m_chipSelect == m_targetChip.ChipSelect && playImData.m_imType == F1ImData.PlayImType.TWO_DATA)
					{
						if (playImData.m_data0 == rhythmReg)
						{
							isRhythm = ((((int)playImData.m_data1) & 0x20) != 0) ? true : false;
						}
						switch(m_targetChip.TargetChipType)
						{
							case ChipType.YM3812:
								OPL2_VolumeRegControl(playImDataIndex, isRhythm);
								break;
							case ChipType.YM2413:
								OPLL_VolumeRegControl(playImDataIndex, isRhythm);
								break;
							case ChipType.YMF262:
								if (playImData.m_data0 == 0x05 && playImData.m_A1 == 1)
								{
									isNewBit = ((((int)playImData.m_data1) & 0x01) != 0) ? true : false;
								}
								if (!isNewBit)
								{
									OPL2_VolumeRegControl(playImDataIndex, isRhythm);
								}
								else
								{
									if (playImData.m_A1 == 1 && playImData.m_data0 == 0x04)
									{
										connectSelBits = (int)(playImData.m_data1 & 0x3F);
									}
									OPL3_VolumeRegControl(playImData, playImDataIndex, connectSelBits, isRhythm);
								}
								break;
						}
					}
				}
				m_imData.CleanupPlayImDataList();
			}
		}
		/// <summary>
		/// OPLL ボリュームレジスタ制御
		/// </summary>
		private void OPLL_VolumeRegControl(int playImDataIndex, bool isRhythm)
		{
			var playImData = m_imData.PlayImDataList[playImDataIndex];
			byte regLimit = (!isRhythm) ? (byte)0x38 : (byte)0x35;

			if (playImData.m_data0 >= 0x30 && playImData.m_data0 <= regLimit)
			{
				int tl = 0x0F - ((int)(playImData.m_data1) & 0x0F);
				if (tl != 0)
				{
					tl = (int)((float)tl + (15f * ((float)(m_imData.FMVol) / 100f)));
					tl = (tl < 0x00) ? 0x00 : ((tl > 0x0F) ? 0x0F : tl);
				}
				tl = 0x0F - tl;
				playImData.m_data1 &= 0xF0;
				playImData.m_data1 |= (byte)tl;
			}
		}

		/// <summary>
		/// OPL2 ボリュームレジスタ制御
		/// </summary>
		private void OPL2_VolumeRegControl(int playImDataIndex, bool isRhythm)
		{
			var playImData = m_imData.PlayImDataList[playImDataIndex];
			if (playImData.m_A1 == 0)
			{
				if (playImData.m_data0 == 0x40) { m_fmTotalLevel[0, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[0, 0] = playImDataIndex; }
				if (playImData.m_data0 == 0x43) { m_fmTotalLevel[0, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[0, 1] = playImDataIndex; }
				if (playImData.m_data0 == 0xC0) { m_fmOpeConnect[0] = (int)(playImData.m_data1 & 0x01); }
				if (playImData.m_data0 == 0xB0) TotalLevelConvert(0, false);
				if (playImData.m_data0 == 0x41) { m_fmTotalLevel[1, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[1, 0] = playImDataIndex; }
				if (playImData.m_data0 == 0x44) { m_fmTotalLevel[1, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[1, 1] = playImDataIndex; }
				if (playImData.m_data0 == 0xC1) { m_fmOpeConnect[1] = (int)(playImData.m_data1 & 0x01); }
				if (playImData.m_data0 == 0xB1) TotalLevelConvert(1, false);
				if (playImData.m_data0 == 0x42) { m_fmTotalLevel[2, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[2, 0] = playImDataIndex; }
				if (playImData.m_data0 == 0x45) { m_fmTotalLevel[2, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[2, 1] = playImDataIndex; }
				if (playImData.m_data0 == 0xC2) { m_fmOpeConnect[2] = (int)(playImData.m_data1 & 0x01); }
				if (playImData.m_data0 == 0xB2) TotalLevelConvert(2, false);
				if (playImData.m_data0 == 0x48) { m_fmTotalLevel[3, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[3, 0] = playImDataIndex; }
				if (playImData.m_data0 == 0x4B) { m_fmTotalLevel[3, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[3, 1] = playImDataIndex; }
				if (playImData.m_data0 == 0xC3) { m_fmOpeConnect[3] = (int)(playImData.m_data1 & 0x01); }
				if (playImData.m_data0 == 0xB3) TotalLevelConvert(3, false);
				if (playImData.m_data0 == 0x49) { m_fmTotalLevel[4, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[4, 0] = playImDataIndex; }
				if (playImData.m_data0 == 0x4C) { m_fmTotalLevel[4, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[4, 1] = playImDataIndex; }
				if (playImData.m_data0 == 0xC3) { m_fmOpeConnect[4] = (int)(playImData.m_data1 & 0x01); }
				if (playImData.m_data0 == 0xB3) TotalLevelConvert(4, false);
				if (playImData.m_data0 == 0x4A) { m_fmTotalLevel[5, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[5, 0] = playImDataIndex; }
				if (playImData.m_data0 == 0x4D) { m_fmTotalLevel[5, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[5, 1] = playImDataIndex; }
				if (playImData.m_data0 == 0xC5) { m_fmOpeConnect[5] = (int)(playImData.m_data1 & 0x01); }
				if (playImData.m_data0 == 0xB5) TotalLevelConvert(5, false);
				if (!isRhythm && playImData.m_data0 == 0x50) { m_fmTotalLevel[6, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[6, 0] = playImDataIndex; }
				if (!isRhythm && playImData.m_data0 == 0x53) { m_fmTotalLevel[6, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[6, 1] = playImDataIndex; }
				if (!isRhythm && playImData.m_data0 == 0xC6) { m_fmOpeConnect[6] = (int)(playImData.m_data1 & 0x01); }
				if (!isRhythm && playImData.m_data0 == 0xB6) TotalLevelConvert(6, false);
				if (!isRhythm && playImData.m_data0 == 0x51) { m_fmTotalLevel[7, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[7, 0] = playImDataIndex; }
				if (!isRhythm && playImData.m_data0 == 0x54) { m_fmTotalLevel[7, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[7, 1] = playImDataIndex; }
				if (!isRhythm && playImData.m_data0 == 0xC7) { m_fmOpeConnect[7] = (int)(playImData.m_data1 & 0x01); }
				if (!isRhythm && playImData.m_data0 == 0xB7) TotalLevelConvert(7, false);
				if (!isRhythm && playImData.m_data0 == 0x52) { m_fmTotalLevel[8, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[8, 0] = playImDataIndex; }
				if (!isRhythm && playImData.m_data0 == 0x55) { m_fmTotalLevel[8, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[8, 1] = playImDataIndex; }
				if (!isRhythm && playImData.m_data0 == 0xC8) { m_fmOpeConnect[8] = (int)(playImData.m_data1 & 0x01); }
				if (!isRhythm && playImData.m_data0 == 0xB8) TotalLevelConvert(8, false);
			}
		}

		/// <summary>
		/// OPL3 ボリュームレジスタ制御
		/// </summary>
		private void OPL3_VolumeRegControl(F1ImData.PlayImData playImData, int index, int connectSelBits, bool isRhythm)
		{
			if ((connectSelBits & 0x01) != 0) {
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0x40) { m_fmTotalLevel[0, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[0, 0] = index; }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0x43) { m_fmTotalLevel[0, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[0, 1] = index; }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0x48) { m_fmTotalLevel[0, 2] = playImData.m_data1 & 0x3F; m_fmTLIndex[0, 2] = index; }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0x4B) { m_fmTotalLevel[0, 3] = playImData.m_data1 & 0x3F; m_fmTLIndex[0, 3] = index; }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0xC0) { m_fmOpeConnect[0] &= 0x1; m_fmOpeConnect[0] |= (int)((playImData.m_data1 & 0x1) << 1); }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0xC3) { m_fmOpeConnect[0] &= 0x2; m_fmOpeConnect[0] |= (int)(playImData.m_data1 & 0x1); }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0xB0) TotalLevelConvert(0, true);
			} else {
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0x40) { m_fmTotalLevel[0, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[0, 0] = index; }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0x43) { m_fmTotalLevel[0, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[0, 1] = index; }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0xC0) { m_fmOpeConnect[0] = (int)(playImData.m_data1 & 0x01); }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0xB0) TotalLevelConvert(0, false);
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0x48) { m_fmTotalLevel[3, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[3, 0] = index; }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0x4B) { m_fmTotalLevel[3, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[3, 1] = index; }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0xC3) { m_fmOpeConnect[3] = (int)(playImData.m_data1 & 0x01); }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0xB3) TotalLevelConvert(3, false);
			}

			if ((connectSelBits & 0x02) != 0) {
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0x41) { m_fmTotalLevel[1, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[1, 0] = index; }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0x44) { m_fmTotalLevel[1, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[1, 1] = index; }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0x49) { m_fmTotalLevel[1, 2] = playImData.m_data1 & 0x3F; m_fmTLIndex[1, 2] = index; }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0x4C) { m_fmTotalLevel[1, 3] = playImData.m_data1 & 0x3F; m_fmTLIndex[1, 3] = index; }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0xC1) { m_fmOpeConnect[1] &= 0x1; m_fmOpeConnect[1] |= (int)((playImData.m_data1 & 0x1) << 1); }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0xC4) { m_fmOpeConnect[1] &= 0x2; m_fmOpeConnect[1] |= (int)(playImData.m_data1 & 0x1); }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0xB1) TotalLevelConvert(1, true);
			} else {
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0x41) { m_fmTotalLevel[1, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[1, 0] = index; }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0x44) { m_fmTotalLevel[1, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[1, 1] = index; }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0xC1) { m_fmOpeConnect[1] = (int)(playImData.m_data1 & 0x01); }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0xB1) TotalLevelConvert(1, false);
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0x49) { m_fmTotalLevel[4, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[4, 0] = index; }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0x4C) { m_fmTotalLevel[4, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[4, 1] = index; }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0xC3) { m_fmOpeConnect[4] = (int)(playImData.m_data1 & 0x01); }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0xB3) TotalLevelConvert(4, false);
			}

			if ((connectSelBits & 0x04) != 0) {
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0x42) { m_fmTotalLevel[2, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[2, 0] = index; }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0x45) { m_fmTotalLevel[2, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[2, 1] = index; }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0x4A) { m_fmTotalLevel[2, 2] = playImData.m_data1 & 0x3F; m_fmTLIndex[2, 2] = index; }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0x4D) { m_fmTotalLevel[2, 3] = playImData.m_data1 & 0x3F; m_fmTLIndex[2, 3] = index; }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0xC2) { m_fmOpeConnect[2] &= 0x1; m_fmOpeConnect[2] |= (int)((playImData.m_data1 & 0x1) << 1); }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0xC5) { m_fmOpeConnect[2] &= 0x2; m_fmOpeConnect[2] |= (int)(playImData.m_data1 & 0x1); }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0xB2) TotalLevelConvert(2, true);
			} else {
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0x42) { m_fmTotalLevel[2, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[2, 0] = index; }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0x45) { m_fmTotalLevel[2, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[2, 1] = index; }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0xC2) { m_fmOpeConnect[2] = (int)(playImData.m_data1 & 0x01); }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0xB2) TotalLevelConvert(2, false);
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0x4A) { m_fmTotalLevel[5, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[5, 0] = index; }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0x4D) { m_fmTotalLevel[5, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[5, 1] = index; }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0xC5) { m_fmOpeConnect[5] = (int)(playImData.m_data1 & 0x01); }
				if (playImData.m_A1 == 0 && playImData.m_data0 == 0xB5) TotalLevelConvert(5, false);
			}

			if ((connectSelBits & 0x08) != 0) {
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0x40) { m_fmTotalLevel[3, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[3, 0] = index; }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0x43) { m_fmTotalLevel[3, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[3, 1] = index; }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0x48) { m_fmTotalLevel[3, 2] = playImData.m_data1 & 0x3F; m_fmTLIndex[3, 2] = index; }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0x4B) { m_fmTotalLevel[3, 3] = playImData.m_data1 & 0x3F; m_fmTLIndex[3, 3] = index; }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0xC0) { m_fmOpeConnect[3] &= 0x1; m_fmOpeConnect[3] |= (int)((playImData.m_data1 & 0x1) << 1); }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0xC3) { m_fmOpeConnect[3] &= 0x2; m_fmOpeConnect[3] |= (int)(playImData.m_data1 & 0x1); }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0xB0) TotalLevelConvert(3, true);
			} else {
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0x40) { m_fmTotalLevel[9, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[9, 0] = index; }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0x43) { m_fmTotalLevel[9, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[9, 1] = index; }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0xC0) { m_fmOpeConnect[9] = (int)(playImData.m_data1 & 0x01); }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0xB0) TotalLevelConvert(9, false);
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0x48) { m_fmTotalLevel[12, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[12, 0] = index; }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0x4B) { m_fmTotalLevel[12, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[12, 1] = index; }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0xC3) { m_fmOpeConnect[12] = (int)(playImData.m_data1 & 0x01); }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0xB3) TotalLevelConvert(12, false);
			}

			if ((connectSelBits & 0x10) != 0) {
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0x41) { m_fmTotalLevel[4, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[4, 0] = index; }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0x44) { m_fmTotalLevel[4, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[4, 1] = index; }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0x49) { m_fmTotalLevel[4, 2] = playImData.m_data1 & 0x3F; m_fmTLIndex[4, 2] = index; }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0x4C) { m_fmTotalLevel[4, 3] = playImData.m_data1 & 0x3F; m_fmTLIndex[4, 3] = index; }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0xC1) { m_fmOpeConnect[4] &= 0x1; m_fmOpeConnect[4] |= (int)((playImData.m_data1 & 0x1) << 1); }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0xC4) { m_fmOpeConnect[4] &= 0x2; m_fmOpeConnect[4] |= (int)(playImData.m_data1 & 0x1); }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0xB1) TotalLevelConvert(4, true);
			} else {
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0x41) { m_fmTotalLevel[10, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[10, 0] = index; }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0x44) { m_fmTotalLevel[10, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[10, 1] = index; }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0xC1) { m_fmOpeConnect[10] = (int)(playImData.m_data1 & 0x01); }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0xB1) TotalLevelConvert(10, false);
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0x49) { m_fmTotalLevel[13, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[13, 0] = index; }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0x4C) { m_fmTotalLevel[13, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[13, 1] = index; }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0xC3) { m_fmOpeConnect[13] = (int)(playImData.m_data1 & 0x01); }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0xB3) TotalLevelConvert(13, false);
			}

			if ((connectSelBits & 0x20) != 0) {
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0x42) { m_fmTotalLevel[5, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[5, 0] = index; }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0x45) { m_fmTotalLevel[5, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[5, 1] = index; }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0x4A) { m_fmTotalLevel[5, 2] = playImData.m_data1 & 0x3F; m_fmTLIndex[5, 2] = index; }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0x4D) { m_fmTotalLevel[5, 3] = playImData.m_data1 & 0x3F; m_fmTLIndex[5, 3] = index; }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0xC2) { m_fmOpeConnect[5] &= 0x1; m_fmOpeConnect[5] |= (int)((playImData.m_data1 & 0x1) << 1); }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0xC5) { m_fmOpeConnect[5] &= 0x2; m_fmOpeConnect[5] |= (int)(playImData.m_data1 & 0x1); }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0xB2) TotalLevelConvert(5, true);
			} else {
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0x42) { m_fmTotalLevel[11, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[11, 0] = index; }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0x45) { m_fmTotalLevel[11, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[11, 1] = index; }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0xC2) { m_fmOpeConnect[11] = (int)(playImData.m_data1 & 0x01); }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0xB2) TotalLevelConvert(11, false);
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0x4A) { m_fmTotalLevel[14, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[14, 0] = index; }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0x4D) { m_fmTotalLevel[14, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[14, 1] = index; }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0xC5) { m_fmOpeConnect[14] = (int)(playImData.m_data1 & 0x01); }
				if (playImData.m_A1 == 1 && playImData.m_data0 == 0xB5) TotalLevelConvert(14, false);
			}
			if (!isRhythm && playImData.m_A1 == 0 && playImData.m_data0 == 0x50) { m_fmTotalLevel[6, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[6, 0] = index; }
			if (!isRhythm && playImData.m_A1 == 0 && playImData.m_data0 == 0x53) { m_fmTotalLevel[6, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[6, 1] = index; }
			if (!isRhythm && playImData.m_A1 == 0 && playImData.m_data0 == 0xC6) { m_fmOpeConnect[6] = (int)(playImData.m_data1 & 0x01); }
			if (!isRhythm && playImData.m_A1 == 0 && playImData.m_data0 == 0xB6) TotalLevelConvert(6, false);
			if (!isRhythm && playImData.m_A1 == 0 && playImData.m_data0 == 0x51) { m_fmTotalLevel[7, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[7, 0] = index; }
			if (!isRhythm && playImData.m_A1 == 0 && playImData.m_data0 == 0x54) { m_fmTotalLevel[7, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[7, 1] = index; }
			if (!isRhythm && playImData.m_A1 == 0 && playImData.m_data0 == 0xC7) { m_fmOpeConnect[7] = (int)(playImData.m_data1 & 0x01); }
			if (!isRhythm && playImData.m_A1 == 0 && playImData.m_data0 == 0xB7) TotalLevelConvert(7, false);
			if (!isRhythm && playImData.m_A1 == 0 && playImData.m_data0 == 0x52) { m_fmTotalLevel[8, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[8, 0] = index; }
			if (!isRhythm && playImData.m_A1 == 0 && playImData.m_data0 == 0x55) { m_fmTotalLevel[8, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[8, 1] = index; }
			if (!isRhythm && playImData.m_A1 == 0 && playImData.m_data0 == 0xC8) { m_fmOpeConnect[8] = (int)(playImData.m_data1 & 0x01); }
			if (!isRhythm && playImData.m_A1 == 0 && playImData.m_data0 == 0xB8) TotalLevelConvert(8, false);
			if (playImData.m_A1 == 1 && playImData.m_data0 == 0x50) { m_fmTotalLevel[15, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[15, 0] = index; }
			if (playImData.m_A1 == 1 && playImData.m_data0 == 0x53) { m_fmTotalLevel[15, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[15, 1] = index; }
			if (playImData.m_A1 == 1 && playImData.m_data0 == 0xC6) { m_fmOpeConnect[15] = (int)(playImData.m_data1 & 0x01); }
			if (playImData.m_A1 == 1 && playImData.m_data0 == 0xB6) TotalLevelConvert(15, false);
			if (playImData.m_A1 == 1 && playImData.m_data0 == 0x51) { m_fmTotalLevel[16, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[16, 0] = index; }
			if (playImData.m_A1 == 1 && playImData.m_data0 == 0x54) { m_fmTotalLevel[16, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[16, 1] = index; }
			if (playImData.m_A1 == 1 && playImData.m_data0 == 0xC7) { m_fmOpeConnect[16] = (int)(playImData.m_data1 & 0x01); }
			if (playImData.m_A1 == 1 && playImData.m_data0 == 0xB7) TotalLevelConvert(16, false);
			if (playImData.m_A1 == 1 && playImData.m_data0 == 0x52) { m_fmTotalLevel[17, 0] = playImData.m_data1 & 0x3F; m_fmTLIndex[17, 0] = index; }
			if (playImData.m_A1 == 1 && playImData.m_data0 == 0x55) { m_fmTotalLevel[17, 1] = playImData.m_data1 & 0x3F; m_fmTLIndex[17, 1] = index; }
			if (playImData.m_A1 == 1 && playImData.m_data0 == 0xC8) { m_fmOpeConnect[17] = (int)(playImData.m_data1 & 0x01); }
			if (playImData.m_A1 == 1 && playImData.m_data0 == 0xB8) TotalLevelConvert(17, false);
		}

		/// <summary>
		///	OPLs FnumberとBlock変換
		/// </summary>
		private void OPLs_FM_ToneConvert(ConvertWordToneData toneCvData, out byte fnum, out byte blk)
		{
			if (m_targetChip.TargetChipType != ChipType.YM2413)
			{
				OPL_FM_ToneConvert(toneCvData, out fnum, out blk);
			}
			else
			{
				OPLL_FM_ToneConvert(toneCvData, out fnum, out blk);
			}
		}

		/// <summary>
		///	OPL の FnumberとBlock変換
		/// </summary>
		private void OPL_FM_ToneConvert(ConvertWordToneData toneCvData, out byte fnum, out byte blk)
		{
			int i_blk = ((toneCvData.m_hiData & 0x01C) >> 2) & 0x07;
			int i_fnum = (((toneCvData.m_hiData & 0x03) << 8) & 0x300) | (toneCvData.m_lowData & 0xFF);
			if (i_blk == 0 && i_fnum == 0)
			{
				blk = (byte)toneCvData.m_hiData;
				fnum = (byte)toneCvData.m_lowData;
				return;
			}
			int keepKon = toneCvData.m_hiData & 0xE0;
			float hardBaseClock = (float)m_targetChip.TargetChipClock;
			float playBaseClock = (float)m_targetChip.SourceChipClock;
			if (m_targetChip.TargetChipType == ChipType.YMF262)
			{
				switch(m_targetChip.SourceChipType)
				{
					case ChipType.YM3526:
					case ChipType.YM3812:
					case ChipType.Y8950:
						playBaseClock *= 4f;
						break;
					default:
						break;
				}
			}
			playBaseClock /= 72f;
			hardBaseClock /= 72f;

			float f_blk = (float)(Math.Pow(2,(double)(20 - i_blk)));
			//	再生データの音程周波数を求める
			float f_tone = ((float)i_fnum) / (f_blk / playBaseClock);
			while(true)
			{
				//	再生ハードで音程周波数を出すための F_Number を求める
				i_fnum = (int)(f_tone * (f_blk / hardBaseClock));
				if (i_fnum > 0x3ff)
				{
					if (i_blk >= 0x07)
					{
						i_fnum = 0x3FF;
						break;
					}
					if (i_blk <= 0x00)
					{
						i_fnum = 0x0;
						break;
					}
					if (m_targetChip.TargetChipClock > m_targetChip.SourceChipClock) i_blk -= 1; else i_blk += 1;
					f_blk = (float)(Math.Pow(2,(double)(20 - i_blk)));
				}
				else
				{
					break;
				}
			}
			//	Block と F-Number のレジスタデータを組みなおして返す
			i_blk = keepKon | ((i_blk << 2) & 0x1C) | ((i_fnum >> 8) & 0x03);
			i_fnum &= 0xFF;
			fnum = (byte)i_fnum;
			blk = (byte)i_blk;
		}

		/// <summary>
		///	OPLL の FnumberとBlock変換
		/// </summary>
		private void OPLL_FM_ToneConvert(ConvertWordToneData toneCvData, out byte fnum, out byte blk)
		{
			int i_blk = ((toneCvData.m_hiData & 0x0E) >> 1) & 0x07;
			int i_fnum = (((toneCvData.m_hiData & 0x01) << 8) & 0x100) | (toneCvData.m_lowData & 0xFF);
			if (i_blk == 0 && i_fnum == 0)
			{
				blk = (byte)toneCvData.m_hiData;
				fnum = (byte)toneCvData.m_lowData;
				return;
			}
			int keepKon = toneCvData.m_hiData & 0xF0;
			float hardClockConst = (float)m_targetChip.TargetChipClock / 72f;
			float playClockConst = (float)(Math.Pow(2,19)) / (((float)m_targetChip.SourceChipClock) / 72f);
			float f_blk = (float)(Math.Pow(2,(double)(i_blk)));
			float pow2_19 = (float)(Math.Pow(2,19));
			//	再生データの音程周波数を求める
			float f_tone = (((float)i_fnum) * f_blk) / playClockConst;
			while(true)
			{
				//	再生ハードで音程周波数を出すための F_Number を求める
				i_fnum = (int)((f_tone * pow2_19 / hardClockConst) / f_blk);
				if (i_fnum > 0x1ff)
				{
					if (i_blk >= 0x07)
					{
						i_fnum = 0x1FF;
						break;
					}
					if (i_blk <= 0x00)
					{
						i_fnum = 0x0;
						break;
					}
					if (m_targetChip.TargetChipClock > m_targetChip.SourceChipClock) i_blk -= 1; else i_blk += 1;
					f_blk = (float)(Math.Pow(2,(double)(i_blk)));
				}
				else
				{
					break;
				}
			}
			//	Block と F-Number のレジスタデータを組みなおして返す
			i_blk = keepKon | ((i_blk << 1) & 0x0E) | ((i_fnum >> 8) & 0x01);
			i_fnum &= 0xFF;
			fnum = (byte)i_fnum;
			blk = (byte)i_blk;
		}

		/// <summary>
		///	トータルレベル変換
		/// </summary>
		private void TotalLevelConvert(int ch, bool isOpe4)
		{
			int[] connectOpe2Table = { 0b0010, 0b0011 };
			int[] connectOpe4Table = { 0b1000, 0b1010, 0b1001, 0b1101 };

			int bitFlag = (!isOpe4) ? connectOpe2Table[m_fmOpeConnect[ch]] : connectOpe4Table[m_fmOpeConnect[ch]];
			for (int ope = 0; ope < m_fmOperatorNum; ope ++)
			{
				if ((bitFlag & 0x01) != 0)
				{
					int index = m_fmTLIndex[ch, ope];
					int kp = ((int)(m_imData.PlayImDataList[index].m_data1)) & 0xC0;
					int tl = 0x7F - m_fmTotalLevel[ch, ope];
					if (tl != 0)
					{
						tl = (int)((float)tl + (128f * ((float)(m_imData.FMVol) / 100f)));
						tl = (tl < 0x00) ? 0x00 : ((tl > 0x7F) ? 0x7F : tl);
					}
					tl = 0x7F - tl;
					tl |= kp;
					m_imData.PlayImDataList[m_fmTLIndex[ch, ope]].m_data1 = (byte)tl;
				}
				bitFlag = bitFlag >> 1;
			}
		}

		/// <summary>
		/// 未実装レジスタチェック	YM3526
		/// </summary>
		private bool CheckRegister_YM3526(F1ImData.PlayImData playImData)
		{
			if (playImData.m_A1 > 0)
			{
				playImData.m_imType = F1ImData.PlayImType.NONE;
				return false;
			}
			var ncRegs = YM3526_NC_REG;
			foreach(var ncReg in ncRegs)
			{
				if (playImData.m_data0 == ncReg)
				{
					playImData.m_imType = F1ImData.PlayImType.NONE;
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// 未実装レジスタチェック	YM3812
		/// </summary>
		private bool CheckRegister_YM3812(F1ImData.PlayImData playImData)
		{
			if (playImData.m_A1 > 0)
			{
				playImData.m_imType = F1ImData.PlayImType.NONE;
				return false;
			}
			var ncRegs = YM3812_NC_REG;
			foreach(var ncReg in ncRegs)
			{
				if (playImData.m_data0 == ncReg)
				{
					playImData.m_imType = F1ImData.PlayImType.NONE;
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// 未実装レジスタチェック	Y8950
		/// </summary>
		private bool CheckRegister_Y8950(F1ImData.PlayImData playImData)
		{
			if (playImData.m_A1 > 0)
			{
				playImData.m_imType = F1ImData.PlayImType.NONE;
				return false;
			}
			if (playImData.m_data0 > 0x09 && playImData.m_data0 < 0x20)
			{
				playImData.m_imType = F1ImData.PlayImType.NONE;
				return false;
			}
			var ncRegs = Y8950_NC_REG;
			foreach(var ncReg in ncRegs)
			{
				if (playImData.m_data0 == ncReg)
				{
					playImData.m_imType = F1ImData.PlayImType.NONE;
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// 未実装レジスタチェック	YM2413
		/// </summary>
		private bool CheckRegister_YM2413(F1ImData.PlayImData playImData)
		{
			if (playImData.m_A1 > 0)
			{
				playImData.m_imType = F1ImData.PlayImType.NONE;
				return false;
			}
			var ncRegs = YM2413_NC_REG;
			foreach(var ncReg in ncRegs)
			{
				if (playImData.m_data0 == ncReg)
				{
					playImData.m_imType = F1ImData.PlayImType.NONE;
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// 未実装レジスタチェック	YMF262
		/// </summary>
		private bool CheckRegister_YMF262(F1ImData.PlayImData playImData)
		{
			var ncRegs = (playImData.m_A1 > 0) ? YMF262_NC_REG_A1_1 : YMF262_NC_REG_A1_0;
			foreach(var ncReg in ncRegs)
			{
				if (playImData.m_data0 == ncReg)
				{
					playImData.m_imType = F1ImData.PlayImType.NONE;
					return false;
				}
			}
			return true;
		}

		/// <summary>
		///	YM3526	未実装レジスタ
		/// </summary>
		private readonly byte[] YM3526_NC_REG = 
		{
			0x00, 0x01,	//	0x01 LSI Test
			0x05, 0x06, 0x07, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
			0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
			0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F,
			0x56, 0x57, 0x58, 0x59, 0x5A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F,
			0x76, 0x77, 0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F,
			0x96, 0x97, 0x98, 0x99, 0x9A, 0x9B, 0x9C, 0x9D, 0x9E, 0x9F,
			0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF,
			0xB9, 0xBA, 0xBB, 0xBC, 0xBE, 0xBF,
			0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
			0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
			0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
			0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF,
		};

		/// <summary>
		///	YM3812	未実装レジスタ
		/// </summary>
		private readonly byte[] YM3812_NC_REG = 
		{
			0x00, 0x01,	//	0x01 LSI Test
			0x05, 0x06, 0x07, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
			0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
			0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F,
			0x56, 0x57, 0x58, 0x59, 0x5A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F,
			0x76, 0x77, 0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F,
			0x96, 0x97, 0x98, 0x99, 0x9A, 0x9B, 0x9C, 0x9D, 0x9E, 0x9F,
			0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF,
			0xB9, 0xBA, 0xBB, 0xBC, 0xBE, 0xBF,
			0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
			0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
			0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF,
		};
		/// <summary>
		///	Y8950	未実装レジスタ
		/// </summary>
		private readonly byte[] Y8950_NC_REG = 
		{
			0x00, 0x01,	//	0x01 LSI Test
			0x13, 0x14, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
			0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F,
			0x56, 0x57, 0x58, 0x59, 0x5A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F,
			0x76, 0x77, 0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F,
			0x96, 0x97, 0x98, 0x99, 0x9A, 0x9B, 0x9C, 0x9D, 0x9E, 0x9F,
			0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF,
			0xB9, 0xBA, 0xBB, 0xBC, 0xBE, 0xBF,
			0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
			0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
			0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
			0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF,
		};

		/// <summary>
		///	YM2413	未実装レジスタ
		/// </summary>
		private readonly byte[] YM2413_NC_REG = 
		{
			0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D,
			0x0F,	//	0x0F LSI Test
			0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
			0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F,
			0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F,
			0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x4A, 0x4B, 0x4C, 0x4D, 0x4E, 0x4F,
			0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F,
			0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x6B, 0x6C, 0x6D, 0x6E, 0x6F,
			0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F,
			0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8A, 0x8B, 0x8C, 0x8D, 0x8E, 0x8F,
			0x90, 0x91, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0x9B, 0x9C, 0x9D, 0x9E, 0x9F,
			0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF,
			0xB0, 0xB1, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF,
			0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
			0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
			0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
			0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF,
		};

		/// <summary>
		///	YMF262	未実装レジスタ
		/// </summary>
		private readonly byte[] YMF262_NC_REG_A1_0 = 
		{
			0x00, 0x01,	//	0x01 LSI Test
			0x05, 0x06, 0x07, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
			0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
			0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F,
			0x56, 0x57, 0x58, 0x59, 0x5A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F,
			0x76, 0x77, 0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F,
			0x96, 0x97, 0x98, 0x99, 0x9A, 0x9B, 0x9C, 0x9D, 0x9E, 0x9F,
			0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF,
			0xB9, 0xBA, 0xBB, 0xBC, 0xBE, 0xBF,
			0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
			0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
			0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF,
		};
		private readonly byte[] YMF262_NC_REG_A1_1 = 
		{
			0x00, 0x01,	//	0x01 LSI Test
			0x02, 0x03, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
			0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
			0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F,
			0x56, 0x57, 0x58, 0x59, 0x5A, 0x5B, 0x5C, 0x5D, 0x5E, 0x5F,
			0x76, 0x77, 0x78, 0x79, 0x7A, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F,
			0x96, 0x97, 0x98, 0x99, 0x9A, 0x9B, 0x9C, 0x9D, 0x9E, 0x9F,
			0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF,
			0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF,
			0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
			0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
			0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF,
		};

	}
}