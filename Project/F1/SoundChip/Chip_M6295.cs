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
						if (playImData.m_data1 == 0x80)
						{	//	Delete Phrase No==0
							playImData.m_imType = F1ImData.PlayImType.NONE;
						}
					}
					m_imData.CleanupPlayImDataList();
					break;

				case ChipType.M6258:
					if (m_imData.PcmImDataList.Count(x=>x.m_chipSelect == m_targetChip.ChipSelect) > 0)
					{
						M6528ToM6295_CheckSamplingRate();
						M6528ToM6295_PcmDataConvert();
						M6258To6295_CreatePhraseData();
						M6258ToM6295_CheckPlayImData();
					}
					break;
			}
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

		/// <summary>
		///	M6258 から M6295 変換
		///		PcmImData にサンプリングレートを設定する
		///		１つの PcmImData を異なるサンプリングレートで再生する場合は、
		///		別サンプリングレートのPcmImData を追加する
		/// </summary>
		private void M6528ToM6295_CheckSamplingRate()
		{
			var addBlockIdBase = m_imData.PcmImDataList.Count(x=>x.m_chipSelect == m_targetChip.ChipSelect);
			var beforePcmImData = m_imData.GetPcmImData(m_targetChip.ChipSelect, 0, (addBlockIdBase-1));
			var addPcmStart = beforePcmImData.m_pcmStart + beforePcmImData.m_pcmSize;

			//	PlayImDataから、VSTRM_START_SIZE_FASTだけを拾い出す
			foreach(var playImData in m_imData.PlayImDataList.Where(x => x.m_chipSelect == m_targetChip.ChipSelect && x.m_imType == F1ImData.PlayImType.VSTRM_START_SIZE_FAST))
			{
				// VSTRM_START_SIZE_FASTのPlayDataからPcmImDataを取り出す
				var pcmImData = m_imData.GetPcmImData(m_targetChip.ChipSelect, 0, playImData.m_vStrmBlockId);

				//	PcmImDataに、まだサンプリングレートが設定されていない場合
				if (pcmImData.m_pcmSamplingRate < 0)
				{	//	PlayImDataのサンプリングレートを設定する
					pcmImData.m_pcmSamplingRate = playImData.m_vStrmSamplingRate;
				}
				//	設定されているPcmImDataのサンプリングレートが、ImPlayData の値と異なる場合
				else if (pcmImData.m_pcmSamplingRate != playImData.m_vStrmSamplingRate)
				{
					byte[] addPcmDataBuff = new byte[pcmImData.m_pcmSize];
					for (int i = 0; i < pcmImData.m_pcmSize; i ++) 
					{
						addPcmDataBuff[i] = pcmImData.m_pcmBinaryArray[i];
					}
					var addSamplingRate = playImData.m_vStrmSamplingRate;
					m_imData.AddSamplingRatePcmImDataList(m_targetChip.ChipSelect, pcmImData.m_pcmDataType, addPcmStart, pcmImData.m_pcmSize, addSamplingRate, addPcmDataBuff);
					foreach(var otPlayImData in m_imData.PlayImDataList.Where(x => x.m_chipSelect == m_targetChip.ChipSelect && x.m_imType == F1ImData.PlayImType.VSTRM_START_SIZE_FAST && x.m_vStrmSamplingRate == addSamplingRate))
					{
						otPlayImData.m_vStrmBlockId = addBlockIdBase;
					}
					addBlockIdBase += 1;
					addPcmStart += pcmImData.m_pcmSize;
				}
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
		///	M6258 から M6295 変換	PCM データの変換
		/// </summary>
		private enum ResamplerMode
		{
			Linear,
			SincLanczos,
			BlackmanWindow,
		//	Catmull-Rom,
		}
		private readonly double LOW_PASS_FILTER_ALPHA = 0.68;

		private void M6528ToM6295_PcmDataConvert()
		{
			int pcmStart = 0;
			ResamplerMode  resampleMode = ResamplerMode.Linear;
//			ResamplerMode  resampleMode = ResamplerMode.SincLanczos;
//			ResamplerMode  resampleMode = ResamplerMode.BlackmanWindow;

			foreach(var pcmImData in m_imData.PcmImDataList.Where(x => x.m_chipSelect == m_targetChip.ChipSelect && x.m_pcmDataType == PcmDataType.PcmData0))
			{
				double targetSamplingRate = 1000000.0 / 132.0;	// 約7575.76Hz
				var sourceSamplingRate = (double)pcmImData.m_pcmSamplingRate;
				var decodedDataList = new List<int>();
				var filteredDataList = new List<int>();
				var resampleDataList = new List<int>();
				var encodedDataList = new List<byte>();

				OkiPcm.M6258Decode(pcmImData.m_pcmBinaryArray.ToList(), decodedDataList);
				switch(resampleMode)
				{
					case ResamplerMode.Linear:
						filteredDataList = LowPassFilter(decodedDataList, LOW_PASS_FILTER_ALPHA);
						resampleDataList = ResampleLinear(filteredDataList, (sourceSamplingRate*2), targetSamplingRate);
						break;
					case ResamplerMode.SincLanczos:
						filteredDataList = LowPassFilter(decodedDataList, LOW_PASS_FILTER_ALPHA);
						resampleDataList = ResampleLanczos(filteredDataList, (sourceSamplingRate*2), targetSamplingRate);
						break;
					case ResamplerMode.BlackmanWindow:
						resampleDataList = ResampleBlackmanWindow(decodedDataList, (sourceSamplingRate*2), targetSamplingRate);
						break;
				}
				OkiPcm.M6295Encode(resampleDataList, encodedDataList);
				pcmImData.m_pcmBinaryArray = encodedDataList.ToArray();
				pcmImData.m_pcmStart = pcmStart;
				pcmImData.m_pcmSize = pcmImData.m_pcmBinaryArray.Length;
				pcmStart += pcmImData.m_pcmSize;
			}
		}

		private List<int> LowPassFilter(List<int> input, double alpha = 0.25)
		{
			var output = new List<int>(input.Count);
			int prev = input[0];
			output.Add(prev);
			for (int i = 1; i < input.Count; i++)
			{
				int v = (int)(prev + alpha * (input[i] - prev));
				output.Add(v);
				prev = v;
			}
			return output;
		}

		private List<int> ResampleLinear(List<int> input, double srcRate, double dstRate)
		{
			double ratio = dstRate / srcRate;
			int newLength = (int)(input.Count * ratio);
			var output = new List<int>();
			for (int i = 0; i < newLength; i++)
			{
				double pos = i / ratio;
				int idx = (int)pos;
				double t = pos - idx;
				int s0 = input[idx];
				int s1 = input[Math.Min(idx + 1, input.Count - 1)];
				int v = (int)(s0 + (s1 - s0) * t);
				output.Add(v);
			}
			return output;
		}

		private double LanczosKernel(double x, int a)
		{
			if (x == 0.0) return 1.0;
			if (x <= -a || x >= a) return 0.0;
			double pix = Math.PI * x;
			return (Math.Sin(pix) / pix) * (Math.Sin(pix / a) / (pix / a));
		}

		private List<int> ResampleLanczos(List<int> input, double srcRate, double dstRate, int a = 2)
		{
			double ratio = dstRate / srcRate;
			int newLength = (int)(input.Count * ratio);
			var output = new List<int>();

			for (int i = 0; i < newLength; i++)
			{
				double pos = i / ratio;
				int center = (int)pos;
				double sum = 0.0;
				double wsum = 0.0;
				// Lanczos の範囲は [-a, +a]
				for (int k = -a; k <= a; k++)
				{
					int idx = center + k;
					if (idx < 0 || idx >= input.Count)
					{
						continue;
					}
					double w = LanczosKernel(pos - idx, a);
					sum += input[idx] * w;
					wsum += w;
				}
				sum = (wsum != 0) ? sum / wsum : sum;
				int v = (int)Math.Round(sum);
//				v = Math.Clamp(v, -32768, 32767);
				v = (v < -32768) ? -32768 : (v > 32767) ? 32767 : v;
				output.Add(v);
			}
			return output;
		}

		private List<int> ResampleBlackmanWindow(List<int> input, double sourceFreq, double targetFreq)
		{
			var output = new List<int>();
			// 比率：出力1サンプルあたりの入力サンプル進捗量
			double step = sourceFreq / targetFreq;
			int dstLen = (int)(input.Count * (targetFreq / sourceFreq));
			// フィルタの設定	窓の広さ
			int kernelWidth = 32; 
			// ダウンサンプリング時は折り返し防止のため帯域を制限
			double cutoff = (sourceFreq > targetFreq) ? (targetFreq / sourceFreq) * 0.95 : 0.95;

			for (int i = 0; i < dstLen; i++)
			{
				double center = i * step;
				int start = (int)Math.Floor(center - kernelWidth);
				int end = (int)Math.Ceiling(center + kernelWidth);
				double sum = 0;
				double weightSum = 0;
				for (int j = start; j <= end; j++)
				{
					if (j < 0 || j >= input.Count) continue;
					// Sinc関数とBlackman窓による重み付け
					double x = (center - j) * Math.PI * cutoff;
					double sinc = (Math.Abs(x) < 1e-9) ? 1.0 : Math.Sin(x) / x;
					double t = (double)(j - center) / kernelWidth;
					double window = 0.42 + 0.5 * Math.Cos(Math.PI * t) + 0.08 * Math.Cos(2 * Math.PI * t);
					double weight = sinc * window;
					sum += input[j] * weight;
					weightSum += weight;
				}
				if (weightSum > 0)
				{
					output.Add((int)Math.Round(sum / weightSum));
				}
				else
				{
					output.Add(0);
				}
			}
			return output;
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

	}
}