using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace F1Converter
{
	public partial class Protcol
	{
		//
		//	STK500v2
		//
		//	[ STK message constants ]
		private const byte STK500V2_MESSAGE_START = 0x1B;     //= ESC = 27 decimal
		private const byte STK500V2_TOKEN = 0x0E;
		//	[ STK general command constants ]
		private const byte STK500V2_CMD_SIGN_ON = 0x01;
		private const byte STK500V2_CMD_LOAD_ADDRESS = 0x06;
		//	[ STK ISP command constants ]
		private const byte STK500V2_CMD_ENTER_PROGMODE_ISP = 0x10;
		private const byte STK500V2_CMD_LEAVE_PROGMODE_ISP = 0x11;
		private const byte STK500V2_CMD_PROGRAM_FLASH_ISP = 0x13;
		private const byte STK500V2_CMD_READ_FLASH_ISP = 0x14;
		private const byte STK500V2_CMD_SPI_MULTI = 0x1D;
		//	[ STK answer constants ]
		private const byte STK500V2_ANSWER_CKSUM_ERROR = 0xB0;
		// Success
		private const byte STK500V2_STATUS_CMD_OK = 0x00;
		// Warnings
		private const byte STK500V2_STATUS_CMD_TOUT = 0x80;
		private const byte STK500V2_STATUS_RDY_BSY_TOUT = 0x81;
		private const byte STK500V2_STATUS_SET_PARAM_MISSING =  0x82;

		private byte[] stk500v2SendBuff = new byte[275 + 6];	 // max MESSAGE_BODY of 275 bytes, 6 bytes overhead
		private byte[] stk500v2RecvBuff = new byte[275 + 6];	 // max MESSAGE_BODY of 275 bytes, 6 bytes overhead
		private int stk500v2CmdSeqNum = 1;

		private bool Stk500v2_Cmd_SignOn(System.IO.Ports.SerialPort port)
		{
			byte[] buf = new byte[1];
			buf[0] =STK500V2_CMD_SIGN_ON; 
			stk500v2Send(port, buf, 1);
			int status = stk500v2Recv(port);
			if ((stk500v2RecvBuff[0] != STK500V2_CMD_SIGN_ON) || (stk500v2RecvBuff[1] != STK500V2_STATUS_CMD_OK) || (status != 0x0B))
			{
				return false;
			}
			if (!stk500v2CompareRecvBuffStr(3, "AVRISP_2"))
			{
				return false;
			}
			return true;
		}

		private bool Stk500v2_EnterProgramMode(System.IO.Ports.SerialPort port)
		{
			byte[] buf = new byte[12];
			buf[0] = STK500V2_CMD_ENTER_PROGMODE_ISP;
			buf[1] = 0xC8;	//	命令実行猶予時間[ms]
			buf[2] = 0x64;	//	ピン安定用待機時間[us]
			buf[3] = 0x19;	//	プログラミング動作移行命令実行での 接続待機時間[ms]
			buf[4] = 0x20;	//	同期化処理試行回数
			buf[5] = 0x00;	//	プログラミング動作移行命令実行での 各ﾊﾞｲﾄ間待機時間[ms]
			buf[6] = 0x53;	//	検査値 (AVR=$53,AT89xx=$69)
			buf[7] = 0x03;	//	検査値の受信バイト位置 (0=検査なし,3=AVR,4=AT89xx)
			buf[8] = 0xAC;	//	プログラミング許可命令第１バイト値
			buf[9] = 0x53;	//	プログラミング許可命令第２バイト値
			buf[10]= 0x00;	//	プログラミング許可命令第３バイト値
			buf[11]= 0x00;	//	プログラミング許可命令第４バイト値
			stk500v2Send(port, buf, 12);
			int status = stk500v2Recv(port);
			if ((stk500v2RecvBuff[0] != STK500V2_CMD_ENTER_PROGMODE_ISP) || (stk500v2RecvBuff[1] != STK500V2_STATUS_CMD_OK) || (status != 2))
			{
				return false;
			}

			buf[0] = STK500V2_CMD_SPI_MULTI;
			buf[1] = 0x04;	//	送信ﾊﾞｲﾄ数
			buf[2] = 0x04;	//	受信ﾊﾞｲﾄ数
			buf[3] = 0x00;	//	返し値の開始位置
			buf[4] = 0x30;
			buf[5] = 0x00;
			buf[6] = 0x00;
			buf[7] = 0x00;
			for (int i = 0; i < 3; i++)
			{
				stk500v2Send(port, buf, 8);
				status = stk500v2Recv(port);
				if ((stk500v2RecvBuff[0] != STK500V2_CMD_SPI_MULTI) || (stk500v2RecvBuff[1] != STK500V2_STATUS_CMD_OK) || (status != 7))
				{
					return false;
				}
				buf[6] = (byte)i;
			}
			return true;
		}

		private bool Stk500v2_Cmd_LoadAddress(System.IO.Ports.SerialPort port, int address)
		{
			byte[] buf = new byte[5];
			buf[0] = STK500V2_CMD_LOAD_ADDRESS;
			buf[1] = (byte)((address >> 25) & 0xff);
			buf[1] |= 0x80;
			buf[2] = (byte)((address >> 17) & 0xff);
			buf[3] = (byte)((address >>  9) & 0xff);
			buf[4] = (byte)((address >>  1) & 0xff);
			stk500v2Send(port, buf, 5);
			int status = stk500v2Recv(port);
			if ((stk500v2RecvBuff[0] != STK500V2_CMD_LOAD_ADDRESS) || (stk500v2RecvBuff[1] != STK500V2_STATUS_CMD_OK) || (status != 2))
			{
				return false;
			}
			return true;
		}

		private int Stk500v2_Cmd_WriteFlash(System.IO.Ports.SerialPort port, byte[] sourceBuff,int sourceIndex, int sendSize)
		{
			byte[] buf = new byte[0x10A];
			buf[0] = STK500V2_CMD_PROGRAM_FLASH_ISP;
			buf[1] = 0x01;
			buf[2] = 0x00;	
			buf[3] = 0xC1;	//	書き込み種別
			buf[4] = 0x0A;	//	遅延時間[ms]
			buf[5] = 0x40;	//	命令バイト値１
			buf[6] = 0x4C;	//	命令バイト値２
			buf[7] = 0x20;	//	命令バイト値３
			buf[8] = 0x00;
			buf[9] = 0x00;
			int d_index = 0x0A;
			int s_index = sourceIndex;
			for (int i = 0; i < sendSize; i++)
			{
				if (s_index >= sourceBuff.Length)
				{
					buf[d_index] = 0xFF;
				}
				else
				{
					buf[d_index] = sourceBuff[s_index];
					s_index += 1;
				}
				d_index += 1;
			}
			stk500v2Send(port, buf, 0x10A);
			int status = stk500v2Recv(port);
			if ((stk500v2RecvBuff[0] != STK500V2_CMD_PROGRAM_FLASH_ISP) || (stk500v2RecvBuff[1] != STK500V2_STATUS_CMD_OK) || (status != 2))
			{
				return -1;
			}
			return sendSize;
		}

		private bool Stk500v2_Cmd_ReadFlash(System.IO.Ports.SerialPort port)
		{
			byte[] buf = new byte[4];
			buf[0] = STK500V2_CMD_READ_FLASH_ISP;
			buf[1] = 0x01;
			buf[2] = 0x00;
			buf[3] = 0x20;	//	命令第１バイト値 (cmd1)
			stk500v2Send(port, buf, 4);
			int status = stk500v2Recv(port);
			if ((stk500v2RecvBuff[0] != STK500V2_CMD_READ_FLASH_ISP) || (stk500v2RecvBuff[1] != STK500V2_STATUS_CMD_OK) || (stk500v2RecvBuff[0x102] != STK500V2_STATUS_CMD_OK) || (status != 0x103))
			{
				return false;
			}
			return true;
		}

		private bool Stk500v2_ReadVeriy(byte[] sourceBuff, int sourceIndex)
		{
			for (int i = 0; i < 0x100; i++)
			{
				byte d = 0xFF;
				if (sourceIndex < sourceBuff.Length)
				{
					d = sourceBuff[sourceIndex];
					sourceIndex += 1;
				}
				if (d != stk500v2RecvBuff[i + 2])
				{
					return false;
				}
			}
			return true;
		}

		private bool Stk500v2_Cmd_LeaveProgramMode(System.IO.Ports.SerialPort port)
		{
			byte[] buf = new byte[3];
			buf[0] = STK500V2_CMD_LEAVE_PROGMODE_ISP;
			buf[1] = 0x01;	//	ピン安定用待機時間[ms]
			buf[2] = 0x01;	//	RESET=L	保持遅延時間[ms]
			stk500v2Send(port, buf, 3);
			int status = stk500v2Recv(port);
			if ((stk500v2RecvBuff[0] != STK500V2_CMD_LEAVE_PROGMODE_ISP) || (stk500v2RecvBuff[1] != STK500V2_STATUS_CMD_OK) || (status != 2))
			{
				return false;
			}
			return true;
		}

		//	STK500v2 送信
		private void stk500v2Send(System.IO.Ports.SerialPort port, byte[] data, int len)
		{
			stk500v2SendBuff[0] = STK500V2_MESSAGE_START;
			stk500v2SendBuff[1] = (byte) stk500v2CmdSeqNum;
			stk500v2SendBuff[2] = (byte) (len / 256);
			stk500v2SendBuff[3] = (byte) (len % 256);
			stk500v2SendBuff[4] = STK500V2_TOKEN;
			Array.Copy(data, 0, stk500v2SendBuff, 5, len);
			//	Calculate The XOR checksum.
			stk500v2SendBuff[5 + len] = 0;
			for (int i = 0; i < 5 + len; i++ )
			{
				stk500v2SendBuff[5 + len] ^= stk500v2SendBuff[i];
			}
			port.Write(stk500v2SendBuff, 0, len + 6);
		}

		//	STK500v2 受信
		private enum stk500v2RecvStateEnum
 		{
			INIT,
			START,
			SEQNUM,
			SIZE1,
			SIZE2,
			TOKEN,
			DATA,
			CSUM,
			DONE,
		}
		private int stk500v2Recv(System.IO.Ports.SerialPort port) 
		{
			stk500v2RecvStateEnum state = stk500v2RecvStateEnum.START;
			int msglen = 0;
			int curlen = 0;
			byte[] c = new byte[1];
			c[0] = 0;
			byte checksum = 0;

			while ((state != stk500v2RecvStateEnum.DONE)) 
			{
				if (!WaitToRead(port, 1))
				{
					return -1;
				}
				if (port.Read(c, 0, 1) != 1)
				{
					return -1;
				}

				checksum ^= c[0];
				switch (state)
				{
					case stk500v2RecvStateEnum.START:
						if (c[0] == STK500V2_MESSAGE_START)
						{
							checksum = STK500V2_MESSAGE_START;
							state = stk500v2RecvStateEnum.SEQNUM;
						}
						break;
					case stk500v2RecvStateEnum.SEQNUM:
						if (c[0] == stk500v2CmdSeqNum)
						{
							state = stk500v2RecvStateEnum.SIZE1;
							stk500v2CmdSeqNum ++;
							if (stk500v2CmdSeqNum == 0x100)
							{
								stk500v2CmdSeqNum = 1;
							}
						}
						 else
						{
							state = stk500v2RecvStateEnum.START;
						}
						break;
					case stk500v2RecvStateEnum.SIZE1:
						msglen = ((int) c[0]) * 256;
						state = stk500v2RecvStateEnum.SIZE2;
						break;
					case stk500v2RecvStateEnum.SIZE2:
						msglen += (int) c[0];
						state = stk500v2RecvStateEnum.TOKEN;
						break;
					case stk500v2RecvStateEnum.TOKEN:
						if (c[0] == STK500V2_TOKEN)
						{
							state = stk500v2RecvStateEnum.DATA;
						}
						else
						{
							state = stk500v2RecvStateEnum.START;
						}
						break;
					case stk500v2RecvStateEnum.DATA:
						if (curlen < stk500v2RecvBuff.Length) 
						{
							stk500v2RecvBuff[curlen] = c[0];
						}
						else
						{
							return -2;
						}
						if ((curlen == 0) && (stk500v2RecvBuff[0] == STK500V2_ANSWER_CKSUM_ERROR)) 
						{
							return -3;
						}
						curlen++;
						if (curlen == msglen)
						{
							state = stk500v2RecvStateEnum.CSUM;
						}
						break;
					case stk500v2RecvStateEnum.CSUM:
						if (checksum == 0)
						{
							state = stk500v2RecvStateEnum.DONE;
						}
						else
						{
							state = stk500v2RecvStateEnum.START;
							return -4;
						}
						break;
					default:
						return -5;
				}
			}
			return msglen;
		}

		private bool stk500v2CompareRecvBuffStr(int index, string str)
		{
			byte[] data = System.Text.Encoding.ASCII.GetBytes(str);
			for (int i = 0; i<str.Length; i++)
			{
				if (stk500v2RecvBuff[i+index] != data[i])
				{
					return false;
				}
			}
			return true;
		}

	}
}
