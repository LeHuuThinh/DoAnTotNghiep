using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InfiniteLabyrinth.Inventory;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Bảng hiển thị thông báo tương tác phím [F] khi lại gần vật phẩm rơi trong thế giới 3D.
  /// Hiển thị Icon 2D, Tên món đồ đổi màu theo phẩm cấp (Rarity), và phím bấm tương tác.
  /// </summary>
  public class PickupPromptUI : MonoBehaviour
  {
    [Header("=== GIAO DIỆN HIỂN THỊ ===")]
    [Tooltip("Khung chứa bảng tương tác (để trống nếu script gắn trực tiếp trên khung này)")]
    [SerializeField] private GameObject promptRoot;
    [Tooltip("Icon 2D của vật phẩm")]
    [SerializeField] private Image itemIcon;
    [Tooltip("Viền màu hiển thị độ hiếm")]
    [SerializeField] private Image rarityBorder;
    [Tooltip("Tên vật phẩm")]
    [SerializeField] private TextMeshProUGUI itemNameText;
    [Tooltip("Gợi ý phím bấm và số lượng (ví dụ: [F] Nhặt x5)")]
    [SerializeField] private TextMeshProUGUI promptKeyText;
    [Tooltip("CanvasGroup điều khiển độ mờ")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("=== HIỆU ỨNG CHUYỂN ĐỘNG ===")]
    [SerializeField] private float fadeSpeed = 12f;

    private readonly List<WorldItemPickup> activePickups = new List<WorldItemPickup>();
    private WorldItemPickup currentTarget;
    private float targetAlpha = 0f;

    private void Awake()
    {
      if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

      // Đảm bảo ẩn lúc đầu qua Alpha và tắt raycast
      canvasGroup.alpha = 0f;
      canvasGroup.interactable = false;
      canvasGroup.blocksRaycasts = false;

      // Chỉ tắt Active nếu promptRoot là một GameObject con riêng biệt, 
      // TUYỆT ĐỐI không tắt GameObject chứa script này vì sẽ ngắt luôn Update và Event!
      if (promptRoot != null && promptRoot != gameObject)
      {
        promptRoot.SetActive(false);
      }

      // Tự động tìm kiếm fallback nếu người dùng thiết kế UI mà quên kéo dây Inspector
      AutoFindReferences();
    }

    private void AutoFindReferences()
    {
      if (itemIcon == null)
      {
        itemIcon = transform.Find("Icon")?.GetComponent<Image>() 
                ?? transform.Find("IconGroup/Icon")?.GetComponent<Image>()
                ?? GetComponentInChildren<Image>();
      }

      if (rarityBorder == null)
      {
        rarityBorder = transform.Find("Icon/Border")?.GetComponent<Image>()
                    ?? transform.Find("IconGroup/Border")?.GetComponent<Image>()
                    ?? transform.Find("Border")?.GetComponent<Image>();
      }

      if (itemNameText == null)
      {
        itemNameText = transform.Find("ItemName")?.GetComponent<TextMeshProUGUI>()
                    ?? transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
      }

      if (promptKeyText == null)
      {
        promptKeyText = transform.Find("KeyPrompt")?.GetComponent<TextMeshProUGUI>()
                     ?? transform.Find("Key")?.GetComponent<TextMeshProUGUI>()
                     ?? transform.Find("Prompt")?.GetComponent<TextMeshProUGUI>();
      }
    }

    private void OnEnable()
    {
      WorldItemPickup.OnFocusPickup += HandlePickupFocused;
      WorldItemPickup.OnUnfocusPickup += HandlePickupUnfocused;
    }

    private void OnDisable()
    {
      WorldItemPickup.OnFocusPickup -= HandlePickupFocused;
      WorldItemPickup.OnUnfocusPickup -= HandlePickupUnfocused;
      activePickups.Clear();
      currentTarget = null;
    }

    private void HandlePickupFocused(WorldItemPickup pickup)
    {
      if (pickup == null || activePickups.Contains(pickup)) return;
      activePickups.Add(pickup);
      UpdateTargetPickup();
    }

    private void HandlePickupUnfocused(WorldItemPickup pickup)
    {
      if (pickup == null) return;
      activePickups.Remove(pickup);
      UpdateTargetPickup();
    }

    private void Update()
    {
      // Dọn dẹp danh sách các pickup đã bị nhặt hoặc destroy
      for (int i = activePickups.Count - 1; i >= 0; i--)
      {
        if (activePickups[i] == null)
        {
          activePickups.RemoveAt(i);
        }
      }

      UpdateTargetPickup();

      // Cập nhật hiệu ứng Fade In / Out mượt mà
      if (canvasGroup != null)
      {
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
        
        // Quản lý bật/tắt promptRoot con (nếu có)
        if (promptRoot != null && promptRoot != gameObject)
        {
          if (targetAlpha > 0f && !promptRoot.activeSelf)
          {
            promptRoot.SetActive(true);
          }
          else if (targetAlpha == 0f && canvasGroup.alpha <= 0.005f && promptRoot.activeSelf)
          {
            promptRoot.SetActive(false);
          }
        }
      }
    }

    private void UpdateTargetPickup()
    {
      if (activePickups.Count == 0)
      {
        currentTarget = null;
        targetAlpha = 0f;
        return;
      }

      // Ưu tiên chọn vật phẩm gần nhất với Player thực tế (Tag "Player")
      Transform playerTr = null;
      var playerObj = GameObject.FindGameObjectWithTag("Player");
      if (playerObj != null)
      {
        playerTr = playerObj.transform;
      }
      else if (Camera.main != null)
      {
        playerTr = Camera.main.transform;
      }
      else
      {
        playerTr = transform;
      }

      WorldItemPickup closest = null;
      float closestDist = float.MaxValue;

      foreach (var p in activePickups)
      {
        if (p == null) continue;
        float d = Vector3.Distance(playerTr.position, p.transform.position);
        if (d < closestDist)
        {
          closestDist = d;
          closest = p;
        }
      }

      currentTarget = closest;

      if (currentTarget != null && currentTarget.ItemData != null)
      {
        DisplayPickupInfo(currentTarget.ItemData, currentTarget.Quantity);
        if (promptRoot != null && promptRoot != gameObject && !promptRoot.activeSelf)
        {
          promptRoot.SetActive(true);
        }
        targetAlpha = 1f;
      }
      else
      {
        targetAlpha = 0f;
      }
    }

    private void DisplayPickupInfo(ItemData item, int quantity)
    {
      Color rarityColor = item.Rarity.GetRarityColor();

      if (itemIcon != null)
      {
        itemIcon.sprite = item.Icon;
        itemIcon.enabled = (item.Icon != null);
      }

      if (rarityBorder != null)
      {
        rarityBorder.color = rarityColor;
      }

      if (itemNameText != null)
      {
        itemNameText.color = Color.white;
        itemNameText.text = item.Rarity.FormatWithGradient(item.ItemName);
      }

      if (promptKeyText != null)
      {
        if (quantity > 1)
        {
          promptKeyText.text = $"Pick up x{quantity}";
        }
        else
        {
          promptKeyText.text = "Pick up";
        }
      }
    }
  }
}
