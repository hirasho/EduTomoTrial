using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainUi : MonoBehaviour
{
	[SerializeField] Button abortButton;
	[SerializeField] Text questionIndexText;
	[SerializeField] Text debugInfoText;
	[SerializeField] Text debugMessageText;
	[SerializeField] Image loadingIcon;
	[SerializeField] Image hanamaru;
	[SerializeField] Image gaugeImage;
	[SerializeField] Image timeGauge;
	[SerializeField] Image minBar;
	[SerializeField] Color gaugeColor0;
	[SerializeField] Color gaugeColor1;

	public bool AbortButtonClicked { get; private set; }

	public void ManualStart()
	{
		loadingIcon.enabled = false;
		hanamaru.enabled = false;
		abortButton.onClick.AddListener(OnClickAbort);
	}

	public void SetQuestionIndex(int current, int min, int max)
	{
		var ratio = (float)(current - 1) / (float)max;
		gaugeImage.transform.localScale = new Vector3(ratio, 1f, 1f);
		questionIndexText.text = string.Format("{0} / {1}", current, max);

		var minRatio = (float)min / (float)max;
		minBar.rectTransform.anchoredPosition = new Vector2(minRatio * 300f, 0f);

		var color = (ratio < minRatio) ? gaugeColor0 : gaugeColor1;
		gaugeImage.color = color;
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

	public void ManualUpdate(float deltaTime, float currentTime, float duration)
	{
		AbortButtonClicked = false;

		UpdateDebugInfo();

		if (loadingIcon.enabled)
		{
			var iconTransform = loadingIcon.transform;
			var dq = Quaternion.AngleAxis(360f * deltaTime, new Vector3(0f, 0f, 1f));
			iconTransform.localRotation = dq * iconTransform.localRotation;
		}

		timeGauge.fillAmount = Mathf.Clamp01((duration - currentTime) / duration);

//		eraserButtonImage.color = EraserEnabled ? new Color(0.75f, 0.75f, 0.75f, 1f) : new Color(1f, 1f, 1f, 1f);
	}

	public void ShowHanamaru()
	{
		hanamaru.enabled = true;
	}

	public void HideHanamaru()
	{
		hanamaru.enabled = false;
	}

	// non public ------
	bool eraserDown;
	bool eraserOn;

	void OnClickAbort()
	{
		AbortButtonClicked = true;
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
