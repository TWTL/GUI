using UnityEngine;
using System.Collections;

public class TestPanel1 : BaseUIPanel
{

	public string backID { get; set; }

	protected override void Initialize()
	{
		base.Initialize();

		alpha   = 0;
	}

	public void OnBtnPanel1()
	{
		UIManager.instance.SetState(UIManager.Layer.Main, "panel1");
	}

	public void OnBtnPanel2()
	{
		UIManager.instance.SetState(UIManager.Layer.Main, "panel2");
	}

	public void OnBtnClose()
	{
		UIManager.instance.SetState(UIManager.Layer.Main, backID);
	}
}
