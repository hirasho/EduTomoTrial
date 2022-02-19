using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class ButtonEventsHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
	public Action OnDown { get; set; }
	public Action OnUp { get; set; }
	public Action OnClick { get; set; }

	public void OnPointerDown(PointerEventData eventData)
	{
		OnDown?.Invoke();
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		OnUp?.Invoke();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		OnClick?.Invoke();
	}
}
