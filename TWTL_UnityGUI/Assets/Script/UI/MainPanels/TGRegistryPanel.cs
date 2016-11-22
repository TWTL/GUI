using UnityEngine;
using System.Collections;

public class TGRegistryPanel : BaseUIPanel
{
	[SerializeField]
	TestUIList      m_testList;

	protected override void Initialize()
	{
		base.Initialize();

		alpha       = 0;
	}

	protected override void OnOpenTransitionEnd()
	{
		for (var i = 0; i < 20; i++)
		{
			m_testList.AddNewItem("테스트 " + (i + 1));
		}
	}
}
