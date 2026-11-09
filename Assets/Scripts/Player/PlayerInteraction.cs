using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using InfiniteLabyrinth.Interaction;
using InfiniteLabyrinth.Inventory;
using InfiniteLabyrinth.UI;

namespace InfiniteLabyrinth.Player
{
  /// <summary>
  /// Nhạc trưởng điều phối toàn bộ tương tác và nhặt đồ tập trung (Centralized Interaction & Pickup Controller).
  /// - Quét toàn bộ IInteractable (Cổng, Rương, Cửa, NPC) và WorldItemPickup (Đồ rơi).
  /// - Sắp xếp danh sách ứng viên theo cự ly & góc nhìn.
  /// - Điều hướng lựa chọn bằng con lăn chuột (Mouse Scroll Wheel) hoặc phím Mũi Tên.
  /// - Bấm/Giữ phím F CHỈ tương tác hoặc nhặt đúng mục tiêu đang được chọn (Selected Target).
  /// </summary>
  public class PlayerInteraction : MonoBehaviour
  {
    [Header("=== CẤU HÌNH TƯƠNG TÁC ===")]
    [Tooltip("Góc nón quét phía trước mặt nhân vật (độ) để ưu tiên đối tượng Player đang nhìn vào")]
    [SerializeField] private float viewAngleLimit = 160f;

    [Tooltip("Khoảng cách chuyển tiếp lăn chuột chống trượt quá nhanh (giây)")]
    [SerializeField] private float scrollCooldown = 0.08f;

    // Static Events đồng bộ với UI danh sách tương tác dọc
    public static event Action<IReadOnlyList<InteractionTarget>, int> OnTargetsUpdated;
    public static event Action<int, float> OnTargetHoldProgressChanged;

    // Legacy Events để tương thích ngược 100% với các hệ thống cũ nếu có
    public static event Action<IInteractable> OnInteractFocusChanged;
    public static event Action<IInteractable, float> OnInteractProgressChanged;

    private readonly List<InteractionTarget> currentCandidates = new List<InteractionTarget>();
    private int selectedIndex = 0;
    private float holdTimer = 0f;
    private float lastScrollTime = 0f;
    private InteractionTarget lastSelectedTarget = null;

    public IReadOnlyList<InteractionTarget> Candidates => currentCandidates;
    public int SelectedIndex => selectedIndex;
    public InteractionTarget CurrentSelectedTarget => (currentCandidates.Count > 0 && selectedIndex >= 0 && selectedIndex < currentCandidates.Count)
      ? currentCandidates[selectedIndex]
      : null;

    // Legacy property
    public IInteractable CurrentTarget => CurrentSelectedTarget?.Interactable;
    public float CurrentHoldProgress => CurrentSelectedTarget != null && CurrentSelectedTarget.RequiresHold && CurrentSelectedTarget.HoldDuration > 0f
      ? Mathf.Clamp01(holdTimer / CurrentSelectedTarget.HoldDuration)
      : 0f;

    private void Start()
    {
      InteractionListUI.EnsureInstance();
    }

    private void Update()
    {
      // 1. Nếu đang mở túi đồ hoặc cửa hàng thì hủy tương tác ngay lập tức
      if (InventoryUIController.IsOpen || ShopUIController.IsOpen)
      {
        ClearAllTargets();
        return;
      }

      // 2. Quét và cập nhật danh sách ứng viên trong tầm
      UpdateCandidates();

      // 3. Xử lý lăn chuột hoặc phím mũi tên để chuyển mục tiêu
      HandleScrollSelection();

      // 4. Xử lý bấm/giữ phím F cho mục tiêu đang chọn
      HandleInput();
    }

    private void UpdateCandidates()
    {
      Vector3 playerPos = transform.position;
      Vector3 playerForward = transform.forward;

      var validTargets = new List<(InteractionTarget target, float score)>();

      // A. Quét các đối tượng IInteractable (Cổng, Rương, Cửa, NPC...)
      var allInteractables = InteractableRegistry.ActiveInteractables;
      for (int i = 0; i < allInteractables.Count; i++)
      {
        var candidate = allInteractables[i];
        if (candidate == null || !candidate.CanInteract(gameObject)) continue;

        Transform tr = candidate.InteractionTransform;
        if (tr == null) continue;

        if (EvaluateTarget(tr.position, candidate.InteractionRadius, playerPos, playerForward, out float score))
        {
          validTargets.Add((new InteractionTarget(candidate), score));
        }
      }

      // B. Quét các vật phẩm WorldItemPickup rơi dưới đất
      var allPickups = WorldItemPickup.ActivePickups;
      for (int i = 0; i < allPickups.Count; i++)
      {
        var pickup = allPickups[i];
        if (pickup == null || !pickup.IsLanded || pickup.ItemData == null) continue;

        // Tiền vàng tự động hút bằng nam châm, không hiển thị trong danh sách bấm phím F
        if (pickup.ItemData.ItemType == ItemType.Currency) continue;

        if (EvaluateTarget(pickup.transform.position, pickup.PickupRadius, playerPos, playerForward, out float score))
        {
          validTargets.Add((new InteractionTarget(pickup), score));
        }
      }

      // TRƯỜNG HỢP 1: Không có bất kỳ mục tiêu nào trong tầm
      if (validTargets.Count == 0)
      {
        if (currentCandidates.Count > 0)
        {
          currentCandidates.Clear();
          selectedIndex = 0;
          NotifyTargetsChanged();
        }
        return;
      }

      // TRƯỜNG HỢP 2: Trước đó danh sách đang rỗng (vừa bước vào phạm vi của cụm item)
      // -> Sắp xếp 1 lần duy nhất theo score (gần nhất / thẳng mắt nhất nằm đầu danh sách)
      if (currentCandidates.Count == 0)
      {
        validTargets.Sort((a, b) => a.score.CompareTo(b.score));
        for (int i = 0; i < validTargets.Count; i++)
        {
          currentCandidates.Add(validTargets[i].target);
        }
        selectedIndex = 0;
        NotifyTargetsChanged();
        return;
      }

      // TRƯỜNG HỢP 3: Đã có sẵn danh sách các item (Stable Order Retention)
      // Giữ nguyên 100% thứ tự các item đã có trong danh sách để khi người chơi đi vòng quanh không bị đảo lộn vị trí!
      bool listChanged = false;
      InteractionTarget prevSelectedTarget = CurrentSelectedTarget;

      // 1. Loại bỏ những mục tiêu không còn hợp lệ hoặc đã đi ra khỏi tầm
      for (int i = currentCandidates.Count - 1; i >= 0; i--)
      {
        var existing = currentCandidates[i];
        if (existing == null || !existing.IsValid())
        {
          currentCandidates.RemoveAt(i);
          listChanged = true;
          continue;
        }

        bool stillValid = false;
        for (int j = 0; j < validTargets.Count; j++)
        {
          if (AreTargetsEqual(existing, validTargets[j].target))
          {
            stillValid = true;
            break;
          }
        }

        if (!stillValid)
        {
          currentCandidates.RemoveAt(i);
          listChanged = true;
        }
      }

      // 2. Thêm các mục tiêu MỚI xuất hiện vào cuối danh sách (không làm xáo trộn các mục đang hiển thị)
      var newEntries = new List<(InteractionTarget target, float score)>();
      for (int i = 0; i < validTargets.Count; i++)
      {
        bool alreadyExists = false;
        for (int j = 0; j < currentCandidates.Count; j++)
        {
          if (AreTargetsEqual(validTargets[i].target, currentCandidates[j]))
          {
            alreadyExists = true;
            break;
          }
        }

        if (!alreadyExists)
        {
          newEntries.Add(validTargets[i]);
        }
      }

      if (newEntries.Count > 0)
      {
        newEntries.Sort((a, b) => a.score.CompareTo(b.score));
        for (int i = 0; i < newEntries.Count; i++)
        {
          currentCandidates.Add(newEntries[i].target);
        }
        listChanged = true;
      }

      // 3. Nếu danh sách có thay đổi (có mục rời đi hoặc mục mới vào):
      if (listChanged)
      {
        if (prevSelectedTarget != null && currentCandidates.Count > 0)
        {
          int foundIndex = -1;
          for (int i = 0; i < currentCandidates.Count; i++)
          {
            if (AreTargetsEqual(currentCandidates[i], prevSelectedTarget))
            {
              foundIndex = i;
              break;
            }
          }

          if (foundIndex >= 0)
          {
            selectedIndex = foundIndex;
          }
          else
          {
            selectedIndex = Mathf.Clamp(selectedIndex, 0, currentCandidates.Count - 1);
          }
        }
        else
        {
          selectedIndex = 0;
        }

        NotifyTargetsChanged();
      }
    }

    private bool EvaluateTarget(Vector3 targetPos, float radius, Vector3 playerPos, Vector3 playerForward, out float score)
    {
      score = float.MaxValue;

      Vector3 diff = targetPos - playerPos;
      float verticalDist = Mathf.Abs(diff.y);
      Vector3 horizontalDiff = new Vector3(diff.x, 0f, diff.z);
      float horizontalDist = horizontalDiff.magnitude;

      // Khoảng cách ngang trong bán kính tương tác và chênh lệch cao độ hợp lý (< 4.5m)
      if (horizontalDist <= radius && verticalDist <= 4.5f)
      {
        float angle = 0f;
        if (horizontalDist > 0.01f)
        {
          Vector3 dir = horizontalDiff / horizontalDist;
          angle = Vector3.Angle(playerForward, dir);
        }

        // Nếu đứng rất gần (<= 2.2m) thì luôn hợp lệ, nếu xa hơn thì phải quay mặt về hướng đối tượng
        bool isFacingOrVeryClose = (horizontalDist <= 2.2f) || (angle <= viewAngleLimit * 0.5f);
        if (isFacingOrVeryClose)
        {
          score = horizontalDist + (angle / 90f) * 0.5f;
          return true;
        }
      }

      return false;
    }

    private bool AreTargetsEqual(InteractionTarget a, InteractionTarget b)
    {
      if (a == null || b == null) return a == b;
      if (a.Type != b.Type) return false;
      if (a.Type == InteractionTargetType.Interactable) return a.Interactable == b.Interactable;
      return a.Pickup == b.Pickup;
    }

    private void HandleScrollSelection()
    {
      if (currentCandidates.Count <= 1) return;

      var mouse = Mouse.current;
      var keyboard = Keyboard.current;

      float scrollDelta = mouse != null ? mouse.scroll.ReadValue().y : 0f;
      bool upPressed = keyboard != null && keyboard.upArrowKey.wasPressedThisFrame;
      bool downPressed = keyboard != null && keyboard.downArrowKey.wasPressedThisFrame;

      if (Time.unscaledTime - lastScrollTime >= scrollCooldown)
      {
        if (scrollDelta > 0.15f || upPressed)
        {
          SelectPrevious();
          lastScrollTime = Time.unscaledTime;
        }
        else if (scrollDelta < -0.15f || downPressed)
        {
          SelectNext();
          lastScrollTime = Time.unscaledTime;
        }
      }
    }

    public void SelectNext()
    {
      if (currentCandidates.Count <= 1) return;
      ResetHold();
      selectedIndex = (selectedIndex + 1) % currentCandidates.Count;
      NotifyTargetsChanged();
    }

    public void SelectPrevious()
    {
      if (currentCandidates.Count <= 1) return;
      ResetHold();
      selectedIndex = (selectedIndex - 1 + currentCandidates.Count) % currentCandidates.Count;
      NotifyTargetsChanged();
    }

    private void HandleInput()
    {
      if (currentCandidates.Count == 0 || selectedIndex < 0 || selectedIndex >= currentCandidates.Count) return;

      var keyboard = Keyboard.current;
      if (keyboard == null) return;

      var selected = currentCandidates[selectedIndex];

      if (!selected.RequiresHold)
      {
        // TAP (Bấm 1 cái ăn ngay)
        if (keyboard.fKey.wasPressedThisFrame)
        {
          ExecuteSelectedTarget();
        }
      }
      else
      {
        // HOLD (Đè giữ phím F)
        if (keyboard.fKey.isPressed)
        {
          holdTimer += Time.deltaTime;
          float duration = Mathf.Max(0.01f, selected.HoldDuration);
          float progress = Mathf.Clamp01(holdTimer / duration);
          OnTargetHoldProgressChanged?.Invoke(selectedIndex, progress);
          if (selected.Interactable != null)
          {
            OnInteractProgressChanged?.Invoke(selected.Interactable, progress);
          }

          if (holdTimer >= duration)
          {
            ResetHold();
            ExecuteSelectedTarget();
          }
        }
        else
        {
          if (holdTimer > 0f)
          {
            ResetHold();
          }
        }
      }
    }

    private void ExecuteSelectedTarget()
    {
      if (currentCandidates.Count == 0 || selectedIndex < 0 || selectedIndex >= currentCandidates.Count) return;

      var targetToExecute = currentCandidates[selectedIndex];
      targetToExecute.Execute(gameObject);

      // Nếu đối tượng là Pickup vừa nhặt xong hoặc Interactable không còn tương tác được nữa
      if (!targetToExecute.IsValid())
      {
        currentCandidates.RemoveAt(selectedIndex);
        if (selectedIndex >= currentCandidates.Count)
        {
          selectedIndex = Mathf.Max(0, currentCandidates.Count - 1);
        }
        NotifyTargetsChanged();
      }
    }

    private void ResetHold()
    {
      if (holdTimer > 0f)
      {
        holdTimer = 0f;
        OnTargetHoldProgressChanged?.Invoke(selectedIndex, 0f);
        if (CurrentSelectedTarget?.Interactable != null)
        {
          OnInteractProgressChanged?.Invoke(CurrentSelectedTarget.Interactable, 0f);
        }
      }
    }

    private void NotifyTargetsChanged()
    {
      ResetHold();
      OnTargetsUpdated?.Invoke(currentCandidates, selectedIndex);

      var selected = CurrentSelectedTarget;
      if (selected != null && selected.Type == InteractionTargetType.Interactable)
      {
        OnInteractFocusChanged?.Invoke(selected.Interactable);
      }
      else
      {
        OnInteractFocusChanged?.Invoke(null);
      }
    }

    private void ClearAllTargets()
    {
      ResetHold();
      currentCandidates.Clear();
      selectedIndex = 0;
      NotifyTargetsChanged();
    }

    private void OnDisable()
    {
      ClearAllTargets();
    }
  }
}
