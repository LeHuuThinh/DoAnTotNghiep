using UnityEngine;

namespace InfiniteLabyrinth.Inventory
{
  /// <summary>
  /// Gắn lên các vật thể có thể phá hủy (Thùng gỗ, hòm vỡ, bình gốm)
  /// Nhận sát thương trực tiếp từ WeaponHitbox của người chơi qua IDamageable
  /// </summary>
  public class BreakableProp : MonoBehaviour, IDamageable
  {
    [Header("=== ĐỘ BỀN ===")]
    [Tooltip("Vỡ ngay sau 1 nhát chém")]
    [SerializeField] private bool breakInOneHit = true;
    [Tooltip("Máu của vật thể nếu không vỡ ngay")]
    [SerializeField] private float maxHealth = 15f;

    [Header("=== HIỆU ỨNG KHI VỠ ===")]
    [Tooltip("Prefab hiệu ứng vụn gỗ vỡ / khói bụi")]
    [SerializeField] private GameObject breakVfxPrefab;
    [Tooltip("Âm thanh vỡ")]
    [SerializeField] private AudioClip breakSound;

    private float currentHealth;
    private bool isBroken = false;
    private LootDropper lootDropper;
    private Collider propCollider;

    private void Awake()
    {
      currentHealth = maxHealth;
      lootDropper = GetComponent<LootDropper>();
      propCollider = GetComponent<Collider>();
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection, float knockbackDistance = 0f, Transform attacker = null)
    {
      if (isBroken) return;

      if (breakInOneHit)
      {
        Break();
        return;
      }

      currentHealth -= damage;
      if (currentHealth <= 0f)
      {
        Break();
      }
    }

    private void Break()
    {
      if (isBroken) return;
      isBroken = true;

      // 1. Tắt va chạm ngay lập tức
      if (propCollider != null)
      {
        propCollider.enabled = false;
      }

      // 2. Kích hoạt rơi chiến lợi phẩm
      if (lootDropper != null)
      {
        lootDropper.DropLoot();
      }

      // 3. Sinh hiệu ứng khói bụi / vụn gỗ
      if (breakVfxPrefab != null)
      {
        Instantiate(breakVfxPrefab, transform.position + Vector3.up * 0.4f, Quaternion.identity);
      }

      // 4. Phát âm thanh
      if (breakSound != null)
      {
        AudioSource.PlayClipAtPoint(breakSound, transform.position);
      }

      // 5. Tự hủy vật thể
      Destroy(gameObject, 0.05f);
    }
  }
}
