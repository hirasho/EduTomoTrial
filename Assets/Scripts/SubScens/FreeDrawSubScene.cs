using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class FreeDrawSubScene : SubScene, IEraserEventReceiver
{
	[SerializeField] Transform lineRoot;
	[SerializeField] Line linePrefab;
	[SerializeField] Button abortButton;
	[SerializeField] AnswerZone answerZone;
	[SerializeField] Annotation annotationPrefab;
	[SerializeField] Eraser eraser;
	[SerializeField] Canvas paperCanvas;
	
	public void ManualStart(Main main)
	{
		this.main = main;
		eraser.ManualStart(this);
		paperCanvas.worldCamera = main.MainCamera;

		annotationViews = new List<Annotation>();

		abortButton.onClick.AddListener(() =>
		{
			abortButtonClicked = true;
		});
		drawingManager = new DrawingManager();
	}

	public override SubScene ManualUpdate(float deltaTime)
	{
		SubScene nextScene = null;

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

	public override void OnTextRecognitionComplete(IReadOnlyList<TextRecognizer.Word> words)
	{
		ClearAnnotations();

		Vector2 zoneMin, zoneMax;
		answerZone.GetScreenBounds(out zoneMin, out zoneMax, main.RenderTextureCamera);

		foreach (var word in words)
		{
			if (Main.BoundsIntersect(word.boundsMin, word.boundsMax, zoneMin, zoneMax))
			{
				ShowAnnotations(word);
			}
		}
	}

	// non public -------
	Main main;
	List<Annotation> annotationViews;
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
		if (main.TextRecognizer == null)
		{
			yield break;
		}

		yield return main.CoSaveRenderTexture(Vector2.one);

		var tex = main.SavedTexture;

		main.TextRecognizer.Request(tex, rects: null);
	}

	void ShowAnnotations(TextRecognizer.Word word)
	{
		foreach (var letter in word.letters)
		{
			ShowAnnotation(letter);
		}
	}

	void ShowAnnotation(TextRecognizer.Letter letter)
	{
		var obj = main.ShowAnnotation(letter, null, true, transform);
		annotationViews.Add(obj);
	}
}
