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
		bool m_isTrap;

		public MessagePanelBuilder(TGUI parent, bool isTrap)
		{
			m_parent    = parent;
			m_isTrap    = isTrap;
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
			var msgPanel    = m_isTrap? m_parent.m_trapMessagePanel : m_parent.m_messagePanel;
			msgPanel.SetTexts(m_message, m_buttonTexts.ToArray());
			msgPanel.SetDelegates(m_buttonDels.ToArray());
			UIManager.instance.SetState(UIManager.Layer.Sub, m_isTrap? c_sub_trapMessage : c_sub_message);
		}
	}


	// Constants

	const string    c_main_mainPanel    = "main";
	const string    c_main_registry     = "registry";
	const string    c_sub_pending		= "pending";
	const string    c_sub_message		= "message";
	const string    c_sub_trapMessage   = "trapmessage";



	// Properties

	[SerializeField]
	TGMainPanel			m_main;
	[SerializeField]
	TGRegistryPanel		m_registry;
	[SerializeField]
	UIDynamicCamera		m_uicam;
	[SerializeField]
	TGPendingPanel		m_pendingPanel;
	[SerializeField]
	TGMessagePanel		m_messagePanel;
	[SerializeField]
	TGMessagePanel		m_trapMessagePanel;


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
		uiMgr.AddDialog(m_registry, UIManager.Layer.Main, c_main_registry);
		m_uicam.AddPanelPosition(c_main_registry, m_registry);

		uiMgr.SetDialogTransitionFromRoot(UIManager.Layer.Main, c_main_mainPanel);
		uiMgr.SetDialogTransitionBi(UIManager.Layer.Main, c_main_mainPanel, c_main_registry);
		//

		uiMgr.AddDialog(m_pendingPanel, UIManager.Layer.Sub, c_sub_pending);
		uiMgr.AddDialog(m_messagePanel, UIManager.Layer.Sub, c_sub_message);
		uiMgr.AddDialog(m_trapMessagePanel, UIManager.Layer.Sub, c_sub_trapMessage);

		uiMgr.SetDialogTransitionRootBi(UIManager.Layer.Sub, c_sub_pending);
		uiMgr.SetDialogTransitionRootBi(UIManager.Layer.Sub, c_sub_message);
		uiMgr.SetDialogTransitionBi(UIManager.Layer.Sub, c_sub_pending, c_sub_message);

		uiMgr.SetDialogTransitionRootBi(UIManager.Layer.Sub, c_sub_trapMessage);
		uiMgr.SetDialogTransitionBi(UIManager.Layer.Sub, c_sub_pending, c_sub_trapMessage);
		uiMgr.SetDialogTransitionBi(UIManager.Layer.Sub, c_sub_message, c_sub_trapMessage);
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
		return new MessagePanelBuilder(instance, false);
	}

	public static void CallMainUI()
	{
		UIManager.instance.SetState(UIManager.Layer.Main, c_main_mainPanel);
	}

	public static void CallRegistryUI()
	{
		UIManager.instance.SetState(UIManager.Layer.Main, c_main_registry);
	}

	public static IMessagePanelBuilder GetTrapMessagePanelBuilder()
	{
		return new MessagePanelBuilder(instance, true);
	}
}
