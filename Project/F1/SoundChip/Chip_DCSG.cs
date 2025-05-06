using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace F1
{
	/// <summary>
	///	CHIP DCSG (SN76489) クラス
	/// </summary>
	public class Chip_DCSG : SoundChip
	{
		private int m_hiData;
		private int m_hiIndex;

		/// <summary>
		/// レジスタ操作にプレイ CHIP にあわせた制御を入れる
		/// </summary>
		public override void ControlPlayChipRegiser()
		{
			m_imData.CleanupPlayImDataList();
		}

		/// <summary>
		/// 重複するレジスタ操作の撤去などでサイズを抑える
		/// </summary>
		public override void ShrinkPlayChipRegiser()
		{
			if (m_imData.IsShrink)
			{
				m_imData.CleanupPlayImDataList();
			}
		}

		/// <summary>
		/// ボリュームと音程変換の初期化
		/// </summary>
		protected override void InitializeForToneAndVolume()
		{
		}

		/// <summary>
		/// レジスタ操作にクロックにあわせた音程制御を入れる
		/// </summary>
		public override void ToneConvert()
		{
			if (m_imData.IsToneAdjust && CheckAdjustClock())
			{
				bool isParameter = false;
				for (int index = 0, l = m_imData.PlayImDataList.Count; index < l; index++)
				{
					var playImData = m_imData.PlayImDataList[index];
					if (playImData.m_chipSelect == m_targetChip.ChipSelect && playImData.m_imType == F1ImData.PlayImType.ONE_DATA)
					{
						if (!isParameter)
						{
							int t1 = (int)(playImData.m_data0 & 0xF0);
							if (t1 == 0x80 || t1 == 0xA0 || t1 == 0xC0)
							{
								m_hiIndex = index;
								m_hiData  = playImData.m_data0;
								isParameter = true;
							}
							else if (t1  == 0xE0)
							{
								int rate = m_targetChip.TargetChipClock / m_targetChip.SourceChipClock;
								int noiseShift = ((int)playImData.m_data0) & 0x03;
								if (noiseShift <= 1 && rate >=2)
								{
									noiseShift += 1;
									playImData.m_data0 = (byte)((((int)playImData.m_data0) & 0xFC) | noiseShift);
								}
							}
						}
						else
						{
							int hi_cmd  = ((int)m_hiData) & 0xF0;
							int lo_data = ((int)m_hiData) & 0x0F;
							int hi_data = ((int)playImData.m_data0) & 0x3F;
							int tune = (hi_data << 4) | lo_data; 
							float playClock = ((float)m_targetChip.SourceChipClock); 
							float hardClock = ((float)m_targetChip.TargetChipClock);
							float tone = playClock / (((float)tune) * 32f);
							int dTune = (int)((hardClock / tone) /32f);
							dTune =  (dTune > 0x3FF) ? 0x3FF : dTune;
							lo_data = dTune & 0x0F;
							playImData.m_data0 = (byte)((dTune >> 4) & 0x3F);
							m_imData.PlayImDataList[m_hiIndex].m_data0 = (byte)(lo_data | hi_cmd);
							isParameter = false;
						}
					}
				}
			}
			m_imData.CleanupPlayImDataList();
		}

		/// <summary>
		/// レジスタ操作に音量設定にあわせた音量制御を入れる
		/// </summary>
		public override void VolumeConvert()
		{
			if (m_imData.SSGVol != 0)
			{
				bool isParameter = false;
				foreach(var playImData in m_imData.PlayImDataList)
				{
					if (playImData.m_chipSelect == m_targetChip.ChipSelect && playImData.m_imType == F1ImData.PlayImType.ONE_DATA)
					{
						if (!isParameter)
						{
							int t1 = (int)(playImData.m_data0 & 0xF0);
							isParameter = (t1 == 0x80 || t1 == 0xA0 || t1 == 0xC0) ? true : false;
							if (t1 == 0x90 || t1 == 0xB0 || t1 == 0xD0 || t1 == 0xF0)
							{
								int vol = 0x0F - ((int)(playImData.m_data0) & 0x0F);
								if (vol != 0)
								{
									vol = (int)((float)vol + (15f * ((float)(m_imData.SSGVol) / 100f)));
									vol = (vol < 0x00) ? 0x00 : ((vol > 0x0F) ? 0x0F : vol);
								}
								vol = 0x0F - vol;
								playImData.m_data0 = (byte)(t1 | vol);
							}
						}
						else 
						{
							isParameter = false;
						}
					}
				}
			}
			m_imData.CleanupPlayImDataList();
		}
	}
}