using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PointerEventBlocker : 
MonoBehaviour, 
IPointerClickHandler, 
IPointerDownHandler, 
IPointerUpHandler, 
IBeginDragHandler, 
IDragHandler, 
IEndDragHandler
{
	public void OnPointerClick(PointerEventData e){}
	public void OnPointerDown(PointerEventData e){}
	public void OnPointerUp(PointerEventData e){}
	public void OnBeginDrag(PointerEventData e){}
	public void OnDrag(PointerEventData e){}
	public void OnEndDrag(PointerEventData e){}
}
