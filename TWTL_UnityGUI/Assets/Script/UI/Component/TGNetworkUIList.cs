using UnityEngine;
using System.Collections;

public class TGNetworkUIList : DynamicUIList<TGNetworkUIListITem, TGNetworkProcedures.IDataEntry>
{
	public event System.Action<TGNetworkUIListITem>    itemClicked;

	protected override void Initialize()
	{
		itemHeight  = 200;
	}

	protected override void OnAfterItemCreate(TGNetworkUIListITem item)
	{
		item.clicked    += () =>
		{
			OnItemClick(item);
		};
	}

	private void OnItemClick(TGNetworkUIListITem item)
	{
		if (itemClicked != null)
			itemClicked(item);
	}
}
