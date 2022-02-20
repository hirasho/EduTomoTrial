using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestionSubScene : SubScene
{
	public enum Operation
	{
		Addition,
		Subtraction,
	}
	[SerializeField] float countingObjectGrabY = 0.1f;
	[SerializeField] MainUi ui;
	[SerializeField] Transform countObjectRoot;
	[SerializeField] Text questionText;
//	[SerializeField] UiNumber operand0;
//	[SerializeField] Text operatorText;
//	[SerializeField] UiNumber operand1;
//	[SerializeField] UiNumber answer;
	[SerializeField] CountingObject redCubePrefab;
	[SerializeField] CountingObject blueCubePrefab;
	[SerializeField] Crane crane;
	[SerializeField] Transform[] answerZoneTransforms;
	[SerializeField] Transform lineRoot;
	[SerializeField] Line linePrefab;
	[SerializeField] Transform rtLineRoot;
	[SerializeField] Material rtLineMaterial;
	[SerializeField] Camera rtCamera;
	[SerializeField] Annotation annotationPrefab;
	
	public void ManualStart(
		Main main, 
		Operation operation, 
		bool allowZero, 
		bool allowCarryBorrow, 
		bool under1000,
		int questionCount)
	{
		this.main = main;
		this.operation = operation;
		this.allowCarryBorrow = allowCarryBorrow;
		this.allowZero = allowZero;
		this.questionCount = questionCount;
		this.under1000 = under1000;

		annotationViews = new List<Annotation>();
		lines = new List<Line>();
		countingObjects = new List<CountingObject>();
		ui.ManualStart();
		rtCamera.enabled = false;
		startTime = System.DateTime.Now;

		StartCoroutine(CoQuestionLoop());
	}

	public override SubScene ManualUpdate(float deltaTime)
	{
		if (ui.ClearButtonClicked)
		{
			ClearLines();	
			ClearAnnotations();
		}

		ui.ManualUpdate(deltaTime);

		if (pointerDown)
		{
			var pointer = main.TouchDetector.ScreenPosition;
			var ray = main.MainCamera.ScreenPointToRay(pointer);
			// y=0点を取得
			var t = -ray.origin.y / ray.direction.y;
			var p = ray.origin + (ray.direction * t);
			lines[lines.Count - 1].AddPoint(p);
		}

		SubScene nextScene = null;
		if (end)
		{
			var result = SubScene.Instantiate<ResultSubScene>(transform.parent);
			var time = (System.DateTime.Now - startTime).TotalSeconds;
			result.ManualStart(main, (float)time);
			nextScene = result;
		}
		return nextScene;
	}

	public override void OnPointerDown()
	{
		var line = Instantiate(linePrefab, lineRoot, false);
		line.ManualStart();
		lines.Add(line);
		pointerDown = true;
	}

	public override void OnPointerUp()
	{
		StartCoroutine(CoRequestEvaluation());
		pointerDown = false;
	}

	public void OnBeginDragCountingObject(Rigidbody rigidbody, Vector2 screenPosition)
	{
		var ray = GetComponent<Camera>().ScreenPointToRay(screenPosition);
		var t = (countingObjectGrabY - ray.origin.y) / ray.direction.y;
		var p = ray.origin + (ray.direction * t);
		crane.Grab(rigidbody, p);
	}

	public void OnDragCountingObject(Vector2 screenPosition)
	{
		var ray = GetComponent<Camera>().ScreenPointToRay(screenPosition);
		var t = (countingObjectGrabY - ray.origin.y) / ray.direction.y;
		var p = ray.origin + (ray.direction * t);
		crane.SetPosition(p);		
	}

	public void OnEndDragCountingObject()
	{
		crane.Release();		
	}

	public override void OnVisionApiDone(VisionApi.BatchAnnotateImagesResponse response)
	{
		ClearAnnotations();
		var answer = operand0Value;
		if (operation == Operation.Addition)
		{
			answer += operand1Value;
		}
		else if (operation == Operation.Subtraction)
		{
			answer -= operand1Value;
		}
		bool correct;
		var letters = Evaluator.Evaluate(response, answer, out correct);
Debug.Log("Evaluated " + letters.Count + " " + correct);
		if (correct)
		{
			nextRequested = true;
			main.SoundPlayer.Play("クイズ正解2");
		}
		ShowAnnotations(letters);
	}

	// non public -------
	Main main;
	int operand0Value;
	int operand1Value;
	List<CountingObject> countingObjects;
	bool nextRequested;
	Color32[] prevRtTexels;
	List<Line> lines;
	bool pointerDown;
	Operation operation;
	bool allowZero;
	bool allowCarryBorrow;
	bool under1000;
	int questionCount;
	int questionIndex;
	bool end;
	System.DateTime startTime;
	List<Annotation> annotationViews;

	IEnumerator CoQuestionLoop()
	{
		while (questionIndex < questionCount)
		{
			yield return CoQuestion();
		}
		end = true;
	}

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

	IEnumerator CoQuestion()
	{
		ClearLines();
		ClearAnnotations();
		UpdateQuestion();
		while (!nextRequested)
		{
			yield return null;
		}
		nextRequested = false;
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
		foreach (var t in answerZoneTransforms)
		{
			min = Vector3.Min(min, t.position);
			max = Vector3.Max(max, t.position);
		}
		var center = (min + max) * 0.5f;
		center.y += 10f;

		rtCamera.transform.localPosition = center;
		rtCamera.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
		rtCamera.orthographicSize = Mathf.Max(max.x - min.x, max.z - min.z);

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
Debug.Log("D: " + i + " " + (i % rtCamera.targetTexture.width) + " " + (i / rtCamera.targetTexture.width) + " " + newTexels[i] + " <> " + prevRtTexels[i]);
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
			for (var i = 0; i < srcVertices.Count; i++)
			{
				var srcV = srcVertices[i];
				var sp = new Vector3(srcV.x, rtCamera.targetTexture.height - srcV.y);
				var ray = rtCamera.ScreenPointToRay(sp);
				var t = (0f - ray.origin.y) / ray.direction.y;
				var wp = ray.origin + (ray.direction * t);
				center += wp;
				min = Vector3.Min(min, wp);
				max = Vector3.Max(max, wp);
			}

			var obj = Instantiate(annotationPrefab, transform, false);
			center /= srcVertices.Count;
			obj.Show(center + new Vector3(-10f, 0f, 0f), max - min, letter.text, letter.correct);
//Debug.Log("\t " + letter.text + " " + letter.correct);
			annotationViews.Add(obj);
		}
	}

	void UpdateQuestion()
	{
		for (var i = 0; i < countingObjects.Count; i++)
		{
			Destroy(countingObjects[i].gameObject);
		}
		countingObjects.Clear();

		questionIndex++;
		ui.SetQuestionIndex(questionIndex, questionCount);

		int op0Min, op0Max, op1Min, op1Max;
		char operatorChar;
		if (under1000)
		{
//			operand0.SetHeight(120f);
//			operand1.SetHeight(120f);
			operation = (UnityEngine.Random.value < 0.5f) ? Operation.Addition : Operation.Subtraction;
			if (operation == Operation.Addition)
			{
				operatorChar = '+';
//				operatorText.text = "+";
				op0Min = op1Min = 0;
				op0Max = UnityEngine.Random.Range(0, 999);
				operand0Value = UnityEngine.Random.Range(op0Min, op0Max + 1);
				op1Max = UnityEngine.Random.Range(0, 999 - operand0Value);
			}
			else if (operation == Operation.Subtraction)
			{
				operatorChar = '-';
//				operatorText.text = "-";
				op0Min = op1Min = 0;
				op0Max = UnityEngine.Random.Range(0, 999);
				operand0Value = UnityEngine.Random.Range(op0Min, op0Max + 1);
				op1Max = UnityEngine.Random.Range(0, operand0Value);
			}
			else
			{
				Debug.Assert(false, "ARIENAI");
				op0Min = op0Max = op1Min = op1Max = 0;
				operatorChar = '?';
			}
		}
		else if (operation == Operation.Addition)
		{
			operatorChar = '+';
//			operand0.SetHeight(300f);
//			operand1.SetHeight(300f);
//			operatorText.text = "+";
			op0Min = allowZero ? 0 : 1;
			op1Min = allowZero ? 0 : 1;
			if (allowCarryBorrow)
			{
				op0Max = 9;
				op1Max = 9;
				operand0Value = UnityEngine.Random.Range(op0Min, op0Max + 1);
			}
			else
			{
				op0Max = allowZero ? 10 : 9;
				operand0Value = UnityEngine.Random.Range(op0Min, op0Max + 1);
				op1Max = 10 - operand0Value;				
			}
		}
		else if (operation == Operation.Subtraction)
		{
			operatorChar = '-';
//			operand0.SetHeight(300f);
//			operand1.SetHeight(300f);
//			operatorText.text = "-";
			op0Min = allowZero ? 0 : 2;
			if (allowCarryBorrow)
			{
				op0Max = 18;
				operand0Value = UnityEngine.Random.Range(op0Min, op0Max + 1);
			}
			else
			{
				op0Max = 10;
				operand0Value = UnityEngine.Random.Range(op0Min, op0Max + 1);
			}
			op1Min = allowZero ? 0 : 1;
			op1Max = allowZero ? Mathf.Min(operand0Value, 9) : Mathf.Min(operand0Value - 1, 9);
		}
		else
		{
			Debug.Assert(false, "ARIENAI");
			op0Min = op0Max = op1Min = op1Max = 0;
			operatorChar = '?';
		}
		operand1Value = UnityEngine.Random.Range(op1Min, op1Max + 1);
Debug.Log(op0Min + " " + op0Max + " " + op1Min + " " + op1Max + " " + operand0Value + " " + operand1Value);
		questionText.text = string.Format("{0} {1} {2} =", operand0Value, operatorChar, operand1Value);
//		operand0.SetValue(operand0Value);
//		operand1.SetValue(operand1Value);
//		var ans = operand0Value + operand1Value;
//		answer.SetValue(ans); 

		var center = new Vector3(-0.7f, 1f, 0.3f);
		for (var i = 0; i < operand0Value; i++)
		{
			var obj = Instantiate(redCubePrefab, countObjectRoot, false);
			var p = center + new Vector3(0.15f * i, 0.5f * i, 0f);
			obj.ManualStart(this, p);
//Debug.Log(p);
			countingObjects.Add(obj);
		}

		center = new Vector3(-0.6f, 1f, -0.3f);
		for (var i = 0; i < operand1Value; i++)
		{
			var obj = Instantiate(blueCubePrefab, countObjectRoot, false);
			var p = center + new Vector3(0.15f * i, 0.5f * i, 0f);
			obj.ManualStart(this, p);
//Debug.Log(p);
			countingObjects.Add(obj);
		}
	}
}
