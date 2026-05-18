using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace F1
{
	/// <summary>
	///	CHIP OPM クラス
	/// </summary>
	public class Chip_OPM : SoundChip
	{
		protected int OPM_OPERATOR { get; } = 4;
		protected int OPM_FM_CH { get; } = 8;
		protected int OPM_SSG_CH { get; } = 0;

		private byte m_reg14State;

		/// <summary>
		/// レジスタ操作にプレイ CHIP にあわせた制御を入れる
		/// </summary>
		public override void ControlPlayChipRegiser()
		{
			m_reg14State = 0x7F;
			foreach(var playImData in m_imData.PlayImDataList.Where(x => x.m_chipSelect == m_targetChip.ChipSelect && x.m_imType == F1ImData.PlayImType.TWO_DATA))
			{
				if (CheckNCRegister_YM2151(playImData)) 
				{
					if (!m_imData.IsTimerReg)
					{
						if (playImData.m_data0 == 0x10 ||	playImData.m_data0 == 0x11 ||	playImData.m_data0 == 0x12)
						{	//	TimerA.TimerB
							playImData.m_imType = F1ImData.PlayImType.NONE;
						}
						if (playImData.m_data0 == 0x14) 		//	CSM Timer Control
						{
							byte md = (byte)(playImData.m_data1 & 0x80);
							playImData.m_data1 = md;
							if (m_reg14State != 0x7F && m_reg14State == md) 
							{
								playImData.m_imType = F1ImData.PlayImType.NONE;
							}
							m_reg14State = md;
						}
					}
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
				Dictionary<byte, byte> SameReg = new Dictionary<byte, byte>();
				foreach(var playImData in m_imData.PlayImDataList)
				{
					if (playImData.m_chipSelect == m_targetChip.ChipSelect && playImData.m_imType == F1ImData.PlayImType.TWO_DATA)
					{
						if ((playImData.m_data0 >= 0x28 && playImData.m_data0 <= 0x37) ||		//	KC , KF
							(playImData.m_data0 >= 0x60 && playImData.m_data0 <= 0x7F))			//	TL
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
							else
							{
								SameReg.Add(playImData.m_data0, playImData.m_data1);
							}
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
			m_fmOperatorNum = OPM_OPERATOR;
			m_fmCannelNum = OPM_FM_CH;
			m_ssgCannelNum = OPM_SSG_CH;
		}

		/// <summary>
		/// レジスタ操作にクロックにあわせた音程制御を入れる
		//	NOTE:	F1_OPM のクロックは、3.579545Mhz。 YM2151は、4Mhz か の２択にする
		///			4Mhzの場合は、4Mhz 用のＫＣをテーブルで全音上げるだけとする
		/// </summary>
		public override void ToneConvert()
		{
			if (m_imData.IsToneAdjust && CheckAdjustClock())
			{
				foreach(var playImData in m_imData.PlayImDataList.Where(x => x.m_chipSelect == m_targetChip.ChipSelect && x.m_imType == F1ImData.PlayImType.TWO_DATA))
				{
					if (playImData.m_data0 >= 0x28 &&	playImData.m_data0 <= 0x2F)	//	KC レジスタ
					{	//	ソース 4M を 3.579545Mhz ターゲットに変換する
						var index = ((int)playImData.m_data1) & 0x7F;
						if (m_targetChip.SourceChipClock < m_targetChip.TargetChipClock)
						{	//	ソース 3.579545Mhz を 4M ターゲット に変換する
							index -= 5;
							if (index < 0) index = 0;
						}
						playImData.m_data1 = (index >= 0x7E) ? (byte)0x07F : YM2151KCTable[index];
					}
				}
				m_imData.CleanupPlayImDataList();
			}
		}

		/// <summary>
		///	YM2151 音程（ＫＣ）レジスタ	（4MHz / 3.579545Mhz）変換テーブル
		/// </summary>
		private static readonly byte[] YM2151KCTable = new byte[0x80]
		{
		//	+00		+01		+02		+03X	+04		+05		+06		+07X	+08		+09		+0A		+0BX	+0C		+0D		+0E		+0FX
			0x02,	0x04,	0x05,	0x05,	0x06,	0x08,	0x09,	0x09,	0x0A,	0x0C,	0x0D,	0x0D,	0x0E,	0x10,	0x11,	0x11,	//	00
			0x12,	0x14,	0x15,	0x15,	0x16,	0x18,	0x19,	0x19,	0x1A,	0x1C,	0x1D,	0x1D,	0x1E,	0x20,	0x21,	0x21,	//	10
			0x22,	0x24,	0x25,	0x25,	0x26,	0x28,	0x29,	0x29,	0x2A,	0x2C,	0x2D,	0x2D,	0x2E,	0x30,	0x31,	0x31,	//	20
			0x32,	0x34,	0x35,	0x35,	0x36,	0x38,	0x39,	0x39,	0x3A,	0x3C,	0x3D,	0x3D,	0x3E,	0x40,	0x41,	0x41,	//	30
			0x42,	0x44,	0x45,	0x45,	0x46,	0x48,	0x49,	0x49,	0x4A,	0x4C,	0x4D,	0x4D,	0x4E,	0x50,	0x51,	0x51,	//	40
			0x52,	0x54,	0x55,	0x55,	0x56,	0x58,	0x59,	0x59,	0x5A,	0x5C,	0x5D,	0x5D,	0x5E,	0x60,	0x61,	0x61,	//	50
			0x62,	0x64,	0x65,	0x65,	0x66,	0x68,	0x69,	0x69,	0x6A,	0x6C,	0x6D,	0x6D,	0x6E,	0x70,	0x71,	0x71,	//	60
			0x72,	0x74,	0x75,	0x75,	0x76,	0x78,	0x79,	0x79,	0x7A,	0x7C,	0x7D,	0x7D,	0x7E,	0x7E,	0x7E,	0x7E,	//	70
		};

		/// <summary>
		/// レジスタ操作に音量設定にあわせた音量制御を入れる
		/// </summary>
		public override void VolumeConvert()
		{
			if (m_imData.FMVol != 0)
			{
				for (int index = 0, l = m_imData.PlayImDataList.Count; index < l; index++)
				{
					var playImData = m_imData.PlayImDataList[index];
					if (playImData.m_chipSelect == m_targetChip.ChipSelect && playImData.m_imType == F1ImData.PlayImType.TWO_DATA)
					{
						if (playImData.m_data0 >= 0x60 && playImData.m_data0 <= 0x7F)
						{
							int ch = (int)(playImData.m_data0 & 0x07);
							int op = 0;
							if (playImData.m_data0 >= 0x68 && playImData.m_data0 <= 0x6F ) op = 2;
							if (playImData.m_data0 >= 0x70 && playImData.m_data0 <= 0x77 ) op = 1;
							if (playImData.m_data0 >= 0x78 && playImData.m_data0 <= 0x7F ) op = 3;
							m_fmTotalLevel[ch,op] = playImData.m_data1 & 0x7F;
							m_fmTLIndex[ch,op] = index;
						}
						else if (playImData.m_data0 >= 0x20 && playImData.m_data0 <= 0x27)
						{
							int ch = (int)(playImData.m_data0 & 0x07);
							m_fmOpeConnect[ch] = (int)(playImData.m_data1 & 0x07);
						}
						else if (playImData.m_data0 == 0x08 && ((playImData.m_data1 & 0x38) != 0))
						{
							int ch = (int)(playImData.m_data1 & 0x07);
							TotalLevelConvert(ch);
						}
					}
				}
			}
			m_imData.CleanupPlayImDataList();
		}

		/// <summary>
		///	YM2151	トータルレベル変換
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
		///	YM2151	未実装レジスタ	チェック
		/// </summary>
		private bool CheckNCRegister_YM2151(F1ImData.PlayImData playImData)
		{
			if (playImData.m_A1 > 0)
			{
				playImData.m_imType = F1ImData.PlayImType.NONE;
				return false;
			}
			foreach(var ncReg in YM2151_NC_REG)
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
		///	YM2151	未実装レジスタ
		/// </summary>
		private readonly byte[] YM2151_NC_REG = 
		{
			0x00, 0x01,	//	0x01 Test
			0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 
			0x13, 0x15, 0x16, 0x17, 0x1A, 0x1C, 0x1D, 0x1E, 0x1F,
		};

	}
}