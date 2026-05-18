using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace F1
{
	/// <summary>
	///	CHIP OPN クラス
	/// </summary>
	public class Chip_OPN : SoundChip
	{
		private byte m_reg27State;
		private byte m_reg29State;
		private byte m_reg07State;
		private byte m_OPN2reg2BState;

		private int FIX_FM_PRESCALER { get; } = 6;
		private int FIX_SSG_PRESCALER { get; } = 4;
		private int OPNs_OPERATOR { get; } = 4;
		private int OPN_FM_CH { get; } = 3;
		private int OPN_SSG_CH { get; } = 3;
		private int OPNA_FM_CH { get; } = 6;
		private int OPNA_SSG_CH { get; } = 3;
		private int OPNB_FM_CH { get; } = 6;	//	Includes 2 dummy channels
		private int OPNB_SSG_CH { get; } = 3;
		private int OPNB2_FM_CH { get; } = 6;
		private int OPNB2_SSG_CH { get; } = 3;
		private int OPN2_FM_CH { get; } = 6;
		private int OPN2_SSG_CH { get; } = 0;
		private int OPN3L_FM_CH { get; } = 6;
		private int OPN3L_SSG_CH { get; } = 3;

		private int m_fmPrescaler;
		private int m_ssgPrescaler;

		private enum ClockToneConvertType
		{
			FM_TONE,
			SSG_TONE,
			SSG_ENV,
		}
		private ConvertWordToneData[] m_fmToneCvDataArray;
		private ConvertWordToneData[] m_ssgToneCvDataArray;
		private ConvertWordToneData m_ssgEnvCvData;

		/// <summary>
		/// レジスタ操作にプレイ CHIP にあわせた制御を入れる
		/// </summary>
		public override void ControlPlayChipRegiser()
		{
			m_reg27State = 0x3F;
			m_reg07State = 0xC0;
			m_reg29State = 0x7F;
			m_OPN2reg2BState = 0x7F;

			m_fmPrescaler = 6;		//    1/6
			m_ssgPrescaler = 4;		//    1/4
			foreach(var playImData in m_imData.PlayImDataList.Where(x => x.m_chipSelect == m_targetChip.ChipSelect && x.m_imType == F1ImData.PlayImType.TWO_DATA))
			{
				var isDualIn1Chip = m_imData.IsDualIn1ChipCSDict[m_targetChip.ChipSelect];

				ControlPlayChipReg_SetPrescaler(playImData);
				switch(m_targetChip.TargetChipType)
				{
					case ChipType.YM2203:
						switch(m_targetChip.SourceChipType)
						{
							case ChipType.AY_3_8910:
								if (!CheckRegister_AY_3_8910(playImData)) continue;
								break;
							case ChipType.YM2149:
								if (!CheckRegister_YM2149(playImData)) continue;
								break;
							case ChipType.YM2203:
								break;
							case ChipType.YM2608:
								if (!CheckPcmRegister_YM2608(playImData, m_targetChip.IsTargetPcmActive)) continue;
								if (!CheckRegister_YM2608(playImData)) continue;
								break;
							default:
								playImData.m_imType = F1ImData.PlayImType.NONE;
								continue;
						}
						CheckRegister_YM2203(playImData, isDualIn1Chip);
						if (!ControlPlayChipReg_CheckTimer(playImData)) continue;
						if (!ControlPlayChipReg_CheckSSGIO(playImData)) continue;
						break;

					case ChipType.YM2608:
						switch(m_targetChip.SourceChipType)
						{
							case ChipType.AY_3_8910:
								if (!CheckRegister_AY_3_8910(playImData)) continue;
								break;
							case ChipType.YM2149:
								if (!CheckRegister_YM2149(playImData)) continue;
								break;
							case ChipType.YM2203:
								if (!ControlPlayChipReg_DualYM2203(playImData, isDualIn1Chip)) continue;
								if (!CheckRegister_YM2203(playImData, isDualIn1Chip)) continue;
								break;
							case ChipType.YM2608:
								if (!CheckPcmRegister_YM2608(playImData, m_targetChip.IsTargetPcmActive)) continue;
								break;
							case ChipType.YM2610:
								if (!CheckPcmRegister_YM2610s(playImData, m_targetChip.IsTargetPcmActive)) continue;
								if (!CheckRegister_YM2610(playImData)) continue;
								break;
							case ChipType.YM2610B:
								if (!CheckPcmRegister_YM2610s(playImData, m_targetChip.IsTargetPcmActive)) continue;
								if (!CheckRegister_YM2610B(playImData)) continue;
								break;
							case ChipType.YM2612:
								if (!CheckPcmRegister_YM2612(playImData, m_targetChip.IsTargetPcmActive)) continue;
								if (!CheckRegister_YM2612(playImData)) continue;
								break;
							default:
								playImData.m_imType = F1ImData.PlayImType.NONE;
								continue;
						}
						if (!CheckRegister_YM2608(playImData)) continue;
						if (!ControlPlayChipReg_CheckTimer(playImData)) continue;
						if (!ControlPlayChipReg_CheckSSGIO(playImData)) continue;
						if (!ControlPlayChipReg_CheckSCH(playImData)) continue;
						break;

					case ChipType.YM2610:
						switch(m_targetChip.SourceChipType)
						{
							case ChipType.AY_3_8910:
								if (!CheckRegister_AY_3_8910(playImData)) continue;
								break;
							case ChipType.YM2149:
								if (!CheckRegister_YM2149(playImData)) continue;
								break;
							case ChipType.YM2203:
								if (!ControlPlayChipReg_DualYM2203(playImData, isDualIn1Chip)) continue;
								if (!CheckRegister_YM2203(playImData, isDualIn1Chip)) continue;
								break;
							case ChipType.YM2608:
								if (!CheckRhythmRegister_YM2608(playImData)) continue;
								if (!CheckPcmRegister_YM2608(playImData, m_targetChip.IsTargetPcmActive)) continue;
								if (!CheckRegister_YM2608(playImData)) continue;
								break;
							case ChipType.YM2610:
								if (!CheckPcmRegister_YM2610s(playImData, m_targetChip.IsTargetPcmActive)) continue;
								break;
							case ChipType.YM2610B:
								if (!CheckPcmRegister_YM2610s(playImData, m_targetChip.IsTargetPcmActive)) continue;
								if (!CheckRegister_YM2610B(playImData)) continue;
								break;
							case ChipType.YM2612:
								if (!CheckPcmRegister_YM2612(playImData, m_targetChip.IsTargetPcmActive)) continue;
								if (!CheckRegister_YM2612(playImData)) continue;
								break;
							default:
								playImData.m_imType = F1ImData.PlayImType.NONE;
								continue;
						}
						if (!CheckRegister_YM2610(playImData)) continue;
						if (!ControlPlayChipReg_CheckTimer(playImData)) continue;
						break;

					case ChipType.YM2610B:
						switch(m_targetChip.SourceChipType)
						{
							case ChipType.AY_3_8910:
								if (!CheckRegister_AY_3_8910(playImData)) continue;
								break;
							case ChipType.YM2149:
								if (!CheckRegister_YM2149(playImData)) continue;
								break;
							case ChipType.YM2203:
								if (!ControlPlayChipReg_DualYM2203(playImData, isDualIn1Chip)) continue;
								if (!CheckRegister_YM2203(playImData, isDualIn1Chip)) continue;
								break;
							case ChipType.YM2608:
								if (!CheckRhythmRegister_YM2608(playImData)) continue;
								if (!CheckPcmRegister_YM2608(playImData, m_targetChip.IsTargetPcmActive)) continue;
								if (!CheckRegister_YM2608(playImData)) continue;
								break;
							case ChipType.YM2610:
								if (!CheckPcmRegister_YM2610s(playImData, m_targetChip.IsTargetPcmActive)) continue;
								if (!CheckRegister_YM2610(playImData)) continue;
								break;
							case ChipType.YM2610B:
								if (!CheckPcmRegister_YM2610s(playImData, m_targetChip.IsTargetPcmActive)) continue;
								break;
							case ChipType.YM2612:
								if (!CheckPcmRegister_YM2612(playImData, m_targetChip.IsTargetPcmActive)) continue;
								if (!CheckRegister_YM2612(playImData)) continue;
								break;
							default:
								playImData.m_imType = F1ImData.PlayImType.NONE;
								continue;
						}
						if (!CheckRegister_YM2610B(playImData)) continue;
						if (!ControlPlayChipReg_CheckTimer(playImData)) continue;
						break;

					case ChipType.YM2612:
						switch(m_targetChip.SourceChipType)
						{
							case ChipType.YM2203:
								if (!ControlPlayChipReg_DualYM2203(playImData, isDualIn1Chip)) continue;
								if (!CheckRegister_YM2203(playImData, isDualIn1Chip)) continue;
								break;
							case ChipType.YM2608:
								if (!CheckPcmRegister_YM2608(playImData, m_targetChip.IsTargetPcmActive)) continue;
								if (!CheckRegister_YM2608(playImData)) continue;
								break;
							case ChipType.YM2610:
								if (!CheckPcmRegister_YM2610s(playImData, m_targetChip.IsTargetPcmActive)) continue;
								if (!CheckRegister_YM2610(playImData)) continue;
								break;
							case ChipType.YM2610B:
								if (!CheckPcmRegister_YM2610s(playImData, m_targetChip.IsTargetPcmActive)) continue;
								if (!CheckRegister_YM2610B(playImData)) continue;
								break;
							case ChipType.YM2612:
								if (!CheckPcmRegister_YM2612(playImData, m_targetChip.IsTargetPcmActive)) continue;
								break;
							default:
								playImData.m_imType = F1ImData.PlayImType.NONE;
								continue;
						}
						if (!CheckRegister_YM2612(playImData)) continue;
						if (!ControlPlayChipReg_CheckTimer(playImData)) continue;
						break;

					case ChipType.YMF288:
						switch(m_targetChip.SourceChipType)
						{
							case ChipType.AY_3_8910:
								if (!CheckRegister_AY_3_8910(playImData)) continue;
								break;
							case ChipType.YM2149:
								if (!CheckRegister_YM2149(playImData)) continue;
								break;
							case ChipType.YM2203:
								if (!ControlPlayChipReg_DualYM2203(playImData, isDualIn1Chip)) continue;
								if (!CheckRegister_YM2203(playImData, isDualIn1Chip)) continue;
								break;
							case ChipType.YM2608:
								if (!CheckPcmRegister_YM2608(playImData, m_targetChip.IsTargetPcmActive)) continue;
								if (!CheckRegister_YM2608(playImData)) continue;
								break;
							case ChipType.YM2610:
								if (!CheckPcmRegister_YM2610s(playImData, m_targetChip.IsTargetPcmActive)) continue;
								if (!CheckRegister_YM2610(playImData)) continue;
								break;
							case ChipType.YM2610B:
								if (!CheckPcmRegister_YM2610s(playImData, m_targetChip.IsTargetPcmActive)) continue;
								if (!CheckRegister_YM2610B(playImData)) continue;
								break;
							case ChipType.YM2612:
								if (!CheckPcmRegister_YM2612(playImData, m_targetChip.IsTargetPcmActive)) continue;
								if (!CheckRegister_YM2612(playImData)) continue;
								break;
							default:
								playImData.m_imType = F1ImData.PlayImType.NONE;
								continue;
						}
						if (!CheckRegister_YMF288(playImData)) continue;
						if (!ControlPlayChipReg_CheckTimer(playImData)) continue;
						if (!ControlPlayChipReg_CheckSSGIO(playImData)) continue;
						if (!ControlPlayChipReg_CheckSCH(playImData)) continue;
						break;
				}
			}
			m_imData.CleanupPlayImDataList();
		}

		/// <summary>
		/// レジスタ操作にプレイ CHIP にあわせた制御を入れる	2203 Dualの制御
		///	NOTE:	ＦＭが６チャンネルの CHIP で、ＦＭが３チャンネルの CHIP(YM2203)ｘ２つのデータを再生する
		/// </summary>
		private bool ControlPlayChipReg_DualYM2203(F1ImData.PlayImData playImData, bool isDualIn1Chip)
		{
			if (isDualIn1Chip)
			{
				if (playImData.m_A1 == 1)
				{
					if (playImData.m_data0 == 0x28)
					{
						uint wdata = (uint)playImData.m_data1;
						uint ch = wdata & 0x03;
						uint nch = ch;
						if (ch == 0) nch = 0x04;
						else if (ch == 1) nch = 0x05;
						else if (ch == 2) nch = 0x06;
						else 
						{
							playImData.m_imType = F1ImData.PlayImType.NONE;
							return false;
						}
						wdata &= 0xF0;
						wdata |= nch;
						playImData.m_data1 = (byte)wdata;
						playImData.m_A1 = 0;
					}
					else
					{	// 	２つめ YM2203 の SSG レジスタ書き込みとキーオンオフ以外の FM 共通部レジスタへの書き込みは、無効とする
						if (playImData.m_data0 < 0x30)
						{
							playImData.m_imType = F1ImData.PlayImType.NONE;
							return false;
						}
					}
				}
			}
			return true;
		}
		/// <summary>
		/// レジスタ操作にプレイ CHIP にあわせた制御を入れる	プリスケーラの制御
		///	NOTE:	ImData の Free 領域を FM と SSG のプリスケーラ値とする
		/// </summary>
		private void ControlPlayChipReg_SetPrescaler(F1ImData.PlayImData playImData)
		{
			switch(m_targetChip.SourceChipType)
			{
				case ChipType.YM2203:
				case ChipType.YM2608:
					if (playImData.m_data0 == 0x2D)
					{
						m_fmPrescaler = 6;
						m_ssgPrescaler = 4;
					}
					else if (playImData.m_data0 == 0x2E)
					{
						m_fmPrescaler = 3;
						m_ssgPrescaler = 2;
					}
					else if (playImData.m_data0 == 0x2F)
					{
						m_fmPrescaler = 2;
						m_ssgPrescaler = 1;
					}
					break;
				case ChipType.AY_3_8910:
				case ChipType.YM2149:
				case ChipType.YM2612:
				case ChipType.YM2610:
				case ChipType.YM2610B:
				default:
					m_fmPrescaler = 6;
					m_ssgPrescaler = 4;
					break;
			}
			playImData.m_tmpFreeData0 = m_fmPrescaler;
			playImData.m_tmpFreeData1 = m_ssgPrescaler;
		}

		/// <summary>
		/// レジスタ操作にプレイ CHIP にあわせた制御を入れる	SCHの制御
		/// </summary>
		private bool ControlPlayChipReg_CheckSCH(F1ImData.PlayImData playImData)
		{
			if (playImData.m_data0 == 0x29)
			{	//	SCH 設定レジスタのチェック
				byte sch = (byte)(playImData.m_data1 & 0x80);
				playImData.m_data1 = sch;
				if (m_reg29State != 0x7F && m_reg29State == sch)
				{
					playImData.m_imType = F1ImData.PlayImType.NONE;
					return false;
				}
				m_reg29State = sch;
			}
			return true;
		}
		/// <summary>
		/// レジスタ操作にプレイ CHIP にあわせた制御を入れる	SSG のIO制御
		/// </summary>
		private bool ControlPlayChipReg_CheckSSGIO(F1ImData.PlayImData playImData)
		{
			if (playImData.m_A1 == 0)
			{
				if (playImData.m_data0 == 0x07)
				{	//	YM22203 SSG のIO設定レジスタのチェック
					byte md = (byte)(playImData.m_data1 & 0x3F);
					playImData.m_data1 = md;
					if (m_reg07State != 0xC0 && m_reg07State == md) 
					{
						playImData.m_imType = F1ImData.PlayImType.NONE;
						return false;
					}
					m_reg07State = md;
				}
			}
			return true;
		}

		/// <summary>
		/// レジスタ操作にプレイ CHIP にあわせた制御を入れる	タイマレジスタの制御
		/// </summary>
		private bool ControlPlayChipReg_CheckTimer(F1ImData.PlayImData playImData)
		{
			if (!m_imData.IsTimerReg)
			{
				if (playImData.m_A1 == 0)
				{
					if (playImData.m_data0 == 0x24 || playImData.m_data0 == 0x25 || playImData.m_data0 == 0x26) 
					{	//	TimerA.TimerB
						playImData.m_imType = F1ImData.PlayImType.NONE;
						return false;
					}
					if (playImData.m_data0 == 0x27)
					{	//	モード設定レジスタのチェック
						byte md = (byte)(playImData.m_data1 & 0xC0);
						playImData.m_data1 = md;
						if (m_reg27State != 0x3F && m_reg27State == md)
						{
							playImData.m_imType = F1ImData.PlayImType.NONE;
							return false;
						}
						m_reg27State = md;
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
				int ch = 0;
				uint reg = 0;
				uint data = 0;
				uint playMilliSec = 0;

				bool isHi = false;
				uint tmpData = 0;

				Dictionary<uint, uint> sameReg = new Dictionary<uint, uint>();

				int ch_num = OPNA_FM_CH;						//	チャンネル数、OPNA より多いものはないハズ
				ConvertWordToneData[] toneShrinkAr = new ConvertWordToneData[ch_num];
				for (int i = 0; i < ch_num; i ++)
				{
					toneShrinkAr[i] = new ConvertWordToneData();
				}

				for (int playImDataIndex = 0, l = m_imData.PlayImDataList.Count; playImDataIndex < l; playImDataIndex++)
				{
					var playImData = m_imData.PlayImDataList[playImDataIndex];
					if (playImData.m_chipSelect == m_targetChip.ChipSelect)
					{
						switch(playImData.m_imType)
						{
							case F1ImData.PlayImType.TWO_DATA:
								reg = (uint)playImData.m_data0;
								data = (uint)playImData.m_data1;
								if ( 
									(reg == 0x06 && playImData.m_A1 == 0) || 	//	SSG	ノイズ周波数
									(reg == 0x07 && playImData.m_A1 == 0) || 	//	SSG	IOA,Bの方向/トーンとノイズのミキシング設定
									(reg == 0x08 && playImData.m_A1 == 0) || 	//	SSG	チャンネルＡのエンベロープ有効 or 固定音量
									(reg == 0x09 && playImData.m_A1 == 0) || 	//	SSG	チャンネルＢのエンベロープ有効 or 固定音量
									(reg == 0x0A && playImData.m_A1 == 0) || 	//	SSG	チャンネルＣのエンベロープ有効 or 固定音量
									(reg >= 0x40 && reg <= 0x4E) 			//	FM	トータルレベル
								)
								{	//	連続して同じ値を書いている場合は、削除する
									if (playImData.m_A1 == 1) reg += 0x100;
									if (sameReg.ContainsKey(reg))
									{
										if (sameReg[reg] == data)
										{
											playImData.m_imType = F1ImData.PlayImType.NONE;
										}
										else
										{
											sameReg[reg] = data;
										}
									}
									else
									{
										sameReg.Add(reg,data);
									}
								}
								else if ( (reg >= 0xA0 && reg <= 0xA2) || (reg >= 0xA4 && reg <= 0xA6) )
								{		//	FM	音程レジスタで同一の周波数を設定している場合は、削除する
									switch(reg) 
									{
										case 0xA0: isHi = false; ch = 0; break;
										case 0xA1: isHi = false; ch = 1; break;
										case 0xA2: isHi = false; ch = 2; break;
										case 0xA4: isHi = true;  ch = 0; break;
										case 0xA5: isHi = true;  ch = 1; break;
										case 0xA6: isHi = true;  ch = 2; break;
									}
									if (playImData.m_A1 == 1) ch += 3;
									if (!isHi)
									{	//	Low Data.
										switch(toneShrinkAr[ch].m_lastByteRegOrder)
										{
											case ByteRegOrder.NONE:
												toneShrinkAr[ch].m_lowData = (int)data;
												toneShrinkAr[ch].m_lowPlayImDataIndex = playImDataIndex;
												break;
											case ByteRegOrder.HI:
												if (toneShrinkAr[ch].m_hiPlayImDataIndex >= 0)
												{
													tmpData = ((uint)(toneShrinkAr[ch].m_hiData) << 8) & 0xFF00;
													tmpData |= data & 0xFF;
													if (toneShrinkAr[ch].m_isOldData)
													{
														if (toneShrinkAr[ch].m_oldData == tmpData)
														{
															m_imData.PlayImDataList[toneShrinkAr[ch].m_hiPlayImDataIndex].m_imType = F1ImData.PlayImType.NONE;
															playImData.m_imType = F1ImData.PlayImType.NONE;
														}
														else
														{
															toneShrinkAr[ch].m_oldData = tmpData;
														}
													}
													else
													{
														toneShrinkAr[ch].m_oldData = tmpData;
														toneShrinkAr[ch].m_isOldData = true;
													}
													toneShrinkAr[ch].m_lowPlayImDataIndex = -1;
													toneShrinkAr[ch].m_hiPlayImDataIndex = -1;
												}
												else
												{
													toneShrinkAr[ch].m_lowData = (int)data;
													toneShrinkAr[ch].m_lowPlayImDataIndex = playImDataIndex;
												}
												break;
											case ByteRegOrder.LOW:
												if (toneShrinkAr[ch].m_lowPlayImDataIndex >= 0)
												{
													tmpData = (uint)(toneShrinkAr[ch].m_lowData) & 0xFF;
													tmpData |= ((uint)(toneShrinkAr[ch].m_hiData) << 8) & 0xFF00;
													if (toneShrinkAr[ch].m_isOldData)
													{
														if (toneShrinkAr[ch].m_oldData == tmpData)
														{
															m_imData.PlayImDataList[toneShrinkAr[ch].m_hiPlayImDataIndex].m_imType = F1ImData.PlayImType.NONE;
															m_imData.PlayImDataList[toneShrinkAr[ch].m_lowPlayImDataIndex].m_imType = F1ImData.PlayImType.NONE;
														}
														else
														{
															toneShrinkAr[ch].m_oldData = tmpData;
														}
													}
													else
													{
														toneShrinkAr[ch].m_oldData = tmpData;
														toneShrinkAr[ch].m_isOldData = true;
													}
												}
												toneShrinkAr[ch].m_lowData = (int)data;
												toneShrinkAr[ch].m_lowPlayImDataIndex = playImDataIndex;
												break;
										}
										toneShrinkAr[ch].m_lowMilliSec = playMilliSec;
										toneShrinkAr[ch].m_lastByteRegOrder = ByteRegOrder.LOW;
									}
									else
									{	//	Hi Data.
										switch(toneShrinkAr[ch].m_lastByteRegOrder)
										{
											case ByteRegOrder.NONE:
											case ByteRegOrder.HI:
												toneShrinkAr[ch].m_hiData = (int)data;
												toneShrinkAr[ch].m_hiPlayImDataIndex = playImDataIndex;
												break;
											case ByteRegOrder.LOW:
												if (toneShrinkAr[ch].m_lowPlayImDataIndex >= 0)
												{
													uint d_milli = playMilliSec - toneShrinkAr[ch].m_lowMilliSec;
													if (d_milli >= 5)
													{
														tmpData = (uint)(toneShrinkAr[ch].m_lowData) & 0xFF;
														tmpData |= ((uint)(toneShrinkAr[ch].m_hiData) << 8) & 0xFF00;
														if (toneShrinkAr[ch].m_isOldData)
														{
															if (toneShrinkAr[ch].m_oldData == tmpData)
															{
																m_imData.PlayImDataList[toneShrinkAr[ch].m_hiPlayImDataIndex].m_imType = F1ImData.PlayImType.NONE;
																m_imData.PlayImDataList[toneShrinkAr[ch].m_lowPlayImDataIndex].m_imType = F1ImData.PlayImType.NONE;
															}
															else
															{
																toneShrinkAr[ch].m_oldData = tmpData;
															}
														}
														else
														{
															toneShrinkAr[ch].m_oldData = tmpData;
															toneShrinkAr[ch].m_isOldData = true;
														}
														toneShrinkAr[ch].m_hiData = (int)data;
														toneShrinkAr[ch].m_hiPlayImDataIndex = playImDataIndex;
													}
													else
													{
														tmpData = (uint)(toneShrinkAr[ch].m_lowData) & 0xFF;
														tmpData |= ((uint)(toneShrinkAr[ch].m_hiData) << 8) & 0xFF00;
														if (toneShrinkAr[ch].m_isOldData)
														{
															if (toneShrinkAr[ch].m_oldData == tmpData)
															{
																m_imData.PlayImDataList[toneShrinkAr[ch].m_hiPlayImDataIndex].m_imType = F1ImData.PlayImType.NONE;
																m_imData.PlayImDataList[toneShrinkAr[ch].m_lowPlayImDataIndex].m_imType = F1ImData.PlayImType.NONE;
															}
															else
															{
																toneShrinkAr[ch].m_oldData = tmpData;
															}
														}
														else
														{
															toneShrinkAr[ch].m_oldData = tmpData;
															toneShrinkAr[ch].m_isOldData = true;
														}
														toneShrinkAr[ch].m_lowPlayImDataIndex = -1;
														toneShrinkAr[ch].m_hiPlayImDataIndex = -1;
													}
												}
												else
												{
													toneShrinkAr[ch].m_hiData = (int)data;
													toneShrinkAr[ch].m_hiPlayImDataIndex = playImDataIndex;
												}
												break;
										}
										toneShrinkAr[ch].m_hiMilliSec = playMilliSec;
										toneShrinkAr[ch].m_lastByteRegOrder = ByteRegOrder.HI;
									}
								}
								else if (reg >= 0xB0 && reg <= 0xB2)
								{		//	B0～B2 Feebback Connect が変更されると音程レジスタの内容は保持されないらしい
									switch(reg)
									{
										case 0xB0: ch = 0; break;
										case 0xB1: ch = 1; break;
										case 0xB2: ch = 2; break;
									}
									if (playImData.m_A1 == 1) ch += 3;
									toneShrinkAr[ch].m_lastByteRegOrder = ByteRegOrder.NONE;
									toneShrinkAr[ch].m_isOldData = false;
									toneShrinkAr[ch].m_lowData = 0;
									toneShrinkAr[ch].m_lowPlayImDataIndex = -1;
									toneShrinkAr[ch].m_lowMilliSec = 0;
									toneShrinkAr[ch].m_hiData = 0;
									toneShrinkAr[ch].m_hiCovertedData = 0;
									toneShrinkAr[ch].m_hiPlayImDataIndex = -1;
									toneShrinkAr[ch].m_hiMilliSec = 0;

									if (m_targetChip.TargetChipType == ChipType.YM2612)
									{	//	YM2612 では、B0～B2 Feebback Connect が変更されると音量レジスタの内容は保持されないらしい
										sameReg.Clear();
									}
								}
								break;
							case F1ImData.PlayImType.CYCLE_WAIT:
								//	ImDataIndex 時点の楽曲再生時間をミリ秒で計算しておく
								playMilliSec += (uint)((((float)m_oneCycleNs * (float)playImData.m_cycleWait) / 1000000f));
								break;
							case F1ImData.PlayImType.END_CODE:
								break;
						}
					}
				}
			}
		}

		/// <summary>
		/// ボリュームと音程変換の初期化
		/// </summary>
		protected override void InitializeForToneAndVolume()
		{
			switch(m_targetChip.TargetChipType)
			{
				case ChipType.YM2203:
					m_fmCannelNum = OPN_FM_CH;
					m_ssgCannelNum = OPN_SSG_CH;
					m_fmOperatorNum = OPNs_OPERATOR;
					break;
				case ChipType.YM2608:
					m_fmCannelNum = OPNA_FM_CH;
					m_ssgCannelNum = OPNA_SSG_CH;
					m_fmOperatorNum = OPNs_OPERATOR;
					break;
				case ChipType.YM2610:
					m_fmCannelNum = OPNB_FM_CH;
					m_ssgCannelNum = OPNB_SSG_CH;
					m_fmOperatorNum = OPNs_OPERATOR;
					break;
				case ChipType.YM2610B:
					m_fmCannelNum = OPNB2_FM_CH;
					m_ssgCannelNum = OPNB2_SSG_CH;
					m_fmOperatorNum = OPNs_OPERATOR;
					break;
				case ChipType.YM2612:
					m_fmCannelNum = OPN2_FM_CH;
					m_ssgCannelNum = OPN2_SSG_CH;
					m_fmOperatorNum = OPNs_OPERATOR;
					break;
				case ChipType.YMF288:
					m_fmCannelNum = OPN3L_FM_CH;
					m_ssgCannelNum = OPN3L_SSG_CH;
					m_fmOperatorNum = OPNs_OPERATOR;
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
				if (m_ssgCannelNum != 0)
				{
					m_ssgToneCvDataArray = new ConvertWordToneData[m_ssgCannelNum];
					for (int i = 0; i < m_ssgCannelNum; i++) m_ssgToneCvDataArray[i] = new ConvertWordToneData();
					m_ssgEnvCvData = new ConvertWordToneData();
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
				bool isHi = false;
				uint playMilliSec = 0;
				for (int playImDataIndex = 0, l = m_imData.PlayImDataList.Count; playImDataIndex < l; playImDataIndex++)
				{
					var playImData = m_imData.PlayImDataList[playImDataIndex];
					if (playImData.m_chipSelect == m_targetChip.ChipSelect)
					{
						if (playImData.m_imType == F1ImData.PlayImType.CYCLE_WAIT)
						{	//	ImDataIndex 時点の楽曲再生時間をミリ秒で計算しておく
							playMilliSec += (uint)((((float)m_oneCycleNs * (float)playImData.m_cycleWait) / 1000000f));
							continue;
						}
						else if (playImData.m_imType == F1ImData.PlayImType.TWO_DATA)
						{
							//	SSG
							if (m_ssgCannelNum !=0)
							{
								if (playImData.m_A1 == 0)
								{
									if (playImData.m_data0 <= 0x05)
									{
										int ssgChannel = -1;
										switch(playImData.m_data0) 
										{
											case 0x00: isHi = false; ssgChannel = 0; break;
											case 0x01: isHi = true;  ssgChannel = 0; break;
											case 0x02: isHi = false; ssgChannel = 1; break;
											case 0x03: isHi = true;  ssgChannel = 1; break;
											case 0x04: isHi = false; ssgChannel = 2; break;
											case 0x05: isHi = true;  ssgChannel = 2; break;
										}
										ConvertWordTone(m_ssgToneCvDataArray[ssgChannel], ClockToneConvertType.SSG_TONE, isHi, playMilliSec, playImDataIndex);
									}
									else if (playImData.m_data0 == 0x06)
									{
										playImData.m_data1 = OPN_SSG_NoiseConvert(playImData.m_data1, playImData.m_tmpFreeData1);
									}
									else if (playImData.m_data0 == 0x0B || playImData.m_data0 == 0x0C)
									{
										isHi = playImData.m_data0 == 0x0B ? false:true;
										ConvertWordTone(m_ssgEnvCvData, ClockToneConvertType.SSG_ENV, isHi, playMilliSec, playImDataIndex);
									}
								}
							}
							if ((playImData.m_data0 >= 0xA0 && playImData.m_data0 <= 0xA2) || (playImData.m_data0 >= 0xA4 && playImData.m_data0 <= 0xA6))
							{
								int fmChannel = -1;
								switch(playImData.m_data0) 
								{
									case 0xA0: isHi = false; fmChannel = (playImData.m_A1 == 0) ? 0 : 3; break;
									case 0xA1: isHi = false; fmChannel = (playImData.m_A1 == 0) ? 1 : 4; break;
									case 0xA2: isHi = false; fmChannel = (playImData.m_A1 == 0) ? 2 : 5; break;
									case 0xA4: isHi = true;  fmChannel = (playImData.m_A1 == 0) ? 0 : 3; break;
									case 0xA5: isHi = true;  fmChannel = (playImData.m_A1 == 0) ? 1 : 4; break;
									case 0xA6: isHi = true;  fmChannel = (playImData.m_A1 == 0) ? 2 : 5; break;
								}
								ConvertWordTone(m_fmToneCvDataArray[fmChannel], ClockToneConvertType.FM_TONE, isHi, playMilliSec, playImDataIndex);
							}
						}
					}
				}
				if (m_fmCannelNum != 0)
				{
					byte fnum = 0;
					byte blk = 0;
					for (int channel = 0; channel < m_fmCannelNum; channel ++)
					{
						if (m_fmToneCvDataArray[channel].m_lowPlayImDataIndex >= 0)
						{
							OPN_FM_ToneConvert(m_fmToneCvDataArray[channel], out fnum, out blk);
							m_imData.PlayImDataList[m_fmToneCvDataArray[channel].m_lowPlayImDataIndex].m_data1 = fnum;
						}
						if (m_fmToneCvDataArray[channel].m_hiPlayImDataIndex >= 0)
						{
							OPN_FM_ToneConvert(m_fmToneCvDataArray[channel], out fnum, out blk);
							m_imData.PlayImDataList[m_fmToneCvDataArray[channel].m_hiPlayImDataIndex].m_data1 = blk;
						}
					}
				}
				if (m_ssgCannelNum != 0)
				{
					byte lo = 0;
					byte hi = 0;
					for (int channel = 0; channel < m_ssgCannelNum; channel ++)
					{
						if (m_ssgToneCvDataArray[channel].m_lowPlayImDataIndex >= 0)
						{
							OPN_SSG_ToneConvert(m_ssgToneCvDataArray[channel], out lo, out hi);
							m_imData.PlayImDataList[m_ssgToneCvDataArray[channel].m_lowPlayImDataIndex].m_data1 = lo;
						}
						if (m_ssgToneCvDataArray[channel].m_hiPlayImDataIndex >= 0)
						{
							OPN_SSG_ToneConvert(m_ssgToneCvDataArray[channel], out lo, out hi);
							m_imData.PlayImDataList[m_ssgToneCvDataArray[channel].m_hiPlayImDataIndex].m_data1 = hi;
						}
					}
					if (m_ssgEnvCvData.m_lowPlayImDataIndex >= 0)
					{
						OPN_SSG_ToneConvert(m_ssgEnvCvData, out lo, out hi);
						m_imData.PlayImDataList[m_ssgEnvCvData.m_lowPlayImDataIndex].m_data1 = lo;
					}
					if (m_ssgEnvCvData.m_hiPlayImDataIndex >= 0)
					{
						OPN_SSG_ToneConvert(m_ssgEnvCvData, out lo, out hi);
						m_imData.PlayImDataList[m_ssgEnvCvData.m_hiPlayImDataIndex].m_data1 = hi;
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
				for (int playImDataindex = 0, l = m_imData.PlayImDataList.Count; playImDataindex < l; playImDataindex++)
				{
					var playImData = m_imData.PlayImDataList[playImDataindex];
					if (playImData.m_chipSelect == m_targetChip.ChipSelect && playImData.m_imType == F1ImData.PlayImType.TWO_DATA)
					{
						if (playImData.m_data0 >= 0x40 && playImData.m_data0 <= 0x4E)
						{
							int ch = (int)(playImData.m_data0 & 0x03);
							if (playImData.m_A1 == 1) ch += 3;
							int op = 0;
							if (playImData.m_data0 >= 0x44 && playImData.m_data0 <= 0x46 ) op = 2;
							if (playImData.m_data0 >= 0x48 && playImData.m_data0 <= 0x4A ) op = 1;
							if (playImData.m_data0 >= 0x4C && playImData.m_data0 <= 0x4E ) op = 3;
							m_fmTotalLevel[ch,op] = playImData.m_data1 & 0x7F;
							m_fmTLIndex[ch,op] = playImDataindex;
						}
						else if (playImData.m_data0 >= 0xB0 && playImData.m_data0 <= 0xB2)
						{
							int ch = (int)(playImData.m_data0 & 0x03);
							if (playImData.m_A1 == 1) ch += 3;
							m_fmOpeConnect[ch] = (int)(playImData.m_data1 & 0x07);
						}
						else if (playImData.m_data0 == 0x28 && ((playImData.m_data1 & 0xF0) != 0))
						{
							switch(playImData.m_data1 & 0x07)
							{
								case 0: TotalLevelConvert(0); break;
								case 1: TotalLevelConvert(1); break;
								case 2: TotalLevelConvert(2); break;
								case 3: TotalLevelConvert(2); break;
								case 4: TotalLevelConvert(3); break;
								case 5: TotalLevelConvert(4); break;
								case 6: TotalLevelConvert(5); break;
								case 7: TotalLevelConvert(5); break;
							}
						}
					}
				}
				m_imData.CleanupPlayImDataList();
			}
			if (m_imData.SSGVol != 0 && m_ssgCannelNum > 0)
			{
				for (int index = 0, l = m_imData.PlayImDataList.Count; index < l; index++)
				{
					var playImData = m_imData.PlayImDataList[index];
					if (playImData.m_A1 == 0 && (playImData.m_data0 >= 0x08 && playImData.m_data0 <= 0x0A))
					{
						int m = ((int)playImData.m_data1) & 0x10;
						int vol = ((int)playImData.m_data1) & 0x0F;
						if (vol != 0)
						{
							vol = (int)((float)vol + (15f * ((float)(m_imData.SSGVol) / 100f)));
							vol = (vol < 0x00) ? 0x00 : ((vol > 0x0F) ? 0x0F : vol);
						}
						playImData.m_data1 = (byte)(m | vol);
					}
				}
				m_imData.CleanupPlayImDataList();
			}
		}

		/// <summary>
		///	16-Bit(WORD)音程データ変換
		/// </summary>
		private void ConvertWordTone(ConvertWordToneData wordToneData, ClockToneConvertType convertType, bool isHiByte, uint playMilliSec, int playImDataIndex)
		{
			byte hi = 0;
			byte low = 0;

			var playImData = m_imData.PlayImDataList[playImDataIndex];
			var writeRegister = playImData.m_data0;
			var writeRegData = playImData.m_data1;
			wordToneData.m_fmPrescaler = playImData.m_tmpFreeData0;
			wordToneData.m_ssgPrescaler = playImData.m_tmpFreeData1;
 			var resData = writeRegData;

			if (!isHiByte)
			{	//	下位バイトデータ
				switch(wordToneData.m_lastByteRegOrder)
				{
					case ByteRegOrder.NONE:
						//	先のバイトオーダーがない場合は、下位バイトのデータとして保持する
						wordToneData.m_lowData = (int)writeRegData;
						wordToneData.m_lowPlayImDataIndex = playImDataIndex;
						break;
					case ByteRegOrder.HI:
						//	先のバイトオーダーが上位バイトデータの場合、
						if (wordToneData.m_hiPlayImDataIndex >= 0)
						{	//	上位バイトの PlayImData が保持されている場合、
							//	下位バイトのデータとして保持し、
							wordToneData.m_lowData = (int)writeRegData;
							//	上位下位のデータが揃ったとして音程変換を実行
							switch(convertType)
							{
								case ClockToneConvertType.FM_TONE:
									OPN_FM_ToneConvert(wordToneData, out low, out hi);
									break;
								case ClockToneConvertType.SSG_TONE:
									OPN_SSG_ToneConvert(wordToneData, out low, out hi);
									break;
								case ClockToneConvertType.SSG_ENV:
									OPN_SSG_EnvConvert(wordToneData, out low, out hi);
									break;
							}
							//	上位バイトの PlayImData の書き込み値を、変換後の上位バイトの値に書き変え
							m_imData.PlayImDataList[wordToneData.m_hiPlayImDataIndex].m_data1 = hi;
							//	上位バイトの 変換後の値を保持して
							wordToneData.m_hiCovertedData = hi;
							//	上位下位の両方の片割れ PlayImData インデックスは無効にする
							wordToneData.m_lowPlayImDataIndex = -1;
							wordToneData.m_hiPlayImDataIndex = -1;
							resData = low;
						}
						else
						{	//	上位バイトの PlayImData が保持されていない場合、
							//	下位バイトのデータとして保持する
							wordToneData.m_lowData = (int)writeRegData;
							wordToneData.m_lowPlayImDataIndex = playImDataIndex;
						}
						break;
					case ByteRegOrder.LOW:
						//	先のバイトオーダーが下位バイトデータの場合、
						if (wordToneData.m_lowPlayImDataIndex >= 0)
						{	//	下位バイトの PlayImData が保持されている場合、
							//	上位バイトは以前の値のまま、下位バイトだけでの音程指定とみなして
							//	上位下位のデータが揃ったとして音程変換を実行
							switch(convertType)
							{
								case ClockToneConvertType.FM_TONE:
									OPN_FM_ToneConvert(wordToneData, out low, out hi);
									break;
								case ClockToneConvertType.SSG_TONE:
									OPN_SSG_ToneConvert(wordToneData, out low, out hi);
									break;
								case ClockToneConvertType.SSG_ENV:
									OPN_SSG_EnvConvert(wordToneData, out low, out hi);
									break;
							}
							//	下位バイトの PlayImData の書き込み値を、変換後の下位バイトの値に書き変え
							m_imData.PlayImDataList[wordToneData.m_lowPlayImDataIndex].m_data1 = low;
							//	変換後の上位バイトの値が、音程変換で先の値と変わってしまった場合
							if (wordToneData.m_hiCovertedData != hi)
							{	//	変換後の上位バイトの値を音源レジスタに書き込むため、PlayImData に挿入でデータを追加する
								m_imData.SetInsertPlayImData(	wordToneData.m_lowPlayImDataIndex+1, 
																F1ImData.PlayImType.TWO_DATA, 
																chipSelect:m_targetChip.ChipSelect, 
																a1:playImData.m_A1, 
																data0:(byte)(writeRegister+1), 
																data1:hi);
								//	上位バイトの 変換後の値を保持
								wordToneData.m_hiCovertedData = hi;
							}
						}
						//	下位バイトのデータを保持する
						wordToneData.m_lowData = (int)writeRegData;
						wordToneData.m_lowPlayImDataIndex = playImDataIndex;
						break;
				}
				//	PlayImDataIndex時点での楽曲再生時間をミリ秒で保持し、バイトオーダーが下位バイトとして保持する
				wordToneData.m_lowMilliSec = playMilliSec;
				wordToneData.m_lastByteRegOrder = ByteRegOrder.LOW;
			}
			else
			{	//	上位バイトデータ
				switch(wordToneData.m_lastByteRegOrder)
				{
					case ByteRegOrder.NONE:
					case ByteRegOrder.HI:
						//	先のバイトオーダーがないか上位バイトの場合は、上位バイトのデータとして保持する
						wordToneData.m_hiData = (int)writeRegData;
						wordToneData.m_hiPlayImDataIndex = playImDataIndex;
						break;
					case ByteRegOrder.LOW:
						//	先のバイトオーダーが下位バイトデータの場合、
						if (wordToneData.m_lowPlayImDataIndex >= 0)
						{	//	下位バイトの PlayImData が保持されている場合、
							//	下位バイトの次に下位バイトなので、
							//	先の下位バイトが、以前の音程向けか、新たな音程向けか、
							//	以前の音程変更の時間が５ミリ秒以上であれば、新たな音程向けとして処理する
							uint d_milli = playMilliSec - wordToneData.m_lowMilliSec;
							if (d_milli >= 5)
							{	//	先の下位バイトが以前の音程向けと見なして音程変換を実行
								switch(convertType)
								{
									case ClockToneConvertType.FM_TONE:
										OPN_FM_ToneConvert(wordToneData, out low, out hi);
										break;
									case ClockToneConvertType.SSG_TONE:
										OPN_SSG_ToneConvert(wordToneData, out low, out hi);
										break;
									case ClockToneConvertType.SSG_ENV:
										OPN_SSG_EnvConvert(wordToneData, out low, out hi);
										break;
								}
								//	変換後の上位バイトの値が、音程変換で先の値と変わってしまった場合
								if (wordToneData.m_hiCovertedData != hi)
								{	//	変換後の上位バイトの値を音源レジスタに書き込むため、PlayImData に挿入でデータを追加する
									m_imData.SetInsertPlayImData(	wordToneData.m_lowPlayImDataIndex+1, 
																	F1ImData.PlayImType.TWO_DATA, 
																	chipSelect:m_targetChip.ChipSelect, 
																	a1:playImData.m_A1, 
																	data0:(byte)(writeRegister), 
																	data1:hi);
									//	上位バイトの 変換後の値を保持
									wordToneData.m_hiCovertedData = hi;
								}
								//	下位バイトの PlayImData の書き込み値を、変換後の下位バイトの値に書き変え
								m_imData.PlayImDataList[wordToneData.m_lowPlayImDataIndex].m_data1 = low;
								//	上位バイトのデータを保持する
								wordToneData.m_hiData = (int)writeRegData;
								wordToneData.m_hiPlayImDataIndex = playImDataIndex;
							}
							else
							{	//	先の下位バイトは、新たな音程向けと見なして上位バイトを使って音程変換を実行
								wordToneData.m_hiData = (int)writeRegData;
								switch(convertType)
								{
									case ClockToneConvertType.FM_TONE:
										OPN_FM_ToneConvert(wordToneData, out low, out hi);
										break;
									case ClockToneConvertType.SSG_TONE:
										OPN_SSG_ToneConvert(wordToneData, out low, out hi);
										break;
									case ClockToneConvertType.SSG_ENV:
										OPN_SSG_EnvConvert(wordToneData, out low, out hi);
										break;
								}
								wordToneData.m_hiCovertedData = hi;
								m_imData.PlayImDataList[wordToneData.m_lowPlayImDataIndex].m_data1 = low;
								//	上位下位の両方の片割れ PlayImData インデックスは無効にする
								wordToneData.m_lowPlayImDataIndex = -1;
								wordToneData.m_hiPlayImDataIndex = -1;
								resData = hi;
							}
						}
						else
						{	//	下位バイトの PlayImData が保持されていない合、
							//	上位バイトのデータを保持する
							wordToneData.m_hiData = (int)writeRegData;
							wordToneData.m_hiPlayImDataIndex = playImDataIndex;
						}
					break;
				}
				//	PlayImDataIndex時点での楽曲再生時間をミリ秒で保持し、バイトオーダーが上位バイトとして保持する
				wordToneData.m_hiMilliSec = playMilliSec;
				wordToneData.m_lastByteRegOrder = ByteRegOrder.HI;
			}
			//	PlayImData の レジスタ書き込みデータを計算した値に書き変える
			playImData.m_data1 = resData;
		}

		/// <summary>
		///	OPN の FnumberとBlock変換
		/// </summary>
		private void OPN_FM_ToneConvert(ConvertWordToneData toneCvData, out byte fnum, out byte blk)
		{
			float hardClock = ((float)m_targetChip.TargetChipClock);
			float playClock = ((float)m_targetChip.SourceChipClock);
			float play_preScaler = (float)toneCvData.m_fmPrescaler;
			float pow2_20 = (float)1048576;				//	2^20(0x100000)
			float hard_ch_num = (float)m_fmCannelNum;
			float hard_prescaler;
			switch(m_targetChip.TargetChipType)
			{
				case ChipType.YMF288:
				case ChipType.YM2610:
				case ChipType.YM2610B:
				case ChipType.YM2612:
					hard_prescaler = FIX_FM_PRESCALER;
					break;
				default:
					hard_prescaler = (float)toneCvData.m_fmPrescaler;
					break;
			}
			float play_ch_num; 
			switch(m_targetChip.SourceChipType)
			{
				case ChipType.YM2203:
					play_ch_num = (float)OPN_FM_CH;
					break;
				default:
					play_ch_num = (float)m_fmCannelNum;
					break;
			}
			float hard_coef = m_fmOperatorNum * hard_ch_num * hard_prescaler * pow2_20 / 2f;
			float play_coef = m_fmOperatorNum * play_ch_num * play_preScaler * pow2_20 / 2f;
			int i_blk = ((toneCvData.m_hiData & 0x038) >> 3) & 0x07;
			float f_blk = (float)Math.Pow(2,(double)((i_blk-1) & 0x07));
			int i_fnum = (((toneCvData.m_hiData & 0x07) << 8) & 0x700) | (toneCvData.m_lowData & 0xFF);
			//	再生データの音程周波数を求める
			float f_tone = ((float)i_fnum) / ( play_coef * (1f / (playClock)) * (1f / f_blk) );
			while(true)
			{
				//	再生ハードで音程周波数を出すための F_Number を求める
				i_fnum = (int)(f_tone * hard_coef / hardClock / f_blk);
				if (i_fnum > 0x7FF)
				{
					if (i_blk >= 0x07)
					{
						i_fnum = 0x7FF;
						break;
					}
					if (i_blk <= 0x0)
					{
						i_fnum = 0x0;
						break;
					}
					if (m_targetChip.TargetChipClock > m_targetChip.SourceChipClock) i_blk -= 1; else i_blk += 1;
					f_blk = (float)Math.Pow(2,(double)((i_blk-1) & 0x07));
				}
				else
				{
					break;
				}
			}
			//	Block と F-Number のレジスタデータを組みなおして返す
			i_blk = ((i_blk << 3) &0x38) | ((i_fnum >> 8) & 0x07);
			i_fnum &= 0xFF;
			fnum = (byte)i_fnum;
			blk = (byte)i_blk;
		}

		/// <summary>
		///	SSG の Tone変換
		/// </summary>
		private void OPN_SSG_ToneConvert(ConvertWordToneData toneCvData, out byte fTune, out byte cTune)
		{
			int sTune = ((toneCvData.m_hiData << 8) & 0x0F00 ) | (toneCvData.m_lowData & 0xFF);
			if (sTune == 0)
			{
				cTune = (byte)toneCvData.m_hiData;
				fTune = (byte)toneCvData.m_lowData;
				return;
			}
			float hard_prescaler;
			switch(m_targetChip.TargetChipType)
			{
				case ChipType.YM2203:
					hard_prescaler = (float)toneCvData.m_ssgPrescaler;
					break;
				case ChipType.YM2608:
					hard_prescaler = (float)toneCvData.m_ssgPrescaler * 2f;
					break;
				default:
					hard_prescaler = (float)FIX_SSG_PRESCALER * 2f;
					break;
			}
			float hardClock = ((float)m_targetChip.TargetChipClock) / (hard_prescaler) * 2f;
			float playClock = ((float)m_targetChip.SourceChipClock); 
			switch(m_targetChip.SourceChipType)
			{
				case ChipType.YM2203:
					playClock /= (((float)toneCvData.m_ssgPrescaler) * 2f);
					break;
				default:
					break;
			}
			float tone = playClock / (((float)sTune) * 16f);
			int dTune = (int)((hardClock / tone) /16f); 
			cTune = (byte)((dTune >> 8) & 0x0F);
			fTune = (byte)(dTune & 0xFF); 
		}

		/// <summary>
		///	SSG の Envlope変換
		/// </summary>
		private void OPN_SSG_EnvConvert(ConvertWordToneData toneCvData, out byte fTune, out byte cTune)
		{
			int sEnv = ((toneCvData.m_hiData << 8) & 0xFF00) | (toneCvData.m_lowData & 0xFF);
			if (sEnv == 0)
			{
				cTune = (byte)toneCvData.m_hiData;
				fTune = (byte)toneCvData.m_lowData;
				return;
			}
			float hard_prescaler;
			switch(m_targetChip.TargetChipType)
			{
				case ChipType.YM2203:
					hard_prescaler = (float)toneCvData.m_ssgPrescaler;
					break;
				case ChipType.YM2608:
					hard_prescaler = (float)toneCvData.m_ssgPrescaler * 2f;
					break;
				default:
					hard_prescaler = (float)FIX_SSG_PRESCALER * 2f;
					break;
			}
			float hardClock = ((float)m_targetChip.TargetChipClock) / (hard_prescaler) * 2f;
			float playClock = ((float)m_targetChip.SourceChipClock);
			switch(m_targetChip.SourceChipType)
			{
				case ChipType.YM2203:
					playClock /= (((float)toneCvData.m_ssgPrescaler) * 2f);
					break;
				default:
					break;
			}
			float env = (((float)sEnv) * 256f) / playClock;
			int dEnv = (int)((hardClock * env) / 256f); 
			cTune = (byte)((dEnv >> 8) & 0xFF);
			fTune = (byte)(dEnv & 0xFF); 
		}

		/// <summary>
		///	SSG の Noise変換
		/// </summary>
		private byte OPN_SSG_NoiseConvert(int sNoise, int ssgPrescaler)
		{
			if (sNoise == 0)
			{
				return (byte)0x00;
			}
			float hard_prescaler;
			switch(m_targetChip.TargetChipType)
			{
				case ChipType.YM2203:
					hard_prescaler = (float)ssgPrescaler;
					break;
				case ChipType.YM2608:
					hard_prescaler = (float)ssgPrescaler * 2f;
					break;
				default:
					hard_prescaler = (float)FIX_SSG_PRESCALER * 2f;
					break;
			}
			float hardClock = ((float)m_targetChip.TargetChipClock) / (hard_prescaler) * 2f;
			float playClock = (float)m_targetChip.SourceChipClock; 
			switch(m_targetChip.SourceChipType)
			{
				case ChipType.YM2203:
					playClock /= (((float)ssgPrescaler) * 2f);
					break;
				default:
					break;
			}
			float noise = playClock / (((float)sNoise) * 16f);
			int dNoise = (int)((hardClock / noise) /16f); 
			return (byte)(dNoise & 0x1F);
		}

		/// <summary>
		/// リズムレジスタチェック	YM2608
		/// </summary>
		private bool CheckRhythmRegister_YM2608(F1ImData.PlayImData playImData)
		{
			if (playImData.m_A1 == 0 && (playImData.m_data0 >= 0x10 && playImData.m_data0 <= 0x1F))
			{
				playImData.m_imType = F1ImData.PlayImType.NONE;
				return false;
			}
			return true;
		}

		/// <summary>
		/// PCMレジスタチェック	YM2608
		/// </summary>
		private bool CheckPcmRegister_YM2608(F1ImData.PlayImData playImData, bool isPcmActive)
		{
			if (!isPcmActive && playImData.m_A1 == 1 && playImData.m_data0 <= 0x10)
			{
				playImData.m_imType = F1ImData.PlayImType.NONE;
				return false;
			}
			return true;
		}

		/// <summary>
		/// PCMレジスタチェック	YM2610	YM2610B
		/// </summary>
		private bool CheckPcmRegister_YM2610s(F1ImData.PlayImData PlayImData, bool isPcmActive)
		{
			if (!isPcmActive)
			{
				if (PlayImData.m_A1 != 0)
				{
					if (PlayImData.m_data0 >= 0x00 && PlayImData.m_data0 <= 0x2D)
					{
						PlayImData.m_imType = F1ImData.PlayImType.NONE;
						return false;
					}
				}
				else
				{
					if (PlayImData.m_data0 >= 0x10 && PlayImData.m_data0 <= 0x1C)
					{
						PlayImData.m_imType = F1ImData.PlayImType.NONE;
						return false;
					}
				}
			}
			return true;
		}

		/// <summary>
		/// PCMレジスタチェック	YM2612
		/// </summary>
		private bool CheckPcmRegister_YM2612(F1ImData.PlayImData PlayImData, bool isPcmActive)
		{
			if (!isPcmActive)
			{	//	2B:DAC_SECELCT(bit7,CAH6:0=FM 1=DAC) と 2A:DAC_DATA への出力を破棄する
				if (PlayImData.m_A1 == 0 && ((PlayImData.m_data0 == 0x2A || PlayImData.m_data0 == 0x2B)))
				{
					PlayImData.m_imType = F1ImData.PlayImType.NONE;
					return false;
				}
			}
			else
			{
				if (PlayImData.m_A1 == 0)
				{
					//	2B:DAC_SECELCT(bit7,CAH6:0=FM 1=DAC)
					if (PlayImData.m_data0 == 0x2B)
					{	//	bit7 だけにして PLAY中間データに戻す
						byte dacSel = (byte)(PlayImData.m_data1 & 0x80);
						PlayImData.m_data1 = dacSel;
						//	2BState が先の情報を保持していて同一ならば 出力を破棄する。（2BState は 7Fで初期化されている）
						if (m_OPN2reg2BState != 0x7F && m_OPN2reg2BState == dacSel) 
						{
							PlayImData.m_imType = F1ImData.PlayImType.NONE;
							return false;
						}
						//	2BState に保持させる
						m_OPN2reg2BState = dacSel;
					}
				}
			}
			return true;
		}

		/// <summary>
		///	トータルレベル変換
		/// </summary>
		private void TotalLevelConvert(int ch)
		{
			int[] connectTable = { 0b1000, 0b1000, 0b1000, 0b1000, 0b1010, 0b1110, 0b1110, 0b1111	};
			int bitFlag = connectTable[m_fmOpeConnect[ch]];
			for (int ope = 0; ope < m_fmOperatorNum; ope++)
			{
				if ((bitFlag & 0x01) != 0)
				{
					int tl = 0x7F - m_fmTotalLevel[ch,ope];
					if (tl != 0)
					{
						tl = (int)((float)tl + (128f * ((float)(m_imData.FMVol) / 100f)));
						tl = (tl < 0x00) ? 0x00 : ((tl > 0x7F) ? 0x7F : tl);
					}
					tl = 0x7F - tl;
					m_imData.PlayImDataList[m_fmTLIndex[ch,ope]].m_data1 = (byte)tl;
				}
				bitFlag = bitFlag >> 1;
			}
		}

		/// <summary>
		/// 未実装レジスタチェック	AY-3-8910
		/// </summary>
		private bool CheckRegister_AY_3_8910(F1ImData.PlayImData playImData)
		{
			if (playImData.m_A1 > 0)
			{
				playImData.m_imType = F1ImData.PlayImType.NONE;
				return false;
			}
			if (playImData.m_data0 >= 0x0E)
			{	//	IOA,IOB 出力は NC とする
				playImData.m_imType = F1ImData.PlayImType.NONE;
				return false;
			}
			return true;
		}

		/// <summary>
		/// 未実装レジスタチェック	YM2149
		/// </summary>
		private bool CheckRegister_YM2149(F1ImData.PlayImData playImData)
		{
			if (playImData.m_A1 > 0)
			{
				playImData.m_imType = F1ImData.PlayImType.NONE;
				return false;
			}
			if (playImData.m_data0 >= 0x0E)
			{	//	IOA,IOB 出力は NC とする
				playImData.m_imType = F1ImData.PlayImType.NONE;
				return false;
			}
			return true;
		}

		/// <summary>
		/// 未実装レジスタとDUAL対応レジスタチェック	YM2203
		/// </summary>
		private bool CheckRegister_YM2203(F1ImData.PlayImData playImData, bool isDualIn1Chip)
		{
			if (!isDualIn1Chip && playImData.m_A1 == 1)
			{
				playImData.m_imType = F1ImData.PlayImType.NONE;
				return false;
			}
			var ncRegs = YM2203_NC_REG;
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
		/// 未実装レジスタチェック	YM2608
		/// </summary>
		private bool CheckRegister_YM2608(F1ImData.PlayImData playImData)
		{
			var ncRegs = (playImData.m_A1 == 1) ? YM2608_NC_REG_A1_1 : YM2608_NC_REG_A1_0;
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
		/// 未実装レジスタチェック	YM2610
		/// </summary>
		private bool CheckRegister_YM2610(F1ImData.PlayImData playImData)
		{
			var ncRegs = (playImData.m_A1 == 1) ? YM2610_NC_REG_A1_1 : YM2610_NC_REG_A1_0;
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
		/// 未実装レジスタとチェック	YM2610B
		/// </summary>
		private bool CheckRegister_YM2610B(F1ImData.PlayImData playImData)
		{
			var ncRegs = (playImData.m_A1 == 1) ? YM2610B_NC_REG_A1_1 : YM2610B_NC_REG_A1_0;
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
		/// 未実装レジスタとチェック	YM2612
		/// </summary>
		private bool CheckRegister_YM2612(F1ImData.PlayImData playImData)
		{
			var ncRegs = (playImData.m_A1 == 1) ? YM2612_NC_REG_A1_1 : YM2612_NC_REG_A1_0;
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
		/// 未実装レジスタチェック	YMF288
		/// </summary>
		private bool CheckRegister_YMF288(F1ImData.PlayImData playImData)
		{
			var ncRegs = (playImData.m_A1 == 1) ? YMF288_NC_REG_A1_1 : YMF288_NC_REG_A1_0;
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
		///	YM2203	未実装レジスタ
		/// </summary>
		private readonly byte[] YM2203_NC_REG = 
		{
			0x0E, 0x0F,	//	0x0E,0x0F I/Port AB
			0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,	0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
			0x20, 0x21,	//	0x21 LSI Test
			0x22, 0x23, 0x29, 0x2A, 0x2B, 0x2C,
			0x33, 0x37, 0x3B, 0x3F,
			0x43, 0x47, 0x4B, 0x4F,
			0x53, 0x57, 0x5B, 0x5F,
			0x63, 0x67, 0x6B, 0x6F,
			0x73, 0x77, 0x7B, 0x7F,
			0x83, 0x87, 0x8B, 0x8F,
			0x93, 0x97, 0x9B, 0x9F,
			0xA3, 0xA7, 0xAB, 0xAF,
			0xB3, 0xB4, 0xB5, 0xB6, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF,
			0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
			0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
			0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
			0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF,
		};
		/// <summary>
		///	YM2608	未実装レジスタ
		/// </summary>
		private readonly byte[] YM2608_NC_REG_A1_0 = 
		{
			0x0E, 0x0F,	//	0x0E,0x0F I/Port AB
			0x13, 0x14, 0x15, 0x16, 0x17, 0x1E, 0x1F,
			0x20, 0x21,	//	0x21 LSI Test
			0x23, 0x2A, 0x2B, 0x2C, 
			0x33, 0x37, 0x3B, 0x3F,
			0x43, 0x47, 0x4B, 0x4F,
			0x53, 0x57, 0x5B, 0x5F,
			0x63, 0x67, 0x6B, 0x6F,
			0x73, 0x77, 0x7B, 0x7F,
			0x83, 0x87, 0x8B, 0x8F,
			0x93, 0x97, 0x9B, 0x9F,
			0xA3, 0xA7, 0xAB, 0xAF,
			0xB3, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF,
			0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
			0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
			0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
			0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF,
		};
		private readonly byte[] YM2608_NC_REG_A1_1 = 
		{
			0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
			0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27,	0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F,
			0x33, 0x37, 0x3B, 0x3F,
			0x43, 0x47, 0x4B, 0x4F,
			0x53, 0x57, 0x5B, 0x5F,
			0x63, 0x67, 0x6B, 0x6F,
			0x73, 0x77, 0x7B, 0x7F,
			0x83, 0x87, 0x8B, 0x8F,
			0x93, 0x97, 0x9B, 0x9F,
			0xA3, 0xA7, 0xAB, 0xAF,
			0xB3, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF,
			0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
			0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
			0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
			0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF,
		};
		/// <summary>
		///	YM2612	未実装レジスタ
		/// </summary>
		private readonly byte[] YM2612_NC_REG_A1_0 = 
		{
			0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,	0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
			0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,	0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
			0x20, 0x21,	//	0x21 LSI Test
			0x23, 0x29, //	0x2C LSI Test
			0x2D, 0x2E, 0x2F,
			0x33, 0x37, 0x3B, 0x3F,
			0x43, 0x47, 0x4B, 0x4F,
			0x53, 0x57, 0x5B, 0x5F,
			0x63, 0x67, 0x6B, 0x6F,
			0x73, 0x77, 0x7B, 0x7F,
			0x83, 0x87, 0x8B, 0x8F,
			0x93, 0x97, 0x9B, 0x9F,
			0xA3, 0xA7, 0xAB, 0xAF,
			0xB3, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF,
			0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
			0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
			0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
			0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF,
		};
		private readonly byte[] YM2612_NC_REG_A1_1 = 
		{
			0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,	0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
			0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,	0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
			0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27,	0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F,
			0x33, 0x37, 0x3B, 0x3F,
			0x43, 0x47, 0x4B, 0x4F,
			0x53, 0x57, 0x5B, 0x5F,
			0x63, 0x67, 0x6B, 0x6F,
			0x73, 0x77, 0x7B, 0x7F,
			0x83, 0x87, 0x8B, 0x8F,
			0x93, 0x97, 0x9B, 0x9F,
			0xA3, 0xA7, 0xAB, 0xAF,
			0xB3, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF,
			0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
			0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
			0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
			0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF,
		};
		/// <summary>
		///	YM2610	ＦＭ音源部の未実装レジスタ
		/// </summary>
		private readonly byte[] YM2610_NC_REG_A1_0 =
		{
			0x0E, 0x0F,
			0x16, 0x17, 0x18, 0x1D, 0x1E, 0x1F,
			0x20, 0x23, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F,
			0x30, 0x33, 0x34, 0x37, 0x38, 0x3B, 0x3C, 0x3F,
			0x40, 0x43, 0x44, 0x47, 0x48, 0x4B, 0x4C, 0x4F,
			0x50, 0x53, 0x54, 0x57, 0x58, 0x5B, 0x5C, 0x5F,
			0x60, 0x63, 0x64, 0x67, 0x68, 0x6B, 0x6C, 0x6F,
			0x70, 0x73, 0x74, 0x77, 0x78, 0x7B, 0x7C, 0x7F,
			0x80, 0x83, 0x84, 0x87, 0x88, 0x8B, 0x8C, 0x8F,
			0x90, 0x93, 0x94, 0x97, 0x98, 0x9B, 0x9C, 0x9F,
			0xA0, 0xA3, 0xA4, 0xA7, 0xA8, 0xAB, 0xAC, 0xAF,
			0xB0, 0xB3, 0xB4, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF,
			0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
			0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
			0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
			0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF,
		};
		private readonly byte[] YM2610_NC_REG_A1_1 =
		{
			0x03, 0x04, 0x05, 0x06, 0x07, 0x0E, 0x0F,
			0x16, 0x17, 0x1E, 0x1F,
			0x26, 0x27, 0x2E, 0x2F,
			0x30, 0x33, 0x34, 0x37, 0x38, 0x3B, 0x3C, 0x3F,
			0x40, 0x43, 0x44, 0x47, 0x48, 0x4B, 0x4C, 0x4F,
			0x50, 0x53, 0x54, 0x57, 0x58, 0x5B, 0x5C, 0x5F,
			0x60, 0x63, 0x64, 0x67, 0x68, 0x6B, 0x6C, 0x6F,
			0x70, 0x73, 0x74, 0x77, 0x78, 0x7B, 0x7C, 0x7F,
			0x80, 0x83, 0x84, 0x87, 0x88, 0x8B, 0x8C, 0x8F,
			0x90, 0x93, 0x94, 0x97, 0x98, 0x9B, 0x9C, 0x9F,
			0xA0, 0xA3, 0xA4, 0xA7, 0xA8, 0xAB, 0xAC, 0xAF,
			0xB0, 0xB3, 0xB4, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF,
			0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
			0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
			0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
			0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF,
		};
		/// <summary>
		///	YM2610B	ＦＭ音源部の未実装レジスタ
		/// </summary>
		private readonly byte[] YM2610B_NC_REG_A1_0 =
		{
			0x0E, 0x0F,
			0x16, 0x17, 0x18, 0x1D, 0x1E, 0x1F,
			0x20, 0x23, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F,
			0x33, 0x37, 0x3B, 0x3F,
			0x43, 0x47, 0x4B, 0x4F,
			0x53, 0x57, 0x5B, 0x5F,
			0x63, 0x67, 0x6B, 0x6F,
			0x73, 0x77, 0x7B, 0x7F,
			0x83, 0x87, 0x8B, 0x8F,
			0x93, 0x97, 0x9B, 0x9F,
			0xA3, 0xA7, 0xAB, 0xAF,
			0xB3, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF,
			0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
			0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
			0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
			0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF,
		};
		private readonly byte[] YM2610B_NC_REG_A1_1 =
		{
			0x03, 0x04, 0x05, 0x06, 0x07, 0x0E, 0x0F,
			0x16, 0x17, 0x1E, 0x1F,
			0x26, 0x27, 0x2E, 0x2F,
			0x33, 0x37, 0x3B, 0x3F,
			0x43, 0x47, 0x4B, 0x4F,
			0x53, 0x57, 0x5B, 0x5F,
			0x63, 0x67, 0x6B, 0x6F,
			0x73, 0x77, 0x7B, 0x7F,
			0x83, 0x87, 0x8B, 0x8F,
			0x93, 0x97, 0x9B, 0x9F,
			0xA3, 0xA7, 0xAB, 0xAF,
			0xB3, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF,
			0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
			0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
			0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
			0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF,
		};
		/// <summary>
		///	YMF288	未実装レジスタ
		/// </summary>
		private readonly byte[] YMF288_NC_REG_A1_0 = 
		{
			0x0E, 0x0F,	//	0x0E,0x0F I/Port AB
			0x13, 0x14, 0x15, 0x16, 0x17, 0x1E, 0x1F,
			0x20, 0x21,	//	0x21 LSI Test
			0x23, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F,
			0x33, 0x37, 0x3B, 0x3F,
			0x43, 0x47, 0x4B, 0x4F,
			0x53, 0x57, 0x5B, 0x5F,
			0x63, 0x67, 0x6B, 0x6F,
			0x73, 0x77, 0x7B, 0x7F,
			0x83, 0x87, 0x8B, 0x8F,
			0x93, 0x97, 0x9B, 0x9F,
			0xA3, 0xA7, 0xAB, 0xAF,
			0xB3, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF,
			0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
			0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
			0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
			0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF,
		};
		private readonly byte[] YMF288_NC_REG_A1_1 = 
		{
			0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,	0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
			0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17,	0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F,
			0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27,	0x28, 0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F,
			0x33, 0x37, 0x3B, 0x3F,
			0x43, 0x47, 0x4B, 0x4F,
			0x53, 0x57, 0x5B, 0x5F,
			0x63, 0x67, 0x6B, 0x6F,
			0x73, 0x77, 0x7B, 0x7F,
			0x83, 0x87, 0x8B, 0x8F,
			0x93, 0x97, 0x9B, 0x9F,
			0xA3, 0xA7, 0xAB, 0xAF,
			0xB3, 0xB7, 0xB8, 0xB9, 0xBA, 0xBB, 0xBC, 0xBD, 0xBE, 0xBF,
			0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF,
			0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF,
			0xE0, 0xE1, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xEB, 0xEC, 0xED, 0xEE, 0xEF,
			0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFB, 0xFC, 0xFD, 0xFE, 0xFF,
		};

	}
}