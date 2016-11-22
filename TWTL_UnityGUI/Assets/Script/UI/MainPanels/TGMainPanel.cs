using UnityEngine;
using System.Collections;

public class TGMainPanel : BaseUIPanel
{
	protected override void Initialize()
	{
		base.Initialize();

		alpha       = 0;
	}

	public void OnButtonRegistryUI()
	{
		TGUI.CallRegistryUI();
	}
}
