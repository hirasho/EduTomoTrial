using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainUi : MonoBehaviour
{
	[SerializeField] Canvas canvas;
	[SerializeField] Button clearButton;
	[SerializeField] Button nextButton;
	[SerializeField] Text debugInfoText;
	[SerializeField] Text debugMessageText;

	public bool ClearButtonClicked { get; private set; }
	public bool NextButtonClicked { get; private set; }

	public void ManualStart()
	{
		clearButton.onClick.AddListener(OnClickClear);
		nextButton.onClick.AddListener(OnClickNext);
	}

	public void SetDebugMessage(string message)
	{
		debugMessageText.text = message;
	}

	public void ManualUpdate(float deltaTime)
	{
		ClearButtonClicked = false;
		NextButtonClicked = false;

		UpdateDebugInfo();
	}

	// non public ------
	void OnClickClear()
	{
		ClearButtonClicked = true;
	}

	void OnClickNext()
	{
		NextButtonClicked = true;
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
