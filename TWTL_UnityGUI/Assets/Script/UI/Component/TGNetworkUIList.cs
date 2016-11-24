using UnityEngine;
using System.Collections;

public class TGNetworkUIList : DynamicUIList<TGNetworkUIListItem, TGNetworkProcedures.IDataEntry>
{
	public event System.Action<TGNetworkUIListItem>    itemClicked;

	protected override void Initialize()
	{
		itemHeight  = 150;
	}

	protected override void OnAfterItemCreate(TGNetworkUIListItem item)
	{
		item.clicked    += () =>
		{
			OnItemClick(item);
		};
	}

	private void OnItemClick(TGNetworkUIListItem item)
	{
		if (itemClicked != null)
			itemClicked(item);
	}
}
