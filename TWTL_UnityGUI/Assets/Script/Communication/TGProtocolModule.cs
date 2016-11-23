using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Module for protocol management
/// </summary>
public class TGProtocolModule : MonoBehaviour
{
	/// <summary>
	/// class for a procedure chain
	/// </summary>
	public abstract class BaseProcedureChain
	{
		public delegate void ChainResultDel(bool result);

		/// <summary>
		/// interface that can be chained by BaseProcedureChain
		/// </summary>
		public interface IChainee
		{
			bool ReadyForChain(BaseProcedureChain chain);
			bool ReleaseFromChain(BaseProcedureChain chain);

			bool BeginChain(BaseProcedureChain chain, ChainResultDel resultDel);
			bool FinishChain(BaseProcedureChain chain);
		}
		//

		// Members

		struct ChaineeInfo
		{
			public IChainee	chainee;
			public string	name;
		}

		List<ChaineeInfo>	m_chainees	= new List<ChaineeInfo>();
		int					m_nextChainIndex;



		private bool AcquireChainees()
		{
			var	fullSuccess = false;
			var count       = m_chainees.Count;
			var i			= 0;
			var needRevert  = false;

			for (i = 0; i < count; i++)				// try to make all chainees ready one by one
			{
				if (!m_chainees[i].chainee.ReadyForChain(this))
				{
					needRevert  = true;
					break;
				}
			}

			if (needRevert)							// if one of the chainees cannot be ready, then release every chainees come before this
			{
				for (var ri	= 0; ri < i; ri++)
				{
					m_chainees[ri].chainee.ReleaseFromChain(this);
				}
			}
			else
			{
				fullSuccess = true;
			}

			return fullSuccess;
		}

		private void ReleaseChainees()
		{
			var count       = m_chainees.Count;
			for (var i = 0; i < count; i++)
			{
				m_chainees[i].chainee.ReleaseFromChain(this);
			}
		}

		public void StartChain()
		{
			if (!AcquireChainees())
			{
				Debug.LogError("cannot acquire procedure chain");
			}
			else
			{
				m_nextChainIndex    = 0;
				NextChain();
			}
		}

		protected void AddChainee(string name, IChainee chainee)
		{
			m_chainees.Add(new ChaineeInfo() { name = name, chainee = chainee });
		}

		protected void NextChain()
		{
			var isFirstOne  = m_nextChainIndex == 0;
			if (!isFirstOne)							// if available, call finish for previous chainee
			{
				if (!m_chainees[m_nextChainIndex - 1].chainee.FinishChain(this))
				{
					Debug.LogWarning("cannot finish a chainee... something's wrong here!");
				}
			}

			var count       = m_chainees.Count;
			var chInfo      = m_chainees[m_nextChainIndex++];
			var isLastOne   = m_nextChainIndex >= count;
			chInfo.chainee.BeginChain(this, (success) =>
			{
				if (isLastOne || !success)				// if this chainee is last one or result was not success, release all chain
				{
					chInfo.chainee.FinishChain(this);
					ReleaseChainees();
				}
				else
				{										// ...or keep running
					NextChain();
				}

				OnChaineeResult(chInfo.name, chInfo.chainee, success);
			});

			if (isFirstOne)
			{
				OnStartingChain(chInfo.name, chInfo.chainee);
			}
		}

		protected abstract void OnStartingChain(string chainName, IChainee chainee);
		protected abstract void OnChaineeResult(string name, IChainee chainee, bool result);
	}

	public abstract class BaseProcedure : BaseProcedureChain.IChainee
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
			obj,	// object

			diff,
			patch,
		}

		/// <summary>
		/// we need this instead of FunctionType.ToString() because of the limitation of c# enum. ("object" cannot be a member of an enum)
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string ConvertFunctionTypeEnum(FunctionType type)
		{
			return type == FunctionType.obj ? "object" : type.ToString();
		}
		//

		protected struct FuncIndexerImpl
		{
			public BaseProcedure proc;

			public FuncDel this[FunctionType type]
			{
				get
				{
					return proc.m_functionDict[ConvertFunctionTypeEnum(type)];
				}
				set
				{
					proc.m_functionDict[ConvertFunctionTypeEnum(type)] = value;
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
		SendResponseDel					m_sendDel;

		BaseProcedureChain					m_chain;            // null if no chain acquired this
		BaseProcedureChain.ChainResultDel   m_chainResultDel;

		/// <summary>
		/// shortcut for m_functionDict
		/// </summary>
		protected FuncIndexerImpl functions { get; private set; }

		public abstract string procedurePath { get; }
		
		

		public BaseProcedure()
		{
			functions	= new FuncIndexerImpl() { proc  = this };
		}

		public void SetMessageDelegate(SendResponseDel del)
		{
			m_sendDel   = del;
		}
		//

		protected void RegisterFunction(FunctionType type, FuncDel del)
		{
			m_functionDict[ConvertFunctionTypeEnum(type)]    = del;
		}

		protected void SendMessage(FunctionType type, JSONObject param)
		{
			if (m_chain != null && m_chainResultDel == null)	// if it's acquired from a chain but not get ready for a chain call, it's an error situation
			{
				Debug.LogError("Procedure acquired from a chain but not ready to call SendMessage");
			}
			else
			{
				m_sendDel(ConvertFunctionTypeEnum(type), procedurePath, param);
			}
		}

		/// <summary>
		/// calls matching function in this procedure
		/// </summary>
		/// <param name="type"></param>
		/// <param name="param"></param>
		public void CallFunction(FunctionType type, JSONObject param)
		{
			CallFunction(ConvertFunctionTypeEnum(type), param);
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

		public void FinishOneCall()
		{
			var result  = OnCallFinish();

			if (m_chain != null && m_chainResultDel == null)
			{
				Debug.LogError("chain error : the chainee should not be finished this time");
			}
			else if (m_chainResultDel != null)
			{
				Debug.Log("FinishOnecall : " + procedurePath);
				m_chainResultDel(result);
			}
		}

		/// <summary>
		/// calls when one response process is end
		/// </summary>
		/// <returns>true if response is valid and successful, false otherwise</returns>
		protected abstract bool OnCallFinish();
		//

		/// <summary>
		/// utility - simply sends get request
		/// </summary>
		public void SimpleRequestGet()
		{
			SendMessage(FunctionType.get, null);
		}


		// chain functions

		public bool ReadyForChain(BaseProcedureChain chain)
		{
			if (m_chain != null)
				return false;

			m_chain = chain;
			return true;
		}

		public bool ReleaseFromChain(BaseProcedureChain chain)
		{
			if (m_chain != chain)
				return false;

			m_chain				= null;
			m_chainResultDel    = null;
			return true;
		}

		public bool BeginChain(BaseProcedureChain chain, BaseProcedureChain.ChainResultDel resultDel)
		{
			if (m_chain != chain)
				return false;

			if (m_chainResultDel != null)
				return false;

			m_chainResultDel    = resultDel;
			return true;
		}

		public bool FinishChain(BaseProcedureChain chain)
		{
			if (m_chain != chain)
				return false;

			if (m_chainResultDel == null)
				return false;

			m_chainResultDel    = null;
			return true;
		}
		//
	}
	//


	// Constants
	
	const string                c_keyApp			= "app";
	const string                c_keyName			= "name";
	const string                c_keyVersion		= "version";
	//const string                c_keyType			= "type";
	const string                c_keyContent		= "contents";
	const string                c_keyFunction		= "type";
	const string                c_keyPath			= "path";
	const string                c_keyData			= "value";

	const string                c_packTypeRequest   = "request";
	const string                c_packTypeResponse  = "response";
	const string                c_packTypeTrap      = "trap";
	const string                c_packTypeTrapAnswer= "trap-ack";

	// NOTE : these are more than just literals, should be categorized as something like global variables.
	const string                c_appVersion		= "1";
	const string                c_appNameClient		= "TWTL-GUI";
	const string                c_appNameServer		= "TWTL-Engine";
	const string                c_appName			= "TWTL";

	

	// Members

	struct ProcedureInfo
	{
		public BaseProcedure	procedure;
		public bool				isTrap;			// whether this procedure is for trap calls
	}

	Dictionary<string, ProcedureInfo>   m_procDict  = new Dictionary<string, ProcedureInfo>();
	Queue<string>                       m_trapMsgQueue  = new Queue<string>();
	int                                 m_responseWaitingCount  = 0;


	public static TGProtocolModule instance { get; private set; }


	void Awake()
	{
		instance    = this;
	}
	
	void Start()
	{
		var comModule					= TGComModule.instance;
		comModule.reqMessageReceived	+= ProcessReceivedResponseData;
		comModule.trapMessageReceived	+= ProcessReceivedTrapData;
	}
	
	void Update()
	{
		if (m_responseWaitingCount == 0)
		{
			while (m_trapMsgQueue.Count > 0)
			{
				var msg = m_trapMsgQueue.Dequeue();
				Debug.Log("Trap execution! : " + msg);
				ProcessReceivedData(msg);
			}
		}
	}

	/// <summary>
	/// register a procedure obj for a path.
	/// </summary>
	/// <param name="path"></param>
	/// <param name="proc"></param>
	/// <param name="isTrap"></param>
	public void RegisterProcedure(BaseProcedure proc, bool isTrap = false)
	{
		var info            = new ProcedureInfo()
		{
			procedure       = proc,
			isTrap          = isTrap,
		};
		m_procDict[proc.procedurePath]    = info;
		proc.SetMessageDelegate((type, ppath, param) =>
		{
			var trap        = isTrap;
			ProcessSendingData(type, ppath, param, trap);
		});
	}

	private void ProcessReceivedResponseData(string message)
	{
		ProcessReceivedData(message);
		m_responseWaitingCount--;           // decrease response counter
	}

	private void ProcessReceivedTrapData(string message)
	{
		m_trapMsgQueue.Enqueue(message);
	}

	private void ProcessReceivedData(string message)
	{
		//Debug.Log("ProcessReceivedData : " + message);

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
			Debug.LogWarning("received packet - sender app is not the expected one");
			//veriFailed      = true;
		}
		if (version != c_appVersion)
		{
			Debug.LogWarning("received packet - app version mismatch!");
			//veriFailed      = true;
		}

		//var type            = parsed[c_keyType].str;


		if (veriFailed)                     // ignore this response if not valid
			return;

		var content         = parsed[c_keyContent].list;
		var count           = content.Count;
		ProcedureInfo	proc = new ProcedureInfo();	// restriction - only one path in one response
		for (var i = 0; i < count; i++)		// process each function calls
		{
			var entry       = content[i];
			var funcsplit	= entry[c_keyFunction].str.Split('.');

			var type        = funcsplit[0];
			var func        = funcsplit[1];
			var path        = entry[c_keyPath].str;
			var data        = entry.HasField(c_keyData)? entry[c_keyData] : null;
			
			if (!m_procDict.TryGetValue(path, out proc))														// path validation
			{
				Debug.LogError("received packet - invalid path : " + path);
			}
			else if (!(!proc.isTrap && type == c_packTypeResponse || proc.isTrap && type == c_packTypeTrap))	// func type validation (response or trap)
			{
				Debug.LogError("receiced packet - invalid packet type for the context : " + type);
			}
			else
			{
				proc.procedure.CallFunction(func, data);														// calls actual function
			}
		}

		proc.procedure.FinishOneCall();		// end call of one response
	}

	private void ProcessSendingData(BaseProcedure.FunctionType type, string path, JSONObject param, bool isTrap)
	{
		ProcessSendingData(BaseProcedure.ConvertFunctionTypeEnum(type), path, param, isTrap);
	}
	
	private void ProcessSendingData(string type, string path, JSONObject param, bool isTrap)
	{
		var packed			= new JSONObject();
		packed.AddField(c_keyApp, c_appNameClient);
		packed.AddField(c_keyName, c_appName);
		packed.AddField(c_keyVersion, c_appVersion);

		//packed.AddField(c_keyType, isTrap ? c_packTypeTrapAnswer : c_packTypeRequest);
		
		var callObj         = new JSONObject();
		callObj.AddField(c_keyFunction, string.Format("{0}.{1}", isTrap ? c_packTypeTrapAnswer : c_packTypeRequest, type));
		callObj.AddField(c_keyPath, path);
		callObj.AddField(c_keyData, param);

		var content         = new JSONObject[] { callObj };
		packed.AddField(c_keyContent, new JSONObject(content));

		var message         = packed.ToString(true); // TEST : pretty
		var comModule       = TGComModule.instance;
		if (isTrap)
		{
			comModule.SendTrapResponse(message);
		}
		else
		{
			m_responseWaitingCount++;       // increase response counter - this prevents trap to be processed before all requests are processed

			comModule.SendRequest(message);
		}
	}

	public BaseProcedure GetProcedureObject(string path)
	{
		return m_procDict[path].procedure;
	}
}
