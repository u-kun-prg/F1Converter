using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace F1
{
	/// <summary>
	///	CHIP KONAMI-SCC(K051649 & K052539) クラス
	/// </summary>
	public class Chip_SCC : SoundChip
	{
		/// <summary>
		/// プレイ CHIP に合わせてレジスタ操作に制御を入れる
		/// </summary>
		public override void ControlPlayChipRegiser()
		{
			foreach(var playImData in m_imData.PlayImDataList.Where(x => x.m_chipSelect == m_targetChip.ChipSelect && x.m_imType == F1ImData.PlayImType.TWO_DATA))
			{	//	NC Register.
				switch(m_targetChip.TargetChipType)
				{
					case ChipType.K051649:
						if (playImData.m_data0 >= 0x90 && playImData.m_data0 < 0xE0)
						{
							playImData.m_imType = F1ImData.PlayImType.NONE;
						}
						break;
					case ChipType.K052539:
						if ((playImData.m_data0 >= 0xB0 && playImData.m_data0 <= 0xBF) || (playImData.m_data0 >= 0xE0 && playImData.m_data0 <= 0xFF))
						{
							playImData.m_imType = F1ImData.PlayImType.NONE;
						}
						break;
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
		}

		/// <summary>
		/// レジスタ操作に音量設定にあわせた音量制御を入れる
		/// </summary>
		public override void VolumeConvert()
		{
		}
	}
}