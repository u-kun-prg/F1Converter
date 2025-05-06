using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace F1
{
	/// <summary>
	///	CHIP M6295 クラス	M6258からの変換に対応する
	/// </summary>
	public class Chip_M6295 : SoundChip
	{
		private enum M6258Rate
		{
			R3906 = 0,
			R5208 = 1,
			R7813 = 2,
			R10417 = 3,
			R15625 = 4,
			UNKNOWN = 5,
		}
		private const float M6295_RATE = 1000000f / 132f;
		private const float M6258_RATE_3906 = 4000000f / 1024f;
		private const float M6258_RATE_5208 = 4000000f /  768f;
		private const float M6258_RATE_7813 = 8000000f / 1024f;
		private const float M6258_RATE_10417 = 8000000f / 768f;
		private const float M6258_RATE_15625 = 8000000f / 512f;

		private class M6258ToM6295BlockIdSR
		{
			public int BlockId;
			public int SamplingRate;
			public M6258ToM6295BlockIdSR(int blockId, int samplingRate)
			{
				this.BlockId = blockId;
				this.SamplingRate = samplingRate;
			}
		}

		/// <summary>
		/// レジスタ操作にプレイ CHIP にあわせた制御を入れる
		/// </summary>
		public override void ControlPlayChipRegiser()
		{
			switch(m_targetChip.SourceChipType)
			{
				case ChipType.M6295:
					foreach(var playImData in m_imData.PlayImDataList.Where(x => x.m_chipSelect == m_targetChip.ChipSelect && x.m_imType == F1ImData.PlayImType.ONE_DATA))
					{
						if ( ((playImData.m_data1 & 0x80) != 0) && ((playImData.m_data1 & 0x7F) == 0) )
						{	//	Delete Phrase No==0
							playImData.m_imType = F1ImData.PlayImType.NONE;
							continue;
						}
					}
					m_imData.CleanupPlayImDataList();
					break;

				case ChipType.M6258:
					M6528ToM6295_MultiSamplingRate();
					M6528ToM6295_PcmDataConvert();
					M6258To6295_CreatePhraseData();
					M6258ToM6295_CheckPlayImData();
					break;
			}
		}

		/// <summary>
		///	M6258 から M6295 変換	Play データ中間データの対応
		/// </summary>
		private void M6258ToM6295_CheckPlayImData()
		{
			//	M6258 Register Conver.
			foreach(var playImData in m_imData.PlayImDataList.Where(x => x.m_chipSelect == m_targetChip.ChipSelect))
			{
				switch (playImData.m_imType)
				{
					case F1ImData.PlayImType.VSTRM_DATA:
					case F1ImData.PlayImType.VSTRM_SAMPLING_RATE:
					case F1ImData.PlayImType.VSTRM_START_SIZE:
						playImData.m_imType = F1ImData.PlayImType.NONE;
						break;
					case F1ImData.PlayImType.VSTRM_STOP_STREAM:
						playImData.m_imType = F1ImData.PlayImType.ONE_DATA;
						playImData.m_data0 = 0x08;	//	Channel 0.Stop
						break;
					case F1ImData.PlayImType.TWO_DATA:
						switch(playImData.m_data0)
						{
							case 0x00:		//	00H ADPCM Command Register.
								if ((playImData.m_data1 & 0x01) != 0x00)
								{				//	Stop.
									playImData.m_imType = F1ImData.PlayImType.ONE_DATA;
									playImData.m_data0 = 0x08;	//	Channel 0.Stop
								}
								else
								{				//	Start with stream 95
									playImData.m_imType = F1ImData.PlayImType.NONE;
								}
								break;
							case 0x02:	//	02H	PAN							(Not compatible.)
								break;
							default:
							//case 0x01:	//	01H	Data write register			(Not compatible.)
							//case 0x0B:	//	0BH	Master Clock.				(Clock control with stream 95.)
							//case 0x0C:	//	0CH	Frequency division,			(Clock control with stream 95.)
							//case 0x17:	//	17H	Port.						(not clear)
								playImData.m_imType = F1ImData.PlayImType.NONE;
								break;
						}
						break;
				}
			}
			m_imData.CleanupPlayImDataList();
			//	Stream Command 95 Convert. And PAN.
			for (int index = 0, l = m_imData.PlayImDataList.Count; index < l; index++)
			{
				if (m_imData.PlayImDataList[index].m_chipSelect == m_targetChip.ChipSelect)
				{
					var playImData = m_imData.PlayImDataList[index];
					if (playImData.m_imType == F1ImData.PlayImType.VSTRM_START_SIZE_FAST)
					{
						playImData.m_imType = F1ImData.PlayImType.ONE_DATA;
						playImData.m_data0 = (byte)(((playImData.m_vStrmBlockId + 1) & 0x7F) | 0x80);
						m_imData.SetInsertPlayImData(	index+1, 
														F1ImData.PlayImType.ONE_DATA, 
														chipSelect:m_targetChip.ChipSelect, 
														a1:playImData.m_A1, 
														data0:0x10);
					}
					else if (playImData.m_imType == F1ImData.PlayImType.TWO_DATA && playImData.m_data0 == 0x02)
					{
						playImData.m_imType = F1ImData.PlayImType.ONE_DATA;
						playImData.m_data0 = 0x80;;
						byte panData = (byte)(0x80 | playImData.m_data1);
						m_imData.SetInsertPlayImData(	index+1, 
														F1ImData.PlayImType.ONE_DATA, 
														chipSelect:m_targetChip.ChipSelect, 
														a1:playImData.m_A1, 
														data0:panData);
					}
				}
			}
			m_imData.InsertPlayImDataList();
		}

		/// <summary>
		///	M6258 から M6295 変換	M6258の複数サンプリング レートの再生に対応
		/// </summary>
		private void M6528ToM6295_MultiSamplingRate()
		{
			var blockIdSrDict = new Dictionary<int, List<M6258ToM6295BlockIdSR>>();
			foreach(var playImData in m_imData.PlayImDataList.Where(x => x.m_chipSelect == m_targetChip.ChipSelect && x.m_imType == F1ImData.PlayImType.VSTRM_START_SIZE_FAST))
			{
				var pcmImData = m_imData.GetPcmImData(m_targetChip.ChipSelect, 0, playImData.m_vStrmBlockId);
				if (pcmImData.m_pcmSamplingRate < 0)
				{
					pcmImData.m_pcmSamplingRate = playImData.m_vStrmSamplingRate;
					continue;
				}
				if (pcmImData.m_pcmSamplingRate != playImData.m_vStrmSamplingRate)
				{
					List<M6258ToM6295BlockIdSR> tmpSrList;
					var blockId = m_imData.PcmImDataList.Count(x=>x.m_chipSelect == m_targetChip.ChipSelect);
					if (blockIdSrDict.ContainsKey(playImData.m_vStrmBlockId))
					{
						tmpSrList = blockIdSrDict[playImData.m_vStrmBlockId];
						var findIndex = tmpSrList.FindIndex(x => x.SamplingRate == playImData.m_vStrmSamplingRate);
						if (findIndex >= 0)
						{
							playImData.m_vStrmBlockId = tmpSrList[findIndex].BlockId;
							continue;
						}
					}
					else 
					{
						tmpSrList = new List<M6258ToM6295BlockIdSR>();
					}
					tmpSrList.Add(new M6258ToM6295BlockIdSR(blockId, playImData.m_vStrmSamplingRate));
					blockIdSrDict.Add(playImData.m_vStrmBlockId, tmpSrList);
					var pcmBinaryArray = new byte[pcmImData.m_pcmSize];
					for (int i = 0; i < pcmImData.m_pcmSize; i++)
					{
						pcmBinaryArray[i] = pcmImData.m_pcmBinaryArray[i];
					}
					playImData.m_vStrmBlockId = blockId;
					var beforePcmImData = m_imData.GetPcmImData(m_targetChip.ChipSelect, 0, (blockId-1));
					var pcmStart = beforePcmImData.m_pcmStart + beforePcmImData.m_pcmSize;
					var addPcmImData = m_imData.AddPcmImDataList(m_targetChip.ChipSelect, 0, pcmStart,pcmImData.m_pcmSize, pcmBinaryArray);
					addPcmImData.m_pcmSamplingRate = playImData.m_vStrmSamplingRate;
				}
			}
		}

		/// <summary>
		///	M6258 から M6295 変換	PCM データの変換
		/// </summary>
		private void M6528ToM6295_PcmDataConvert()
		{
			int pcmStart = 0;
			foreach(var pcmImData in m_imData.PcmImDataList.Where(x => x.m_chipSelect == m_targetChip.ChipSelect && x.m_pcmDataType == PcmDataType.PcmData0))
			{
				var decodedDataList = new List<int>();
				var resampleDataList = new List<int>();
				OkiPcm.M6258Decode(pcmImData.m_pcmBinaryArray.ToList(), decodedDataList);
				int decodedSize = decodedDataList.Count;

				var m6258Rate = M6258Rate.UNKNOWN;
				float[] rateTable = { M6258_RATE_3906, M6258_RATE_5208, M6258_RATE_7813, M6258_RATE_10417, M6258_RATE_15625 };
				float samplingRate = (float)pcmImData.m_pcmSamplingRate;
				for (int ri = 0; ri < rateTable.Length; ri ++)
				{
					if ((samplingRate < (rateTable[ri]+100f)) && (samplingRate > (rateTable[ri]-100f)) )
					{
						m6258Rate = (M6258Rate)ri;
						break;
					}
				}
				switch(m6258Rate)
				{
					case M6258Rate.R3906:
						for (int i = 0; i < decodedSize; i ++)
						{
							if ((i & 0x01) == 0)
							{
								resampleDataList.Add(decodedDataList[i]);
								resampleDataList.Add(decodedDataList[i]);
							}
						}
						break;
					case M6258Rate.R5208:
						{
							int ctr = 0;
							for (int i = 0; i < decodedSize; i ++)
							{
								if (ctr != 2)
								{
									resampleDataList.Add(decodedDataList[i]);
									ctr += 1;
								}
								else
								{
									resampleDataList.Add(decodedDataList[i]);
									resampleDataList.Add(decodedDataList[i]);
									ctr = 0;
								}
							}
						}
						break;
					case M6258Rate.R7813:
						for (int i = 0; i < decodedSize; i ++)
						{
							resampleDataList.Add(decodedDataList[i]);
						}
						break;
					case M6258Rate.R10417:
						{
							int ctr = 0;
							for (int i = 0; i < decodedSize; i ++)
							{
								if (ctr != 2)
								{
									resampleDataList.Add(decodedDataList[i]);
									ctr += 1;
								}
								else
								{
									ctr = 0;
								}
							}
						}
						break;
					case M6258Rate.R15625:
						for (int i = 0; i < decodedSize; i ++)
						{
							if ((i & 0x01) == 0)
							{
								resampleDataList.Add(decodedDataList[i]);
							}
						}
						break;
					case M6258Rate.UNKNOWN:
						for (int i = 0; i < decodedSize; i ++)
						{
							resampleDataList.Add(decodedDataList[i]);
						}
						break;
				}

				var pcmBinaryList = new List<byte>();
				OkiPcm.M6295Encode(resampleDataList, pcmBinaryList);
				pcmImData.m_pcmBinaryArray = pcmBinaryList.ToArray();
				pcmImData.m_pcmStart = pcmStart;
				pcmImData.m_pcmSize = pcmImData.m_pcmBinaryArray.Length;
				pcmStart += pcmImData.m_pcmSize;
			}
		}

		/// <summary>
		///	M6258 から M6295 変換	M6295のフェイズデータの生成
		/// </summary>
		private void M6258To6295_CreatePhraseData()
		{
			var pcmPhraseDataList = new List<byte>();
			pcmPhraseDataList.AddRange(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF});
			int pcmNum = 0;
			foreach(var playImData in m_imData.PcmImDataList.Where(x => x.m_chipSelect == m_targetChip.ChipSelect && x.m_pcmDataType == PcmDataType.PcmData0)) 
			{
				pcmNum += 1;
			}
			//var pcmNum = m_imData.PlayImDataList.Count(x => x.ChipSelect == m_targetChip.ChipSelect);
			var addBase  = (pcmNum + 1) * 8;
			foreach(var playImData in m_imData.PcmImDataList.Where(x => x.m_chipSelect == m_targetChip.ChipSelect && x.m_pcmDataType == PcmDataType.PcmData0))
			{
				var start = playImData.m_pcmStart += addBase;
				var size =  playImData.m_pcmSize;
				pcmPhraseDataList.Add( (byte)(((start & 0x00FF0000) >> 16) & 0xFF) );
				pcmPhraseDataList.Add( (byte)(((start & 0x0000FF00) >>  8) & 0xFF) );
				pcmPhraseDataList.Add( (byte)(  start & 0x000000FF				  )	);
				pcmPhraseDataList.Add( (byte)((( (size+start-1) & 0x00FF0000) >> 16) & 0xFF) );
				pcmPhraseDataList.Add( (byte)((( (size+start-1) & 0x0000FF00) >>  8) & 0xFF) );
				pcmPhraseDataList.Add( (byte)(   (size+start-1) & 0x000000FF			   ) );
				pcmPhraseDataList.Add( 0xFF );
				pcmPhraseDataList.Add( 0xFF );
			}
			m_imData.AddTopPcmImDataList(m_targetChip.ChipSelect, 0, 0x00000000, pcmPhraseDataList.Count, pcmPhraseDataList.ToArray());
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