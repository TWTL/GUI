using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;


public interface IUIPanel
{
	//void Open();
	//void Close();
	void SetGlobalListener(StateEventHub.IListenerProtocol listener);
}

/// <summary>
/// Base class for all UI panels
/// </summary>
public abstract partial class BaseUIPanel : MonoBehaviour, IUIPanel
{
	// Constants

	const string    c_internalSMName    = "uiInternal";

	const string    c_stateBeginOpen    = "beginOpen";
	const string    c_stateEndOpen      = "endOpen";
	const string    c_stateIdle         = "idle";
	const string    c_stateBeginClose   = "beginClose";
	const string    c_stateEndClose     = "endClose";


	// Members

	bool            m_initialized   = false;
	bool            m_smInit        = false;

	StateEventHub.IListenerProtocol m_globalListener;

	StateMachine    m_uiStates;
	StateEventHub   m_internalStateEventHub;
	StateEventHub.IListenerProtocol m_internalListener;

	TransitionController    m_transCtrl;

	CanvasGroup     m_canvasGroup;


	/// <summary>
	/// previous state name that this dialog transitioned from
	/// </summary>
	protected string	previousDialogStateID { get; private set; }

	/// <summary>
	/// whole panel alpha
	/// </summary>
	protected float alpha
	{
		get { return m_canvasGroup.alpha; }
		set { m_canvasGroup.alpha = value; }
	}
	


	void Awake()
	{
		if (m_initialized)				// init only once - Awake() is called every place that needs this obj to be initialized
			return;
		m_initialized   = true;

		m_canvasGroup   = GetComponent<CanvasGroup>();
		
		InternalStateSetup();
		Initialize();

		m_transCtrl		= new TransitionController();
		TransitionSetup(m_transCtrl);

		// callbacks for transition finish events
		m_transCtrl[TransitionType.Open].finished	+= () => m_uiStates.TransitionTo(c_stateEndOpen);
		m_transCtrl[TransitionType.Close].finished  += () => m_uiStates.TransitionTo(c_stateEndClose);
	}

	void InternalStateSetup()
	{
		if (m_smInit)
			return;
		m_smInit		= true;

		m_uiStates      = new StateMachine(c_internalSMName);

		var start       = m_uiStates.rootState;

		var beginOpen   = m_uiStates.AddState(c_stateBeginOpen);
		var endOpen     = m_uiStates.AddState(c_stateEndOpen);
		var idle        = m_uiStates.AddState(c_stateIdle);
		var beginClose  = m_uiStates.AddState(c_stateBeginClose);
		var endClose	= m_uiStates.AddState(c_stateEndClose);

		m_uiStates.SetTransition(start, beginOpen);

		m_uiStates.SetTransition(beginOpen, endOpen);
		m_uiStates.SetTransition(beginOpen, beginClose);

		m_uiStates.SetTransition(endOpen, idle);

		m_uiStates.SetTransition(idle, beginClose);

		m_uiStates.SetTransition(beginClose, endClose);
		m_uiStates.SetTransition(beginClose, beginOpen);

		m_uiStates.SetTransition(endClose, start);

		m_internalStateEventHub = new StateEventHub();
		m_uiStates.SetEventHub(m_internalStateEventHub);

		//
		m_internalStateEventHub.CreateStateListener();
		m_internalListener  = m_internalStateEventHub.CreateStateListener();
		m_internalListener.SetFilterMethod(c_internalSMName, StateEventHub.FilterType.All);

		m_internalListener.stateEntered += OnInternalStateEnter;
		m_internalListener.stateLeaved  += OnInternalStateLeave;
	}

	void OnInternalStateEnter(string machineID, string stateID)
	{
		//Debug.LogFormat("{0} internal state enter : {1}", gameObject.name, stateID);
		switch(stateID)
		{
			case c_stateBeginOpen:

				m_canvasGroup.interactable  = false;
				m_transCtrl.DoTransition(TransitionType.Open);  // transition animation start
				OnOpenTransitionStart();
				break;

			case c_stateEndOpen:

				OnOpenTransitionEnd();
				m_uiStates.TransitionTo(c_stateIdle);
				break;

			case c_stateBeginClose:

				m_canvasGroup.interactable  = false;
				m_transCtrl.DoTransition(TransitionType.Close);  // transition animation start
				OnCloseTransitionStart();
				break;

			case c_stateEndClose:

				OnCloseTransitionEnd();
				m_uiStates.TransitionTo(StateMachine.c_rootStateName);
				break;

			case c_stateIdle:
				m_canvasGroup.interactable  = true;
				break;

			case StateMachine.c_rootStateName:
				gameObject.SetActive(false);
				break;
		}
	}

	void OnInternalStateLeave(string machineID, string stateID)
	{
		//Debug.LogFormat("{0} internal state leave : {1}", gameObject.name, stateID);
		switch (stateID)
		{
			case StateMachine.c_rootStateName:
				gameObject.SetActive(true);
				break;
		}
	}
	//

	/// <summary>
	/// event listener from UIManagers
	/// </summary>
	/// <param name="listener"></param>
	public void SetGlobalListener(StateEventHub.IListenerProtocol listener)
	{
		m_globalListener	= listener;

		m_globalListener.stateEntered   += (machineid, stateid) =>
		{
			//Debug.LogFormat("{0} stateEntered : {1}, {2}", gameObject.name, machineid, stateid);
			previousDialogStateID   = listener.prevStateID;
			Open();
		};
		m_globalListener.stateLeaved    += (machineid, stateid) =>
		{
			//Debug.LogFormat("{0} stateLeaved : {1}, {2}", gameObject.name, machineid, stateid);
			Close();
		};
	}

	/// <summary>
	/// Open this panel
	/// </summary>
	void Open()
	{
		InternalStateSetup();					// check if state machine is not initialized

		switch (m_uiStates.current.stateID)
		{
			case StateMachine.c_rootStateName:  // case 1 : completely closed and deactivated
				
				m_uiStates.TransitionTo(c_stateBeginOpen);		// state change
				break;

			case c_stateBeginClose:             // case 2 : in the way closing

				m_uiStates.TransitionTo(c_stateBeginOpen);		// state change
				break;

			default:
				// do nothing
				break;
		}
	}

	/// <summary>
	/// Close this panel
	/// </summary>
	void Close()
	{
		InternalStateSetup();                   // check if state machine is not initialized

		switch(m_uiStates.current.stateID)
		{
			case c_stateIdle:                   // case 1. completely opened

				m_uiStates.TransitionTo(c_stateBeginClose);
				break;

			case c_stateBeginOpen:              // case 2. in the way opening

				m_uiStates.TransitionTo(c_stateBeginClose);
				break;

			default:
				// do nothing
				break;
		}
	}


	//

	protected virtual void Initialize() { }
	protected virtual void TransitionSetup(TransitionController ctrl)
	{
		// TODO : override and set other transitions if you need

		ctrl[TransitionType.Open]   = new FadeIn(this);
		ctrl[TransitionType.Close]  = new FadeOut(this);
	}

	protected virtual void OnOpenTransitionStart() { }
	protected virtual void OnOpenTransitionEnd() { }
	protected virtual void OnCloseTransitionStart() { }
	protected virtual void OnCloseTransitionEnd() { }
}
