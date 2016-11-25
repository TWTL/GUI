using UnityEngine;
using System.Collections;

public class TGNetworkPanel : BaseUIPanel
{
	// Properties

	[SerializeField]
	TGNetworkUIList     m_netList;


	// Members

	TGNetworkProcedures.IDataEntry  []      m_netDataArray;


	protected override void Initialize()
	{
		base.Initialize();

		alpha       = 0;


		m_netList.itemClicked += (item) =>
		{
			TGUI.GetMessagePanelBuilder()
				.SetMessage(string.Format("이 프로세스를 어떻게 처리하시겠습니까?\n{0}", item.currentData.ProcessImagePath))
				.AddButton("강제 종료", () =>
				{
					var data    = item.currentData;
					new TGPerfProcedures.RegisterAutoKillChain(data.ProcessImagePath)
						.SetFinishDel(() => m_netList.RemoveItem(item.index))
						.StartChain();
				})
				.AddButton("삭제", () =>
				{
					var data    = item.currentData;
					new TGPerfProcedures.RemoveExecImageChain(data.ProcessImagePath)
						.SetFinishDel(() => m_netList.RemoveItem(item.index))
						.StartChain();
				})
				.AddButton("취소")
				.Show();
		};
	}

	protected override void OnOpenTransitionEnd()
	{
		var count   = m_netDataArray.Length;
		for (var i = 0; i < count; i++)
		{
			var item    = m_netDataArray[i];
			if (item.IsDangerous)				// shows dangerous items only
			{
				m_netList.AddNewItem(item);
			}
		}
	}

	protected override void OnCloseTransitionEnd()
	{
		
	}

	public void SetData(TGNetworkProcedures.IDataEntry [] data)
	{
		m_netDataArray  = data;
	}

	public void OnBtnClose()
	{
		TGUI.CallMainUI();
	}
}
