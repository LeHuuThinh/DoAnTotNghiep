using UnityEngine;

public class RHandAdjust : MonoBehaviour
{
  [Header("Bone References")]
  public Transform rightShoulderBone; // Kéo xương bắp tay phải (Thường tên là RightArm hoặc RightUpperArm)
  public Transform rightHandBone;     // Kéo xương cổ tay phải (RightHand)

  [Header("Offsets")]
  public Vector3 shoulderOffset;      // Chỉnh góc cánh tay (dang rộng/hẹp, đưa ra trước/sau)
  public Vector3 wristOffset;         // Chỉnh góc bàn tay và thanh kiếm

  void LateUpdate()
  {
    // 1. Ghi đè góc vai / cánh tay
    if (rightShoulderBone != null)
    {
      rightShoulderBone.localRotation *= Quaternion.Euler(shoulderOffset);
    }

    // 2. Ghi đè góc cổ tay
    if (rightHandBone != null)
    {
      rightHandBone.localRotation *= Quaternion.Euler(wristOffset);
    }
  }
}