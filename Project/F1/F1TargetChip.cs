using System;
using System.Collections.Generic;

namespace F1
{
	///	<summary>
	///	ターゲット CHIP クラス
	/// </summary>
	public class F1TargetChip
	{
		///	<summary>
		///	Active 状態
		/// </summary>
		public ActiveStatus TargetActiveStatus { get; private set; }
		///	<summary>
		///	ターゲットハード上での CHIP_SELECT
		/// </summary>
		public int ChipSelect { get; private set; }
		///	<summary>
		///	ターゲット CHIP		CHIP タイプ
		/// </summary>
		public ChipType TargetChipType { get; private set; }
		///	<summary>
		///	PCM がアクティブか
		/// </summary>
		public bool IsTargetPcmActive { get; private set; }
		///	<summary>
		///	ターゲット CHIP		PCM 機能のタイプ
		/// </summary>
		public PcmFunctionType TargetPcmFunction { get; private set; }
		///	<summary>
		///	ターゲット CHIP		クロック
		/// </summary>
		public int TargetChipClock { get; private set; }
		///	<summary>
		///	ソース CHIP 	CHIP タイプ
		/// </summary>
		public ChipType SourceChipType { get; private set; }
		///	<summary>
		///	ソース CHIP 	クロック
		/// </summary>
		public int SourceChipClock { get; private set; }
		///	<summary>
		///	ソース CHIP 	名称（チップタイプ文字列）
		/// </summary>
		public string SourceChipName { get; private set; }

		///	<summary>
		///	コンストラクタ
		/// </summary>
		public F1TargetChip(int chipSelect, ChipType chipType, int chipClock)
		{
			this.TargetActiveStatus = ActiveStatus.INACTIVE;
			this.TargetChipType = chipType;
			this.TargetChipClock = chipClock;
			this.ChipSelect = chipSelect;
			this.IsTargetPcmActive = false;
			this.TargetPcmFunction = ChipPcmFunction.GetPcmFunction(chipType);
			this.SourceChipType = ChipType.NONE;
			this.SourceChipClock = 0;
			this.SourceChipName ="";
		}

		///	<summary>
		///	ターゲット CHIP をアクティブ化
		/// </summary>
		public void Active(ChipType sourceChipType, int sourceChipClock, string sourceChipName, bool isPcmActive)
		{
			TargetActiveStatus = ActiveStatus.ACTIVE; 
			SourceChipType = sourceChipType;
			SourceChipClock = sourceChipClock;
			SourceChipName = sourceChipName;
			IsTargetPcmActive = isPcmActive;
		}

		///	<summary>
		///	ターゲット CHIP を非アクティブ化
		/// </summary>
		public void Deactivate()
		{
			TargetActiveStatus = ActiveStatus.DESTROYED;
		}

		///	<summary>
		///	ターゲット CHIP の PCM を非アクティブ化
		/// </summary>
		public void DeactivatePcm()
		{
			IsTargetPcmActive = false;
		}

		///	<summary>
		///	ターゲット CHIP タイプ文字列を取得
		///	Get Target ChipType String.
		///	</summary>
		public string GetTargetChipTypeNameString()
		{
			return TargetChipType.ToString();
		}

		///	<summary>
		///	ターゲット CHIP	クロックをMhz単位文字列で返す
		/// </summary>
		public string GetTargetChipClockMhzString()
		{
			var targetClock = (float)TargetChipClock / 1000000f;
			return $"{targetClock} Mhz";
		}

		///	<summary>
		///	ソース CHIP		クロックをMhz単位文字列で返す
		/// </summary>
		public string GetSourceClockString()
		{
			var souceClock = (float)SourceChipClock / 1000000f;
			return $"{souceClock} Mhz";
		}

	}
}
