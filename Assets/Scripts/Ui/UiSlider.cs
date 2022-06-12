using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UiSlider : MonoBehaviour
{
	[SerializeField] string labelText;
	[SerializeField] float step;
	[SerializeField] float min;
	[SerializeField] float max;
	[SerializeField] float defaultValue;
	[SerializeField] Text label;
	[SerializeField] Text value;
	[SerializeField] Slider slider;

	public float Value { get; private set; }
	public int IntValue { get => Mathf.RoundToInt(Value); }
	
	public void ManualStart(System.Action onChange, float initialValue)
	{
		slider.minValue = 0f;
		slider.maxValue = 1f;
		slider.wholeNumbers = false;
		slider.onValueChanged.AddListener((unused) => onChange?.Invoke());

		label.text = labelText;

		SetValue(initialValue);

		UpdateValueText();
	}

	public void SetValue(float value)
	{
		var t = (value - min) / (max - min);
		slider.value = t;
	}

	public void ManualUpdate(float deltaTime)
	{
		UpdateValueText();
	}

	// non public ------
	void UpdateValueText()
	{
		var t = slider.value;
		var v = (t * (max - min)) + min;
		
		// 量子化
		var q = Mathf.Round((float)(v - defaultValue) / step);
		Value = defaultValue + (q * step);
//Debug.LogError("\tQ: " + defaultValue + " " + q + " " + Value + " " + step);

		value.text = Value.ToString();
	}

}
