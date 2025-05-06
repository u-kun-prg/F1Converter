using System;
using System.Collections.Generic;

namespace F1
{
	/// <summary>
	///	F1 Header Class.
	/// </summary>
	public class F1Header
	{
		public static int F1_HeaderSize { get; }  =  0x20;
		public static int F1_CommandCodeSize {get; } = 0x14;

		private List<byte> m_headerList;

		private int IndexLoopCount		{ get; } = 0x03;
		private int IndexOneCycleNs 	{ get; } = 0x04;
		private int IndexPCMData		{ get; } = 0x08;
		private int IndexCommandAry		{ get; } = 0x0C;

		private int IndexCmdEndCode			{ get; } = 0x0C;
		private int IndexCmdChangeA1		{ get; } = 0x0D;
		private int IndexCmdChangeCS		{ get; } = 0x0E;
		private int IndexCmdLooPoint		{ get; } = 0x0F;
		private int IndexCmdCycleWaitByte	{ get; } = 0x10;
		private int IndexCmdCycleWaitWord	{ get; } = 0x11;
		private int IndexCmdCycle1Wait		{ get; } = 0x12;
		private int IndexCmdCycle2Wait		{ get; } = 0x13;
		private int IndexCmdCycle3Wait		{ get; } = 0x14;
		private int IndexCmdCycle4Wait		{ get; } = 0x15;
		private int IndexCmdCycle5Wait		{ get; } = 0x16;
		private int IndexCmdCycle6Wait		{ get; } = 0x17;
		private int IndexCmdWriteWait		{ get; } = 0x18;
		private int IndexCmdWriteWaitRL		{ get; } = 0x19;
		private int IndexCmdWriteSeek		{ get; } = 0x1A;
		private int IndexCmdFree0			{ get; } = 0x1B;
		private int IndexCmdFree1			{ get; } = 0x1C;
		private int IndexCmdFree2			{ get; } = 0x1D;
		private int IndexCmdFree3			{ get; } = 0x1E;
		private int IndexCmdFree4			{ get; } = 0x1F;

		private static readonly byte[] initialHeaderData =
		{
			0x46, 0x31,				//	00H	"F1"
			0x10,					//	02H	Version.
			0x03,					//	03H	LoopCount.
			0x00,0x00,0x00,0x00,	//	04H	One Cycle[ns]
			0xFF,0xFF,0xFF,0xFF,	//	08H	PCM Data Offset.
			0x00,					//	0CH	Code EndAndLoop.		//	0
			0x00,					//	0DH	Code ChangeA1.			//	1
			0x00,					//	0EH	Code ChipSelect.		//	2
			0x00,					//	0FH	Code Loop.				//	3
			0x00,					//	10H	Code CycleWait1Byte.	//	4
			0x00,					//	11H	Code CycleWait2Byte.	//	5
			0x00,					//	12H	Code CycleWait1.		//	6
			0x00,					//	13H	Code CycleWait2.		//	7
			0x00,					//	14H	Code CycleWait3.		//	8
			0x00,					//	15H	Code CycleWait4.		//	9
			0x00,					//	16H	Code CycleWait5.		//	A
			0x00,					//	17H	Code CycleWait6.		//	B
			0x00,					//	18H	Code WriteWait.			//	C
			0x00,					//	19H	Code WriteWaitRL,		//	F
			0x00,					//	1AH	Code WriteSeek.			//	E
			0x00,					//	1BH	Code Free,				//	F
			0x00,					//	1CH	Code Free,				//	F
			0x00,					//	1DH	Code Free,				//	F
			0x00,					//	1EH	Code Free,				//	F
			0x00,					//	1FH	Code Free,				//	F
		};

		private float m_tempoRate = 100f;

		public F1Header(int tempo)
		{
			m_tempoRate = ((float)tempo) / 100f;
			m_headerList = new List<byte>();
			for(int i = 0, l = initialHeaderData.Length; i < l; i++)
			{
				m_headerList.Add(initialHeaderData[i]);
			}
		}

		public string GetIDString()
		{
			return  $"{(char)GetByte(0)}{(char)GetByte(1)}";
		}

		public string GetVersionString()
		{
			var ver = (int)GetByte(2);
			var major = (ver >> 4) & 0x0F;
			var minor = ver & 0x0F;
			return  $"{major}.{minor}";
		}

		public void SetVersion(uint version)
		{
			m_headerList[IndexLoopCount] = (byte)version;
		}

		public byte GetLoopCount()
		{
			return GetByte(IndexLoopCount);
		}
		public void SetLoopCount(uint loopCount)
		{
			m_headerList[IndexLoopCount] = (byte)loopCount;
		}

		public uint GetOneCycleNs()
		{
			uint d0;
			uint d1;
			uint d2;
			uint d3;
			d0 = (uint)GetByte(IndexOneCycleNs);
			d1 = (uint)GetByte(IndexOneCycleNs+1);
			d2 = (uint)GetByte(IndexOneCycleNs+2);
			d3 = (uint)GetByte(IndexOneCycleNs+3);
			return ((d0 << 24) & 0xFF000000) + ((d1 <<16) & 0xFF0000) + ((d2 << 8) & 0xFF00) + (d3 & 0xFF); ;
		}
		public void SetOneCycleNs(uint oneCycleNs)
		{
			uint tempo = (uint)((float)(oneCycleNs) * m_tempoRate);
			SetBigEndian32Bit(tempo, IndexOneCycleNs);
		}

		public byte GetCmdCodeEnd()
		{
			return GetByte(IndexCmdEndCode);
		}
		public void SetCmdCodeEnd(uint cmdEndCode)
		{
			m_headerList[IndexCmdEndCode] = (byte)cmdEndCode;
		}

		public byte GetCmdCodeA1()
		{
			return GetByte(IndexCmdChangeA1);
		}
		public void SetCmdCodeA1(uint cmdCodeA1)
		{
			m_headerList[IndexCmdChangeA1] = (byte)cmdCodeA1;
		}

		public byte GetCmdCodeCS()
		{
			return GetByte(IndexCmdChangeCS);
		}
		public void SetCmdCodeCS(uint cmdCodeCS)
		{
			m_headerList[IndexCmdChangeCS] = (byte)cmdCodeCS;
		}

		public byte GetCmdCodeLoop()
		{
			return GetByte(IndexCmdLooPoint);
		}
		public void SetCmdCodeLoop(uint cmdCodeLoop)
		{
			m_headerList[IndexCmdLooPoint] = (byte)cmdCodeLoop;
		}

		public byte GetCmdCodeCycleWaitByte()
		{
			return GetByte(IndexCmdCycleWaitByte);
		}
		public void SetCmdCodeCycleWaitByte(uint cmdCodeWaitByte)
		{
			m_headerList[IndexCmdCycleWaitByte] = (byte)cmdCodeWaitByte;
		}

		public byte GetCmdCodeCycleWaitWord()
		{
			return GetByte(IndexCmdCycleWaitWord);
		}
		public void SetCmdCodeCycleWaitWord(uint cmdCodeWaitWord)
		{
			m_headerList[IndexCmdCycleWaitWord] = (byte)cmdCodeWaitWord;
		}

		public byte GetCmdCodeCycleNWait(int wait)
		{
			switch(wait)
			{
				case 1:
					return GetCmdCodeCycle1Wait();
				case 2:
					return GetCmdCodeCycle2Wait();
				case 3:
					return GetCmdCodeCycle3Wait();
				case 4:
					return GetCmdCodeCycle4Wait();
				case 5:
					return GetCmdCodeCycle5Wait();
				case 6:
					return GetCmdCodeCycle6Wait();
			}
			return GetByte(IndexCmdCycle1Wait);
		}
		public byte GetCmdCodeCycle1Wait()
		{
			return GetByte(IndexCmdCycle1Wait);
		}
		public void SetCmdCode1Wait(uint cmdCode1Wait)
		{
			m_headerList[IndexCmdCycle1Wait] = (byte)cmdCode1Wait;
		}

		public byte GetCmdCodeCycle2Wait()
		{
			return GetByte(IndexCmdCycle2Wait);
		}
		public void SetCmdCode2Wait(uint cmdCode2Wait)
		{
			m_headerList[IndexCmdCycle2Wait] = (byte)cmdCode2Wait;
		}

		public byte GetCmdCodeCycle3Wait()
		{
			return GetByte(IndexCmdCycle3Wait);
		}
		public void SetCmdCode3Wait(uint cmdCode3Wait)
		{
			m_headerList[IndexCmdCycle3Wait] = (byte)cmdCode3Wait;
		}

		public byte GetCmdCodeCycle4Wait()
		{
			return GetByte(IndexCmdCycle4Wait);
		}
		public void SetCmdCode4Wait(uint cmdCode4Wait)
		{
			m_headerList[IndexCmdCycle4Wait] = (byte)cmdCode4Wait;
		}

		public byte GetCmdCodeCycle5Wait()
		{
			return GetByte(IndexCmdCycle5Wait);
		}
		public void SetCmdCode5Wait(uint cmdCode5Wait)
		{
			m_headerList[IndexCmdCycle5Wait] = (byte)cmdCode5Wait;
		}

		public byte GetCmdCodeCycle6Wait()
		{
			return GetByte(IndexCmdCycle6Wait);
		}
		public void SetCmdCode6Wait(uint cmdCode6Wait)
		{
			m_headerList[IndexCmdCycle6Wait] = (byte)cmdCode6Wait;
		}

		public byte GetCmdCodeWriteWait()
		{
			return GetByte(IndexCmdWriteWait);
		}
		public void SetCmdCodeWriteWait(uint cmdWriteWait)
		{
			m_headerList[IndexCmdWriteWait] = (byte)cmdWriteWait;
		}

		public byte GetCmdCodeWriteWaitRunLength()
		{
			return GetByte(IndexCmdWriteWaitRL);
		}
		public void SetCmdCodeWriteWaitRunLength(uint cmdWriteWaitRL)
		{
			m_headerList[IndexCmdWriteWaitRL] = (byte)cmdWriteWaitRL;
		}

		public byte GetCmdCodeWriteSeek()
		{
			return GetByte(IndexCmdWriteSeek);
		}
		public void SetCmdCodeWriteSeek(uint cmdWaitSeek)
		{
			m_headerList[IndexCmdWriteSeek] = (byte)cmdWaitSeek;
		}

		public byte GetCmdCodeFree0()
		{
			return GetByte(IndexCmdFree0);
		}
		public void SetCmdCodeFree0(uint cmdCodeFree0)
		{
			m_headerList[IndexCmdFree0] = (byte)cmdCodeFree0;
		}

		public byte GetCmdCodeFree1()
		{
			return GetByte(IndexCmdFree1);
		}
		public void SetCmdCodeFree1(uint cmdCodeFree1)
		{
			m_headerList[IndexCmdFree1] = (byte)cmdCodeFree1;
		}

		public byte GetCmdCodeFree2()
		{
			return GetByte(IndexCmdFree2);
		}
		public void SetCmdCodeFree2(uint cmdCodeFree2)
		{
			m_headerList[IndexCmdFree2] = (byte)cmdCodeFree2;
		}

		public byte GetCmdCodeFree3()
		{
			return GetByte(IndexCmdFree3);
		}
		public void SetCmdCodeFree3(uint cmdCodeFree3)
		{
			m_headerList[IndexCmdFree3] = (byte)cmdCodeFree3;
		}

		public byte GetCmdCodeFree4()
		{
			return GetByte(IndexCmdFree4);
		}
		public void SetCmdCodeFree4(uint cmdCodeFree4)
		{
			m_headerList[IndexCmdFree4] = (byte)cmdCodeFree4;
		}

		public void SetPCMDataOffset(uint pcmDataOffset)
		{
			SetBigEndian32Bit(pcmDataOffset, IndexPCMData);
		}

		public void SetCommandCodes(byte[] codes)
		{
			int index = IndexCommandAry;
			for (int i = 0, l = codes.Length; i < l ; i++ )
			{
				m_headerList[index] = codes[i];
				index +=1;
			}
		}

		public void WriteHeader(List<byte> f1DataList)
		{
			for (int i = 0, l = m_headerList.Count; i < l; i ++)
			{
				f1DataList.Add(m_headerList[i]);
			}
		}

		private void SetBigEndian32Bit(uint data, int offset)
		{
			m_headerList[offset+0] = (byte)((data >> 24) & 0xFF);
			m_headerList[offset+1] = (byte)((data >> 16) & 0xFF);
			m_headerList[offset+2] = (byte)((data >>  8) & 0xFF);
			m_headerList[offset+3] = (byte)(data & 0xFF);
		}

		public byte GetByte(int index)
		{
			return  m_headerList[index];
		}

		public uint GetDataText(int index, DataSize dataSize, bool isHex, out string dataStr)
		{
			uint d0,d1,d2,d3;
			dataStr = "";
			uint res = 0;
			switch(dataSize)
			{
				case DataSize.DB:
					res = (uint)GetByte(index);
					break;
				case DataSize.DW:
					d0 = (uint)GetByte(index);
					d1 = (uint)GetByte(index+1);
 					res = ((d0 << 8) & 0xFF00) + (d1 & 0xFF00);
					break;
				case DataSize.DL:
					d0 = (uint)GetByte(index);
					d1 = (uint)GetByte(index+1);
					d2 = (uint)GetByte(index+2);
					d3 = (uint)GetByte(index+3);
 					res = ((d0 << 24) & 0xFF000000) + ((d1 <<16) & 0xFF0000) + ((d2 << 8) & 0xFF00) + (d3 & 0xFF);
					break;
			}
			dataStr = isHex ? $"0x{res:X2}" : $"{res}";
			return res;
		}

		public uint GetDataTextDump(int index, DataSize dataSize, out string byteString)
		{
			uint d0,d1,d2,d3;
			byteString = "";
			switch(dataSize)
			{
				case DataSize.DB:
					d0 = (uint)GetByte(index);
					byteString = $"{d0:X2}         ";
					return d0;
				case DataSize.DW:
					d0 = (uint)GetByte(index);
					d1 = (uint)GetByte(index+1);
					byteString = $"{d0:X2} {d1:X2}      ";
 					return ((d0 << 8) & 0xFF00) + (d1 & 0xFF00);
				case DataSize.DL:
					d0 = (uint)GetByte(index);
					d1 = (uint)GetByte(index+1);
					d2 = (uint)GetByte(index+2);
					d3 = (uint)GetByte(index+3);
					byteString = $"{d0:X2} {d1:X2} {d2:X2} {d3:X2}";
 					return ((d0 << 24) & 0xFF000000) + ((d1 <<16) & 0xFF0000) + ((d2 << 8) & 0xFF00) + (d3 & 0xFF);
			}
			return (uint)0;
		}

	}
}
