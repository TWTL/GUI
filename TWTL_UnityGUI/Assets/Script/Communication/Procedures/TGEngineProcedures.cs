using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public static class TGEngineProcedures
{
	public class Name : TGProtocolModule.BaseProcedure
	{
		public override string procedurePath
		{
			get
			{
				return "/Engine/Name/";
			}
		}

		public Name()
		{
			RegisterFunction(FunctionType.status, (param) =>
			{
				Debug.Log(procedurePath + " status : " + (int)param.n);
			});

			RegisterFunction(FunctionType.obj, (param) =>
			{
				Debug.Log(procedurePath + " object : " + param.str);

				// TEST sequence
				//procVersion.SimpleRequestGet();
			});
		}

		protected override bool OnCallFinish()
		{
			Debug.Log(procedurePath + " OnCallFinish()");
			return true;
		}
	}

	public class Version : TGProtocolModule.BaseProcedure
	{
		public override string procedurePath
		{
			get
			{
				return "/Engine/Version/";
			}
		}

		public Version()
		{
			RegisterFunction(FunctionType.status, (param) =>
			{
				Debug.Log(procedurePath + " status : " + (int)param.n);
			});

			RegisterFunction(FunctionType.obj, (param) =>
			{
				Debug.Log(procedurePath + " object : " + param.str);

				// TEST sequence
				//procRequestPort.SimpleRequestGet();
			});
		}

		protected override bool OnCallFinish()
		{
			Debug.Log(procedurePath + " OnCallFinish()");
			return true;
		}
	}

	public class RequestPort : TGProtocolModule.BaseProcedure
	{
		public override string procedurePath
		{
			get
			{
				return "/Engine/RequestPort/";
			}
		}

		public RequestPort()
		{
			RegisterFunction(FunctionType.status, (param) =>
			{
				Debug.Log(procedurePath + " status : " + (int)param.n);
			});

			RegisterFunction(FunctionType.obj, (param) =>
			{
				Debug.Log(procedurePath + " object : " + param.str);

				// TEST sequence
				//procTrapPort.RequestSetPort(TGComModule.instance.trapPort);
			});
		}

		protected override bool OnCallFinish()
		{
			Debug.Log(procedurePath + " OnCallFinish()");
			return true;
		}
	}

	public class TrapPort : TGProtocolModule.BaseProcedure
	{
		public override string procedurePath
		{
			get
			{
				return "/Engine/TrapPort/";
			}
		}

		public TrapPort()
		{
			RegisterFunction(FunctionType.status, (param) =>
			{
				Debug.Log(procedurePath + " status : " + (int)param.n);
			});
		}

		protected override bool OnCallFinish()
		{
			Debug.Log(procedurePath + " OnCallFinish()");
			return true;
		}

		public void RequestSetPort(int portnum)
		{
			SendMessage(FunctionType.set, new JSONObject(portnum));
		}
	}
	//

	public class InitChain : TGProtocolModule.BaseProcedureChain
	{
		public InitChain()
		{
			AddChainee("name", procName);
			AddChainee("version", procVersion);
			AddChainee("requestport", procRequestPort);
			AddChainee("trapport", procTrapPort);
		}

		protected override void OnStartingChain(string chainName, IChainee chain)
		{
			procName.SimpleRequestGet();
		}

		protected override void OnChaineeResult(string name, IChainee chainee, bool result)
		{
			switch (name)
			{
				case "name":
					procVersion.SimpleRequestGet();
					break;

				case "version":
					procRequestPort.SimpleRequestGet();
					break;

				case "requestport":
					procTrapPort.RequestSetPort(TGComModule.instance.trapPort);
					break;

				case "trapport":

					break;
			}
		}
	}


	// Members

	public static Name procName { get; private set; }
	public static Version procVersion { get; private set; }
	public static RequestPort procRequestPort { get; private set; }
	public static TrapPort procTrapPort { get; private set; }
	
	static TGEngineProcedures()
	{
		procName        = new Name();
		procVersion     = new Version();
		procRequestPort = new RequestPort();
		procTrapPort    = new TrapPort();
	}
	
	public static void RegisterProcedures()
	{
		var procModule  = TGProtocolModule.instance;

		procModule.RegisterProcedure(procName);
		procModule.RegisterProcedure(procVersion);
		procModule.RegisterProcedure(procRequestPort);
		procModule.RegisterProcedure(procTrapPort);
	}
}
