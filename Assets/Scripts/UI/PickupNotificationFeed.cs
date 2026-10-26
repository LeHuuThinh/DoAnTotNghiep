using System.Collections.Generic;
using UnityEngine;
using InfiniteLabyrinth.Inventory;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Quản lý danh sách thông báo nhặt đồ xếp chồng ở góc mép trái màn hình
  /// Lắng nghe trực tiếp từ sự kiện InventoryManager.OnItemAdded
  /// </summary>
  public class PickupNotificationFeed : MonoBehaviour
  {
    [Header("=== CẤU HÌNH FEED ===")]
    [Tooltip("Prefab của từng dòng thông báo (PickupNotificationItem)")]
    [SerializeField] private GameObject notificationPrefab;

    [Tooltip("Số lượng thông báo tối đa hiển thị cùng một lúc")]
    [SerializeField] private int maxVisibleItems = 5;

    [Tooltip("Thời gian mỗi thông báo hiển thị trước khi mờ dần (giây)")]
    [SerializeField] private float itemDisplayDuration = 2.5f;

    private readonly List<GameObject> activeNotifications = new List<GameObject>();

    private void Start()
    {
      SubscribeToInventory();
    }

    private void OnEnable()
    {
      SubscribeToInventory();
    }

    private void OnDisable()
    {
      UnsubscribeFromInventory();
    }

    private void SubscribeToInventory()
    {
      if (InventoryManager.HasInstance)
      {
        InventoryManager.Instance.OnItemAdded -= HandleItemAdded;
        InventoryManager.Instance.OnItemAdded += HandleItemAdded;
      }
    }

    private void UnsubscribeFromInventory()
    {
      if (InventoryManager.HasInstance)
      {
        InventoryManager.Instance.OnItemAdded -= HandleItemAdded;
      }
    }

    private void Update()
    {
      // Failsafe kết nối lại nếu InventoryManager sinh sau
      if (InventoryManager.HasInstance)
      {
        SubscribeToInventory();
      }

      // Dọn dẹp danh sách khi các item tự hủy
      for (int i = activeNotifications.Count - 1; i >= 0; i--)
      {
        if (activeNotifications[i] == null)
        {
          activeNotifications.RemoveAt(i);
        }
      }
    }

    private void HandleItemAdded(ItemData item, int count)
    {
      if (item == null || count <= 0 || notificationPrefab == null) return;

      // Nếu vượt quá số lượng tối đa -> Hủy thông báo cũ nhất ở trên cùng
      if (activeNotifications.Count >= maxVisibleItems)
      {
        if (activeNotifications[0] != null)
        {
          Destroy(activeNotifications[0]);
        }
        activeNotifications.RemoveAt(0);
      }

      // Sinh thông báo mới làm con của Feed Container
      GameObject newNotiObj = Instantiate(notificationPrefab, transform, false);
      
      // Đảm bảo PosZ luôn bằng 0 và Scale là (1,1,1)
      RectTransform rt = newNotiObj.GetComponent<RectTransform>();
      if (rt != null)
      {
        Vector3 pos = rt.localPosition;
        pos.z = 0f;
        rt.localPosition = pos;
        rt.localScale = Vector3.one;
      }

      activeNotifications.Add(newNotiObj);

      var notiItem = newNotiObj.GetComponent<PickupNotificationItem>();
      if (notiItem != null)
      {
        notiItem.Setup(item, count, itemDisplayDuration);
      }
    }
  }
}
