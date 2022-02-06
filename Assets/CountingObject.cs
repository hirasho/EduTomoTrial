using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CountingObject : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
	[SerializeField] new Rigidbody rigidbody;

	public void ManualStart(Main main, Vector3 initialPosition)
	{
		this.main = main;
		rigidbody.MovePosition(initialPosition);
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		main.OnBeginDragCountingObject(rigidbody, eventData.position);
	}

	public void OnDrag(PointerEventData eventData)
	{
		main.OnDragCountingObject(eventData.position);
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		main.OnEndDragCountingObject();		
	}

	// non public 
	Main main;
}
