using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;

namespace F1
{
	/// <summary>
	///	VGM フォーマット パーサー クラス
	/// </summary>
	public class VgmParser : Parser
	{
		private readonly uint VGM_ONE_WAIT = 22676;			//	22,675.73696[ns]

		private int m_vgmVersion;
		private int m_sourceAddress;
		private int m_vgmStartAddress;
		private int m_m6258SamplingRate;

		/// <summary>
		///	VGM ストリームのクラス
		/// </summary>
		private class VGMStream
		{
			public ParseChip m_parseChip;
			public int m_port;
			public int m_register;
			public int m_dataBankId;
			public VGMStream(ParseChip parseChip, int port, int register)
			{
				this.m_parseChip = parseChip;
				this.m_port = port;
				this.m_register = register;
				this.m_dataBankId = 0;
			}
		}
		/// <summary>
		///	VGM ストリームId のディクショナリ
		/// </summary>
		private Dictionary<int, VGMStream>m_vgmStreamDict = new Dictionary<int, VGMStream>();

		/// <summary>
		///	VGM フォーマット パース
		/// </summary>
		public override bool Parse()
		{
			int loopPoint_address = 0;
			uint tmp_d0 = 0;

			m_m6258SamplingRate = 15625;

			//	F1 ヘッダーのサンプル時間に VGM の WAIT時間を設定
			Header.SetOneCycleNs(VGM_ONE_WAIT);

			//	VGM ヘッダーのバージョンを取得
			if (!GetSourceData(0x0008, DataSize.DL, false, out tmp_d0)) return false;
			m_vgmVersion = (int)tmp_d0;

			//	VGM ヘッダーのデータオフセットを定める
			if (!GetSourceData(0x0034, DataSize.DL, false, out tmp_d0)) return false;
			//	Prior to Version 1.50, VGM data offset was 0 and VGM data started at offset 0x40.
			m_sourceAddress = (tmp_d0 == 0) ? 0x40 : ((int)(tmp_d0 + 0x34));
			m_vgmStartAddress = m_sourceAddress;

			//	VGM ヘッダーのループポイントを取得
			if (!GetVgmHeaderData( 0x1C, DataSize.DL, false, out tmp_d0)) return false;
			if (tmp_d0 != 0 )
			{
				loopPoint_address = (int)(tmp_d0 + 0x1C);
			}
			//	VGM ヘッダーを解析
			if (!ParseVgmHeaderChip()) return false;

			//	VGM データブロックの解析
			if (!ParseVGMDataBLock()) return false;

			//	VGM コマンドデータをパース
			bool isDataEnd = false;
			while(!isDataEnd)
			{
				bool isCommandReady = false;
				uint vgmCommand = 0;
				if (!GetSourceData(m_sourceAddress, DataSize.DB, false, out vgmCommand))
				{	//	終了コマンドがない状態で、コマンドデータがなくなった場合は、強制終了させる。
					ErrorString = null;
					AddWarningString("WARNING : VGM End mark not found.");
					AddEndCodeToPlayImData();
					isDataEnd = true;
					continue;
				}

				//	VGM アドレス	ループアドレスチェック
				if (m_sourceAddress == loopPoint_address)
				{
					AddLoopPointToPlayImData();
				}
				m_sourceAddress += 1;

				//	VGM コマンド	サウンドデータ終了	0x66
				if (vgmCommand == 0x66)
				{
					isDataEnd = true;
					AddEndCodeToPlayImData();
					continue;
				}

				//	VGM コマンド	ストリーム			0x90-0x95
				if (vgmCommand >= 0x90 && vgmCommand <= 0x95)
				{
					if (!CheckCommand_Stream(vgmCommand)) return false;
					continue;
				}
				//	VGM コマンド	CHIP レジスタ
				if (!CheckCommand_ChipRegister(vgmCommand, out isCommandReady)) return false;
				if (isCommandReady) continue;

				//	VGM コマンド	サイクル WAIT 	0x61-0x63	0x70-0x7F
				if (!CheckCommand_CycleWait(vgmCommand, out isCommandReady)) return false;
				if (isCommandReady) continue;

				//	VGM コマンド	YM2612 書き込み WAIT	0x80-0x8F
				if (vgmCommand >= 0x80 && vgmCommand <= 0x8F)
				{
					SetWriteWait((int)(vgmCommand  & 0x0F));
					continue;
				}

				// VGM コマンド		YM2612 書き込みシーク	0xE0
				if (vgmCommand == 0xE0)
				{
					if (!GetSourceData(m_sourceAddress, DataSize.DL, false, out tmp_d0)) return false;
					m_sourceAddress += 4;
					foreach(var parseChip in m_parseChipList.Where(x => x.ChipActiveStatus == ActiveStatus.ACTIVE))
					{
						var targetChip = TargetHardware.TargetChipList[parseChip.ChipSelect];
						if (targetChip.IsTargetPcmActive && targetChip.TargetChipType == ChipType.YM2612)
						{
							AddWriteSeekToPlayImData((int)tmp_d0);
						}
					}
					continue;
				}

				// VGM コマンド		コマンドスキップ
				switch(vgmCommand)
				{
					case 0x30:	//	PSG (SN76489/SN76496) write value dd	DUAL_Chip.
						m_sourceAddress += 1;
						break;
					case 0x31:	//	Reserve 1 Ope.
					case 0x32:	//	Reserve 1 Ope.
					case 0x33:	//	Reserve 1 Ope.
					case 0x34:	//	Reserve 1 Ope.
					case 0x35:	//	Reserve 1 Ope.
					case 0x36:	//	Reserve 1 Ope.
					case 0x37:	//	Reserve 1 Ope.
					case 0x38:	//	Reserve 1 Ope.
					case 0x39:	//	Reserve 1 Ope.
					case 0x3A:	//	Reserve 1 Ope.
					case 0x3B:	//	Reserve 1 Ope.
					case 0x3C:	//	Reserve 1 Ope.
					case 0x3D:	//	Reserve 1 Ope.
					case 0x3E:	//	Reserve 1 Ope.
					case 0x3F:	//	Reserve 1 Ope.
						m_sourceAddress += 1;
						break;
					case 0x40:	//	Reserve 1 Ope.
					case 0x41:	//	Reserve 1 Ope.
					case 0x42:	//	Reserve 1 Ope.
					case 0x43:	//	Reserve 1 Ope.
					case 0x44:	//	Reserve 1 Ope.
					case 0x45:	//	Reserve 1 Ope.
					case 0x46:	//	Reserve 1 Ope.
					case 0x47:	//	Reserve 1 Ope.
					case 0x48:	//	Reserve 1 Ope.
					case 0x49:	//	Reserve 1 Ope.
					case 0x4A:	//	Reserve 1 Ope.
					case 0x4B:	//	Reserve 1 Ope.
					case 0x4C:	//	Reserve 1 Ope.
					case 0x4D:	//	Reserve 1 Ope.
					case 0x4E:	//	Reserve 1 Ope.
						if (m_vgmVersion < 0x161) 
						{
							m_sourceAddress += 1;
						} else {
							m_sourceAddress += 2;
						}
						break;
					case 0x4F:	//	Game Gear PSG stereo, write dd to port 0x06
					case 0x50:	//	PSG (SN76489/SN76496) write value dd
						m_sourceAddress += 1;
						break;
					case 0x51: 	//	aa dd : YM2413, write value dd to register aa
					case 0x52: 	//	aa dd : YM2612 port 0, write value dd to register aa
					case 0x53: 	//	aa dd : YM2612 port 1, write value dd to register aa
					case 0x54:	//	aa dd : YM2151,write value dd to register aa
					case 0x55:	//	aa dd : YM2203,write value dd to register aa
					case 0x56:	//	aa dd : YM2608 port 0, write value dd to register aa
					case 0x57: 	//	aa dd : YM2608 port 1, write value dd to register aa
					case 0x58: 	//	aa dd : YM2610 port 0, write value dd to register aa
					case 0x59: 	//	aa dd : YM2610 port 1, write value dd to register aa
					case 0x5A: 	//	aa dd : YM3812, write value dd to register aa
					case 0x5B: 	//	aa dd : YM3526, write value dd to register aa
					case 0x5C: 	//	aa dd : Y8950, write value dd to register aa
					case 0x5D: 	//	aa dd : YMZ280B, write value dd to register aa
					case 0x5E: 	//	aa dd : YMF262 port 0, write value dd to register aa
					case 0x5F: 	//	aa dd : YMF262 port 1, write value dd to register aa
						m_sourceAddress += 2;
						break;
					case 0x68:	//	PCM RAM write
						m_sourceAddress += 11;
						break;
					case 0xA0:	//	aa dd : AY8910, write value dd to register aa
						m_sourceAddress += 2;
						break;
					case 0xA1:	//	aa dd : YM2413, write value dd to register aa			DUAL_Chip.
					case 0xA2:	//	aa dd : YM2612 port 0, write value dd to register aa	DUAL_Chip.
					case 0xA3:	//	aa dd : YM2612 port 1, write value dd to register aa	DUAL_Chip.
					case 0xA4:	//	aa dd : YM2151, write value dd to register aa			DUAL_Chip.
					case 0xA5:	//	aa dd : YM2203, write value dd to register aa			DUAL_Chip.
					case 0xA6:	//	aa dd : YM2608 port 0, write value dd to register aa	DUAL_Chip.
					case 0xA7:	//	aa dd : YM2608 port 1, write value dd to register aa	DUAL_Chip.
					case 0xA8:	//	aa dd : YM2610 port 0, write value dd to register aa	DUAL_Chip.
					case 0xA9:	//	aa dd : YM2610 port 1, write value dd to register aa	DUAL_Chip.
					case 0xAA:	//	aa dd : YM3812, write value dd to register aa			DUAL_Chip.
					case 0xAB:	//	aa dd : YM3526, write value dd to register aa			DUAL_Chip.
					case 0xAC:	//	aa dd : Y8950, write value dd to register aa			DUAL_Chip.
					case 0xAD:	//	aa dd : YMZ280B, write value dd to register aa			DUAL_Chip.
					case 0xAE:	//	aa dd : YMF262 port 0, write value dd to register aa	DUAL_Chip.
					case 0xAF:	//	aa dd : YMF262 port 1, write value dd to register aa	DUAL_Chip.
					case 0xB0:	//	aa dd : RF5C68, write value dd to register aa
					case 0xB1:	//	aa dd : RF5C164, write value dd to register aa
					case 0xB2:	//	ad dd : PWM, write value ddd to register a (d is MSB, dd is LSB)
					case 0xB3:	//	aa dd : GameBoy DMG, write value dd to register aa
					case 0xB4:	//	aa dd : NES APU, write value dd to register aa
					case 0xB5:	//	aa dd : MultiPCM, write value dd to register aa
					case 0xB6:	//	aa dd : uPD7759, write value dd to register aa
					case 0xB7:	//	aa dd : OKIM6258, write value dd to register aa
					case 0xB8:	//	aa dd : OKIM6295, write value dd to register aa
					case 0xB9:	//	aa dd : HuC6280, write value dd to register aa
					case 0xBA:	//	aa dd : K053260, write value dd to register aa
					case 0xBB:	//	aa dd : Pokey, write value dd to register aa
						m_sourceAddress += 2;
						break;
					case 0xC0:	//	aaaa dd : Sega PCM, write value dd to memory offset aaaa
					case 0xC1:	//	aaaa dd : RF5C68, write value dd to memory offset aaaa
					case 0xC2:	//	aaaa dd : RF5C164, write value dd to memory offset aaaa
					case 0xC3:	//	cc aaaa : MultiPCM, write set bank offset aaaa to channel cc
					case 0xC4:	//	mmll rr : QSound, write value mmll to register rr (mm - data MSB, ll - data LSB)
					case 0xD0:	//	pp aa dd : YMF278B port pp, write value dd to register aa
					case 0xD1:	//	pp aa dd : YMF271 port pp, write value dd to register aa
					case 0xD2:	//	pp aa dd : SCC,SCC-I port pp, write value dd to register aa
					case 0xD3:	//	pp aa dd : K054539 write value dd to register ppaa
					case 0xD4:	//	pp aa dd : C140 write value dd to register ppaa
						m_sourceAddress += 3;
						break;
					default:
						ErrorString = $"VGM data not supooorted {tmp_d0:X}. Address;{m_sourceAddress:X}.";
						return false;
				}
			}
			CreateResultMessage();
			return true;
		}

		/// <summary>
		///	VGM コマンド	サイクル WAIT	0x61-0x63	0x70-0x7F
		/// </summary>
		private bool CheckCommand_CycleWait(uint vgmCommand, out bool isCommandReady)
		{
			uint tmp_d0 = 0;
			int waitCount = 0;

			isCommandReady = false;
			switch(vgmCommand)
			{
				case 0x61:	//	nn nn : Wait n samples, max 65535 (approx 1.49[s]). Longer pauses than this are represented by multiple wait commands.
					if (!GetSourceData(m_sourceAddress, DataSize.DW, false, out tmp_d0)) return false;
					m_sourceAddress += 2;
					SetCycleWait((int)tmp_d0);
					isCommandReady = true;
					break;
				case 0x62:	// wait 735 samples (60th of a second), a shortcut for
					SetCycleWait(735);
					isCommandReady = true;
					break;
				case 0x63:	//	wait 882 samples (50th of a second), a shortcut for
					SetCycleWait(882);
					isCommandReady = true;
					break;
				default:
					if (vgmCommand >= 0x70 &&  vgmCommand <= 0x7F) 
					{		//	wait n+1 samples, n can range from 0 to 15
						waitCount = (int)(vgmCommand & 0x0F)+1;
						SetCycleWait(waitCount);
						isCommandReady = true;
					}
					break;
			}
			return true;
		}
		private void SetCycleWait(int cycleWait)
		{
			if (cycleWait > 0)
			{
				AddCycleWaitToPlayImData(cycleWait);
			}
		}
		private void SetWriteWait(int writeWait)
		{
			foreach(var parseChip in m_parseChipList.Where(x => x.ChipActiveStatus == ActiveStatus.ACTIVE))
			{
				var targetChip = TargetHardware.TargetChipList[parseChip.ChipSelect];
				if (targetChip.IsTargetPcmActive && targetChip.TargetChipType == ChipType.YM2612)
				{
					AddWriteWaitToPlayImData(writeWait, isUseRunLength:true);
					return;
				}
			}
			SetCycleWait(writeWait);
		}

		/// <summary>
		///	VGM コマンド	データブロックの解析
		/// </summary>
		private bool ParseVGMDataBLock()
		{
			//	チップセレクトで、格納アドレスを保持しておく
			Dictionary<int, int>startAddressDict = new Dictionary<int, int>();

			//	すべてのデータブロックをループ処理する
			while(true)
			{	//	コマンドを取得
				uint vgmCommand;
				if (!GetSourceData(m_sourceAddress, DataSize.DB, false, out vgmCommand)) return false;
				//	コマンドがデータブロックでない場合は終了
				if (vgmCommand != 0x67) 
				{
					break;
				}
				uint tmpData = 0;

				m_sourceAddress ++;
				var blockAddress = m_sourceAddress;

				//	VGM の互換性コマンドを取得		DB	(0x66)	+1
				if (!GetSourceData(m_sourceAddress,	  DataSize.DB, false, out tmpData)) return false;
				var compatibleData = tmpData;

				//	BLK データタイプ				DB			+1	= 2
				if (!GetSourceData(m_sourceAddress+1, DataSize.DB, false, out tmpData)) return false;
				var blk_dataType = (int)tmpData;

				//	BLK データサイズ				DL			+4	= 6
				if (!GetSourceData(m_sourceAddress+2, DataSize.DL, false, out tmpData)) return false;
				bool blk_isDualData = false;
				if (tmpData > 0x7FFFFFFF)
				{	//	データサイズの最上位ビットは２つめの CHIPを示すフラグ。フラグを確保して、最上位ビットをクリアする
					blk_isDualData = true;
					tmpData &= 0x7FFFFFFF;
				}
				var blk_dataSize = (int)tmpData;

				//	ソースアドレスをデータサイズ分進めておく
				m_sourceAddress += 6;
				var blk_dataAddress = m_sourceAddress;
				m_sourceAddress += blk_dataSize;

				//	BLK データタイプが、0x80-0xBFのデータブロックは、ROM/RAM のイメージダンプ
				var blk_isRomRamImage = false;
				uint dataBlockStartAddress = 0x00000000;
				if (blk_dataType >= 0x80 && blk_dataType <= 0xBF)
				{	//	４バイトは ROM全体のサイズ ROM全体のサイズは使わないから読み飛ばす
					blk_dataAddress += 4;
					//	４バイトが開始アドレス
					if (!GetSourceData(blk_dataAddress, DataSize.DL, false, out dataBlockStartAddress)) return false;
					blk_dataAddress += 4;
					//	サイズと開始アドレスの８バイトを差し引いた値が、実データのサイズ
					blk_dataSize -= 8;
					blk_isRomRamImage = true;
				}

				//	ターゲットハードのPCM使用がオフ		データブロックを読み飛ばす
				if (!TargetHardware.IsUsePCM)
					continue;
				//	VGM の 互換性コマンドが0x66でない	データブロックを読み飛ばす
				if (compatibleData != 0x66)
					continue;
				//	BLK データサイズが異常				データブロックを読み飛ばす
				if (blk_dataSize < 0)
					continue;
				//	BLK データタイプの設定が存在しない	データブロックを読み飛ばす
				if (!DataBlockTypeSettingDict.ContainsKey(blk_dataType))
					continue;

				//	BLK データタイプの設定
				var dataBlockTypeSetting = DataBlockTypeSettingDict[blk_dataType];

				//	ループ後にデータブロックを取り込めたか？フラグ
				var isReadyDataBlock = false;
				//	パース CHIP（アクティブで DUALナンバーがなし or １つ目）でループ
				foreach(var parseChip in m_parseChipList.Where(x => x.ChipActiveStatus == ActiveStatus.ACTIVE && x.ChipDualNumber != DualNumber.Dual2nd))
				{
					var targetChip = TargetHardware.TargetChipList[parseChip.ChipSelect];
					//	データブロックが DUAL ２つめの CHIP の場合
					if (blk_isDualData)
					{	//	パースチップが DUAL １つ目の場合
						if (parseChip.ChipDualNumber == DualNumber.Dual1st && m_parseChipList[parseChip.ChipDualIndex].ChipActiveStatus == ActiveStatus.ACTIVE)
						{	//	ターゲット CHIP を、DUAL ２つめのターゲット CHIP にする
							targetChip = TargetHardware.TargetChipList[m_parseChipList[parseChip.ChipDualIndex].ChipSelect];
						}
						else
						//	パースチップが DUAL ナシの場合
						{	//	次のパース CHIP
							continue;
						}
					}
					//	ターゲット CHIP が、データブロックの対象データと合致するかを判定
					var tmpIndex = Array.IndexOf(dataBlockTypeSetting.m_toUseChipArray, targetChip.TargetChipType);
					if (tmpIndex < 0)
					{	//	合致しない場合	次のパース CHIP 
						continue;
					}
					//	データブロックが RomRam イメージでない場合
					if (!blk_isRomRamImage)
					{
						if (!startAddressDict.ContainsKey(targetChip.ChipSelect))
						{	//	格納アドレスがない場合は、アドレス 0x0000 を追加する
							startAddressDict.Add(targetChip.ChipSelect, blk_dataSize);
							dataBlockStartAddress = 0;
						}
						else
						{	//	格納アドレスがある場合は、次ブロックのためアドレスにサイズを加算する
							dataBlockStartAddress = (uint)startAddressDict[targetChip.ChipSelect];
							startAddressDict[targetChip.ChipSelect] += blk_dataSize;
						}
					}
					//	PCM データのバイナリを取り込む
					byte[] tmpPcmDataBuff = new byte[blk_dataSize];
					for (int i = 0; i < blk_dataSize; i ++) 
					{
						if (!GetSourceData(blk_dataAddress, DataSize.DB, false, out tmpData)) return false;
						blk_dataAddress += 1;
						tmpPcmDataBuff[i] = (byte)tmpData;
					}
					//	PCM中間にデータブロックを追加する
					ImData.AddPcmImDataList(targetChip.ChipSelect, dataBlockTypeSetting.m_pcmDataType, (int)dataBlockStartAddress, blk_dataSize, tmpPcmDataBuff);
					//	BLK データを取り込んだ
					isReadyDataBlock = true;
					break;
				}
				//	BLK データを取り込めなかった場合は、データブロックを読み飛ばす
				if (!isReadyDataBlock)
				{
					continue;
				}
			}
			return true;
		}

		/// <summary>
		///	VGM コマンド	ストリーム
		/// </summary>
		private bool CheckCommand_Stream(uint vgmCommand)
		{
			var strmSourceAddress = m_sourceAddress;
			uint tmp_d0;
			uint tmp_d1;
			uint tmp_d2;
			switch(vgmCommand)
			{
				//	ストリーム制御セットアップ		ss tt pp cc	
				case 0x90:
					m_sourceAddress += 4;
					if (TargetHardware.IsUsePCM)
					{	//	ストリームID 読み込み
						if (!GetSourceData(strmSourceAddress, DataSize.DB, false, out tmp_d0)) return false;
						var streamId = (int)tmp_d0;
						if (m_vgmStreamDict.ContainsKey(streamId))
						{	//	ストリーム ID がある場合は、同一ストリームID でセットアップが開始された警告表示をして、処理しない
							AddWarningString($"WARNING : VGM stream setup. Duplicate stream id. {streamId:X}. Address;{m_sourceAddress-4:X}.");
							break;
						}
						//	クロックテーブル番号を読み込み、DUAL と CHIP タイプを定める
						if (!GetSourceData(strmSourceAddress+1, DataSize.DB, false, out tmp_d0)) return false;
						bool isDual = ((tmp_d0 & 0x80) != 0);
						var clockTableNumber = ((int)tmp_d0) & 0x7F;
						var chipIndex = Array.FindIndex(VgmHeaderChipDataArray, x => x.m_clkTblNumber == clockTableNumber);
						if (chipIndex < 0)
						{	//	CHIP が見つからない場合は、警告を表示をして処理しない
							AddWarningString($"WARNING : VGM stream setup. No chip clock. Address;{m_sourceAddress-4:X}.");
							break;
						}
						var chipType = VgmHeaderChipDataArray[chipIndex].m_chipType;
						//	CHIP タイプからアクティブなパース CHIP を特定する
						var parseIndex = m_parseChipList.FindIndex(x => x.ChipActiveStatus == ActiveStatus.ACTIVE && x.ChipType == chipType);
						if (parseIndex < 0)
						{	//	パース CHIP が存在しない場合、警告表示をして、処理しない
							AddWarningString($"WARNING : VGM stream setup. Chip not actives. Address;{m_sourceAddress-4:X}.");
							break;
						}
						//	DUAL チップの場合
						if (isDual)
						{
							if (m_parseChipList[parseIndex].ChipDualNumber != DualNumber.Dual2nd)
							{	//	パース CHIP が DUAL でない場合、警告表示をして、処理しない
								AddWarningString($"WARNING : VGM stream setup. Chip No Actives. Address;{m_sourceAddress-4:X}.");
								break;
							}
							//	パース CHIP を ２つめの CHIP にする
							parseIndex = m_parseChipList[parseIndex].ChipDualIndex;
						}
						var parseChip = m_parseChipList[parseIndex];
						//	ターゲット CHIP が PCM を処理しない場合は、処理しない
						var targetChip = TargetHardware.TargetChipList[parseChip.ChipSelect];
						if (!targetChip.IsTargetPcmActive)
						{
							break;
						}
						//	パース CHIP が VGM ストリームを使用する CHIP でない場合は、処理しない
						if (Array.IndexOf(StreamEnableChipArray, parseChip.ChipType) < 0)
						{
							break;
						}
						//	ポート と 書き込みコマンド/レジスタを読み込み
						if (!GetSourceData(strmSourceAddress+2, DataSize.DB, false, out tmp_d0)) return false;
						if (!GetSourceData(strmSourceAddress+3, DataSize.DB, false, out tmp_d1)) return false;
						//	ストリーム ID でディクショナリにデータを加える
						var vgmStream = new VGMStream(parseChip, (int)tmp_d0, (int)tmp_d1);
						m_vgmStreamDict.Add(streamId, vgmStream);
					}
					break;

				//	ストリーム データ設定			ss dd ll bb
				case 0x91:
					m_sourceAddress += 4;
					if (TargetHardware.IsUsePCM)
					{	//	ストリームID 読み込み
						if (!GetSourceData(strmSourceAddress, DataSize.DB, false, out tmp_d0)) return false;
						var streamId = (int)tmp_d0;
						if (!m_vgmStreamDict.ContainsKey(streamId))
						{	//	ディクショナリにストリーム ID がない場合は、ストリームID が存在しない警告表示をして、処理しない
							AddWarningString($"WARNING : VGM stream set bank. No stream id. {streamId:X}. Address;{m_sourceAddress-4:X}.");
							break;
						}
						var vgmStream = m_vgmStreamDict[streamId];
						//	データバンクID
						if (!GetSourceData(strmSourceAddress+1, DataSize.DB, false, out tmp_d0)) return false;
						vgmStream.m_dataBankId = (int)tmp_d0;
						//	ステップサイズ
						if (!GetSourceData(strmSourceAddress+1, DataSize.DB, false, out tmp_d0)) return false;
						//	ステップベース
						if (!GetSourceData(strmSourceAddress+1, DataSize.DB, false, out tmp_d1)) return false;
						//	PLAY中間データへ VgmStreamData を格納
						var parseChip = m_vgmStreamDict[streamId].m_parseChip;
						AddVgmStreamDataToPlayImData(parseChip.ChipSelect, a1:0, code:(byte)0x91, vstrmStepSize:(byte)tmp_d0, vstrmStepBase:(byte)tmp_d1);
					}
					break;

				//	ストリーム 周波数設定			ss ff ff ff ff
				case 0x92:
					m_sourceAddress += 5;
					if (TargetHardware.IsUsePCM)
					{	//	ストリームID 読み込み
						if (!GetSourceData(strmSourceAddress, DataSize.DB, false, out tmp_d0)) return false;
						var streamId = (int)tmp_d0;
						if (!m_vgmStreamDict.ContainsKey(streamId))
						{	//	ディクショナリにストリーム ID がない場合は、ストリームID が存在しない警告表示をして、処理しない
							AddWarningString($"WARNING : VGM stream set frequency. No stream id. {streamId:X}. Address;{m_sourceAddress-4:X}.");
							break;
						}
						var vgmStream = m_vgmStreamDict[streamId];
						if (!GetSourceData(strmSourceAddress+1, DataSize.DL, false, out tmp_d0)) return false;
						var parseChip = m_vgmStreamDict[streamId].m_parseChip;

						var frequency = (int)tmp_d0;
						if (parseChip.ChipType == ChipType.M6258)
						{
							var fx2 = frequency * 2;
							m_m6258SamplingRate = 15625;
							int[] rateTable = { 7813, 10417, 15625, 3906, 5208 };
							for (int ri = 0; ri < rateTable.Length; ri ++)
							{
								if ( (fx2 < (rateTable[ri]+100)) && (fx2 > (rateTable[ri]-100)) )
								{
									m_m6258SamplingRate = rateTable[ri];
									break;
								}
							}
						}
						//	PLAY中間データへ VgmStreamSamplingRata を格納
						AddVgmStreamSamplingRataToPlayImData(parseChip.ChipSelect, a1:0, code:(byte)0x92, vstrmSamplingRate:m_m6258SamplingRate);
					}
					break;

				//	ストリームを開始			ss aa aa aa aa mm ll ll ll ll
				case 0x93:
					m_sourceAddress += 10;
					if (TargetHardware.IsUsePCM)
					{
						//	ストリームID 読み込み
						if (!GetSourceData(strmSourceAddress, DataSize.DB, false, out tmp_d0)) return false;
						var streamId = (int)tmp_d0;
						if (!m_vgmStreamDict.ContainsKey(streamId))
						{	//	ディクショナリにストリーム ID がない場合は、ストリームID が存在しない警告表示をして、処理しない
							AddWarningString($"WARNING : VGM Stream Start stream. No stream id. {streamId:X}. Address;{m_sourceAddress-4:X}.");
							break;
						}
						if (!GetSourceData(strmSourceAddress+1, DataSize.DL, false, out tmp_d0)) return false;
						if (!GetSourceData(strmSourceAddress+5, DataSize.DB, false, out tmp_d1)) return false;
						if (!GetSourceData(strmSourceAddress+6, DataSize.DL, false, out tmp_d2)) return false;
						//	PLAY中間データへ VgmStreamStartSizeToPlay を格納
						var parseChip = m_vgmStreamDict[streamId].m_parseChip;
						AddVgmStreamStartSizeToPlayImData(parseChip.ChipSelect, a1:0, code:(byte)0x93, vstrmFlagMode:(byte)tmp_d1, vstrmStart:(int)tmp_d0, vstrmSize:(int)tmp_d2);
					}
					break;

				//	ストリームを停止			ss
				case 0x94:
					m_sourceAddress += 1;
					if (TargetHardware.IsUsePCM)
					{
						//	ストリームID 読み込み
						if (!GetSourceData(strmSourceAddress, DataSize.DB, false, out tmp_d0)) return false;
						var streamId = (int)tmp_d0;
						if (!m_vgmStreamDict.ContainsKey(streamId))
						{	//	ディクショナリにストリーム ID がない場合は、ストリームID が存在しない警告表示をして、処理しない
							AddWarningString($"WARNING : VGM Stream Stop stream. No stream id. {streamId:X}. Address;{m_sourceAddress-4:X}.");
							break;
						}
						//	PLAY中間データへ VgmStopStream を格納
						var parseChip = m_vgmStreamDict[streamId].m_parseChip;
						AddVgmStopStreamToPlayImData(parseChip.ChipSelect,  a1:0, code:(byte)0x94);
					}
					break;

				//	ストリームの開始 （高速呼び出し）ss bb bb ff
				case 0x95:
					m_sourceAddress += 4;
					if (TargetHardware.IsUsePCM)
					{
						//	ストリームID 読み込み
						if (!GetSourceData(strmSourceAddress, DataSize.DB, false, out tmp_d0)) return false;
						var streamId = (int)tmp_d0;
						if (!m_vgmStreamDict.ContainsKey(streamId))
						{	//	ディクショナリにストリーム ID がない場合は、ストリームID が存在しない警告表示をして、処理しない
							AddWarningString($"WARNING : VGM Stream Start fast call. No stream id. {streamId:X}. Address;{m_sourceAddress-4:X}.");
							break;
						}
						if (!GetSourceData(strmSourceAddress+1, DataSize.DW, false, out tmp_d0)) return false;
						if (!GetSourceData(strmSourceAddress+3, DataSize.DB, false, out tmp_d1)) return false;
						var pcmBlockId = (int)tmp_d0;
						int samplingRate = -1;
						var parseChip = m_vgmStreamDict[streamId].m_parseChip;
						var pcmImData = ImData.GetPcmImData(parseChip.ChipSelect, 0, pcmBlockId);
						if (pcmImData != null)
						{
							if (pcmImData.m_pcmSize == 0)
							{
								AddWarningString($"WARNING : VGM Stream Start fast call. PCM block size 0. BlockID:{pcmBlockId:X}.");
								break;
							}
							if (parseChip.ChipType == ChipType.M6258)
							{
								samplingRate = m_m6258SamplingRate;
							}
						}
						else
						{
							AddWarningString($"WARNING : VGM Stream Start fast call. None PCM block id. BlockID:{pcmBlockId:X}.");
							break;
						}
						//	PLAY中間データへ VgmStreamStartSizeFastCall を格納
						AddVgmStreamStartSizeFastCallToPlayImData(parseChip.ChipSelect, a1:0, code:(byte)0x95, vstrmBlockId:pcmBlockId, vstrmFlagMode:(byte)tmp_d1, vstrmStart:pcmImData.m_pcmStart, vstrmSize:pcmImData.m_pcmSize, vstrmSamplingRate:samplingRate);
					}
					break;
			}
			return true;
		}

		/// <summary>
		///	VGM コマンド	CHIP レジスタ
		/// </summary>
		private bool CheckCommand_ChipRegister(uint vgmCommand, out bool isCommandReady)
		{
			isCommandReady = false;
			foreach(var parseChip in m_parseChipList.Where(x => x.ChipActiveStatus == ActiveStatus.ACTIVE))
			{
				if (vgmCommand == parseChip.ChipIdCode0 || vgmCommand == parseChip.ChipIdCode1)
				{
					uint dualBit = 0;
					uint port = 0;
					uint regCode = 0;
					uint regData = 0;
					int a1 = (vgmCommand == parseChip.ChipIdCode0) ? 0 : 1;
					switch(parseChip.ChipType)
					{
						default:
							if (!GetSourceData(m_sourceAddress,   DataSize.DB, false, out regCode)) return false;
							if (!GetSourceData(m_sourceAddress+1, DataSize.DB, false, out regData)) return false;
							m_sourceAddress += 2;
							AddTwoDataToPlayImData(parseChip.ChipSelect, a1:a1, data0:(byte)regCode, data1:(byte)regData);
							isCommandReady = true;
							break;
						case ChipType.SN76489:		//	One Data.
							if (!GetSourceData(m_sourceAddress, DataSize.DB, false, out regCode)) return false;
							m_sourceAddress += 1;
							AddOneDataToPlayImData(parseChip.ChipSelect, a1:0, data0:(byte)regCode);
							isCommandReady = true;
							break;
						case ChipType.M6295:		//	One Data.
							if (!GetSourceData(m_sourceAddress, DataSize.DB,   false, out regCode)) return false;
							if (!GetSourceData(m_sourceAddress+1, DataSize.DB, false, out regData)) return false;
							dualBit = (parseChip.ChipDualMode == DualMode.SECOND) ? (uint)0x80 : (uint)0x00;
							if ((regCode & 0x7F) != 0)
							{
								AddWarningString($"WARNING : VGM M6295 NMK112 or MasterClock not support. Address;{m_sourceAddress-2:X}.");
								break;
							}
							if ((regCode & 0x80) == dualBit)
							{
								m_sourceAddress += 2;
								AddOneDataToPlayImData(parseChip.ChipSelect, a1:0, data0:(byte)regData);
								isCommandReady = true;
							}
							break;
						case ChipType.AY_3_8910:	//	Decide between duals at RegCode 0x80.
						case ChipType.YM2149:
						case ChipType.M6258:
							if (!GetSourceData(m_sourceAddress, DataSize.DB,   false, out regCode)) return false;
							if (!GetSourceData(m_sourceAddress+1, DataSize.DB, false, out regData)) return false;
							dualBit = (parseChip.ChipDualMode == DualMode.SECOND) ? (uint)0x80 : (uint)0x00;
							if ((regCode & 0x80) == dualBit)
							{
								m_sourceAddress += 2;
								regCode &= 0x7F;
								AddTwoDataToPlayImData(parseChip.ChipSelect, a1:0, data0:(byte)regCode, data1:(byte)regData);
								isCommandReady = true;
							}
							break;
						case ChipType.K051649:		//	SCC.	Decide between duals at RegCode 0x80.	RegCode Convert.
						case ChipType.K052539:		//	SCC-I.	Decide between duals at RegCode 0x80.	RegCode Convert.
							port = 0;
							if (!GetSourceData(m_sourceAddress,   DataSize.DB, false, out port)) return false;
							if (!GetSourceData(m_sourceAddress+1, DataSize.DB, false, out regCode)) return false;
							if (!GetSourceData(m_sourceAddress+2, DataSize.DB, false, out regData)) return false;
							dualBit = (parseChip.ChipDualMode == DualMode.SECOND) ? (uint)0x80 : (uint)0x00;
							if ((regCode & 0x80) == dualBit)
							{
								m_sourceAddress += 3;
								regCode &= 0x7F;
								switch(port)
								{
									case 0x00: break;
									case 0x01: { if (parseChip.ChipType == ChipType.K051649) regCode += 0x80; else regCode +=0xA0; } break;
									case 0x02: { if (parseChip.ChipType == ChipType.K051649) regCode += 0x8A; else regCode +=0xAA; } break;
									case 0x03: { if (parseChip.ChipType == ChipType.K051649) regCode += 0x8F; else regCode +=0xAF; } break;
									case 0x04: { if (parseChip.ChipType == ChipType.K051649) regCode =  0xFF; 						} break;
									case 0x05: regCode += 0xE0; break;
									default: regCode = 0xFF; break;
								}
								AddTwoDataToPlayImData(parseChip.ChipSelect, a1:0, data0:(byte)regCode, data1:(byte)regData);
								isCommandReady = true;
							}
							break;
					}
				}
				if (isCommandReady) break;
			}
			return true;
		}

		/// <summary>
		///	VGM ヘッダーを解析
		/// </summary>
		private bool ParseVgmHeaderChip()
		{
			//	VGM ヘッダーからパース CHIP を生成する
			foreach (var headerChipData in VgmHeaderChipDataArray)
			{
				uint chipClock;
				ChipType chipType = headerChipData.m_chipType;
				if (!GetVgmHeaderData(headerChipData.m_clkTblIndex, DataSize.DL, false, out chipClock)) return false;
				if (chipClock != 0)
				{
					if (chipType == ChipType.YM2610)
					{	//	YM2610とYM2610Bは クロックの最上位ビットで判定する
						chipType = ((chipClock & 0x80000000) == 0) ? chipType : ChipType.YM2610B;
					}
					else if (chipType == ChipType.K051649)
					{	//	SCC(K051649)とSCCI(K052539)は クロックの最上位ビットで判定する
						chipType = ((chipClock & 0x80000000) == 0) ? chipType : ChipType.K052539;
					}
					chipClock &= 0x7FFFFFFF;
					var firstParseChip = new ParseChip(chipType, 0, headerChipData.m_idA0, headerChipData.m_idA1);
					if (!ParseChipSetVgmChipName(firstParseChip)) return false;
					//	デュアル CHIP のチェック
					if ((chipClock & 0x40000000) == 0)
					{	//	シングル CHIP
						firstParseChip.SetChipClock((int)chipClock);
						m_parseChipList.Add(firstParseChip);
					}
					else
					{	//	デュアル CHIP
						chipClock &= 0xBFFFFFFF;
						firstParseChip.SetChipClock((int)chipClock);
						var secondParseChip = new ParseChip(chipType, (int)chipClock, headerChipData.m_dualIdA0, headerChipData.m_dualIdA1);
						if (!ParseChipSetVgmChipName(secondParseChip)) return false;
						firstParseChip.SetChipDualNumber(DualNumber.Dual1st);
						secondParseChip.SetChipDualNumber(DualNumber.Dual2nd);
						firstParseChip.SetChipDualIndex(m_parseChipList.Count+1);
						m_parseChipList.Add(firstParseChip);
						m_parseChipList.Add(secondParseChip);
					}
				}
			}
			if (m_parseChipList.Count == 0)
			{	//	未対応の VGM ファイル
				SetNoSupportedMessage();
				return false;
			}
			//	パース CHIP をターゲットデータに反映
			ReflectParseChipToTargetChip();
			if (!TargetHardware.IsActiveTarget())
			{
				SetNoSupportedMessage();
				return false;
			}
			return true;
		}

		/// <summary>
		///	VGM ヘッダーからのデータ取得
		///	/// </summary>
		private bool GetVgmHeaderData(int source_address, DataSize dataSize, bool isBig, out uint resData)
		{
			resData = 0;
			int ck_address = 0;
			switch(dataSize)
			{
				case DataSize.DB:
					ck_address = m_vgmStartAddress;
					break;
				case DataSize.DW:
					ck_address = m_vgmStartAddress-2;
					break;
				case DataSize.DL:
					ck_address = m_vgmStartAddress-4;
					break;
			}
			if (source_address <= ck_address)
			{
				return GetSourceData(source_address, dataSize, isBig, out resData);
			}
			return true;
		}



		/// <summary>
		///	PSG の CHIP 名対応
		/// </summary>
		private bool ParseChipSetVgmChipName(ParseChip parseChip)
		{
			if (parseChip.ChipType == ChipType.AY_3_8910 || parseChip.ChipType == ChipType.YM2149)
			{
				uint ayt = 0x00;
				if (!GetVgmHeaderData(0x78, DataSize.DB, false, out ayt)) return false;
				switch(ayt)
				{
					case 0x00: parseChip.SetChipName("AY-3-8910"); break;
					case 0x01: parseChip.SetChipName("AY-3-8912"); break;
					case 0x02: parseChip.SetChipName("AY-3-8913"); break;
					case 0x10: parseChip.SetChipName("YM2149");    break;
					case 0x11: parseChip.SetChipName("YM3439");    break;
					case 0x12: parseChip.SetChipName("YMZ284");    break;
					case 0x13: parseChip.SetChipName("YMZ294");    break;
					case 0x03: parseChip.SetChipName("AY-3-8930"); break;
					default: break;
				}
			}
			return true;
		}

		/// <summary>
		///	VGM ストリームを使用する CHIP
		/// </summary>
		private readonly ChipType[] StreamEnableChipArray = 
		{
			ChipType.M6258,
		};

		/// <summary>
		///	Vgm Header Clock And Command Data Dictionary.
		/// </summary>
		private struct VgmHeaderChipData
		{
			public ChipType m_chipType;
			public int m_clkTblNumber;
			public int m_clkTblIndex;
			public uint m_idA0;
			public uint m_idA1;
			public uint m_dualIdA0;
			public uint m_dualIdA1;
		}
		private readonly VgmHeaderChipData[] VgmHeaderChipDataArray =
		{
			new VgmHeaderChipData {m_chipType = ChipType.YM2151,	m_clkTblNumber = 0x03, m_clkTblIndex = 0x30,  m_idA0 = 0x54, m_idA1 = 0xFF,  m_dualIdA0 = 0xA4, m_dualIdA1 = 0xFF},
			new VgmHeaderChipData {m_chipType = ChipType.YM2203,	m_clkTblNumber = 0x06, m_clkTblIndex = 0x44,  m_idA0 = 0x55, m_idA1 = 0xFF,  m_dualIdA0 = 0xA5, m_dualIdA1 = 0xFF},
			new VgmHeaderChipData {m_chipType = ChipType.YM2608,	m_clkTblNumber = 0x07, m_clkTblIndex = 0x48,  m_idA0 = 0x56, m_idA1 = 0x57,  m_dualIdA0 = 0xA6, m_dualIdA1 = 0xA7},
			new VgmHeaderChipData {m_chipType = ChipType.YM2612,	m_clkTblNumber = 0x02, m_clkTblIndex = 0x2C,  m_idA0 = 0x52, m_idA1 = 0x53,  m_dualIdA0 = 0xA2, m_dualIdA1 = 0xA3},
			new VgmHeaderChipData {m_chipType = ChipType.YM2610,	m_clkTblNumber = 0x08, m_clkTblIndex = 0x4C,  m_idA0 = 0x58, m_idA1 = 0x59,  m_dualIdA0 = 0xA8, m_dualIdA1 = 0xA9},
		//	new VgmHeaderChipData {chipType = CHIP_TYPE.YM2610B,	m_clkTblNumber = 0x08, m_clkTblIndex = 0x4C,  m_idA0 = 0x58, m_idA1 = 0x59,  m_dualIdA0 = 0xA8, m_dualIdA1 = 0xA9},
			new VgmHeaderChipData {m_chipType = ChipType.YM3526,	m_clkTblNumber = 0x0A, m_clkTblIndex = 0x54,  m_idA0 = 0x5B, m_idA1 = 0xFF,  m_dualIdA0 = 0xAB, m_dualIdA1 = 0xFF},
			new VgmHeaderChipData {m_chipType = ChipType.YM3812,	m_clkTblNumber = 0x09, m_clkTblIndex = 0x50,  m_idA0 = 0x5A, m_idA1 = 0xFF,  m_dualIdA0 = 0xAA, m_dualIdA1 = 0xFF},
			new VgmHeaderChipData {m_chipType = ChipType.Y8950,		m_clkTblNumber = 0x0B, m_clkTblIndex = 0x58,  m_idA0 = 0x5C, m_idA1 = 0xFF,  m_dualIdA0 = 0xAC, m_dualIdA1 = 0xFF},
			new VgmHeaderChipData {m_chipType = ChipType.YM2413,	m_clkTblNumber = 0x01, m_clkTblIndex = 0x10,  m_idA0 = 0x51, m_idA1 = 0xFF,  m_dualIdA0 = 0xA1, m_dualIdA1 = 0xFF},
			new VgmHeaderChipData {m_chipType = ChipType.YMF262,	m_clkTblNumber = 0x0C, m_clkTblIndex = 0x5C,  m_idA0 = 0x5E, m_idA1 = 0x5F,  m_dualIdA0 = 0xAE, m_dualIdA1 = 0xAF},
			new VgmHeaderChipData {m_chipType = ChipType.SN76489,	m_clkTblNumber = 0x00, m_clkTblIndex = 0x0C,  m_idA0 = 0x50, m_idA1 = 0xFF,  m_dualIdA0 = 0x30, m_dualIdA1 = 0xFF},
		//	new VgmHeaderChipData {chipType = CHIP_TYPE.YM2149,		m_clkTblNumber = 0x12, m_clkTblIndex = 0x74,  m_idA0 = 0xA0, m_idA1 = 0xFF,  m_dualIdA0 = 0xA0, m_dualIdA1 = 0xFF},
			new VgmHeaderChipData {m_chipType = ChipType.AY_3_8910,	m_clkTblNumber = 0x12, m_clkTblIndex = 0x74,  m_idA0 = 0xA0, m_idA1 = 0xFF,  m_dualIdA0 = 0xA0, m_dualIdA1 = 0xFF},
			new VgmHeaderChipData {m_chipType = ChipType.K051649,	m_clkTblNumber = 0x19, m_clkTblIndex = 0x9C,  m_idA0 = 0xD2, m_idA1 = 0xFF,  m_dualIdA0 = 0xD2, m_dualIdA1 = 0xFF},
		//	new VgmHeaderChipData {chipType = CHIP_TYPE.K052539,	m_clkTblNumber = 0x19, m_clkTblIndex = 0x9C,  m_idA0 = 0xD2, m_idA1 = 0xFF,  m_dualIdA0 = 0xD2, m_dualIdA1 = 0xFF},
			new VgmHeaderChipData {m_chipType = ChipType.M6258,		m_clkTblNumber = 0x17, m_clkTblIndex = 0x90,  m_idA0 = 0xB7, m_idA1 = 0xFF,  m_dualIdA0 = 0xB7, m_dualIdA1 = 0xFF},
			new VgmHeaderChipData {m_chipType = ChipType.M6295,		m_clkTblNumber = 0x18, m_clkTblIndex = 0x98,  m_idA0 = 0xB8, m_idA1 = 0xFF,  m_dualIdA0 = 0xB8, m_dualIdA1 = 0xFF},
			new VgmHeaderChipData {m_chipType = ChipType.K053260,	m_clkTblNumber = 0x1D, m_clkTblIndex = 0xAC,  m_idA0 = 0xBA, m_idA1 = 0xFF,  m_dualIdA0 = 0xBA, m_dualIdA1 = 0xFF}
		};

		/// <summary>
		///	Vgm Data Block Data Dictionary.
		/// </summary>
		private struct DataBlockTypeSetting
		{
			public ChipType[] m_toUseChipArray;
			public PcmDataType m_pcmDataType;
		}
		//	ブロックデータのデータ型に対する CHIP の情報
		private readonly ReadOnlyDictionary<int, DataBlockTypeSetting>DataBlockTypeSettingDict = 
			new ReadOnlyDictionary<int, DataBlockTypeSetting>(new Dictionary<int, DataBlockTypeSetting>()
			{
			 	//	0x00	YM2612 		関連するコマンドで使用する PCM データ
				{ 0x00, new DataBlockTypeSetting { m_toUseChipArray = new ChipType[] { ChipType.YM2612 }, m_pcmDataType = PcmDataType.PcmData0 } },
				//	0x04	OKIM6258	関連するコマンドで使用する ADPCM データ
				{ 0x04, new DataBlockTypeSetting {	m_toUseChipArray = new ChipType[] { ChipType.M6258, ChipType.M6295 }, m_pcmDataType = PcmDataType.PcmData0 } },
				//	0x81	YM2608 		DELTA-T ROM データ
				{ 0x81, new DataBlockTypeSetting { m_toUseChipArray = new ChipType[] { ChipType.YM2608 }, m_pcmDataType = PcmDataType.PcmData0 } },
				//	0x82	YM2610 		ADPCM ROM  データ
				{ 0x82, new DataBlockTypeSetting { m_toUseChipArray = new ChipType[] { ChipType.YM2610, ChipType.YM2610B},	m_pcmDataType = PcmDataType.PcmData0 } },
				//	0x83	YM2610 		DELTA-T ROM データ
				{ 0x83, new DataBlockTypeSetting {	m_toUseChipArray = new ChipType[] { ChipType.YM2610, ChipType.YM2610B}, m_pcmDataType = PcmDataType.PcmData1 } },
				//	0x8B	OKIM6295	ROM データ
				{ 0x8B, new DataBlockTypeSetting { m_toUseChipArray = new ChipType[] { ChipType.M6295	}, m_pcmDataType = PcmDataType.PcmData0 } },
				//	0x8E	K053260		ROM データ
				{ 0x8E, new DataBlockTypeSetting {	m_toUseChipArray = new ChipType[] { ChipType.K053260 }, m_pcmDataType = PcmDataType.PcmData0 } }
			}
		);
	}
}
