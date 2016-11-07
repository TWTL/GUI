using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

public class TGTCPServerConnection : TGBaseServerConnection<TGTCPServerConnection.ConnectionInfo>
{
	public struct ConnectionInfo : IConnectionInfo
	{
		public IPAddress    hostIP;
		public int			port;

		/// <summary>
		/// find available port number randomly.
		/// </summary>
		/// <param name="minNum"></param>
		/// <param name="maxNum"></param>
		public void AllocateAvailablePort(int minNum = 10000, int maxNum = 20000)
		{
			var ipGlobalProperties	= IPGlobalProperties.GetIPGlobalProperties();
			var tcpConnInfoArray	= ipGlobalProperties.GetActiveTcpConnections();

			// Build a blacklist for port numbers
			var blacklist           = new HashSet<int>();
			var count               = tcpConnInfoArray.Length;
			for (var i = 0; i < count; i++)
			{
				var tcpi            = tcpConnInfoArray[i];
				blacklist.Add(tcpi.LocalEndPoint.Port);
			}

			// find a port number that is not in the list.
			do
			{
				this.port           = Random.Range(minNum, maxNum);
			}
			while (blacklist.Contains(this.port));	// ...do we need to care about an infinite loop case?
		}
	}

	class AcceptStateObject
	{
		public TGTCPServerConnection connection;
		public Socket socket;

		public AcceptStateObject(TGTCPServerConnection con)
		{
			connection  = con;
			socket      = con.m_listenSocket;
		}
	}

	class SendStateObject
	{
		public TGTCPServerConnection  connection;
		public Socket           socket;

		public SendStateObject(TGTCPServerConnection connection)
		{
			this.connection = connection;
			this.socket     = connection.workSocket;
		}
	}

	/// <summary>
	/// state object used within receive operation
	/// </summary>
	class ReceiveStateObject
	{
		public TGTCPServerConnection    connection;
		public Socket           socket;

		ReceiveBuffer           recvBuffer;

		public int bufferSize
		{
			get { return recvBuffer.bufferSize; }
		}

		public ReceiveStateObject(TGTCPServerConnection connection)
		{
			this.connection = connection;
			this.socket     = connection.workSocket;

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

	Socket                  m_listenSocket;
	Socket                  m_workSocket;               // socket for this connection

	object                  m_lockObj   = new object();


	Socket workSocket
	{
		get
		{
			lock (m_lockObj)
				return m_workSocket;
		}
		set
		{
			lock (m_lockObj)
				m_workSocket    = value;
		}
	}


	protected override void MakeConnectionImpl(ConnectionInfo coninfo)
	{
		var endpoint        = new IPEndPoint(coninfo.hostIP, coninfo.port);
		m_listenSocket		= new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		m_listenSocket.Bind(endpoint);

		var accso           = new AcceptStateObject(this);
		m_listenSocket.Listen(10);
		m_listenSocket.BeginAccept(new System.AsyncCallback(AcceptCallback), accso);
	}

	static void AcceptCallback(System.IAsyncResult ar)
	{
		var accso           = ar.AsyncState as AcceptStateObject;
		var self            = accso.connection;
		var listensocket    = accso.socket;

		try
		{
			var worksocket      = listensocket.EndAccept(ar);
			self.workSocket     = worksocket;
			self.SetConnectionEstablished();

			ReceiveChainStart(self, worksocket);
		}
		catch(System.Exception e)
		{
			Debug.LogWarning("Connection failed : " + e);
			self.SetConnectionFailed();
		}
	}

	private static void ReceiveChainStart(TGTCPServerConnection self, Socket socket)
	{
		var readso  = new ReceiveStateObject(self);
		socket.BeginReceive(readso.AllocateNewBuffer(), 0, readso.bufferSize, SocketFlags.None, ReceiveChainCallback, readso);
	}

	private static void ReceiveChainCallback(System.IAsyncResult ar)
	{
		Debug.Log("ReceiveChainCallback");

		var stateObject = ar.AsyncState as ReceiveStateObject;
		var socket      = stateObject.socket;
		var self        = stateObject.connection;

		try
		{
			var bytesRead   = socket.EndReceive(ar);
			if (bytesRead == 0)                     // if receives zero data, assume this is due to disconnection
			{
				Debug.LogWarning("disconnected!");
				self.PushErrorCode(ErrorCode.Disconnected);
			}
			else if (bytesRead == stateObject.bufferSize)    // continue another receive chain, because there might be more data incoming
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
		catch (System.Exception e)
		{
			Debug.LogWarning("cannot receive : " + e);
			self.PushErrorCode(ErrorCode.CannotReceive);
		}
	}

	protected override void KillConnectionImpl()
	{
		lock(m_lockObj)
		{
			if (m_workSocket.Connected)
				m_workSocket.Shutdown(SocketShutdown.Both);

			m_workSocket.Close();
			m_workSocket    = null;
		}

		m_listenSocket.Close();
		m_listenSocket		= null;
	}

	protected override void SendDataImpl(byte[] data)
	{

		var sendSO  = new SendStateObject(this);
		sendSO.socket.BeginSend(data, 0, data.Length, SocketFlags.None, SendCallback, sendSO);
	}

	private static void SendCallback(System.IAsyncResult ar)
	{
		var stateObject = ar.AsyncState as SendStateObject;
		var socket      = stateObject.socket;
		var self        = stateObject.connection;

		try
		{
			//var bytesSent   = socket.EndSend(ar);
			socket.EndSend(ar);
		}
		catch (System.Exception e)
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
