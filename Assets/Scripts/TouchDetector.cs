using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TouchDetector : BaseRaycaster
{
	[SerializeField] Camera attachedCamera;
	[SerializeField] GameObject receiver;
	[SerializeField] float distanceFromCamera = 1000f;

	public Vector2 NormalizedScreenPosition { get; private set; }
	public Vector2 ScreenPosition { get; private set; }

	public override Camera eventCamera{ get => attachedCamera; }

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

		// 正規化スクリーン座標を返す左上(0,1)、右下(1,0)
		var vmin = Mathf.Min(Screen.height, Screen.width);
		NormalizedScreenPosition = eventData.position / (float)vmin;
		ScreenPosition = eventData.position;
	}
}