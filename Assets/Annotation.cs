using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Annotation : MonoBehaviour
{
	[SerializeField] Transform cubeTransform;
	[SerializeField] TextMesh textMesh;

	public void Show(Vector3 center, Vector3 size, string text)
	{
		cubeTransform.position = center + new Vector3(0f, 0.05f, 0f);
		cubeTransform.localScale = size;
		textMesh.text = text;
		textMesh.transform.position = center + new Vector3(0f, 0.1f, (size.z * 0.5f) + 0.01f);
	}
}

