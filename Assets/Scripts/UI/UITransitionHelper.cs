using System;
using UnityEngine;

namespace InfiniteLabyrinth.UI
{
  /// <summary>
  /// Tiện ích tính toán nội suy và đường cong chuyển động UI phong cách Souls-like / Action RPG.
  /// Thuần toán học, 0 GC allocation, chạy bằng Time.unscaledDeltaTime để mượt mà cả khi pause game.
  /// </summary>
  public static class UITransitionHelper
  {
    /// <summary>
    /// Đường cong giảm tốc nhanh (Cubic Ease Out) - cảm giác dứt khoát, chắc chắn.
    /// </summary>
    public static float EaseOutCubic(float t)
    {
      t = Mathf.Clamp01(t);
      float f = 1f - t;
      return 1f - f * f * f;
    }

    /// <summary>
    /// Đường cong giảm tốc nhẹ nhàng (Quad Ease Out) - thích hợp cho trượt ngắn.
    /// </summary>
    public static float EaseOutQuad(float t)
    {
      t = Mathf.Clamp01(t);
      return 1f - (1f - t) * (1f - t);
    }

    /// <summary>
    /// Đường cong tăng tốc (Cubic Ease In) - thích hợp khi đóng / ẩn dứt điểm.
    /// </summary>
    public static float EaseInCubic(float t)
    {
      t = Mathf.Clamp01(t);
      return t * t * t;
    }

    /// <summary>
    /// Đường cong tăng tốc nhẹ (Quad Ease In).
    /// </summary>
    public static float EaseInQuad(float t)
    {
      t = Mathf.Clamp01(t);
      return t * t;
    }

    /// <summary>
    /// Đường cong S-curve mượt mà (SmoothStep).
    /// </summary>
    public static float SmoothStep(float t)
    {
      t = Mathf.Clamp01(t);
      return t * t * (3f - 2f * t);
    }
  }
}
