using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class DynamicUIList<ItemT, ParamT> : MonoBehaviour
	where ItemT : DynamicUIListItem<ParamT>
{
	// Properties

	[SerializeField]
	GameObject			m_originalItem;
	[SerializeField]
	RectTransform       m_contentArea;
	[SerializeField]
	RectTransform       m_viewportArea;


	// Members

	protected float		itemHeight { get; set; }

	Queue<ParamT>       m_newItemQueue  = new Queue<ParamT>();
	List<ItemT>         m_items         = new List<ItemT>();

	Coroutine           m_itemCreateCo;
	Coroutine           m_itemRealignCo;


	private void Awake()
	{
		Initialize();

		if (itemHeight == 0f)
			Debug.LogError("itemHeight should not be zero");
	}

	protected abstract void Initialize();

	private void Update()
	{
		// Keep fitting content area size
		var oldSize		= m_contentArea.rect.height;
		var calcSize    = CalculateContentAreaHeight();
		if (Mathf.Abs(calcSize - oldSize) > 0.01f)
		{
			m_contentArea.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, calcSize);
		}

		UpdateItemVisibility();
	}

	private void UpdateItemVisibility()
	{
		var contentYPos         = m_contentArea.anchoredPosition.y;
		var viewportHeight      = m_viewportArea.rect.height;
		var showingStartIndex   = Mathf.Max(0, Mathf.CeilToInt((contentYPos - 5f) / itemHeight));
		var hidingStartIndex    = Mathf.Max(0, Mathf.CeilToInt((contentYPos + viewportHeight + 5f) / itemHeight));

		var i		= 0;
		var count   = m_items.Count;

		for (i = showingStartIndex - 1; i >= 0; i--)
		{
			var item		= m_items[i];
			if (item.isShowing)
				item.Hide();
			else
				break;
		}

		for (i = showingStartIndex; i < hidingStartIndex && i < count; i++)
		{
			var item        = m_items[i];
			if (!item.isShowing)
				item.Show();
		}

		for (; i < count; i++)
		{
			var item        = m_items[i];
			if (item.isShowing)
				item.Hide();
			else
				break;
		}
	}

	private float CalculateContentAreaHeight()
	{
		return m_items.Count * itemHeight;
	}

	private bool IsInViewportArea(int index)
	{
		var areaHeight  = m_viewportArea.rect.height;
		var contentY    = m_contentArea.anchoredPosition.y;
		return (index + 1) * itemHeight <= areaHeight - contentY;
	}

	private void StartItemCreation()
	{
		if (m_itemCreateCo == null)	// Stary only when the coroutine is not running
			m_itemCreateCo  = StartCoroutine(co_ItemCreation());
	}

	IEnumerator co_ItemCreation()
	{
		while (m_newItemQueue.Count > 0)
		{
			var newItemParam    = m_newItemQueue.Dequeue();
			CreateItem(newItemParam);

			yield return new WaitForSeconds(0.15f);	// item creation delay
		}
	}

	private void CreateItem(ParamT param)
	{
		
		var newGO			= Instantiate<GameObject>(m_originalItem);
		newGO.SetActive(true);

		var rt				= newGO.GetComponent<RectTransform>();
		rt.SetParent(m_contentArea, false);

		var newItem			= newGO.GetComponent<ItemT>();
		var newIndex		= m_items.Count;
		newItem.index		= newIndex;
		newItem.itemHeight  = itemHeight;
		newItem.SetupData(param);

		m_items.Add(newItem);

		if (IsInViewportArea(newIndex))				// if it's in content view area, then show this items
		{
			newItem.Show();
		}
		else
		{                                           // or, disable this item for now.
			newGO.SetActive(false);
		}

		OnAfterItemCreate(newItem);
	}

	protected virtual void OnAfterItemCreate(ItemT item)
	{

	}

	public void AddNewItem(ParamT item)
	{
		m_newItemQueue.Enqueue(item);
		StartItemCreation();
	}

	public void RemoveItem(int index)
	{
		var itemToRemove    = m_items[index];
		m_items.RemoveAt(index);

		itemToRemove.Remove();

		StartRealigning(index);
	}

	private void StartRealigning(int startIndex)
	{
		if (m_itemRealignCo != null)
			StopCoroutine(m_itemRealignCo);
		m_itemRealignCo = StartCoroutine(co_Realign(startIndex));
	}

	IEnumerator co_Realign(int startIndex)
	{
		for(var i = startIndex; i < m_items.Count; i++)
		{
			var item    = m_items[i];

			if (item.isShowing)
				yield return new WaitForSeconds(0.08f);
					
			item.RealignPosition(i);
		}
	}
}
