using UnityEngine;
using System.Collections;

public class TGMainPanel : BaseUIPanel
{
	protected override void Initialize()
	{
		base.Initialize();

		alpha       = 0;


		// TEST
		//StartCoroutine(co_test());
	}

	IEnumerator co_test()
	{
		TGNotificationPanel.instance.SetEventDelegate("test1", () =>
		{
			Debug.Log("test1!!!");
		});
		TGNotificationPanel.instance.SetEventDelegate("test2", () =>
		{
			Debug.Log("test2!!!");
		});

		yield return new WaitForSeconds(1);

		TGNotificationPanel.instance.ShowNotification("test1", "알림 111111");
		yield return new WaitForSeconds(0.5f);
		TGNotificationPanel.instance.ShowNotification("test2", "알림 222222");
		yield return new WaitForSeconds(0.5f);
		TGNotificationPanel.instance.ShowNotification("test2", "알림 2222223333");
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
