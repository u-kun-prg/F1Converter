using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace F1
{
	public static class OkiPcm
	{
		private static readonly int[] StepArray = 
		{	//	00H - 30H
			  16,  17,  19,  21,  23,  25,  28,  31,  34,  37,  41,  45,  50,  55,  60,  66,		//	00H - 0FH
			  73,  80,  88,  97, 107, 118, 130, 143, 157, 173, 190, 209, 230, 253, 279, 307,		//	10H - 1FH
			 337, 371, 408, 449, 494, 544, 598, 658, 724, 796, 876, 963,1060,1166,1282,1411,1552	//	20H - 30H
		};
		private static readonly sbyte[] AdjustArray =
		{
			 -1,  -1,  -1,  -1,  2,   4,   6,   8
		};

		private static int OkiStep(int step, ref int history, ref int stepHist)
		{
			var stepSize = StepArray[stepHist];
			var delta = stepSize >> 3;
			if ((step & 0x01) != 0) delta += (stepSize >> 2);
			if ((step & 0x02) != 0) delta += (stepSize >> 1);
			if ((step & 0x04) != 0) delta += stepSize;
			if ((step & 0x08) != 0) delta = (delta * -1);
//			var outData = (history + delta);
			var outData = ((delta << 8) + (history * 245)) >> 8;

			outData = (outData < -2048) ? -2048 : ((outData > 2047) ? 2047 : outData);
			history = outData;
			var adjustedStep = (stepHist + AdjustArray[step & 0x07]);
			stepHist = (adjustedStep < 0) ? 0 : ((adjustedStep > 48) ? 48 : adjustedStep);
			return outData;
		}
		public static void M6258Decode(List<byte> sourceDataList, List<int>decodedDataList)
		{
			int counter = 0;
			int index = 0;
			int history = 0;
			int stepHistory = 0;
			while(counter != (sourceDataList.Count * 2))
			{
				var step = (int)sourceDataList[index];
				if ((counter & 0x0001) != 0)
				{
					step = step >> 4;
					index += 1;
				}
				step &= 0x0F;
				decodedDataList.Add( (OkiStep(step, ref history, ref stepHistory) << 4) );
				counter += 1;
			}
		}

		public static void M6295Encode(List<int> sourceDataList, List<byte>encodedDataList)
		{
			int history = 0;
			int stepHistory = 0;
			int bufferSample = 0;
			for (int sourceIndex = 0, l = sourceDataList.Count; sourceIndex < l; sourceIndex ++)
			{
				var sample = sourceDataList[sourceIndex];
				sample = (sample < 0x7FF8) ? (sample+0x08) : sample;
				sample = (sample >> 4);
				var stepSize = StepArray[stepHistory];
				var delta = sample - history;
				var adpcmSample = (delta < 0) ? 0b1000 : 0b0000;
				delta = Math.Abs(delta);
				for (int bit = 2; bit >= 0 ; bit--)
				{
					if (delta >= stepSize)
					{
						adpcmSample |= (1 << bit);
						delta -= stepSize;
					}
					stepSize >>= 1;
				}
				OkiStep(adpcmSample, ref history, ref stepHistory);

				if ((sourceIndex & 0x01) == 0)
				{
					bufferSample = ((adpcmSample << 4) & 0xF0);
				}
				else
				{
					bufferSample = bufferSample | (adpcmSample & 0x0F);
					encodedDataList.Add((byte)bufferSample);
				}
			}
		}
	}
}

