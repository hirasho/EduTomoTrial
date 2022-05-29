﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class FreeDrawSubScene : SubScene, ILinePointerEventReceiver
{
	[SerializeField] Transform lineRoot;
	[SerializeField] Line linePrefab;
	[SerializeField] Transform rtLineRoot;
	[SerializeField] Material rtLineMaterial;
	[SerializeField] Camera rtCamera;
	[SerializeField] Button clearButton;
	[SerializeField] AnswerZone answerZone;
	[SerializeField] Annotation annotationPrefab;
	
	public void ManualStart(Main main)
	{
		this.main = main;

		annotationViews = new List<Annotation>();
		lines = new List<Line>();
		rtCamera.enabled = false;

		clearButton.onClick.AddListener(() =>
		{
			clearButtonClicked = true;
		});
	}

	public override SubScene ManualUpdate(float deltaTime)
	{
		if (clearButtonClicked)
		{
			clearButtonClicked = false;
			ClearLines();	
			ClearAnnotations();
		}

		if (pointerDown)
		{
			var pointer = main.TouchDetector.ScreenPosition;
			if (drawing)
			{
				if (lines.Count > 0)
				{
					var ray = main.MainCamera.ScreenPointToRay(pointer);
					// y=0点を取得
					var t = -ray.origin.y / ray.direction.y;
					var p = ray.origin + (ray.direction * t);
					lines[lines.Count - 1].AddPoint(p);
				}
			}
			prevPointer = pointer;
		}

		SubScene nextScene = null;
		return nextScene;
	}

	public override void OnPointerDown()
	{
		var line = Instantiate(linePrefab, lineRoot, false);
		line.ManualStart(this, main.MainCamera, main.DefaultLineWidth);
		if (lines.Count > 0)
		{
			lines[lines.Count - 1].GenerateCollider();
		}
		lines.Add(line);
		pointerDown = true;
		drawing = true;
		prevPointer = main.TouchDetector.ScreenPosition;
	}

	public override void OnPointerUp()
	{
		if (drawing)
		{
			if (lines.Count > 0)
			{
				lines[lines.Count - 1].GenerateCollider();
			}
			StartCoroutine(CoRequestEvaluation());
		}
		drawing = false;
		pointerDown = false;
	}

	public void OnLineDown(Line line)
	{
		OnPointerDown();
	}

	public void OnLineUp()
	{
		OnPointerUp();
	}
	
	public override void OnVisionApiDone(VisionApi.BatchAnnotateImagesResponse response)
	{
		if (response != null)
		{
			ClearAnnotations();
			var letters = Evaluator.GetLetters(response);
			ShowAnnotations(letters);
		}
	}

	// non public -------
	Main main;
	Color32[] prevRtTexels;
	List<Line> lines;
	Vector2 prevPointer;
	bool pointerDown;
	bool drawing;
	List<Annotation> annotationViews;
	bool clearButtonClicked;

	void ClearLines()
	{
		foreach (var line in lines)
		{
			Destroy(line.gameObject);
		}
		lines.Clear();
	}

	void ClearAnnotations()
	{
		foreach (var annotation in annotationViews)
		{
			Destroy(annotation.gameObject);
		}
		annotationViews.Clear();
	}

	IEnumerator CoRequestEvaluation()
	{
		if (main.VisionApi == null)
		{
			yield break;
		}
		// カメラ位置合わせ
		var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
		var max = -min;
		foreach (var t in answerZone.RectTransforms)
		{
			min = Vector3.Min(min, t.position);
			max = Vector3.Max(max, t.position);
		}
		var center = (min + max) * 0.5f;
		center.y += 10f;
Debug.Log(min +  " " + max);
		rtCamera.transform.localPosition = center;
		rtCamera.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
		rtCamera.orthographicSize = Mathf.Max((max.x - min.x) / rtCamera.aspect, max.z - min.z);

		// Lineを全部コピー
		var rtLines = new List<Line>();
		foreach (var line in lines)
		{
			var rtLine = Instantiate(line, rtLineRoot, false);
			rtLine.ReplaceMaterial(rtLineMaterial);
			rtLines.Add(rtLine);
		}
		rtCamera.enabled = true;

		// 描画待ち
		yield return new WaitForEndOfFrame();
		rtCamera.enabled = false;
		// 即破棄
		foreach (var line in rtLines)
		{
			Destroy(line.gameObject);
		}

		var rt = rtCamera.targetTexture;
		Graphics.SetRenderTarget(rt, 0);
		// 読み出し用テクスチャを生成して差し換え
		var texture2d = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
		texture2d.ReadPixels(new Rect(0, 0, rt.width, rt.height), destX: 0, destY: 0);

		var dirty = false;
		var newTexels = texture2d.GetPixels32();
		if (prevRtTexels == null)
		{
			dirty = true;
		}
		else if (newTexels.Length != prevRtTexels.Length)
		{
			dirty = true;
		}
		else
		{
			for (var i = 0; i < newTexels.Length; i++)
			{
				if ((newTexels[i].r != prevRtTexels[i].r) || 
					(newTexels[i].g != prevRtTexels[i].g) ||
					(newTexels[i].b != prevRtTexels[i].b))
				{
					dirty = true;
					break;
				}
			}
		}
		prevRtTexels = newTexels;

		if (dirty)
		{
			if (!main.VisionApi.IsDone()) // 前のが終わってないので止める
 			{
				main.VisionApi.Abort();
			}
			main.VisionApi.Request(texture2d);
		}
	}

	void ShowAnnotations(IList<Evaluator.Letter> letters)
	{
		foreach (var letter in letters)
		{
			// 頂点抽出
			var srcVertices = letter.vertices;
			var dstVertices = new Vector3[srcVertices.Count];
			var center = Vector3.zero;
			var min = Vector3.one * float.MaxValue;
			var max = -min;
			var inBox = false;
			for (var i = 0; i < srcVertices.Count; i++)
			{
				var srcV = srcVertices[i];
				if ((srcV.x >= 64) && (srcV.x < 192) && (srcV.y >= 48) && (srcV.y <= 144))
				{
					inBox = true;
				}
				var sp = new Vector3(srcV.x, rtCamera.targetTexture.height - srcV.y);
				var ray = rtCamera.ScreenPointToRay(sp);
				var t = (0f - ray.origin.y) / ray.direction.y;
				var wp = ray.origin + (ray.direction * t);
				center += wp;
				min = Vector3.Min(min, wp);
				max = Vector3.Max(max, wp);
			}

			if (inBox)
			{
				var obj = Instantiate(annotationPrefab, transform, false);
				center /= srcVertices.Count;
				obj.Show(center + new Vector3(-10f, 0f, 0f), max - min, letter.text, letter.correct);
//Debug.Log("\t " + letter.text + " " + letter.correct);
				annotationViews.Add(obj);
			}
		}
	}
}
