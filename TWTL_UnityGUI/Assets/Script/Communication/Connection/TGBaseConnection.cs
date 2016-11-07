using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public abstract class TGBaseConnection<InfoT>
	where InfoT : TGBaseConnection<InfoT>.IConnectionInfo
{
	/// <summary>
	/// buffer implementation for socket data receiving
	/// </summary>
	protected class ReceiveBuffer
	{
		int                 m_bufferSize;
		List<byte[]>        m_bufferList    = new List<byte[]>();

		public int bufferSize { get { return m_bufferSize; } }

		public ReceiveBuffer(int bufferSize = 1024)
		{
			m_bufferSize = bufferSize;
		}

		public byte[] lastBuffer
		{
			get { return m_bufferList[m_bufferList.Count-1]; }
		}

		/// <summary>
		/// allocate additional buffer and return
		/// </summary>
		/// <returns></returns>
		public byte[] AllocateNewBuffer()
		{
			var buffer      = new byte[m_bufferSize];
			m_bufferList.Add(buffer);
			return buffer;
		}

		/// <summary>
		/// Combine and truncate all buffers into one big buffer. awares null termination character
		/// </summary>
		/// <returns></returns>
		public byte[] GetCombinedBuffer()
		{
			var bufferCount = m_bufferList.Count;
			if (bufferCount == 0)                                   // if there is no buffer...
				return new byte[] { 0 };

			var size        = (bufferCount - 1) * m_bufferSize;		// Assuming that only the last one has null termination character.
			var lastbuf     = m_bufferList[bufferCount - 1];
			var nullIndex   = 0;
			for (; nullIndex < m_bufferSize; nullIndex++)			// search for a null character
			{
				if (lastbuf[nullIndex] == 0)                        // break the loop if we get the index of a null char.
					break;
			}
			size            += nullIndex;                           // total data size, ends right before the null character.

			var combuf      = new byte[size];
			var writeInd    = 0;
			for (var i = 0; i < bufferCount - 1; i++)               // copying data to new buffer
			{
				m_bufferList[i].CopyTo(combuf, writeInd);
				writeInd    += m_bufferSize;
			}

			Array.Copy(lastbuf, 0, combuf, writeInd, nullIndex);    // copying last buffer

			m_bufferList.Clear();                                   // flush the buffer list

			return combuf;
		}
	}

	//

	/// <summary>
	/// data(parameter) implementation needed for making connection.
	/// </summary>
	public interface IConnectionInfo
	{

	}

	public enum ConnectionStatus
	{
		None,
		Waiting,
		Established,
	}

	public enum ErrorCode
	{
		NoError = 0,

		CannotConnect,
		Disconnected,
		CannotReceive,
		CannotSend,
	}



	// Members

	Queue<ErrorCode>    m_errorQueue    = new Queue<ErrorCode>();
	Queue<byte[]>       m_recvQueue     = new Queue<byte[]>();
	Queue<byte[]>       m_sendQueue     = new Queue<byte[]>();

	object              m_stateLock     = new object();         // locking object


	/// <summary>
	/// current connection status
	/// </summary>
	public ConnectionStatus connectionStatus { get; private set; }

	/// <summary>
	/// is sending message?
	/// </summary>
	protected bool isSending { get; private set; }



	/// <summary>
	/// Make connection
	/// </summary>
	/// <param name="coninfo"></param>
	/// <param name="delResult">
	public void Make(InfoT coninfo)
	{
		lock (m_stateLock)
		{
			if (connectionStatus != ConnectionStatus.None)          // Try only when the connection is not made
			{
				Debug.LogWarning("cannot make connection now");
			}
			else
			{
				connectionStatus    = ConnectionStatus.Waiting;
				MakeConnectionImpl(coninfo);
			}
		}
	}

	/// <summary>
	/// Kill connection
	/// </summary>
	public void Kill()
	{
		lock (m_stateLock)
		{
			if (connectionStatus == ConnectionStatus.Established)
			{
				KillConnectionImpl();
				connectionStatus    = ConnectionStatus.None;

				m_errorQueue.Clear();           // clear all buffers
				m_recvQueue.Clear();
				m_sendQueue.Clear();
			}
		}
	}

	/// <summary>
	/// sending data
	/// </summary>
	/// <param name="data"></param>
	public void Send(byte[] data)
	{
		lock (m_stateLock)
		{
			if (connectionStatus != ConnectionStatus.Established)
			{
				Debug.LogWarning("cannot send data when the connection is not made. operation ignored.");
			}
			else
			{
				m_sendQueue.Enqueue(data);              // not sending the message directly... actually it is sent by Update
			}
		}
	}

	/// <summary>
	/// reads data
	/// </summary>
	/// <returns></returns>
	public byte[] PollData()
	{
		lock (m_stateLock)
		{
			return m_recvQueue.Count > 0 ? m_recvQueue.Dequeue() : null;
		}
	}

	/// <summary>
	/// reads error code if any
	/// </summary>
	/// <returns></returns>
	public ErrorCode PollError()
	{
		lock (m_stateLock)
		{
			return m_errorQueue.Count > 0 ? m_errorQueue.Dequeue() : ErrorCode.NoError;
		}
	}

	/// <summary>
	/// need to update per frame
	/// </summary>
	public void Update()
	{
		if (connectionStatus != ConnectionStatus.Established)   // only if the connection is established
			return;

		if (!isSending && m_sendQueue.Count > 0)    // not in sending status and there's something to send
		{
			byte[] data;

			lock (m_stateLock)
			{
				isSending   = true;
				data        = m_sendQueue.Dequeue();
			}

			try
			{
				SendDataImpl(data);
			}
			catch (System.Exception e)
			{
				Debug.LogWarning("cannot attempt to send : " + e);
				PushErrorCode(ErrorCode.CannotSend);
			}
		}
	}


	//

	protected void SetConnectionEstablished()
	{
		connectionStatus    = ConnectionStatus.Established;
	}

	protected void SetConnectionFailed()
	{
		connectionStatus    = ConnectionStatus.None;
		PushErrorCode(ErrorCode.CannotConnect);
	}

	protected void SetSendingOver()
	{
		lock (m_stateLock)
			isSending           = false;
	}

	/// <summary>
	/// push error code to internal error queue
	/// </summary>
	/// <param name="code"></param>
	protected void PushErrorCode(ErrorCode code)
	{
		lock (m_stateLock)
		{
			m_errorQueue.Enqueue(code);
		}
	}

	/// <summary>
	/// push received data to internal queue
	/// </summary>
	/// <param name="data"></param>
	protected void PushRecvData(byte[] data)
	{
		lock (m_stateLock)
		{
			m_recvQueue.Enqueue(data);
		}
	}



	// implementation stubs

	/// <summary>
	/// actual implementation for making connection
	/// </summary>
	/// <param name="coninfo"></param>
	protected abstract void MakeConnectionImpl(InfoT coninfo);

	/// <summary>
	/// actual implementation for disconnection
	/// </summary>
	protected abstract void KillConnectionImpl();

	/// <summary>
	/// 
	/// </summary>
	/// <param name="data"></param>
	protected abstract void SendDataImpl(byte[] data);
}
