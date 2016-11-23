using UnityEngine;
using System.Collections;

public class TGRegistryPanel : BaseUIPanel
{
	// Properties

	[SerializeField]
	TestUIList			m_testList;
	[SerializeField]
	TGRegistryUIList    m_regList;



	// Members

	RegistryData    m_data;


	protected override void Initialize()
	{
		base.Initialize();

		alpha       = 0;


		m_regList.itemClicked += (item) =>
		{
			TGUI.GetMessagePanelBuilder()
				.SetMessage(string.Format("이 레지스트리값을 어떻게 처리시겠습니까?\n{0}", item.currentData.name))
				.AddButton("삭제", () =>
				{
					var data    = item.currentData;
					new TGRegistryProcedures.SingleRegistryPatch(data.category, data.name, data.value, true)
						.SetFinishDel(() => m_regList.RemoveItem(item.index))
						.StartChain();
				})
				.AddButton("유지", () =>
				{
					var data    = item.currentData;
					new TGRegistryProcedures.SingleRegistryPatch(data.category, data.name, data.value, false)
						.SetFinishDel(() => m_regList.RemoveItem(item.index))
						.StartChain();
				})
				.AddButton("취소")
				.Show();
		};
	}

	protected override void OnOpenTransitionEnd()
	{
		AddRegistryCategory(RegistryData.Category.GlobalServices);
		AddRegistryCategory(RegistryData.Category.GlobalRun);
		AddRegistryCategory(RegistryData.Category.GlobalRunOnce);
		AddRegistryCategory(RegistryData.Category.UserRun);
		AddRegistryCategory(RegistryData.Category.UserRunOnce);
	}

	private void AddRegistryCategory(RegistryData.Category category)
	{
		var dict    = m_data.Get(category);
		var paths   = dict.GetAllPaths();
		var count   = paths.Length;
		for(var i = 0; i < count; i++)
		{
			var param   = new TGRegistryUIListItem.Param()
			{
				category	= category,
				name		= paths[i],
				value		= dict.Get(paths[i]),
			};

			m_regList.AddNewItem(param);
		}
	}

	protected override void OnCloseTransitionEnd()
	{
		m_regList.ClearAll();
	}

	public void SetRegistryData(RegistryData data)
	{
		m_data  = data;
	}

	public void OnBtnClose()
	{
		TGUI.CallMainUI();
	}
}
