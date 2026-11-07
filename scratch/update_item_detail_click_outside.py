import os

path = r"Assets/Scripts/UI/Inventory/ItemDetailPopupUI.cs"
with open(path, "r", encoding="utf-8") as f:
    content = f.read()

# 1. Update usings
target_usings = """using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InfiniteLabyrinth.Inventory;"""

replacement_usings = """using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using InfiniteLabyrinth.Inventory;"""

assert target_usings.replace("\r\n", "\n") in content.replace("\r\n", "\n"), "target_usings not found"
content_norm = content.replace("\r\n", "\n").replace(target_usings.replace("\r\n", "\n"), replacement_usings.replace("\r\n", "\n"))

# 2. Add fields in ItemDetailPopupUI
target_fields = """    private CanvasGroup popupCanvasGroup;
    private Coroutine activeTransitionRoutine;"""

replacement_fields = """    private CanvasGroup popupCanvasGroup;
    private Coroutine activeTransitionRoutine;
    private RectTransform popupRectTransform;
    private int lastOpenedFrame = -1;"""

assert target_fields.replace("\r\n", "\n") in content_norm, "target_fields not found"
content_norm = content_norm.replace(target_fields.replace("\r\n", "\n"), replacement_fields.replace("\r\n", "\n"))

# 3. In AutoFindReferences, cache popupRectTransform
target_autofind = """      if (popupCanvasGroup == null)
      {
        popupCanvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
      }
    }"""

replacement_autofind = """      if (popupCanvasGroup == null)
      {
        popupCanvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
      }
      if (popupRectTransform == null)
      {
        popupRectTransform = transform as RectTransform;
      }
    }"""

assert target_autofind.replace("\r\n", "\n") in content_norm, "target_autofind not found"
content_norm = content_norm.replace(target_autofind.replace("\r\n", "\n"), replacement_autofind.replace("\r\n", "\n"))

# 4. In Update, call CheckClickOutside()
target_update = """    private void Update()
    {
      if (btnUse != null && btnUse.gameObject.activeSelf && InventoryManager.HasInstance)
      {
        btnUse.interactable = !InventoryManager.Instance.IsConsumableOnCooldown;
      }
    }"""

replacement_update = """    private void Update()
    {
      if (btnUse != null && btnUse.gameObject.activeSelf && InventoryManager.HasInstance)
      {
        btnUse.interactable = !InventoryManager.Instance.IsConsumableOnCooldown;
      }

      CheckClickOutside();
    }

    /// <summary>
    /// Đóng popup khi click chuột ra bên ngoài khung chi tiết (trừ trường hợp click vào ô đồ khác)
    /// </summary>
    private void CheckClickOutside()
    {
      if (!gameObject.activeSelf) return;

      // Đang mờ dần hoặc chưa hiển thị
      if (popupCanvasGroup != null && popupCanvasGroup.alpha < 0.05f) return;

      // Bỏ qua nếu không có phím chuột trái vừa nhấn trong frame này
      if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;

      // Bỏ qua frame vừa mở popup để tránh đóng ngay lập tức
      if (Time.frameCount == lastOpenedFrame) return;

      // Bỏ qua nếu đang kéo thả vật phẩm trong kho đồ
      if (InventoryDragDropController.Instance != null && InventoryDragDropController.Instance.IsDragging) return;

      Vector2 mousePos = Mouse.current.position.ReadValue();

      // 1. Kiểm tra xem chuột có nằm trong khung của ItemDetailPopup hay không
      if (popupRectTransform == null) popupRectTransform = transform as RectTransform;

      Canvas rootCanvas = GetComponentInParent<Canvas>();
      Camera uiCam = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay) ? rootCanvas.worldCamera : null;

      if (popupRectTransform != null && RectTransformUtility.RectangleContainsScreenPoint(popupRectTransform, mousePos, uiCam))
      {
        // Chuột nằm trong khung chi tiết -> Cho phép thao tác các nút bên trong bình thường
        return;
      }

      // 2. Chuột nằm ngoài khung popup: Kiểm tra xem có click trúng ô đồ nào không
      if (EventSystem.current != null)
      {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
          position = mousePos
        };

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        for (int i = 0; i < raycastResults.Count; i++)
        {
          GameObject hitObj = raycastResults[i].gameObject;
          if (hitObj == null) continue;

          // Nếu click trúng bất kỳ ô đồ nào (túi đồ hoặc trang bị) -> Để sự kiện Click của ô đó tự xử lý (chuyển item hoặc toggle)
          if (hitObj.GetComponentInParent<InventorySlotUI>() != null ||
              hitObj.GetComponentInParent<EquipmentSlotUI>() != null)
          {
            return;
          }
        }
      }

      // 3. Chuột bấm ra ngoài khung popup và không bấm vào ô đồ nào -> Tự động đóng popup
      Hide();
    }"""

assert target_update.replace("\r\n", "\n") in content_norm, "target_update not found"
content_norm = content_norm.replace(target_update.replace("\r\n", "\n"), replacement_update.replace("\r\n", "\n"))

# 5. Record lastOpenedFrame in ShowForBagSlot and ShowForEquipmentSlot
target_show_bag = """      currentBagSlot.SetSelected(true);
      UpdateUIContent();
      ShowNearSlotAnimated(slot.transform as RectTransform);
    }"""

replacement_show_bag = """      currentBagSlot.SetSelected(true);
      UpdateUIContent();
      lastOpenedFrame = Time.frameCount;
      ShowNearSlotAnimated(slot.transform as RectTransform);
    }"""

assert target_show_bag.replace("\r\n", "\n") in content_norm, "target_show_bag not found"
content_norm = content_norm.replace(target_show_bag.replace("\r\n", "\n"), replacement_show_bag.replace("\r\n", "\n"))

target_show_eq = """      currentEquipSlot.SetSelected(true);
      UpdateUIContent();
      ShowNearSlotAnimated(eqSlot.transform as RectTransform);
    }"""

replacement_show_eq = """      currentEquipSlot.SetSelected(true);
      UpdateUIContent();
      lastOpenedFrame = Time.frameCount;
      ShowNearSlotAnimated(eqSlot.transform as RectTransform);
    }"""

assert target_show_eq.replace("\r\n", "\n") in content_norm, "target_show_eq not found"
content_norm = content_norm.replace(target_show_eq.replace("\r\n", "\n"), replacement_show_eq.replace("\r\n", "\n"))

with open(path, "w", encoding="utf-8") as f:
    f.write(content_norm)

print("ITEM_DETAIL_POPUP_CLICK_OUTSIDE_UPDATED_SUCCESSFULLY")
