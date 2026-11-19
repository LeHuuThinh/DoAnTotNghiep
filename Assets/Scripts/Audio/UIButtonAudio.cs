using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace InfiniteLabyrinth.Audio
{
    /// <summary>
    /// Gắn lên bất kỳ Button UI nào để tự động phát âm thanh khi click chuột:
    /// - Nút bấm bình thường -> PlayUIInteract (Click / tương tác UI dứt khoát)
    /// - Nút bị disable / không thể bấm -> PlayUIDisabledClick (Tít trầm cảnh báo)
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class UIButtonAudio : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("Nếu tích, sẽ bỏ qua âm thanh click (dành cho nút đã có sound riêng như Đe rèn / Giao dịch shop)")]
        [SerializeField] private bool muteCustomClick = false;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (muteCustomClick) return;

            Selectable sel = GetComponent<Selectable>();
            if (sel != null && sel.interactable)
            {
                AudioManager.Instance?.PlayUIInteract();
            }
            else
            {
                AudioManager.Instance?.PlayUIDisabledClick();
            }
        }
    }
}
