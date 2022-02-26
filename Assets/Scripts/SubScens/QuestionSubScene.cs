using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class QuestionSubScene : SubScene
{
	public enum Operation
	{
		Addition,
		Subtraction,
		Multiplication,
		AddAndSub,
	}

	public class Settings
	{
		public Settings(
			string description,
			Operation operation,
			int questionCount,
			int operand0Digits,
			int operand1Digits,
			int answerMinDigits,
			int answerMaxDigits,
			bool allowZero,
			bool invertOperation)
		{
			this.description = description;
			this.operation = operation;
			this.questionCount = questionCount;
			this.operand0Digits = operand0Digits;
			this.operand1Digits = operand1Digits;
			this.answerMinDigits = answerMinDigits;
			this.answerMaxDigits = answerMaxDigits;
			this.allowZero = allowZero;
			this.invertOperation = invertOperation;
		}
			
		public string description;
		public Operation operation;
		public int questionCount;
		public int operand0Digits;
		public int operand1Digits;
		public int answerMinDigits;
		public int answerMaxDigits;
		public bool allowZero;
		public bool invertOperation;
	}
	[SerializeField] float countingObjectGrabY = 0.1f;
	[SerializeField] MainUi ui;
	[SerializeField] Transform countObjectRoot;
	[SerializeField] Text questionText;
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
		Settings settings)
	{
		this.main = main;
		this.settings = settings;

		annotationViews = new List<Annotation>();
		lines = new List<Line>();
		countingObjects = new List<CountingObject>();
		ui.ManualStart();
		rtCamera.enabled = false;
		sessionData = new SessionData(
			settings.operand0Digits, 
			settings.operand1Digits, 
			settings.answerMaxDigits, 
			settings.description,
			main.UserName,
			main.Birthday);
		sessionStartTime = System.DateTime.Now;

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
			if (drawing)
			{
				if (lines.Count > 0)
				{
					// y=0点を取得
					var t = -ray.origin.y / ray.direction.y;
					var p = ray.origin + (ray.direction * t);
					lines[lines.Count - 1].AddPoint(p);
				}
			}
			else
			{
				var hits = Physics.RaycastAll(ray.origin, ray.direction, 1000f, Physics.AllLayers);
				foreach (var hit in hits)
				{
					var line = hit.collider.gameObject.GetComponent<Line>();
					if (line != null)
					{
						RemoveLine(line);
					}
				}
			}
		}

		SubScene nextScene = null;
		if (end)
		{
			var result = SubScene.Instantiate<ResultSubScene>(transform.parent);
			var duration = (System.DateTime.Now - sessionStartTime).TotalSeconds;
			main.OnSessionEnd(sessionData);

			result.ManualStart(main, (float)duration, settings.questionCount);
			nextScene = result;
		}
		return nextScene;
	}

	public override void OnPointerDown()
	{
		if (!ui.EraserDown)
		{
			var line = Instantiate(linePrefab, lineRoot, false);
			line.ManualStart(this, main.MainCamera);
			lines.Add(line);
			drawing = true;
			strokeCount++;
		}
		pointerDown = true;
	}

	public void OnLineDown(Line line)
	{
		if (ui.EraserDown)
		{
			RemoveLine(line);
		}
		else
		{
			OnPointerDown();
		}
	}

	public void OnLineUp()
	{
		OnPointerUp();
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

	public void OnBeginDragCountingObject(Rigidbody rigidbody, Vector2 screenPosition)
	{
		var ray = main.MainCamera.ScreenPointToRay(screenPosition);
		var t = (countingObjectGrabY - ray.origin.y) / ray.direction.y;
		var p = ray.origin + (ray.direction * t);
		crane.Grab(rigidbody, p);
	}

	public void OnDragCountingObject(Vector2 screenPosition)
	{
		var ray = main.MainCamera.ScreenPointToRay(screenPosition);
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
	Settings settings;
	int operand0;
	int operand1;
	int answer;
	List<CountingObject> countingObjects;
	bool nextRequested;
	Color32[] prevRtTexels;
	List<Line> lines;
	bool pointerDown;
	bool drawing;
	int questionIndex;
	bool end;
	List<Annotation> annotationViews;
	System.DateTime sessionStartTime;
	System.DateTime problemStartTime;
	SessionData sessionData;
	int strokeCount;
	int eraseCount;
	string problemText;

	void RemoveLine(Line line)
	{
		var dst = 0;
		for (var i = 0; i < lines.Count; i++)
		{
			lines[dst] = lines[i];
			if (lines[i] == line)
			{
				Destroy(lines[i].gameObject);
				eraseCount++;
			}
			else
			{
				dst++;
			}
		}
		lines.RemoveRange(dst, lines.Count - dst);
	}
	
	IEnumerator CoQuestionLoop()
	{
		while (questionIndex < settings.questionCount)
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
#if UNITY_EDITOR
if (Input.GetKeyDown(KeyCode.S))
{
	nextRequested = true;
}
#endif
			yield return null;
		}
		var seconds = (System.DateTime.Now - problemStartTime).TotalSeconds;
		var problem = new ProblemData(problemText, (float)seconds, strokeCount, eraseCount);
		sessionData.AddProblemData(problem);

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

	void UpdateQuestion()
	{
		for (var i = 0; i < countingObjects.Count; i++)
		{
			Destroy(countingObjects[i].gameObject);
		}
		countingObjects.Clear();

		questionIndex++;
		ui.SetQuestionIndex(questionIndex, settings.questionCount);

		int op0Min, op0Max, op1Min, op1Max, ansMin, ansMax;

		// まず粗く範囲を狭める
		op0Min = Pow10(settings.operand0Digits - 1);
		if (!settings.allowZero && (op0Min == 0))
		{
			op0Min = 1;
		}
		op0Max = Pow10(settings.operand0Digits) - 1;

		op1Min = Pow10(settings.operand1Digits - 1);
		if (!settings.allowZero && (op1Min == 0))
		{
			op1Min = 1;
		}
		op1Max = Pow10(settings.operand1Digits) - 1;

		ansMin = Pow10(settings.answerMinDigits - 1);
		if (!settings.allowZero && (ansMin == 0))
		{
			ansMin = 1;
		}
		ansMax = Pow10(settings.answerMaxDigits) - 1;
		// operand0を雑に決定する
		operand0 = Random.Range(op0Min, op0Max + 1);

		var operation = settings.operation;
		// 加減算両方なら今確定させる
		if (operation == Operation.AddAndSub)
		{
			operation = (Random.value < 0.5f) ? Operation.Addition : Operation.Subtraction;
		}

		char operatorChar;
		if (operation == Operation.Addition)
		{
			// op0の範囲をまず削る
			op0Min = Mathf.Max(op0Min, ansMin - op1Max); // 答えの最大+op1の最大が最大
			op0Max = Mathf.Min(op0Max, ansMax - op1Min); // 答えの最大+op1の最大が最大
			operand0 = UnityEngine.Random.Range(op0Min, op0Max + 1);
			// op1の範囲は、[ansMin - op0, ansMax - op0]
			op1Min = Mathf.Max(op1Min, ansMin - operand0);
			op1Max = Mathf.Min(op1Max, ansMax - operand0);
			operand1 = UnityEngine.Random.Range(op1Min, op1Max + 1);
			answer = operand0 + operand1;
			operatorChar = '＋';
		}
		else if (operation == Operation.Subtraction)
		{
			// op0の範囲をまず削る
			op0Min = Mathf.Max(op0Min, ansMin + op1Min); // 答えの最大+op1の最大が最大
			op0Max = Mathf.Min(op0Max, ansMax + op1Max); // 答えの最大+op1の最大が最大
			operand0 = UnityEngine.Random.Range(op0Min, op0Max + 1);
			// op0 - op1 >= ansMin
			// op1の範囲は、[ansMin + op0, ansMax + op0]
			op1Min = Mathf.Max(op1Min, operand0 - ansMax);
			op1Max = Mathf.Min(op1Max, operand0 - ansMin);
			operand1 = UnityEngine.Random.Range(op1Min, op1Max + 1);
			answer = operand0 - operand1;
			operatorChar = '−';
		}
		else if (operation == Operation.Multiplication)
		{
			// op0 * op1 >= ansMin
			op1Min = Mathf.Max(op1Min, (ansMin + operand0 - 1) / operand0);
			// op0 * op1 <= ansMax
			op1Max = Mathf.Min(op1Max, ansMax / operand0);
			operand1 = UnityEngine.Random.Range(op1Min, op1Max + 1);
			answer = operand0 * operand1;
			operatorChar = '×';
		}
		else
		{
			Debug.Assert(false, "BUG.");
			operatorChar = '?';
			operand0 = operand1 = answer = 0;
		}
		Debug.LogFormat("{0}({1},{2})={3} op0=[{4},{5}] op1=[{6}.{7}]", operatorChar, operand0, operand1, answer, op0Min, op0Max, op1Min, op1Max);
		Debug.Assert(operand0 >= op0Min);
		Debug.Assert(operand0 <= op0Max);
		Debug.Assert(operand1 >= op1Min);
		Debug.Assert(operand1 <= op1Max);
		Debug.Assert(answer >= ansMin);
		Debug.Assert(answer <= ansMax);

		questionText.text = string.Format("{0} {1} {2} =", operand0, operatorChar, operand1);

		// 加算に限ってキューブ置く
		if ((operation == Operation.Addition) && (settings.operand0Digits == 1) && (settings.operand1Digits == 1))
		{
			var center = new Vector3(-0.7f, 1f, 0.3f);
			for (var i = 0; i < operand0; i++)
			{
				var obj = Instantiate(redCubePrefab, countObjectRoot, false);
				var p = center + new Vector3(0.15f * i, 0.5f * i, 0f);
				obj.ManualStart(this, p);
				countingObjects.Add(obj);
			}

			center = new Vector3(-0.6f, 1f, -0.3f);
			for (var i = 0; i < operand1; i++)
			{
				var obj = Instantiate(blueCubePrefab, countObjectRoot, false);
				var p = center + new Vector3(0.15f * i, 0.5f * i, 0f);
				obj.ManualStart(this, p);
				countingObjects.Add(obj);
			}
		}
		problemStartTime = System.DateTime.Now;
		switch (operation)
		{
			case Operation.Addition: operatorChar = '+'; break;
			case Operation.Subtraction: operatorChar = '-'; break;
			case Operation.Multiplication: operatorChar = '*'; break;
			default: operatorChar = '\0'; break;
		}
		problemText = string.Format("{0}{1}{2}", operand0, operatorChar, operand1);
		strokeCount = eraseCount = 0;
	}

	static int Pow10(int e)
	{
		var ret = 1;
		for (var i = 0; i < e; i++)
		{
			ret *= 10;
		}
		return ret;
	}
}
