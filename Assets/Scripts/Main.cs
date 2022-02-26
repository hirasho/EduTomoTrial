using UnityEngine;
using UnityEngine.EventSystems;

public class Main : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
	[SerializeField] string defaultUserName = "平山オトモ";
	[SerializeField] System.DateTime defaultBirthday = new System.DateTime(2015, 12, 20);
	[SerializeField] Transform subSceneRoot;
	[SerializeField] new Camera camera;
	[SerializeField] TouchDetector touchDetector;
	[SerializeField] SoundPlayer soundPlayer;

	public TouchDetector TouchDetector { get => touchDetector; }
	public SoundPlayer SoundPlayer { get => soundPlayer; }
	public VisionApi.Client VisionApi { get => visionApi; }
	public Camera MainCamera { get => camera; }
	public LogData LogData { get; private set; }
	public string UserName { get => defaultUserName; }
	public System.DateTime Birthday { get => defaultBirthday; }

	public void OnPointerDown(PointerEventData eventData)
	{
		subScene.OnPointerDown();
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		subScene.OnPointerUp();
	}

	public void OnSessionEnd(SessionData session)
	{
		LogData.sessions.Add(session);
		TrySaveLog();		
	}

	void Start()
	{
		var jsonAsset = Resources.Load<TextAsset>("keys");
		if (jsonAsset != null)
		{
			try
			{
				var keys = JsonUtility.FromJson<Keys>(jsonAsset.text);
				if (!string.IsNullOrEmpty(keys.VisionApiKey))
				{
					visionApi = new VisionApi.Client(keys.VisionApiKey);
				}
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
			}
		}
		Application.targetFrameRate = 120;

		LogData = new LogData();

		var titleSubScene = SubScene.Instantiate<TitleSubScene>(subSceneRoot);
		titleSubScene.ManualStart(this);
		subScene = titleSubScene;
	}

	void Update()
	{
		var dt = Time.deltaTime;

		var nextSubScene = subScene.ManualUpdate(dt);
		if (nextSubScene != null)
		{
			Destroy(subScene.gameObject);
			subScene = nextSubScene;
		}

		AdjustCameraHeight();

		if ((visionApi != null) && visionApi.Requested && visionApi.IsDone())
		{
			subScene.OnVisionApiDone(visionApi.Response);
			visionApi.Complete();
		}
	}

	// non public -------
	VisionApi.Client visionApi;
	SubScene subScene;

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

	void AdjustCameraHeight()
	{
		// カメラ位置調整
		var y0 = (1f * 9f / 16f) / Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
		var y1 = 1f / Mathf.Tan(camera.fieldOfView * camera.aspect * Mathf.Deg2Rad * 0.5f);
		var y = Mathf.Max(y0, y1);
		camera.transform.localPosition = new Vector3(0f, y, 0f);
	}
}
