using System;
using UnityEngine;
using UnityEngine.InputSystem;
using InfiniteLabyrinth.Interaction;
using InfiniteLabyrinth.UI;

namespace InfiniteLabyrinth.Player
{
  /// <summary>
  /// Quản lý phát hiện đối tượng tương tác gần nhất trước mặt Player và xử lý đè/nhả phím F
  /// </summary>
  public class PlayerInteraction : MonoBehaviour
  {
    [Header("=== CẤU HÌNH TƯƠNG TÁC ===")]
    [Tooltip("Góc nón quét phía trước mặt nhân vật (độ) để ưu tiên đối tượng Player đang nhìn vào")]
    [SerializeField] private float viewAngleLimit = 160f;

    // Static Events để UI bắt tín hiệu đồng bộ
    public static event Action<IInteractable> OnInteractFocusChanged;
    public static event Action<IInteractable, float> OnInteractProgressChanged;

    public IInteractable CurrentTarget => currentTarget;
    public float CurrentHoldProgress => currentTarget != null && currentTarget.RequiresHold && currentTarget.HoldDuration > 0f
      ? Mathf.Clamp01(holdTimer / currentTarget.HoldDuration)
      : 0f;

    private IInteractable currentTarget;
    private float holdTimer = 0f;

    private void Update()
    {
      // Nếu đang mở túi đồ thì hủy tương tác ngay lập tức
      if (InventoryUIController.IsOpen)
      {
        ClearTarget();
        return;
      }

      UpdateBestTarget();
      HandleInput();
    }

    private void UpdateBestTarget()
    {
      var all = InteractableRegistry.ActiveInteractables;
      IInteractable bestCandidate = null;
      float bestScore = float.MaxValue;
      Vector3 playerPos = transform.position;
      Vector3 playerForward = transform.forward;

      for (int i = 0; i < all.Count; i++)
      {
        var candidate = all[i];
        if (candidate == null || !candidate.CanInteract(gameObject)) continue;

        Vector3 targetPos = candidate.InteractionTransform.position;

        // Tính khoảng cách mặt phẳng ngang (XZ) và chênh lệch chiều cao (Y) riêng biệt
        // để hỗ trợ các đối tượng cao/thấp (như Cổng dịch chuyển 3.8m, cửa hầm ngục)
        Vector3 diff = targetPos - playerPos;
        float verticalDist = Mathf.Abs(diff.y);
        Vector3 horizontalDiff = new Vector3(diff.x, 0f, diff.z);
        float horizontalDist = horizontalDiff.magnitude;

        // Cự ly hợp lệ: khoảng cách ngang trong bán kính tương tác và chênh lệch chiều cao hợp lý (< 5m)
        if (horizontalDist <= candidate.InteractionRadius && verticalDist <= 5.0f)
        {
          float angle = 0f;
          if (horizontalDist > 0.01f)
          {
            Vector3 dir = horizontalDiff / horizontalDist;
            angle = Vector3.Angle(playerForward, dir);
          }

          // QUAN TRỌNG:
          // Nếu đứng RẤT GẦN (horizontalDist <= 2.2m) thì LUÔN cho phép tương tác
          // (khắc phục triệt để lỗi đứng sát mặt cổng hoặc đi ngang pivot bị mất UI)
          // Nếu đứng xa hơn (> 2.2m) thì yêu cầu Player phải quay mặt về hướng đối tượng (angle <= viewAngleLimit / 2)
          bool isFacingOrVeryClose = (horizontalDist <= 2.2f) || (angle <= viewAngleLimit * 0.5f);

          if (isFacingOrVeryClose)
          {
            // Score: Ưu tiên gần hơn và góc nhìn thẳng hơn
            float score = horizontalDist + (angle / 90f) * 0.5f;
            if (score < bestScore)
            {
              bestScore = score;
              bestCandidate = candidate;
            }
          }
        }
      }

      if (bestCandidate != currentTarget)
      {
        SetTarget(bestCandidate);
      }
    }

    private void SetTarget(IInteractable newTarget)
    {
      if (currentTarget != null && holdTimer > 0f)
      {
        holdTimer = 0f;
        OnInteractProgressChanged?.Invoke(currentTarget, 0f);
      }

      currentTarget = newTarget;
      holdTimer = 0f;
      OnInteractFocusChanged?.Invoke(currentTarget);
    }

    private void ClearTarget()
    {
      if (currentTarget != null)
      {
        SetTarget(null);
      }
    }

    private void HandleInput()
    {
      if (currentTarget == null) return;

      var keyboard = Keyboard.current;
      if (keyboard == null) return;

      if (!currentTarget.RequiresHold)
      {
        // Hành vi TAP (Bấm 1 cái ăn ngay)
        if (keyboard.fKey.wasPressedThisFrame)
        {
          ExecuteInteraction();
        }
      }
      else
      {
        // Hành vi HOLD (Đè giữ phím)
        if (keyboard.fKey.isPressed)
        {
          holdTimer += Time.deltaTime;
          float duration = Mathf.Max(0.01f, currentTarget.HoldDuration);
          float progress = Mathf.Clamp01(holdTimer / duration);
          OnInteractProgressChanged?.Invoke(currentTarget, progress);

          if (holdTimer >= duration)
          {
            holdTimer = 0f;
            OnInteractProgressChanged?.Invoke(currentTarget, 0f);
            ExecuteInteraction();
          }
        }
        else
        {
          if (holdTimer > 0f)
          {
            holdTimer = 0f;
            OnInteractProgressChanged?.Invoke(currentTarget, 0f);
          }
        }
      }
    }

    private void ExecuteInteraction()
    {
      if (currentTarget == null) return;

      var targetToExecute = currentTarget;
      targetToExecute.OnInteract(gameObject);

      // Kiểm tra xem đối tượng có còn tương tác được nữa không (ví dụ rương đã mở thì mất target)
      if (!targetToExecute.CanInteract(gameObject))
      {
        ClearTarget();
      }
    }

    private void OnDisable()
    {
      ClearTarget();
    }
  }
}
