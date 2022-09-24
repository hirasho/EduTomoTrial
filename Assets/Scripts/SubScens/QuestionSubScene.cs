using System.Collections;
using System.Collections.Generic;
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
			bool invertOperation,
			bool useSpecialFormula)
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
			this.useSpecialFormula = useSpecialFormula;
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
		public bool useSpecialFormula;
	}
	[SerializeField] float rotationMax = 30f;
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

		if (settings.useSpecialFormula)
		{
			if (settings.operation == Operation.Addition)
			{
				activeFormula = formulae[2];
			}
		}
		else
		{
			activeFormula = settings.invertOperation ? formulae[1] : formulae[0];
		}

		foreach (var formula in formulae)
		{
			formula.gameObject.SetActive(formula == activeFormula);
		}

		recognitionParam = new Dictionary<int, RecognitionParams>();

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

		// MLKitなら認識しっぱなし
		if (!main.TextRecognizer.UsingVisionApi && drawingManager.Drawn())
		{
			if (!main.TextRecognizer.IsBusy())
			{
				if (evaluationCoroutine == null)
				{
					evaluationCoroutine = StartCoroutine(CoRequestEvaluation());
				}
			}
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
			evaluationCoroutine = StartCoroutine(CoRequestEvaluation());
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

	public override void OnTextRecognitionComplete(TextRecognizer.Text text)
	{
		RecognitionParams rp;
		Debug.LogWarning("Result : " + text.requestId);
		if (recognitionParam.TryGetValue(text.requestId, out rp))
		{
			recognitionParam.Remove(text.requestId);
			main.TransformToRtScreen(text, rp.scale, rp.rotation);
		}
		else
		{
			Debug.Assert(false, "BAKANA");
		}
		ui.EndLoading();
		ClearAnnotations();
		if (evaluationRequestQuestionIndex == questionIndex) // もう次の問題行ってるので評価しない
		{
			var zones = activeFormula.AnswerZones;
//Debug.Log("Evaluation: words=" + words.Count + " zones=" + zones.Length + " " + evaluationRequestQuestionIndex + " " + questionIndex);
			var zoneMins = new Vector2[zones.Length];
			var zoneMaxs = new Vector2[zones.Length];
			for (var i = 0; i < zones.Length; i++)
			{

				Vector2 zoneMin, zoneMax;
				zones[i].GetScreenBounds(out zoneMin, out zoneMax, main.RenderTextureCamera);
				zoneMins[i] = zoneMin;
				zoneMaxs[i] = zoneMax;
			}

			var correctValues = new int[zones.Length];
			if ((settings.operation == Operation.Addition) && settings.useSpecialFormula && (zones.Length == 3))
			{
				correctValues[0] = subAnswer0;
				correctValues[1] = subAnswer1;
				correctValues[2] = answer;
			}
			else
			{
				correctValues[0] = settings.invertOperation ? operand1 : answer;
			}

			var correctCount = 0;
			foreach (var word in text.words)
			{
//Debug.Log("Word: " + word.text + " -> " + word.boundsMin + " " + word.boundsMax);
				var minD = float.MaxValue;
				var minI = -1;
				for (var zoneIndex = 0; zoneIndex < zones.Length; zoneIndex++)
				{
					var zoneMin = zoneMins[zoneIndex];
					var zoneMax = zoneMaxs[zoneIndex];
//Debug.Log("\tZone: " + zoneIndex + " " + zoneMin + " " + zoneMax + " collide:" + Main.BoundsIntersect(word.boundsMin, word.boundsMax, zoneMin, zoneMax));
					if (Main.BoundsIntersect(word.boundsMin, word.boundsMax, zoneMin, zoneMax))
					{
						var d = (((word.boundsMin + word.boundsMax) - (zoneMin + zoneMax)) * 0.5f).magnitude;
						if (d < minD)
						{
							minD = d;
							minI = zoneIndex;
						}
					}
				}

				if (minI >= 0)
				{
//Debug.Log(word.text + " -> " + minI + " " + minD + " " + correctValues[minI]);
					var letters = new List<Evaluator.EvaluatedLetter>();
					if (Evaluator.EvaluateWord(letters, word, correctValues[minI]))
					{
						correctCount++;
					}

					ShowAnnotations(letters);
				}
			}

			if (correctCount == zones.Length)
			{
				nextRequested = true;
			}
		}
		evaluationCoroutine = null;
	}

	// non public -------
	Main main;
	Settings settings;
	int operand0;
	int operand1;
	int? operand2;
	int subAnswer0;
	int subAnswer1;
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
	Coroutine evaluationCoroutine;
	int evaluationRequestQuestionIndex;
	class RecognitionParams
	{
		public Vector2 scale;
		public float rotation;
	}
	Dictionary<int, RecognitionParams> recognitionParam;

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
					Debug.Log("Break1 " + questionIndex + " " + duration + " " + sd.minProblemCount);
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
		main.TextRecognizer.ClearDiffImage();
		
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
		if (main.TextRecognizer == null)
		{
			yield break;
		}
//Debug.Log(Time.frameCount + " " + ": CoRequestEvaluation");
		evaluationRequestQuestionIndex = questionIndex;

		// 縦横比ランダム変更
		// まず、-1,1を生成して、そこから1-2の比を作る
		var scaleSeed = Random.Range(-1f, 0.5f);
		Vector2 scale;
		if (scaleSeed < 0f) // 縦長
		{
			scale.y = 1f;
			scale.x = 1f / (1f - scaleSeed);
		}
		else
		{
			scale.x = 1f;
			scale.y = 1f / (1f + scaleSeed);
		}
		var rotation = Random.Range(-rotationMax, rotationMax);
		if (main.TextRecognizer.UsingVisionApi)
		{
			rotation = 0f;
		}
		yield return main.CoSaveRenderTexture(scale, rotation);

		var tex = main.SavedTexture;

		var zones = activeFormula.AnswerZones;
		var rects = new List<RectInt>();
		foreach (var zone in zones)
		{
			var rect = main.GetRectInRenderTexture(zone);
			rects.Add(rect);
		}

		var reqId = main.TextRecognizer.Request(tex, rects);
		if (reqId >= 0)
		{
			var recogParams = new RecognitionParams();
			recogParams.scale = scale;
			recogParams.rotation = rotation;
			recognitionParam.Add(reqId, recogParams);
			Debug.LogWarning("RequestRecog: " + reqId + " " + scale + " " + rotation);
			ui.BeginLoading();
		}
	}

	void ShowAnnotations(IReadOnlyList<Evaluator.EvaluatedLetter> letters)
	{
		foreach (var letter in letters)
		{
			var obj = main.ShowAnnotation(letter.srcLetter, letter.numberText, letter.correct, transform);
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
						questionSet.Add(new Question(v0, v1, 0, ans, Operation.Addition));
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
			if (settings.useSpecialFormula)
			{
				subAnswer0 = 10 - operand0;
				subAnswer1 = operand1 - subAnswer0;
			}
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
					string.Format("{0} {1} = {2}", operatorChar1.Value, operand2.Value, answer));
			}
			else
			{
				activeFormula.SetFormulaText(
					string.Format("{0} {1}", operand0, operatorChar),
					string.Format("= {0}", answer));
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
