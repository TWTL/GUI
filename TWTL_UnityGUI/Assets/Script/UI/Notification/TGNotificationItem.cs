using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TGExtensions;

public class TGNotificationItem : MonoBehaviour
{
	// Properties
	
	[SerializeField]
	Text				m_notiText;


	// Members

	System.Action<string>	m_notiAction;
	CanvasGroup				m_canvasGroup;
	RectTransform			m_rectTr;

	int						m_index;
	float					m_itemHeight;
	string					m_eventID;

	Coroutine				m_animCo;


	public string eventID { get { return m_eventID; } }
	public int index { get { return m_index; } }



	private void Awake()
	{
		m_canvasGroup   = GetComponent<CanvasGroup>();
		m_rectTr        = GetComponent<RectTransform>();
		m_itemHeight    = m_rectTr.rect.height;
	}

	public void Setup(string eventID, string text, System.Action<string> del)
	{
		m_eventID       = eventID;
		m_notiText.text = text;
		m_notiAction    = del;
	}

	public void OnBtnClick()
	{
		m_notiAction(m_eventID);
	}



	private void StopPrevCoroutine()
	{
		if (m_animCo != null)
		{
			StopCoroutine(m_animCo);
			m_animCo    = null;
		}
	}

	public void Show(int index)
	{
		m_index = index;

		StopPrevCoroutine();

		m_rectTr.SetAsFirstSibling();   // suppose that every items showing first time are the last one in the list - and should be rendered first

		m_canvasGroup.interactable  = false;
		m_canvasGroup.alpha			= 0;
		var startPos				= new Vector2(0, -(index - 1) * m_itemHeight);
		var targetPos				= new Vector2(0, -(index) * m_itemHeight);

		m_animCo	= this.StartTimedCoroutine(0.2f, (t) =>
		{
			t = Mathf.Pow(t, 0.5f);
			m_rectTr.anchoredPosition   = Vector2.Lerp(startPos, targetPos, t);
			m_canvasGroup.alpha         = t;

			if (t == 1)
				m_canvasGroup.interactable  = true;
		});
	}

	public void ChangePosition(int index)
	{
		m_index		= index;

		var startAlpha  = m_canvasGroup.alpha;
		var curPosition = m_rectTr.anchoredPosition;
		var targetPos	= new Vector2(0, -(index) * m_itemHeight);

		m_animCo    = this.StartTimedCoroutine(0.2f, (t) =>
		{
			t = Mathf.Pow(t, 0.5f);
			m_rectTr.anchoredPosition   = Vector2.Lerp(curPosition, targetPos, t);
			m_canvasGroup.alpha         = Mathf.Lerp(startAlpha, 1, t);
		});
	}

	public void Dismiss()
	{
		StopPrevCoroutine();

		m_canvasGroup.interactable  = false;
		m_animCo    = this.StartTimedCoroutine(0.2f, (t) =>
		{
			t = Mathf.Pow(t, 0.5f);
			m_canvasGroup.alpha         = 1 - t;

			if (t == 1)
				Destroy(gameObject);
		});
	}
}
