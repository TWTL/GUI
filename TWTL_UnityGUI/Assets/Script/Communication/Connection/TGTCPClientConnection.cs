using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System;
using System.Collections.Generic;

/// <summary>
/// TCP implementation of TGBaseConnection
/// </summary>
public class TGTCPClientConnection : TGBaseClientConnection<TGTCPClientConnection.ConnectionInfo>
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
		public TGTCPClientConnection	connection;
		public Socket			socket;

		public ConnectStateObject(TGTCPClientConnection connection)
		{
			this.connection = connection;
			this.socket     = connection.m_socket;
		}
	}

	class SendStateObject
	{
		public TGTCPClientConnection  connection;
		public Socket           socket;

		public SendStateObject(TGTCPClientConnection connection)
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
		public TGTCPClientConnection	connection;
		public Socket			socket;

		ReceiveBuffer           recvBuffer;

		public int bufferSize
		{
			get { return recvBuffer.bufferSize; }
		}

		public ReceiveStateObject(TGTCPClientConnection connection)
		{
			this.connection = connection;
			this.socket     = connection.m_socket;

			recvBuffer      = new ReceiveBuffer();
		}

		public byte[] AllocateNewBuffer()
		{
			return recvBuffer.AllocateNewBuffer();
		}

		public byte[] GetCombinedBuffer()
		{
			return recvBuffer.GetCombinedBuffer();
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
			self.SetConnectionEstablished();            // if no exception occurs, this will be executed

			ReceiveChainStart(self, socket);			// start new receive chain
		}
		catch (Exception e)                             // if there's any connection error...
		{
			Debug.LogWarning("Connection failed : " + e);
			self.SetConnectionFailed();
		}
	}

	private static void ReceiveChainStart(TGTCPClientConnection self, Socket socket)
	{
		var readso  = new ReceiveStateObject(self);
		socket.BeginReceive(readso.AllocateNewBuffer(), 0, readso.bufferSize, SocketFlags.None, ReceiveChainCallback, readso);
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

			if (bytesRead == 0)						// if receives zero data, assume this is due to disconnection
			{
				Debug.LogWarning("disconnected!");
				self.PushErrorCode(ErrorCode.Disconnected);
			}
			else if (bytesRead == stateObject.bufferSize)	// continue another receive chain, because there might be more data incoming
			{
				Debug.Log("ReceiveChain continues...");

				socket.BeginReceive(stateObject.AllocateNewBuffer(), 0, stateObject.bufferSize, SocketFlags.None, ReceiveChainCallback, stateObject);
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
			//var bytesSent   = socket.EndSend(ar);
			socket.EndSend(ar);
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
