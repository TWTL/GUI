using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;

public class TGNetworkUIListITem : DynamicUIListItem<TGNetworkProcedures.IDataEntry>
{
	// Properties

	[SerializeField]
	Text            m_source;
	[SerializeField]
	Text            m_destination;
	[SerializeField]
	Text            m_imagePath;
	[SerializeField]
	Text            m_pid;
	[SerializeField]
	Text            m_isAlive;



	// Members

	public event System.Action clicked;


	public override void SetupDataImpl(TGNetworkProcedures.IDataEntry param)
	{
		m_source.text		= string.Format("{0}:{1}", param.SrcIP, param.SrcPort);
		m_destination.text	= string.Format("{0}:{1}", param.DestIP, param.DestPort);
		m_imagePath.text    = param.ProcessImagePath;
		m_pid.text          = param.PID.ToString();
		m_isAlive.text      = param.Alive ? "현재 접속됨!!" : "접속 끊김";
		m_isAlive.color     = param.Alive ? Color.red : Color.grey;
		m_isAlive.fontSize  = param.Alive ? 32 : 28;
	}

	public void OnBtnClick()
	{
		clicked();
	}
}
