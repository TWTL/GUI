using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TGExtensions;

public class TGOpening : MonoBehaviour
{
	// Properties

	[SerializeField]
	Text            m_origText;
	[SerializeField]
	Text []         m_breaks;
	[SerializeField]
	Text            m_description;


	// Members

	public event System.Action finished;

	private void OnEnable()
	{
		m_origText.CrossFadeAlpha(1, 0, true);
		m_description.CrossFadeAlpha(0, 0, true);
		CallForAllBreaks((index, br) =>
		{
			br.CrossFadeAlpha(0, 0, true);
		});
	}

	private void CallForAllBreaks(System.Action<int, Text> del)
	{
		var count   = m_breaks.Length;
		for(var i = 0; i < count; i++)
		{
			del(i, m_breaks[i]);
		}
	}

	public void StartOpening()
	{
		StartCoroutine(co_Opening());
	}

	private IEnumerator co_Opening()
	{
		yield return null;
		
		CallForAllBreaks((index, br) =>
		{
			br.CrossFadeAlpha(1, 1, false);
		});
		yield return new WaitForSeconds(1f);

		m_origText.CrossFadeAlpha(0, 1, false);
		yield return new WaitForSeconds(0.5f);

		CallForAllBreaks((index, br) =>
		{
			var tr		= br.rectTransform;
			var origPos = tr.anchoredPosition;
			var charw   = 50f;
			var newX    = (index) * charw - (float)((m_breaks.Length - 1) * charw) / 2f;
			var newPos  = new Vector2(newX, origPos.y);

			this.StartTimedCoroutine(2, (t) =>
			{
				tr.anchoredPosition = Vector2.Lerp(origPos, newPos, t);
			});
		});
		yield return new WaitForSeconds(2f);

		m_description.CrossFadeAlpha(1, 0.5f, false);
		yield return new WaitForSeconds(2f);


		if (finished != null)
			finished();
	}
}
