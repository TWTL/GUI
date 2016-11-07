using UnityEngine;
using System.Collections;

public class TestUISetup : MonoBehaviour
{
	[SerializeField]
	TestPanel1      m_main;
	[SerializeField]
	TestPanel1      m_panel1;
	[SerializeField]
	TestPanel1      m_panel2;


	void Start()
	{
		var uiMgr   = UIManager.instance;
		uiMgr.AddDialog(m_main, UIManager.Layer.Main, "main");
		m_main.backID   = UIManager.c_rootStateName;
		uiMgr.AddDialog(m_panel1, UIManager.Layer.Main, "panel1");
		m_panel1.backID = "main";
		uiMgr.AddDialog(m_panel2, UIManager.Layer.Main, "panel2");
		m_panel2.backID = "main";

		uiMgr.SetDialogTransition(UIManager.Layer.Main, UIManager.c_rootStateName, "main");
		uiMgr.SetDialogTransition(UIManager.Layer.Main, "main", UIManager.c_rootStateName);

		uiMgr.SetDialogTransition(UIManager.Layer.Main, "main", "panel1");
		uiMgr.SetDialogTransition(UIManager.Layer.Main, "panel1", "main");

		uiMgr.SetDialogTransition(UIManager.Layer.Main, "main", "panel2");
		uiMgr.SetDialogTransition(UIManager.Layer.Main, "panel2", "main");
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Return))
		{
			UIManager.instance.SetState(UIManager.Layer.Main, "main");
		}
	}
}
