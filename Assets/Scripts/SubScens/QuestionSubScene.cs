using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using System.Linq;

public class QuestionSubScene : SubScene, IEraserEventReceiver
{
	public enum Operation
	{
		Addition,
		Subtraction,
		Multiplication,
		Madd,
		AddAndSub,
		Count,
	}

	public class Settings
	{
		public Settings(
			string description,
			Operation operation,
			int operand0Digits,
			int operand1Digits,
			int answerMinDigits,
			int answerMaxDigits,
			bool allowZero,
			bool invertOperation)
		{
			this.description = description;
			this.operation = operation;
			this.operand0Digits = operand0Digits;
			this.operand1Digits = operand1Digits;
			this.answerMinDigits = answerMinDigits;
			this.answerMaxDigits = answerMaxDigits;
			this.allowZero = allowZero;
			this.invertOperation = invertOperation;
		}
			
		public string description;
		public Operation operation;
		public int operand0Digits;
		public int operand1Digits;
		public int answerMinDigits;
		public int answerMaxDigits;
		public bool allowZero;
		public bool invertOperation;
	}
	[SerializeField] Eraser eraser;
	[SerializeField] float countingObjectGrabY = 0.1f;
	[SerializeField] MainUi ui;
	[SerializeField] Transform countObjectRoot;
	[SerializeField] CountingObject redCubePrefab;
	[SerializeField] CountingObject blueCubePrefab;
	[SerializeField] Crane crane;
	[SerializeField] Formula[] formulae;
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
		eraser.ManualStart(this);

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

		activeFormula = settings.invertOperation ? formulae[1] : formulae[0];
		foreach (var formula in formulae)
		{
			formula.gameObject.SetActive(formula == activeFormula);
		}

		MakeQuestions();

		StartCoroutine(CoQuestionLoop());
	}

	public override SubScene ManualUpdate(float deltaTime)
	{
		var currentTime = (System.DateTime.Now - sessionStartTime).TotalSeconds;
		var aborted = ui.AbortButtonClicked;
		ui.ManualUpdate(deltaTime, (float)currentTime, (float)(main.SaveData.timeMinute * 60));

		var eraserPosition = eraser.DefaultPosition;
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
			else
			{
				if (eraser.PointerDown)
				{
					var ray = main.MainCamera.ScreenPointToRay(pointer);
					var t = (0f - ray.origin.y) / ray.direction.y;
					eraserPosition = ray.origin + (ray.direction * t);
				}
			}
			prevPointer = pointer;
		}
		eraser.transform.position = eraserPosition;

		SubScene nextScene = null;
		if (end)
		{
			var result = SubScene.Instantiate<ResultSubScene>(transform.parent);
			main.OnSessionEnd(sessionData);

			result.ManualStart(main, (float)currentTime, questionIndex);
			nextScene = result;
		}
		else if (aborted)
		{
			var title = SubScene.Instantiate<TitleSubScene>(transform.parent);
			title.ManualStart(main);
			nextScene = title;
		}
		return nextScene;
	}

	public override void OnPointerDown()
	{
		if (!eraser.PointerDown)
		{
			var line = Instantiate(linePrefab, lineRoot, false);
			line.ManualStart(main.MainCamera, main.DefaultLineWidth);
			if (lines.Count > 0)
			{
				lines[lines.Count - 1].GenerateCollider();
			}
			lines.Add(line);
			drawing = true;
			strokeCount++;
		}
		pointerDown = true;
		prevPointer = main.TouchDetector.ScreenPosition;
	}

	public override void OnPointerUp()
	{
		var eval = justErased;
		if (drawing)
		{
			if (lines.Count > 0)
			{
				lines[lines.Count - 1].GenerateCollider();
			}
			eval = true;
		}
		drawing = false;
		pointerDown = false;
		ui.SetEraserOff();
		if (eval)
		{
			StartCoroutine(CoRequestEvaluation());
		}
		justErased = false;
	}

	public void OnEraserDown()
	{
		OnPointerDown();
	}

	public void OnEraserUp()
	{
		OnPointerUp();
	}

	public void OnEraserHitLine(Line line)
	{
		RemoveLine(line);
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
		if (response != null)
		{
			ui.EndLoading();
			ClearAnnotations();
			bool correct;
			var correctValue = settings.invertOperation ? operand1 : answer;
			var letters = Evaluator.Evaluate(response, correctValue, out correct);
//Debug.Log("Evaluate: " + letters[0].text + " " + correct + " Answer=" + correctValue);
			if (correct)
			{
				nextRequested = true;
			}
			ShowAnnotations(letters);
		}
	}

	// non public -------
	Main main;
	Settings settings;
	int operand0;
	int operand1;
	int? operand2;
	int answer;
	List<CountingObject> countingObjects;
	bool nextRequested;
	Color32[] prevRtTexels;
	List<Line> lines;
	Vector2 prevPointer;
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
	Formula activeFormula;
	bool justErased;

	struct Question
	{
		public Question(int op0, int op1, int op2, int ans, Operation op)
		{
			this.op0 = op0;
			this.op1 = op1;
			this.op2 = op2;
			this.ans = ans;
			this.op = op;
		}
		public int op0;
		public int op1;
		public int op2;
		public int ans;
		public Operation op;
	}
	List<Question> questions;
	

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
				justErased = true;
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
		while (true)
		{
			var sd = main.SaveData;
			var duration = (System.DateTime.Now - sessionStartTime).TotalSeconds / 60.0;
			if (questionIndex >= sd.maxProblemCount) // 1. 最大問題数終わってれば終わっていい
			{
				Debug.Log("Break2 " + questionIndex + " " + duration + " " + sd.maxProblemCount);
				break;
			} 
			else if (questionIndex >= sd.minProblemCount) // 2. 最小問題数終わって規定時間を過ぎていれば終わっていい。
			{
				if (duration >= sd.timeMinute)
				{
					Debug.Log("Break1 " + questionIndex + " " + duration + " " + sd.minProblemCount + " " + sd.timeMinute);
					break;
				}
			}
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
		ui.HideHanamaru();
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

		main.SoundPlayer.Play("クイズ正解2");
		ui.ShowHanamaru();
		yield return new WaitForSeconds(0.5f);

		var seconds = (System.DateTime.Now - problemStartTime).TotalSeconds;
		var problem = new ProblemData(problemText, (float)seconds, strokeCount, eraseCount);
		sessionData.AddProblemData(problem);
		questionIndex++;

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
		foreach (var t in activeFormula.AnswerZone.RectTransforms)
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
			ui.BeginLoading();
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

	int GetMax(int digitCount)
	{
		var r = 1;
		for (var i = 0; i < digitCount; i++)
		{
			r *= 10;
		}
		return r - 1;
	}

	void MakeQuestions()
	{
		var questionSet = new HashSet<Question>();
		var op0min = (settings.operand0Digits == 1) ? 0 : ((GetMax(settings.operand0Digits) + 1) / 10);
		var op0max = GetMax(settings.operand0Digits);

		var op1min = (settings.operand1Digits == 1) ? 0 : ((GetMax(settings.operand1Digits) + 1) / 10);
		var op1max = GetMax(settings.operand1Digits);

		var ansMin = (settings.answerMinDigits == 1) ? 0 : ((GetMax(settings.answerMinDigits) + 1) / 10);
		var ansMax = GetMax(settings.answerMaxDigits);
		if (settings.operation == Operation.Count)
		{
			for (var v0 = 0; v0 <= 10; v0++)
			{
				questionSet.Add(new Question(v0, 0, 0, v0, Operation.Count));
			}
		}
		else if (settings.operation == Operation.Addition)
		{
			for (var v0 = op0min; v0 <= op0max; v0++)
			{
				for (var v1 = op1min; v1 <= op1max; v1++)
				{
					var ans = v0 + v1;
					if ((ans >= ansMin) && (ans <= ansMax))
					{
						if (questionSet.Add(new Question(v0, v1, 0, ans, Operation.Addition)))
						{
//Debug.LogFormat("{0}\t {1} + {2} = {3}", questionSet.Count, v0, v1, ans);
						}
					}
				}
			}
		}
		else if (settings.operation == Operation.Subtraction)
		{
			for (var v0 = op0min; v0 <= op0max; v0++)
			{
				for (var v1 = op1min; v1 <= op1max; v1++)
				{
					var ans = v0 - v1;
					if ((ans >= ansMin) && (ans <= ansMax))
					{
						if (questionSet.Add(new Question(v0, v1, 0, ans, Operation.Subtraction)))
						{
//Debug.LogFormat("{0}\t {1} - {2} = {3}", questionSet.Count, v0, v1, ans);
						}

					}
				}
			}
		}
		else if (settings.operation == Operation.AddAndSub)
		{
			for (var v0 = op0min; v0 <= op0max; v0++)
			{
				for (var v1 = op1min; v1 <= op1max; v1++)
				{
					var ans = v0 + v1;
					if ((ans >= ansMin) && (ans <= ansMax))
					{
						questions.Add(new Question(v0, v1, 0, ans, Operation.Addition));
					}
					ans = v0 - v1;
					if ((ans >= ansMin) && (ans <= ansMax))
					{
						questionSet.Add(new Question(v0, v1, 0, ans, Operation.Subtraction));
					}
				}
			}
		}
		else if (settings.operation == Operation.Multiplication)
		{
			for (var v0 = op0min; v0 <= op0max; v0++)
			{
				for (var v1 = op1min; v1 <= op1max; v1++)
				{
					var ans = v0 * v1;
					if ((ans >= ansMin) && (ans <= ansMax))
					{
						if (questionSet.Add(new Question(v0, v1, 0, ans, Operation.Multiplication)))
						{
//Debug.LogFormat("{0}\t {1} * {2} = {3}", questionSet.Count, v0, v1, ans);
						}
					}
				}
			}
		}
		else if (settings.operation == Operation.Madd)
		{
			for (var v0 = op0min; v0 <= op0max; v0++)
			{
				for (var v1 = op1min; v1 <= op1max; v1++)
				{
					for (var v2 = 0; v2 < v0; v2++)
					{
						var ans = (v0 * v1) + v2;
						if ((ans >= ansMin) && (ans <= ansMax))
						{
							questionSet.Add(new Question(v0, v1, v2, ans, Operation.Madd));
						}
					}
				}
			}
		}

		this.questions = questionSet.ToList();
		Debug.Log("MakeQuestions: count=" + this.questions.Count);
	}

	void UpdateQuestion()
	{
		for (var i = 0; i < countingObjects.Count; i++)
		{
			Destroy(countingObjects[i].gameObject);
		}
		countingObjects.Clear();

		ui.SetQuestionIndex(
			questionIndex + 1, 
			main.SaveData.minProblemCount,
			main.SaveData.maxProblemCount);

		if ((questionIndex % questions.Count) == 0)
		{
			Utils.Shuffle(questions);
		}
		var q = questions[questionIndex % questions.Count];
		
		operand0 = q.op0;
		operand1 = q.op1;
		answer = q.ans;
		var operatorChar = '\0';
		char? operatorChar1 = null;
		if (q.op == Operation.Addition)
		{
			operatorChar = '＋';
		}
		else if (q.op == Operation.Subtraction)
		{
			operatorChar = '−';
		}
		else if (q.op == Operation.Multiplication)
		{
			operatorChar = '×';
		}
		else if (q.op == Operation.Madd) // まだ一桁しか対応してない
		{
			operatorChar = '×';
			operatorChar1 = '＋';
			operand2 = q.op2;
		}
		else
		{
			Debug.Assert(false, "BUG.");
			operatorChar = '?';
		}

		if (settings.invertOperation)
		{
			if (operatorChar1.HasValue && operand2.HasValue)
			{
				activeFormula.SetFormulaText(
					string.Format("{0} {1}", operand0, operatorChar),
					string.Format(" {0} {1} = {2}", operatorChar1.Value, operand2.Value, answer));
			}
			else
			{
				activeFormula.SetFormulaText(
					string.Format("{0} {1}", operand0, operatorChar),
					string.Format(" = {0}", answer));
			}
		}
		else
		{
			if (operatorChar1.HasValue && operand2.HasValue)
			{
				activeFormula.SetFormulaText(
					string.Format("{0} {1} {2} {3} {4} =", operand0, operatorChar, operand1, operatorChar1.Value, operand2.Value),
					null);
			}
			else
			{
				activeFormula.SetFormulaText(
					string.Format("{0} {1} {2} =", operand0, operatorChar, operand1),
					null);
			}
		}

		// キューブ置く
		if (q.op == Operation.Count)
		{
			activeFormula.SetFormulaText("", "");
			var shapeType = UnityEngine.Random.Range(0, 4);
Debug.Log("ShapeType: " + shapeType + " " + answer);
			if (shapeType == 0) // 直列5分け
			{
				var start = new Vector3(-0.85f, 1f, 0f);
				if (answer > 5)
				{
					start.z += 0.1f;
				}

				for (var i = 0; i < Mathf.Min(5, answer); i++)
				{
					var obj = Instantiate(redCubePrefab, countObjectRoot, false);
					var p = start + new Vector3(0.15f * i, 0f, 0f);
					obj.ManualStart(this, p);
					countingObjects.Add(obj);
				}

				start.z -= 0.2f;
				for (var i = 5; i < answer; i++)
				{
					var obj = Instantiate(redCubePrefab, countObjectRoot, false);
					var p = start + new Vector3(0.15f * (i - 5), 0f, 0f);
					obj.ManualStart(this, p);
					countingObjects.Add(obj);
				}
			}
			else if (shapeType == 1) // 2列
			{
				var start = new Vector3(-0.85f, 1f, 0f);
				if ((answer % 2) == 1)
				{
					start.x += 0.075f;
				}

				for (var i = 0; i < (answer / 2); i++)
				{
					var obj = Instantiate(redCubePrefab, countObjectRoot, false);
					var p = start + new Vector3(0.15f * i, 0f, 0.1f);
					Debug.Log(i + "\t" + p);
					obj.ManualStart(this, p);
					countingObjects.Add(obj);
				}

				if ((answer % 2) == 1)
				{
					start.x -= 0.075f;
				}
				for (var i = (answer / 2); i < answer; i++)
				{
					var obj = Instantiate(redCubePrefab, countObjectRoot, false);
					var p = start + new Vector3(0.15f * (i - (answer / 2)), 0f, -0.1f);
					Debug.Log(i + "\t" + p);
					obj.ManualStart(this, p);
					countingObjects.Add(obj);
				}
			}
			else if (shapeType == 2) // ランダム
			{
				// 5x5に割ってそのうちのanswer個に配置し、
				// 多少散らす
				var indices = new int[25];
				for (var i = 0; i < 25; i++)
				{
					indices[i] = i;
				}
				for (var i = 0; i < 100; i++) // 雑すぎシャッフル
				{
					var i0 = Random.Range(0, 25);
					var i1 = Random.Range(0, 25);
					var t = indices[i0];
					indices[i0] = indices[i1];
					indices[i1] = t;
				}

				var start = new Vector3(-0.85f, 1f, -0.3f);
				for (var i = 0; i < answer; i++)
				{
					var x = indices[i] / 5;
					var y = indices[i] % 5;
					var obj = Instantiate(redCubePrefab, countObjectRoot, false);
					var p = start + new Vector3(0.15f * x, 0f, 0.15f * y);
					obj.ManualStart(this, p);
					countingObjects.Add(obj);
				}
			}
			else if (shapeType == 3) // 円形か3列
			{
				if (answer > 6)
				{
					var start = new Vector3(-0.8f, 1f, 0.15f);
					var begin = 0;
					var end = (answer / 3);
					var offset = ((end - begin) > (answer / 3)) ? -0.075f : 0f;
					for (var i = begin; i < end; i++)
					{
						var obj = Instantiate(redCubePrefab, countObjectRoot, false);
						var p = start + new Vector3((i - begin) * 0.15f + offset, 0f, 0f);
						obj.ManualStart(this, p);
						countingObjects.Add(obj);
					}

					start.z -= 0.15f;
					begin = end;
					end = (answer - (answer / 3));
					offset = ((end - begin) > (answer / 3)) ? -0.075f : 0f;
					for (var i = begin; i < end; i++)
					{
						var obj = Instantiate(redCubePrefab, countObjectRoot, false);
						var p = start + new Vector3((i - begin) * 0.15f + offset, 0f, 0f);
						obj.ManualStart(this, p);
						countingObjects.Add(obj);
					}

					start.z -= 0.15f;
					begin = end;
					end = answer;
					offset = ((end - begin) > (answer / 3)) ? -0.075f : 0f;
					for (var i = begin; i < end; i++)
					{
						var obj = Instantiate(redCubePrefab, countObjectRoot, false);
						var p = start + new Vector3((i - begin) * 0.15f + offset, 0f, 0f);
						obj.ManualStart(this, p);
						countingObjects.Add(obj);
					}
				}
				else
				{
					var n = answer;
					var center = new Vector3(-0.4f, 1f, 0f);
					var rad = (0.2f * n) / (2f * Mathf.PI); // 距離0.15で×nが円周。これを2piで割ると半径
					var theta = Random.Range(0f, Mathf.PI * 2f);
					for (var i = 0; i < n; i++)
					{
						theta += (Mathf.PI * 2) / n;
						var x = Mathf.Sin(theta) * rad;
						var y = Mathf.Cos(theta) * rad;
						var obj = Instantiate(redCubePrefab, countObjectRoot, false);
						var p = center + new Vector3(x, 0f, y);
						obj.ManualStart(this, p);
						obj.transform.localRotation = Quaternion.Euler(0f, theta, 0f);
						countingObjects.Add(obj);
					}
				}
			}
		}
		else if (
			((q.op == Operation.Addition) || (q.op == Operation.Subtraction)))
		{
			if (settings.operand0Digits == 1)
			{
				var center = new Vector3(-0.85f, 1f, 0.375f);
				for (var i = 0; i < operand0; i++)
				{
					var obj = Instantiate(redCubePrefab, countObjectRoot, false);
					var p = center + new Vector3(0.14f * i, 0.5f * i, 0f);
					obj.ManualStart(this, p);
					countingObjects.Add(obj);
				}
			}

			if ((settings.operand1Digits == 1) && !settings.invertOperation)
			{
				var center = new Vector3(-0.85f, 1f, 0.25f);
				for (var i = 0; i < operand1; i++)
				{
					var obj = Instantiate(blueCubePrefab, countObjectRoot, false);
					var p = center + new Vector3(0.14f * i, 0.5f * i, 0f);
					obj.ManualStart(this, p);
					countingObjects.Add(obj);
				}
			}
		}
		problemStartTime = System.DateTime.Now;
		switch (q.op)
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
