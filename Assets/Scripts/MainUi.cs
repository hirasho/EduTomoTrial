using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainUi : MonoBehaviour
{
	[SerializeField] Canvas canvas;
	[SerializeField] Button clearButton;
	[SerializeField] ButtonEventsHandler eraserButton;
	[SerializeField] Text questionIndexText;
	[SerializeField] Text debugInfoText;
	[SerializeField] Text debugMessageText;

	public bool ClearButtonClicked { get; private set; }
	public bool EraserDown { get; private set; }

	public void ManualStart()
	{
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

	public void ManualUpdate(float deltaTime)
	{
		ClearButtonClicked = false;

		UpdateDebugInfo();
#if UNITY_EDITOR
		EraserDown = Input.GetKey(KeyCode.E);
#endif
	}

	// non public ------
	void OnClickClear()
	{
		ClearButtonClicked = true;
	}

	void OnEraserDown()
	{
		EraserDown = true;
	}

	void OnEraserUp()
	{
		EraserDown = false;
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
