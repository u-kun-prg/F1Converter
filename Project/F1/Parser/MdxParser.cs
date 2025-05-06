using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace F1
{
	/// <summary>
	///	MDX フォーマット パーサー クラス
	/// </summary>
	public class MdxParser : Parser
	{
		private const int MAX_CH_NUM = (8+8);		//	FM + PCM8
		private const int FM_ONLY_CH_NUM = 8;		//	Only FM Channel
		private const int FM_PCM1_CH_NUM = 8+1;		//	FM And PCM Channel

		private readonly uint MDX_ONE_WAIT = 256000;
		private readonly int MDX_YM2151_CLOCK = 4000000;
		private readonly int MDX_M6258_CLOCK = 4000000;
		private readonly int DEFAULT_TEMPO = 100;

		private readonly int PDX_BLOCK_NUM = 96;
		private readonly int PDX_BLOCK_SIZE = 0x300;	//	8 Bytes x 96 = 300H

		private int m_channelNum;

		private bool m_mdxIsUsePCM;

		private string m_pdxFileName ="";
		private byte[] m_pdxSourceArray;
		private bool m_isUsePDX;

		private int m_ym2151CS = -1;
		private int m_m6258CS = -1;

		private int m_m6258SamplingRate;

		private int m_tempo;
		private int m_clock;
		private bool m_hasLoop;
		private bool m_isLoopReParse;
		private int m_playImDataStartIndex;
		private int[] m_loopCycleArray = new int[MAX_CH_NUM];
		private int[] m_loopImIndexArray = new int[MAX_CH_NUM];
		private int[] m_loopCounters = new int[MAX_CH_NUM];
		private int m_neiroAddress;
		private short m_mdxRandomSeed;
		private int m_currentChannel;
		private bool[] m_isSendSyncArray = new bool[MAX_CH_NUM];
		private bool[] m_isSyncArray = new bool[MAX_CH_NUM];
		private List<MMLParser> m_MMLParserList = new List<MMLParser>();

		private class PdxData
		{
			public bool m_isActive;
			public int m_start;
			public int m_size;
			public int m_sourceIndex;
			public byte[] m_pcmBinaryArray;
			public PdxData(int start, int size, int index)
			{
				this.m_isActive = false;
				this.m_start = start;
				this.m_size = size;
				this.m_sourceIndex = index;
				this.m_pcmBinaryArray = null;
			}
		}
		private List<PdxData> m_pdxDataList = new List<PdxData>(); 

		/// <summary>
		///	MDX フォーマット パース
		/// </summary>
		public override bool Parse()
		{
			int source_address = 0;
			int mdx_base_address = 0;
			uint tmp_d0 = 0;

			m_mdxRandomSeed = 0x1234;
			m_isUsePDX = false;

			m_m6258SamplingRate = 0;

			//	パース CHIP 情報
			m_parseChipList.Add(new ParseChip(ChipType.YM2151, MDX_YM2151_CLOCK, 0, 0));
			m_parseChipList.Add(new ParseChip(ChipType.M6258, MDX_M6258_CLOCK, 0, 0));
			//	パース CHIP をターゲット CHIP に反映
			ReflectParseChipToTargetChip();

			//	CHIP の搭載確認
			if (!TargetHardware.IsActiveTarget())
			{
				SetNoSupportedMessage();
				return false;
			}

			m_ym2151CS =m_parseChipList[0].ChipSelect;
			m_m6258CS =m_parseChipList[1].ChipSelect;
			m_mdxIsUsePCM = (m_m6258CS >=0) ? TargetHardware.GetTargetIsPcmActive(m_m6258CS) : false;
			var keepMmdxIsUsePCM = m_mdxIsUsePCM;

			//	データタイトルを読み飛ばし、ＰＤＸファイル名を確保する
			{
				int endStep = 0;
				m_pdxFileName ="";
				while(true)
				{
					if (!GetSourceData(source_address, DataSize.DB, true, out tmp_d0)) return false;
					source_address += 1;
					switch(endStep)
					{
						case 0:
							endStep = (tmp_d0 == 0x0D) ? 1 : 0;
							break;
						case 1:
							endStep = (tmp_d0 == 0x0A) ? 2 : 0;
							break;
						case 2:
							endStep = (tmp_d0 == 0x1A) ? 3 : 0;
							break;
						case 3:
							if (tmp_d0 == 0x00)
							{
								endStep = 4;
							}
							else 
							{
								m_pdxFileName += ((char)tmp_d0).ToString();
							}
							break;
					}
					if (endStep == 4) 
					{
						if (!String.IsNullOrEmpty(m_pdxFileName))
						{
							if (m_pdxFileName.IndexOf(".") < 0) m_pdxFileName += ".pdx";
						}
						else
						{
							DeactivateMdxPcm();
						}
						mdx_base_address = source_address;
						break;
					}
				}
			}
			//	ＰＤＸファイルの読み込み
			if (m_mdxIsUsePCM)
			{
				try
				{
					var pdxFilePath = Path.Combine(Path.GetDirectoryName(SourceFileName), m_pdxFileName);
					FileStream rdStream = new FileStream(pdxFilePath, FileMode.Open, FileAccess.Read);
					rdStream.Seek(0, SeekOrigin.Begin);
					var size = (int)rdStream.Length;
					m_pdxSourceArray = new byte[size];
					rdStream.Read(m_pdxSourceArray, 0, (int)size);
	    		    rdStream.Close();
				}
				catch(Exception)
				{
					DeactivateMdxPcm();
					AddWarningString($"WARNING : MDX Pdx File can not Read. File Name:{m_pdxFileName}.");
				}
			}

			//	サンプル時間
			Header.SetOneCycleNs(MDX_ONE_WAIT);
			//	テンポセット	100は、256-100で156Wait。最小ウェイト値は256us x156 = 39936us. 
			SetTempo(DEFAULT_TEMPO);
			//	音色データのオフセットを確保する
			if (!GetSourceData(source_address, DataSize.DW, true, out tmp_d0)) return false;
			source_address += 2;
			m_neiroAddress = mdx_base_address + (int)tmp_d0;

			m_hasLoop = false;
			m_isLoopReParse = false;
			m_MMLParserList.Clear();

			//	PCM8 の確認		PCM4/8 の MML オフセットWORD に続けて PCM4/8 使用宣言 [$E8] 
			if (keepMmdxIsUsePCM || m_mdxIsUsePCM)
			{
				if (!GetSourceData(source_address, DataSize.DW, true, out tmp_d0)) return false;
				if (!GetSourceData((mdx_base_address + (int)tmp_d0), DataSize.DB, true, out tmp_d0)) return false;
				if (tmp_d0 == 0xE8)
				{	//	PCM4/8 は対応しない
					DeactivateMdxPcm();
					AddWarningString("WARNING : MDX PCM8/PCM4 Not Supported. pcmOFF.");
				}
			}

			//	PDX を展開
			if (m_mdxIsUsePCM)
			{
				var headerIndex = 0;
				for (int i = 0; i < PDX_BLOCK_NUM; i ++)
				{
					//	スタートとサイズを取り込む
					uint start = 0;
					uint size = 0;
					if (!GetPdxBinary(headerIndex,     DataSize.DL, out start)) 
					{
						DeactivateMdxPcm();
						break;
					}
					if (!GetPdxBinary(headerIndex + 6, DataSize.DW, out size))
					{
						DeactivateMdxPcm();
						break;
					}
					headerIndex += 8;
					int pcmStart = (int)start;
					int pcmSize = (int)size;
					//	内容を取り込む
					var index = m_pdxDataList.Count;
					var pdxData = new PdxData((pcmStart - PDX_BLOCK_SIZE), pcmSize, index);
					if (pcmSize != 0)
					{
						var pcmDataIndex = pcmStart;
						pdxData.m_pcmBinaryArray = new byte[pcmSize];
						for (int j = 0; j < pcmSize; j ++)
						{
							uint pcmData = 0;
							if (!GetPdxBinary(pcmDataIndex, DataSize.DB, out pcmData))
							{
								DeactivateMdxPcm();
								break;
							}
							pdxData.m_pcmBinaryArray[j] = (byte)pcmData;
							pcmDataIndex += 1;
						}
						if (!m_mdxIsUsePCM) break;
					}
					m_pdxDataList.Add(pdxData);
				}
			}
			//	チャンネル数
			m_channelNum = (m_mdxIsUsePCM) ? FM_PCM1_CH_NUM : FM_ONLY_CH_NUM;

			//	チャンネルパーサーの生成
			for (int channel = 0; channel < m_channelNum; channel ++)
			{	//	８チャンネルのＭＭＬデータのオフセットを確保する
				if (!GetSourceData(source_address, DataSize.DW, true, out tmp_d0)) return false;
				//	チャンネルごとのＭＭＬデータを初期化する
				m_isSendSyncArray[channel] = false;
				m_isSyncArray[channel] = false;
				m_MMLParserList.Add(new MMLParser(this, channel, (mdx_base_address + (int)tmp_d0), ((channel >= FM_ONLY_CH_NUM) ? true : false)));
				source_address += 2;
				m_loopCycleArray[channel] = -1;
				m_loopImIndexArray[channel] = -1;
				m_loopCounters[channel] = -1;
			}
			//	YM2151 ノイズモードオフ
			AddTwoDataToPlayImData(m_ym2151CS, a1:0, data0:(byte)0x0F, data1:(byte)0x00);

			m_playImDataStartIndex = GetPlayImDataListCount();


			bool isExit = false;
			while(!isExit)
			{
				AddCycleWaitToPlayImData(m_clock);
				if (!MdxParse(out isExit))
				{
					return false;
				}
			}
			//	PDX を PcmImData に展開
			if (m_mdxIsUsePCM && m_isUsePDX)
			{
				int blockId = 0;
				var BlockIdDict = new Dictionary<int, int>();
				for (int i = 0; i < PDX_BLOCK_NUM; i ++)
				{
					var pdxData = m_pdxDataList[i];
					if (pdxData.m_isActive)
					{
						ImData.AddPcmImDataList(m_m6258CS, 0, pdxData.m_start, pdxData.m_size, pdxData.m_pcmBinaryArray);
						BlockIdDict.Add(pdxData.m_sourceIndex, blockId);
						blockId += 1;
					}
				}
				if (BlockIdDict.Count != 0)
				{
					foreach(var playImData in ImData.PlayImDataList.Where(x => x.m_chipSelect == m_m6258CS && x.m_imType == F1ImData.PlayImType.VSTRM_START_SIZE_FAST))
					{
						playImData.m_vStrmBlockId =BlockIdDict[playImData.m_vStrmBlockId];
					}
				}
				else
				{
					foreach(var playImData in ImData.PlayImDataList.Where(x => x.m_chipSelect == m_m6258CS))
					{
						playImData.m_imType = F1ImData.PlayImType.NONE;
					}
					m_isUsePDX = false;
					DeactivateMdxPcm(); 
				}
			}
			CreateResultMessage();
			return true;
		}

		///	<summary>
		///	PCM を非アクティブにする
		/// </summary>
		private void DeactivateMdxPcm()
		{
			m_mdxIsUsePCM = false;
			PcmDeactiveParseChipAndTargetChip();
		}

		///	<summary>
		///	チャンネルごとのパース
		/// </summary>
		private bool MdxParse(out bool isExit)
		{
			isExit = false;
			int haltChannelCount = 0;
			for (int channel = 0; channel < m_channelNum; channel ++ )
			{
				//	Halt したチャンネルをカウントして変換処理をしない
				if (m_MMLParserList[channel].m_isChannelHalt)
				{
					haltChannelCount += 1;
					continue;
				}
				//	チャンネルを変換
				m_currentChannel = channel;
				if (!m_MMLParserList[channel].ConvertMML())
				{	//	コンバートでエラーがあった
					return false;
				}
			}
			//	すべてのチャンネルの、変換が終わった
			if (haltChannelCount == m_channelNum)
			{	//	チャンネルのいずれかにループが存在する
				if (m_hasLoop)
				{	//	パースやり直しフラグがオフ
					if (!m_isLoopReParse)
					{	//	チャンネルのうち、もっとも長いループ区間サイクル数
						var maxCycle = m_loopCycleArray.Max();

						for (int channel = 0; channel < m_channelNum; channel ++ )
						{
							var loopCycle = m_loopCycleArray[channel];
							if (loopCycle >=0 && loopCycle < maxCycle && (maxCycle - loopCycle) > 100)
							{
								var num = maxCycle / loopCycle;
								if (num > 0)	// && ( maxCycle % loopCycle) < 100)
								{
									m_isLoopReParse = true;
									var mod = maxCycle % loopCycle;
									if ((float)mod  > ((float)loopCycle)*0.9f) num +=1;
									m_loopCounters[channel] = num;
								}
							}
						}
						if (m_isLoopReParse)
						{
							AddWarningString("WARNING : MDX Loop It may not work.");
							RemovePlayImDataListAfterIndex(m_playImDataStartIndex);
							for (int channel = 0; channel < m_channelNum; channel ++ )
							{
								m_MMLParserList[channel].ResetMMLParser();
							}
							return true;
						}
					}
					var minIndex = m_loopImIndexArray.Where(n => n > 0).Min();
					ImData.SetInsertPlayImData(minIndex, F1ImData.PlayImType.LOOP_POINT);
					ImData.InsertPlayImDataList();
				}
				isExit = true;
				AddEndCodeToPlayImData();
			}
			return true;
		}

		///	<summary>
		///	１ウェイトのカウント時間を設定する
		/// </summary>
		private void SetTempo(int tempo)
		{
			m_tempo = tempo;
			m_clock = 256 - tempo;
		}

		///	<summary>
		///	音色データアドレスの取得
		/// </summary>
		private int GetNeiroAddress(uint neiro_no)
		{
			uint tmp_d0;
			for (int i=0; i < 256; i++)
			{
				int address = m_neiroAddress + (27 * i);
				if (!GetSourceData(address, DataSize.DB, true, out tmp_d0))
				{
					AddWarningString($"WARNING : MDX Neiro data not found. No:{neiro_no:X}.");
					return -1;
				}
				if (tmp_d0 == neiro_no)
				{
					address += 1;
					return address;
				}
			}
			AddWarningString($"WARNING : MDX Neiro data not found. No:{neiro_no:X}.");
			return -1;
		}

		///	<summary>
		///	MDX 専用の簡易乱数
		/// </summary>
		private short EasyRandom()
		{
			uint rnd = (uint)(m_mdxRandomSeed & 0xFFFF);
			rnd *= 0xC549;
			rnd += 0x0C;
			m_mdxRandomSeed = (short)(rnd & 0xFFFF);
			return (short)((rnd >> 3) & 0xFFFF);
		}

		///	<summary>
		///	M6258 Sampling Rate.
		/// </summary>
		private void SetM6258SamplingRate(byte code)
		{
			var samplingRate = 15625;
			switch(code)
			{
				case 0:	samplingRate =  3906; break;
				case 1:	samplingRate =  5208; break;
				case 2:	samplingRate =  7813; break;
				case 3: samplingRate = 10417; break;
				case 4:	samplingRate = 15625; break;
			}
			m_m6258SamplingRate = samplingRate;
			AddVgmStreamSamplingRataToPlayImData(m_m6258CS, a1: 0, code:(byte)0x92, vstrmSamplingRate:m_m6258SamplingRate);
		}

		///	<summary>
		///	M6258 Pan.
		/// </summary>
		private void SetM6258Pan(byte code)
		{
			AddTwoDataToPlayImData(m_m6258CS, a1:0, data0:(byte)0x02, data1:code);
		}

		///	<summary>
		///	MML レジスタとデータを格納
		/// </summary>
		private void WriteYM2151RegData(byte reg, byte data)
		{
			AddTwoDataToPlayImData(m_ym2151CS, a1:0, data0:reg, data1:data);
		}

		/// <summary>
		///	PDX データを取得
		/// </summary>
		private PdxData GetPdxData(int index)
		{
			var sourceIndex = m_pdxDataList.FindIndex(x => x.m_sourceIndex == index);
			if (sourceIndex >= 0)
			{
				return m_pdxDataList[sourceIndex];
			}
			return null;
		}

		/// <summary>
		///	PDX バイナリからのデータ取得
		/// </summary>
		private bool GetPdxBinary(int pdxIndex, DataSize dataSize, out uint resData)
		{
			bool result = false;
			uint d0,d1,d2,d3;

			resData = 0;
			switch(dataSize)
			{
				case DataSize.DB:
					if (CheckPdxAddressRange(pdxIndex))
					{
						resData = (uint)m_pdxSourceArray[pdxIndex];
						result = true;
					}
					break;
				case DataSize.DW:
					if (CheckPdxAddressRange(pdxIndex))
					{
						d0 = (uint)m_pdxSourceArray[pdxIndex];
						if (CheckPdxAddressRange(pdxIndex+1)) 
						{
							d1 = (uint)m_pdxSourceArray[pdxIndex+1];
							resData = ((d0 << 8) & 0xFF00) | (d1 & 0xFF);
							result = true;
						}
					}
					break;
				case DataSize.DL:
					if (CheckPdxAddressRange(pdxIndex))
					{
						d0 = (uint)m_pdxSourceArray[pdxIndex];
						if (CheckPdxAddressRange(pdxIndex+1)) 
						{
							d1 = (uint)m_pdxSourceArray[pdxIndex+1];
							if (CheckPdxAddressRange(pdxIndex+2)) 
							{
								d2 = (uint)m_pdxSourceArray[pdxIndex+2];
								if (CheckPdxAddressRange(pdxIndex+3)) 
								{
									d3 = (uint)m_pdxSourceArray[pdxIndex+3];
									resData = ((d0 << 24) & 0xFF000000) | ((d1 << 16) & 0xFF0000) | ((d2 << 8) & 0xFF00) | (d3 & 0xFF);
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
		///	PDX データのアドレス範囲チェック
		/// </summary>
		private bool CheckPdxAddressRange(int pdxIndex)
		{
			if (pdxIndex >= 0 && pdxIndex < m_pdxSourceArray.Length)
			{
				return true;
			}
			AddWarningString($"WARNING : MDX illegal PDX size. Index:{pdxIndex:X}.");
			return false;
		}

		///	<summary>
		///	MML コンバートクラス
		/// </summary>
		private class MMLParser
		{
			private const int REPEAT_COUNT = 4;
			private const int NEIRO_BYTE_SIZE = 26;

			private struct RepeatFrame
			{
				public int m_repeatAddress;
				public int m_repeatCount;
			}
			///	<summary>
			///	MML コマンドのアドレスと PLAY 中間のインデックスを対応するデータ
			/// </summary>
			private class AddressAndIndex
			{
				public int m_mmlCmdAddress;
				public int m_playImIndex;
				public AddressAndIndex(int mmlCmdAddress, int playImIndex)
				{
					this.m_mmlCmdAddress = mmlCmdAddress;
					this.m_playImIndex = playImIndex;
				}
			}

			public int m_channelNo;
			public bool m_isChannelHalt;

			public bool m_isPcmChannel;

			private MdxParser m_mdxParser;

			private int m_sourceAddress;
			private int m_keepAddress;
			private List<AddressAndIndex> m_addressAndIndexList = new List<AddressAndIndex>();

			//	MML Bit Check.
			private bool m_isSetNote = false;		//	16H[A6]	0	0x0001	Set Note.
			private bool m_isWaveLFO = false;		//	16H[A6]	1	0x0002	WAVE LFO
			private bool m_isNoKeyOff = false;		//	16H[A6]	2	0x0004	Key OFF disabled.	Not Key off After Next Note Play.
			private bool m_isKeyOn = false;			//	16H[A6]	3	0x0008	Key ON
			private bool m_isEX3NoKeyOff = false;	//	16H[A6]	4	0x0010	Expansion MML3 Not Key Off.
			private bool m_isPitchLFO = false;		//	16H[A6]	5	0x0020	PLFO ON
			private bool m_isVolumeLFO = false;		//	16H[A6]	6	0x0040	VLFO ON
			private bool m_isPortamento = false;	//	16H[A6]	7	0x0080	Portamento ON
			//private bool m_isSetVolume = false;	//	17H[A6]	0	0x0001	Set Volume.		//	FF Masked Because Processing Has Not Been Confirmed.
			private bool m_isSetNeiro = false;		//	17H[A6]	1	0x0002	Set Neiro.
			private bool m_isSetPan = false;		//	17H[A6]	2	0x0004	Set Pan.
			private bool m_isSetSync = false;		//	17H[A6]	3	0x0008	Set Sync.
			//private bool Is						//	17H[A6]	4	0x0010	Unused.
			//private bool Is						//	17H[A6]	5	0x0020	Unused.
			//private bool Is						//	17H[A6]	6	0x0040	Unused.
			//private bool IsEX6Reserve = false		//	17H[A6]	7	0x0080	Expansion MML６ Not Use.	// Reserve Mask

			private byte m_keyOffClock;
			private byte m_keyOnClock;
			private byte m_keyOnDelayTimer;
			private byte m_keyLength;
			private byte m_keyOnDelay;

			private byte m_pitchLFO_Type;
			private int m_pitchLFO_OffsetStart;	//	32->16
			private int m_pitchLFO_DeltaStart;	//	32->16
			private int m_pitchLFO_Offset;		//	32->16
			private int m_pitchLFO_Delta;			//	32->16
			private ushort m_pitchLFO_LengthFixd;
			private ushort m_pitchLFO_Length;
			private ushort m_pitchLFO_LengthCounter;

			private byte m_volumeLFO_Type;
			private short m_volumeLFO_DeltaStart;
			private short m_volumeLFO_DeltaFixd;
			private short m_volumeLFO_Offset;
			private short m_volumeLFO_Delta;
			private ushort m_volumeLFO_Length;
			private ushort m_volumeLFO_LengthCounter;

			private short m_fmNote;
			private short m_writeFMNote;
			private short m_pcmNote;

			private int m_portamento;
			private int m_portamentoDelta;
			private short m_detune;
			private byte m_volume;
			private byte m_volumeCode;
			private byte m_delayLFO;
			private byte m_delayLFOTimer;

			private byte m_regPAN;
			private byte m_regSamplingRate;
			private byte m_regPMSAMS;
			private byte m_regSlotMask;
			private byte m_regCarrierSlot;

			private RepeatFrame[] m_repeatFrameArray = new RepeatFrame[REPEAT_COUNT];
			private byte[] m_neiroDataArray = new byte[NEIRO_BYTE_SIZE];


			public MMLParser(MdxParser mdxParser, int channel, int address, bool isPCM)
			{
				this.m_mdxParser = mdxParser;
				m_isPcmChannel = isPCM;
				m_channelNo = channel;
				ResetMMLParser(address);
			}

			public void ResetMMLParser(int address = -1)
			{
				if (address >= 0)
				{
					m_sourceAddress = address;
					m_keepAddress = address;
				}
				else
				{
					m_sourceAddress = m_keepAddress;
				}
				m_isChannelHalt = false;
				m_regPAN = 0xC0;
				m_regSlotMask = (byte)0x00;
				m_regCarrierSlot = (byte)0x00;
				m_keyOnClock = 1;
				m_keyLength = 8;
				m_writeFMNote = -1;
				m_volume = 0x08;
				m_volumeCode = 0xFF;
				m_volumeLFO_Offset = 0;
				m_pitchLFO_Offset = 0;
				m_portamento = 0;
				for (int i = 0; i < REPEAT_COUNT; i ++)
				{
					m_repeatFrameArray[i].m_repeatAddress = 0;
				}
			}

			///	<summary>
			///	MML データの変換
			/// </summary>
			public bool ConvertMML()
			{
				CalculateMML();
				if (!ParseMML())
				{
					return false;
				}
				if (m_isSetNote)
				{
					if (m_keyOnDelayTimer == 0)
					{
						if (!m_isPcmChannel)
						{
							MMLSetNeiro();
							MMLSetPan();
							if (!m_isKeyOn)
							{
								m_delayLFOTimer = m_delayLFO;
								if (m_delayLFOTimer !=0)
								{
									m_pitchLFO_Offset = 0;
									m_volumeLFO_Offset = 0;
									m_delayLFOTimer -= 1;
									if (m_delayLFOTimer == 0)
									{
										if (m_isPitchLFO)
										{
											MMLPLfoReInit();
										}
										if (m_isVolumeLFO)
										{
											MMLVLfoReInit();
										}
									}
								}
								if (m_isWaveLFO)
								{
									m_mdxParser.WriteYM2151RegData((byte)0x01 , (byte)0x02);
									m_mdxParser.WriteYM2151RegData((byte)0x01 , (byte)0x00);
								}
							}
							m_portamento = 0;
							MMLSetTone();
							MMLUpdateVolume();
						}
						MMLKeyOn();
						m_isSetNote = false;
						return true;
					}
					m_keyOnDelayTimer -= 1;
				}
				if (!m_isPcmChannel)
				{
					MMLSetTone();
					MMLUpdateVolume();
				}
				return true;
			}

			///	<summary>
			///	MML	カリキュレーター	ボリュームやＬＦＯなどの処理
			/// </summary>
			private void CalculateMML()
			{
				if (!m_isPcmChannel)
				{
					if (m_isPortamento)
					{
						if (m_keyOnDelayTimer == 0)
						{
							m_portamento += m_portamentoDelta;
						}
					}
					if (m_delayLFO != 0)
					{
						if (m_keyOnDelayTimer != 0)
						{
							return;
						}
						if (m_delayLFOTimer != 0)
						{
							m_delayLFOTimer -= 1;
							if(m_delayLFOTimer != 0)
							{
								return;
							}
							if (m_isPitchLFO)
							{
								MMLPLfoReInit();
							}
							if (m_isVolumeLFO)
							{
								MMLVLfoReInit();
							}
						}
					}
					if (m_isPitchLFO)
					{
						MMLPlfoCalculate();
					}
					if (m_isVolumeLFO)
					{
						MMLVlfoCalculate();
					}
				}
			}

			///	<summary>
			///	MML	MML のパース
			///	Parse MML
			/// </summary>
			private bool ParseMML()
			{
				if (m_isChannelHalt)
				{
					return true;
				}

				if (m_isSetSync)
				{
					int currentChannel = m_mdxParser.m_currentChannel;
					if (!m_mdxParser.m_isSyncArray[currentChannel])
					{
						return true;
					}
					m_mdxParser.m_isSyncArray[currentChannel] = false;
					m_mdxParser.m_isSendSyncArray[currentChannel] = false;
					m_isSetSync = false;
				}
				else
				{
					if (!m_isNoKeyOff)
					{
						m_keyOffClock -= 1;
						if (m_keyOffClock == 0)
						{
							MMLKeyOff();
						}
					}
					m_keyOnClock -= 1;
					if (m_keyOnClock != 0)
					{
						return true;
					}
				}
				m_isPortamento = false;
				m_isNoKeyOff = false;

				bool isExit = false;
				while(!isExit)
				{
					if (!ParseCommandMML(out isExit))
					{
						return false;
					}
				}
				return true;
			}

			///	<summary>
			///	MML	MML のコマンドパース
			/// </summary>
			private bool ParseCommandMML(out bool isExit)
			{
				isExit = false;
				byte command = 0;
				uint tmp_d0 = 0;
				uint tmp_d1 = 0;

				//	MML ループのために、MML コマンドの読み出しアドレスと PLAY 中間データのインデックスと合わせてリストに収納する。
				var addressAndIndex = new AddressAndIndex(m_sourceAddress, m_mdxParser.GetPlayImDataListCount());
				m_addressAndIndexList.Add(addressAndIndex);
				//	MML コマンド読み出し、	
				if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
				m_sourceAddress += 1;
				command = (byte)tmp_d0;

				if (command < 0x80)
				{
					//	休符コマンド	[00H ～ 7FH]	長さはデータ値+１クロック
					m_keyOnClock = (byte)(command + 1);
					m_keyOffClock = (byte)(command + 1);
					isExit = true;
					return true;
				}
				if (command < 0xE0)
				{
					//	音符コマンド	[80H ～ DFH] + [クロック - 1]	音程は 80H が o0d+、DFH が o8d、PCMパートではデータ番号
					m_fmNote = (short)(command - 0x80);
					m_pcmNote = m_fmNote;
					m_fmNote *= 64;		//	<< 6
					m_fmNote += 5;
					m_fmNote += m_detune;
					m_isSetNote = true;

					m_keyOnDelayTimer = m_keyOnDelay;

					if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
					m_sourceAddress += 1;

					uint on_clk = tmp_d0;
					uint off_clk = m_keyLength;
					if (off_clk < 0x80)
					{
						off_clk *= on_clk;
						off_clk /= 8;
					}
					else
					{
						off_clk += on_clk;
						if (off_clk < 0x100)
						{
							off_clk = 0;
						}
					}
					m_keyOnClock = (byte)(on_clk + 1);
					m_keyOffClock = (byte)(off_clk + 1);
					isExit = true;
					return true;
				}
				//	Control Command.
				switch(command)
				{
					case 0xE6:
						//	拡張ＭＭＬコマンド２	[E6H]			パラメータだけ読み飛ばす
						if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
						m_sourceAddress += 1;
						switch(tmp_d0)
						{
							case 0:		//	ERROR = 強制終了					[$00]b
								m_isChannelHalt = true;
								isExit = true;
								return true;
							case 1:		//	相対ディチューン	(-32768～32768)	[$01]b + [DETUNE]w
								if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DW, true, out tmp_d0)) return false;
								m_sourceAddress += 2;
								return true;
							case 2:		//	移調				(-127～127)		[$02]b + [KEY TRANS]b
								if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
								m_sourceAddress += 1;
								return true;
							case 3:		//	相対移調			(-127～127)		[$03]b + [KEY TRANS]b
								if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
								m_sourceAddress += 1;
								return true;
						}
						m_sourceAddress -= 1;
						m_mdxParser.ErrorString= $"Mdx Unknown Command  0xE6 {tmp_d0:X}. Address;{m_sourceAddress:X}.";
						m_isChannelHalt = true;
						return false;
					case 0xE7:
						//	拡張 MML コマンド		[E7H]
						if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
						m_sourceAddress += 1;
						switch(tmp_d0)
						{
							case 0:		//	ERROR=強制終了						[$00]b
								m_isChannelHalt = true;
								isExit = true;
								return true;
							case 1:		//	FADEOUT								[$01]b + [SPEED]b			とりあえず無効としておく
								m_isChannelHalt = true;
								isExit = true;
								return false;
							case 2:		//	PCM8を直接ドライブする				[$02]b + [d0.w] + [d1.l]	パラメータだけ読み飛ばす
								if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DW, true, out tmp_d0)) return false;
								m_sourceAddress += 2;
								if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DL, true, out tmp_d0)) return false;
								m_sourceAddress += 4;
								return true;
							case 3:		//	 $00=KEYOFFする/$01=KEYOFFしない	[$03]b + [FLAG]b   
								if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
								m_sourceAddress += 1;
								m_isEX3NoKeyOff = (tmp_d0 != 0) ? true : false;
								return true;
							case 4:		//	他のチャンネルをコントロール		[$04]b + [CH]b + [MML]?	とりあえず無効としておく
								m_isChannelHalt = true;
								return false;
							case 5:		//	音長加算する						[$05]b + [DATA]b 
								if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
								m_isSetNote = true;
								tmp_d0 += 1;
								m_keyOnClock = (byte)(tmp_d0);
								m_keyOffClock = (byte)(tmp_d0);
								MMLKeyOn();
								return true;
							case 6:		//	リザーブ							[$06]b + [FLAG]b	パラメータだけ読み飛ばす	
								if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
								m_sourceAddress += 1;
								return true;
						}
						m_sourceAddress -= 1;
						m_mdxParser.ErrorString= $"Mdx Unknown Command  0xE6 {tmp_d0:X}. Address;{m_sourceAddress:X}.";
						m_isChannelHalt = true;
						return false;
					case 0xE8:
						//	PCM8拡張モード移行		[$E8]			Achの頭で有効
						return true;
					case 0xE9:
						//	LFOディレイ設定			[$E9] + [???]	MDコマンド対応
						if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
						m_sourceAddress += 1;
						m_delayLFO = (byte)(tmp_d0);
						return true;
					case 0xEA:
						//	OPMLFO制御
						//	[$EA] + [$80]	MHOF
						//	[$EA] + [$81]	MHON
						//	[$EA] + [SYNC/WAVE] + [LFRQ] + [PMD] + [AMD] + [PMS/AMS]
						//		[SYNC*$40+WAVE]b + [LFRQ]b + [PMD+128]b + [AMD]b + [PMS/AMS}b
						{
							if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
							m_sourceAddress += 1;
							byte lfo_com =(byte)tmp_d0;
							if (lfo_com > 0x7F)
							{
								if ((lfo_com & 0x01) == 0)
								{	//	MHOF
									int reg = 0x38 + m_channelNo;
									m_mdxParser.WriteYM2151RegData((byte)reg , (byte)0x00);
								}
								else 
								{	//	MHON
									int reg = 0x38 + m_channelNo;
									m_mdxParser.WriteYM2151RegData((byte)reg , m_regPMSAMS);
								}
								return true;
							}

							m_isWaveLFO = ((lfo_com & 0x40) != 0) ? true : false;
							lfo_com &= 0xBF;
							m_mdxParser.WriteYM2151RegData((byte)0x1B , lfo_com);
							//	LFRQ
							if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
							m_sourceAddress += 1;
							m_mdxParser.WriteYM2151RegData((byte)0x18 , (byte)tmp_d0);
							//	PMD
							if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
							m_sourceAddress += 1;
							m_mdxParser.WriteYM2151RegData((byte)0x19 , (byte)tmp_d0);
							//	AMD
							if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
							m_sourceAddress += 1;
							m_mdxParser.WriteYM2151RegData((byte)0x19 , (byte)tmp_d0);
							//	PMS/AMS
							if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
							m_sourceAddress += 1;
							{
								m_regPMSAMS = (byte)tmp_d0;
								int reg = 0x38 + m_channelNo;
								m_mdxParser.WriteYM2151RegData((byte)reg , m_regPMSAMS);
							}
						}
						return true;
					case 0xEB:
						//	音量LFO制御
						//		[$EB] + [$80]	MAOF
						//		[$EB] + [$81]	MAON
						//		[$EB] + [WAVE※1] + [周期※2].w + [変移※4].w
						{
							m_isVolumeLFO = true;
							//	LFO_com
							if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
							m_sourceAddress += 1;
							byte lfo_com = (byte)tmp_d0;
							if (lfo_com > 0x7F)
							{
								if ((lfo_com & 0x01) == 0)
								{	//	MAOF
									m_isVolumeLFO = false;
									m_volumeLFO_Offset = 0;
								}
								else 
								{	//	MAON
									MMLVLfoReInit();
								}
								return true;
							}
							m_volumeLFO_Type =  (byte)((lfo_com & 0x03) + 1);
							lfo_com *= 2;
							//	周期
							if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DW, true, out tmp_d0)) return false;
							m_sourceAddress += 2;
							m_volumeLFO_Length = (ushort)tmp_d0;
							//	変移
							if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DW, true, out tmp_d0)) return false;
							m_sourceAddress += 2;
							m_volumeLFO_DeltaStart = (short)tmp_d0;
							short delta_fixd = (short)tmp_d0;
							if ((lfo_com & 0x02) != 0)
							{
								delta_fixd *= (short)m_volumeLFO_Length;
							}
							delta_fixd *= -1;
							if (delta_fixd < 0)
							{
								delta_fixd = 0;
							}
							
							m_volumeLFO_DeltaFixd = delta_fixd;
							MMLVLfoReInit();
						}
						return true;
					case 0xEC:
						//	音程ＬＦＯ制御
						//		[$EC] + [$80]b	MPOF
						//		[$EC] + [$81]b	MPON
						//		[$EC] + [WAVE※1]b + [周期※2].w + [変移※3].w
						{
							m_isPitchLFO = true;
							if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
							m_sourceAddress += 1;
							byte lfo_com_f =(byte)tmp_d0;
							if (lfo_com_f > 0x7F)
							{
								if ((lfo_com_f & 0x1) == 0)
								{	//	MPOF
									m_isPitchLFO = false;
									m_pitchLFO_Offset = 0;
								}
								else
								{	//	MPON
									MMLPLfoReInit();
								}
								return true;
							}
							byte lfo_com = lfo_com_f;
							lfo_com &= 0x3;
							m_pitchLFO_Type = (byte)(lfo_com + 1);
							lfo_com += lfo_com;
							//	周期.w
							if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DW, true, out tmp_d0)) return false;
							m_sourceAddress += 2;
							m_pitchLFO_Length = (ushort)tmp_d0;
							ushort length = (ushort)tmp_d0;
							if (lfo_com != 2)
							{
								length = (ushort)(length >> 1);
								if (lfo_com == 6)
								{
									length = 1;
								}
							}
							m_pitchLFO_LengthFixd = length;
							//	変移.w
							if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DW, true, out tmp_d0)) return false;
							m_sourceAddress += 2;
							int delta = (int)((short)tmp_d0);
							delta *= 256;
							if (lfo_com_f >=0x04)
							{
								delta *= 256;
								lfo_com_f &= 0x03;
							}
							m_pitchLFO_DeltaStart = delta;
							if (lfo_com_f != 0x02)
							{
								delta = 0;
							}
							m_pitchLFO_OffsetStart = delta;
							MMLPLfoReInit();
						}
						return true;
					case 0xED:
						//	ADPCM/ノイズ	周波数設定
						//		チャンネルH	[$ED] + [???]	ノイズ周波数設定。ビット7はノイズON/OFF
						//		チャンネルP	[$ED] + [???]	Fコマンド対応
						if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
						m_sourceAddress += 1;
						if (!m_isPcmChannel)
						{
							m_mdxParser.WriteYM2151RegData((byte)0x0F, (byte)tmp_d0);
						}
						else
						{
							m_regSamplingRate = (byte)(tmp_d0 & 0x07);
						}
						return true;
					case 0xEE:
						//	同期信号待機	[$EE]
						int currentChannel = m_mdxParser.m_currentChannel;
						if (m_mdxParser.m_isSyncArray[currentChannel])
						{
							m_mdxParser.m_isSyncArray[currentChannel] = false;
							m_mdxParser.m_isSendSyncArray[currentChannel] = false;
							m_isSetSync = false;
						}
						else
						{
							m_isSetSync = true;
							isExit = true;
						}
						return true;
					case 0xEF:
						//	同期信号送出	[$EF] + [チャネル番号(0～15)]
						//	CHANNEL
						if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
						m_sourceAddress += 1;
						if (tmp_d0 < m_mdxParser.m_channelNum)
						{
							m_mdxParser.m_isSyncArray[(int)tmp_d0] = true;
							m_mdxParser.m_isSendSyncArray[(int)tmp_d0] = true;
						}
						return true;
					case 0xF0:
						//	キーオンディレイ	[$F0] + [???]	kコマンド対応
						if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
						m_sourceAddress += 1;
						m_keyOnDelay = (byte)tmp_d0;
						return true;
					case 0xF1:
						//	データエンド	[$F1] + [$00]				演奏終了
						//					[$F1] + [ループポインタ].w	ポインタ位置から再演奏
						if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
						m_sourceAddress += 1;
						//	演奏終了
						if (tmp_d0 == 0)
						{
							MMLKeyOff();
							m_isChannelHalt = true;
							isExit = true;
							return true;
						}
						//	ループポインタ
						else
						{	//	MML のループアドレス
							if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d1)) return false;
							var loopAddress = m_sourceAddress + (int)((short)(((tmp_d0 << 8) & 0xFF00) | (tmp_d1 & 0x00FF))) + 1;
							//	MML のループアドレス の PLAY 中間データのインデックスを見つける
							var addressAndIndx = m_addressAndIndexList.Find(x => x.m_mmlCmdAddress == loopAddress);
							if (addressAndIndx == null)
							{
								m_mdxParser.ErrorString= $"Mdx Loop Address can not found.";
								return false;
							}
							//	チャンネルのループカウント値が設定がされている（負数でない）場合は、減算してゼロになったら負数にする
							if (m_mdxParser.m_loopCounters[m_channelNo] >= 0)
							{
								m_mdxParser.m_loopCounters[m_channelNo] -= 1;
								if (m_mdxParser.m_loopCounters[m_channelNo] == 0)
								{
									m_mdxParser.m_loopCounters[m_channelNo] = -1;
								}
							}
							//	ループカウントが完了した	か	チャンネルのループカウント値が設定がされていない場合
							if (m_mdxParser.m_loopCounters[m_channelNo] < 0)
							{
								m_mdxParser.m_hasLoop = true;
								var index = addressAndIndx.m_playImIndex;
								m_mdxParser.m_loopImIndexArray[m_channelNo] = index;
								var lastCycle = m_mdxParser.GetPlayImDataCycleWaitByIndex(-1);
								var loopCycle = m_mdxParser.GetPlayImDataCycleWaitByIndex(index);
								m_mdxParser.m_loopCycleArray[m_channelNo] = lastCycle - loopCycle;
								m_isChannelHalt = true;
								isExit = true;
							}
							//	 ループカウントが継続
							else
							{
								m_sourceAddress = loopAddress;
							}
							return true;
						}
					case 0xF2:
						//	ポルタメント	[$F2] + [変移※3].w	_コマンド対応	１単位は（半音／１６３８４）
						//					※3	変移	１クロック毎の変化量。単位はデチューンの1/256
						if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DW, true, out tmp_d0)) return false;
						m_sourceAddress += 2;
						m_portamentoDelta = (int)((((short)tmp_d0)*256));
						m_isPortamento = true;
						return true;
					case 0xF3:
						//	デチューン		[$F3] + [???].w		Dコマンド対応   １単位は（半音／６４）
						if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DW, true, out tmp_d0)) return false;
						m_sourceAddress += 2;
						m_detune = (short)tmp_d0;
						return true;
					case 0xF4:
						//	リピート脱出	[$F4] + [終端コマンドへのオフセット+1].w
						if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DW, true, out tmp_d0)) return false;
						m_sourceAddress += 2;
						{
							int btmaddr = m_sourceAddress + (int)tmp_d0;
							if (!m_mdxParser.GetSourceData(btmaddr, DataSize.DW, true, out tmp_d0)) return false;
							btmaddr += 2;
							int btmoffset = (int)((tmp_d0 ^ 0xFFFF)+1);
							int targetAddress = btmaddr - btmoffset - 1;

							int index = Array.FindIndex(m_repeatFrameArray, x => x.m_repeatAddress == targetAddress);
							if (index >= REPEAT_COUNT) 
							{
								m_mdxParser.ErrorString= $"Mdx Address can not found.";
								return false;
							}
							if (m_repeatFrameArray[index].m_repeatCount==1)
							{
								// break
								m_sourceAddress = btmaddr;
								m_repeatFrameArray[index].m_repeatAddress = 0;
								m_repeatFrameArray[index].m_repeatCount = 0;
							}
						}
						return true;
					case 0xF5:
						//	リピート終端	[$F5] + [開始コマンドへのオフセット+2].w
						if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DW, true, out tmp_d0)) return false;
						m_sourceAddress += 2;
						{
							int offset = (int)((tmp_d0 ^ 0xFFFF)+1);
							int targetAddress = m_sourceAddress - offset - 1;
							int index = Array.FindIndex(m_repeatFrameArray, x => x.m_repeatAddress == targetAddress);
							if (index >= REPEAT_COUNT) 
							{
								m_mdxParser.ErrorString= $"Mdx Address can not found.";
								return false;
							}
							m_repeatFrameArray[index].m_repeatCount--;
							if (m_repeatFrameArray[index].m_repeatCount>0)
							{
								//	Repeat
								m_sourceAddress -= offset;
							} 
							else
							{
								// Exit Repeat
								m_repeatFrameArray[index].m_repeatAddress = 0;
							}
						}
						return true;
					case 0xF6:
						//	リピート開始	[$F6] + [リピート回数] + [$00]
						if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
						//	00 を読みとばす
						m_sourceAddress += 2;
						{
							int index = Array.FindIndex(m_repeatFrameArray, x => x.m_repeatAddress == 0);
							if (index >= REPEAT_COUNT)
							{
								m_mdxParser.ErrorString= $"Mdx Repeat list overflow.";
								return false;
							}
							m_repeatFrameArray[index].m_repeatAddress = m_sourceAddress - 1;
							m_repeatFrameArray[index].m_repeatCount = (int)tmp_d0;
						}
						return true;
					case 0xF7:
						//	キーオフ無効	[$F7]	次のNOTE発音後キーオフしない
						//		A6->S0016 |= 0x04;
						m_isNoKeyOff = true;
						return true;
					case 0xF8:
						//	発音長指定
						//		[$F8] + [$01～$08]	qコマンド対応
						//		[$F8] + [$FF～$80]	@qコマンド対応（2の補数）
						if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
						m_sourceAddress += 1;
						m_keyLength = (byte)(tmp_d0);
						return true;
					case 0xF9:
						//	音量増大
						//		[$F9]	※vコマンド後では、 v0  →   v15 へと変化
						//				※@vコマンド後では、@v0 → @v127 へと変化
						if (m_volume != 0x80 && m_volume != 0x0F)
						{
							if (m_volume < 0x80)
							{
								m_volume += 1;
							}
							else
							{
								m_volume -= 1;
							}
							//m_isSetVolume = true;
						}
						return true;
					case 0xFA:
						//	音量減小
						//	[$FA]		※vコマンド後では、 v15   →  v0 へと変化
						//				※@vコマンド後では、@v127 → @v0 へと変化
						if (m_volume != 0xFF && m_volume != 0x00)
						{
							if (m_volume > 0x7F)
							{
								m_volume += 1;
							}
							else
							{
								m_volume -= 1;
							}
							//m_isSetVolume = true;
						}
						return true;
					case 0xFB:
						//	音量設定
						//		[$FB] + [$00～$15]		vコマンド対応
						//		[$FB] + [$80～$FF]		@vコマンド対応（ビット7無効）
						if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
						m_sourceAddress += 1;
						m_volume = (byte)tmp_d0;
						//m_isSetVolume = true;
						return true;
					case 0xFC:
						//	出力位相設定	[$FC] + [???]	pコマンド対応
						if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
						m_sourceAddress += 1;
						if (!m_isPcmChannel)
						{
							tmp_d1 = (((uint)m_regPAN) & 0x3F) | ((tmp_d0 << 6) & 0xC0);
							m_regPAN = (byte)(tmp_d1);
							m_isSetPan = true;
						}
						else
						{
							tmp_d0 = (tmp_d0 == 0x00 || tmp_d0 == 0x03) ?  (tmp_d0 ^ 0x03) : tmp_d0;
							m_regPAN = ((byte)tmp_d0);
						}
						return true;
					case 0xFD:
						//	音色設定		[$FD] + [???]	@コマンド対応
						{
							if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
							m_sourceAddress += 1;
							if (!m_isPcmChannel)
							{
								int nAddress = m_mdxParser.GetNeiroAddress(tmp_d0);
								if (nAddress < 0)
								{
									return true;
								}
								for (int i = 0; i < (int)NEIRO_BYTE_SIZE ; i++)
								{
									if (!m_mdxParser.GetSourceData(nAddress, DataSize.DB, true, out tmp_d0)) return false;
									m_neiroDataArray[i] = (byte)tmp_d0;
									nAddress += 1;
								}
								m_isSetNeiro = true;
							}
						}
						return true;
					case 0xFE:
						//	OPMレジスタ設定	[$FE] + [レジスタ番号] + [出力データ]
						if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
						m_sourceAddress += 1;
						if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
						m_sourceAddress += 1;
						m_mdxParser.WriteYM2151RegData((byte)tmp_d0, (byte)tmp_d1);
						return true;
					case 0xFF:
						//	テンポ設定		[$FF] + [???]	@tコマンド対応
						if (!m_mdxParser.GetSourceData(m_sourceAddress, DataSize.DB, true, out tmp_d0)) return false;
						m_sourceAddress += 1;
						m_mdxParser.SetTempo((int)tmp_d0);
						return true;
					default:
						//	未定義コマンド
						m_mdxParser.ErrorString= $"Mdx Unknown Command.";
						m_isChannelHalt = true;
						return false;
				}
			}

			///	<summary>
			///	音程ＬＦＯの計算処理
			/// </summary>
			private void MMLPlfoCalculate()
			{
				switch(m_pitchLFO_Type)
				{
					default:
						break;
					case 1:
						m_pitchLFO_Offset += m_pitchLFO_Delta;
						m_pitchLFO_LengthCounter -= 1;
						if (m_pitchLFO_LengthCounter == 0)
						{
							m_pitchLFO_LengthCounter = m_pitchLFO_Length;
							m_pitchLFO_Offset *= -1;
						}
						break;
					case 2:
						m_pitchLFO_Offset = m_pitchLFO_Delta;
						m_pitchLFO_LengthCounter -= 1;
						if (m_pitchLFO_LengthCounter == 0)
						{
							m_pitchLFO_LengthCounter = m_pitchLFO_Length;
							m_pitchLFO_Delta *= -1;
						}
						break;
					case 3:
						m_pitchLFO_Offset += m_pitchLFO_Delta;
						m_pitchLFO_LengthCounter -= 1;
						if (m_pitchLFO_LengthCounter == 0)
						{
							m_pitchLFO_LengthCounter = m_pitchLFO_Length;
							m_pitchLFO_Delta *= -1;
						}
						break;
					case 4:
						m_pitchLFO_LengthCounter -= 1;
						if (m_pitchLFO_LengthCounter == 0)
						{
							short rnd = m_mdxParser.EasyRandom();
							m_pitchLFO_Offset = m_pitchLFO_Delta * (int)rnd;
							m_pitchLFO_LengthCounter = m_pitchLFO_Length;
						}
						break;
				}
			}

			///	<summary>
			///	音量ＬＦＯの計算処理
			/// </summary>
			private void MMLVlfoCalculate()
			{
				switch(m_volumeLFO_Type)
				{
					default:
						break;
					case 1:
						m_volumeLFO_Offset += m_volumeLFO_Delta;
						m_volumeLFO_LengthCounter -= 1;
						if (m_volumeLFO_LengthCounter == 0)
						{
							m_volumeLFO_LengthCounter = m_volumeLFO_Length;
							m_volumeLFO_Offset = m_volumeLFO_DeltaFixd;
						}
						break;
					case 2:
						m_volumeLFO_LengthCounter -= 1;
						if (m_volumeLFO_LengthCounter == 0)
						{
							m_volumeLFO_LengthCounter = m_volumeLFO_Length;
							m_volumeLFO_Offset += m_volumeLFO_Delta;
							m_volumeLFO_Delta *= -1;
						}
						break;
					case 3:
						m_volumeLFO_Offset += m_volumeLFO_Delta;
						m_volumeLFO_LengthCounter -= 1;
						if (m_volumeLFO_LengthCounter == 0)
						{
							m_volumeLFO_LengthCounter = m_volumeLFO_Length;
							m_volumeLFO_Delta *= -1;
						}
						break;
					case 4:
						m_volumeLFO_LengthCounter -= 1;
						if (m_volumeLFO_LengthCounter == 0)
						{
							short rnd = m_mdxParser.EasyRandom();
							m_volumeLFO_LengthCounter = m_volumeLFO_Length;
							m_volumeLFO_Offset =(short)(m_volumeLFO_Delta * rnd);
						}
						break;
				}
			}

			///	<summary>
			///	音程ＬＦＯの再設定
			/// </summary>
			private void MMLPLfoReInit()
			{
				m_pitchLFO_LengthCounter = m_pitchLFO_LengthFixd;
				m_pitchLFO_Delta = m_pitchLFO_DeltaStart;
				m_pitchLFO_Offset = m_pitchLFO_OffsetStart;
			}

			///	<summary>
			///	音量ＬＦＯの再設定
			/// </summary>
			private void MMLVLfoReInit()
			{
				m_volumeLFO_LengthCounter = m_volumeLFO_Length;
				m_volumeLFO_Delta = m_volumeLFO_DeltaStart;
				m_volumeLFO_Offset = m_volumeLFO_DeltaFixd;
			}

			///	<summary>
			///	MML	セットトーン
			/// </summary>
			private void MMLSetTone()
			{
				short note = m_fmNote;
				note += (short)((m_portamento >> 16) & 0xFFFF);
				note += (short)((m_pitchLFO_Offset >> 16) & 0xFFFF);
				if (note != m_writeFMNote)
				{
					m_writeFMNote = note;
					if (note < 0)
					{
						note = 0;
					}
					else if (note > 0x17fF)
					{
						note = 0x17FF;
					}
					note *= 4;
					byte kf_data = (byte)(note & 0xFC);
					m_mdxParser.WriteYM2151RegData((byte)(0x30 + m_channelNo), kf_data);	//	KF
					byte kc_data = MMLKeyCodeTable[(int)((note >> 8) &0xFF)];
					m_mdxParser.WriteYM2151RegData((byte)(0x28 + m_channelNo), kc_data);	//	KC
				}
			}
			//	param keycode	オクターブ0のD#を0とした音階、D# E F F# G G# A A# B (オクターブ1) C C# D....と並ぶ
			//	param kf		音階微調整、64で1音分上がる。
			private static readonly byte[] MMLKeyCodeTable = new byte[0x60]
			{
				0x00,	0x01,	0x02,	0x04,	0x05,	0x06,	0x08,	0x09,	//	00	o0-D#	o0-E	o0-F	o0-F#	o0-G	o0-G#	o0-A	o0-A#
				0x0A,	0x0C,	0x0D,	0x0E,	0x10,	0x11,	0x12,	0x14,	//	08	o0-B	o1-C	o1-C#	o1-D	o1-D#	o1-E	o1-F	o1-F#
				0x15,	0x16,	0x18,	0x19,	0x1A,	0x1C,	0x1D,	0x1E,	//	10	o1-G	o1-G#	o1-A	o1-A#	o1-B	o2-C	o2-C#	o2-D
				0x20,	0x21,	0x22,	0x24,	0x25,	0x26,	0x28,	0x29,	//	18	o2-D#	o2-E	o2-F	o2-F#	o2-G	o2-G#	o2-A	o2-A#
				0x2A,	0x2C,	0x2D,	0x2E,	0x30,	0x31,	0x32,	0x34,	//	20	o2-B	o3-C	o3-C#	o3-D	o3-D#	o3-E	o3-F	o3-F#
				0x35,	0x36,	0x38,	0x39,	0x3A,	0x3C,	0x3D,	0x3E,	//	28	o3-G	o3-G#	o3-A	o3-A#	o3-B	o4-C	o4-C#	o4-D
				0x40,	0x41,	0x42,	0x44,	0x45,	0x46,	0x48,	0x49,	//	30	o4-D#	o4-E	o4-F	o4-F#	o4-G	o4-G#	o4-A	o4-A#
				0x4A,	0x4C,	0x4D,	0x4E,	0x50,	0x51,	0x52,	0x54,	//	38	o4-B	o5-C	o5-C#	o5-D	o5-D#	o5-E	o5-F	o5-F#
				0x55,	0x56,	0x58,	0x59,	0x5A,	0x5C,	0x5D,	0x5E,	//	40	o5-G	o5-G#	o5-A	o5-A#	o5-B	o6-C	o6-C#	o6-D
				0x60,	0x61,	0x62,	0x64,	0x65,	0x66,	0x68,	0x69,	//	48	o6-D#	o6-E	o6-F	o6-F#	o6-G	o6-G#	o6-A	o6-A#
				0x6A,	0x6C,	0x6D,	0x6E,	0x70,	0x71,	0x72,	0x74,	//	50	o6-B	o7-C	o7-C#	o7-D	o7-D#	o7-E	o7-F	o7-F#
				0x75,	0x76,	0x78,	0x79,	0x7A,	0x7C,	0x7D,	0x7E	//	58	o7-G	o7-G#	o7-A	o7-A#	o7-B	o8-C	o8-C#	o8-D
			};

			///	<summary>
			///	MML	セット音色
			/// </summary>
			private void MMLSetNeiro()
			{
				if (m_isSetNeiro)
				{
					m_regPAN &= (byte)0xC0;
					m_regPAN |= m_neiroDataArray[0];
					m_regCarrierSlot = MMLCarrierSlotTable[((int)m_neiroDataArray[0]) & 0x07];
					int carrierSlot = (int)m_regCarrierSlot;
					m_regSlotMask = (byte)( (((int)m_neiroDataArray[1]) << 3) | m_channelNo );

					int index = 2;
					int reg = 0x40 + m_channelNo;
					for (int i = 0; i < 4; i++)
					{
						m_mdxParser.WriteYM2151RegData((byte)reg, m_neiroDataArray[index]);
						index += 1;
						reg += 8;
					}
					for (int i = 0; i < 4; i++)
					{
						byte tl = m_neiroDataArray[index];
						if ((carrierSlot & 0x01) != 0)
						{
							tl = 0x7F;
						}
						m_mdxParser.WriteYM2151RegData((byte)reg, tl);
						carrierSlot /= 2;
						index += 1;
						reg += 8;
					}
					for (int i = 0; i < 16; i++)
					{
						m_mdxParser.WriteYM2151RegData((byte)reg, m_neiroDataArray[index]);
						index += 1;
						reg += 8;
					}
					m_volumeCode = 0xFF;
					m_isSetPan = true;
				}
				m_isSetNeiro = false;
			}
			private static readonly byte[] MMLCarrierSlotTable = new byte[0x8]
			{
				0x08,	0x08,	0x08,	0x08,	0x0c,	0x0e,	0x0e,	0x0f,
			};

			///	<summary>
			///	MML	音量制御
			/// </summary>
			private void MMLUpdateVolume()
			{
				int tl = (int)m_volume;
				if ((tl & 0x80) == 0)
				{
					tl = (int)(MMLVolumeTable[tl]);
				}
				else 
				{
					tl &= 0x7F;
				}
				int vlfoOffset =(int)(m_volumeLFO_Offset/256);
				vlfoOffset += tl;
				if (vlfoOffset >= 0x100 || vlfoOffset < 0)
				{
					vlfoOffset = 0x7F;
				}
				byte tb = (byte)(vlfoOffset & 0xFF);
				if (tb != m_volumeCode)
				{
					m_volumeCode = tb;
					uint carrierSlot = (uint)m_regCarrierSlot;
					int reg = 0x60 + m_channelNo;
					int tlIndex = 6;
					for (int i = 0; i < 4; i++)
					{
						byte att = m_neiroDataArray[tlIndex+i];
						if ((carrierSlot & 0x01) != 0)
						{
							att += tb;
							if (att > 0x7f)
							{
								att = 0x7F;
							}
							m_mdxParser.WriteYM2151RegData((byte)reg , att);
						}
						carrierSlot = carrierSlot >> 1;
						reg += 8;
					}
				}
			}
			private static readonly byte[] MMLVolumeTable = new byte[0x10]
			{
				0x2a,	0x28,	0x25,	0x22,	0x20,	0x1d,	0x1a,	0x18,
				0x15,	0x12,	0x10,	0x0d,	0x0a,	0x08,	0x05,	0x02,
			};

			///	<summary>
			///	MML	セットＰＡＮ
			/// </summary>
			private void MMLSetPan()
			{
				if (m_isSetPan)
				{
					int reg = 0x20 + m_channelNo;
					m_mdxParser.WriteYM2151RegData((byte)reg, m_regPAN);
				}
				m_isSetPan = false;
			}

			///	<summary>
			///	MML	キーオン
			/// </summary>
			private void MMLKeyOn()
			{
				if (!m_isKeyOn)
				{
					m_isKeyOn = true;
					if (!m_isEX3NoKeyOff)
					{
						ForceRegisterKeyOff(true);
					}
					if (!m_isPcmChannel)
					{
						m_mdxParser.WriteYM2151RegData((byte)0x08, m_regSlotMask);
					}
					else
					{
						var pdxData = m_mdxParser.GetPdxData(m_pcmNote);
						if (pdxData == null)
						{
							m_mdxParser.AddWarningString($"WARNING : MDX None PCM block id. BlockID:{m_pcmNote:X}.");
							m_isKeyOn = false;
							return;
						}
						if (pdxData.m_size == 0)
						{
							m_mdxParser.AddWarningString($"WARNING : MDX PCM size 0. BlockID:{m_pcmNote:X}.");
							m_isKeyOn = false;
							return;
						}
						m_mdxParser.SetM6258Pan(m_regPAN);
						m_mdxParser.SetM6258SamplingRate(m_regSamplingRate);
						m_mdxParser.AddVgmStreamStartSizeFastCallToPlayImData(m_mdxParser.m_m6258CS, a1:0, code:(byte)0x95, vstrmBlockId:(int)m_pcmNote, vstrmFlagMode:(byte)0, vstrmStart:pdxData.m_start, vstrmSize:pdxData.m_size, vstrmSamplingRate:m_mdxParser.m_m6258SamplingRate);
						m_mdxParser.m_isUsePDX = true;
						pdxData.m_isActive = true;
					}
				}
			}

			///	<summary>
			///	MML	キーオフ
			/// </summary>
			private void MMLKeyOff()
			{
				if (m_isKeyOn)
				{
					if (!m_isEX3NoKeyOff)
					{
						ForceRegisterKeyOff(false);
					}
				}
				m_isKeyOn = false;
			}
			private void ForceRegisterKeyOff(bool isBeforePCMPlay)
			{
				if (!m_isPcmChannel)
				{
					m_mdxParser.WriteYM2151RegData((byte)0x08, (byte)m_channelNo);
				}
				else
				{
					if (!isBeforePCMPlay)
					{
						if (/*!m_isSetVolume && */!m_isSetNeiro && !m_isSetPan && !m_isSetSync)
						{
							m_mdxParser.AddVgmStopStreamToPlayImData(m_mdxParser.m_m6258CS, code:(byte)0x94, a1:0) ;
						}
						m_mdxParser.AddTwoDataToPlayImData(m_mdxParser.m_m6258CS, a1:0, data0:(byte)0x00, data1:(byte)0x01);
					}
				}
			}
		}
	}
}
