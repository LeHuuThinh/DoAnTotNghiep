using UnityEngine;

namespace InfiniteLabyrinth.Progression
{
  /// <summary>
  /// Hệ thống Điều Phối Thang Đo Độ Khó & Kinh Nghiệm Theo Tầng Hầm Ngục (Dungeon Floor Difficulty Scaler):
  /// - Tự động đồng bộ với GameManager.Instance.CurrentFloor (Tầng 1, 2, 3...).
  /// - Công thức EXP Người chơi: Base 150 EXP, Exponent 1.07 (tăng trưởng gần như tuyến tính ~190 EXP/cấp).
  /// - Quái thường:
  ///   + Máu (HP): Tăng +28% mỗi tầng (đáp ứng đòn đánh khi Player nâng đồ +1 đến +5).
  ///   + Sát thương (ATK): Tăng +12% mỗi tầng (tăng thận trọng, giữ chuẩn Souls-like, chống one-shot).
  ///   + Giáp (DEF): Tăng +2 DEF mỗi tầng.
  ///   + EXP thưởng: Tăng tuyến tính phẳng +2 EXP mỗi tầng (+0 ở tầng 1, +2 ở tầng 2, +4 ở tầng 3...).
  /// - Quái Tinh Nhuệ (Elite Monsters):
  ///   + Xuất hiện từ Tầng 2 trở đi (tỷ lệ 12% - 25%).
  ///   + Chỉ số: HP x1.5, ATK x1.25, EXP x2.0, Scale x1.15, Viền máu hoàng kim và hào quang sáng chân.
  /// - Trùm Hầm Ngục (Boss Ice Golem & Red Ignis):
  ///   + Máu Boss: Tăng +32% mỗi tầng.
  ///   + Sát thương Boss: Tăng +14% mỗi tầng.
  ///   + Giáp Boss: Tăng +3 DEF mỗi tầng.
  ///   + EXP Thưởng Boss: Tầng 1: 350 EXP; Mỗi tầng tiếp theo +100 EXP (Tầng 2: 450, Tầng 3: 550, Tầng 5: 750, Tầng 10: 1250).
  /// </summary>
  public static class DungeonDifficultyScaler
  {
    // =========================================================================
    // HỆ SỐ SCALE QUÁI THƯỜNG
    // =========================================================================

    public static float GetMonsterHealthMultiplier(int floor)
    {
      if (floor <= 1) return 1.0f;
      return 1.0f + 0.28f * (floor - 1);
    }

    public static float GetMonsterDamageMultiplier(int floor)
    {
      if (floor <= 1) return 1.0f;
      return 1.0f + 0.12f * (floor - 1);
    }

    public static float GetMonsterDefenseBonus(int floor)
    {
      if (floor <= 1) return 0f;
      return 2.0f * (floor - 1);
    }

    public static int GetMonsterExpBonus(int floor)
    {
      if (floor <= 1) return 0;
      return 2 * (floor - 1);
    }

    // =========================================================================
    // HỆ SỐ SCALE BOSS
    // =========================================================================

    public const int BaseBossExpReward = 350;
    public const int BossExpBonusPerFloor = 100;

    public static float GetBossHealthMultiplier(int floor)
    {
      if (floor <= 1) return 1.0f;
      return 1.0f + 0.32f * (floor - 1);
    }

    public static float GetBossDamageMultiplier(int floor)
    {
      if (floor <= 1) return 1.0f;
      return 1.0f + 0.14f * (floor - 1);
    }

    public static float GetBossDefenseBonus(int floor)
    {
      if (floor <= 1) return 0f;
      return 3.0f * (floor - 1);
    }

    public static int GetBossExpReward(int floor)
    {
      if (floor <= 1) return BaseBossExpReward;
      return BaseBossExpReward + BossExpBonusPerFloor * (floor - 1);
    }

    // =========================================================================
    // CƠ CHẾ QUÁI TINH NHUỆ (ELITE / CHAMPION)
    // =========================================================================

    public const float EliteHpMultiplier = 1.5f;
    public const float EliteDamageMultiplier = 1.25f;
    public const float EliteExpMultiplier = 2.0f;
    public const float EliteScaleMultiplier = 1.3f;

    /// <summary>
    /// Tỷ lệ quái thường trở thành Tinh Nhuệ (Elite) theo tầng
    /// </summary>
    public static float GetEliteChance(int floor)
    {
      if (floor < 2) return 0f;
      // Tầng 2: 12%, Tầng 3: 15%, Tầng 4: 18%, Tầng 5: 21%, cap tối đa 25%
      return Mathf.Clamp(0.12f + (floor - 2) * 0.03f, 0.12f, 0.25f);
    }

    // =========================================================================
    // ÁP DỤNG THANG ĐO LÊN THỰC THỂ
    // =========================================================================

    /// <summary>
    /// Áp dụng scale chỉ số toàn diện cho Quái thường
    /// </summary>
    public static void ApplyMonsterScaling(EnemyAIBase enemy, int floor)
    {
      if (enemy == null) return;
      floor = Mathf.Max(1, floor);

      float hpMult = GetMonsterHealthMultiplier(floor);
      float dmgMult = GetMonsterDamageMultiplier(floor);
      float defBonus = GetMonsterDefenseBonus(floor);
      int expBonus = GetMonsterExpBonus(floor);

      bool isElite = false;
      if (floor >= 2 && Random.value < GetEliteChance(floor))
      {
        isElite = true;
        hpMult *= EliteHpMultiplier;
        dmgMult *= EliteDamageMultiplier;
        expBonus = Mathf.RoundToInt((enemy.ExpReward + expBonus) * EliteExpMultiplier) - enemy.ExpReward;
      }

      enemy.ApplyScaledStats(hpMult, dmgMult, defBonus, expBonus, isElite);
    }
  }
}
