using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Line : MonoBehaviour
{
	[SerializeField] float width;
	[SerializeField] float baseY;
	[SerializeField] new LineRenderer renderer;

	public void ManualStart()
	{
		renderer.startWidth = renderer.endWidth = width;
		renderer.positionCount = 0;
		transform.localPosition = new Vector3(0f, baseY, 0f);
	}

	public void ReplaceMaterial(Material material)
	{
		renderer.sharedMaterial = material;
	}

	public void AddPoint(Vector3 p)
	{
		p.y = baseY;
		var index = renderer.positionCount;
		renderer.positionCount++;
		renderer.SetPosition(index, p);
	}
}
