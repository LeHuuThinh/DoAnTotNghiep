using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Bắt sự kiện hover chuột trên từng dòng Enchant để kích hoạt / ẩn EnchantHint
  /// </summary>
  public class EnchantRowHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
  {
    public Action<int> onHoverEnter;
    public Action<int> onHoverExit;
    public int rowIndex;

    public void OnPointerEnter(PointerEventData eventData)
    {
      onHoverEnter?.Invoke(rowIndex);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
      onHoverExit?.Invoke(rowIndex);
    }
  }
}
