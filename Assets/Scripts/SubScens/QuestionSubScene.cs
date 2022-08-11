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
			int operand0min,
			int operand0max,
			int operand1min,
			int operand1max,
			int answerMin,
			int answerMax,
			bool invertOperation)
		{
			this.description = description;
			this.operation = operation;
			this.operand0min = operand0min;
			this.operand0max = operand0max;
			this.operand1min = operand1min;
			this.operand1max = operand1max;
			this.answerMin = answerMin;
			this.answerMax = answerMax;
			this.invertOperation = invertOperation;
		}
			
		public string description;
		public Operation operation;
		public int operand0min;
		public int operand0max;
		public int operand1min;
		public int operand1max;
		public int answerMin;
		public int answerMax;
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
	[SerializeField] Annotation annotationPrefab;
	[SerializeField] Canvas paperCanvas;
	
	public void ManualStart(
		Main main, 
		Settings settings)
	{
		this.main = main;
		this.settings = settings;
		paperCanvas.worldCamera = main.MainCamera;

		eraser.ManualStart(this);

		annotationViews = new List<Annotation>();
		countingObjects = new List<CountingObject>();
		ui.ManualStart();
		sessionData = new SessionData(
			settings.operand0min, 
			settings.operand0max, 
			settings.operand1min, 
			settings.operand1max, 
			settings.answerMin, 
			settings.answerMax,
			settings.description,
			main.UserName,
			main.Birthday);
		sessionStartTime = System.DateTime.Now;
		drawingManager = new DrawingManager();

		timeLimit = main.SaveData.secondsPerProblem * main.SaveData.maxProblemCount;

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
		ui.ManualUpdate(deltaTime, (float)currentTime, timeLimit);
		
		var eraserPosition = eraser.DefaultPosition;

		drawingManager.ManualUpdate(
			ref eraserPosition,
			main.TouchDetector,
			main.MainCamera);
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

	public override void OnPointerDown(int pointerId)
	{
		drawingManager.OnPointerDown(
			ref strokeCount,
			linePrefab,
			lineRoot,
			main.TouchDetector,
			main.MainCamera,
			main.DefaultLineWidth,
			pointerId,
			isEraser: false);
	}

	public override void OnPointerUp(int pointerId)
	{
		bool eval;
		drawingManager.OnPointerUp(
			out eval,
			justErased,
			pointerId);
		if (eval)
		{
			StartCoroutine(CoRequestEvaluation());
		}
		justErased = false;
	}

	public void OnEraserDown(int pointerId)
	{
		drawingManager.OnPointerDown(
			ref strokeCount,
			linePrefab,
			lineRoot,
			main.TouchDetector,
			main.MainCamera,
			main.DefaultLineWidth,
			pointerId,
			true);
	}

	public void OnEraserUp(int pointerId)
	{
		OnPointerUp(pointerId);
	}

	public void OnEraserHitLine(Line line)
	{
		drawingManager.RemoveLine(
			ref eraseCount,
			ref justErased,
			line);
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
	DrawingManager drawingManager;
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
	float timeLimit;

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
	
	IEnumerator CoQuestionLoop()
	{
		while (true)
		{
			var sd = main.SaveData;
			var duration = (System.DateTime.Now - sessionStartTime).TotalSeconds;
			if (questionIndex >= sd.maxProblemCount) // 1. 最大問題数終わってれば終わっていい
			{
				Debug.Log("Break2 " + questionIndex + " " + duration + " " + sd.maxProblemCount);
				break;
			}
			else if (questionIndex >= sd.minProblemCount) // 2. 最小問題数終わって規定時間を過ぎていれば終わっていい。
			{
				if (duration >= timeLimit)
				{
					Debug.Log("Break1 " + questionIndex + " " + duration + " " + sd.minProblemCount + " " + sd.timeMinute);
					break;
				}
			}
			Debug.Log("Cont. " + duration + "/" + timeLimit + "\t" + questionIndex + "/" + + sd.minProblemCount + "-" + sd.maxProblemCount);
			yield return CoQuestion();
		}
		end = true;
	}

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

	IEnumerator CoQuestion()
	{
		ui.HideHanamaru();
		ClearLines();
		ClearAnnotations();
		main.VisionApi.ClearDiffImage(); // キャッシュ消す
		
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
		yield return main.CoSaveRenderTexture();

		var tex = main.SavedTexture;

		var zone = activeFormula.AnswerZone;
		var rect = main.GetRectInRenderTexture(zone);
		var rects = new List<RectInt>();
		rects.Add(rect);

		if (main.VisionApi.Request(tex, rects))
		{
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
			for (var i = 0; i < srcVertices.Count; i++)
			{
				var srcV = srcVertices[i];
				var sp = new Vector3(srcV.x, main.RenderTextureCamera.targetTexture.height - srcV.y);
				var ray = main.RenderTextureCamera.ScreenPointToRay(sp);
				var t = (0f - ray.origin.y) / ray.direction.y;
				var wp = ray.origin + (ray.direction * t);
				center += wp;
				min = Vector3.Min(min, wp);
				max = Vector3.Max(max, wp);
			}

			var obj = Instantiate(annotationPrefab, transform, false);
			center /= srcVertices.Count;
			obj.Show(center, max - min, letter.text, letter.correct);
//Debug.Log("\t " + letter.text + " " + letter.correct);
			annotationViews.Add(obj);
		}
	}

	void MakeQuestions()
	{
		var questionSet = new HashSet<Question>();
		var op0min = settings.operand0min;
		var op0max = settings.operand0max;

		var op1min = settings.operand1min;
		var op1max = settings.operand1max;
		
		var ansMin = settings.answerMin;
		var ansMax = settings.answerMax;
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
		else if (main.SaveData.showCubes && 
			((q.op == Operation.Addition) || (q.op == Operation.Subtraction)))
		{
			if (settings.operand0max <= 10)
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

			if ((settings.operand1max <= 10) && !settings.invertOperation)
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
}
