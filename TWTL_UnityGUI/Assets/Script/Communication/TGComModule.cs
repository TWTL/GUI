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



	// Members
	TGTCPConnection     m_connection;

	
	protected override void Initialize()
	{
		base.Initialize();

		m_connection    = new TGTCPConnection();
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
		// Establishing Connection

		var conInfo = new TGTCPConnection.ConnectionInfo() { hostIP = System.Net.IPAddress.Parse(m_serverAddress), port = m_portNumber };
		m_connection.Make(conInfo);

		while (m_connection.connectionStatus == TGTCPConnection.ConnectionStatus.Trying)    // pending until connection state changes
			yield return null;

		if (m_connection.connectionStatus == TGTCPConnection.ConnectionStatus.None)
		{
			if (m_connection.PollError() == TGTCPConnection.ErrorCode.CannotConnect)
			{
				Debug.LogWarning("cannot connect!");
			}
			else
			{
				Debug.LogWarning("uh.... something is strange...");
			}

			yield break;
		}

		// Typical receive / send loop

		while(true)
		{
			if (m_connection.PollError() != TGTCPConnection.ErrorCode.NoError)
			{
				m_connection.Kill();
				continue;
			}

			m_connection.Update();

			var read    = m_connection.PollData();
			if (read != null)
			{
				var text	= System.Text.Encoding.UTF8.GetString(read, 0, read.Length);
				Debug.Log("received : " + text);

				m_connection.Send(System.Text.Encoding.UTF8.GetBytes("I sent something to you! " + Random.Range(1, 10000)));
			}
			
			yield return null;
		}
	}

	void OnDestroy()
	{
		Debug.Log("TGComModule quit");
		m_connection.Kill();
	}
}
