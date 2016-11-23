using UnityEngine;
using System.Collections;
using System;

public class TGRegistryUIList : DynamicUIList<TGRegistryUIListItem, TGRegistryUIListItem.Param>
{
	public event System.Action<TGRegistryUIListItem>    itemClicked;

	protected override void Initialize()
	{
		itemHeight  = 150;
	}

	protected override void OnAfterItemCreate(TGRegistryUIListItem item)
	{
		item.clicked    += () =>
		{
			OnItemClick(item);
		};
	}

	private void OnItemClick(TGRegistryUIListItem item)
	{
		if (itemClicked != null)
			itemClicked(item);
	}
}
