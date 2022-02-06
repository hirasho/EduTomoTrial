using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UiDigit : MonoBehaviour
{
	[SerializeField] int value;
	[SerializeField] Sprite[] sprites; // 0,1,2....9の順
	[SerializeField] Image image;

	public float Layout(float x, float height, int value, Color color)
	{
		if (value < 0)
		{
			value = -value;
		}
		this.value = value % 10;
		var sprite = sprites[this.value];
		image.sprite = sprite;	
		var origH = sprite.rect.height;
		var origW = sprite.rect.width;
		var scale = height / origH;
		var w = origW * scale;
		image.color = color;
		image.rectTransform.sizeDelta = new Vector3(w, height);
		image.rectTransform.anchoredPosition = new Vector3(x, 0f);
		return x + w;
	}

	void Start()
	{
	}

	void Update()
	{
	}

	// non public ---
}
