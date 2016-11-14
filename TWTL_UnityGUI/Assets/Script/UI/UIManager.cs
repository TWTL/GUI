using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class UIManager : MonoBehaviour
{
	// Constants
	
	public enum Layer
	{
		Main,
		Sub,
	}

	public const string     c_rootStateName = StateMachine.c_rootStateName;


	// Properties

	
	


	// Members

	StateMachine        m_mainSM;
	StateMachine        m_subSM;

	StateEventHub       m_stateEventHub;


	public static UIManager instance { get; private set; }



	void Awake()
	{
		instance        = this;
		
		m_mainSM        = new StateMachine(Layer.Main.ToString());
		m_subSM         = new StateMachine(Layer.Sub.ToString());
		m_stateEventHub = new StateEventHub();

		m_mainSM.SetEventHub(m_stateEventHub);
		m_subSM.SetEventHub(m_stateEventHub);
	}

	StateMachine GetSM(Layer layer)
	{
		switch (layer)
		{
			case Layer.Main:
				return m_mainSM;
			case Layer.Sub:
				return m_subSM;
		}
		return null;
	}
	
	public void AddDialog(IUIPanel panel, Layer layer, params string[] stateIDforDialog)
	{
		// Setup event listener for the panel
		var newListener	= CreateUIEventListener();
		newListener.SetFilterMethod(layer.ToString(), StateEventHub.FilterType.OnlyIncludes, stateIDforDialog);
		panel.SetGlobalListener(newListener);

		// set actual state for this panel
		var sm          = GetSM(layer);
		var scount      = stateIDforDialog.Length;
		for (var i = 0; i < scount; i++)
		{
			sm.AddState(stateIDforDialog[i]);
		}
	}

	public void SetDialogTransition(Layer layer, string fromID, string toID)
	{
		GetSM(layer).SetTransition(fromID, toID);
	}

	public StateEventHub.IListenerProtocol	CreateUIEventListener()
	{
		return m_stateEventHub.CreateStateListener();
	}

	public void SetState(Layer layer, string stateID)
	{
		GetSM(layer).TransitionTo(stateID);
	}
}
