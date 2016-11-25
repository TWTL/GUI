using UnityEngine;
using System.Collections;
using System;

public static class TGTrapProcedures
{
	public class NetworkChangeTrap : TGProtocolModule.BaseProcedure
	{
		public override string procedurePath
		{
			get
			{
				return "/Net/Connections/";
			}
		}

		public NetworkChangeTrap()
		{
			RegisterFunction(FunctionType.change, (param) =>
			{
				TGNotificationPanel.instance.ShowNotification("netChange", "네트워크 변경점이 발생했습니다!");

				SendMessage(FunctionType.check, new JSONObject(true));
			});
		}

		protected override bool OnCallFinish()
		{
			return true;
		}
	}

	public class RegistryChangeTrap : TGProtocolModule.BaseProcedure
	{
		public override string procedurePath
		{
			get
			{
				return "/Reg/Short/";
			}
		}

		public RegistryChangeTrap()
		{
			RegisterFunction(FunctionType.change, (param) =>
			{
				TGNotificationPanel.instance.ShowNotification("regChange", "레지스트리 변경점이 발생했습니다!");

				SendMessage(FunctionType.check, new JSONObject(true));
			});
		}

		protected override bool OnCallFinish()
		{
			return true;
		}
	}
	//


	// Members

	public static RegistryChangeTrap procRegChangeTrap { get; private set; }
	public static NetworkChangeTrap procNetChangeTrap { get; private set; }

	static TGTrapProcedures()
	{
		procRegChangeTrap   = new RegistryChangeTrap();
		procNetChangeTrap   = new NetworkChangeTrap();
	}

	public static void RegisterProcedures()
	{
		var procModule  = TGProtocolModule.instance;
		procModule.RegisterProcedure(procRegChangeTrap, true);
		procModule.RegisterProcedure(procNetChangeTrap, true);

		// setup noti events
		var noti    = TGNotificationPanel.instance;
		noti.SetEventDelegate("netChange", () =>
		{
			new TGNetworkProcedures.NetworkConnectionsChain().StartChain();
		});

		noti.SetEventDelegate("regChange", () =>
		{
			new TGRegistryProcedures.RegistryGetChain().StartChain();
		});
	}
}
