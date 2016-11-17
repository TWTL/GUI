using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UIDynamicCamera : MonoBehaviour
{
	// Members

	StateEventHub.IListenerProtocol m_listener;
	Dictionary<string, Vector3>     m_positionDict  = new Dictionary<string, Vector3>();

	Transform                       m_tr;
	Rigidbody                       m_rigid;
	Coroutine                       m_transitionCo;

	readonly Vector3				m_screenCenterPos		= new Vector3(Screen.width / 2, Screen.height / 2);
	float                           m_mousePosAngleRatio    = 45;
	float                           m_cameraDistance        = 15;


	void Awake()
	{
		m_tr        = transform;
		m_rigid     = GetComponent<Rigidbody>();
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

	void Update()
	{
		
	}

	void FixedUpdate()
	{
		var mouseFromCenter     = m_screenCenterPos - Input.mousePosition;
		var cameraEulerAngle    = mouseFromCenter / m_mousePosAngleRatio;
		var targetRot           = Quaternion.Euler(cameraEulerAngle.y, -cameraEulerAngle.x, 0);

		m_rigid.rotation		= Quaternion.Lerp(m_rigid.rotation, targetRot, 0.25f);
		//m_rigid.rotation        = targetRot;
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
		var duration    = 0.5f;
		var elapsed     = 0f;

		yield return new WaitForFixedUpdate();
		var startTime   = Time.fixedTime;
		

		while ((elapsed = Time.fixedTime - startTime) < duration)
		{
			var t				= Mathf.Pow(elapsed / duration, 0.2f);
			m_rigid.position	= Vector3.Lerp(startPos, newPos, t);
			
			yield return new WaitForFixedUpdate();
		}
		m_rigid.position		= newPos;
	}

	public void AddPanelPosition(string stateID, BaseUIPanel panel)
	{
		var calcPos				= panel.transform.position + (Vector3.back * m_cameraDistance);
		m_positionDict.Add(stateID, calcPos);
	}
}
