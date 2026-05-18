using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace F1
{
	/// <summary>
	///	CHIP PSG クラス
	/// </summary>
	public class Chip_PSG : SoundChip
	{
		private int m_reg07State;

		private int PSG_CH { get; } = 3;

		private enum ClockToneConvertType
		{
			PSG_TONE,
			PSG_ENV,
		}

		private ConvertWordToneData[] m_psgToneCvDataArray;
		private ConvertWordToneData m_psgEnvCvData;

		/// <summary>
		/// レジスタ操作にプレイ CHIP にあわせた制御を入れる
		/// </summary>
		public override void ControlPlayChipRegiser()
		{
			m_reg07State = 0xC0;

			foreach(var playImData in m_imData.PlayImDataList.Where(x => x.m_chipSelect == m_targetChip.ChipSelect && x.m_imType == F1ImData.PlayImType.TWO_DATA))
			{	//	NC Register.
				if (playImData.m_data0 >= 0x0E)
				{
					playImData.m_imType = F1ImData.PlayImType.NONE;
				}
				else if (playImData.m_data0 == 0x07)
				{
					byte md = (byte)(playImData.m_data1 & 0x3F);
					playImData.m_data1 = md;
					if (m_reg07State != 0xC0 && m_reg07State == md) 
					{
						playImData.m_imType = F1ImData.PlayImType.NONE;
					}
					m_reg07State = md;
				}
			}
			m_imData.CleanupPlayImDataList();
		}

		/// <summary>
		/// 重複するレジスタ操作の撤去などでサイズを抑える
		/// </summary>
		public override void ShrinkPlayChipRegiser()
		{
			if (m_imData.IsShrink)
			{
				uint reg = 0;
				uint data = 0;

				Dictionary<uint, uint> sameReg = new Dictionary<uint, uint>();
				foreach(var playImData in m_imData.PlayImDataList.Where(x => x.m_chipSelect == m_targetChip.ChipSelect && x.m_imType == F1ImData.PlayImType.TWO_DATA))
				{
					reg = (uint)playImData.m_data0;
					data = (uint)playImData.m_data1;
					if ( 
						(reg == 0x06) || 	//	PSG	ノイズ周波数
						(reg == 0x07) || 	//	PSG	IOA,Bの方向/トーンとノイズのミキシング設定
						(reg == 0x08) || 	//	PSG	チャンネルＡのエンベロープ有効 or 固定音量
						(reg == 0x09) || 	//	PSG	チャンネルＢのエンベロープ有効 or 固定音量
						(reg == 0x0A) 	 	//	PSG	チャンネルＣのエンベロープ有効 or 固定音量
					)
					{	//	連続して同じ値を書いている場合は、削除する
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
				}
			}
			m_imData.CleanupPlayImDataList();
		}
		/// <summary>
		/// ボリュームと音程変換の初期化
		/// </summary>
		protected override void InitializeForToneAndVolume()
		{
			if (m_imData.IsToneAdjust && CheckAdjustClock())
			{
				m_psgToneCvDataArray = new ConvertWordToneData[PSG_CH];
				for (int i = 0; i < PSG_CH; i++) m_psgToneCvDataArray[i] = new ConvertWordToneData();
				m_psgEnvCvData = new ConvertWordToneData();
			}
		}
		/// <summary>
		/// レジスタ操作にクロックにあわせた音程制御を入れる
		/// </summary>
		public override void ToneConvert()
		{
			if (m_imData.IsToneAdjust && CheckAdjustClock())
			{
				bool isHiByte = false;
				int ch = 0;
				uint playMilliSec = 0;
				for (int playImDataIndex = 0, l = m_imData.PlayImDataList.Count; playImDataIndex < l; playImDataIndex++)
				{
					var playImData = m_imData.PlayImDataList[playImDataIndex];
					if (playImData.m_chipSelect == m_targetChip.ChipSelect)
					{
						if (playImData.m_imType == F1ImData.PlayImType.CYCLE_WAIT)
						{	//	ImDataIndex 時点の楽曲再生時間をミリ秒計算しておく
							playMilliSec += (uint)((((float)m_oneCycleNs * (float)playImData.m_cycleWait) / 1000000f));
							continue;
 						}
						else if (playImData.m_imType == F1ImData.PlayImType.TWO_DATA)
						{
							if (playImData.m_data0 <= 0x05)
							{
								switch(playImData.m_data0) 
								{
									case 0x00: isHiByte = false; ch = 0; break;
									case 0x01: isHiByte = true;  ch = 0; break;
									case 0x02: isHiByte = false; ch = 1; break;
									case 0x03: isHiByte = true;  ch = 1; break;
									case 0x04: isHiByte = false; ch = 2; break;
									case 0x05: isHiByte = true;  ch = 2; break;
								}
								ConvertWordTone(m_psgToneCvDataArray[ch], ClockToneConvertType.PSG_TONE, isHiByte, playMilliSec, playImDataIndex, playImData);
							}
							else if (playImData.m_data0 == 0x06)
							{
								playImData.m_data1 = PSG_NoiseConvert(playImData.m_data1);
							}
							else if (playImData.m_data0 == 0x0B || playImData.m_data0 == 0x0C)
							{
								isHiByte = playImData.m_data0 == 0x0B ? false:true;
								ConvertWordTone(m_psgEnvCvData, ClockToneConvertType.PSG_ENV, isHiByte, playMilliSec, playImDataIndex, playImData);
							}
						}
					}
				}
				byte lo = 0;
				byte hi = 0;
				for (int channel = 0; channel < PSG_CH; channel ++)
				{
					if (m_psgToneCvDataArray[channel].m_lowPlayImDataIndex >= 0)
					{
						PSG_ToneConvert(m_psgToneCvDataArray[channel], out lo, out hi);
						m_imData.PlayImDataList[m_psgToneCvDataArray[channel].m_lowPlayImDataIndex].m_data1 = lo;
					}
					if (m_psgToneCvDataArray[channel].m_hiPlayImDataIndex >= 0)
					{
						PSG_ToneConvert(m_psgToneCvDataArray[channel], out lo, out hi);
						m_imData.PlayImDataList[m_psgToneCvDataArray[channel].m_hiPlayImDataIndex].m_data1 = hi;
					}
				}
				if (m_psgEnvCvData.m_lowPlayImDataIndex >= 0)
				{
					PSG_ToneConvert(m_psgEnvCvData, out lo, out hi);
					m_imData.PlayImDataList[m_psgEnvCvData.m_lowPlayImDataIndex].m_data1 = lo;
				}
				if (m_psgEnvCvData.m_hiPlayImDataIndex >= 0)
				{
					PSG_ToneConvert(m_psgEnvCvData, out lo, out hi);
					m_imData.PlayImDataList[m_psgEnvCvData.m_hiPlayImDataIndex].m_data1 = hi;
				}
				m_imData.InsertPlayImDataList();
				m_imData.CleanupPlayImDataList();
			}
		}

		/// <summary>
		///	16-Bit(WORD)音程データ変換
		/// </summary>
		private void ConvertWordTone(ConvertWordToneData wordToneData, ClockToneConvertType convertType, bool isHiByte, uint playMilliSec, int playImDataIndex, F1ImData.PlayImData playImData)
		{
			byte hi = 0;
			byte low = 0;
			byte writeRegister = playImData.m_data0;
			byte writeRegData = playImData.m_data1;
 			byte resData = writeRegData;

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
								case ClockToneConvertType.PSG_TONE:
									PSG_ToneConvert(wordToneData, out low, out hi);
									break;
								case ClockToneConvertType.PSG_ENV:
									PSG_EnvConvert(wordToneData, out low, out hi);
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
								case ClockToneConvertType.PSG_TONE:
									PSG_ToneConvert(wordToneData, out low, out hi);
									break;
								case ClockToneConvertType.PSG_ENV:
									PSG_EnvConvert(wordToneData, out low, out hi);
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
									case ClockToneConvertType.PSG_TONE:
										PSG_ToneConvert(wordToneData, out low, out hi);
										break;
									case ClockToneConvertType.PSG_ENV:
										PSG_EnvConvert(wordToneData, out low, out hi);
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
									case ClockToneConvertType.PSG_TONE:
										PSG_ToneConvert(wordToneData, out low, out hi);
										break;
									case ClockToneConvertType.PSG_ENV:
										PSG_EnvConvert(wordToneData, out low, out hi);
										break;
								}
								//	上位バイトの 変換後の値を保持
								wordToneData.m_hiCovertedData = hi;
								//	下位バイトの PlayImData の書き込み値を、変換後の下位バイトの値に書き変え
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
		///	PSG の Tone変換
		/// </summary>
		private void PSG_ToneConvert(ConvertWordToneData toneCvData, out byte fTune, out byte cTune)
		{
			int sTune = ((toneCvData.m_hiData << 8) & 0x0F00 ) | (toneCvData.m_lowData & 0xFF);
			if (sTune == 0)
			{
				cTune = (byte)toneCvData.m_hiData;
				fTune = (byte)toneCvData.m_lowData;
				return;
			}
			float hardClock = ((float)m_targetChip.TargetChipClock) / 2f;
			float playClock = ((float)m_targetChip.SourceChipClock); 
			playClock /=  2f;
			float tone = playClock / (((float)sTune) * 16f);
			int dTune = (int)((hardClock / tone) /16f); 
			cTune = (byte)((dTune >> 8) & 0x0F);
			fTune = (byte)(dTune & 0xFF); 
		}

		/// <summary>
		///	PSG の Envlope変換
		/// </summary>
		private void PSG_EnvConvert(ConvertWordToneData toneCvData, out byte fTune, out byte cTune)
		{
			int sEnv = ((toneCvData.m_hiData << 8) & 0xFF00) | (toneCvData.m_lowData & 0xFF);
			if (sEnv == 0)
			{
				cTune = (byte)toneCvData.m_hiData;
				fTune = (byte)toneCvData.m_lowData;
				return;
			}
			float hardClock = ((float)m_targetChip.TargetChipClock) / 2f;
			float playClock = ((float)m_targetChip.SourceChipClock);
			playClock /= 2f;
			float env = (((float)sEnv) * 256f) / playClock;
			int dEnv = (int)((hardClock * env) / 256f); 
			cTune = (byte)((dEnv >> 8) & 0xFF);
			fTune = (byte)(dEnv & 0xFF); 
		}

		/// <summary>
		///	PSG の Noise変換
		/// </summary>
		private byte PSG_NoiseConvert(int sNoise)
		{
			if (sNoise == 0)
			{
				return (byte)0x00;
			}
			float hardClock = ((float)m_targetChip.TargetChipClock) / 2f;
			float playClock = ((float)m_targetChip.SourceChipClock); 
			playClock /=  2f;
			float noise = playClock / (((float)sNoise) * 16f);
			int dNoise = (int)((hardClock / noise) /16f); 
			return (byte)(dNoise & 0x1F);
		}

		/// <summary>
		/// レジスタ操作に音量設定にあわせた音量制御を入れる
		/// </summary>
		public override void VolumeConvert()
		{
			if (m_imData.SSGVol != 0)
			{
				for (int index = 0, l = m_imData.PlayImDataList.Count; index < l; index++)
				{
					var playImData = m_imData.PlayImDataList[index];
					if (playImData.m_chipSelect == m_targetChip.ChipSelect && playImData.m_imType == F1ImData.PlayImType.TWO_DATA)
					{
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
				}
				m_imData.CleanupPlayImDataList();
			}
		}
	}
}