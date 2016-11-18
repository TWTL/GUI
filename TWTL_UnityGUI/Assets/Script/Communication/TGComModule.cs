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
	TGTCPClientConnection				m_reqConnection;
	TGTCPServerConnection				m_trapConnection;
	
	public event System.Action<string>	reqMessageReceived;
	public event System.Action<string>	trapMessageReceived;


	/// <summary>
	/// auto-generated trap socket port number
	/// </summary>
	public int trapPort { get; private set; }

	public enum Status
	{
		NotConnected,
		Connecting,
		RequestChannelOpen,
		TrapChannelConnecting,
		FullChannelOpen,
		Closed,

		ConnectionFailed = -1,
	}

	public Status status { get; private set; }


	protected override void Initialize()
	{
		base.Initialize();

		m_reqConnection		= new TGTCPClientConnection();
		m_trapConnection    = new TGTCPServerConnection();
	}

	void Start()
	{

	}
	
	public void StartRequestConnection(System.Action<Status> completeDel)
	{
		StartCoroutine(co_MakeRequestConnection(completeDel));
	}

	public void StartTrapConnection(System.Action<Status> completeDel)
	{
		StartCoroutine(co_MakeTrapConnection(completeDel));
	}

	IEnumerator co_MakeRequestConnection(System.Action<Status> completeDel)
	{
		if (status != Status.NotConnected)
		{
			Debug.LogError("cannot make request connection - wrong TGComModule status : " + status);
			yield break;
		}

		Debug.Log("Start request channel...");

		var conInfo = new TGTCPClientConnection.ConnectionInfo() { hostIP = System.Net.IPAddress.Parse(m_serverAddress), port = m_portNumber };
		m_reqConnection.Make(conInfo);

		status      = Status.Connecting;

		while (m_reqConnection.connectionStatus == TGTCPClientConnection.ConnectionStatus.Waiting)    // pending until connection state changes
			yield return null;

		if (m_reqConnection.connectionStatus == TGTCPClientConnection.ConnectionStatus.None)
		{
			if (m_reqConnection.PollError() == TGTCPClientConnection.ErrorCode.CannotConnect)
			{
				Debug.LogWarning("cannot connect to server!");
				status  = Status.ConnectionFailed;
			}
			else
			{
				Debug.LogWarning("uh.... something is strange...");
				status  = Status.ConnectionFailed;
			}
		}
		else
		{
			Debug.Log("request connection established!");
			status  = Status.RequestChannelOpen;
		}

		completeDel(status);

		StartCoroutine(co_RequestSocketCycle());
	}

	IEnumerator co_MakeTrapConnection(System.Action<Status> completeDel)
	{
		if (status != Status.RequestChannelOpen)
		{
			Debug.LogError("cannot make request connection - wrong TGComModule status : " + status);
			yield break;
		}

		Debug.Log("start listening trap connection");
		var trConInfo   = new TGTCPServerConnection.ConnectionInfo() { hostIP = System.Net.IPAddress.Parse(localAddress) };
		trConInfo.AllocateAvailablePort();
		m_trapConnection.Make(trConInfo);

		status          = Status.TrapChannelConnecting;

		trapPort        = trConInfo.port;
		Debug.Log("trao port number assigned : " + trConInfo.port);

		//// notice server the trap socket's port number
		//m_reqConnection.Send(System.Text.Encoding.UTF8.GetBytes(trConInfo.port.ToString()));

		while (m_trapConnection.connectionStatus == TGTCPServerConnection.ConnectionStatus.Waiting)    // pending until connection state changes
		{
			//m_reqConnection.Update();
			m_trapConnection.Update();
			yield return null;
		}

		if (m_trapConnection.connectionStatus == TGTCPServerConnection.ConnectionStatus.None)
		{
			if (m_trapConnection.PollError() == TGTCPServerConnection.ErrorCode.CannotConnect)
			{
				Debug.LogWarning("cannot accept connection from server!");
				status      = Status.ConnectionFailed;
			}
			else
			{
				Debug.LogWarning("uh.... something is strange...");
				status      = Status.ConnectionFailed;
			}
		}
		else
		{
			Debug.Log("full connection established!");
			status          = Status.FullChannelOpen;
		}
		
		completeDel(status);

		StartCoroutine(co_TrapSocketCycle());
	}

	IEnumerator co_RequestSocketCycle()
	{
		Debug.Log("start co_RequestSocketCycle()");

		while (status >= Status.RequestChannelOpen && status < Status.Closed)
		{
			var reqerror    = m_reqConnection.PollError();
			var errorRecv   = false;

			if (reqerror != TGTCPClientConnection.ErrorCode.NoError)
			{
				Debug.LogWarning("request socket error : " + reqerror.ToString());
				errorRecv   = true;
			}

			if (errorRecv)
			{
				CloseConnection();
				continue;
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

			yield return null;
		}
	}

	IEnumerator co_TrapSocketCycle()
	{
		Debug.Log("start co_TrapSocketCycle()");

		while (status == Status.FullChannelOpen)
		{
			var traperror   = m_trapConnection.PollError();
			var errorRecv   = false;

			if (traperror != TGTCPServerConnection.ErrorCode.NoError)
			{
				Debug.LogWarning("trap socket error : " + traperror.ToString());
				errorRecv   = true;
			}

			if (errorRecv)
			{
				CloseConnection();
				continue;
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

	public void CloseConnection()
	{
		m_reqConnection.Kill();
		m_trapConnection.Kill();

		status      = Status.Closed;
	}

	void OnDestroy()
	{
		Debug.Log("TGComModule quit");
		CloseConnection();
	}


	public void SendRequest(string message)
	{
		if (m_reqConnection.connectionStatus != TGTCPClientConnection.ConnectionStatus.Established)
		{
			Debug.LogWarning("cannot send request : the socket is not established");
		}
		else
		{
			m_reqConnection.Send(System.Text.Encoding.UTF8.GetBytes(message));
		}
	}

	public void SendTrapResponse(string message)
	{
		if (m_trapConnection.connectionStatus != TGTCPServerConnection.ConnectionStatus.Established)
		{
			Debug.LogWarning("cannot send request : the socket is not established");
		}
		else
		{
			m_trapConnection.Send(System.Text.Encoding.UTF8.GetBytes(message));
		}
	}
}
