using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace F1
{
	/// <summary>
	///	音源 CHIP クラス
	/// </summary>
	public abstract class SoundChip
	{
		protected F1ImData m_imData;

		protected F1TargetHardware m_targetHardware;
		protected F1TargetChip m_targetChip;

		protected uint m_oneCycleNs;

		protected int m_fmCannelNum;
		protected int m_ssgCannelNum;
		protected int m_fmOperatorNum;

		protected int[] m_fmOpeConnect;
		protected int[,] m_fmTotalLevel;
		protected int[,] m_fmTLIndex;

		private int TOLERANCE_CLK_DIFF { get; } = 100000;	//	0.1Mhz

		/// <summary>
		/// 初期化
		/// </summary>
		public void Initialize(F1TargetHardware targetHardware, F1TargetChip targetChip, F1ImData imData, uint oneCycleNs)
		{
			m_targetHardware = targetHardware;
			m_targetChip = targetChip;
			m_imData = imData;
			m_oneCycleNs = oneCycleNs;

			if ((m_imData.IsToneAdjust && CheckAdjustClock()) || m_imData.FMVol !=0 || m_imData.SSGVol != 0)
			{
				InitializeForToneAndVolume();

				if (m_imData.FMVol != 0)
				{
					m_fmOpeConnect = new int[m_fmCannelNum];
					m_fmTotalLevel = new int[m_fmCannelNum, m_fmOperatorNum];
					m_fmTLIndex = new int[m_fmCannelNum, m_fmOperatorNum];
					for (int i = 0; i < m_fmCannelNum; i++)
					{
						m_fmOpeConnect[i] = 0;
						for (int j = 0; j < m_fmOperatorNum; j++)
						{
							m_fmTotalLevel[i,j] = 0;
							m_fmTLIndex[i,j] = 0;
						}
					}
				}
			}
		}

		/// <summary>
		/// クロック補正の判定
		/// </summary>
		protected bool CheckAdjustClock()
		{
			bool result = false;
			switch(m_targetChip.TargetChipType)
			{
				case ChipType.YM2151:
					result = ((Math.Abs(m_targetChip.SourceChipClock - m_targetChip.TargetChipClock)) <= TOLERANCE_CLK_DIFF) ? false : true;
					break;
				case ChipType.YM2203:
					switch(m_targetChip.SourceChipType)
					{
						case ChipType.YM2203:
							result = ((Math.Abs(m_targetChip.SourceChipClock - m_targetChip.TargetChipClock)) <= TOLERANCE_CLK_DIFF) ? false : true;
							break;
						case ChipType.AY_3_8910:
						case ChipType.YM2149:
							result = ((Math.Abs(m_targetChip.SourceChipClock - (m_targetChip.TargetChipClock / 2))) <= TOLERANCE_CLK_DIFF) ? false : true;
							break;
						case ChipType.YM2608:
							result = ((Math.Abs((m_targetChip.SourceChipClock / 2) - m_targetChip.TargetChipClock)) <= TOLERANCE_CLK_DIFF) ? false : true;
							break;
						default:
							result = false;
							break;
					}
					break;
				case ChipType.YM2608:
				case ChipType.YM2610:
				case ChipType.YM2610B:
					switch(m_targetChip.SourceChipType)
					{
						case ChipType.YM2203:
							result = ((Math.Abs(m_targetChip.SourceChipClock - (m_targetChip.TargetChipClock / 2))) <= TOLERANCE_CLK_DIFF) ? false : true;
							break;
						case ChipType.AY_3_8910:
						case ChipType.YM2149:
							result = ((Math.Abs(m_targetChip.SourceChipClock - (m_targetChip.TargetChipClock / 4))) <= TOLERANCE_CLK_DIFF) ? false : true;
							break;
						case ChipType.YM2608:
							result = ((Math.Abs(m_targetChip.SourceChipClock - m_targetChip.TargetChipClock)) <= TOLERANCE_CLK_DIFF) ? false : true;
							break;
						case ChipType.YM2612:
							result = ((Math.Abs(m_targetChip.SourceChipClock - m_targetChip.TargetChipClock)) <= TOLERANCE_CLK_DIFF) ? false : true;
							break;
						default:
							result = false;
							break;
					}
					break;
				case ChipType.YM2612:
					switch(m_targetChip.SourceChipType)
					{
						case ChipType.YM2203:
							result = ((Math.Abs(m_targetChip.SourceChipClock - (m_targetChip.TargetChipClock / 2))) <= TOLERANCE_CLK_DIFF) ? false : true;
							break;
						case ChipType.YM2608:
							result = ((Math.Abs(m_targetChip.SourceChipClock - m_targetChip.TargetChipClock)) <= TOLERANCE_CLK_DIFF) ? false : true;
							break;
						case ChipType.YM2612:
							result = ((Math.Abs(m_targetChip.SourceChipClock - m_targetChip.TargetChipClock)) <= TOLERANCE_CLK_DIFF) ? false : true;
							break;
						default:
							result = false;
							break;
					}
					break;
				case ChipType.YMF288:
					switch(m_targetChip.SourceChipType)
					{
						case ChipType.YM2203:
							result = ((Math.Abs(m_targetChip.SourceChipClock - (m_targetChip.TargetChipClock / 2))) <= TOLERANCE_CLK_DIFF) ? false : true;
							break;
						case ChipType.AY_3_8910:
						case ChipType.YM2149:
							result = ((Math.Abs(m_targetChip.SourceChipClock - (m_targetChip.TargetChipClock / 4))) <= TOLERANCE_CLK_DIFF) ? false : true;
							break;
						case ChipType.YM2608:
							result = ((Math.Abs(m_targetChip.SourceChipClock - m_targetChip.TargetChipClock)) <= TOLERANCE_CLK_DIFF) ? false : true;
							break;
						case ChipType.YM2612:
							result = ((Math.Abs(m_targetChip.SourceChipClock - m_targetChip.TargetChipClock)) <= TOLERANCE_CLK_DIFF) ? false : true;
							break;
						default:
							result = false;
							break;
					}
					break;
				case ChipType.YM3526:
				case ChipType.YM3812:
					switch(m_targetChip.SourceChipType)
					{
						case ChipType.YM3526:
						case ChipType.YM3812:
						case ChipType.Y8950:
							result = ((Math.Abs(m_targetChip.SourceChipClock - m_targetChip.TargetChipClock)) <= TOLERANCE_CLK_DIFF) ? false : true;
							break;
						case ChipType.YMF262:
							result = ((Math.Abs((m_targetChip.SourceChipClock / 4) - m_targetChip.TargetChipClock)) <= TOLERANCE_CLK_DIFF) ? false : true;
							break;
						default:
							result = false;
							break;
					}
					break;
				case ChipType.YMF262:
					switch(m_targetChip.SourceChipType)
					{
						case ChipType.YMF262:
							result = ((Math.Abs(m_targetChip.SourceChipClock - m_targetChip.TargetChipClock)) <= TOLERANCE_CLK_DIFF) ? false : true;
							break;
						case ChipType.YM3526:
						case ChipType.YM3812:
						case ChipType.Y8950:
							result = ((Math.Abs((m_targetChip.SourceChipClock * 4) - m_targetChip.TargetChipClock)) <= TOLERANCE_CLK_DIFF) ? false : true;
							break;
						default:
							result = false;
							break;
					}
					break;
				case ChipType.YM2413:
					result = ((Math.Abs(m_targetChip.SourceChipClock - m_targetChip.TargetChipClock)) <= TOLERANCE_CLK_DIFF) ? false : true;
					break;
				case ChipType.SN76489:
					result = ((Math.Abs(m_targetChip.SourceChipClock - m_targetChip.TargetChipClock)) <= TOLERANCE_CLK_DIFF) ? false : true;
					break;
				case ChipType.AY_3_8910:
					result = ((Math.Abs(m_targetChip.SourceChipClock - m_targetChip.TargetChipClock)) <= TOLERANCE_CLK_DIFF) ? false : true;
					break;
				case ChipType.K051649:
					result = false;
					break;
			}
			return result;
		}

		/// <summary>
		/// レジスタ操作にプレイ CHIP にあわせた制御を入れる
		/// </summary>
		public abstract void ControlPlayChipRegiser();

		/// <summary>
		/// 重複するレジスタ操作の撤去などでサイズを抑える
		/// </summary>
		public abstract void ShrinkPlayChipRegiser();

		/// <summary>
		/// ボリュームと音程変換の初期化
		/// </summary>
		protected abstract void InitializeForToneAndVolume();

		/// <summary>
		/// レジスタ操作にクロックにあわせた音程制御を入れる
		/// </summary>
		public abstract void ToneConvert();

		/// <summary>
		/// レジスタ操作に音量設定にあわせた音量制御を入れる
		/// </summary>
		public abstract void VolumeConvert();

	}
}