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

	public void TransformToRtScreen(TextRecognizer.Text text)
	{
Debug.Log("TransformToRtScreen: " + text.imageWidth + " " + text.imageHeight);
		foreach (var word in text.words)
		{
			TransformToRtScreen(word, text.imageWidth, text.imageHeight);
		}
	}

	public void TransformToRtScreen(TextRecognizer.Word word, int imageWidth, int imageHeight)
	{
		TransformToRtScreen(
			out word.boundsMin, 
			out word.boundsMax, 
			word.boundsMin, 
			word.boundsMax,
			imageWidth,
			imageHeight);
		foreach (var letter in word.letters)
		{
			for (var i = 0; i < letter.vertices.Count; i++)
			{
				letter.vertices[i] = TransformToRtScreen(letter.vertices[i], imageWidth, imageHeight);
			}
		}
	}

	public void TransformToRtScreen(
		out Vector2 minOut,
		out Vector2 maxOut, 
		Vector2 minIn, 
		Vector2 maxIn,
		int imageWidth,
		int imageHeight)
	{
		var sx = (float)renderTexture.width / (float)imageWidth;
		var sy = (float)renderTexture.height / (float)imageHeight; 
		minOut.x = minIn.x * sx;
		maxOut.x = maxIn.x * sx;
		var invMinY = imageHeight - minIn.y; 
		var invMaxY = imageHeight - maxIn.y; 
		minOut.y = invMaxY * sy; // ひっくりかえる
		maxOut.y = invMinY * sy; // ひっくりかえる
	}

	public Vector2 TransformToRtScreen(Vector2 p, int imageWidth, int imageHeight)
	{
		p.y = imageHeight - p.y; // y反転
		// スケール
		p.x *= (float)renderTexture.width / (float)imageWidth;
		p.y *= (float)renderTexture.height / (float)imageHeight;
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
		var minX = Mathf.FloorToInt(min.x);
		var minY = Mathf.FloorToInt(min.y);
		var maxX = Mathf.CeilToInt(max.x);
		var maxY = Mathf.CeilToInt(max.y);
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
