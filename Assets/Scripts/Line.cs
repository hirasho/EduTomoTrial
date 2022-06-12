using UnityEngine;
using UnityEngine.EventSystems;

public class Line : MonoBehaviour
{
	[SerializeField] float minimumStep = 0.01f;
	[SerializeField] float baseY;
	[SerializeField] new LineRenderer renderer;
	[SerializeField] new MeshCollider collider;
	[SerializeField] LineMeshGenerator generator;

	public void ManualStart(Camera camera, float lineWidth)
	{
		this.camera = camera;
		transform.localPosition = new Vector3(0f, baseY, 0f);
		prevPoint = Vector3.one * float.MaxValue;
		generator.Width = lineWidth;
	}

	public void ReplaceMaterial(Material material)
	{
		generator.SetMaterial(material);
	}

	public void AddPoint(Vector3 p)
	{
		p.y = baseY;
		if (Vector3.Distance(prevPoint, p) > minimumStep)
		{
			prevPoint = p;
			generator.AddPoint(p);
		}
	}

	public void GenerateCollider()
	{
		generator.UpdateMesh();
		collider.sharedMesh = generator.Mesh;
	}

	// non public ------
	new Camera camera;
	Vector3 prevPoint;
}
