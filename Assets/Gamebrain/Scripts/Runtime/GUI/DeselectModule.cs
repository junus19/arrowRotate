using System;
using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

namespace GameBrain.Casual
{
    public class DeselectModule : MonoBehaviour, IDeselectHandler
    {
        [SerializeField] RectTransform rectTransform;
        public Action OnDeselected;

        public void SetSelected()
        {
            EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition, null))
                    StartCoroutine(SelectObject());
                else
                    OnDeselected.Invoke();
            }
        }

        IEnumerator SelectObject()
        {
            yield return null;
            SetSelected();
        }
    }
}
