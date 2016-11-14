using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// State change event distributor
/// </summary>
public class StateEventHub
{
	/// <summary>
	/// delegate type for state event changes
	/// </summary>
	/// <param name="machineid"></param>
	/// <param name="stateid"></param>
	public delegate void StateEventDelegate(string machineid, string stateid);

	/// <summary>
	/// Event filtering type
	/// </summary>
	public enum FilterType
	{
		All,					// listen to all states
		Never,					// never react to any states
		AllButExcludes,			// listen to states except... (blacklist-like behavior)
		OnlyIncludes,			// listen to the states only (whitelist-like behavior)
	}
	

	public interface IMachineProtocol
	{
		void ReportStateChange(string statefrom, string stateto);
	}


	public interface IListenerProtocol
	{
		event StateEventDelegate    stateEntered;
		event StateEventDelegate	stateLeaved;

		string prevStateID { get; }

		/// <summary>
		/// Setup filter for this listener. as default, filter never accepts any states from any machines.
		/// </summary>
		/// <param name="machineid"></param>
		/// <param name="filter"></param>
		/// <param name="stateids"></param>
		void SetFilterMethod(string machineid, FilterType filter, params string[] stateids);
	}
	//

	
	private class MachineProtocolImpl : IMachineProtocol
	{
		// Members
		
		StateEventHub   m_parent;


		public string machineID { get; set; }

		public MachineProtocolImpl(StateEventHub parent)
		{
			m_parent        = parent;
		}

		public void ReportStateChange(string statefrom, string stateto)
		{
			m_parent.SendStateChange(machineID, statefrom, stateto);
		}
	}

	private class ListenerProtocolImpl : IListenerProtocol
	{

		private class FilterImpl
		{
			public FilterType		filterType	= FilterType.Never;
			HashSet<string>			stateIDs    = new HashSet<string>();

			public void SetupStateIDs(string [] ids)
			{
				stateIDs.Clear();
				var count   = ids.Length;
				for(var i = 0; i < count; i++)
				{
					stateIDs.Add(ids[i]);
				}
			}

			public bool CheckAccepting(string stateid)
			{
				switch(filterType)
				{
					case FilterType.All:
						return true;

					case FilterType.Never:
						return false;

					case FilterType.AllButExcludes:
						return !stateIDs.Contains(stateid);

					case FilterType.OnlyIncludes:
						return stateIDs.Contains(stateid);
				}
				return false;
			}
		}


		// Members

		Dictionary<string, FilterImpl>		m_filtersByEachMachine = new Dictionary<string, FilterImpl>();

		public event StateEventDelegate    stateEntered;
		public event StateEventDelegate    stateLeaved;

		public string prevStateID { get; set; }

		public ListenerProtocolImpl()
		{

		}

		public void CallStateEnter(string machineID, string stateID)
		{
			if (stateEntered != null && CheckAccepting(machineID, stateID))
				stateEntered(machineID, stateID);
		}

		public void CallStateLeave(string machineID, string stateID)
		{
			if (stateLeaved != null && CheckAccepting(machineID, stateID))
				stateLeaved(machineID, stateID);
		}

		bool CheckAccepting(string machineid, string stateID)
		{
			FilterImpl filterObj;
			if (m_filtersByEachMachine.TryGetValue(machineid, out filterObj))	// if there's a filter set for the machine id, use that filter
			{
				return filterObj.CheckAccepting(stateID);
			}
			else
			{                                                                   // if there is not, treat it as "Never" filter.
				return false;
			}
		}

		public void SetFilterMethod(string machineid, FilterType filter, params string[] stateids)
		{
			var filterObj			= new FilterImpl();
			filterObj.filterType    = filter;
			filterObj.SetupStateIDs(stateids);

			m_filtersByEachMachine[machineid] = filterObj;
		}
	}
	//



	// Members

	List<ListenerProtocolImpl>      m_stateListeners    = new List<ListenerProtocolImpl>();



	public IMachineProtocol CreateNewMachineConnection(StateMachine machine)
	{
		var newObject		= new MachineProtocolImpl(this);
		newObject.machineID	= machine.machineID;

		return newObject;
	}

	public IListenerProtocol CreateStateListener()
	{
		var newObject       = new ListenerProtocolImpl();
		m_stateListeners.Add(newObject);

		return newObject;
	}

	private void SendStateChange(string machine, string from, string to)
	{
		var count       = m_stateListeners.Count;
		for(var i = 0; i < count; i++)
		{
			var listener			= m_stateListeners[i];

			listener.CallStateLeave(machine, from);
			listener.prevStateID    = from;
			listener.CallStateEnter(machine, to);
		}
	}
}
