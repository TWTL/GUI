using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class UIDynamicCamera : MonoBehaviour
{
	// Members

	StateEventHub.IListenerProtocol m_listener;
	Dictionary<string, Vector3>     m_positionDict  = new Dictionary<string, Vector3>();

	Transform                       m_tr;
	Coroutine                       m_transitionCo;


	void Awake()
	{
		m_tr        = transform;
	}

	void Start()
	{
		m_listener  = UIManager.instance.CreateUIEventListener();
		m_listener.SetFilterMethod(UIManager.Layer.Main.ToString(), StateEventHub.FilterType.All);
		m_listener.stateEntered += (machineID, stateID) =>
		{
			Vector3 newPos;
			if (m_positionDict.TryGetValue(stateID, out newPos))
			{
				StartNewTransition(newPos);
			}
		};
	}

	void StartNewTransition(Vector3 newPos)
	{
		if (m_transitionCo != null)
			StopCoroutine(m_transitionCo);

		m_transitionCo	= StartCoroutine(co_Transition(newPos));
	}

	IEnumerator co_Transition(Vector3 newPos)
	{
		var startPos	= m_tr.position;
		var startTime   = Time.time;
		var duration    = 0.5f;
		var elapsed     = 0f;

		while ((elapsed = Time.time - startTime) < duration)
		{
			var t			= Mathf.Pow(elapsed / duration, 0.2f);
			m_tr.position   = Vector3.Lerp(startPos, newPos, t);
			yield return null;
		}
		m_tr.position       = newPos;
	}

	public void AddPanelPosition(string stateID, BaseUIPanel panel)
	{
		var calcPos         = panel.transform.position + (Vector3.back * 10);
		m_positionDict.Add(stateID, calcPos);
	}
}
