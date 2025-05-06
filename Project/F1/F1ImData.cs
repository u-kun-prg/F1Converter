using System;
using System.Collections.Generic;
using System.Linq;

namespace F1
{
	///	<summary>
	/// F1中間データクラス
	/// </summary>
	public class F1ImData
	{
		///	<summary>
		///	PLAY中間データリスト
		/// </summary>
		public List<PlayImData> PlayImDataList { get; private set; }
		///	<summary>
		///	PCM中間データリスト
		///	</summary>
		public List<PcmImData> PcmImDataList { get; private set; }

		///	<summary>
		///	YM2612 Run Legnth 圧縮フラグ
		///	</summary>
		public bool IsYM2612DacRL { get; private set; }

		///	<summary>
		///	Chip Seclect ごとの TopCode 
		///	</summary>
		public List<int>TopCodeCSList  { get; private set; }

		///	<summary>
		///	Chip Seclect ごとの １CHIP でデュアル CHIP に対応フラグ
		///	</summary>
		public Dictionary<int, bool>IsDualIn1ChipCSDict { get; private set; }

		///	<summary>
		///	設定フラグ	音程周波数変換を行う
		/// </summary>
		public bool IsToneAdjust { get; private set; }

		///	<summary>
		///	設定フラグ	データシュリンクを行う
		/// </summary>
		public bool IsShrink { get; private set; }

		///	<summary>
		///	設定フラグ	デュアルチップの再生指定
		/// </summary>
		public bool IsDual2nd { get; private set; }

		///	<summary>
		///	設定フラグ	タイマーレジスタの有効
		/// </summary>
		public bool IsTimerReg { get; private set; }

		///	<summary>
		///	設定フラグ	FM 音量下げ値
		/// </summary>
		public int FMVol { get; private set; }

		///	<summary>
		///	設定フラグ	SSG 音量上げ値
		/// </summary>
		public int SSGVol { get; private set; }

		///	<summary>
		///	コンストラクタ
		///	</summary>
		public F1ImData(bool isYM2612DacRL, List<int> topCodeList, bool isToneAdjust, bool isShrink, bool isDual2nd, bool isTimerReg, int fmVol, int ssgVol )
		{
			this.PlayImDataList = new List<PlayImData>();
			this.PcmImDataList = new List<PcmImData>();
			this.IsYM2612DacRL = isYM2612DacRL;
			this.TopCodeCSList = new List<int>(topCodeList);
			this.IsDualIn1ChipCSDict = new Dictionary<int,bool>();
			this.IsToneAdjust = isToneAdjust;
			this.IsShrink = isShrink;
			this.IsDual2nd = isDual2nd;
			this.IsTimerReg = isTimerReg;
			this.FMVol = fmVol;
			this.SSGVol = ssgVol;
		}

		///	<summary>
		///	PLAY中間データのタイプ
		///	</summary>
		public enum PlayImType
		{
			NONE,
			TWO_DATA,
			ONE_DATA,
			CYCLE_WAIT,
			WRITE_WAIT,
			WRITE_WAIT_RL,
			WRITE_SEEK,
			VSTRM_DATA,
			VSTRM_SAMPLING_RATE,
			VSTRM_START_SIZE,
			VSTRM_STOP_STREAM,
			VSTRM_START_SIZE_FAST,
			END_CODE,
			LOOP_POINT,
		}
		///	<summary>
		///	PLAY中間データクラス
		///	</summary>
		public class PlayImData
		{
			public PlayImType m_imType;
			public int m_chipSelect;
			public int m_A1;

			public int m_cycleWait;
			public int m_writeWait;
			public int m_writeWaitRL;
			public int m_writeTotal;
			public int m_writeRunLength;
			public int m_cycleWaitCount;
			public int m_seekAddress;
			public byte m_data0;
			public byte m_data1;

			public byte m_vStrmStepSize;
			public byte m_vStrmStepBase;
			public int m_vStrmBlockId;
			public byte m_vStrmFlagMode;
			public int m_vStrmStart;
			public int m_vStrmSize;
			public int m_vStrmSamplingRate;

			public int m_tmpIndex;
			public int m_tmpFreeData0;
			public int m_tmpFreeData1;

			public PlayImData(	PlayImType imType, 
								int chipSelect, 
								int a1 = 0, 

								int cycleWait = 0, 
								int writeWait = 0,
								int writeWaitRL = 0,
								int writeRunLength = 0,
								int seekAddress = 0,
								byte data0 = 0x00, 
								byte data1 = 0x00, 

								byte vstrmStepSize = 0, 
								byte vstrmStepBase = 0, 
								int vstrmBlockId = 0,
								byte vstrmFlagMode = 0,
								int vstrmStart = 0, 
								int vstrmSize = 0, 
								int vstrmSamplingRate = 0
			)
			{
				this.m_imType = imType;
				this.m_chipSelect = chipSelect;
				this.m_A1 = a1;

				this.m_cycleWait = cycleWait;
				this.m_writeWait = writeWait;
				this.m_writeWaitRL = writeWaitRL;
				this.m_writeRunLength = writeRunLength;
				this.m_cycleWaitCount = 0;
				this.m_seekAddress = seekAddress;
				this.m_data0 = data0;
				this.m_data1 = data1;

				this.m_vStrmStepSize = vstrmStepBase;
				this.m_vStrmStepBase = vstrmStepSize;
				this.m_vStrmBlockId = vstrmBlockId;
				this.m_vStrmFlagMode = vstrmFlagMode;
				this.m_vStrmStart = vstrmStart;
				this.m_vStrmSize = vstrmSize;
				this.m_vStrmSamplingRate = vstrmSamplingRate;

				this.m_tmpIndex = -1;
				this.m_tmpFreeData0 = -1;
				this.m_tmpFreeData1 = -1;
			}
		}

		///	<summary>
		///	PCM中間データクラス
		///	</summary>
		public class PcmImData
		{
			public int m_chipSelect;
			public PcmDataType m_pcmDataType;
			public int m_pcmStart;
			public int m_pcmSize;
			public int m_pcmSamplingRate;
			public byte[] m_pcmBinaryArray;

			public PcmImData(int chipSelect, PcmDataType pcmDataType, int pcmStart, int pcmSize, byte[] souceBinaryArray)
			{
				this.m_chipSelect = chipSelect;
				this.m_pcmDataType = pcmDataType;
				this.m_pcmStart = pcmStart;
				this.m_pcmSize = pcmSize;
				this.m_pcmSamplingRate = -1;
				this.m_pcmBinaryArray = new byte[pcmSize];
				for (int i = 0; i < (int)pcmSize ; i++)
				{
					this.m_pcmBinaryArray[i] = souceBinaryArray[i];
				}
			}
		}

		///	<summary>
		///	PLAY中間データリストへのデータ追加
		///	</summary>
		public void AddPlayImDataList(	PlayImType imType, 
										int chipSelect = 0, 
										int a1 = 0, 

										int cycleWait = 0, 
										int writeWait = 0,
										int writeWaitRL = 0,
										int writeRunLength = 0,
										int seekAddress = 0,
										byte data0 = 0x00, 
										byte data1 = 0x00, 

										byte vstrmStepSize = 0, 
										byte vstrmStepBase = 0, 
										int vstrmBlockId = 0, 
										byte vstrmFlagMode = 0,
										int vstrmStart = 0, 
										int vstrmSize = 0, 
										int vstrmSamplingRate = 0
									)
		{
			var listCount = PlayImDataList.Count;
			PlayImDataList.Add(new PlayImData(	imType:imType,
												chipSelect:chipSelect,
												a1:a1,

												cycleWait:cycleWait,
												writeWait:writeWait,
												writeWaitRL:writeWaitRL,
												writeRunLength:writeRunLength,
												seekAddress:seekAddress,
												data0:data0,
												data1:data1,

												vstrmStepSize:vstrmStepSize,
												vstrmStepBase:vstrmStepBase,
												vstrmBlockId:vstrmBlockId,
												vstrmFlagMode:vstrmFlagMode,
												vstrmStart:vstrmStart,
												vstrmSize:vstrmSize,
												vstrmSamplingRate:vstrmSamplingRate));
			//	CycleWaitCount は、データ実行直前の経過サイクル WAIT を格納する
			var addCycle = PlayImDataList[listCount].m_cycleWait + PlayImDataList[listCount].m_writeWait;
			addCycle += PlayImDataList[listCount].m_writeWaitRL * PlayImDataList[listCount].m_writeRunLength;
			if (listCount > 0)
			{
				PlayImDataList[listCount].m_cycleWaitCount = PlayImDataList[listCount-1].m_cycleWaitCount + addCycle;
			}
			else
			{
				PlayImDataList[listCount].m_cycleWaitCount = addCycle;
			}
		}

		///	<summary>
		///	PLAY中間データリストの個数を返す
		///	</summary>
		public int GetPlayImDataListCount()
		{
			return PlayImDataList.Count;
		}

		///	<summary>
		///	PLAY中間データへリストへのインサートデータのリスト
		///	</summary>
		private List<PlayImData> m_insertPlayImDataList = new List<PlayImData>();
		///	<summary>
		///	PLAY中間データのインサートデータをセットする
		/// </summary>
		public void SetInsertPlayImData(int index, 

										PlayImType imType, 
										int chipSelect = 0, 
										int a1 = 0, 

										int cycleWait = 0, 
										int writeWait = 0, 
										int writeWaitRL = 0,
										int writeRunLength = 0,
										int seekAddress = 0,
										byte data0 = 0x00, 
										byte data1 = 0x00, 

										byte vstrmStepSize = 0, 
										byte vstrmStepBase = 0, 
										int vstrmBlockId = 0, 
										byte vstrmFlagMode = 0,
										int vstrmStart = 0, 
										int vstrmSize = 0, 
										int vstrmSamplingRate = 0
										)
		{
			var playImData = new PlayImData(imType:imType, 
											chipSelect:chipSelect, 
											a1:a1, 
											cycleWait:cycleWait, 
											writeWait:writeWait, 
											writeWaitRL:writeWaitRL,
											writeRunLength:writeRunLength,
											seekAddress:seekAddress,
											data0:data0, 
											data1:data1, 

											vstrmStepSize:vstrmStepSize,
											vstrmStepBase:vstrmStepBase,
											vstrmBlockId:vstrmBlockId,
											vstrmFlagMode:vstrmFlagMode,
											vstrmStart:vstrmStart,
											vstrmSize:vstrmSize,
											vstrmSamplingRate:vstrmSamplingRate);
			playImData.m_tmpIndex = index;
			m_insertPlayImDataList.Add(playImData);
		}
		/// <summary>
		/// PLAY中間データリストへのインサート
		/// </summary>
		public void InsertPlayImDataList()
		{
			m_insertPlayImDataList.Sort((t0, t1) => t0.m_tmpIndex - t1.m_tmpIndex);
			int indexAdd = 0;
			foreach(var playImData in m_insertPlayImDataList)
			{
				PlayImDataList.Insert(playImData.m_tmpIndex+indexAdd, playImData);
				indexAdd += 1;
			}
			m_insertPlayImDataList.Clear();
			//	PLAY中間データリスト全体のサイクル WAIT を再計算.
			RecalculatePlayImDataListCycleWaitCount();
		}
		///	<summary>
		///	PLAY中間データリストの指定インデックスからデータを取得する
		///	</summary>
		public PlayImData GetPlayImData(int index)
		{
			return PlayImDataList[index];
		}
		///	<summary>
		///	PLAY中間データリストの指定インデックス以降を削除する
		///	</summary>
		public void RemovePlayImDataListAfterIndex(int index)
		{
			var count = PlayImDataList.Count;
			PlayImDataList.RemoveRange(index, count - index);
		}

		///	<summary>
		///	PLAY中間データリストの指定インデックスまでのサイクル WAIT 数を返す
		///	指定インデックスが負数の場合は、PLAY中間データリスト全体のサイクル WAIT 数を返す
		///	</summary>
		public int GetPlayImDataListCycleWaitByIndex(int index)
		{
			index = (index < 0) ? (PlayImDataList.Count-1) : index;
			return PlayImDataList[index].m_cycleWaitCount;
		}
		///	<summary>
		///	PLAY中間データリストに ChipSelect 値が３つ以上存在するかを返す
		///	</summary>
		public bool IsPlayImDataList3CS()
		{
			return (PlayImDataList.FindIndex(x => x.m_chipSelect >=2) >= 0);
		}
		///	<summary>
		///	PLAY中間データリストにＡ１値 が３以上存在するかを返す
		///	</summary>
		public bool IsPlayImDataList3A1()
		{
			return (PlayImDataList.FindIndex(x => x.m_A1 >=2) >= 0);
		}
		///	<summary>
		///	PLAY中間データリスト全体のサイクル WAIT を再計算する
		///	</summary>
		private void RecalculatePlayImDataListCycleWaitCount()
		{
			for (int listCount = 0, l = PlayImDataList.Count; listCount < l; listCount ++)
			{
				var addCycle = PlayImDataList[listCount].m_cycleWait + PlayImDataList[listCount].m_writeWait;
				addCycle += PlayImDataList[listCount].m_writeWaitRL * PlayImDataList[listCount].m_writeRunLength;
				if (listCount > 0)
				{
					PlayImDataList[listCount].m_cycleWaitCount = PlayImDataList[listCount-1].m_cycleWaitCount + addCycle;
				}
				else
				{
					PlayImDataList[listCount].m_cycleWaitCount = addCycle;
				}
			}
		}
		///	<summary>
		///	PLAY中間データリスト全体をクリーンアップする
		///	</summary>
		public void CleanupPlayImDataList()
		{
			//	PLAY中間データリスト	タイプが NONE のデータを削除する
			PlayImDataList.RemoveAll(p => p.m_imType == PlayImType.NONE);

			//	サイクル WAIT が連続する場合は、サイクル WAIT を合算して１つのデータにする
			for (int i=1, l = PlayImDataList.Count; i < l; i++)
			{
				if (PlayImDataList[i-1].m_imType == PlayImType.CYCLE_WAIT && PlayImDataList[i].m_imType == PlayImType.CYCLE_WAIT)
				{
					PlayImDataList[i].m_cycleWait += PlayImDataList[i-1].m_cycleWait;
					PlayImDataList[i-1].m_imType = PlayImType.NONE;
				}
			}
			//	サイクル WAIT 合算の結果、タイプが NONE になったデータを削除する
			PlayImDataList.RemoveAll(p => p.m_imType == PlayImType.NONE);
			//	PLAY中間データリスト全体のサイクル WAIT を再計算する
			RecalculatePlayImDataListCycleWaitCount();
		}

		///	<summary>
		///	PLAY中間データリストへの 書き込み WAITを ランレングスで詰めながら追加
		///	</summary>
		public void AddCompWriteWaitPlayImDataList(int writeWait, bool isUseRunLength) 
		{
			if (isUseRunLength && IsYM2612DacRL)
			{
				//	PLAY 中間データリストの個数がゼロではランレングスはできない
				if (PlayImDataList.Count > 0)
				{
					//	１つ前のPLAY 中間データ
					var beforPlayImData = PlayImDataList[PlayImDataList.Count-1];
					if (beforPlayImData.m_imType == PlayImType.WRITE_WAIT)
					{	//	１つ前のPLAY 中間データが書き込み WAIT 場合
						if (Math.Abs(beforPlayImData.m_writeWait - writeWait) < 3)
						{	//	WAIT 値の差が小さければ、１つ前のPLAY 中間データをランレングスにする
							beforPlayImData.m_imType = PlayImType.WRITE_WAIT_RL;
							beforPlayImData.m_writeRunLength = 2;
							//	WAIT 値は１つ前の値と追加値の単純な平均値
							beforPlayImData.m_writeTotal = beforPlayImData.m_writeWait + writeWait;
							var fave =  (float)(beforPlayImData.m_writeTotal) / 2f;
							//	WAIT 値平均化で WAIT 値がソースの値より大きくなりにくいよう、Float 計算
							beforPlayImData.m_writeWaitRL = (int)(fave);	// + 0.5f);
							beforPlayImData.m_writeWait = 0;
							return;
						}
					}
					else if (beforPlayImData.m_imType == PlayImType.WRITE_WAIT_RL && beforPlayImData.m_writeRunLength < 255)
					{	//	１つ前のPLAY 中間データが書き込み WAIT ランレングスで、レングスが 255 個を越えてない場合
						if (Math.Abs(beforPlayImData.m_writeWaitRL - writeWait) < 3)
						{	//	WAIT 値の差が小さければ、１つ前の書き込み WAIT ランレングスに入れ込む
							beforPlayImData.m_writeTotal += writeWait;
							beforPlayImData.m_writeRunLength += 1;
							var fave = (float)(beforPlayImData.m_writeTotal) / (float)beforPlayImData.m_writeRunLength;
							//	WAIT 値平均化で WAIT 値がソースの値より大きくなりにくいよう、Float 計算
							beforPlayImData.m_writeWaitRL = (int)(fave);	//	 + 0.5f);
							var totalWaitRL = (beforPlayImData.m_writeWaitRL * beforPlayImData.m_writeRunLength);
							return;
						}
					}
				}
			}
			AddPlayImDataList(PlayImType.WRITE_WAIT, writeWait:writeWait);
		}


		///	<summary>
		///	 PCM中間データリストへのデータ追加
		/// </summary>
		public PcmImData AddPcmImDataList(int chipSelect, PcmDataType pcmDataType, int pcmStart, int pcmSize, byte[] souceBinaryArray)
		{
			var pcmImData = new PcmImData(chipSelect, pcmDataType, pcmStart, pcmSize, souceBinaryArray);
			PcmImDataList.Add(pcmImData);
			return pcmImData;
		}
		///	<summary>
		///	PCM中間データリストの先頭にデータ追加
		/// </summary>
		public void AddTopPcmImDataList(int chipSelect, PcmDataType pcmDataType, int pcmStart, int pcmSize, byte[] souceBinaryArray)
		{
			PcmImDataList.Insert( 0, new PcmImData(chipSelect, pcmDataType, pcmStart, pcmSize, souceBinaryArray));
		}
		///	<summary>
		///	PCM中間データリストから指定のデータを取得する
		/// </summary>
		public PcmImData GetPcmImData(int chipSelect, PcmDataType dataType, int blockId)
		{
			int blockCounter = 0;
			foreach(var pcmData in PcmImDataList.Where(x => x.m_chipSelect == chipSelect && x.m_pcmDataType == dataType))
			{
				if (blockCounter == blockId)
				{
					return pcmData;
				}
				blockCounter += 1;
			}
			return null;
		}
		///	<summary>
		///	PCM中間データリストから指定データのスタートとサイズを返す
		/// </summary>
		public bool GetPcmImDataStartSize(int chipSelect, PcmDataType pcmDataType, int vstrmBlockId, out int vstrmStart, out int vstrmSize)
		{
			bool result = false;
			int blockCounter = 0;
			vstrmStart = 0;
			vstrmSize = 0;
			foreach(var pcmData in PcmImDataList.Where(x => x.m_chipSelect == chipSelect && x.m_pcmDataType == pcmDataType))
			{
				if (blockCounter == vstrmBlockId)
				{
					vstrmStart = pcmData.m_pcmStart;
					vstrmSize = pcmData.m_pcmSize;
					result = true;
					break;
				}
				blockCounter += 1;
			}
			return result;
		}

	}
}