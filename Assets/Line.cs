﻿using UnityEngine;
using UnityEngine.EventSystems;

public class Line : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
	[SerializeField] float width;
	[SerializeField] float baseY;
	[SerializeField] new LineRenderer renderer;
	[SerializeField] new MeshCollider collider;

	public void ManualStart(QuestionSubScene subScene, Camera camera)
	{
		this.subScene = subScene;
		this.camera = camera;
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

	public void GenerateCollider()
	{
		if (mesh == null)
		{
			mesh = new Mesh();
			mesh.name = "LineBaked";
		}
		renderer.BakeMesh(mesh, camera, false);
		collider.sharedMesh = mesh;
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		subScene.OnLineDown(this);
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		subScene.OnLineUp();
	}

	// non public ------
	Mesh mesh;
	QuestionSubScene subScene;
	new Camera camera;
}
