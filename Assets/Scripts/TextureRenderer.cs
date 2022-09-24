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
	[SerializeField] Transform scalingImageRotation;

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

	public Matrix4x4 GetImageToRtTransform(Vector2 scale, float rotation)
	{
		var imageScale = CalcImageScale(scale, rotation);
		var w = Mathf.CeilToInt(renderTexture.width * scale.x);
		var h = Mathf.CeilToInt(renderTexture.height * scale.y);
		var ret = Matrix4x4.Translate(new Vector3(-renderTexture.width * 0.5f, -renderTexture.height * 0.5f, 0f));
		ret = Matrix4x4.Scale(new Vector3(imageScale.x, imageScale.y, 1f)) * ret;
		ret = Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, rotation)) * ret;
		ret = Matrix4x4.Translate(new Vector3(renderTexture.width * 0.5f, renderTexture.height * 0.5f, 0f)) * ret;
		ret = Matrix4x4.Scale(new Vector3(1f, -1f, 1f)) * ret;
		ret = Matrix4x4.Translate(new Vector3(0f, h, 0f)) * ret;
		ret = ret.inverse;
		return ret;
	}

	Vector2 CalcImageScale(Vector2 scale, float rotation)
	{
		var ret = scale;
		// 回転角に応じて縮小 s = a/(a*cosθ + b*sinθ) 拡大率s a長辺 b短辺
		var a = (float)renderTexture.width;
		var b = (float)renderTexture.height;
		var theta = Mathf.Abs(rotation) * Mathf.Deg2Rad;
		var rotScale0 = a / ((a * Mathf.Cos(theta)) + (b * Mathf.Sin(theta)));
		var rotScale1 = b / ((a * Mathf.Sin(theta)) + (b * Mathf.Cos(theta)));
		var rotScale = Mathf.Min(rotScale0, rotScale1);
//Debug.Log(rotScale0 + " " + rotScale1 + " " + angle + " " + a + " " + b);
		ret *= rotScale;
		return ret;
	}

	public IEnumerator CoRender(Vector2 scale, float rotation)
	{
		var imageScale = CalcImageScale(scale, rotation);
		// 回転角に応じて縮小 s = a/(a*cosθ + b*sinθ) 拡大率s a長辺 b短辺
		var a = (float)renderTexture.width;
		var b = (float)renderTexture.height;
		scalingImage.rectTransform.localScale = new Vector3(imageScale.x, imageScale.y, 1f);
		scalingImageRotation.localRotation = Quaternion.Euler(0f, 0f, rotation);
		camera.enabled = true;
		scalingCamera.enabled = true;

		yield return new WaitForEndOfFrame();

		Graphics.SetRenderTarget(scaledTexture, 0);

		var w = Mathf.CeilToInt(renderTexture.width * scale.x);
		var h = Mathf.CeilToInt(renderTexture.height * scale.y);
		if (savedTexture != null)
		{
			Resources.UnloadAsset(savedTexture);
		}
		savedTexture = new Texture2D(w, h, TextureFormat.RGBA32, false);
		savedTexture.ReadPixels(new Rect(0, 0, w, h), destX: 0, destY: 0);

		camera.enabled = false;
		scalingCamera.enabled = false;

#if UNITY_EDITOR
var jpg = savedTexture.EncodeToJPG();
System.IO.File.WriteAllBytes("rtTest.jpg", jpg);
#endif
	}

	public void TransformToRtScreen(TextRecognizer.Text text, Vector2 scale, float rotation)
	{
		var imageToRt = GetImageToRtTransform(scale, rotation);
		foreach (var word in text.words)
		{
			TransformToRtScreen(word, imageToRt);
		}
	}

	public void TransformToRtScreen(TextRecognizer.Word word, Matrix4x4 imageToRt)
	{
		TransformToRtScreen(
			out word.boundsMin, 
			out word.boundsMax, 
			word.boundsMin, 
			word.boundsMax,
			imageToRt);
		foreach (var letter in word.letters)
		{
			for (var i = 0; i < letter.vertices.Count; i++)
			{
				letter.vertices[i] = TransformToRtScreen(letter.vertices[i], imageToRt);
			}
		}
	}

	public void TransformToRtScreen(
		out Vector2 minOut,
		out Vector2 maxOut, 
		Vector2 minIn, 
		Vector2 maxIn,
		Matrix4x4 imageToRt)
	{
		// 4点定義して全部変換してしまう
		var points = new List<Vector2>();
		points.Add(new Vector3(minIn.x, minIn.y));
		points.Add(new Vector3(minIn.x, maxIn.y));
		points.Add(new Vector3(maxIn.x, minIn.y));
		points.Add(new Vector3(maxIn.x, maxIn.y));
		minOut = Vector2.one * float.MaxValue;
		maxOut = -minOut;
		for (var i = 0; i < points.Count; i++)
		{
			points[i] = TransformToRtScreen(points[i], imageToRt);
			minOut = Vector2.Min(minOut, points[i]);
			maxOut = Vector2.Max(maxOut, points[i]);
		}
	}

	public static Vector2 TransformToRtScreen(Vector2 p, Matrix4x4 imageToRt)
	{
		var p3 = imageToRt.MultiplyPoint3x4(p);
		return new Vector2(p3.x, p3.y);
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
	Matrix4x4 imageToRt;


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
