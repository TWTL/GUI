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
		//TGUI.CallRegistryUI(null);

		new TGRegistryProcedures.RegistryGetChain().StartChain();
	}

	public void OnButtonNetworkUI()
	{
		new TGNetworkProcedures.NetworkConnectionsChain().StartChain();
	}
}
