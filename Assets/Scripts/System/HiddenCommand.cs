using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class HiddenCommand : BaseRaycaster
{
	public void SetPassword(string password)
	{
		this.password = password;
		this.current = new string(' ', password.Length);
	}

	// 不便かつ原始的なポーリング型インターフェイス
	public bool Unlocked { get; private set; }
	// 発動後はこれを
	public void ClearUnlocked()
	{
		Unlocked = false;            
	}

	public override Camera eventCamera
	{
		get
		{
			return Camera.main;
		}
	}

	// 他に影響を及ぼさないために、イベントは送信せず、ここで全部処理する
	public override void Raycast(
	PointerEventData eventData,
	List<RaycastResult> resultAppendList)
	{
		var newPointerDown = (eventData.pointerPress != null);
		if (!pointerDown && newPointerDown) // 押された瞬間
		{
			var p = eventData.position;
			int x = Mathf.FloorToInt(p.x * 3f / Screen.width);
			int y = Mathf.FloorToInt(p.y * 3f / Screen.height);
			if ((x >= 0) && (x < 3) && (y >= 0) && (y < 3))
			{
				current = current.Substring(1); // 先頭除去
				current += new string((char)('1' + (y * 3) + x), 1);
				if (current == password)
				{
					Unlocked = true;
				}
			}
		}
		pointerDown = newPointerDown;
	}

	// non public --------
	string current = " "; 
	bool pointerDown;
	string password = "A"; //絶対ない1文字入れとく

#if UNITY_EDITOR
	[UnityEditor.CustomEditor(typeof(HiddenCommand))]
	class CustomEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			var self = target as HiddenCommand;
			base.OnInspectorGUI();
			EditorGUILayout.TextField("現在値", self.current);
			EditorGUILayout.TextField("パスワード", self.password);
			if (GUILayout.Button("発動"))
			{
				self.Unlocked = true;
			}
		}
	}
#endif
}
