using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class FakeCursorClicker : MonoBehaviour
{
    [SerializeField]
    private RectTransform fakeCursorRect;

    // 현재 포인터 이벤트 데이터를 저장할 변수 (상태 유지를 위해)
    private PointerEventData pointerData;

    // 드래그 중인지 판단하기 위한 플래그
    private bool isDragging = false;

    void Update()
    {
        // --- 1. 버튼을 처음 눌렀을 때 (Down) ---
        if (Input.GetButtonDown("Submit") || Input.GetMouseButtonDown(0))
        {
            // 새로운 포인터 데이터 생성
            pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = fakeCursorRect.position;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            if (results.Count > 0)
            {
                GameObject target = results[0].gameObject;

                // "Pointer Down" 이벤트를 받을 오브젝트를 찾아 실행
                pointerData.pointerPress = ExecuteEvents.GetEventHandler<IPointerDownHandler>(target);
                if (pointerData.pointerPress != null)
                {
                    ExecuteEvents.Execute(pointerData.pointerPress, pointerData, ExecuteEvents.pointerDownHandler);
                }

                // "Begin Drag" 이벤트를 받을 오브젝트를 찾아 실행
                pointerData.pointerDrag = ExecuteEvents.GetEventHandler<IBeginDragHandler>(target);
                if (pointerData.pointerDrag != null)
                {
                    ExecuteEvents.Execute(pointerData.pointerDrag, pointerData, ExecuteEvents.beginDragHandler);
                    isDragging = true; // 드래그 시작됨
                }
            }
        }

        // --- 2. 버튼을 뗐을 때 (Up) ---
        if (Input.GetButtonUp("Submit") || Input.GetMouseButtonUp(0))
        {
            // 눌렀던 상태가 아니면 무시
            if (pointerData == null)
                return;

            // "Pointer Up" 이벤트 전송 (Down을 받았던 오브젝트에게)
            if (pointerData.pointerPress != null)
            {
                GameObject upHandler = ExecuteEvents.GetEventHandler<IPointerUpHandler>(pointerData.pointerPress);
                if (upHandler != null)
                {
                    ExecuteEvents.Execute(upHandler, pointerData, ExecuteEvents.pointerUpHandler);
                }
            }

            // "End Drag" 이벤트 전송 (Drag를 받고 있던 오브젝트에게)
            if (pointerData.pointerDrag != null)
            {
                ExecuteEvents.Execute(pointerData.pointerDrag, pointerData, ExecuteEvents.endDragHandler);
                isDragging = false; // 드래그 끝
            }

            // "Pointer Click" 이벤트 전송 (드래그 중이 아니었을 때만)
            if (!pointerData.dragging) // pointerData.dragging은 BeginDrag/Drag/EndDrag에 의해 내부적으로 설정됨
            {
                GameObject clickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(pointerData.pointerPress);
                if (clickHandler != null)
                {
                    ExecuteEvents.Execute(clickHandler, pointerData, ExecuteEvents.pointerClickHandler);
                }
            }

            // 상태 초기화
            pointerData = null;
        }

        // --- 3. 버튼을 누르고 있는 동안 (Held) ---
        if (Input.GetButton("Submit") || Input.GetMouseButton(0))
        {
            // 드래그 중일 때만
            if (isDragging && pointerData != null && pointerData.pointerDrag != null)
            {
                // 현재 커서 위치로 포인터 데이터 업데이트
                pointerData.position = fakeCursorRect.position;

                // "Drag" 이벤트 전송
                ExecuteEvents.Execute(pointerData.pointerDrag, pointerData, ExecuteEvents.dragHandler);
            }
        }
    }
}