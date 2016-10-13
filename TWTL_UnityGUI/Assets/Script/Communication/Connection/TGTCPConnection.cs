using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System;
using System.Collections.Generic;

/// <summary>
/// TCP implementation of TGBaseConnection
/// </summary>
public class TGTCPConnection : TGBaseConnection<TGTCPConnection.ConnectionInfo>
{
	/// <summary>
	/// parameter structure used when making a connection
	/// </summary>
	public struct ConnectionInfo : IConnectionInfo
	{
		public IPAddress    hostIP;
		public int          port;
	}


	class ConnectStateObject
	{
		public TGTCPConnection	connection;
		public Socket			socket;

		public ConnectStateObject(TGTCPConnection connection)
		{
			this.connection = connection;
			this.socket     = connection.m_socket;
		}
	}

	class SendStateObject
	{
		public TGTCPConnection  connection;
		public Socket           socket;

		public SendStateObject(TGTCPConnection connection)
		{
			this.connection = connection;
			this.socket     = connection.m_socket;
		}
	}

	/// <summary>
	/// state object used within receive operation
	/// </summary>
	class ReceiveStateObject
	{
		public TGTCPConnection	connection;
		public Socket			socket;
		public const int		bufferSize	= 1024;

		List<byte[]>        m_bufferList    = new List<byte[]>();

		public byte[]	lastBuffer
		{
			get { return m_bufferList[m_bufferList.Count-1]; }
		}

		public ReceiveStateObject(TGTCPConnection connection)
		{
			this.connection = connection;
			this.socket     = connection.m_socket;
		}

		/// <summary>
		/// allocate additional buffer and return
		/// </summary>
		/// <returns></returns>
		public byte[] AllocateNewBuffer()
		{
			var buffer      = new byte[bufferSize];
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
			if (bufferCount == 0)									// if there is no buffer...
				return new byte[] { 0 };

			var size		= (bufferCount - 1) * bufferSize;       // Assuming that only the last one has null termination character.
			var lastbuf     = m_bufferList[bufferCount - 1];
			var nullIndex   = 0;
			for (; nullIndex < bufferSize; nullIndex++)				// search for a null character
			{
				if (lastbuf[nullIndex] == 0)						// break the loop if we get the index of a null char.
					break;
			}
			size            += nullIndex;                           // total data size, ends right before the null character.

			var combuf      = new byte[size];
			var writeInd    = 0;
			for (var i = 0; i < bufferCount - 1; i++)				// copying data to new buffer
			{
				m_bufferList[i].CopyTo(combuf, writeInd);
				writeInd    += bufferSize;
			}

			Array.Copy(lastbuf, 0, combuf, writeInd, nullIndex);	// copying last buffer

			m_bufferList.Clear();									// flush the buffer list

			return combuf;
		}
	}
	//



	// Members

	Socket                  m_socket;				// socket for this connection

	
	protected override void MakeConnectionImpl(ConnectionInfo coninfo)
	{
		var endpoint        = new IPEndPoint(coninfo.hostIP, coninfo.port);
		m_socket            = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

		var conSo           = new ConnectStateObject(this);

		m_socket.BeginConnect(endpoint, ConnectCallback, conSo);
	}

	private static void ConnectCallback(IAsyncResult conar)
	{
		var stateObject = conar.AsyncState as ConnectStateObject;
		var socket      = stateObject.socket;
		var self        = stateObject.connection;

		try
		{
			socket.EndConnect(conar);
			self.SetConnectionEstablished();            // if no exception occurs, this will be excuted

			ReceiveChainStart(self, socket);			// start new receive chain
		}
		catch (Exception e)                             // if there's any connection error...
		{
			Debug.LogWarning("Connection failed : " + e);
			self.SetConnectionFailed();
		}
	}

	private static void ReceiveChainStart(TGTCPConnection self, Socket socket)
	{
		var readso  = new ReceiveStateObject(self);
		socket.BeginReceive(readso.AllocateNewBuffer(), 0, ReceiveStateObject.bufferSize, SocketFlags.None, ReceiveChainCallback, readso);
	}

	private static void ReceiveChainCallback(IAsyncResult ar)
	{
		Debug.Log("ReceiveChainCallback");

		var stateObject = ar.AsyncState as ReceiveStateObject;
		var socket      = stateObject.socket;
		var self        = stateObject.connection;

		try
		{
			var bytesRead   = socket.EndReceive(ar);
			if (bytesRead == ReceiveStateObject.bufferSize)	// continue another receive chain, because there might be more data incoming
			{
				Debug.Log("ReceiveChain continues...");

				socket.BeginReceive(stateObject.AllocateNewBuffer(), 0, ReceiveStateObject.bufferSize, SocketFlags.None, ReceiveChainCallback, stateObject);
			}
			else
			{                                       // all the data has arrived.

				var combuf  = stateObject.GetCombinedBuffer();
				self.PushRecvData(combuf);          // push to internal buffer

				Debug.Log("ReceiveChain end...");

				ReceiveChainStart(self, socket);            // start new receive chain
			}
		}
		catch (Exception e)
		{
			Debug.LogWarning("cannot receive : " + e);
			self.PushErrorCode(ErrorCode.CannotReceive);
		}
	}

	protected override void KillConnectionImpl()
	{
		if (m_socket.Connected)
			m_socket.Shutdown(SocketShutdown.Both);
		m_socket.Close();
		m_socket    = null;
	}

	protected override void SendDataImpl(byte[] data)
	{

		var sendSO  = new SendStateObject(this);
		m_socket.BeginSend(data, 0, data.Length, SocketFlags.None, SendCallback, sendSO);
	}

	private static void SendCallback(IAsyncResult ar)
	{
		var stateObject = ar.AsyncState as SendStateObject;
		var socket      = stateObject.socket;
		var self        = stateObject.connection;

		try
		{
			var bytesSent   = socket.EndSend(ar);
		}
		catch(Exception e)
		{
			Debug.LogWarning("cannot send data : " + e);
			self.PushErrorCode(ErrorCode.CannotSend);
		}
		finally
		{
			self.SetSendingOver();
		}
	}
}
