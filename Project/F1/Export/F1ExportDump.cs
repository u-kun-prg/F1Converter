using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;

namespace F1
{
	/// <summary>
	/// F1 Dump  エクスポート クラス
	/// </summary>
	public class F1ExportDump
	{
		/// <summary>
		/// F1 Dump List の生成
		/// </summary>
		public void CreateDump(List<string> textDataList, List<byte> f1DataList)
		{
			int ix = 0;
			var size = f1DataList.Count / 16;
			var modSize = f1DataList.Count & 0x0F;
			for (int i=0; i < size; i++)
			{
				textDataList.Add($"\t0x{f1DataList[ix+0x00]:X2}, 0x{f1DataList[ix+0x01]:X2}, 0x{f1DataList[ix+0x02]:X2}, 0x{f1DataList[ix+0x03]:X2}, 0x{f1DataList[ix+0x04]:X2}, 0x{f1DataList[ix+0x05]:X2}, 0x{f1DataList[ix+0x06]:X2}, 0x{f1DataList[ix+0x07]:X2}, 0x{f1DataList[ix+0x08]:X2}, 0x{f1DataList[ix+0x09]:X2}, 0x{f1DataList[ix+0x0A]:X2}, 0x{f1DataList[ix+0x0B]:X2}, 0x{f1DataList[ix+0x0C]:X2}, 0x{f1DataList[ix+0x0D]:X2}, 0x{f1DataList[ix+0x0E]:X2}, 0x{f1DataList[ix+0x0F]:X2}\t//\t{ix:X8}");
				ix += 16;
			}
			if (modSize != 0)
			{
				StringBuilder sb = new StringBuilder("");
				for (int j=0; j < modSize; j++)
				{
					sb.Append($"0x{f1DataList[ix]:X2}, ");
					ix += 1;
				}
				textDataList.Add(sb.ToString());
			}
		}
	}
}
