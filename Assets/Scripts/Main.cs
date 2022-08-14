using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;

public class Main : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
	[SerializeField] float lineWidthMm = 1f; 
	[SerializeField] float minDpi = 100f; 
	[SerializeField] float maxDpi = 500f;
	[SerializeField] int renderTextureHeight = 432;
	[SerializeField] string defaultUserName = "平山オトモ";
	[SerializeField] System.DateTime defaultBirthday = new System.DateTime(2015, 12, 20);
	[SerializeField] Transform subSceneRoot;
	[SerializeField] new Camera camera;
	[SerializeField] Camera renderTextureCamera;
	[SerializeField] TouchDetector touchDetector;
	[SerializeField] SoundPlayer soundPlayer;
	[SerializeField] Annotation annotationPrefab;
	[SerializeField] TextRecognizer textRecognizer;

	public TouchDetector TouchDetector { get => touchDetector; }
	public SoundPlayer SoundPlayer { get => soundPlayer; }
	public TextRecognizer TextRecognizer { get => textRecognizer; }
	public Camera MainCamera { get => camera; }
	public Camera RenderTextureCamera { get => renderTextureCamera; }
	public LogData LogData { get; private set; }
	public string UserName { get => defaultUserName; }
	public System.DateTime Birthday { get => defaultBirthday; }
	public SaveData SaveData { get => saveData; }
	public Texture2D SavedTexture { get => savedTexture; }

	public float DefaultLineWidth { get => ConvertMilliMeterToWorldUnit(lineWidthMm); }

	public void OnPointerDown(PointerEventData eventData)
	{
		var id = (eventData != null) ? eventData.pointerId : ushort.MaxValue;
		subScene.OnPointerDown(id);
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		var id = (eventData != null) ? eventData.pointerId : ushort.MaxValue;
		subScene.OnPointerUp(id);
	}

	public void OnSessionEnd(SessionData session)
	{
		LogData.sessions.Add(session);
		TrySaveLog();
	}

	public IEnumerator CoSaveRenderTexture()
	{
		renderTextureCamera.enabled = true;
		yield return new WaitForEndOfFrame();

		Graphics.SetRenderTarget(renderTexture, 0);
		savedTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), destX: 0, destY: 0);
		renderTextureCamera.enabled = false;

		var jpg = savedTexture.EncodeToJPG();
#if UNITY_EDITOR
System.IO.File.WriteAllBytes("rtTest.jpg", jpg);
#endif
	}

	public RectInt GetRectInRenderTexture(AnswerZone answerZone)
	{
		var min = Vector2.one * float.MaxValue;
		var max = -min;
		foreach (var rectTransform in answerZone.RectTransforms)
		{
			var wp = rectTransform.position;
			var sp = renderTextureCamera.WorldToScreenPoint(wp);
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

	public Annotation ShowAnnotation(TextRecognizer.Letter letter, string textOverride, bool correctColor, Transform parent)
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
			var sp = new Vector3(srcV.x, RenderTextureCamera.targetTexture.height - srcV.y);
			var ray = RenderTextureCamera.ScreenPointToRay(sp);
			var t = (0f - ray.origin.y) / ray.direction.y;
			var wp = ray.origin + (ray.direction * t);
			center += wp;
			min = Vector3.Min(min, wp);
			max = Vector3.Max(max, wp);
		}

		var obj = Instantiate(annotationPrefab, parent, false);
		center /= srcVertices.Count;
		var text = string.IsNullOrEmpty(textOverride) ? letter.text : textOverride;
		obj.Show(center, max - min, text, correctColor);
		return obj;
	}

	void Start()
	{
		saveData = SaveData.Load();
		dpi = Mathf.Clamp(Screen.dpi, minDpi, maxDpi);
		Debug.Log("DPI: " + Screen.dpi + " -> " + dpi);

		string visionApiKey = null;
		var jsonAsset = Resources.Load<TextAsset>("keys");
		if (jsonAsset != null)
		{
			try
			{
				var keys = JsonUtility.FromJson<Keys>(jsonAsset.text);
				visionApiKey = keys.VisionApiKey;
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
			}
		}

		textRecognizer.ManualStart(visionApiKey);

		Application.targetFrameRate = 120;

		LogData = new LogData();

		var titleSubScene = SubScene.Instantiate<TitleSubScene>(subSceneRoot);
		titleSubScene.ManualStart(this);
		subScene = titleSubScene;

		var w = (renderTextureHeight * 16) / 9;
		renderTexture = new RenderTexture(w, renderTextureHeight, 0);
		renderTextureCamera.targetTexture = renderTexture;
		renderTextureCamera.enabled = false;
		savedTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);

		// Firebase
		FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => 
		{
			if (task.Result == DependencyStatus.Available) 
			{
				FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
			} 
			else 
			{
				Debug.LogError("Could not resolve all Firebase dependencies: " + task.Result);
			}
		});
	}

	void Update()
	{
#if UNITY_EDITOR
		if (Input.GetKeyDown(KeyCode.Space))
		{
			OnPointerDown(null);
		}
		if (Input.GetKeyUp(KeyCode.Space))
		{
			OnPointerUp(null);
		}
		if (Input.GetKeyDown(KeyCode.Return))
		{
			StartCoroutine(CoSaveRenderTexture());
		}
#endif
		var dt = Time.deltaTime;

		var nextSubScene = subScene.ManualUpdate(dt);
		if (nextSubScene != null)
		{
			Destroy(subScene.gameObject);
			subScene = nextSubScene;
		}

		AdjustCameraHeight();

		if ((textRecognizer != null) && textRecognizer.Requested && textRecognizer.IsDone())
		{
Debug.Log("Main.textRecognizer.GetResult");
			var result = textRecognizer.GetResult();
			if (result != null)
			{
				subScene.OnTextRecognitionComplete(result);
			}
			else
			{
				Debug.LogError("TextRecognizer.GetResult returns null.");
			}
		}
	}

	public static bool BoundsIntersect(Vector2 min0, Vector2 max0, Vector2 min1, Vector2 max1)
	{
		if (max0.x < min1.x)
		{
			return false;
		}

		if (min0.x > max1.x)
		{
			return false;
		}

		if (max0.y < min1.y)
		{
			return false;
		}

		if (min0.y > max1.y)
		{
			return false;
		}

		return true;
	}


	// non public -------
	SubScene subScene;
	float dpi;
	SaveData saveData;
	RenderTexture renderTexture;
	Texture2D savedTexture;

	float ConvertMilliMeterToWorldUnit(float milliMeter)
	{
		// 画面は幅2000mmでこれがScreen.widthに当たる。
		// ピクセルの大きさは2000/Screen.width
		// 現実世界のピクセルの大きさはdpiの逆数で、1/dpiインチ。ミリに直すと25.4/dpi
		// これが何倍かを見れば何分の1すればいいかわかるので、
		// 2000 / Screen.width * dpi / 25.4
		var scale = (2000f / Screen.width) / (25.4f / dpi);
		// 例えばこれが10なら、リアルで0.5mm幅にしたければ5mmにすれば良いことになる。5mm=0.005。
//Debug.Log("Scale: " + scale + " DPI=" + dpi);
		return milliMeter * scale * 0.001f;
	}

	void TrySaveLog()
	{
		// まず最新ログをロード
		var path = SaveDataPathUtil.MakeFullPath("log_latest.json");
		var saved = false;
		if (System.IO.File.Exists(path))
		{
			try
			{
				var json = System.IO.File.ReadAllText(path);
				var latest = JsonUtility.FromJson<LogData>(json);
				var currentYear = LogData.createTime.Year;
				var currentMonth = LogData.createTime.Month;
				var fileYear = latest.createTime.Year;
				var fileMonth = latest.createTime.Month;
				if ((fileYear == currentYear) && (fileMonth == currentMonth)) // 追記
				{
					latest.sessions.AddRange(LogData.sessions);

					json = JsonUtility.ToJson(latest, prettyPrint: true);
					System.IO.File.WriteAllText(path, json);
					saved = true;
				}
				else // 追記せず別のものをセーブ
				{
					// リネームしてセーブし直し
					var filename = string.Format("log_{0}_{1}.json", fileYear, fileMonth);
					var oldPath = SaveDataPathUtil.MakeFullPath(filename);
					System.IO.File.WriteAllText(oldPath, json);
				}
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
			}
		}

		if (!saved)
		{
			try
			{			
				var json = JsonUtility.ToJson(LogData, prettyPrint: true);
				System.IO.File.WriteAllText(path, json);
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
			}
		}
	}

	// カメラ位置調整
	void AdjustCameraHeight()
	{
		// 16/9幅の机が入るように調整する
		var w = (float)Screen.width;
		var h = (float)Screen.height;
		var y = 0f;
		if ((w * 9f) > (h * 16f)) // 横長 w/h > 16/9
		{
			// 0.5/aspect/y = tan(θ/2)
			y = 0.5f;
		}
		else
		{
			// 0.5/y = tan(θ/2)
			y = 0.5f * (16f / 9f) * (h / w);
		}
		y /= Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
		camera.transform.localPosition = new Vector3(0f, y, 0f);
	}

#if UNITY_EDITOR
	[UnityEditor.CustomEditor(typeof(Main))]
	class CustomEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			var self = target as Main;
			base.OnInspectorGUI();
			EditorGUILayout.ObjectField("RT", self.renderTexture, typeof(RenderTexture), allowSceneObjects: false);
			EditorGUILayout.ObjectField("TEX", self.savedTexture, typeof(Texture2D), allowSceneObjects: false);
		}
	}
#endif

}
