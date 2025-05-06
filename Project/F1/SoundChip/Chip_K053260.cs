using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace F1
{
	/// <summary>
	///	CHIP K053260 クラス
	/// </summary>
	public class Chip_K053260 : SoundChip
	{
		/// <summary>
		/// レジスタ操作にプレイ CHIP にあわせた制御を入れる
		/// </summary>
		public override void ControlPlayChipRegiser()
		{
			foreach(var playImData in m_imData.PlayImDataList.Where(x => x.m_chipSelect == m_targetChip.ChipSelect && x.m_imType == F1ImData.PlayImType.TWO_DATA))
			{
				//	未実装レジスタのチェック
				var isNC = ( playImData.m_A1 > 0 || playImData.m_data0 >= 0x30);
				if (!isNC)
				{
					var ncRegs = K053260_NC_REG;
					foreach(var ncReg in ncRegs)
					{
						if (playImData.m_data0 == ncReg)
						{
							isNC = true;
							break;
						}
					}
				}
				if (isNC)
				{
					playImData.m_imType = F1ImData.PlayImType.NONE;
				}
			}
			m_imData.CleanupPlayImDataList();
		}
		/// <summary>
		///	K053260	未実装レジスタ	0x30以降は未実装
		/// </summary>
		private readonly byte[] K053260_NC_REG = 
		{
			0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,	0x2B, 0x2E,	
		};

		/// <summary>
		/// 重複するレジスタ操作の撤去などでサイズを抑える
		/// </summary>
		public override void ShrinkPlayChipRegiser()
		{
			m_imData.CleanupPlayImDataList();
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