using UnityEngine;
using System.Collections;
using System.Collections.Generic;


/// <summary>
/// Class for TWTL GUI specific UIs
/// </summary>
public class TGUI : MonoBehaviour, UIManager.IUIInitializer
{
	public interface IMessagePanelBuilder
	{
		IMessagePanelBuilder SetMessage(string message);
		IMessagePanelBuilder AddButton(string text, System.Action del = null);
		void Show();
	}

	class MessagePanelBuilder : IMessagePanelBuilder
	{
		string              m_message;
		List<string>        m_buttonTexts	= new List<string>();
		List<System.Action> m_buttonDels	= new List<System.Action>();

		TGUI m_parent;

		public MessagePanelBuilder(TGUI parent)
		{
			m_parent    = parent;
		}

		public IMessagePanelBuilder SetMessage(string message)
		{
			m_message       = message;
			return this;
		}

		public IMessagePanelBuilder AddButton(string text, System.Action del)
		{
			m_buttonTexts.Add(text);
			m_buttonDels.Add(del);
			return this;
		}

		public void Show()
		{
			var msgPanel    = m_parent.m_messagePanel;
			msgPanel.SetTexts(m_message, m_buttonTexts.ToArray());
			msgPanel.SetDelegates(m_buttonDels.ToArray());
			UIManager.instance.SetState(UIManager.Layer.Sub, c_sub_message);
		}
	}


	// Constants

	const string    c_main_mainPanel    = "main";
	const string    c_sub_pending		= "pending";
	const string    c_sub_message		= "message";



	// Properties

	[SerializeField]
	TGMainPanel     m_main;
	[SerializeField]
	UIDynamicCamera m_uicam;
	[SerializeField]
	TGPendingPanel  m_pendingPanel;
	[SerializeField]
	TGMessagePanel  m_messagePanel;


	// Members

	static TGUI instance { get; set; }


	void Awake()
	{
		instance    = this;
	}

	public void InitUI()
	{
		var uiMgr   = UIManager.instance;

		uiMgr.AddDialog(m_main, UIManager.Layer.Main, c_main_mainPanel);
		m_uicam.AddPanelPosition(c_main_mainPanel, m_main);

		uiMgr.SetDialogTransitionFromRoot(UIManager.Layer.Main, c_main_mainPanel);
		
		//

		uiMgr.AddDialog(m_pendingPanel, UIManager.Layer.Sub, c_sub_pending);
		uiMgr.AddDialog(m_messagePanel, UIManager.Layer.Sub, c_sub_message);

		uiMgr.SetDialogTransitionRootBi(UIManager.Layer.Sub, c_sub_pending);
		uiMgr.SetDialogTransitionRootBi(UIManager.Layer.Sub, c_sub_message);
		uiMgr.SetDialogTransitionBi(UIManager.Layer.Sub, c_sub_pending, c_sub_message);
	}
	//

	
	
	public static void ShowPendingUI()
	{
		UIManager.instance.SetState(UIManager.Layer.Sub, c_sub_pending);
	}

	public static void HidePendingUI()
	{
		UIManager.instance.SetState(UIManager.Layer.Sub, UIManager.c_rootStateName);
	}

	public static IMessagePanelBuilder GetMessagePanelBuilder()
	{
		return new MessagePanelBuilder(instance);
	}

	public static void CallMainUI()
	{
		UIManager.instance.SetState(UIManager.Layer.Main, c_main_mainPanel);
	}
}
