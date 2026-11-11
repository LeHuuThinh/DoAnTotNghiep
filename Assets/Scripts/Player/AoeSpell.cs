using System.Collections;
using UnityEngine;

public class AoeSpell : MonoBehaviour
{
  [Header("AOE Damage Settings")]
  [SerializeField] private float radius = 5.5f;          // Bán kính vùng nổ
  [SerializeField] private float damage = 80f;           // Sát thương
  [SerializeField] private float knockback = 2.5f;        // Lực hất văng
  [SerializeField] private float explodeDelay = 0.5f;     // Thời gian tụ khí trước khi phát nổ
  [SerializeField] private LayerMask targetLayers;       // BẮT BUỘC CHỌN "Enemy" TRÊN INSPECTOR

  [Header("Hitlag Settings")]
  [Tooltip("Thời gian khựng đơ game (giây) khi vụ nổ chấn địa AOE trúng quái vật")]
  [SerializeField] private float hitlagDuration = 0.20f;
  [Range(0.01f, 0.2f)]
  [SerializeField] private float hitlagTimeScale = 0.05f;

  [Header("Timing")]
  [SerializeField] private float totalLifeTime = 3.0f;

  private Transform attacker;
  private float bonusDamage = 0f;
  private float critRate = 0f;
  private float critDamageMultiplier = 1.5f;
  private float lifestealPercent = 0f;

  public void Initialize(Transform attackerRoot, float additionalBonusDamage = 0f, float critRate = 0f, float critDamageMultiplier = 1.5f, float lifesteal = 0f)
  {
    attacker = attackerRoot;
    bonusDamage = additionalBonusDamage;
    this.critRate = critRate;
    this.critDamageMultiplier = critDamageMultiplier > 0f ? critDamageMultiplier : 1.5f;
    this.lifestealPercent = lifesteal;

    // Kích hoạt tất cả Particle System con của cụm AOE
    ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();
    foreach (ParticleSystem p in particles)
    {
      p.Play();
    }

    StartCoroutine(ExplodeRoutine());
    Destroy(gameObject, totalLifeTime);
  }

  private IEnumerator ExplodeRoutine()
  {
    yield return new WaitForSeconds(explodeDelay);

    // Quét sát thương hình cầu quanh tâm
    Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius, targetLayers);

    bool hitAny = false;
    foreach (Collider col in hitColliders)
    {
      if (attacker != null && col.transform.root == attacker) continue;

      IDamageable damageable = col.GetComponentInParent<IDamageable>();
      if (damageable != null)
      {
        hitAny = true;
        Vector3 hitPoint = col.ClosestPoint(transform.position);
        Vector3 knockbackDir = (col.transform.position - transform.position).normalized;
        knockbackDir.y = 0.25f; // Hất quái hơi văng bổng lên

        bool isCrit = critRate > 0f && UnityEngine.Random.Range(0f, 100f) < critRate;
        float rawDamage = Mathf.Max(1f, damage + bonusDamage);
        float finalDamage = isCrit ? (rawDamage * critDamageMultiplier) : rawDamage;
        damageable.TakeDamage(finalDamage, hitPoint, knockbackDir, knockback, attacker, InfiniteLabyrinth.UI.Combat.DamageNumberType.SkillQ, isCrit);

        if (isCrit && attacker != null)
        {
          if (InfiniteLabyrinth.Inventory.InventoryManager.HasInstance &&
              InfiniteLabyrinth.Inventory.InventoryManager.Instance.HasEnchantment(InfiniteLabyrinth.Inventory.EnchantmentType.BleedingCrit))
          {
            Health targetHealth = damageable as Health ?? (damageable as Component)?.GetComponentInParent<Health>();
            if (targetHealth != null && !targetHealth.IsDead)
            {
              targetHealth.ApplyBleed(finalDamage * 0.3f, 3f, 3, attacker);
            }
          }

          if (InfiniteLabyrinth.Inventory.InventoryManager.HasInstance &&
              InfiniteLabyrinth.Inventory.InventoryManager.Instance.HasEnchantment(InfiniteLabyrinth.Inventory.EnchantmentType.CritFrenzy))
          {
            var pcCombat = attacker.GetComponentInChildren<PlayerCombat>() ?? attacker.GetComponent<PlayerCombat>();
            if (pcCombat != null) pcCombat.TriggerAttackSpeedBoost(15f, 3.5f);
          }
        }

        if (lifestealPercent > 0f && attacker != null)
        {
          Health attackerHealth = attacker.GetComponent<Health>();
          if (attackerHealth != null && !attackerHealth.IsDead)
          {
            float healAmount = Mathf.Min(finalDamage * (lifestealPercent / 100f), 25f);
            if (healAmount > 0.5f) attackerHealth.Heal(healAmount);
          }
        }
      }
    }

    if (hitAny)
    {
      TriggerPlayerHitlag();
    }
  }

  // Vẽ bán kính nổ trong Scene view để bạn căn chỉnh độ rộng dễ dàng
  private void OnDrawGizmosSelected()
  {
    Gizmos.color = new Color(1f, 0f, 0.5f, 0.35f);
    Gizmos.DrawSphere(transform.position, radius);
    Gizmos.color = Color.magenta;
    Gizmos.DrawWireSphere(transform.position, radius);
  }

  private void TriggerPlayerHitlag()
  {
    if (hitlagDuration <= 0f) return;
    if (attacker != null)
    {
      PlayerFeedback feedback = attacker.GetComponent<PlayerFeedback>() ?? attacker.GetComponentInChildren<PlayerFeedback>();
      if (feedback != null)
      {
        feedback.TriggerHitlag(hitlagDuration, hitlagTimeScale);
        return;
      }
    }
    PlayerFeedback anyFeedback = FindAnyObjectByType<PlayerFeedback>();
    anyFeedback?.TriggerHitlag(hitlagDuration, hitlagTimeScale);
  }
}
