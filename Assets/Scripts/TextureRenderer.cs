using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TextureRenderer : MonoBehaviour
{
	[SerializeField] int textureHeight = 216;
	[SerializeField] new Camera camera;

	public Camera Camera { get => camera; }
	public Texture2D SavedTexture { get => savedTexture; }

	public void ManualStart()
	{
		var w = (textureHeight * 16) / 9;
		renderTexture = new RenderTexture(w, textureHeight, 0);
		camera.targetTexture = renderTexture;
		camera.enabled = false;
		savedTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
	}

	public IEnumerator CoRender()
	{
		camera.enabled = true;
		yield return new WaitForEndOfFrame();

		Graphics.SetRenderTarget(renderTexture, 0);
		savedTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), destX: 0, destY: 0);
		camera.enabled = false;

#if UNITY_EDITOR
var jpg = savedTexture.EncodeToJPG();
System.IO.File.WriteAllBytes("rtTest.jpg", jpg);
#endif

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
			EditorGUILayout.ObjectField("TEX", self.savedTexture, typeof(Texture2D), allowSceneObjects: false);
		}
	}
#endif

}
