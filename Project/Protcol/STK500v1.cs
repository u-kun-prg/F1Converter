using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace F1Converter
{
	public partial class Protcol
	{
		// ATMEL STK500 Communication Protocol
		private const byte Cmnd_STK_GET_SYNC = 0x30;
		private const byte Cmnd_STK_GET_SIGN_ON = 0x31;
		private const byte Cmnd_STK_GET_PARAMETER = 0x41;
		private const byte Cmnd_STK_ENTER_PROGMODE = 0x50;
		private const byte Cmnd_STK_LEAVE_PROGMODE = 0x51;
		private const byte Cmnd_STK_LOAD_ADDRESS = 0x55;
		private const byte Cmnd_STK_PROG_PAGE = 0x64;
		private const byte Cmnd_STK_READ_PAGE = 0x74;

		private const byte Param_STK_SW_MAJOR = 0x81;
		private const byte Param_STK_SW_MINOR = 0x82;

		private const byte Sync_CRC_EOP = 0x20;

		private const byte Resp_STK_INSYNC = 0x14;
		private const byte Resp_STK_OK = 0x10;

		private byte[] stk500v1RecvBuff = new byte[256];

		private bool Stk500v1_Cmd_GetSync(System.IO.Ports.SerialPort port)
		{
			for (int i = 0; i < 3; i++)
			{
				byte[] com = {  Cmnd_STK_GET_SYNC,      Sync_CRC_EOP };
				byte[] rq = {   Resp_STK_INSYNC,        Resp_STK_OK };
				if (!stk500v1RequestForAnswer(port, com, rq, stk500v1RecvBuff))
				{
					return false;
				}
			}
			return true;
		}

		private bool Stk500v1_Cmd_LoadAddress(System.IO.Ports.SerialPort port, int address)
		{
			byte[] com = new byte[4];
			byte[] res = {	Resp_STK_INSYNC,	Resp_STK_OK	};
			com[0] = Cmnd_STK_LOAD_ADDRESS;
			com[1] = (byte)((address >> 1) & 0xff);
			com[2] = (byte)((address >> 9) & 0xff);
			com[3] = Sync_CRC_EOP;
			if (!stk500v1RequestForAnswer(port, com, res, stk500v1RecvBuff))
			{
				return false;
			}
			return true;
		}

		private int Stk500v1_Cmd_WriteFlash(System.IO.Ports.SerialPort port, byte[] sourceBuff,int sourceIndex, int sendSize)
		{
			if (sourceIndex + sendSize > sourceBuff.Length) 
			{
				sendSize = (byte)(sourceBuff.Length - sourceIndex);
			}
			byte[] com = new byte[sendSize + 5];
			byte[] res = {	Resp_STK_INSYNC,	Resp_STK_OK };
			com[0] = Cmnd_STK_PROG_PAGE;
			com[1] = 0x00;
			com[2] = (byte)sendSize;
			com[3] = (byte)'F';		//	'F'=flash	'E'=eeprom
			for (int i = 0; i < sendSize; i++)
			{
				com[4 + i] = sourceBuff[sourceIndex + i];
			}
			com[com.Length - 1] = Sync_CRC_EOP;
			if (!stk500v1RequestForAnswer(port, com, res, stk500v1RecvBuff))
			{
				return -1;
			}
			return sendSize;
		}

		private bool Stk500v1_Cmd_ReadFlash(System.IO.Ports.SerialPort port, int recvSize)
		{
			byte[] com = new byte[5];
			com[0] = Cmnd_STK_READ_PAGE;
			com[1] = 0x00;
			com[2] = (byte)recvSize;
			com[3] = (byte)'F';
			com[4] = Sync_CRC_EOP;
			if (!stk500v1RequestForAnswer(port, com, stk500v1RecvBuff, recvSize + 2))
			{
				return false;
			}
			if (stk500v1RecvBuff[0] != Resp_STK_INSYNC || stk500v1RecvBuff[recvSize + 1] != Resp_STK_OK)
			{
				return false;
			}
			return true;
		}

		private bool Stk500v1_ReadVeriy(byte[] sourceBuff, int sourceIndex, int recvSize)
		{
			for (int i = 0; i < recvSize; i++)
			{
				if (sourceBuff[sourceIndex + i] != stk500v1RecvBuff[i + 1])
				{
					return false;
				}
			}
			return true;
		}

		private bool Stk500v1_Cmd_LeaveProgramMode(System.IO.Ports.SerialPort port)
		{
			byte[] com = {	Cmnd_STK_LEAVE_PROGMODE,	Sync_CRC_EOP	};
			byte[] res = {	Resp_STK_INSYNC,			Resp_STK_OK		};
			if (!stk500v1RequestForAnswer(port, com, res, stk500v1RecvBuff))
			{
				return false;
			}
			return true;
		}

		private bool stk500v1RequestForAnswer(System.IO.Ports.SerialPort port, byte[] command, byte[] result, byte[] readbuf)
		{
			port.Write(command, 0, command.Length);
			if (!WaitToRead(port, result.Length)) 
			{
				return false;
			}
			port.Read(readbuf, 0, result.Length);
			for (int i = 0; i < result.Length; i++)
			{
				if (result[i] != readbuf[i]) return false;
			}
			return true;
		}

		private bool stk500v1RequestForAnswer(System.IO.Ports.SerialPort port, byte[] command, byte[] readbuf,int readlen)
		{
			port.Write(command, 0, command.Length);
			if (!WaitToRead(port, readlen)) 
			{
				return false;
			}
			port.Read(readbuf, 0, readlen);
			return true;
		}
	}
}
