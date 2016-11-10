using UnityEngine;
using System.Collections;

public class TGCommunicationConsole : MonoBehaviour
{
	// Members

	bool            m_enableConsole = false;
	string          m_buffer        = "";
	string          m_currentline   = "";

	void Awake()
	{
		if (!Debug.isDebugBuild)				// disable if the build is in release mode
		{
			enabled = false;
			return;
		}

		Application.logMessageReceived += ProcessLogMessage;
	}

	void Start()
	{
		if (!Debug.isDebugBuild)
			return;

		var comModule   = TGComModule.instance;
		comModule.reqMessageReceived += (message) =>
		{
			PushMessage("recv : " + message, Color.cyan);
		};
		comModule.trapMessageReceived += (message) =>
		{
			PushMessage("trap : " + message, Color.magenta);
		};
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Slash))
		{
			m_enableConsole = !m_enableConsole;
		}
	}

	void OnGUI()
	{
		if (m_enableConsole)
		{
			GUI.Box(new Rect(0, 0, 600, 600), "Communication Console");

			var tfstyle			= GUI.skin.GetStyle("TextField");
			tfstyle.alignment   = TextAnchor.LowerLeft;
			tfstyle.richText    = true;
			GUI.TextArea(new Rect(10, 15, 580, 550), m_buffer, tfstyle);

			m_currentline		= GUI.TextField(new Rect(10, 570, 580, 20), m_currentline);

			// response to "enter key" press
			var ev              = Event.current;
			if (ev.type == EventType.KeyDown && ev.character == '\n')
			{
				ProcessCommandLine(m_currentline);
				m_currentline   = "";
			}
		}
		else
		{
			GUI.Label(new Rect(10, 0, 500, 50), "/ : communication console");
		}
	}

	void ProcessLogMessage(string logString, string stackTrace, LogType type)
	{
		Color color;
		switch(type)
		{
			case LogType.Warning:
				color   = Color.yellow;
				break;

			case LogType.Exception:
			case LogType.Assert:
			case LogType.Error:
				color   = Color.red;
				break;

			case LogType.Log:
			default:
				color   = Color.white;
				break;
		}

		PushMessage(logString, color);
	}

	static readonly char [] c_splitChars = new char[] { ' ' };
	void ProcessCommandLine(string command)
	{
		PushMessage(command, Color.gray);

		var split   = command.Split(c_splitChars, 2);
		switch (split[0].Trim())
		{
			case "request":
				TGComModule.instance.SendRequest(split[1].Trim());
				break;

			case "response":
				TGComModule.instance.SendTrapResponse(split[1].Trim());
				break;

			default:
				TGComModule.instance.SendRequest(command);
				break;
		}
	}

	void PushMessage(string message)
	{
		PushMessage(message, Color.white);
	}

	void PushMessage(string message, Color color)
	{
		var formatstr   = (color == Color.white)? "\n{0}" : string.Format("\n<color={0}>{{0}}</color>", ColorToHex(color));
		m_buffer		+= string.Format(formatstr, message);
	}


	const string c_hexChars = "0123456789ABCDEF";
	string ColorToHex(Color color)
	{
		var r   = (int)(color.r * 255f);
		var g   = (int)(color.g * 255f);
		var b	= (int)(color.b * 255f);

		return string.Format("#{0}{1}{2}{3}{4}{5}",
			c_hexChars[r / 16], c_hexChars[r % 16],
			c_hexChars[g / 16], c_hexChars[g % 16],
			c_hexChars[b / 16], c_hexChars[b % 16]);
	}
}
