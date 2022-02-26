using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UiNumber : MonoBehaviour
{
	[SerializeField] int value;
	[SerializeField] float height;
	[SerializeField] Color color;
	[SerializeField] UiDigit digitPrefab;
	
	public void SetValue(int value)
	{
		this.value = value;
		dirty = true;
	}

	public void SetHeight(float height)
	{
		this.height = height;
	}

	void Start()
	{
		digits = new List<UiDigit>();
		dirty = true;
	}

	void Update()
	{
		if (dirty)
		{
			UpdateDigits();
		}
	}

	// non public ----
	bool dirty;
	List<UiDigit> digits;

	void UpdateDigits()
	{
		if (value < 0)
		{
			value = -value;
		}
		// 桁数える
		var tmpValue = value;
		var digitCount = 1;
		while (tmpValue >= 10)
		{
			digitCount++;
			tmpValue /= 10;
		}

		// 使う桁を有効化
		for (var i = 0; i < digitCount; i++)
		{
			if (i < digits.Count)
			{
				digits[i].gameObject.SetActive(true);
			}
		}

		// 足りない桁補充
		while (digits.Count < digitCount)
		{
			var digit = Instantiate(digitPrefab, transform, false);
			digits.Add(digit);
		}

		// 余ってる桁を無効化
		for (var i = digitCount; i < digits.Count; i++)
		{
			digits[i].gameObject.SetActive(false);
		}

		var scale = 1;
		while ((scale * 10) <= value)
		{
			scale *= 10;
		}

		// 使う桁に値をセットしてレイアウトする
		var x = 0f;
		tmpValue = value;
		for (var i = 0; i < digitCount; i++)
		{
			var q = tmpValue / scale;
			tmpValue -= q * scale;
			scale /= 10;
			x = digits[i].Layout(x, height, q, color);
		}
	}
}
