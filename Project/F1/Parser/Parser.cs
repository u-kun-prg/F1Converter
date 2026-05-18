using System;
using System.Collections.Generic;
using System.Linq;

namespace F1
{
	/// <summary>
	///	パーサー
	/// </summary>
	public abstract class Parser
	{
		protected string SourceFileName { get; private set; }
		protected F1Header Header { get; private set; }
		protected F1TargetHardware TargetHardware { get; private set; }
		protected F1ImData ImData { get; private set; }
		protected byte[] BinaryArray { get; private set; }
		protected string[] TextArray { get; private set; }
		protected bool DisableTopCode { get; private set; }

		public List<string> WarningStrings = new List<string>();
		public string ErrorString { get; protected set; }
		public string SuccessString { get; protected set; }

		/// <summary>
		///	パーサーの初期化
		/// </summary>
		public void Initialize(string sourceFileName, F1TargetHardware targetHardware, F1Header header, F1ImData imData, bool isOffTopCode, byte[] binaryArray, string[] textArray)
		{
			SourceFileName = sourceFileName;
			TargetHardware = targetHardware;
			Header = header;
			ImData = imData;
			DisableTopCode = isOffTopCode;
			BinaryArray = binaryArray;
			TextArray = textArray;
		}

		/// <summary>
		///	パース CHIP クラス
		/// </summary>
		protected class ParseChip
		{
			///	<summary>
			///	Active 状態
			/// </summary>
			public ActiveStatus ChipActiveStatus { get; private set; }
			///	<summary>
			///	パース  CHIP		CHIP タイプ
			/// </summary>
			public ChipType ChipType { get; private set; }
			///	<summary>
			///	パース  CHIP		CHIP 名前
			/// </summary>
			public string ChipName { get; private set; }
			///	<summary>
			///	パース  CHIP		クロック
			/// </summary>
			public int ChipClock { get; private set; }
			///	<summary>
			///	ターゲット CHIP	 	CHIP_SELECT
			/// </summary>
			public int ChipSelect { get; private set; }
			///	<summary>
			///	パース時の CHIP	識別ID
			/// </summary>
			public uint ChipIdCode0 { get; private set; }
			///	<summary>
			///	パース時の CHIP	識別ID
			/// </summary>
			public uint ChipIdCode1 { get; private set; }
			///	<summary>
			///	パース  CHIP		デュアルモード
			/// </summary>
			public DualMode ChipDualMode { get; private set; }
			///	<summary>
			///	パース  CHIP		デュアル番号
			/// </summary>
			public DualNumber ChipDualNumber  { get; private set; }
			///	<summary>
			///	パース  CHIP		デュアルパース CHIP のインデックス
			/// </summary>
			public int ChipDualIndex { get; private set; }

			///	<summary>
			///	コンストラクタ
			/// </summary>
			public ParseChip(ChipType chipType, int clock, uint idA0, uint idA1)
			{
				this.ChipActiveStatus = ActiveStatus.INACTIVE;
				this.ChipType = chipType;
				this.ChipName = chipType.ToString();
				this.ChipSelect = -1;
				this.ChipClock = clock;
				this.ChipIdCode0 = idA0;
				this.ChipIdCode1 = idA1;
				this.ChipDualMode = DualMode.NONE;
				this.ChipDualNumber = DualNumber.DualNone;
				this.ChipDualIndex = -1;
			}
			///	<summary>
			///	パース CHIP をアクティブ化
			/// </summary>
			public void Active(int chipSelect, DualMode dualMode)
			{
				ChipActiveStatus = ActiveStatus.ACTIVE;
				ChipSelect = chipSelect;
				ChipDualMode = dualMode;
			}
			///	<summary>
			///	パース CHIP を非アクティブ化
			/// </summary>
			public void Deactivate()
			{
				ChipActiveStatus = ActiveStatus.DESTROYED;
				ChipSelect = -1;
			}
			///	<summary>
			///	パース CHIP クロック設定
			/// </summary>
			public void SetChipClock(int clock)
			{
				ChipClock = clock;
			}
			///	<summary>
			///	パース CHIP 名前設定
			/// </summary>
			public void SetChipName(string name)
			{
				ChipName = name;
			}
			public void SetIdCode0(uint idCode0)
			///	<summary>
			///	パース CHIP ID コード０設定
			/// </summary>
			{
				ChipIdCode0 = idCode0;
			} 
			///	<summary>
			///	パース CHIP ID コード１設定
			/// </summary>
			public void SetIdCode1(uint idCode1)
			{
				ChipIdCode1 = idCode1;
			} 
			///	<summary>
			///	パース CHIP デュアルモード設定
			/// </summary>
			public void SetChipDualMode(DualMode dualMode)
			{
				ChipDualMode = dualMode;
			} 
			///	<summary>
			///	パース CHIP デュアル番号設定
			/// </summary>
			public void SetChipDualNumber(DualNumber dualNumder)
			{
				ChipDualNumber = dualNumder;
			} 
			///	<summary>
			///	パース CHIP デュアルチップのインデックス設定
			/// </summary>
			public void SetChipDualIndex(int index)
			{
				ChipDualIndex = index;
			} 
		}
		/// <summary>
		///	パース CHIP リスト
		/// </summary>
		protected List<ParseChip> m_parseChipList = new List<ParseChip>();

		/// <summary>
		///	パース CHIP をターゲット CHIP に反映する
		/// </summary>
		protected void ReflectParseChipToTargetChip()
		{
			//	ターゲット CHIP のループ
			for (int targetIndex = 0, tl = TargetHardware.TargetChipList.Count; targetIndex < tl; targetIndex++)
			{
				var targetChip = TargetHardware.TargetChipList[targetIndex];
				//	非アクティブでないターゲット CHIP は無視する
				if (targetChip.TargetActiveStatus != ActiveStatus.INACTIVE)
				{
					continue;
				}
				//	PCM がオフの場合、PCM 機能だけ、DAC 機能だけのターゲット CHIP は無視する
				if (!TargetHardware.IsUsePCM && (targetChip.TargetPcmFunction == PcmFunctionType.PCM_ONLY || targetChip.TargetPcmFunction == PcmFunctionType.DAC_ONLY))
				{
					continue;
				}
				//	ターゲット CHIP のコンパチデータを取得する
				ChipCompatibleData[] chipCompatibleDataArray;
				//	コンパチデータを取得できないターゲット CHIP は、無視する
				if (!ChipCompatible.GetChipCompatibleDataArray(targetChip.TargetChipType, out chipCompatibleDataArray))
				{
					continue;
				}

				//	パース CHIP のループ
				for (int parseIndex = 0, pl = m_parseChipList.Count; parseIndex < pl; parseIndex ++)
				{
					var parseChip = m_parseChipList[parseIndex];
					if (parseChip.ChipActiveStatus != ActiveStatus.INACTIVE)
					{	//	非アクティブでないパース CHIP は無視する
						continue;
					}
					//	ターゲット CHIP のコンパチデータからパース CHIP を見つける
					var chipCompatibleData = Array.Find(chipCompatibleDataArray, x => x.m_chipType == parseChip.ChipType);
					if (chipCompatibleData.m_chipType != ChipType.NONE)
					{
						//	コンパチデータがデュアルに対応可能、かつ、パースチップがデュアルの 1st の場合
						var isDualIn1Chip = chipCompatibleData.m_isDualIn1Chip;
						var isPcmActive = (TargetHardware.IsUsePCM) ? chipCompatibleData.m_isPcmActive : false;
						if (isDualIn1Chip && parseChip.ChipDualNumber == DualNumber.Dual1st)
						{
							var secondParseChip = m_parseChipList[parseChip.ChipDualIndex];
							parseChip.SetIdCode1(secondParseChip.ChipIdCode0);
							secondParseChip.Deactivate();
							ActiveParseChipAndTargetChip(targetChip, parseChip, isPcmActive:isPcmActive, isDualIn1Chip:true, dualMode:DualMode.BOTH);
							ImData.IsDualIn1ChipCSDict.Add(targetChip.ChipSelect, isDualIn1Chip);
							break;
						}
						else
						{
							isDualIn1Chip = false;
							//	パース CHIP がデュアルの 1st の場合
							if (parseChip.ChipDualNumber == DualNumber.Dual1st)
							{	//	同じ CHIP_TYPE で CHIP セレクトが違う、ターゲット CHIP を見つける
								var dualTargetIndex = TargetHardware.TargetChipList.FindIndex(
																x =>	x.TargetActiveStatus == ActiveStatus.INACTIVE 
																	&&	x.TargetChipType == targetChip.TargetChipType 
																	&&	x.ChipSelect != targetChip.ChipSelect);
								//	見つからない場合
								if (dualTargetIndex < 0)
								{
									//	デュアルの再生指定が、２つめの場合
									if (ImData.IsDual2nd)
									{	//	ターゲット CHIP と２つめのパース CHIP を SECOND でアクティブにする
										var secondParseChip = m_parseChipList[parseChip.ChipDualIndex];
										ActiveParseChipAndTargetChip(targetChip, secondParseChip, isPcmActive:isPcmActive, isDualIn1Chip:isDualIn1Chip, dualMode:DualMode.SECOND);
										//	PLAY 中間データに、１CHIP でデュアル CHIP に対応フラグを設定する
										ImData.IsDualIn1ChipCSDict.Add(targetChip.ChipSelect, isDualIn1Chip);
										break;
									}
									//	デュアルの再生指定が、ない場合
									else
									{	//	ターゲット CHIP とパース CHIP を FIRST でアクティブにする
										ActiveParseChipAndTargetChip(targetChip, parseChip, isPcmActive:isPcmActive, isDualIn1Chip:isDualIn1Chip, dualMode:DualMode.FIRST);
										//	PLAY 中間データに、１CHIP でデュアル CHIP に対応フラグを設定する
										ImData.IsDualIn1ChipCSDict.Add(targetChip.ChipSelect, isDualIn1Chip);
										break;
									}
								}
								//	見つかった場合
								else
								{	//	デュアルのターゲット CHIP とパース CHIP を FIRST と SECOND で２つアクティブにする
									var secondParseChip = m_parseChipList[parseChip.ChipDualIndex];
									var secondTargetChip = TargetHardware.TargetChipList[dualTargetIndex];
									ActiveParseChipAndTargetChip(targetChip, 		parseChip,		isPcmActive:isPcmActive,	isDualIn1Chip:isDualIn1Chip, dualMode:DualMode.FIRST);
									ImData.IsDualIn1ChipCSDict.Add(targetChip.ChipSelect, isDualIn1Chip);
									ActiveParseChipAndTargetChip(secondTargetChip,	secondParseChip,isPcmActive:isPcmActive,	isDualIn1Chip:isDualIn1Chip, dualMode:DualMode.SECOND);
									ImData.IsDualIn1ChipCSDict.Add(secondTargetChip.ChipSelect, isDualIn1Chip);
									break;
								}
							}
							//	パース CHIP がデュアルの 1st でない場合
							else
							{	//	ターゲット CHIP とパース CHIP を アクティブにする
								ActiveParseChipAndTargetChip(targetChip, parseChip,isPcmActive:isPcmActive, isDualIn1Chip:isDualIn1Chip, dualMode:DualMode.NONE);
								//	PLAY 中間データに、１CHIP でデュアル CHIP に対応フラグを設定する
								ImData.IsDualIn1ChipCSDict.Add(targetChip.ChipSelect, isDualIn1Chip);
								break;
							}
						}
					}
				}
			}
		}
		///	<summary>
		///	パース CHIP とターゲット CHIPをアクティブにする
		///	</summary>
		private void ActiveParseChipAndTargetChip(F1TargetChip targetChip, ParseChip parseChip, bool isPcmActive, bool isDualIn1Chip, DualMode dualMode)
		{
			parseChip.Active(targetChip.ChipSelect, dualMode);
			targetChip.Active(parseChip.ChipType, parseChip.ChipClock, parseChip.ChipName, isPcmActive);
		}
		/// <summary>
		///	パース CHIP とターゲット CHIP の PCM を非アクティブにする	PCM データが存在しない場合
		/// </summary>
		protected void PcmDeactiveParseChipAndTargetChip()
		{
			for (int parseIndex = 0, pl = m_parseChipList.Count; parseIndex < pl; parseIndex ++)
			{
				var parseChip = m_parseChipList[parseIndex];
				if (parseChip.ChipActiveStatus != ActiveStatus.ACTIVE)
				{	//	アクティブでないパース CHIP は無視する
					continue;
				}
				var targetChip = TargetHardware.TargetChipList[parseChip.ChipSelect];
				if (targetChip.TargetPcmFunction == PcmFunctionType.PCM_ONLY || targetChip.TargetPcmFunction == PcmFunctionType.DAC_ONLY)
				{	//	PCM 機能だけ、DAC 機能だけのターゲット CHIP はパース CHIP もろとも非アクティブにする
					DeactivateParseChipAndTargetChip(parseChip);
				}
				else
				{   //	ターゲット CHIP の PCM を非アクティブにする
					targetChip.DeactivatePcm();
				}
			}
		}

		/// <summary>
		///	パース CHIP とターゲット CHIPを非アクティブにする
		/// </summary>
		private void DeactivateParseChipAndTargetChip(ParseChip parseChip)
		{
			if (parseChip.ChipActiveStatus == ActiveStatus.ACTIVE)
			{
				var targetChip = TargetHardware.TargetChipList[parseChip.ChipSelect];
				targetChip.Deactivate();
				parseChip.Deactivate();;
			}
		}

		/// <summary>
		///	パース
		/// </summary>
		public abstract bool Parse();

		/// <summary>
		///	PLAY中間データリストの個数を返す
		/// </summary>
		protected int GetPlayImDataListCount()
		{
			return ImData.GetPlayImDataListCount();
		}

		/// <summary>
		/// PLAY中間データリストの指定インデックス以降を削除する
		/// </summary>
		protected void RemovePlayImDataListAfterIndex(int index)
		{
			ImData.RemovePlayImDataListAfterIndex(index);
		}

		/// <summary>
		///	PLAY中間データリストの指定インデックスまでのサイクル WAIT 数を取得する
		///	指定インデックスが負数の場合は、リスト全体の数を取得する
		/// </summary>
		protected int GetPlayImDataCycleWaitByIndex(int index)
		{
			return ImData.GetPlayImDataListCycleWaitByIndex(index);
		}


		/// <summary>
		///	PLAY中間データリスト追加	２バイトデータ
		/// </summary>
		protected void AddTwoDataToPlayImData(int chipSelect, int a1, byte data0, byte data1)
		{
			if (chipSelect >= 0)
			{
				ImData.AddPlayImDataList(F1ImData.PlayImType.TWO_DATA, chipSelect:chipSelect, a1:a1, data0:data0, data1:data1);
			}
		}

		/// <summary>
		///	PLAY中間データリスト追加	１バイトデータ
		/// </summary>
		protected void AddOneDataToPlayImData(int chipSelect, int a1, byte data0)
		{
			if (chipSelect >= 0)
			{
				ImData.AddPlayImDataList(F1ImData.PlayImType.ONE_DATA, chipSelect:chipSelect, a1:a1, data0:data0);
			}
		}

		/// <summary>
		///	PLAY中間データリスト追加	VGM Stream.StremData
		/// </summary>
		protected void AddVgmStreamDataToPlayImData(int chipSelect, int a1, byte code, byte vstrmStepSize, byte vstrmStepBase)
		{
			if (chipSelect >= 0)
			{
				ImData.AddPlayImDataList(F1ImData.PlayImType.VSTRM_DATA, chipSelect:chipSelect, a1:a1, data0:code, data1:code, vstrmStepSize:vstrmStepSize, vstrmStepBase:vstrmStepBase);
			}
		}

		/// <summary>
		///	PLAY中間データリスト追加	VGM Stream Sampling Rate
		/// </summary>
		protected void AddVgmStreamSamplingRataToPlayImData(int chipSelect, int a1, byte code, int vstrmSamplingRate)
		{
			if (chipSelect >= 0)
			{
				ImData.AddPlayImDataList(F1ImData.PlayImType.VSTRM_SAMPLING_RATE, chipSelect:chipSelect, a1:a1, data0:code, data1:code, vstrmSamplingRate: vstrmSamplingRate);
			}
		}

		/// <summary>
		///	PLAY中間データリスト追加	VGM Stream Start And Size
		/// </summary>
		protected void AddVgmStreamStartSizeToPlayImData(int chipSelect, int a1, byte code, byte vstrmFlagMode, int vstrmStart, int vstrmSize)
		{
			if (chipSelect >= 0)
			{
				ImData.AddPlayImDataList(F1ImData.PlayImType.VSTRM_START_SIZE, chipSelect:chipSelect, a1:a1, data0:code, data1:code, vstrmFlagMode:vstrmFlagMode, vstrmStart:vstrmStart, vstrmSize: vstrmSize);
			}
		}

		/// <summary>
		///	PLAY中間データリスト追加	VGM Streeam Start And Size
		/// </summary>
		protected void AddVgmStopStreamToPlayImData(int chipSelect, byte code, int a1)
		{
			if (chipSelect >= 0)
			{
				ImData.AddPlayImDataList(F1ImData.PlayImType.VSTRM_STOP_STREAM, chipSelect:chipSelect, a1:a1, data0:code, data1:code);
			}
		}

		/// <summary>
		///	PLAY中間データリスト追加	VGM Streeam Start Size Fast Call
		/// </summary>
		protected void AddVgmStreamStartSizeFastCallToPlayImData(int chipSelect, int a1, byte code, int vstrmBlockId, byte vstrmFlagMode, int vstrmStart, int vstrmSize, int vstrmSamplingRate)
		{
			if (chipSelect >= 0)
			{
				ImData.AddPlayImDataList(F1ImData.PlayImType.VSTRM_START_SIZE_FAST, chipSelect:chipSelect, a1:a1, data0:code, data1:code, vstrmBlockId:vstrmBlockId, vstrmFlagMode:vstrmFlagMode, vstrmStart:vstrmStart, vstrmSize: vstrmSize, vstrmSamplingRate:vstrmSamplingRate);
			}
		}

		/// <summary>
		///	PLAY中間データリスト追加	サイクル WAIT
		/// </summary>
		protected void AddCycleWaitToPlayImData(int cycleWait)
		{
			ImData.AddPlayImDataList(F1ImData.PlayImType.CYCLE_WAIT, cycleWait:cycleWait);
		}

		/// <summary>
		///	PLAY中間データリスト追加	書き込み WAIT
		/// </summary>
		protected void AddWriteWaitToPlayImData(int writeWait, bool isUseRunLength = true)
		{
			ImData.AddCompWriteWaitPlayImDataList(writeWait, isUseRunLength);
		}

		/// <summary>
		///	PLAY中間データリスト追加	書き込み WAIT ランレングス
		/// </summary>
		protected void AddWriteWaitRunLengthToPlayImData(int writeWaitRL, int writeRunLength)
		{
			ImData.AddPlayImDataList(F1ImData.PlayImType.WRITE_WAIT_RL, writeWaitRL:writeWaitRL, writeRunLength:writeRunLength);
		}

		/// <summary>
		///	PLAY中間データリスト追加	書き込みシークアドレス
		/// </summary>
		protected void AddWriteSeekToPlayImData(int writeSeek)
		{
			ImData.AddPlayImDataList(F1ImData.PlayImType.WRITE_SEEK, seekAddress:writeSeek);
		}


		/// <summary>
		///	PLAY中間データリスト追加	エンドコード
		/// </summary>
		protected void AddEndCodeToPlayImData()
		{
			ImData.AddPlayImDataList(F1ImData.PlayImType.END_CODE);
		}

		/// <summary>
		///	PLAY中間データリスト追加	ループポイント
		/// </summary>
		protected void AddLoopPointToPlayImData()
		{
			ImData.AddPlayImDataList(F1ImData.PlayImType.LOOP_POINT);
		}

		/// <summary>
		///	パースリザルトメッセージ文字列の生成
		/// </summary>
		protected void CreateResultMessage()
		{
			var resString ="";
			foreach(var parseChip in m_parseChipList.Where(x => x.ChipActiveStatus == ActiveStatus.ACTIVE))
			{
				var targetChip = TargetHardware.TargetChipList[parseChip.ChipSelect];
				var targetChipStr = targetChip.GetTargetChipTypeNameString();
				var targetClockStr = targetChip.GetTargetChipClockMhzString();
				var sourceChipStr = targetChip.SourceChipName;
				var sourceChipClock = targetChip.GetSourceClockString();
				string dualStr = "";
				switch(parseChip.ChipDualMode)
				{
					case DualMode.FIRST: dualStr = "(Dual1ST)"; break;
					case DualMode.SECOND: dualStr = "(Dual2ND)"; break;
					case DualMode.BOTH: dualStr = "(DualBOTH)"; break;
				}
				resString += $"{targetChipStr} {targetClockStr}  Play  {sourceChipStr} {sourceChipClock}{dualStr}\r\n";
			}
			SuccessString = resString;
		}

		/// <summary>
		///	パース WARNING メッセージ文字列の生成.
		///	</summary>
		protected void AddWarningString(string warningString)
		{
			WarningStrings.Add(warningString);
		}

		/// <summary>
		///	パース サポートしていないデータのメッセージ文字列を生成
		///	</summary>
		protected void SetNoSupportedMessage()
		{
			ErrorString = "No Supported Data..";
		}


		/// <summary>
		///	ソースデータ取得
		///	/// </summary>
		protected bool GetSourceData(int source_address, DataSize dataSize, bool isBig, out uint resData)
		{
			bool result = false;
			uint d0,d1,d2,d3;

			resData = 0;
			switch(dataSize)
			{
				case DataSize.DB:
					if (CheckSourceDataAddressRange(source_address))
					{
						resData = (uint)BinaryArray[source_address];
						result = true;
					}
					break;
				case DataSize.DW:
					if (CheckSourceDataAddressRange(source_address))
					{
						d0 = (uint)BinaryArray[source_address];
						if (CheckSourceDataAddressRange(source_address+1)) 
						{
							d1 = (uint)BinaryArray[source_address+1];
							if (isBig)
							{
								resData = ((d0 << 8) & 0xFF00) | (d1 & 0xFF);
							}
							else 
							{
								resData = ((d1 << 8) & 0xFF00) | (d0 & 0xFF);
							}
							result = true;
						}
					}
					break;
				case DataSize.DL:
					if (CheckSourceDataAddressRange(source_address))
					{
						d0 = (uint)BinaryArray[source_address];
						if (CheckSourceDataAddressRange(source_address+1)) 
						{
							d1 = (uint)BinaryArray[source_address+1];
							if (CheckSourceDataAddressRange(source_address+2)) 
							{
								d2 = (uint)BinaryArray[source_address+2];
								if (CheckSourceDataAddressRange(source_address+3)) 
								{
									d3 = (uint)BinaryArray[source_address+3];
									if (isBig)
									{
										resData = ((d0 << 24) & 0xFF000000) | ((d1 << 16) & 0xFF0000) | ((d2 << 8) & 0xFF00) | (d3 & 0xFF);
									}
									else 
									{
										resData = ((d3 << 24) & 0xFF000000) | ((d2 << 16) & 0xFF0000) | ((d1 << 8) & 0xFF00) | (d0 & 0xFF);
									}
									result = true;
								}
							}
						}
					}
					break;
			}
			return result;
		}

		/// <summary>
		///	ソースデータのアドレスレンジチェック
		/// </summary>
		private bool CheckSourceDataAddressRange(int source_address)
		{
			if (source_address >= 0 && source_address < BinaryArray.Length)
			{
				return true;
			}
			ErrorString = $"Source Addresss:{source_address:X}H";
			return false;
		}

	}
}