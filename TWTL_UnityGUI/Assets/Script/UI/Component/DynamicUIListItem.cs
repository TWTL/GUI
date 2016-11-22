using UnityEngine;
using System.Collections;

public abstract class DynamicUIListItem<ParamT> : MonoBehaviour
{
	const float     c_fadeDuration  = 0.25f;
	const float     c_moveDuration	= 0.12f;

	// Properties

	
	
	// Members

	public float	itemHeight { get; set; }
	public int		index { get; set; }

	public bool		isShowing { get; private set; }

	public ParamT	currentData { get; private set; }

	Coroutine       m_fadeCo;
	Coroutine       m_positionCo;

	RectTransform   m_tr;
	CanvasGroup     m_canvasGroup;
	


	private void Awake()
	{
		m_tr			= GetComponent<RectTransform>();
		m_canvasGroup   = GetComponent<CanvasGroup>();

		m_canvasGroup.alpha = 0;
	}

	public void SetupData(ParamT param)
	{
		currentData = param;
		SetupDataImpl(param);
	}

	public abstract void SetupDataImpl(ParamT param);

	/// <summary>
	/// 
	/// </summary>
	public void Show()
	{
		gameObject.SetActive(true);

		m_tr.anchoredPosition   = new Vector2(0, -itemHeight * index);

		StartFade(1, null);

		isShowing   = true;
	}

	public void Hide()
	{
		StartFade(0, () =>
		{
			gameObject.SetActive(false);
		});

		isShowing   = false;
	}

	public void Remove()
	{
		StartFade(0, () =>
		{
			Destroy(gameObject);
		});

		isShowing   = false;
	}

	public void RealignPosition(int index)
	{
		this.index  = index;
		if (isShowing)
			StartRealign(0);
	}


	private void StartFade(float target, System.Action finishDel)
	{
		if (m_fadeCo != null)
			StopCoroutine(m_fadeCo);

		m_fadeCo    = StartCoroutine(co_Fade(target, finishDel));
	}

	IEnumerator co_Fade(float target, System.Action finishDel)
	{
		var start       = m_canvasGroup.alpha;
		var startTime   = Time.time;
		var elapsed     = 0f;

		while ((elapsed = Time.time - startTime) < c_fadeDuration)
		{
			var t				= elapsed / c_fadeDuration;
			m_canvasGroup.alpha	= Mathf.Lerp(start, target, t);

			yield return null;
		}
		m_canvasGroup.alpha = target;

		if (finishDel != null)
			finishDel();
	}

	private void StartRealign(float predelay = 0f)
	{
		if (m_positionCo != null)
			StopCoroutine(m_positionCo);
		m_positionCo    = StartCoroutine(co_Realign());
	}

	IEnumerator co_Realign(float predelay = 0f)
	{
		if (predelay > 0f)
			yield return new WaitForSeconds(predelay);

		var start       = m_tr.anchoredPosition.y;
		var target      = -itemHeight * index;
		var startTime   = Time.time;
		var elapsed     = 0f;

		while ((elapsed = Time.time - startTime) < c_moveDuration)
		{
			var t					= elapsed / c_moveDuration;
			m_tr.anchoredPosition	= new Vector2(0, Mathf.Lerp(start, target, t));

			yield return null;
		}
		m_tr.anchoredPosition   = new Vector2(0, target);
	}
}
