using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System;
using System.Collections.Generic;

/// <summary>
/// TCP implementation of TGBaseConnection
/// </summary>
public class TGTCPClientConnection : TGBaseTCPConnection<TGTCPClientConnection.ConnectionInfo>
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
			this.socket     = connection.workSocket;
		}
	}
	//



	// Members

	//Socket                  m_socket;				// socket for this connection
	protected override Socket workSocket
	{
		get; set;
	}


	protected override void MakeConnectionImpl(ConnectionInfo coninfo)
	{
		var endpoint        = new IPEndPoint(coninfo.hostIP, coninfo.port);
		workSocket          = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

		var conSo           = new ConnectStateObject(this);

		workSocket.BeginConnect(endpoint, ConnectCallback, conSo);
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
	
	protected override void KillConnectionImpl()
	{
		if (workSocket.Connected)
			workSocket.Shutdown(SocketShutdown.Both);
		workSocket.Close();
		workSocket    = null;
	}
}
