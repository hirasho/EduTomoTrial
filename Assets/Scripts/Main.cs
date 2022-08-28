using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using Firebase;
using Firebase.Analytics;
using Firebase.Extensions;

public class Main : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
	[SerializeField] float lineWidthMm = 1f; 
	[SerializeField] float minDpi = 100f; 
	[SerializeField] float maxDpi = 500f;
	[SerializeField] string defaultUserName = "平山オトモ";
	[SerializeField] System.DateTime defaultBirthday = new System.DateTime(2015, 12, 20);
	[SerializeField] Transform subSceneRoot;
	[SerializeField] new Camera camera;
	[SerializeField] TouchDetector touchDetector;
	[SerializeField] SoundPlayer soundPlayer;
	[SerializeField] Annotation annotationPrefab;
	[SerializeField] TextRecognizer textRecognizer;
	[SerializeField] RawImage noiseBackground;
	[SerializeField] Text recognitionHintForVisionApi;
	[SerializeField] TextureRenderer textureRenderer;
	[SerializeField] Canvas debugCanvas;
	[SerializeField] RawImage debugRtView;

	public TouchDetector TouchDetector { get => touchDetector; }
	public SoundPlayer SoundPlayer { get => soundPlayer; }
	public TextRecognizer TextRecognizer { get => textRecognizer; }
	public Camera MainCamera { get => camera; }
	public Camera RenderTextureCamera { get => textureRenderer.Camera; }
	public LogData LogData { get; private set; }
	public string UserName { get => defaultUserName; }
	public System.DateTime Birthday { get => defaultBirthday; }
	public SaveData SaveData { get => saveData; }
	public Texture2D SavedTexture { get => textureRenderer.SavedTexture; }
	public Keys Keys { get => keys; }

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

	public IEnumerator CoSaveRenderTexture(Vector2 scale)
	{
		yield return textureRenderer.CoRender(scale);
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
Debug.Log("Anno: " + letter.text + " " + i + " " + srcV);
			var ray = textureRenderer.Camera.ScreenPointToRay(srcV);
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
		dpi = Mathf.Clamp(Screen.dpi, minDpi, maxDpi);
		Debug.Log("DPI: " + Screen.dpi + " -> " + dpi);

		// 鍵取得
		this.keys = Keys.Instantiate();
		saveData = SaveData.Load(keys.SaveDataEncryptionKey);

		textRecognizer.ManualStart(keys.VisionApiKey, saveData.useVisionApi);
//		recognitionHintForVisionApi.enabled = textRecognizer.UsingVisionApi;
		recognitionHintForVisionApi.enabled = true;

		Application.targetFrameRate = 120;

		LogData = new LogData();

		var titleSubScene = SubScene.Instantiate<TitleSubScene>(subSceneRoot);
		titleSubScene.ManualStart(this);
		subScene = titleSubScene;

		textureRenderer.ManualStart();

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
			StartCoroutine(CoSaveRenderTexture(Vector2.one));
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
			var result = textRecognizer.GetResult();
			textRecognizer.Abort(); // 結果破棄
			debugRtView.texture = textureRenderer.RenderTexture;
			if (result != null)
			{
				textureRenderer.TransformToRtScreen(result);
				subScene.OnTextRecognitionComplete(result);
			}
			else
			{
				Debug.LogError("TextRecognizer.GetResult returns null.");
			}
		}

		// MLKit対策
		noiseBackground.uvRect = new Rect(
			Random.value * 0.5f,
			Random.value * 0.5f,
			0.5f,
			0.5f);
	}

	public void Save()
	{
		saveData.Save(keys.SaveDataEncryptionKey);		
	}

	public void ResetTextRecognizer()
	{
		TextRecognizer.TryReset(keys.VisionApiKey, saveData.useVisionApi);
		recognitionHintForVisionApi.enabled = textRecognizer.UsingVisionApi;
	}

	public RectInt GetRectInRenderTexture(AnswerZone answerZone)
	{
		return textureRenderer.GetRect(answerZone);
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
	Keys keys;

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
}
