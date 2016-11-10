using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Module for protocol management
/// </summary>
public class TGProtocolModule : MonoBehaviour
{
	public abstract class BaseProcedure
	{
		public delegate void FuncDel(JSONObject param);
		public delegate void SendResponseDel(string type, string path, JSONObject param);

		public enum FunctionType
		{
			get,
			set,
			status,
			meta,
			value,
		}

		protected struct FuncIndexerImpl
		{
			public BaseProcedure proc;

			public FuncDel this[FunctionType type]
			{
				get
				{
					return proc.m_functionDict[type.ToString()];
				}
				set
				{
					proc.m_functionDict[type.ToString()] = value;
				}
			}

			public FuncDel this[string type]
			{
				get
				{
					return proc.m_functionDict[type];
				}
				set
				{
					proc.m_functionDict[type] = value;
				}
			}
		}


		// Members

		Dictionary<string, FuncDel>		m_functionDict  = new Dictionary<string, FuncDel>();
		SendResponseDel					m_respDel;

		/// <summary>
		/// shortcut for m_functionDict
		/// </summary>
		protected FuncIndexerImpl functions { get; private set; }
		
		

		public BaseProcedure()
		{
			functions	= new FuncIndexerImpl() { proc  = this };
		}

		public void SetResponseDelegate(SendResponseDel del)
		{
			m_respDel   = del;
		}
		//

		protected void RegisterFunction(FunctionType type, FuncDel del)
		{
			m_functionDict[type.ToString()]    = del;
		}

		protected void SendResponse(FunctionType type, string path, JSONObject param)
		{
			m_respDel(type.ToString(), path, param);
		}

		/// <summary>
		/// calls matching function in this procedure
		/// </summary>
		/// <param name="type"></param>
		/// <param name="param"></param>
		public void CallFunction(FunctionType type, JSONObject param)
		{
			CallFunction(type.ToString(), param);
		}

		/// <summary>
		/// calls matching function in this procedure
		/// </summary>
		/// <param name="type"></param>
		/// <param name="param"></param>
		public void CallFunction(string type, JSONObject param)
		{
			FuncDel func;
			if (!m_functionDict.TryGetValue(type, out func))
			{
				Debug.LogError("function not registered : " + type);
			}
			else
			{
				func(param);
			}
		}
	}
	//


	// Constants
	
	const string                c_keyApp			= "app";
	const string                c_keyName			= "name";
	const string                c_keyVersion		= "version";
	const string                c_keyType			= "type";
	const string                c_keyContent		= "context";
	const string                c_keyFunction		= "function";
	const string                c_keyPath			= "path";
	const string                c_keyData			= "data";

	const string                c_packTypeRequest   = "request";
	const string                c_packTypeResponse  = "response";
	const string                c_packTypeTrap      = "trap";
	const string                c_packTypeTrapAnswer= "trap-ack";

	// NOTE : these are more than just literals, should be categorized as something like global variables.
	const string                c_appVersion		= "1.0";
	const string                c_appNameClient		= "TWTL_GUI";
	const string                c_appNameServer		= "TWTL_Engine";
	const string                c_appName			= "TWTL";

	

	// Members

	struct ProcedureInfo
	{
		public BaseProcedure	procedure;
		public bool				isTrap;			// whether this procedure is for trap calls
	}

	Dictionary<string, ProcedureInfo>   m_procDict  = new Dictionary<string, ProcedureInfo>();



	public static TGProtocolModule instance { get; private set; }


	void Awake()
	{
		instance    = this;
	}
	
	void Start()
	{
		var comModule					= TGComModule.instance;
		comModule.reqMessageReceived	+= ProcessReceivedData;
		comModule.trapMessageReceived	+= ProcessReceivedData;
	}
	
	void Update()
	{

	}

	/// <summary>
	/// register a procedure obj for a path.
	/// </summary>
	/// <param name="path"></param>
	/// <param name="proc"></param>
	/// <param name="isTrap"></param>
	public void RegisterProcedure(string path, BaseProcedure proc, bool isTrap = false)
	{
		var info            = new ProcedureInfo()
		{
			procedure       = proc,
			isTrap          = isTrap,
		};
		m_procDict[path]    = info;
		proc.SetResponseDelegate((type, ppath, param) =>
		{
			var trap        = isTrap;
			ProcessSendingData(type, ppath, param, trap);
		});
	}

	private void ProcessReceivedData(string message)
	{
		JSONObject parsed   = null;
		try
		{
			parsed      = new JSONObject(message);
		}
		catch (System.Exception e)
		{
			Debug.LogErrorFormat("cannot parse json data : {0}, exception info : {1}", message, e.ToString());
		}

		if (parsed == null)                 // if cannot parse, just return
			return;

		// verification process
		bool veriFailed     = false;
		var app             = parsed[c_keyApp].str;
		var appname         = parsed[c_keyName].str;
		var version         = parsed[c_keyVersion].str;

		if (appname != c_appName)
		{
			Debug.LogError("received packet - app name mismatch!");
			veriFailed      = true;
		}
		if (app != c_appNameServer)
		{
			Debug.LogError("received packet - sender app is not the expected one");
			veriFailed      = true;
		}
		if (version != c_appVersion)
		{
			Debug.LogError("received packet - app version mismatch!");
			veriFailed      = true;
		}

		var type            = parsed[c_keyType].str;


		if (veriFailed)                     // ignore this response if not valid
			return;

		var content         = parsed[c_keyContent].list;
		var count           = content.Count;
		for (var i = 0; i < count; i++)		// process each function calls
		{
			var entry       = content[i];

			var func        = entry[c_keyFunction].str;
			var path        = entry[c_keyPath].str;
			var data        = entry[c_keyData];

			ProcedureInfo proc;
			if (!m_procDict.TryGetValue(path, out proc))
			{
				Debug.LogError("received packet - invalid path : " + path);
			}
			else if (!(!proc.isTrap && type == c_packTypeResponse || proc.isTrap && type == c_packTypeTrap))
			{
				Debug.LogError("receiced packet - invalid packet type for the context : " + type);
			}
			else
			{
				proc.procedure.CallFunction(func, data);			// calls actual function
			}
		}
	}

	private void ProcessSendingData(BaseProcedure.FunctionType type, string path, JSONObject param, bool isTrap)
	{
		ProcessSendingData(type.ToString(), path, param, isTrap);
	}
	
	private void ProcessSendingData(string type, string path, JSONObject param, bool isTrap)
	{
		var packed			= new JSONObject();
		packed.AddField(c_keyApp, c_appNameClient);
		packed.AddField(c_keyName, c_appName);
		packed.AddField(c_keyVersion, c_appVersion);

		packed.AddField(c_keyType, isTrap ? c_packTypeTrapAnswer : c_packTypeRequest);
		
		var callObj         = new JSONObject();
		callObj.AddField(c_keyFunction, type.ToString());
		callObj.AddField(c_keyPath, path);
		callObj.AddField(c_keyData, param);

		var content         = new JSONObject[] { callObj };
		packed.AddField(c_keyContent, new JSONObject(content));

		var message         = packed.ToString();
		var comModule       = TGComModule.instance;
		if (isTrap)
		{
			comModule.SendTrapResponse(message);
		}
		else
		{
			comModule.SendRequest(message);
		}
	}
}
