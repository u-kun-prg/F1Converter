using System;
using System.Collections.Generic;

namespace F1
{
	///	<summary>
	///	ターゲットハードウェア クラス
	/// </summary>
	public class F1TargetHardware
	{
		///	<summary>
		///	ターゲットハード名称
		/// </summary>
		public string Name { get; private set; }

		///	<summary>
		///	設定フラグ	PCM 再生有効
		/// </summary>
		public bool IsUsePCM { get; private set; }

		///	<summary>
		///	ターゲットハードが搭載している CHIP のリスト
		/// </summary>
		public List<F1TargetChip> TargetChipList { get; private set; }

		///	<summary>
		///	コンストラクタ
		/// </summary>
		public F1TargetHardware(string name, List<ChipType> chipTypeList, List<int> chipClockList, bool isUsePCM)
		{
			this.Name = name;
			this.IsUsePCM = isUsePCM;
			this.TargetChipList = new List<F1TargetChip>();
			//	ターゲットハードが搭載している CHIP それぞれのクロックを揃える
			for (int i=0, l=chipTypeList.Count; i<l; i++)
			{
				var targetChip = new F1TargetChip(i, chipTypeList[i], chipClockList[i]);
				this.TargetChipList.Add(targetChip);
			}
		}

		///	<summary>
		///	CHIP リストに、アクティブな CHIP が存在するかを返す
		/// </summary>
		public bool IsActiveTarget()
		{
			return TargetChipList.Exists(x => x.TargetActiveStatus == ActiveStatus.ACTIVE);
		}

		///	<summary>
		///	ターゲット CHIP_のクロックを取得
		/// </summary>
		public int GetTargetChipClock(int chipSelect)
		{
			if (chipSelect < TargetChipList.Count)
			{
				return TargetChipList[chipSelect].TargetChipClock;
			}
			return -1;
		}

		///	<summary>
		///	ターゲット CHIP のPCM がアクティブかを取得
		/// </summary>
		public bool GetTargetIsPcmActive(int chipSelect)
		{
			if (chipSelect < TargetChipList.Count)
			{
				return TargetChipList[chipSelect].IsTargetPcmActive;
			}
			return false;
		}

	}
}
