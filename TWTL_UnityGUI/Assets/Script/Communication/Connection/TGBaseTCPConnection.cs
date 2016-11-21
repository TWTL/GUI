using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

public abstract class TGBaseTCPConnection<InfoT> : TGBaseConnection<InfoT>
	where InfoT : TGBaseConnection<InfoT>.IConnectionInfo
{
	protected class SendStateObject
	{
		public TGBaseTCPConnection<InfoT>  connection;
		public Socket           socket;

		public SendStateObject(TGBaseTCPConnection<InfoT> connection)
		{
			this.connection = connection;
			this.socket     = connection.workSocket;
		}
	}

	/// <summary>
	/// state object used within receive operation
	/// </summary>
	protected class ReceiveStateObject
	{
		public TGBaseTCPConnection<InfoT>    connection;
		public Socket           socket;

		ReceiveBuffer           recvBuffer;

		public int bufferSize
		{
			get { return recvBuffer.bufferSize; }
		}

		public ReceiveStateObject(TGBaseTCPConnection<InfoT> connection)
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


	// Members

	protected abstract Socket workSocket { get; set; }


	protected static void ReceiveChainStart(TGBaseTCPConnection<InfoT> self, Socket socket)
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

	protected override void SendDataImpl(byte[] data)
	{

		var sendSO  = new SendStateObject(this);
		sendSO.socket.BeginSend(data, 0, data.Length, SocketFlags.None, SendCallback, sendSO);
	}

	protected static void SendCallback(System.IAsyncResult ar)
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
