using UnityEngine;

public class SimpleSpringBone : MonoBehaviour
{
    [Header("Cài đặt đung đưa")]
    [Tooltip("Đốt xương con liền kề phía dưới (nếu để trống script sẽ tự tìm đốt con đầu tiên)")]
    public Transform childBone;

    [Range(0.01f, 1f)]
    [Tooltip("Độ cứng: Càng cao càng nhanh trở về dáng gốc")]
    public float stiffness = 0.1f;

    [Range(0.01f, 1f)]
    [Tooltip("Độ cản: Càng cao càng triệt tiêu rung lắc nhanh")]
    public float damping = 0.75f;

    [Tooltip("Trọng lực kéo tà áo/đuôi tóc rủ xuống")]
    public Vector3 gravity = new Vector3(0, -0.05f, 0);

    private Vector3 currentVelocity;
    private Vector3 dynamicPos;

    void Start()
    {
        // Tự động gán đốt xương con đầu tiên nếu chưa gán thủ công
        if (childBone == null && transform.childCount > 0)
        {
            childBone = transform.GetChild(0);
        }

        if (childBone != null)
        {
            dynamicPos = childBone.position;
        }
    }

    void LateUpdate()
    {
        if (childBone == null) return;

        // Tính vị trí mục tiêu theo chuyển động gốc của animation
        Vector3 targetPos = transform.position + transform.rotation * (childBone.localPosition);

        // Tính lực quán tính, lực đàn hồi lò xo và trọng lực
        Vector3 force = (targetPos - dynamicPos) * stiffness + gravity;
        currentVelocity = (currentVelocity + force) * damping;
        dynamicPos += currentVelocity;

        // Giữ cố định khoảng cách giữa 2 đốt xương (tránh làm giãn lưới)
        dynamicPos = transform.position + (dynamicPos - transform.position).normalized * Vector3.Distance(transform.position, targetPos);

        // Xoay khớp xương hướng về vị trí quán tính
        transform.rotation = Quaternion.FromToRotation(transform.rotation * (childBone.localPosition).normalized, dynamicPos - transform.position) * transform.rotation;
    }
}