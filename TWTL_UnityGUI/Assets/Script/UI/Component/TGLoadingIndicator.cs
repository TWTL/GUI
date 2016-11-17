using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class TGLoadingIndicator : MonoBehaviour
{
	[SerializeField]
	Image   []      m_slices;


	void OnEnable()
	{
		StartCoroutine(co_spinnerRoutine());
	}

	IEnumerator co_spinnerRoutine()
	{
		var startTime       = Time.time;
		var oneCycleTime    = 1f;

		var sliceCount			= m_slices.Length;
		var intervalPerSlice    = oneCycleTime / (float)sliceCount;

		while(true)
		{
			var elapsed     = Time.time - startTime;
			
			for(var i = 0; i < sliceCount; i++)
			{
				var sliceTime   = Mathf.Repeat(elapsed + i * intervalPerSlice, oneCycleTime);
				SetSlice(m_slices[i], sliceTime / oneCycleTime);
			}

			yield return null;
		}
	}

	static Color __tmpSliceColor    = new Color(1, 1, 1, 1);
	void SetSlice(Image slice, float time)
	{
		var colorValue      = Mathf.Pow(1 - time, 4f);
		__tmpSliceColor.r	= colorValue;
		__tmpSliceColor.g	= colorValue;
		__tmpSliceColor.b	= colorValue;

		__tmpSliceColor.a	= 1f - Mathf.Pow(time, 0.3f);

		slice.color = __tmpSliceColor;

		var sizeValue		= Mathf.Lerp(0.5f, 1.5f, Mathf.Pow(1 - time, 4f));
		var tr				= slice.rectTransform;
		tr.localScale		= Vector3.one * sizeValue;
		tr.localPosition	= Vector3.up * Mathf.Lerp(40, 50, Mathf.Pow(1 - time, 8f));
	}
}
