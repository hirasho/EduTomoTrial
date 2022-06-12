using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Eraser : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
	[SerializeField] Vector3 defaultPosition;

	public Vector3 DefaultPosition { get => defaultPosition; }
	public bool PointerDown { get; private set; }

	public void ManualStart(IEraserEventReceiver receiver)
	{
		this.receiver = receiver;		
	}

	void OnTriggerStay(Collider collider)
	{
		if (PointerDown)
		{
			var line = collider.gameObject.GetComponent<Line>();
			if (line != null)
			{
				receiver.OnEraserHitLine(line);
			}
		}
	}

	public void OnPointerDown(PointerEventData data)
	{
		PointerDown = true;
		receiver.OnEraserDown();
	}

	public void OnPointerUp(PointerEventData data)
	{
		PointerDown = false;
		receiver.OnEraserUp();
	}
	// non public ----
	IEraserEventReceiver receiver;
}
