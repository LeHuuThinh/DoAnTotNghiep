using UnityEngine;

/// <summary>
/// Tự động hủy GameObject sau một khoảng thời gian nhất định (sử dụng cho VFX, Projectile, hiệu ứng tạm thời)
/// để tránh rò rỉ bộ nhớ và không làm rác Scene Hierarchy.
/// </summary>
public class AutoDestroyVfx : MonoBehaviour
{
    [Tooltip("Thời gian tồn tại (giây) trước khi tự hủy")]
    [SerializeField] private float lifetime = 4.0f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
