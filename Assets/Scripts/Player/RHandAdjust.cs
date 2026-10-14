using UnityEngine;

public class RHandAdjust : MonoBehaviour
{
  [Header("Bone References")]
  public Transform rightShoulderBone; // Xương bắp tay phải (RightArm / RightUpperArm)
  public Transform rightHandBone;     // Xương cổ tay phải (RightHand)

  [Header("References")]
  [SerializeField] private Animator animator;
  [SerializeField] private TargetLockSystem lockSystem;

  [Header("Layer Sync Settings")]
  [Tooltip("Chỉ số Index của Layer tay phải trong Animator (thường là 1)")]
  [SerializeField] private int swordLayerIndex = 1;
  [Tooltip("Tốc độ chuyển đổi trọng số khi vào/thoát đòn đánh hoặc Block")]
  [SerializeField] private float weightFadeSpeed = 10f;

  [Header("Normal Offsets (Đi dạo / Không khóa)")]
  public Vector3 normalShoulderOffset;
  public Vector3 normalWristOffset;

  [Header("Combat Offsets (Lock-on / Thế thủ)")]
  public Vector3 combatShoulderOffset;
  public Vector3 combatWristOffset;

  [Header("Transition Settings")]
  [SerializeField] private float transitionSpeed = 8f;

  private Vector3 currentShoulderOffset;
  private Vector3 currentWristOffset;
  private float currentWeight = 1f;

  private void Awake()
  {
    if (lockSystem == null)
    {
      lockSystem = GetComponentInParent<TargetLockSystem>();
    }

    if (animator == null)
    {
      animator = GetComponent<Animator>();
      if (animator == null) animator = GetComponentInParent<Animator>();
      if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    currentShoulderOffset = normalShoulderOffset;
    currentWristOffset = normalWristOffset;
    currentWeight = 1f;
  }

  private void LateUpdate()
  {
    if (rightShoulderBone == null && rightHandBone == null) return;

    // 1. ĐỒNG BỘ TRỌNG SỐ THEO LAYER TAY PHẢI TRONG ANIMATOR
    float targetWeight = 1f;
    if (animator != null && swordLayerIndex < animator.layerCount)
    {
      // Đọc trọng số thực tế của Layer tay phải (0 khi Attack/Block, 1 khi Locomotion)
      targetWeight = animator.GetLayerWeight(swordLayerIndex);
    }

    currentWeight = Mathf.MoveTowards(currentWeight, targetWeight, weightFadeSpeed * Time.deltaTime);

    // Nếu trọng số = 0 (đang trong đòn Attack hoặc Block hoàn toàn), không can thiệp vào xương
    if (currentWeight <= 0.001f) return;

    // 2. TÍNH TOÁN GÓC BÙ TRỪ GIỮA THƯỜNG VÀ COMBAT
    bool isCombat = lockSystem != null && lockSystem.IsLockedOn;
    Vector3 targetShoulder = isCombat ? combatShoulderOffset : normalShoulderOffset;
    Vector3 targetWrist = isCombat ? combatWristOffset : normalWristOffset;

    currentShoulderOffset = Vector3.Lerp(currentShoulderOffset, targetShoulder, transitionSpeed * Time.deltaTime);
    currentWristOffset = Vector3.Lerp(currentWristOffset, targetWrist, transitionSpeed * Time.deltaTime);

    // 3. HÒA TRỘN GÓC XOAY THEO TRỌNG SỐ (Slerp từ Identity về góc bù trừ)
    // Khi currentWeight = 0 -> Slerp trả về Quaternion.identity (không làm lệch xương chút nào)
    Quaternion shoulderAdjustment = Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(currentShoulderOffset), currentWeight);
    Quaternion wristAdjustment = Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(currentWristOffset), currentWeight);

    // 4. ÁP DỤNG LÊN XƯƠNG
    if (rightShoulderBone != null)
    {
      rightShoulderBone.localRotation *= shoulderAdjustment;
    }

    if (rightHandBone != null)
    {
      rightHandBone.localRotation *= wristAdjustment;
    }
  }
}