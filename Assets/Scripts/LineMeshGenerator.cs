using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineMeshGenerator : MonoBehaviour
{
	[SerializeField] float width = 1f;
	[SerializeField] Vector3 normal = new Vector3(0f, 1f, 0f);
	[SerializeField] MeshRenderer meshRenderer;
	[SerializeField] MeshFilter meshFilter;

	public float Width { get => width; set { width = value; } }
	public Vector3 Normal { get => normal; set { normal = value; } }
	public Mesh Mesh { get; private set; }

	public void Clear()
	{
		if (points != null)
		{
			points.Clear();
		}
		dirty = true;
	}

	public void AddPoint(Vector3 p)
	{
		if (points == null)
		{
			points = new List<Vector3>();
		}
		points.Add(p);
		dirty = true;
	}

	public void SetMaterial(Material material)
	{
		meshRenderer.sharedMaterial = material;
	}

	public void UpdateMesh()
	{
		if (points.Count < 2)
		{
			return;
		}

		if (Mesh == null)
		{
			Mesh = new Mesh();
			Mesh.name = "LineMeshGenerator";
		}

		if (vertices == null)
		{
			vertices = new List<Vector3>();
		}
		vertices.Clear();

		if (indices == null)
		{
			indices = new List<int>();
		}
		indices.Clear();

		Mesh.Clear();

		// 頂点生成
		for (var i = 0; i < points.Count; i++)
		{
			var p1 = points[i];
			var p0 = (i == 0) ? ((points[0] * 2f) - points[1]) : points[i - 1];
			var p2 = (i == (points.Count - 1)) ? ((points[points.Count - 1] * 2f) - points[points.Count - 2]) : points[i + 1];
			var tangent = p2 - p0;
			var binormal = Vector3.Cross(normal, tangent);
			var l = binormal.magnitude;
			Vector3 left;
			if (l == 0f)
			{
				left = Vector3.zero;
			}
			else
			{
				left = binormal * (width * 0.5f / l);
			}
			var v0 = p1 + left;
			var v1 = p1 - left;
			vertices.Add(v0);
			vertices.Add(v1);
		}

		// インデクス
		for (var i = 0; i < (points.Count - 1); i++)
		{
			indices.Add((i * 2) + 0);
			indices.Add((i * 2) + 1);
			indices.Add(((i + 1) * 2) + 0);

			indices.Add(((i + 1) * 2) + 0);
			indices.Add((i * 2) + 1);
			indices.Add(((i + 1) * 2) + 1);
		}

		Mesh.SetVertices(vertices);
		Mesh.SetIndices(indices, MeshTopology.Triangles, submesh: 0);
		meshFilter.sharedMesh = Mesh;
		dirty = false;
	}

	void Update()
	{
		if (dirty)
		{
			UpdateMesh();
		}
	}
	
	// non public ------
	bool dirty;
	List<Vector3> points;
	List<Vector3> vertices;
	List<int> indices;
}
