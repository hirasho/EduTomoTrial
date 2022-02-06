﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

public class Main : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
	[SerializeField] float countingObjectGrabY = 0.1f;
	[SerializeField] MainUi ui;
	[SerializeField] new Camera camera;
	[SerializeField] TouchDetector touchDetector;
	[SerializeField] Transform countObjectRoot;
	[SerializeField] Transform lineRoot;
	[SerializeField] Line linePrefab;
	[SerializeField] Transform rtLineRoot;
	[SerializeField] Material rtLineMaterial;
	[SerializeField] UiNumber operand0;
	[SerializeField] UiNumber operand1;
	[SerializeField] UiNumber answer;
	[SerializeField] CountingObject redCubePrefab;
	[SerializeField] CountingObject blueCubePrefab;
	[SerializeField] Crane crane;
	[SerializeField] Transform[] answerZoneTransforms;
	[SerializeField] Camera rtCamera;
	[SerializeField] SoundPlayer soundPlayer;

	public void OnPointerDown(PointerEventData eventData)
	{
		var line = Instantiate(linePrefab, lineRoot, false);
		line.ManualStart();
		lines.Add(line);
		pointerDown = true;
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		pointerDown = false;
		StartCoroutine(CoRequestEvaluation());
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
		lines = new List<Line>();
		countingObjects = new List<CountingObject>();
		Application.targetFrameRate = 120;
		ui.ManualStart();
		StartCoroutine(CoMain());
		rtCamera.enabled = false;
	}

	void Update()
	{
		AdjustCameraHeight();

		var dt = Time.deltaTime;
		if (ui.ClearButtonClicked)
		{
			ClearLines();	
		}

		if (ui.NextButtonClicked)
		{
			nextRequested = true;
		}
		ui.ManualUpdate(dt);

		if (pointerDown)
		{
			var pointer = touchDetector.ScreenPosition;
			var ray = camera.ScreenPointToRay(pointer);
			// y=0点を取得
			var t = -ray.origin.y / ray.direction.y;
			var p = ray.origin + (ray.direction * t);
			lines[lines.Count - 1].AddPoint(p);
		}

		if ((visionApi != null) && visionApi.Requested && visionApi.IsDone())
		{
			CompleteEvaluation(visionApi.Response);
		}
	}

	public void OnBeginDragCountingObject(Rigidbody rigidbody, Vector2 screenPosition)
	{
		var ray = camera.ScreenPointToRay(screenPosition);
		var t = (countingObjectGrabY - ray.origin.y) / ray.direction.y;
		var p = ray.origin + (ray.direction * t);
		crane.Grab(rigidbody, p);
	}

	public void OnDragCountingObject(Vector2 screenPosition)
	{
		var ray = camera.ScreenPointToRay(screenPosition);
		var t = (countingObjectGrabY - ray.origin.y) / ray.direction.y;
		var p = ray.origin + (ray.direction * t);
		crane.SetPosition(p);		
	}

	public void OnEndDragCountingObject()
	{
		crane.Release();		
	}

	// non public -------
	bool pointerDown;
	List<Line> lines;
	int operand0Value;
	int operand1Value;
	List<CountingObject> countingObjects;
	bool nextRequested;
	Color32[] prevRtTexels;
	VisionApi visionApi;

	IEnumerator CoMain()
	{
		while (true)
		{
			yield return CoQuestion();
		}
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
		if (visionApi == null)
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
			if (!visionApi.IsDone()) // 前のが終わってないので止める
 			{
				visionApi.Abort();
			}
			visionApi.Request(texture2d);
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
					soundPlayer.Play("クイズ正解2");
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

	void AdjustCameraHeight()
	{
		// カメラ位置調整
		var y = 0.5f / Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f);
		camera.transform.localPosition = new Vector3(0f, y, 0f);
	}
	
	void ClearLines()
	{
		foreach (var line in lines)
		{
			Destroy(line.gameObject);
		}
		lines.Clear();
	}

	void UpdateQuestion()
	{
		for (var i = 0; i < countingObjects.Count; i++)
		{
			Destroy(countingObjects[i].gameObject);
		}
		countingObjects.Clear();

		operand0Value = UnityEngine.Random.Range(1, 9);
		operand1Value = UnityEngine.Random.Range(1, 9);
		var ans = operand0Value + operand1Value;
		operand0.SetValue(operand0Value);
		operand1.SetValue(operand1Value);
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