using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using InfiniteLabyrinth.Inventory;
using InfiniteLabyrinth.Player;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Quản lý Thanh Buff HUD (EffectBar) hiển thị các hiệu ứng trạng thái chiến đấu của Player:
  /// - SpeedEffect (Phong Hành Bộ - SprintWind): Đếm ngược thời gian tăng 20% tốc độ chạy sau khi Roll (2.5s).
  /// - AttackEffect (Cuồng Trảm - CritFrenzy): Đếm ngược thời gian tăng 15% tốc độ đánh sau khi Crit (3.5s).
  /// - DefEffect (Da Sắt - IronSkin): Đếm ngược thời gian tăng +15 DEF từ Thuốc Da Sắt (30s).
  /// - LifeStealEffect (Khát Máu Cuồng Nộ - LowHealthBloodthirst): Kích hoạt khi trang bị phù phép và máu <= 50% HP (Heartbeat Pulse).
  /// Sử dụng HorizontalLayoutGroup để các icon tự động dồn hàng sang trái khi bật/tắt linh hoạt.
  /// </summary>
  public class PlayerBuffHUD : MonoBehaviour
  {
    [Header("=== SPEED EFFECT (SPRINT WIND) ===")]
    [SerializeField] private GameObject speedEffectObj;
    [SerializeField] private Image speedFillImage;

    [Header("=== ATTACK EFFECT (CRIT FRENZY) ===")]
    [SerializeField] private GameObject attackEffectObj;
    [SerializeField] private Image attackFillImage;

    [Header("=== DEF EFFECT (IRON SKIN) ===")]
    [SerializeField] private GameObject defEffectObj;
    [SerializeField] private Image defFillImage;

    [Header("=== LIFESTEAL EFFECT (LOW HEALTH BLOODTHIRST) ===")]
    [SerializeField] private GameObject lifeStealEffectObj;
    [SerializeField] private Image lifeStealFillImage;

    [Header("=== TARGET PLAYER REFERENCES ===")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private Health playerHealth;

    private Vector3 lifeStealInitialScale = Vector3.one;

    private void Awake()
    {
      AutoFindUIComponents();
      ResetAllEffects();
    }

    private void Start()
    {
      FindPlayerReferences();
    }

    private void OnEnable()
    {
      ResetAllEffects();
    }

    private void Update()
    {
      if (playerController == null || playerCombat == null || playerHealth == null)
      {
        FindPlayerReferences();
        if (playerController == null) return;
      }

      UpdateSpeedBuff();
      UpdateAttackBuff();
      UpdateDefBuff();
      UpdateLifeStealBuff();
    }

    // =========================================================================
    // CẬP NHẬT TỪNG BUFF
    // =========================================================================

    private void UpdateSpeedBuff()
    {
      if (speedEffectObj == null) return;

      if (playerController != null && playerController.HasTemporarySpeedBoost)
      {
        if (!speedEffectObj.activeSelf) speedEffectObj.SetActive(true);
        if (speedFillImage != null)
        {
          float maxDur = Mathf.Max(0.1f, playerController.TemporarySpeedBonusDuration);
          speedFillImage.fillAmount = Mathf.Clamp01(playerController.TemporarySpeedBonusTimer / maxDur);
        }
      }
      else
      {
        if (speedEffectObj.activeSelf) speedEffectObj.SetActive(false);
      }
    }

    private void UpdateAttackBuff()
    {
      if (attackEffectObj == null) return;

      if (playerCombat != null && playerCombat.IsAttackSpeedBoosted)
      {
        if (!attackEffectObj.activeSelf) attackEffectObj.SetActive(true);
        if (attackFillImage != null)
        {
          float maxDur = Mathf.Max(0.1f, playerCombat.AttackSpeedBonusDuration);
          attackFillImage.fillAmount = Mathf.Clamp01(playerCombat.AttackSpeedBonusTimer / maxDur);
        }
      }
      else
      {
        if (attackEffectObj.activeSelf) attackEffectObj.SetActive(false);
      }
    }

    private void UpdateDefBuff()
    {
      if (defEffectObj == null) return;

      if (playerHealth != null && playerHealth.HasTemporaryDefenseBoost)
      {
        if (!defEffectObj.activeSelf) defEffectObj.SetActive(true);
        if (defFillImage != null)
        {
          float maxDur = Mathf.Max(0.1f, playerHealth.TemporaryDefenseDuration);
          defFillImage.fillAmount = Mathf.Clamp01(playerHealth.TemporaryDefenseTimer / maxDur);
        }
      }
      else
      {
        if (defEffectObj.activeSelf) defEffectObj.SetActive(false);
      }
    }

    private void UpdateLifeStealBuff()
    {
      if (lifeStealEffectObj == null) return;

      bool hasEnchant = InventoryManager.HasInstance && 
                        InventoryManager.Instance.HasEnchantment(EnchantmentType.LowHealthBloodthirst);

      bool isLowHealth = playerHealth != null && 
                         !playerHealth.IsDead && 
                         (playerHealth.CurrentHealth / Mathf.Max(1f, playerHealth.MaxHealth) <= 0.5f);

      if (hasEnchant && isLowHealth)
      {
        if (!lifeStealEffectObj.activeSelf)
        {
          lifeStealEffectObj.SetActive(true);
          if (lifeStealFillImage != null) lifeStealFillImage.fillAmount = 1f;
        }

        // Hiệu ứng nhịp đập tim báo hiệu nguy cấp (Heartbeat pulse scale: 1.0 -> 1.18 -> 1.0)
        float pulse = 1.0f + 0.18f * Mathf.Abs(Mathf.Sin(Time.time * 5.0f));
        lifeStealEffectObj.transform.localScale = lifeStealInitialScale * pulse;
      }
      else
      {
        if (lifeStealEffectObj.activeSelf)
        {
          lifeStealEffectObj.transform.localScale = lifeStealInitialScale;
          lifeStealEffectObj.SetActive(false);
        }
      }
    }

    // =========================================================================
    // HỖ TRỢ VÀ TỰ ĐỘNG DÒ TÌM COMPONENT
    // =========================================================================

    public void ResetAllEffects()
    {
      if (speedEffectObj != null) speedEffectObj.SetActive(false);
      if (attackEffectObj != null) attackEffectObj.SetActive(false);
      if (defEffectObj != null) defEffectObj.SetActive(false);
      if (lifeStealEffectObj != null)
      {
        lifeStealEffectObj.transform.localScale = lifeStealInitialScale;
        lifeStealEffectObj.SetActive(false);
      }
    }

    [ContextMenu("Auto Find UI Components")]
    public void AutoFindUIComponents()
    {
      Transform root = transform.Find("EffectBar") ?? transform.Find("BarsContainer/EffectBar") ?? transform;

      // 1. Tìm SpeedEffect
      if (speedEffectObj == null)
      {
        Transform t = root.Find("SpeedEffect") ?? FindDeepChild(transform, "SpeedEffect");
        if (t != null) speedEffectObj = t.gameObject;
      }
      if (speedEffectObj != null && speedFillImage == null)
      {
        Transform fill = speedEffectObj.transform.Find("Fill");
        if (fill != null) speedFillImage = fill.GetComponent<Image>();
      }

      // 2. Tìm AttackEffect
      if (attackEffectObj == null)
      {
        Transform t = root.Find("AttackEffect") ?? FindDeepChild(transform, "AttackEffect");
        if (t != null) attackEffectObj = t.gameObject;
      }
      if (attackEffectObj != null && attackFillImage == null)
      {
        Transform fill = attackEffectObj.transform.Find("Fill");
        if (fill != null) attackFillImage = fill.GetComponent<Image>();
      }

      // 3. Tìm DefEffect
      if (defEffectObj == null)
      {
        Transform t = root.Find("DefEffect") ?? FindDeepChild(transform, "DefEffect");
        if (t != null) defEffectObj = t.gameObject;
      }
      if (defEffectObj != null && defFillImage == null)
      {
        Transform fill = defEffectObj.transform.Find("Fill");
        if (fill != null) defFillImage = fill.GetComponent<Image>();
      }

      // 4. Tìm LifeStealEffect
      if (lifeStealEffectObj == null)
      {
        Transform t = root.Find("LifeStealEffect") ?? FindDeepChild(transform, "LifeStealEffect");
        if (t != null) lifeStealEffectObj = t.gameObject;
      }
      if (lifeStealEffectObj != null)
      {
        lifeStealInitialScale = lifeStealEffectObj.transform.localScale;
        if (lifeStealFillImage == null)
        {
          Transform fill = lifeStealEffectObj.transform.Find("Fill");
          if (fill != null) lifeStealFillImage = fill.GetComponent<Image>();
        }
      }
    }

    private Transform FindDeepChild(Transform parent, string childName)
    {
      foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
      {
        if (child.name == childName) return child;
      }
      return null;
    }

    public void FindPlayerReferences()
    {
      GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
      if (playerObj != null)
      {
        if (playerController == null) playerController = playerObj.GetComponent<PlayerController>();
        if (playerHealth == null) playerHealth = playerObj.GetComponent<Health>();
        if (playerCombat == null)
        {
          playerCombat = playerObj.GetComponentInChildren<PlayerCombat>() ?? playerObj.GetComponent<PlayerCombat>();
        }
      }
    }
  }
}
