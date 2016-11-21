using UnityEngine;
using System.Collections;
using System;

public static class TestTrapProcedure
{
	public class TestTrap : TGProtocolModule.BaseProcedure
	{
		public override string procedurePath
		{
			get
			{
				return "/Test/Trap/";
			}
		}

		string      m_message   = "";

		public TestTrap()
		{
			RegisterFunction(FunctionType.set, (param) =>
			{
				m_message	= param.str;
			});
		}

		protected override bool OnCallFinish()
		{
			TGUI.GetTrapMessagePanelBuilder()
					.SetMessage("Test Trap : " + m_message)
					.AddButton("확인")
					.Show();

			return true;
		}
	}

	public static void RegisterProcedures()
	{
		var procModule  = TGProtocolModule.instance;

		procModule.RegisterProcedure(new TestTrap(), true);
	}
}
