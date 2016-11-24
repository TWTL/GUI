using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public static class TGNetworkProcedures
{
	public interface IDataEntry
	{
		string	SrcIP				{ get; }
		int		SrcPort				{ get; }
		string	DestIP				{ get; }
		int		DestPort			{ get; }
		int		PID					{ get; }
		string	ProcessImagePath	{ get; }
		bool	IsDangerous			{ get; }
		bool	Alive				{ get; }
	}

	class DataEntry : IDataEntry
	{
		public string	SrcIP				{ get; set; }
		public int		SrcPort				{ get; set; }
		public string	DestIP				{ get; set; }
		public int		DestPort			{ get; set; }
		public int		PID					{ get; set; }
		public string	ProcessImagePath	{ get; set; }
		public bool		IsDangerous			{ get; set; }
		public bool		Alive				{ get; set; }
	}


	public class NetConnections : TGProtocolModule.BaseProcedure
	{
		List<IDataEntry> m_list;


		public override string procedurePath
		{
			get
			{
				return "/Net/Connections/";
			}
		}

		public NetConnections()
		{
			RegisterFunction(FunctionType.status, (JSONObject param) =>
			{

			});

			RegisterFunction(FunctionType.obj, (JSONObject param) =>
			{
				var list    = param.list;
				var count   = list.Count;

				for(var i = 0; i < count; i++)
				{
					var entry	= list[i];
					var newData	= new DataEntry();

					newData.SrcIP				= entry["SrcIP"].str;
					newData.SrcPort				= (int)entry["SrcPort"].n;
					newData.DestIP				= entry["DestIP"].str;
					newData.DestPort			= (int)entry["DestPort"].n;
					newData.PID					= (int)entry["PID"].n;
					newData.ProcessImagePath    = entry["ProcessImagePath"].str;
					newData.IsDangerous			= entry["IsDangerous"].b;
					newData.Alive				= entry["Alive"].b;

					m_list.Add(newData);
				}
			});
		}

		public void RequestGet(List<IDataEntry> list)
		{
			m_list  = list;
			m_list.Clear();
			SimpleRequestGet();
		}

		protected override bool OnCallFinish()
		{
			return true;
		}
	}
	//


	public class NetworkConnectionsChain : TGProtocolModule.BaseProcedureChain
	{
		public IDataEntry [] dataGenerated { get; private set; }
		List<IDataEntry>    m_tempList;

		public NetworkConnectionsChain()
		{
			AddChainee("connections", procNetConnections);
		}

		
		protected override void OnStartingChain(string chainName, IChainee chainee)
		{
			TGUI.ShowPendingUI();

			m_tempList  = new List<IDataEntry>();
			procNetConnections.RequestGet(m_tempList);
		}

		protected override void OnChaineeResult(string name, IChainee chainee, bool result)
		{
			TGUI.HidePendingUI();

			dataGenerated   = m_tempList.ToArray();
			m_tempList      = null;

			// TODO : show ui
		}
	}
	//


	// Members

	public static NetConnections procNetConnections { get; private set; }

	static TGNetworkProcedures()
	{
		procNetConnections  = new NetConnections();
	}

	public static void RegisterProcedures()
	{
		var procModule  = TGProtocolModule.instance;

		procModule.RegisterProcedure(procNetConnections);
	}
}
