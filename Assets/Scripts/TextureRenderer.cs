using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TextureRenderer : MonoBehaviour
{
	[SerializeField] int textureHeight = 216;
	[SerializeField] new Camera camera;
	[SerializeField] Camera scalingCamera;
	[SerializeField] RawImage scalingImage;

	public Camera Camera { get => camera; }
	public Texture2D SavedTexture { get => savedTexture; }
	public RenderTexture RenderTexture { get => scaledTexture; }

	public void ManualStart()
	{
		var w = (textureHeight * 16) / 9;
		renderTexture = new RenderTexture(w, textureHeight, 0);
		camera.targetTexture = renderTexture;
		camera.enabled = false;

		scalingImage.texture = renderTexture;
		scaledTexture = new RenderTexture(w, textureHeight, 0);
		scalingCamera.targetTexture = scaledTexture;
		scalingCamera.enabled = false;
	}

	public IEnumerator CoRender(Vector2 scale)
	{
		scalingImage.rectTransform.localScale = new Vector3(scale.x, scale.y, 1f);
		camera.enabled = true;
		scalingCamera.enabled = true;

		yield return new WaitForEndOfFrame();

		Graphics.SetRenderTarget(scaledTexture, 0);

		var w = Mathf.CeilToInt(renderTexture.width * scale.x);
		var h = Mathf.CeilToInt(renderTexture.height * scale.y);
		savedTexture = new Texture2D(w, h, TextureFormat.RGBA32, false);
		savedTexture.ReadPixels(new Rect(0, 0, w, h), destX: 0, destY: 0);

		camera.enabled = false;
		scalingCamera.enabled = false;

#if UNITY_EDITOR
var jpg = savedTexture.EncodeToJPG();
System.IO.File.WriteAllBytes("rtTest.jpg", jpg);
#endif
	}

	public void TransformToRtScreen(IReadOnlyList<TextRecognizer.Word> words)
	{
		foreach (var word in words)
		{
			TransformToRtScreen(word);
		}
	}

	public void TransformToRtScreen(TextRecognizer.Word word)
	{
		TransformToRtScreen(out word.boundsMin, out word.boundsMax, word.boundsMin, word.boundsMax);
		foreach (var letter in word.letters)
		{
			for (var i = 0; i < letter.vertices.Count; i++)
			{
				letter.vertices[i] = TransformToRtScreen(letter.vertices[i]);
			}
		}
	}

	public void TransformToRtScreen(out Vector2 minOut, out Vector2 maxOut, Vector2 minIn, Vector2 maxIn)
	{
		var sx = (float)renderTexture.width / (float)savedTexture.width; 
		var sy = (float)renderTexture.height / (float)savedTexture.height; 
		minOut.x = minIn.x * sx;
		maxOut.x = maxIn.x * sx;
		var invMinY = savedTexture.height - minIn.y; 
		var invMaxY = savedTexture.height - maxIn.y; 
		minOut.y = invMaxY * sy; // ひっくりかえる
		maxOut.y = invMinY * sy; // ひっくりかえる
	}

	public Vector2 TransformToRtScreen(Vector2 p)
	{
		p.y = savedTexture.height - p.y; // y反転
		// スケール
		p.x *= (float)renderTexture.width / (float)savedTexture.width;
		p.y *= (float)renderTexture.height / (float)savedTexture.height;
		return p;
	}

	public RectInt GetRect(AnswerZone answerZone)
	{
		var min = Vector2.one * float.MaxValue;
		var max = -min;
		foreach (var rectTransform in answerZone.RectTransforms)
		{
			var wp = rectTransform.position;
			var sp = camera.WorldToScreenPoint(wp);
//Debug.Log(wp.ToString("F2") + " -> " + sp);
			min = Vector2.Min(min, sp);
			max = Vector2.Max(max, sp);
		}
		var sx = (float)savedTexture.width / (float)renderTexture.width;
		var sy = (float)savedTexture.height / (float)renderTexture.height;
		var minX = Mathf.FloorToInt(min.x * sx);
		var minY = Mathf.FloorToInt(min.y * sy);
		var maxX = Mathf.CeilToInt(max.x * sx);
		var maxY = Mathf.CeilToInt(max.y * sy);
		return new RectInt(minX, minY, maxX - minX, maxY - minY);
	}

	// non public -------
	RenderTexture renderTexture;
	RenderTexture scaledTexture;
	Texture2D savedTexture;


#if UNITY_EDITOR
	[UnityEditor.CustomEditor(typeof(TextureRenderer))]
	class CustomEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			var self = target as TextureRenderer;
			base.OnInspectorGUI();
			EditorGUILayout.ObjectField("RT", self.renderTexture, typeof(RenderTexture), allowSceneObjects: false);
			EditorGUILayout.ObjectField("Scaled", self.scaledTexture, typeof(RenderTexture), allowSceneObjects: false);
			EditorGUILayout.ObjectField("TEX", self.savedTexture, typeof(Texture2D), allowSceneObjects: false);
		}
	}
#endif

}
