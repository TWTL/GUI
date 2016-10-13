using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Abstraction for "Connection" ex) socket
/// </summary>
/// <typeparam name="InfoT">connection info type</typeparam>
public abstract class TGBaseConnection<InfoT>
	where InfoT : TGBaseConnection<InfoT>.IConnectionInfo
{
	/// <summary>
	/// data(parameter) implementation needed for making connection.
	/// </summary>
	public interface IConnectionInfo
	{

	}
	
	public enum ConnectionStatus
	{
		None,
		Trying,
		Established,
	}

	public enum ErrorCode
	{
		NoError			= 0,

		CannotConnect,
		Disconnected,
		CannotReceive,
		CannotSend,
	}


	// Members
	
	Queue<ErrorCode>    m_errorQueue    = new Queue<ErrorCode>();
	Queue<byte[]>       m_recvQueue     = new Queue<byte[]>();
	Queue<byte[]>       m_sendQueue     = new Queue<byte[]>();

	object				m_stateLock     = new object();			// locking object


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
		lock(m_stateLock)
		{
			if (connectionStatus != ConnectionStatus.None)          // Try only when the connection is not made
			{
				Debug.LogWarning("cannot make connection now");
			}
			else
			{
				connectionStatus    = ConnectionStatus.Trying;
				MakeConnectionImpl(coninfo);
			}
		}
	}

	/// <summary>
	/// Kill connection
	/// </summary>
	public void Kill()
	{
		lock(m_stateLock)
		{
			if (connectionStatus == ConnectionStatus.Established)
			{
				KillConnectionImpl();
				connectionStatus    = ConnectionStatus.None;

				m_errorQueue.Clear();			// clear all buffers
				m_recvQueue.Clear();
				m_sendQueue.Clear();
			}
		}
	}

	/// <summary>
	/// sending data
	/// </summary>
	/// <param name="data"></param>
	public void Send(byte [] data)
	{
		lock(m_stateLock)
		{
			if (connectionStatus != ConnectionStatus.Established)
			{
				Debug.LogWarning("cannot send data when the connection is not made. operation ignored.");
			}
			else
			{
				m_sendQueue.Enqueue(data);				// not sending the message directly... actually it is sent by Update
			}
		}
	}

	/// <summary>
	/// reads data
	/// </summary>
	/// <returns></returns>
	public byte [] PollData()
	{
		lock(m_stateLock)
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
		lock(m_stateLock)
		{
			return m_errorQueue.Count > 0 ? m_errorQueue.Dequeue() : ErrorCode.NoError;
		}
	}

	/// <summary>
	/// need to update per frame
	/// </summary>
	public void Update()
	{
		if (connectionStatus != ConnectionStatus.Established)	// only if the connection is established
			return;

		if (!isSending && m_sendQueue.Count > 0)	// not in sending status and there's something to send
		{
			byte[] data;

			lock (m_stateLock)
			{
				isSending   = true;
				data		= m_sendQueue.Dequeue();
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
		lock(m_stateLock)
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
		lock(m_stateLock)
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
