using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

public class Main : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
	[SerializeField] Transform subSceneRoot;
	[SerializeField] new Camera camera;
	[SerializeField] TouchDetector touchDetector;
	[SerializeField] SoundPlayer soundPlayer;

	public TouchDetector TouchDetector { get => touchDetector; }
	public SoundPlayer SoundPlayer { get => soundPlayer; }
	public VisionApi VisionApi { get => visionApi; }
	public Camera MainCamera { get => camera; }

	public void OnPointerDown(PointerEventData eventData)
	{
		subScene.OnPointerDown();
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		subScene.OnPointerUp();
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
					visionApi = new VisionApi(keys.VisionApiKey);
				}
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
			}
		}
		Application.targetFrameRate = 120;

		var titleSubScene = SubScene.Instantiate<TitleSubScene>(subSceneRoot);
		titleSubScene.ManualStart(main: this);
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
		}
	}

	// non public -------
	VisionApi visionApi;
	SubScene subScene;

	void AdjustCameraHeight()
	{
		// カメラ位置調整
		var y = 0.5f / Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
		camera.transform.localPosition = new Vector3(0f, y, 0f);
	}
}
