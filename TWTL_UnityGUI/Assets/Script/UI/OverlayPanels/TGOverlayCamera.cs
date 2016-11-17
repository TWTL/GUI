using UnityEngine;
using System.Collections;
using UnityStandardAssets.ImageEffects;

public class TGOverlayCamera : MonoBehaviour
{
	// Properties
	[SerializeField]
	BlurOptimized					m_blur;


	// Members

	StateEventHub.IListenerProtocol m_listener;
	float                           m_effectPercentage;
	Coroutine                       m_effectCo;

	void Start()
	{
		m_listener  = UIManager.instance.CreateUIEventListener();
		m_listener.SetFilterMethod(UIManager.Layer.Sub.ToString(), StateEventHub.FilterType.All);
		m_listener.stateEntered += (machineID, stateID) =>
		{
			if (stateID == UIManager.c_rootStateName)
				StartEffectTransition(0);
		};
		m_listener.stateLeaved  += (machineID, stateID) =>
		{
			if (stateID == UIManager.c_rootStateName)
				StartEffectTransition(1);
		};
	}

	void StartEffectTransition(float targetRatio)
	{
		if (m_effectCo != null)
			StopCoroutine(m_effectCo);
		m_effectCo  = StartCoroutine(co_EffectTransition(targetRatio));
	}

	IEnumerator co_EffectTransition(float targetRatio)
	{
		var startR      = m_effectPercentage;
		var startTime   = Time.time;
		var duration    = 0.5f;
		var elapsed     = 0f;

		while((elapsed = Time.time - startTime) < duration)
		{
			var t       = elapsed / duration;
			ApplyEffectRatio(Mathf.Lerp(startR, targetRatio, t));
			yield return null;
		}
		ApplyEffectRatio(targetRatio);
	}

	void ApplyEffectRatio(float ratio)
	{
		m_effectPercentage  = ratio;

		// TODO : actual effect application

		m_blur.enabled      = (ratio < 0.01f) ? false : true;
		m_blur.blurSize			= ratio * 10f;
		m_blur.blurIterations   = (int)Mathf.Min(ratio * 2f * 4f + 1, 4f);
	}
}
