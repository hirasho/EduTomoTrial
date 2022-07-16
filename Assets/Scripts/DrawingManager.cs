using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawingManager
{
	class Pointer
	{
		public Pointer()
		{
			lines = new List<Line>();
			prevPoint = Vector2.one * float.MaxValue;
		}
		public List<Line> lines;
		public bool down;
		public bool isEraser;
		public Vector2 prevPoint;
	}

	public DrawingManager()
	{
		pointers = new Dictionary<int, Pointer>();
	}

	public void ManualUpdate(
		ref Vector3 eraserPosition,
		TouchDetector touchDetector, 
		Camera camera)
	{
		foreach (var pair in pointers)
		{
			var pointerId = pair.Key;
			var pointer = pair.Value;
			if (pointer.down)
			{
//Debug.Log(pointerId + " " + pointer.prevPoint);
				var point = touchDetector.GetScreenPosition(pointerId);
				var ray = camera.ScreenPointToRay(point);
				// y=0点を取得
				var t = (0f - ray.origin.y) / ray.direction.y;
				var wp = ray.origin + (ray.direction * t);
				if (pointer.isEraser)
				{
					eraserPosition = wp;
				}
				else
				{
					if (pointer.lines.Count > 0)
					{
						pointer.lines[pointer.lines.Count - 1].AddPoint(wp);
					}
				}
				pointer.prevPoint = point;
			}
		}
	}

	public void OnPointerDown(
		ref int strokeCount,
		Line linePrefab,
		Transform lineRoot,
		TouchDetector touchDetector,
		Camera camera,
		float lineWidth,
		int pointerId,
		bool isEraser)
	{
		Pointer pointer;
		if (!pointers.TryGetValue(pointerId, out pointer))
		{
			pointer = new Pointer();
			pointers.Add(pointerId, pointer);
		}

		if (!isEraser)
		{
			var line = GameObject.Instantiate(linePrefab, lineRoot, false);
			line.ManualStart(camera, lineWidth);
			if (pointer.lines.Count > 0)
			{
				pointer.lines[pointer.lines.Count - 1].GenerateCollider();
			}
			pointer.lines.Add(line);
			strokeCount++;
		}
		pointer.isEraser = isEraser;
		pointer.down = true;
		pointer.prevPoint = touchDetector.GetScreenPosition(pointerId);
//Debug.Log("Down: "  + pointerId + " " + pointer + " " + pointer.prevPoint);
	}

	public void OnPointerUp(
		out bool evaluationRequested,
		bool justErased,
		int pointerId)
	{
//Debug.Log("Up: " + pointerId);
		Pointer pointer;
		if (!pointers.TryGetValue(pointerId, out pointer))
		{
			pointer = new Pointer();
			pointers.Add(pointerId, pointer);
		}

		evaluationRequested = justErased;
		if (!pointer.isEraser)
		{
			if (pointer.lines.Count > 0)
			{
				pointer.lines[pointer.lines.Count - 1].GenerateCollider();
			}
			evaluationRequested = true;
		}
		pointer.down = false;
		pointer.isEraser = false;
	}

	public void RemoveLine(
		ref int eraseCount,
		ref bool justErased,
		Line line)
	{
		var dst = 0;
		foreach (var pointer in pointers.Values)
		{
			var lines = pointer.lines;
			for (var i = 0; i < lines.Count; i++)
			{
				lines[dst] = lines[i];
				if (lines[i] == line)
				{
					GameObject.Destroy(lines[i].gameObject);
					eraseCount++;
					justErased = true;
				}
				else
				{
					dst++;
				}
			}
			lines.RemoveRange(dst, lines.Count - dst);
		}
	}

	public void ClearLines()
	{
		foreach (var pointer in pointers.Values)
		{
			var lines = pointer.lines;
			foreach (var line in lines)
			{
				GameObject.Destroy(line.gameObject);
			}
			lines.Clear();
		}
	}

	public IEnumerable<Line> EnumerateLines()
	{
		foreach (var pointer in pointers.Values)
		{
			var lines = pointer.lines;
			foreach (var line in lines)
			{
				yield return line;
			}
		}
	}

	// non public ----
	Dictionary<int, Pointer> pointers;
}
