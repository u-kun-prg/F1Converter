using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace F1
{
	/// <summary>
	///	CHIP M6258 クラス
	/// </summary>
	public class Chip_M6258 : SoundChip
	{
		/// <summary>
		/// レジスタ操作にプレイ CHIP にあわせた制御を入れる
		/// </summary>
		public override void ControlPlayChipRegiser()
		{
			foreach(var playImData in m_imData.PlayImDataList.Where(x => x.m_chipSelect == m_targetChip.ChipSelect))
			{
				switch(playImData.m_imType)
				{
					case F1ImData.PlayImType.VSTRM_DATA:
					case F1ImData.PlayImType.VSTRM_SAMPLING_RATE:
					case F1ImData.PlayImType.VSTRM_START_SIZE:
					case F1ImData.PlayImType.VSTRM_STOP_STREAM:
					case F1ImData.PlayImType.VSTRM_START_SIZE_FAST:
						playImData.m_data0 = (byte)((playImData.m_data1 & 0x0F) | 0xF0);
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