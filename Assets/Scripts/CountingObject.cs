using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CountingObject : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
	[SerializeField] new Rigidbody rigidbody;

	public void ManualStart(QuestionSubScene questionScene, Vector3 initialPosition)
	{
		this.questionScene = questionScene;
		rigidbody.MovePosition(initialPosition);
		rigidbody.transform.position = initialPosition;
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		questionScene.OnBeginDragCountingObject(rigidbody, eventData.position);
	}

	public void OnDrag(PointerEventData eventData)
	{
		questionScene.OnDragCountingObject(eventData.position);
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		questionScene.OnEndDragCountingObject();		
	}

	// non public 
	QuestionSubScene questionScene;
}
