using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TouchDetector : BaseRaycaster
{
	[SerializeField] Camera attachedCamera;
	[SerializeField] GameObject receiver;
	[SerializeField] float distanceFromCamera = 1000f;

	public override Camera eventCamera{ get => attachedCamera; }

	public Vector2 GetScreenPosition(int pointerId)
	{
		Vector2 ret;
		if (!pointers.TryGetValue(pointerId, out ret))
		{
			ret = Vector2.one * float.MaxValue;
		}
		return ret;
	}

	public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
	{
		var cameraTransform = attachedCamera.transform;
		var result = new RaycastResult
		{
			gameObject = receiver,
			module = this,
			distance = distanceFromCamera,
			worldPosition = cameraTransform.position + (cameraTransform.forward * distanceFromCamera),
			worldNormal = -cameraTransform.forward,
			screenPosition = eventData.position,
			index = resultAppendList.Count,
			sortingLayer = 0,
			sortingOrder = 0
		};
		resultAppendList.Add(result);

		if (pointers == null)
		{
			pointers = new Dictionary<int, Vector2>();
		}
		pointers[eventData.pointerId] = eventData.position;
	}

	// non public --------
	Dictionary<int, Vector2> pointers;
}
