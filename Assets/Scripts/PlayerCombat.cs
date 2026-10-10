using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
  [Header("References")]
  [SerializeField] private Animator animator;
  [SerializeField] private PlayerController playerController;
  [SerializeField] private CharacterController characterController;

  [Header("Layer Settings")]
  [SerializeField] private int swordLayerIndex = 1;
  [SerializeField] private float layerFadeSpeed = 6f;

  [Header("Combo Settings")]
  [SerializeField] private float maxFailsafeTimeout = 3.0f;

  [Header("Charge / Jump Attack Settings")]
  [Tooltip("Thời gian giữ chuột tối thiểu (giây) để kích hoạt Jump Attack")]
  [SerializeField] private float chargeThreshold = 0.35f;
  [Tooltip("Quãng đường chạy lấy đà trên mặt đất trước khi bật nhảy (mét)")]
  [SerializeField] private float runUpDistance = 2.5f;
  [Tooltip("Quãng đường nhân vật phóng về phía trước (mét)")]
  [SerializeField] private float leapDistance = 4.5f;
  [Tooltip("Độ cao tối đa của cú nhảy nhấc bổng lên (mét)")]
  [SerializeField] private float leapHeight = 1.2f; // <-- BẠN TỰ CHỈNH ĐỘ CAO Ở ĐÂY
  [Tooltip("Thời gian bay trên không (giây)")]
  [SerializeField] private float leapDuration = 0.45f;
  [Tooltip("Thời gian chờ nhún chân lấy đà trước khi thực sự phóng đi (giây)")]
  [SerializeField] private float leapDelay = 0.15f;

  private int comboStep = 0;
  private bool isAttacking = false;
  private bool canCombo = false;
  private bool comboBuffered = false;
  private float attackTimer = 0f;
  private float targetLayerWeight = 1f;

  // Biến đo thời gian giữ chuột
  private float holdTimer = 0f;
  private bool isHoldingMouse = false;
  private Coroutine leapCoroutine;

  private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
  private static readonly int ComboStepHash = Animator.StringToHash("ComboStep");
  private static readonly int JumpAttackTriggerHash = Animator.StringToHash("JumpAttack");

  private void Awake()
  {
    if (animator == null) animator = GetComponent<Animator>();
    if (playerController == null) playerController = GetComponentInParent<PlayerController>();
    if (characterController == null) characterController = GetComponentInParent<CharacterController>();

    targetLayerWeight = 1f;
  }

  private void Update()
  {
    HandleCombatInput();

    // Failsafe chống kẹt animation quá thời gian quy định
    if (isAttacking)
    {
      attackTimer += Time.deltaTime;
      if (attackTimer >= maxFailsafeTimeout)
      {
        ForceResetCombat();
      }
    }

    // Hòa trộn mượt mà trọng số của Layer tay phải
    if (animator != null && swordLayerIndex < animator.layerCount)
    {
      float currentWeight = animator.GetLayerWeight(swordLayerIndex);
      if (!Mathf.Approximately(currentWeight, targetLayerWeight))
      {
        float newWeight = Mathf.MoveTowards(currentWeight, targetLayerWeight, layerFadeSpeed * Time.deltaTime);
        animator.SetLayerWeight(swordLayerIndex, newWeight);
      }
    }
  }

  private void HandleCombatInput()
  {
    if (Mouse.current == null || Cursor.lockState != CursorLockMode.Locked) return;

    // 1. NHẤN CHUỘT XUỐNG
    if (Mouse.current.leftButton.wasPressedThisFrame)
    {
      if (!isAttacking)
      {
        isHoldingMouse = true;
        holdTimer = 0f;
      }
      else
      {
        // Đang trong đòn đánh khác -> Lưu đệm để nối combo thường
        if (comboStep > 0 && comboStep < 3)
        {
          comboBuffered = true;
          if (canCombo)
          {
            AdvanceCombo();
          }
        }
      }
    }

    // 2. ĐANG GIỮ CHUỘT: TÍNH GIỜ VÀ TỰ ĐỘNG KÍCH HOẠT KHI ĐỦ THỜI GIAN
    if (isHoldingMouse && Mouse.current.leftButton.isPressed)
    {
      holdTimer += Time.deltaTime;

      // ĐỦ 0.35s LÀ TỰ BẬT ĐÒN JUMP ATTACK NGAY LẬP TỨC (KHÔNG CẦN CHỜ NHẢ CHUỘT)
      if (holdTimer >= chargeThreshold)
      {
        isHoldingMouse = false; // Tắt cờ để nhả chuột ra không bị chém thường
        ExecuteJumpAttack();
      }
    }

    // 3. NHẢ CHUỘT RA (CHỈ DÀNH CHO NHẤP NHANH < 0.35s)
    if (isHoldingMouse && Mouse.current.leftButton.wasReleasedThisFrame)
    {
      isHoldingMouse = false;
      comboStep = 1;
      ExecuteNormalAttack();
    }
  }

  // =========================================================================
  // XỬ LÝ CHÉM THƯỜNG COMBO
  // =========================================================================

  private void ExecuteNormalAttack()
  {
    isAttacking = true;
    canCombo = false;
    comboBuffered = false;
    attackTimer = 0f;

    targetLayerWeight = 0f;
    if (animator != null) animator.SetLayerWeight(swordLayerIndex, 0f);

    if (playerController != null) playerController.LockMovement();

    animator.SetInteger(ComboStepHash, comboStep);
    animator.SetTrigger(AttackTriggerHash);
  }

  private void AdvanceCombo()
  {
    canCombo = false;
    comboBuffered = false;
    comboStep++;
    ExecuteNormalAttack();
  }

  // =========================================================================
  // XỬ LÝ ĐÒN CHARGE / JUMP ATTACK (CÓ ĐỘ CAO THEO ĐƯỜNG CUNG)
  // =========================================================================

  private void ExecuteJumpAttack()
  {
    isAttacking = true;
    comboStep = 0;
    canCombo = false;
    comboBuffered = false;
    attackTimer = 0f;

    targetLayerWeight = 0f;
    if (animator != null) animator.SetLayerWeight(swordLayerIndex, 0f);

    if (playerController != null) playerController.LockMovement();

    animator.SetTrigger(JumpAttackTriggerHash);

    if (leapCoroutine != null) StopCoroutine(leapCoroutine);
    leapCoroutine = StartCoroutine(PerformLeapRoutine());
  }

  private IEnumerator PerformLeapRoutine()
  {
    // Lấy hướng nhìn hiện tại của nhân vật
    Vector3 direction = playerController != null ? playerController.transform.forward : transform.forward;
    direction.y = 0f;
    direction.Normalize();

    // =========================================================================
    // GIAI ĐOẠN 1: CHẠY LẤY ĐÀ SÁT MẶT ĐẤT (0.4s)
    // =========================================================================
    if (leapDelay > 0f)
    {
      float runTimer = 0f;
      float runSpeed = runUpDistance / leapDelay;

      while (runTimer < leapDelay)
      {
        runTimer += Time.deltaTime;

        if (characterController != null)
        {
          // Thêm lực đè nhẹ trục Y (-2f) để nhân vật bám sát mặt sàn khi chạy
          Vector3 runDelta = (direction * runSpeed * Time.deltaTime) + (Vector3.down * 2f * Time.deltaTime);
          characterController.Move(runDelta);
        }

        yield return null;
      }
    }

    // =========================================================================
    // GIAI ĐOẠN 2: BẬT NHẢY BỔ KIẾM TRÊN KHÔNG (1.0s)
    // =========================================================================
    float leapTimer = 0f;
    float leapSpeed = leapDistance / leapDuration;
    float lastArcY = 0f;

    while (leapTimer < leapDuration)
    {
      leapTimer += Time.deltaTime;
      float progress = Mathf.Clamp01(leapTimer / leapDuration);

      // Quỹ đạo Parabol: Bay bổng lên đỉnh leapHeight rồi hạ cánh
      float currentArcY = 4f * leapHeight * progress * (1f - progress);
      float deltaY = currentArcY - lastArcY;
      lastArcY = currentArcY;

      if (characterController != null)
      {
        Vector3 leapDelta = (direction * leapSpeed * Time.deltaTime) + (Vector3.up * deltaY);
        characterController.Move(leapDelta);
      }

      yield return null;
    }

    leapCoroutine = null;
  }

  // =========================================================================
  // ANIMATION EVENTS
  // =========================================================================

  public void EnableComboWindow()
  {
    canCombo = true;
    if (comboBuffered && comboStep < 3)
    {
      AdvanceCombo();
    }
  }

  public void DisableComboWindow()
  {
    canCombo = false;
  }

  public void OnAttackEnd()
  {
    ForceResetCombat();
  }

  public void ForceResetCombat()
  {
    if (leapCoroutine != null)
    {
      StopCoroutine(leapCoroutine);
      leapCoroutine = null;
    }

    comboStep = 0;
    isAttacking = false;
    canCombo = false;
    comboBuffered = false;
    isHoldingMouse = false;
    holdTimer = 0f;
    attackTimer = 0f;

    targetLayerWeight = 1f;

    if (animator != null)
      animator.SetInteger(ComboStepHash, 0);

    if (playerController != null)
      playerController.UnlockMovement();
  }
}