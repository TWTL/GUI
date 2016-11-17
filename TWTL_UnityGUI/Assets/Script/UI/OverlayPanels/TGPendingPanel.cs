using UnityEngine;
using System.Collections;

public class TGPendingPanel : BaseUIPanel
{
	protected override void Initialize()
	{
		base.Initialize();

		alpha       = 0;
	}

	protected override void OnOpenTransitionStart()
	{
		base.OnOpenTransitionStart();


	}
}
