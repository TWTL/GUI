using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// General Implementation for state machine
/// </summary>
public class StateMachine
{
	/// <summary>
	/// interface representing a state
	/// </summary>
	public interface IState
	{
		/// <summary>
		/// ID for this state
		/// </summary>
		string		stateID { get; }

		/// <summary>
		/// Check whether this state can transition to the target.
		/// </summary>
		/// <param name="state"></param>
		/// <returns></returns>
		bool CanTransitionTo(IState state);

		/// <summary>
		/// Check whether this state can transition to the target.
		/// </summary>
		bool CanTransitionTo(string stateid);
	}

	private class State : IState
	{
		// Members

		Dictionary<string, State>   m_canTransTo    = new Dictionary<string, State>();

		
		public string stateID { get; set; }

		/// <summary>
		/// add a target state that can be transitioned from this state.
		/// NOTE : this is not bi-directional
		/// </summary>
		/// <param name="state"></param>
		public void AddStateTo(State state)
		{
			m_canTransTo[state.stateID] = state;
		}

		/// <summary>
		/// Check whether this state can transition to the target.
		/// </summary>
		/// <param name="state"></param>
		/// <returns></returns>
		public bool CanTransitionTo(IState state)
		{
			return m_canTransTo.ContainsKey(state.stateID);
		}

		public bool CanTransitionTo(string stateid)
		{
			return m_canTransTo.ContainsKey(stateid);
		}
	}

	// Constants

	public const string			c_rootStateName     = "start";


	// Members

	State                       m_rootState         = null;
	State                       m_currentState      = null;
	Dictionary<string, State>	m_stateLookupDict   = new Dictionary<string, State>();

	StateEventHub.IMachineProtocol  m_eventProtocol;

	

	/// <summary>
	/// ID for this machine
	/// </summary>
	public string machineID { get; private set; }

	/// <summary>
	/// Current state
	/// </summary>
	public IState current
	{
		get { return m_currentState; }
	}

	public IState rootState
	{
		get { return this[c_rootStateName]; }
	}

	public IState this[string id]
	{
		get { return m_stateLookupDict[id]; }
	}


	public StateMachine(string machineid)
	{
		m_rootState     = AddStateInternal(c_rootStateName);		// create the root state
		m_currentState  = m_rootState;                              // ...and set this as current state

		machineID       = machineid;
	}

	/// <summary>
	/// Set event hub for sending event change messages
	/// </summary>
	/// <param name="hub"></param>
	public void SetEventHub(StateEventHub hub)
	{
		m_eventProtocol = hub.CreateNewMachineConnection(this);
	}

	/// <summary>
	/// add new state.
	/// </summary>
	/// <param name="id"></param>
	private State AddStateInternal(string id)
	{
		if (m_stateLookupDict.ContainsKey(id))						// if a state named with the same name exists, treat as an error.
		{
			throw new System.InvalidOperationException("state already exists : " + id);
		}

		var newstate		= new State();
		newstate.stateID    = id;
		m_stateLookupDict.Add(id, newstate);						// add the state to lookup dictionary
		return newstate;
	}

	/// <summary>
	/// add new state. this does nothing with transition settings. (creates isolated state)
	/// </summary>
	/// <param name="id"></param>
	/// <returns></returns>
	public IState AddState(string id)
	{
		return AddStateInternal(id);
	}

	/// <summary>
	/// get a state named "id"
	/// </summary>
	/// <param name="id"></param>
	/// <returns></returns>
	public IState GetState(string id)
	{
		State state;
		if (!m_stateLookupDict.TryGetValue(id, out state))
		{
			throw new System.InvalidOperationException("no state named " + id);
		}
		else
		{
			return state;
		}
	}

	/// <summary>
	/// setup transition between states. this is uni-directional.
	/// if you want to make bi-directional transition, you should call this twice by reversing the from-to order.
	/// </summary>
	/// <param name="fromID"></param>
	/// <param name="toID"></param>
	public void SetTransition(string fromID, string toID)
	{
		var from    = GetState(fromID);
		var to      = GetState(toID);
		SetTransition(from, to);
	}

	/// <summary>
	/// setup transition between states. this is uni-directional.
	/// if you want to make bi-directional transition, you should call this twice by reversing the from-to order.
	/// </summary>
	public void SetTransition(IState from, IState to)
	{
		(from as State).AddStateTo(to as State);
	}
	
	public bool CanTransitionTo(string toID)
	{
		return current.CanTransitionTo(this[toID]);
	}

	public bool CanTransitionTo(IState to)
	{
		return current.CanTransitionTo(to);
	}

	/// <summary>
	/// Change current state to...
	/// </summary>
	/// <param name="toID"></param>
	/// <returns></returns>
	public bool TransitionTo(string toID)
	{
		return TransitionTo(this[toID]);
	}

	/// <summary>
	/// Change current state to...
	/// </summary>
	/// <param name="to"></param>
	/// <returns></returns>
	public bool TransitionTo(IState to)
	{
		if (!m_currentState.CanTransitionTo(to))
		{
			Debug.LogWarningFormat("(statemachine : {0}) cannot change state from {1} to {2}", machineID, current.stateID, to.stateID);
			return false;
		}
		else
		{
			var oldStateID      = m_currentState.stateID;
			m_currentState		= to as State;              // Actual state change
			if (m_eventProtocol != null)					// Sending state change event if there's an assigned event hub
			{
				m_eventProtocol.ReportStateChange(oldStateID, m_currentState.stateID);
			}

			
			return true;
		}
	}
}
