using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class TGMessagePanel : BaseUIPanel
{
	struct ButtonInfo
	{
		public Button       button;
		public GameObject   gameObject;
		public Text         text;

		public System.Action    del;
	}


	// Properties

	[SerializeField]
	Text            m_message;
	[SerializeField]
	Button []       m_buttons;


	// Members

	bool                m_msgPanelInit   = false;
	ButtonInfo  []      m_buttonInfos;
	


	protected override void Initialize()
	{
		base.Initialize();
		

		var buttonCount = m_buttons.Length;
		m_buttonInfos   = new ButtonInfo[buttonCount];
		for (var i = 0; i < buttonCount; i++)
		{
			var button                  = m_buttons[i];
			var text                    = button.GetComponentInChildren<Text>();
			m_buttonInfos[i]            = new ButtonInfo()
			{
				button                  = button,
				text                    = text,
				gameObject              = button.gameObject,
				del                     = null,
			};

			var btnIndex    = i;
			button.onClick.AddListener(() =>
			{
				//Debug.Log(btnIndex);
				OnButtonClick(btnIndex);
			});
		}

		alpha   = 0;
	}

	void CheckInitialize()
	{
		if (!gameObject.activeSelf && !m_msgPanelInit)
		{
			gameObject.SetActive(true);
			m_msgPanelInit   = true;
		}
	}

	public void SetTexts(string message, params string [] buttonText)
	{
		CheckInitialize();

		m_message.text      = message;
		var count           = buttonText.Length;
		var fullCount       = m_buttonInfos.Length;
		var i               = 0;
		for (i = 0; i < count; i++)
		{
			m_buttonInfos[i].gameObject.SetActive(true);
			m_buttonInfos[i].text.text  = buttonText[i];
		}
		for (; i < fullCount; i++)
		{
			m_buttonInfos[i].gameObject.SetActive(false);
		}
	}

	public void SetDelegates(params System.Action [] dels)
	{
		CheckInitialize();

		if (dels != null)
		{
			var count		= dels.Length;
			var buttonCount = m_buttonInfos.Length;
			var i           = 0;
			for(; i < count; i++)
			{
				m_buttonInfos[i].del = dels[i];
			}
			for(; i < buttonCount; i++)
			{
				m_buttonInfos[i].del = null;
			}
		}
	}

	void ClearDelegates()
	{
		var count   = m_buttonInfos.Length;
		for(var i = 0; i < count; i++)
		{
			m_buttonInfos[i].del    = null;
		}
	}

	void OnButtonClick(int index)
	{
		UIManager.instance.SetState(UIManager.Layer.Sub, UIManager.c_rootStateName);

		var del     = m_buttonInfos[index].del;
		if (del != null)
			del();

		// Cleanups
		ClearDelegates();
	}
}
