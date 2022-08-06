using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class FreeDrawSubScene : SubScene, IEraserEventReceiver
{
	[SerializeField] Transform lineRoot;
	[SerializeField] Line linePrefab;
	[SerializeField] Transform rtLineRoot;
	[SerializeField] Material rtLineMaterial;
	[SerializeField] Camera rtCamera;
	[SerializeField] Button clearButton;
	[SerializeField] Button abortButton;
	[SerializeField] AnswerZone answerZone;
	[SerializeField] Annotation annotationPrefab;
	[SerializeField] Eraser eraser;
	
	public void ManualStart(Main main)
	{
		this.main = main;
		eraser.ManualStart(this);

		annotationViews = new List<Annotation>();
		rtCamera.enabled = false;

		clearButton.onClick.AddListener(() =>
		{
			clearButtonClicked = true;
		});

		abortButton.onClick.AddListener(() =>
		{
			abortButtonClicked = true;
		});
		drawingManager = new DrawingManager();
	}

	public override SubScene ManualUpdate(float deltaTime)
	{
		SubScene nextScene = null;
		if (clearButtonClicked)
		{
			clearButtonClicked = false;
			ClearLines();	
			ClearAnnotations();
		}

		if (abortButtonClicked)
		{
			var title = SubScene.Instantiate<TitleSubScene>(transform.parent);
			title.ManualStart(main);
			nextScene = title;
		}

		var eraserPosition = eraser.DefaultPosition;
		drawingManager.ManualUpdate(
			ref eraserPosition,
			main.TouchDetector,
			main.MainCamera);
		eraser.transform.position = eraserPosition;

		return nextScene;
	}

	public override void OnPointerDown(int pointerId)
	{
		int strokeCount = 0;
		drawingManager.OnPointerDown(
			ref strokeCount,
			linePrefab,
			lineRoot,
			main.TouchDetector,
			main.MainCamera,
			main.DefaultLineWidth,
			pointerId,
			false);
	}

	public override void OnPointerUp(int pointerId)
	{		
		var eval = false;
		drawingManager.OnPointerUp(out eval, justErased, pointerId);
		if (eval)
		{
			StartCoroutine(CoRequestEvaluation());
		}
		justErased = false;
	}

	public void OnEraserDown(int pointerId)
	{
		int strokeCount = 0;
		drawingManager.OnPointerDown(
			ref strokeCount,
			linePrefab,
			lineRoot,
			main.TouchDetector,
			main.MainCamera,
			main.DefaultLineWidth,
			pointerId,
			isEraser: true);
	}

	public void OnEraserUp(int pointerId)
	{
		OnPointerUp(pointerId);
	}

	public void OnEraserHitLine(Line line)
	{
		int eraseCount = 0;
		drawingManager.RemoveLine(ref eraseCount, ref justErased, line);
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
	List<Annotation> annotationViews;
	bool clearButtonClicked;
	bool abortButtonClicked;
	bool justErased;
	DrawingManager drawingManager;

	void ClearLines()
	{
		drawingManager.ClearLines();
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
		rtCamera.transform.localPosition = center;
		rtCamera.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
		rtCamera.orthographicSize = Mathf.Max((max.x - min.x) / rtCamera.aspect, max.z - min.z);

		// Lineを全部コピー
		var rtLines = new List<Line>();
		foreach (var line in drawingManager.EnumerateLines())
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

		main.VisionApi.Request(rtCamera.targetTexture);
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
				obj.Show(center + new Vector3(-10f, 0f, 0f), max - min, letter.text, ok: true); // 自由帳に不正解はない
//Debug.Log("\t " + letter.text + " " + letter.correct);
				annotationViews.Add(obj);
			}
		}
	}
}
