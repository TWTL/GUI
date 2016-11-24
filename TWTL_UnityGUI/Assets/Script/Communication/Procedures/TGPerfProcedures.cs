using UnityEngine;
using System.Collections;
using System;

public static class TGPerfProcedures
{
	public class PerfRegisterAutoKill : TGProtocolModule.BaseProcedure
	{
		public override string procedurePath
		{
			get
			{
				return "/Perf/RegisterAutoKill/";
			}
		}

		public PerfRegisterAutoKill()
		{
			RegisterFunction(FunctionType.status, (JSONObject param) =>
			{

			});
		}

		protected override bool OnCallFinish()
		{
			return true;
		}

		public void RequestPut(string imagePath)
		{
			var param   = new JSONObject();
			param.AddField("ProcessImagePath", imagePath);

			SendMessage(FunctionType.put, param);
		}
	}

	public class PerfRemoveExecImage : TGProtocolModule.BaseProcedure
	{
		public override string procedurePath
		{
			get
			{
				return "/Perf/RemoveExecImage/";
			}
		}

		public PerfRemoveExecImage()
		{
			RegisterFunction(FunctionType.status, (param) =>
			{

			});
		}

		protected override bool OnCallFinish()
		{
			return true;
		}

		public void RequestSet(string imagePath)
		{
			var param   = new JSONObject();
			param.AddField("ProcessImagePath", imagePath);

			SendMessage(FunctionType.set, param);
		}
	}

	public class PerfResolveImagePath : TGProtocolModule.BaseProcedure
	{
		/// <summary>
		/// the last result returned as response of request.beta.
		/// </summary>
		public string lastResult { get; private set; }

		public override string procedurePath
		{
			get
			{
				return "/Perf/ResolveImagePath/";
			}
		}

		public PerfResolveImagePath()
		{
			RegisterFunction(FunctionType.status, (param) =>
			{

			});

			RegisterFunction(FunctionType.obj, (param) =>
			{
				lastResult  = param["ImagePath"].str;
			});
		}

		protected override bool OnCallFinish()
		{
			return true;
		}

		public void RequestBeta(string origPath)
		{
			lastResult  = null;
			var param   = new JSONObject();
			param.AddField("ImagePath", origPath);

			SendMessage(FunctionType.beta, param);
		}
	}
	//


	public class RegisterAutoKillChain : TGProtocolModule.BaseProcedureChain
	{
		string      m_origImagePath;

		public RegisterAutoKillChain(string imagePath)
		{
			AddChainee("resolve", procResolveImagePath);
			AddChainee("autokill", procRegisterAutoKill);

			m_origImagePath = imagePath;
		}

		protected override void OnStartingChain(string chainName, IChainee chainee)
		{
			TGUI.ShowPendingUI();

			procResolveImagePath.RequestBeta(m_origImagePath);
		}

		protected override void OnChaineeResult(string name, IChainee chainee, bool result)
		{
			switch(name)
			{
				case "resolve":
					procRegisterAutoKill.RequestPut(procResolveImagePath.lastResult);
					break;

				case "autokill":
					TGUI.HidePendingUI();
					break;

				default:
					Debug.LogError("wrong chainee name : " + name);
					break;
			}
		}
	}

	public class RemoveExecImageChain : TGProtocolModule.BaseProcedureChain
	{
		string      m_origImagePath;

		public RemoveExecImageChain(string imagePath)
		{
			AddChainee("resolve", procResolveImagePath);
			AddChainee("remove", procRemoveExecImage);

			m_origImagePath = imagePath;
		}

		protected override void OnStartingChain(string chainName, IChainee chainee)
		{
			TGUI.ShowPendingUI();

			procResolveImagePath.RequestBeta(m_origImagePath);
		}

		protected override void OnChaineeResult(string name, IChainee chainee, bool result)
		{
			switch (name)
			{
				case "resolve":
					procRemoveExecImage.RequestSet(procResolveImagePath.lastResult);
					break;

				case "autokill":
					TGUI.HidePendingUI();
					break;

				default:
					Debug.LogError("wrong chainee name : " + name);
					break;
			}
		}
	}
	//


	// Members

	public static PerfRegisterAutoKill procRegisterAutoKill { get; private set; }
	public static PerfRemoveExecImage procRemoveExecImage { get; private set; }
	public static PerfResolveImagePath procResolveImagePath { get; private set; }

	static TGPerfProcedures()
	{
		procRegisterAutoKill    = new PerfRegisterAutoKill();
		procRemoveExecImage     = new PerfRemoveExecImage();
		procResolveImagePath    = new PerfResolveImagePath();
	}

	public static void RegisterProcedures()
	{
		var procModule  = TGProtocolModule.instance;

		procModule.RegisterProcedure(procRegisterAutoKill);
		procModule.RegisterProcedure(procRemoveExecImage);
		procModule.RegisterProcedure(procResolveImagePath);
	}
}
