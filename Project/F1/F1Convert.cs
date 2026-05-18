using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace F1
{
	public class F1Convert
	{
		private System.Windows.Forms.TextBox m_infoTextBox;

		/// <summary>
		///	F1 Converter	変換処理のメイン
		/// </summary>
		public bool F1Converter(
								string sourceFileName, 
								List<byte> f1DataList, 
								List<string> textDataList, 
								List<string> dumpDataList, 
								List<string> f1tDataList, 
								List<byte> S98DataList, 
								string targetName,
								byte[] commandArray,
								List<string> chipNameList,
								List<int> chipClockList,
								List<int> targetTopCodeList,
								bool isToneAdjust, 
								bool isShrink, 
								bool isDual2nd, 
								bool isTimerReg,
								bool isUsePcm,
								bool isYM2612DacRL,
								int	 loopNum,
								int	 tempo,
								int  fmVolDown, 
								int  ssgVolAdd, 
								bool isOutputText,
								bool isOutputDump,
								bool isOutputF1T,
								bool isOutputS98,
								bool isNoOutputPcmBlock,
								System.Windows.Forms.TextBox info_text_box,
								out bool isOutputtedF1,
								out bool isOutputtedF1T,
								out bool isOutputtedS98)
		{
			isOutputtedF1 = false;
			isOutputtedF1T = false;
			isOutputtedS98 = false;
			//	ログ表示用のテキスト Box を保持
			m_infoTextBox = info_text_box;
			//	ターゲットの情報を生成する
			var chipTypeList = new List<ChipType>();
			for (int i=0, l=chipNameList.Count; i<l; i++)
			{
				var chipType = ChipType.NONE;
				Enum.TryParse(chipNameList[i], out chipType);
				chipTypeList.Add(chipType);
			}
			F1TargetHardware targetHardware = new F1TargetHardware(targetName, chipTypeList, chipClockList, isUsePcm);
			//	F1 Header を生成
			F1Header header = new F1Header(tempo);
			header.SetCommandCodes(commandArray);
			header.SetLoopCount((uint)loopNum);

			//	F1 中間データを生成
			F1ImData imData = new F1ImData(isYM2612DacRL, targetTopCodeList, isToneAdjust, isShrink, isDual2nd, isTimerReg, fmVolDown, ssgVolAdd);

			//	ソースファイルを中間データにパースする
			SourceFormat sourceFormat = ParseSouceFileToImData(targetHardware, header, imData, sourceFileName, f1DataList);

			switch(sourceFormat)
			{
				case SourceFormat.NONE:
					return false;
				case SourceFormat.F1:
					isOutputtedF1 = true;
					break;
				case SourceFormat.F1T:
					isOutputtedF1T = true;
					break;
				default:
					break;
			}

			if (!isOutputtedF1)
			{
				imData.CleanupPlayImDataList();
				//	ターゲット CHIP ごとの変換制御
				if (sourceFormat != SourceFormat.F1T)
				{
					foreach(var targetChip in targetHardware.TargetChipList) 
					{
						SoundChip soundChip = null;
						switch(targetChip.TargetChipType)
						{
							case ChipType.YM2151:
								soundChip = new Chip_OPM();
								break;
							case ChipType.YM2203:
							case ChipType.YM2608:
							case ChipType.YM2612:
							case ChipType.YM2610:
							case ChipType.YM2610B:
							case ChipType.YMF288:
								soundChip = new Chip_OPN();
								break;
							case ChipType.YM3526:
							case ChipType.YM3812:
							case ChipType.Y8950:
							case ChipType.YMF262:
							case ChipType.YM2413:
								soundChip = new Chip_OPL();
								break;
							case ChipType.SN76489:
								soundChip = new Chip_DCSG();
								break;
							case ChipType.AY_3_8910:
							case ChipType.YM2149:
								soundChip = new Chip_PSG();
								break;
							case ChipType.K051649:
							case ChipType.K052539:
								soundChip = new Chip_SCC();
								break;
							case ChipType.M6258:
								soundChip = new Chip_M6258();
								break;
							case ChipType.M6295:
								soundChip = new Chip_M6295();
								break;
							case ChipType.K053260:
								soundChip = new Chip_K053260();
								break;
						}
						if (soundChip == null)
						{
							return false;
						}
						soundChip.Initialize(targetHardware, targetChip, imData, header.GetOneCycleNs());
						soundChip.ControlPlayChipRegiser();
						soundChip.ShrinkPlayChipRegiser();
						soundChip.ToneConvert();
						soundChip.VolumeConvert();
					}
				}
				imData.CleanupPlayImDataList();
				{
					var exportF1 = new F1Export();
					exportF1.CreateF1(f1DataList, header, imData, (uint)loopNum, isNoOutputPcmBlock);
				}
				if (isOutputText)
				{
					var exportText = new F1ExportText();
					exportText.CreateText(textDataList, header, imData, isNoOutputPcmBlock);
				}
				if (isOutputF1T && !isOutputtedF1T)
				{
					var exportF1T = new F1ExportF1T();
					exportF1T.CreateF1T(f1tDataList, header, imData, isNoOutputPcmBlock);
				}
				if (isOutputS98)
				{
					if (sourceFormat != SourceFormat.F1T && sourceFormat != SourceFormat.S98)
					{
						var exportS98 = new F1ExportS98();
						isOutputtedS98 = exportS98.CreateS98(S98DataList, targetHardware, header, imData);
					}
				}
			}
			if (isOutputDump)
			{
				var exportDump = new F1ExportDump();
				exportDump.CreateDump(dumpDataList, f1DataList);
			}
			return true;
		}

		/// <summary>
		///	ソースファイルを中間データにパースする
		/// </summary>
		private SourceFormat ParseSouceFileToImData(F1TargetHardware targetHardware, F1Header header, F1ImData imData, string  sourceFileName, List<byte>f1DataList)
		{
			//	Souece Binary Read
			byte[] binaryArray;
			string[] textArray;
			SourceFormat sourceFormat = ReadSourceFile(sourceFileName, out binaryArray, out textArray);
			Parser parser;
			bool isOffTopCode = false;
			switch(sourceFormat)
			{
				case SourceFormat.F1:
					//	F1 Format is expanded to the converted buffer and finished.
					DisplayTextBox("Source File is F1-File.");
					for (int i = 0, l = binaryArray.Length; i < l; i++)
					{
						f1DataList.Add(binaryArray[i]);
					}
					return sourceFormat;

				case SourceFormat.F1T:
					parser = new F1TParser();
					isOffTopCode = true;
					break;

				case SourceFormat.S98:
					parser = new S98Parser();
					break;

				case SourceFormat.VGM:
					parser = new VgmParser();
					break;

				case SourceFormat.MDX:
					parser = new MdxParser();
					break;

				default:
					DisplayError("Unknown sorce file format.");
					return SourceFormat.NONE;
			}

			//	Parse Source.
			parser.Initialize(sourceFileName, targetHardware, header, imData, isOffTopCode, binaryArray, textArray);
			if (!parser.Parse())
			{
				//	Parse Error.
				DisplayError(parser.ErrorString);
				return SourceFormat.NONE;
			}
			else
			{
				//	Parse Warning.
				if (parser.WarningStrings.Count !=0)
				{
					foreach(var warningString in parser.WarningStrings)
					{
						DisplayWarning(warningString);
					}
				}
				DisplayTextBox(parser.SuccessString);
			}
			return sourceFormat;
		}

		/// <summary>
		/// エラーと警告の表示
		/// </summary>
		public void DisplayError(string str)
		{
			m_infoTextBox.AppendText($"ERROR : {str}\r\n");
		}
		public void DisplayWarning(string str)
		{
			m_infoTextBox.AppendText($"WARNING : {str}\r\n");
		}
		public void DisplayTextBox(string str)
		{
			m_infoTextBox.AppendText($"{str}\r\n");
		}

		/// <summary>
		///	ソースファイルの読み込み
		/// </summary>
		private SourceFormat ReadSourceFile(string fileName, out byte[] sourceBinary, out string[] sourceText)
		{
			SourceFormat resultFormat = SourceFormat.NONE;
			if (System.IO.Path.GetExtension(fileName).ToLower() == ".f1t")
			{
				sourceBinary = new byte[0];
				StreamReader sr = new StreamReader(fileName);

				string line = null;
				var lines = new List<string>();
				while((line = sr.ReadLine()) != null)
				{
					lines.Add(line);
				}
				sourceText = lines.ToArray();
				sr.Close();
				return SourceFormat.F1T;
			}
			sourceText = new string[0];
			FileStream rdStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
			GZipStream gzStream = new GZipStream(rdStream, CompressionMode.Decompress);
			int size = 0;
			try
			{
				var tmpArray = new byte[1024];
				var tmpList  = new List<byte>();
				while((size = gzStream.Read(tmpArray, 0, tmpArray.Length)) > 0) 
				{
					for (int i = 0; i < size; i++) 
					{
						tmpList.Add(tmpArray[i]);
					}
				}
				sourceBinary = tmpList.ToArray();
			}
			catch(Exception)
			{
				rdStream.Seek(0, SeekOrigin.Begin);
				size = (int)rdStream.Length;
				sourceBinary = new byte[size];
				rdStream.Read(sourceBinary, 0, (int)size);
			}
	        rdStream.Close();
	        gzStream.Close();

			if (sourceBinary.Length > 0x10)
			{
				if (System.IO.Path.GetExtension(fileName).ToLower() == ".mdx")
				{
					resultFormat = SourceFormat.MDX;
				}
				else
				{		//	"s98"	or 	"S98"
					if ((sourceBinary[0] == 0x73 || sourceBinary[0] == 0x53) && (sourceBinary[1] == 0x39) && (sourceBinary[2] == 0x38))
					{
						resultFormat = SourceFormat.S98;
					}	//	"Vgm"
					else if ((sourceBinary[0] == 0x56 && sourceBinary[1] == 0x67 && sourceBinary[2] == 0x6D))
					{
						resultFormat = SourceFormat.VGM;
					}	//	"F1"	or 	"f1"
					else if ((sourceBinary[0] == 0x46 || sourceBinary[0] == 0x66) && (sourceBinary[1] == 0x31))
					{
						resultFormat = SourceFormat.F1;
					}
				}
			}
			return resultFormat;
		}

	}
}
