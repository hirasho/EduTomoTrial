using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnswerZone : MonoBehaviour
{
	[SerializeField] RectTransform[] rectTransforms;
	
	public RectTransform[] RectTransforms { get => rectTransforms; }

	public void GetScreenBounds(out Vector2 min, out Vector2 max, Camera camera)
	{
		min = Vector2.one * float.MaxValue;
		max = -min;
		foreach (var rect in rectTransforms)
		{
			var p = rect.position;
			var sp = camera.WorldToScreenPoint(p);
			if (camera.targetTexture != null)
			{
				sp.y = camera.targetTexture.height - sp.y;
			}
			min = Vector2.Min(min, sp);
			max = Vector2.Max(max, sp);
		}
	}

	public Bounds GetBounds()
	{
		var min = Vector3.one * float.MaxValue;
		var max = -min;
		foreach (var rect in rectTransforms)
		{
			min = Vector3.Min(min, rect.position);
			max = Vector3.Min(max, rect.position);
		}
		return new Bounds(
			(min + max) * 0.5f,
			(max - min));
	}

}
