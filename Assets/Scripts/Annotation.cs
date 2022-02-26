using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Annotation : MonoBehaviour
{
	[SerializeField] Transform cubeTransform;
	[SerializeField] Renderer cubeRenderer;
	[SerializeField] TextMesh textMesh;

	public void Show(Vector3 center, Vector3 size, string text, bool ok)
	{
		cubeTransform.position = center + new Vector3(0f, 0.05f, 0f);
		cubeTransform.localScale = size;
		textMesh.text = text;
		textMesh.transform.position = center + new Vector3(0f, 0.1f, (size.z * 0.5f) + 0.01f);
		
		cubeRenderer.material.SetColor("_Color", ok ? new Color(0f, 0.75f, 0.25f, 0.2f) : new Color(1f, 0f, 0f, 0.2f));
		textMesh.color = ok ? new Color(0f, 0.75f, 0.25f, 1f) : new Color(1f, 0f, 0f, 1f);
	}
}

