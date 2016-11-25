using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TGNotificationPanel : MonoBehaviour
{
	// Properties

	[SerializeField]
	GameObject      m_origNotiItem;



	// Members

	Dictionary<string, System.Action>	m_eventDelDict	= new Dictionary<string, System.Action>();
	List<TGNotificationItem>            m_itemList		= new List<TGNotificationItem>();

	public static TGNotificationPanel instance;

	private void Awake()
	{
		instance    = this;
	}
	

	public void SetEventDelegate(string eventID, System.Action del)
	{
		m_eventDelDict[eventID] = del;
	}

	public void ShowNotification(string eventID, string text)
	{
		var count   = m_itemList.Count;
		for(var i = 0; i < count; i++)
		{
			if (m_itemList[i].eventID == eventID)
			{
				Debug.LogWarning("notification already shown : " + eventID);
				return;
			}
		}

		var newGO       = Instantiate<GameObject>(m_origNotiItem);
		newGO.SetActive(true);

		var newTr       = newGO.transform;
		newTr.SetParent(this.transform, false);

		var newItem     = newGO.GetComponent<TGNotificationItem>();
		var del         = m_eventDelDict[eventID];
		newItem.Setup(eventID, text, (id) =>
		{
			RemoveNotification(id);
			del();
		});

		newItem.Show(count);
		m_itemList.Add(newItem);
	}

	public void RemoveNotification(string eventID)
	{
		var count		= m_itemList.Count;
		var delIndex    = 0;

		for (delIndex = 0; delIndex < count; delIndex++)
		{
			if (m_itemList[delIndex].eventID == eventID)
			{
				break;
			}
		}

		if (delIndex < count)		// if there's item to delete
		{
			m_itemList[delIndex].Dismiss();

			for (var i = delIndex + 1; i < count; i++)	// rearrange items below removed one
			{
				m_itemList[i].ChangePosition(i - 1);
			}

			m_itemList.RemoveAt(delIndex);
		}
	}
}
