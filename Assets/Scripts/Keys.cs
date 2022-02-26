using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Keys
{
	public string VisionApiKey { get => visionApiKey; }

	// non public ---
	[SerializeField] string visionApiKey;
}
