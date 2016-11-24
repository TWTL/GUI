using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;



public class RegistryData
{
	public enum Category
	{
		GlobalServices,
		GlobalRun,
		GlobalRunOnce,
		UserRun,
		UserRunOnce,
	}

	public interface ISubDict
	{
		string this[string name] { get; }
		void Add(string name, string value);
		string Get(string name);
		string[] GetAllPaths();
	}

	class SubDict : ISubDict
	{
		// Members

		Dictionary<string, string>  m_kvDict    = new Dictionary<string, string>();


		public string this[string name]
		{
			get { return Get(name); }
		}

		public void Add(string name, string value)
		{
			m_kvDict.Add(name, value);
		}

		public string Get(string name)
		{
			return m_kvDict[name];
		}

		public string[] GetAllPaths()
		{
			var array   = new string[m_kvDict.Keys.Count];
			m_kvDict.Keys.CopyTo(array, 0);
			return array;
		}
	}


	// Members

	Dictionary<Category, ISubDict>   m_subDicts  = new Dictionary<Category, ISubDict>();
	

	public ISubDict this[Category category]
	{
		get { return Get(category); }
	}

	public RegistryData()
	{
		var categoryNames   = System.Enum.GetValues(typeof(Category));
		var count           = categoryNames.Length;
		for(var i = 0; i < count; i++)
		{
			m_subDicts[(Category)categoryNames.GetValue(i)] = new SubDict();
		}
	}

	public ISubDict Get(Category category)
	{
		return m_subDicts[category];
	}
}


public static class TGRegistryProcedures
{
	public abstract class BaseRegShortProcedures : TGProtocolModule.BaseProcedure
	{
		protected RegistryData	outputData { get; private set; }


		public BaseRegShortProcedures()
		{
			RegisterFunction(FunctionType.status, (JSONObject param) =>
			{

			});

			RegisterFunction(FunctionType.obj, (JSONObject param) =>
			{
				var arr     = param.list;
				var count   = arr.Count;
				for (var i = 0; i < count; i++)
				{
					var entry   = arr[i];
					var name    = entry["Name"].str;
					var value   = entry["Value"].str;

					AddEntry(name, value);
				}
			});
		}

		public void RequestDiff(RegistryData dataToFill)
		{
			outputData  = dataToFill;

			SendMessage(FunctionType.diff, null);
		}

		public void RequestPatch(string name, string value, bool remove)
		{
			var param		= new JSONObject();

			var objPair		= new JSONObject();
			objPair.AddField("Name", name);
			objPair.AddField("Value", value);

			param.AddField("value", new JSONObject(new JSONObject[] { objPair }));
			param.AddField("accept", new JSONObject(new JSONObject[] { new JSONObject(!remove) }));
			param.AddField("reject", new JSONObject(new JSONObject[] { new JSONObject(remove) }));

			SendMessage(FunctionType.patch, param);
		}

		protected override bool OnCallFinish()
		{
			outputData  = null;
			return true;
		}

		protected abstract void AddEntry(string name, string value);
	}


	public class RegShortGlobalServices : BaseRegShortProcedures
	{
		public override string procedurePath
		{
			get
			{
				return "/Reg/Short/GlobalServices/";
			}
		}

		protected override void AddEntry(string name, string value)
		{
			outputData[RegistryData.Category.GlobalServices].Add(name, value);
		}
	}

	public class RegShortGlobalRun : BaseRegShortProcedures
	{
		public override string procedurePath
		{
			get
			{
				return "/Reg/Short/GlobalRun/";
			}
		}

		protected override void AddEntry(string name, string value)
		{
			outputData[RegistryData.Category.GlobalRun].Add(name, value);
		}
	}

	public class RegShortGlobalRunOnce : BaseRegShortProcedures
	{
		public override string procedurePath
		{
			get
			{
				return "/Reg/Short/GlobalRunOnce/";
			}
		}

		protected override void AddEntry(string name, string value)
		{
			outputData[RegistryData.Category.GlobalRunOnce].Add(name, value);
		}
	}

	public class RegShortUserRun : BaseRegShortProcedures
	{
		public override string procedurePath
		{
			get
			{
				return "/Reg/Short/UserRun/";
			}
		}

		protected override void AddEntry(string name, string value)
		{
			outputData[RegistryData.Category.UserRun].Add(name, value);
		}
	}

	public class RegShortUserRunOnce : BaseRegShortProcedures
	{
		public override string procedurePath
		{
			get
			{
				return "/Reg/Short/UserRunOnce/";
			}
		}

		protected override void AddEntry(string name, string value)
		{
			outputData[RegistryData.Category.UserRunOnce].Add(name, value);
		}
	}
	//

	public class RegistryGetChain : TGProtocolModule.BaseProcedureChain
	{
		/// <summary>
		/// Output.
		/// </summary>
		public RegistryData dataGenerated { get; private set; }

		public RegistryGetChain()
		{
			AddChainee("globalservices", procRegGlobalServices);
			AddChainee("globalrun", procRegGlobalRun);
			AddChainee("globalrunonce", procRegGlobalRunOnce);
			AddChainee("userrun", procRegUserRun);
			AddChainee("userrunonce", procRegUserRunOnce);
		}

		protected override void OnStartingChain(string chainName, IChainee chainee)
		{
			TGUI.ShowPendingUI();

			dataGenerated   = new RegistryData();
			procRegGlobalServices.RequestDiff(dataGenerated);
		}

		protected override void OnChaineeResult(string name, IChainee chainee, bool result)
		{
			switch(name)
			{
				case "globalservices":
					procRegGlobalRun.RequestDiff(dataGenerated);
					break;

				case "globalrun":
					procRegGlobalRunOnce.RequestDiff(dataGenerated);
					break;

				case "globalrunonce":
					procRegUserRun.RequestDiff(dataGenerated);
					break;

				case "userrun":
					procRegUserRunOnce.RequestDiff(dataGenerated);
					break;

				case "userrunonce":
					TGUI.HidePendingUI();
					TGUI.CallRegistryUI(dataGenerated);
					break;

				default:
					Debug.LogError("wrong chain name : " + name);
					break;
			}
		}
	}

	public class SingleRegistryPatch : TGProtocolModule.BaseProcedureChain
	{
		//RegistryData.Category   m_category;
		BaseRegShortProcedures  m_proc;

		string                  m_name;
		string                  m_value;
		bool                    m_remove;

		public SingleRegistryPatch(RegistryData.Category category, string name, string value, bool remove)
		{
			//m_category  = category;

			m_name      = name;
			m_value     = value;
			m_remove    = remove;

			switch(category)
			{
				case RegistryData.Category.GlobalServices:
					m_proc = procRegGlobalServices;
					break;

				case RegistryData.Category.GlobalRun:
					m_proc = procRegGlobalRun;
					break;

				case RegistryData.Category.GlobalRunOnce:
					m_proc = procRegGlobalRunOnce;
					break;

				case RegistryData.Category.UserRun:
					m_proc = procRegUserRun;
					break;

				case RegistryData.Category.UserRunOnce:
					m_proc = procRegUserRunOnce;
					break;
			}

			AddChainee("proc", m_proc);
		}

		protected override void OnStartingChain(string chainName, IChainee chainee)
		{
			m_proc.RequestPatch(m_name, m_value, m_remove);
		}

		protected override void OnChaineeResult(string name, IChainee chainee, bool result)
		{
			
		}
	}
	


	// Members

	public static RegShortGlobalServices	procRegGlobalServices	{ get; private set; }
	public static RegShortGlobalRun			procRegGlobalRun		{ get; private set; }
	public static RegShortGlobalRunOnce		procRegGlobalRunOnce	{ get; private set; }
	public static RegShortUserRun			procRegUserRun			{ get; private set; }
	public static RegShortUserRunOnce		procRegUserRunOnce		{ get; private set; }

	static TGRegistryProcedures()
	{
		procRegGlobalServices   = new RegShortGlobalServices();
		procRegGlobalRun        = new RegShortGlobalRun();
		procRegGlobalRunOnce    = new RegShortGlobalRunOnce();
		procRegUserRun          = new RegShortUserRun();
		procRegUserRunOnce      = new RegShortUserRunOnce();
	}

	public static void RegisterProcedures()
	{
		var procModule  = TGProtocolModule.instance;

		procModule.RegisterProcedure(procRegGlobalServices);
		procModule.RegisterProcedure(procRegGlobalRun);
		procModule.RegisterProcedure(procRegGlobalRunOnce);
		procModule.RegisterProcedure(procRegUserRun);
		procModule.RegisterProcedure(procRegUserRunOnce);
	}
}
