using UnityEngine;
using System.Collections;
using System;

public class TestUIList : DynamicUIList<TestUIListItem, string>
{
	protected override void Initialize()
	{
		itemHeight  = 120f;
	}

	protected override void OnAfterItemCreate(TestUIListItem item)
	{
		base.OnAfterItemCreate(item);

		item.onClick += (index) =>
		{
			RemoveItem(index);
		};
	}
}
