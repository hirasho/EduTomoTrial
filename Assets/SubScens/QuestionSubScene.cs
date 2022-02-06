using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
	[SerializeField] UiNumber operand0;
	[SerializeField] UiNumber operand1;
	[SerializeField] UiNumber answer;
	[SerializeField] CountingObject redCubePrefab;
	[SerializeField] CountingObject blueCubePrefab;
	[SerializeField] Crane crane;
	[SerializeField] Transform[] answerZoneTransforms;
	[SerializeField] Transform lineRoot;
	[SerializeField] Line linePrefab;
	[SerializeField] Transform rtLineRoot;
	[SerializeField] Material rtLineMaterial;
	[SerializeField] Camera rtCamera;

	public void ManualStart(Main main, Operation operation, bool allowZero, bool allowCarryBorrow, int questionCount)
	{
		this.main = main;
		this.operation = operation;
		this.allowCarryBorrow = allowCarryBorrow;
		this.allowZero = allowZero;
		this.questionCount = questionCount;

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
		}

		if (ui.NextButtonClicked)
		{
			nextRequested = true;
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
		CompleteEvaluation(response);
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
	int questionCount;
	int questionIndex;
	bool end;
	System.DateTime startTime;

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

	IEnumerator CoQuestion()
	{
		ClearLines();
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

	void CompleteEvaluation(VisionApi.BatchAnnotateImagesResponse body)
	{
		var answer = operand0Value + operand1Value;

		// TODO: どうにかする
		foreach (var response in body.responses)
		{
			var annotations = response.textAnnotations;
			foreach (var annotation in annotations)
			{
				long value = 0;
				foreach (var c in annotation.description)
				{
					var digit = TryReadDigit(c);
					if (digit >= 0)
					{
						value *= 10;
						value += digit;
					}
				}
				if (value == answer)
				{
					Debug.Log("正解: " + value);
					nextRequested = true;
					main.SoundPlayer.Play("クイズ正解2");
				}
				ui.SetDebugMessage(annotation.description);					
			}
		}
	}

	int TryReadDigit(char c)
	{
		var digit = -1;
		if ((c >= '0') && (c <= '9'))
		{
			digit = c - '0';
		}
		else if ((c == 'o') || (c == 'O') || (c == 'D'))
		{
			digit = 0;
		}
		else if ((c == '|') || (c == 'i') || (c == 'I') || (c == 'l') || (c == ')') || (c == '('))
		{
			digit = 1;
		}
		else if ((c == 's') || (c == 'S'))
		{
			digit = 5;
		}
		else if ((c == 'q') || (c == '។') || (c == 'a'))
		{
			digit = 9;
		}
		return digit;
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
		if (operation == Operation.Addition)
		{
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
			op1Min = op1Max = 0;
		}
		operand1Value = UnityEngine.Random.Range(op1Min, op1Max + 1);
		operand0.SetValue(operand0Value);
		operand1.SetValue(operand1Value);
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
