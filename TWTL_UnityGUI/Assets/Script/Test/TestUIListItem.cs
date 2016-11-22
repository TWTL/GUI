using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;

public class TestUIListItem : DynamicUIListItem<string>
{
	[SerializeField]
	Text            m_text;

	public event System.Action<int> onClick;
	
	public override void SetupDataImpl(string param)
	{
		m_text.text = param;
	}

	public void OnBtnClick()
	{
		onClick(index);
	}
}
