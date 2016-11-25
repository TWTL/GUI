using UnityEngine;
using System.Collections;


public class TGEngine : MonoBehaviour
{
	void Start()
	{
		// Initialize process


		// Protocol Startup

		StartCoroutine(co_LateStart());
	}

	IEnumerator co_LateStart()
	{
		yield return null;

		TGUI.ShowPendingUI();

		TGEngineProcedures.RegisterProcedures();
		TGRegistryProcedures.RegisterProcedures();
		//TestTrapProcedure.RegisterProcedures();
		TGNetworkProcedures.RegisterProcedures();
		TGPerfProcedures.RegisterProcedures();

		TGTrapProcedures.RegisterProcedures();

		var comModule   = TGComModule.instance;
		comModule.StartRequestConnection(ReqConnectionCallback);
	}

	void ReqConnectionCallback(TGComModule.Status status)
	{
		if (status != TGComModule.Status.RequestChannelOpen)
		{
			TGUI.GetMessagePanelBuilder()
				.SetMessage("엔진에 연결할 수 없습니다.")
				.AddButton("재시도", RetryConnection)
				.Show();
		}
		else
		{
			var comModule   = TGComModule.instance;
			comModule.StartTrapConnection(TrapConnectionCallback);

			// Start init sequence
			//TGEngineProcedures.procName.SimpleRequestGet();
			new TGEngineProcedures.InitChain().StartChain();
		}
	}

	void TrapConnectionCallback(TGComModule.Status status)
	{
		if (status != TGComModule.Status.FullChannelOpen)
		{
			TGUI.GetMessagePanelBuilder()
				.SetMessage("엔진에 연결할 수 없습니다.")
				.AddButton("재시도", RetryConnection)
				.Show();
		}
		else
		{
			// we're all done, so......
			
			TGUI.ShowIntro();
			//TGUI.HidePendingUI();
			//TGUI.CallMainUI(); // NOTE : we need fancy splashscreen here right before calling main panel.
		}
	}

	void RetryConnection()
	{
		TGUI.ShowPendingUI();

		var comModule   = TGComModule.instance;
		comModule.ResetConnection();
		comModule.StartRequestConnection(ReqConnectionCallback);
	}
	//
}
