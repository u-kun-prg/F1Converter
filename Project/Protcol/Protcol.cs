using System;
using System.Collections.Generic;
using System.Threading;

namespace F1Converter
{
	public class Board
	{
		public string Name;
		public enum ProtcolTypeEnum
		{
			NONE = 0,
			STK500 = 1,
			STK500V2 = 2,
			SAM_BA = 3,
		}
		public ProtcolTypeEnum ProtcolType;
		public int FlashStartAddress;
		public int FlashEndAddress;
		public int UpLoadSpeed;
		public enum DTRControlTypeEnum
		{
			ENABLE_AND_THROUGH_DISABLE,
			ENABLE_AND_THROUGH,
			UNKNOWN
		}
		public DTRControlTypeEnum DTRControl;
	}

	public partial class Protcol
	{
		public enum MainStateEnum
		{
			IDLE,
			RESET,
			SEND,
			VERIFY,
			ABORT
		}
		public MainStateEnum MainState = MainStateEnum.IDLE;

		public enum ErrorStatusEnum
		{
			NONE,
			SUCCESS,
			UNKNOWN,
			BOARD_DTRE_EXCEPTION,
			REQUEST_TIMEOUT,
			ADDRESS_FAULT,
			CANNOT_ENTER_PROGRAMING_MODE,
			SET_ADDRESS,
			WRITE_DATA,
			READ_DATA,
			PORT_NOT_FOUND,
			VERIYFY_UNMUCH,
		}
		public ErrorStatusEnum ErrorState = ErrorStatusEnum.NONE;

		public class DataRecord
		{
			public int Address;
			public byte[] Data;
			public bool Verify;
		}

		public class MessageRecord
		{
			public enum MessageTypeEnum
			{
				Stop,
				Start,
				Clear,
				Abort,
				SetPortName,
				SetDevice,
				SetRecord,
				EOT
			}
			public MessageTypeEnum MessageType;

			public object Data;
			public MessageRecord(MessageTypeEnum messageType, object data)
			{
				MessageType = messageType;
				Data = data;
			}
			public MessageRecord(MessageTypeEnum messageType)
			{
				MessageType = messageType;
				Data = null;
			}
		}
		//
		public int RecordIndex;
		public int RecordCount;
		public float RecordProcessing;
		public delegate void StateCallbackMethod();
		public StateCallbackMethod StateCallback=null;
		//
		private AutoResetEvent AwakeEvent = new AutoResetEvent(false);
		private AutoResetEvent AfterPopMessageEvent = new AutoResetEvent(false);
		private AutoResetEvent CriticalEvent = new AutoResetEvent(true);
		private int CriticalCounter = 0;

		private List<DataRecord> DataRecordList = new List<DataRecord>();
		private ReaderWriterLock queueLock = new ReaderWriterLock();
		private Queue<MessageRecord> MessageQueue = new Queue<MessageRecord>();

		private Thread SerialIOThread = null;
		private string Portname;
		private Board CurrentBoard = null;

		private int m_protcolType;

		public Protcol()
		{
			//
		}
		public void WorkerThreadMain()
		{
			System.IO.Ports.SerialPort port = null;

			while (true)
			{
				//Interlocked.Increment
				AwakeEvent.WaitOne();
				AwakeEvent.Reset();
				do
				{
					MessageRecord mr = PopMessage();
					if (mr == null)
					{
						break;
					}
					switch (mr.MessageType)
					{
						case MessageRecord.MessageTypeEnum.Clear:
							DataRecordList.Clear();
							break;
						case MessageRecord.MessageTypeEnum.SetPortName:
							Portname = (string)mr.Data;
							break;
						case MessageRecord.MessageTypeEnum.SetDevice:
							CurrentBoard = (Board)mr.Data;
							break;
						case MessageRecord.MessageTypeEnum.SetRecord:
							DataRecordList.Add((DataRecord)mr.Data);
							break;
						case MessageRecord.MessageTypeEnum.Start:
							break;
						case MessageRecord.MessageTypeEnum.Abort:
							SerialIOThread.Abort();
							break;
					}
					if (mr.MessageType == MessageRecord.MessageTypeEnum.Start)
					{
						break;
					}
				} while (true);

				try
				{
					if (port != null)
					{
						port.Close();
						port.Dispose();
						port = null;
					}
				}
				catch (Exception exp)
				{
					port = null;
					System.Console.WriteLine("{0}", exp.Message);
				}
				AfterPopMessageEvent.Set();
				Interlocked.Increment(ref CriticalCounter);     // Enter critical section
				if (CurrentBoard == null) continue;
				byte[] rbuf = new byte[256];
				MainState = MainStateEnum.RESET;
				ErrorState = ErrorStatusEnum.NONE;
				RecordIndex = 0;
				RecordCount = DataRecordList.Count;
				port = new System.IO.Ports.SerialPort(Portname);
				port.ErrorReceived += new System.IO.Ports.SerialErrorReceivedEventHandler(port_ErrorReceived);
				{
					try
					{
						port.Open();
						port.DiscardInBuffer();
						port.DiscardOutBuffer();
					}
					catch (Exception exp)
					{
						System.Console.WriteLine("{0}", exp.Message);
						ErrorState = ErrorStatusEnum.PORT_NOT_FOUND;
						goto ErrorExit;
					}

					if (StateCallback != null) 
					{
						StateCallback();
					}

					try
					{
						port.BaudRate = CurrentBoard.UpLoadSpeed;
						port.WriteTimeout = 1000;
						port.ReadTimeout = 1000;
						m_protcolType = (int)CurrentBoard.ProtcolType;
						switch (CurrentBoard.DTRControl)
						{
							case Board.DTRControlTypeEnum.ENABLE_AND_THROUGH_DISABLE:
								port.DtrEnable = true;
								port.DiscardInBuffer();
								port.DiscardOutBuffer();
								Thread.Sleep(500);
								port.DtrEnable = false;
								port.DiscardInBuffer();
								port.DiscardOutBuffer();
								Thread.Sleep(100);
								port.DiscardInBuffer();
								port.DiscardOutBuffer();
								break;
							case Board.DTRControlTypeEnum.ENABLE_AND_THROUGH:
								port.DtrEnable = true;
								port.DiscardInBuffer();
								port.DiscardOutBuffer();
								Thread.Sleep(500);
								port.DiscardInBuffer();
								port.DiscardOutBuffer();
								break;
						}
					}
					catch (Exception exp)
					{
						System.Console.WriteLine("{0}", exp.Message);
						ErrorState = ErrorStatusEnum.BOARD_DTRE_EXCEPTION;
						goto ErrorExit;
					}

					switch(m_protcolType)
					{
						case 1:			//	Board.ProtcolTypeEnum.STK500:
							if (!Stk500v1_Cmd_GetSync(port))
							{
								ErrorState = ErrorStatusEnum.REQUEST_TIMEOUT;
								goto ErrorExit;
							}
							break;
						case 2:			//	Board.ProtcolTypeEnum.STK500v2:
							if (!Stk500v2_Cmd_SignOn(port))
							{
								ErrorState = ErrorStatusEnum.REQUEST_TIMEOUT;
								goto ErrorExit;
							}
							if (!Stk500v2_EnterProgramMode(port))
							{
								ErrorState = ErrorStatusEnum.CANNOT_ENTER_PROGRAMING_MODE;
								goto ErrorExit;
							}
							break;
						case 3:			//	Board.ProtcolTypeEnum.SAM_BA:
							break;
						default:
							break;
					}

					foreach (DataRecord record in DataRecordList)
					{
						if (DataRecordList.Count > 0) 
						{
							MainState = MainStateEnum.SEND;
						}
						if (StateCallback != null) 
						{
							StateCallback();
						}
						if (!CheckData(record))
						{
							ErrorState = ErrorStatusEnum.ADDRESS_FAULT;
							goto ErrorExit;
						}

						int sendcount;
						RecordProcessing = 0;
						switch(m_protcolType)
						{
							case 1:			//	Board.ProtcolTypeEnum.STK500:
								for (sendcount = 0; sendcount < record.Data.Length; )
								{
									int address = record.Address + sendcount;
									if (!Stk500v1_Cmd_LoadAddress(port,address))
									{
										ErrorState = ErrorStatusEnum.SET_ADDRESS;
										goto ErrorExit;
									}
									int sendSize = 128;
									sendSize = Stk500v1_Cmd_WriteFlash(port, record.Data, sendcount, sendSize);
									if (sendSize < 0)
									{
										ErrorState = ErrorStatusEnum.WRITE_DATA;
										goto ErrorExit;
									}
									sendcount += sendSize;
									SetRecordProcess((float)sendcount / (float)record.Data.Length);
								}
								break;
							case 2:			//	Board.ProtcolTypeEnum.STK500v2:
								//	stk500v2(Arduino MEGA R3
								//	フラッシュへの書き込みアドレスのロードを動作させることができていない。かならず、0 から書き込まれる
								//	0 からロードアドレスまでのデータを読み出しロードデータを結合させて書き込むように対策する
								{
									sendcount = 0;
									byte[] tmp_read_buff = new byte[record.Address + record.Data.Length];
									for (int tmp_read_blk = 0; tmp_read_blk < record.Address; )
									{
										int address = sendcount;
										if (!Stk500v2_Cmd_LoadAddress(port, address))
										{
											ErrorState = ErrorStatusEnum.SET_ADDRESS;
											goto ErrorExit;
										}
										if (!Stk500v2_Cmd_ReadFlash(port))
										{
											ErrorState = ErrorStatusEnum.READ_DATA;
											goto ErrorExit;
										}
										for (int i = 0; i < 0x100; i++)
										{
											tmp_read_buff[sendcount] = stk500v2RecvBuff[i + 2];
											sendcount += 1;
										}
										tmp_read_blk += 0x100;
									}
									for (int i = 0; i < record.Data.Length; i++)
									{
										tmp_read_buff[sendcount] = record.Data[i];
										sendcount += 1;
									}
									record.Address = 0;
									record.Data = tmp_read_buff;
								}
								for (sendcount = 0; sendcount < record.Data.Length; )
								{
									int address = record.Address + sendcount;
									if (!Stk500v2_Cmd_LoadAddress(port, address))
									{
										ErrorState = ErrorStatusEnum.SET_ADDRESS;
										goto ErrorExit;
									}
									int sendSize = 0x100;
									sendSize = Stk500v2_Cmd_WriteFlash(port, record.Data, sendcount, sendSize);
									if (sendSize < 0)
									{
										ErrorState = ErrorStatusEnum.WRITE_DATA;
										goto ErrorExit;
									}
									sendcount += sendSize;
									SetRecordProcess((float)sendcount / (float)record.Data.Length);
								}
								break;
							case 3:			//	Board.ProtcolTypeEnum.SAM_BA:
								break;
							default:
								break;
						}

						if (record.Verify)
						{
							if (DataRecordList.Count > 0) 
							{
								MainState = MainStateEnum.VERIFY;
							}
							if (StateCallback != null) 
							{
								StateCallback();
							}
							RecordProcessing = 0;
							switch(m_protcolType)
							{
								case 1:			//	Board.ProtcolTypeEnum.STK500:
									for (sendcount = 0; sendcount < record.Data.Length; )
									{
            		                    int address = record.Address + sendcount;
										if (!Stk500v1_Cmd_LoadAddress(port,address))
										{
											ErrorState = ErrorStatusEnum.SET_ADDRESS;
											goto ErrorExit;
										}
	                        	        int recvSize = 128;
	                        	        if (sendcount + recvSize > record.Data.Length) 
										{
											recvSize = (byte)(record.Data.Length - sendcount);
										}
										if (!Stk500v1_Cmd_ReadFlash(port, recvSize))
										{
											ErrorState = ErrorStatusEnum.READ_DATA;
											goto ErrorExit;
										}
										if (!Stk500v1_ReadVeriy(record.Data, sendcount, recvSize))
										{
											ErrorState = ErrorStatusEnum.VERIYFY_UNMUCH;
											goto ErrorExit;
										}
										sendcount += recvSize;
										SetRecordProcess((float)sendcount / (float)record.Data.Length);
									}
									break;
								case 2:			//	Board.ProtcolTypeEnum.STK500v2:
									for (sendcount = 0; sendcount < record.Data.Length; )
									{
            	            	        int address = record.Address + sendcount;
										if (!Stk500v2_Cmd_LoadAddress(port, address))
										{
											ErrorState = ErrorStatusEnum.SET_ADDRESS;
											goto ErrorExit;
										}
										if (!Stk500v2_Cmd_ReadFlash(port))
										{
											ErrorState = ErrorStatusEnum.READ_DATA;
											goto ErrorExit;
										}
										if (!Stk500v2_ReadVeriy(record.Data, sendcount))
										{
											ErrorState = ErrorStatusEnum.VERIYFY_UNMUCH;
											goto ErrorExit;
										}
										sendcount += 0x100;
										SetRecordProcess((float)sendcount / (float)record.Data.Length);
									}
									break;
								case 3:			//	Board.ProtcolTypeEnum.SAM_BA:
									break;
								default:
									break;
							}
						}
						RecordIndex++;
					}

					switch(m_protcolType)
					{
						case 1:			//	Board.ProtcolTypeEnum.STK500:
							if (!Stk500v1_Cmd_LeaveProgramMode(port))
							{
								ErrorState = ErrorStatusEnum.UNKNOWN;
								goto ErrorExit;
							}
							break;
						case 2:			//	Board.ProtcolTypeEnum.STK500v2:
							if (!Stk500v2_Cmd_LeaveProgramMode(port))
							{
								ErrorState = ErrorStatusEnum.UNKNOWN;
								goto ErrorExit;
							}
							break;
						case 3:			//	Board.ProtcolTypeEnum.SAM_BA:
							break;
						default:
							break;
					}

					port.Close();
					port.Dispose();
					Interlocked.Decrement(ref CriticalCounter);
					MainState = MainStateEnum.IDLE;
					ErrorState = ErrorStatusEnum.SUCCESS;
					if (StateCallback != null) 
					{
						StateCallback();
					}
					continue;

				ErrorExit:
					while (PopMessage() != null) ;
					try
					{
						port.Close();
						port.Dispose();
						port = null;
					}
					catch (Exception exp)
					{
						port = null;
						System.Console.WriteLine("{0}", exp.Message);
					}
					if (CriticalCounter == 1) 
					{
						Interlocked.Decrement(ref CriticalCounter);
					}
					MainState = MainStateEnum.ABORT;
					if (StateCallback != null) 
					{
						StateCallback();
					}
					continue;
				}
			}
		}

		private void SetRecordProcess(float rp)
		{
			RecordProcessing = (rp < 0f) ? 0f : ((rp > 1f) ? 1f : rp);
			if (StateCallback != null) 
			{
				StateCallback();
			}
		}

		void port_ErrorReceived(object sender, System.IO.Ports.SerialErrorReceivedEventArgs e)
		{
			throw new NotImplementedException();
		}

		private bool WaitToRead(System.IO.Ports.SerialPort port, int cnt)
		{
			//for (int i = 0; i < 1024; i++)
			for (int i = 0; i < 256; i++)
			{
				if (port.BytesToWrite ==0 && port.BytesToRead >= cnt) 
				{
					return true;
				}
				Thread.Sleep(4);
			}
			return false;
		}

		private bool CheckData(DataRecord r)
		{
			if (r.Address < 0) 
			{
				return false;
			}
			if (r.Address + r.Data.Length >= CurrentBoard.FlashEndAddress) 
			{
				return false;
			}
			return true;
		}

		// Only Worker Thread.
		public int CountMessage()
		{
			return MessageQueue.Count;
		}

		// Only Worker Thread.
		public MessageRecord PopMessage()
		{
			lock (MessageQueue)
			{
				if (MessageQueue.Count == 0) 
				{
					return null;
				}
				return MessageQueue.Dequeue();
			}
		}

		// Only Main Thread.
		public void PushMessage(MessageRecord rc)
		{
			lock (MessageQueue)
			{
				MessageQueue.Enqueue(rc);
			}
		}

		public void AwakeWorker()
		{
			if (SerialIOThread == null)
			{
				SerialIOThread = new Thread(WorkerThreadMain);
				SerialIOThread.Name = "ProtcolControl";
				SerialIOThread.Start();
			}
			int lcount=0;
			while (SerialIOThread.ThreadState != ThreadState.WaitSleepJoin)
			{
				Thread.Sleep(10);
				if (lcount++ > 100) return;
			} 
			AfterPopMessageEvent.Reset();
			AwakeEvent.Set();
			AfterPopMessageEvent.WaitOne();
		}

		public bool IsRunningCritical
		{
			get
			{
				if (CriticalCounter == 0)
				{
					return false;
				}
				else
				{
					return true;
				}
			}
		}

		public void AbortThread()
		{
			if (SerialIOThread != null)
			{
				SerialIOThread.Abort();
			}
		}
	}
}
