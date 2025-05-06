using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace F1
{
	///	<summary>
	///	Source File フォーマット定義
	///	</summary>
	public enum SourceFormat
	{
		NONE,
		F1,
		F1T,
		S98,
		VGM,
		MDX,
	}

	///	<summary>
	///	デュアル CHIP のプレイモード定義
	///	</summary>
	public enum DualMode
	{
		NONE,
		FIRST,		//	１つめの CHIP
		SECOND,		//	２つめの CHIP
		BOTH,		//	両方の CHIP
	}
	///	<summary>
	///	デュアル CHIP の番号
	///	</summary>
	public enum DualNumber
	{
		DualNone = -1,
		Dual1st = 1,	//	１つめの CHIP
		Dual2nd = 2,	//	２つめの CHIP
	}

	///	<summary>
	///	データサイズ指定の定義
	///	</summary>
	public enum DataSize
	{
		DB,			//	 8 Bits.
		DW,			//	16 Bits.
		DL,			//	32 Bits.
	};

	///	<summary>
	///	アクティブ状態の定義
	///	</summary>
	public enum ActiveStatus
	{
		INACTIVE = 0,
		ACTIVE = 1,
		DESTROYED = 2,
	}

	///	<summary>
	///	バイトレジスタの書き込み順の定義
	///	</summary>
	public enum ByteRegOrder
	{
		NONE,
		LOW,
		HI,
	}

	///	<summary>
	///	サウンド CHIP タイプの定義
	///	</summary>
	public enum ChipType
	{
		NONE,
		YM2151,		//	OPM
		YM2203,		//	OPN
		YM2608,		//	OPNA
		YM2612,		//	OPN2
		YM2610,		//	OPNB
		YM2610B,	//	OPNB2
		YMF288,		//	OPN3
		YM3526,		//	OPL
		YM3812,		//	OPL2
		Y8950,		//	MSXAUDIO
		YMF262,		//	OPL4
		YM2413,		//	OPLL
		SN76489,	//	DCSG
		AY_3_8910,	//	PSG
		YM2149,		//	YPSG
		K051649,	//	SCC
		K052539,	//	SCCI
		M6258,		//	M6258
		M6295,		//	M6295
		K053260, 	//	K053260
	};

	///	<summary>
	///	PCM 機能の定義
	///	</summary>
	public enum PcmFunctionType
	{
		NONE,
		HAS_PCM,	//	他音源機能と PCM 機能を持つ
		HAS_DAC,	//	他音源機能と DAC 機能を持つ
		PCM_ONLY,	//	PCM 機能だけを持つ
		DAC_ONLY,	//	DAC 機能だけを持つ
	}

	///	<summary>
	///	PCM の データタイプ
	///	</summary>
	public enum PcmDataType
	{
		PcmData0 = 0,
		PcmData1 = 1,
	}

	///	<summary>
	///	サウンド CHIP の PCM 機能のディクショナリ
	///	</summary>
	public static class ChipPcmFunction 
	{
		public static PcmFunctionType GetPcmFunction(ChipType chipType)
		{
			if (ChipPcmFunctionDict.ContainsKey(chipType))
			{
				return ChipPcmFunctionDict[chipType];
			}
			return PcmFunctionType.NONE;
		}
		public static readonly Dictionary<ChipType, PcmFunctionType> ChipPcmFunctionDict = new Dictionary<ChipType, PcmFunctionType>()
		{
			{ ChipType.YM2151,		PcmFunctionType.NONE		},	//	OPM
			{ ChipType.YM2203,		PcmFunctionType.NONE		},	//	OPN
			{ ChipType.YM2608,		PcmFunctionType.HAS_PCM		},	//	OPNA
			{ ChipType.YM2612,		PcmFunctionType.HAS_DAC		},	//	OPN2
			{ ChipType.YM2610,		PcmFunctionType.HAS_PCM		},	//	OPNB
			{ ChipType.YM2610B,		PcmFunctionType.HAS_PCM		},	//	OPNB2
			{ ChipType.YMF288,		PcmFunctionType.NONE		},	//	OPN3
			{ ChipType.YM3526,		PcmFunctionType.NONE		},	//	OPL
			{ ChipType.YM3812,		PcmFunctionType.NONE		},	//	OPL2
			{ ChipType.Y8950,		PcmFunctionType.HAS_PCM		},	//	MSXAUDIO
			{ ChipType.YMF262,		PcmFunctionType.NONE		},	//	OPL4
			{ ChipType.YM2413,		PcmFunctionType.NONE		},	//	OPLL
			{ ChipType.SN76489,		PcmFunctionType.NONE		},	//	DCSG
			{ ChipType.AY_3_8910,	PcmFunctionType.NONE		},	//	PSG
			{ ChipType.YM2149,		PcmFunctionType.NONE		},	//	YPSG
			{ ChipType.K051649,		PcmFunctionType.NONE		},	//	SCC
			{ ChipType.K052539,		PcmFunctionType.NONE		},	//	SCCI
			{ ChipType.M6258,		PcmFunctionType.PCM_ONLY	},	//	M6258
			{ ChipType.M6295,		PcmFunctionType.PCM_ONLY	},	//	M6295
			{ ChipType.K053260,		PcmFunctionType.PCM_ONLY	},	//	K053260
		};
	};

	///	<summary>
	///	サウンド CHIP の 互換データ構造
	///	</summary>
	public struct ChipCompatibleData
	{
		public ChipType m_chipType;
		public bool m_isPcmActive;
		public bool m_isDualIn1Chip;
	}
	///	<summary>
	///	サウンド CHIP の 互換データクラス
	///	</summary>
	public static class ChipCompatible
	{
		///	<summary>
		///	サウンド CHIP の 互換データを取得する
		///	</summary>
		public static bool GetChipCompatibleDataArray(ChipType targetChipType, out ChipCompatibleData[] chipCompatibleDataArray)
		{
			chipCompatibleDataArray = new ChipCompatibleData[0];
			if (ChipCompatibleDataDict.ContainsKey(targetChipType))
			{
				chipCompatibleDataArray = ChipCompatibleDataDict[targetChipType];
				return true;
			}
			return false;
		}
		///	<summary>
		///	サウンド CHIP の 互換データをディクショナリ
		///	<NOTE>	ターゲット CHIP が対応可能の、パース CHIP を配列で定義する
		///	</summary>
		private static readonly ReadOnlyDictionary<ChipType, ChipCompatibleData[]>ChipCompatibleDataDict = 
			new ReadOnlyDictionary<ChipType, ChipCompatibleData[]>(new Dictionary<ChipType, ChipCompatibleData[]>()
			{
				{ ChipType.YM2151, new ChipCompatibleData[] {
					new ChipCompatibleData() {m_chipType = ChipType.YM2151,		m_isPcmActive = false,	m_isDualIn1Chip = false}}
				},
				{ ChipType.YM2203, new ChipCompatibleData[] {
					new ChipCompatibleData() {m_chipType = ChipType.YM2203,		m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.YM2608,		m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.AY_3_8910,	m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.YM2149,		m_isPcmActive = false,	m_isDualIn1Chip = false}}
				},
				{ ChipType.YM2608, new ChipCompatibleData[] {
					new ChipCompatibleData() {m_chipType = ChipType.YM2608,		m_isPcmActive = true,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.YM2203,		m_isPcmActive = false,	m_isDualIn1Chip = true },
					new ChipCompatibleData() {m_chipType = ChipType.YM2610,		m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.YM2610B,	m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.YM2612,		m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.AY_3_8910,	m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.YM2149,		m_isPcmActive = false,	m_isDualIn1Chip = false}}
				},
				{ ChipType.YM2612, new ChipCompatibleData[] {
					new ChipCompatibleData() {m_chipType = ChipType.YM2612, 	m_isPcmActive = true,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.YM2608, 	m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.YM2203, 	m_isPcmActive = false,	m_isDualIn1Chip = true },
					new ChipCompatibleData() {m_chipType = ChipType.YM2610, 	m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.YM2610B,	m_isPcmActive = false,	m_isDualIn1Chip = false}}
				},
				{ ChipType.YM2610, new ChipCompatibleData[] {
					new ChipCompatibleData() {m_chipType = ChipType.YM2610,		m_isPcmActive = true,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.YM2610B,	m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.YM2608,		m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.YM2612,		m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.YM2203,		m_isPcmActive = false,	m_isDualIn1Chip = true },
					new ChipCompatibleData() {m_chipType = ChipType.AY_3_8910,	m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.YM2149,		m_isPcmActive = false,	m_isDualIn1Chip = false}}
				},
				{ ChipType.YM2610B, new ChipCompatibleData[] {
					new ChipCompatibleData() {m_chipType = ChipType.YM2610B,	m_isPcmActive = true,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.YM2610,		m_isPcmActive = true,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.YM2608,		m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.YM2612,		m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.YM2203,		m_isPcmActive = false,	m_isDualIn1Chip = true },
					new ChipCompatibleData() {m_chipType = ChipType.AY_3_8910,	m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.YM2149,		m_isPcmActive = false,	m_isDualIn1Chip = false}}
				},
				{ ChipType.YMF288, new ChipCompatibleData[] {
					new ChipCompatibleData() {m_chipType = ChipType.YM2608,		m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.YM2203,		m_isPcmActive = false,	m_isDualIn1Chip = true },
					new ChipCompatibleData() {m_chipType = ChipType.YM2610,		m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.YM2610B,	m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.YM2612,		m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.AY_3_8910,	m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.YM2149,		m_isPcmActive = false,	m_isDualIn1Chip = false}}
				},
				{ ChipType.YM3526, new ChipCompatibleData[] {
					new ChipCompatibleData() {m_chipType = ChipType.YM3526,		m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.YM3812,		m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.Y8950,		m_isPcmActive = false,	m_isDualIn1Chip = false}}
				},
				{ ChipType.YM3812, new ChipCompatibleData[] {
					new ChipCompatibleData() {m_chipType = ChipType.YM3812, 	m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.YM3526, 	m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.Y8950, 		m_isPcmActive = false,	m_isDualIn1Chip = false}}
				},
				{ ChipType.YMF262, new ChipCompatibleData[] {
					new ChipCompatibleData() {m_chipType = ChipType.YMF262,		m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.YM3812,		m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.YM3526,		m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.Y8950,		m_isPcmActive = false,	m_isDualIn1Chip = false}}
				},
				{ ChipType.YM2413, new ChipCompatibleData[] {
					new ChipCompatibleData() {m_chipType = ChipType.YM2413,		m_isPcmActive = false,	m_isDualIn1Chip = false}}
				},
				{ ChipType.SN76489, new ChipCompatibleData[] {
					new ChipCompatibleData() {m_chipType = ChipType.SN76489,	m_isPcmActive = false,	m_isDualIn1Chip = false}}
				},
				{ ChipType.AY_3_8910, new ChipCompatibleData[] {
					new ChipCompatibleData() {m_chipType = ChipType.AY_3_8910,	m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.YM2149,		m_isPcmActive = false,	m_isDualIn1Chip = false}}
				},
				{ ChipType.YM2149, new ChipCompatibleData[] {
					new ChipCompatibleData() {m_chipType = ChipType.YM2149,		m_isPcmActive = false,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.AY_3_8910,	m_isPcmActive = false,	m_isDualIn1Chip = false}}
				},
				{ ChipType.K051649, new ChipCompatibleData[] {
					new ChipCompatibleData() {m_chipType = ChipType.K051649,	m_isPcmActive = false,	m_isDualIn1Chip = false}}
				},
				{ ChipType.K052539, new ChipCompatibleData[] {
					new ChipCompatibleData() {m_chipType = ChipType.K052539,	m_isPcmActive = false,	m_isDualIn1Chip = false}}
				},
				{ ChipType.M6258, new ChipCompatibleData[] {
					new ChipCompatibleData() {m_chipType = ChipType.M6258,		m_isPcmActive = true,	m_isDualIn1Chip = false}}
				},
				{ ChipType.M6295, new ChipCompatibleData[] {
					new ChipCompatibleData() {m_chipType = ChipType.M6295,  	m_isPcmActive = true,	m_isDualIn1Chip = false},
					new ChipCompatibleData() {m_chipType = ChipType.M6258,  	m_isPcmActive = true,	m_isDualIn1Chip = false}}
				},
				{ ChipType.K053260, new ChipCompatibleData[] {
					new ChipCompatibleData() {m_chipType = ChipType.K053260,	m_isPcmActive = true,	m_isDualIn1Chip = false}}
				}
			}
		);
	}

	///	<summary>
	///	16-Bit(WORD)音程データ変換データクラス
	/// </summary>
	public class ConvertWordToneData
	{
		public int m_fmPrescaler;
		public int m_ssgPrescaler;

		public int m_lowData;
		public int m_lowPlayImDataIndex;
		public uint m_lowMilliSec;

		public int m_hiData;
		public int m_hiPlayImDataIndex;
		public uint m_hiMilliSec;
		public byte m_hiCovertedData;

		public ByteRegOrder m_lastByteRegOrder;
		public bool m_isOldData;
		public uint m_oldData;
		public ConvertWordToneData()
		{
			this.m_lowData = 0;
			this.m_lowPlayImDataIndex = -1;
			this.m_lowMilliSec = 0;

			this.m_hiData = 0;
			this.m_hiCovertedData = 0;
			this.m_hiPlayImDataIndex = -1;
			this.m_hiMilliSec = 0;

			this.m_lastByteRegOrder = ByteRegOrder.NONE;
			this.m_isOldData = false;
		}
	}

	///	<summary>
	//	F1T の予約語定義
	/// </summary>
	public static class F1TReservedWord
	{
		public enum F1TLabel
		{
			HEADER,			//	0
			PLAY_DATA,		//	1
			PCM_DATA,		//	2
			MAX,
		}
		public static readonly string[] F1TLabelStrings = 
		{
			"F1Header:",		//	0
			"F1PlayData:",		//	1
			"F1PcmData:",		//	2
		};
		public enum F1THeaderOpecode
		{
			VERSION,		//	0
			LOOP_COUNT,		//	1
			ONE_WAIT_NS,	//	2
			CMD_END,		//	3
			CMD_A1,			//	4
			CMD_CS,			//	5
			CMD_LP,			//	6
			CMD_BYTE_W,		//	7
			CMD_WORD_W,		//	8
			CMD_W1,			//	9
			CMD_W2,			//	10
			CMD_W3,			//	11
			CMD_W4,			//	12
			CMD_W5,			//	13
			CMD_W6,         //	14
			CMD_WR_WAIT,    //	15
			CMD_WR_WAIT_RL, //	16
			CMD_WR_SEEK,    //	17
			CMD_F0,			//	18
			CMD_F1,			//	19
			CMD_F2,			//	20
			CMD_F3,			//	21
			CMD_F4,			//	22
			MAX,
		}
		public static readonly string[] F1THeaderOpecodeStrings = 
		{
			"Version",		//	0
			"LoopCount",	//	1
			"OneWaitNs",	//	2
			"CmdEnd",		//	3
			"CmdA1",		//	4
			"CmdCS",		//	5
			"CmdLp",		//	6
			"CmdByteW",		//	7
			"CmdWordW",		//	8
			"CmdW1",		//	9
			"CmdW2",		//	10
			"CmdW3",		//	11
			"CmdW4",		//	12
			"CmdW5",		//	13
			"CmdW6",		//	14
			"CmdWrByteW",	//	15
			"CmdWrRLW",		//	16
			"CmdWrSeek",	//	17
			"CmdF0",		//	18
			"CmdF1",		//	19
			"CmdF2",		//	20
			"CmdF3",		//	21
			"CmdF4",		//	22
		};
		public enum F1TPlayDataOpecode
		{
			END = 0,		//	0
			CS = 1,			//	1
			A1 = 2,			//	2
			LP = 3,			//	3
			WAIT = 4,		//	4
			WRWAIT = 5,		//	5
			WRWAITRL = 6,	//	6
			WRSEEK = 7,		//	7
			MAX,
		}
		public static readonly string[] F1TPlayDataOpecodeStrings = 
		{
			"End",			//	0
			"ChCS",			//	1
			"ChA1",			//	2
			"LoopPoint",	//	3
			"Wait",			//	4
			"WrWait",		//	5
			"WrWaitRL",		//	6
			"WrSeek",		//	7
		};
		public enum F1TPcmDataOpecode
		{
			PCMHEADER,	//	0
			DATA,		//	1
			MAX,
		}
		public static readonly string[] F1TPcmDataOpecodeStrings = 
		{
			"PcmHeader",	//	0
			"data",			//	1
		};
	}
}