using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

public class TGTCPServerConnection : TGBaseTCPConnection<TGTCPServerConnection.ConnectionInfo>
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
	//



	// Members

	Socket                  m_listenSocket;
	Socket                  m_workSocket;               // socket for this connection

	object                  m_lockObj   = new object();


	protected override Socket workSocket
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
}
