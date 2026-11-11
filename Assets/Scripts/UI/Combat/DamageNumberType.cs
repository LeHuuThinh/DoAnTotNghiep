namespace InfiniteLabyrinth.UI.Combat
{
  /// <summary>
  /// Phân loại sát thương hiển thị trên hệ thống Floating Damage Numbers
  /// Quy định màu sắc, kích thước và nhịp búng số (Pop) theo chuẩn Game Feel ARPG.
  /// </summary>
  public enum DamageNumberType
  {
    /// <summary>
    /// Đòn chém thường (Combo 1, 2, 3) - Màu Trắng Bạc viền đen (#FFFFFF)
    /// </summary>
    Normal,

    /// <summary>
    /// Đòn đánh gồng lực (Charge Attack) - Màu Vàng Hổ Phách (#FFB300), Size to 1.25x
    /// </summary>
    Charge,

    /// <summary>
    /// Đòn phản công sau Parry (Counter Attack) - Màu Vàng Cam rực rỡ (#FFA000), Size to 1.35x
    /// </summary>
    Counter,

    /// <summary>
    /// Kỹ năng E (Moon Slash - Kiếm khí tầm xa) - Màu Xanh Lam Ngọc / Cyan Băng (#38D9FF), Size 1.30x
    /// </summary>
    SkillE,

    /// <summary>
    /// Kỹ năng Q (Ground AOE - Chấn địa bộc phát) - Màu Cam Lửa Rực Rỡ (#FF7A29), Size 1.35x
    /// </summary>
    SkillQ,

    /// <summary>
    /// Tuyệt kỹ R (Ultimate Colossal Slash) - Màu ĐỎ HUYẾT TINH / CRIMSON RED (#FF1E27), Size Cực Đại 1.65x
    /// </summary>
    SkillR,

    /// <summary>
    /// Người chơi bị dính sát thương (Player Damaged) - Màu Đỏ tươi máu (#FF3333) kèm dấu trừ: -X
    /// </summary>
    PlayerHit,

    /// <summary>
    /// Sát thương Chảy Máu định kỳ (Bleed DoT Damage) - Màu Đỏ Huyết Sẫm (#D32F2F / #B71C1C) kèm Icon Giọt Máu
    /// </summary>
    Bleed
  }
}
