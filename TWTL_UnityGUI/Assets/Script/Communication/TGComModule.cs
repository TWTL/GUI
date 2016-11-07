using UnityEngine;
using System.Collections;


/// <summary>
/// singleton module for communcation between engine-gui
/// </summary>
public class TGComModule : BaseSingleton<TGComModule>
{
	// Properties
	[SerializeField]
	string          m_serverAddress = "127.0.0.1";
	[SerializeField]
	int             m_portNumber	= 12345;

	/// <summary>
	/// TEMP
	/// </summary>
	string localAddress
	{
		get
		{
			return "127.0.0.1";
		}
	}



	// Members
	TGTCPClientConnection	m_reqConnection;
	TGTCPServerConnection   m_trapConnection;


	public event System.Action<string> reqMessageReceived;
	public event System.Action<string> trapMessageReceived;

	
	protected override void Initialize()
	{
		base.Initialize();

		m_reqConnection		= new TGTCPClientConnection();
		m_trapConnection    = new TGTCPServerConnection();
	}

	void Start()
	{
		// TEST
		StartRoutine();
	}

	/// <summary>
	/// 
	/// </summary>
	public void StartRoutine()
	{
		StartCoroutine(co_MainCycle());
	}

	IEnumerator co_MainCycle()
	{
		// Establishing Connection (Request)

		var conInfo = new TGTCPClientConnection.ConnectionInfo() { hostIP = System.Net.IPAddress.Parse(m_serverAddress), port = m_portNumber };
		m_reqConnection.Make(conInfo);

		while (m_reqConnection.connectionStatus == TGTCPClientConnection.ConnectionStatus.Waiting)    // pending until connection state changes
			yield return null;

		if (m_reqConnection.connectionStatus == TGTCPClientConnection.ConnectionStatus.None)
		{
			if (m_reqConnection.PollError() == TGTCPClientConnection.ErrorCode.CannotConnect)
			{
				Debug.LogWarning("cannot connect to server!");
			}
			else
			{
				Debug.LogWarning("uh.... something is strange...");
			}

			yield break;
		}

		// Establishing Connection (Trap)

		Debug.Log("start listening trap connection");
		var trConInfo   = new TGTCPServerConnection.ConnectionInfo() { hostIP = System.Net.IPAddress.Parse(localAddress) };
		trConInfo.AllocateAvailablePort();
		m_trapConnection.Make(trConInfo);

		Debug.Log("sending port number : " + trConInfo.port);
		// notice server the trap socket's port number
		m_reqConnection.Send(System.Text.Encoding.UTF8.GetBytes(trConInfo.port.ToString()));
		
		while (m_trapConnection.connectionStatus == TGTCPServerConnection.ConnectionStatus.Waiting)    // pending until connection state changes
		{
			m_reqConnection.Update();
			m_trapConnection.Update();
			yield return null;
		}

		if (m_trapConnection.connectionStatus == TGTCPServerConnection.ConnectionStatus.None)
		{
			if (m_trapConnection.PollError() == TGTCPServerConnection.ErrorCode.CannotConnect)
			{
				Debug.LogWarning("cannot accept connection from server!");
			}
			else
			{
				Debug.LogWarning("uh.... something is strange...");
			}

			yield break;
		}



		Debug.Log("connection established!");

		// Typical receive / send loop

		while (true)
		{
			var reqerror    = m_reqConnection.PollError();
			var traperror   = m_trapConnection.PollError();
			var errorRecv   = false;

			if (reqerror != TGTCPClientConnection.ErrorCode.NoError)
			{
				Debug.LogWarning("request socket error : " + reqerror.ToString());
				errorRecv   = true;
			}

			if (traperror != TGTCPServerConnection.ErrorCode.NoError)
			{
				Debug.LogWarning("trap socket error : " + traperror.ToString());
				errorRecv   = true;
			}

			if (errorRecv)
			{
				m_reqConnection.Kill();
				m_trapConnection.Kill();
				continue;

				// TODO : process after disconnection
			}

			// req connection
			{
				m_reqConnection.Update();

				var read    = m_reqConnection.PollData();
				if (read != null)
				{
					var text    = System.Text.Encoding.UTF8.GetString(read, 0, read.Length);
					//Debug.Log("received (req) : " + text);

					if (reqMessageReceived != null)
						reqMessageReceived(text);
					//m_reqConnection.Send(System.Text.Encoding.UTF8.GetBytes("I sent something to you! " + Random.Range(1, 10000)));
				}
			}

			// trap connection
			{
				m_trapConnection.Update();

				var read    = m_trapConnection.PollData();
				if (read != null)
				{
					var text    = System.Text.Encoding.UTF8.GetString(read, 0, read.Length);
					//Debug.Log("received (trap) : " + text);

					if (trapMessageReceived != null)
						trapMessageReceived(text);
					//m_trapConnection.Send(System.Text.Encoding.UTF8.GetBytes("I sent something to you! " + Random.Range(1, 10000)));
				}
			}

			yield return null;
		}
	}

	void OnDestroy()
	{
		Debug.Log("TGComModule quit");
		m_reqConnection.Kill();
		m_trapConnection.Kill();
	}


	public void SendRequest(string message)
	{
		m_reqConnection.Send(System.Text.Encoding.UTF8.GetBytes(message));
	}

	public void SendTrapResponse(string message)
	{
		m_trapConnection.Send(System.Text.Encoding.UTF8.GetBytes(message));
	}
}
