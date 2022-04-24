using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainUi : MonoBehaviour
{
	[SerializeField] Canvas canvas;
	[SerializeField] Button clearButton;
	[SerializeField] Image eraserButtonImage;
	[SerializeField] ButtonEventsHandler eraserButton;
	[SerializeField] Text questionIndexText;
	[SerializeField] Text debugInfoText;
	[SerializeField] Text debugMessageText;
	[SerializeField] Image loadingIcon;

	public bool ClearButtonClicked { get; private set; }
	public bool EraserEnabled { get => (!eraserDown && eraserOn); } // down中は何であれ有効,それ以外はonなら有効

	public void ManualStart()
	{
		loadingIcon.enabled = false;
		clearButton.onClick.AddListener(OnClickClear);

		eraserButton.OnDown = OnEraserDown;
		eraserButton.OnUp = OnEraserUp;
	}

	public void SetQuestionIndex(int current, int total)
	{
		questionIndexText.text = string.Format("{0} / {1}", current, total);
	}

	public void SetDebugMessage(string message)
	{
		debugMessageText.text = message;
	}

	public void BeginLoading()
	{
		loadingIcon.enabled = true;
	}

	public void EndLoading()
	{
		loadingIcon.enabled = false;
	}

	public void ManualUpdate(float deltaTime)
	{
		ClearButtonClicked = false;

		UpdateDebugInfo();
#if UNITY_EDITOR
		if (Input.GetKeyDown(KeyCode.E))
		{
			eraserOn = !eraserOn;
		}
#endif
		if (loadingIcon.enabled)
		{
			var iconTransform = loadingIcon.transform;
			var dq = Quaternion.AngleAxis(360f * deltaTime, new Vector3(0f, 0f, 1f));
			iconTransform.localRotation = dq * iconTransform.localRotation;
		}

		eraserButtonImage.color = EraserEnabled ? new Color(0.75f, 0.75f, 0.75f, 1f) : new Color(1f, 1f, 1f, 1f);
	}

	public void SetEraserOff()
	{
		if (eraserOn)
		{
			OnEraserDown();
		}
	}

	// non public ------
	bool eraserDown;
	bool eraserOn;

	void OnClickClear()
	{
		ClearButtonClicked = true;
	}

	void OnEraserDown()
	{
		eraserOn = !eraserOn;
		eraserDown = true;
	}

	void OnEraserUp()
	{
		eraserDown = false;
	}

	void UpdateDebugInfo()
	{
		var str = string.Format("{0}x{1}\n{2:F2}\n{3}", 
			Screen.width,
			Screen.height,
			Time.unscaledDeltaTime * 1000f,
			SystemInfo.graphicsDeviceVersion);

		debugInfoText.text = str;
	}
}
