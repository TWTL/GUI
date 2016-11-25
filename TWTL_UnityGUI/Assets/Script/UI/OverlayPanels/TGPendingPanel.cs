using UnityEngine;
using System.Collections;
using TGExtensions;

public class TGPendingPanel : BaseUIPanel
{
	// Properteis
	[SerializeField]
	CanvasGroup     m_normalGroup;
	[SerializeField]
	TGOpening		m_opening;



	protected override void Initialize()
	{
		base.Initialize();

		alpha       = 0;
	}

	protected override void OnOpenTransitionStart()
	{
		base.OnOpenTransitionStart();
		
		//m_normalGroup.gameObject.SetActive(true);
		m_normalGroup.alpha = 1;
		//m_opening.gameObject.SetActive(false);
	}

	public void ShowOpening(System.Action finishDel)
	{
		this.StartTimedCoroutine(0.5f, (t) =>
		{
			m_normalGroup.alpha = Mathf.Lerp(1, 0, t);
		});
		m_opening.finished += finishDel;
		m_opening.StartOpening();
	}
}
