using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class TGRegistryUIListItem : DynamicUIListItem<TGRegistryUIListItem.Param>
{
	public struct Param
	{
		public RegistryData.Category	category;
		public string                   name;
		public string                   value;
	}

	// Procedures

	[SerializeField]
	Text                m_textCategory;
	[SerializeField]
	Text                m_textName;
	[SerializeField]
	Text                m_textValue;


	// Members

	public event System.Action clicked;


	public override void SetupDataImpl(Param param)
	{
		m_textCategory.text = param.category.ToString();
		m_textName.text     = param.name;
		m_textValue.text    = "= " + param.value;
	}

	public void OnBtnClick()
	{
		clicked();
	}
}
