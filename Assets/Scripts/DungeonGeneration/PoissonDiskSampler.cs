using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Thuật toán Poisson Disk Sampling (Bridson's Algorithm):
/// Tạo các điểm ngẫu nhiên phân bố tự nhiên và đồng đều trong không gian 2D,
/// đảm bảo khoảng cách giữa 2 điểm bất kỳ luôn lớn hơn hoặc bằng bán kính r.
/// Dùng để rải thùng gỗ, nến, rương, đạo cụ mà không bao giờ bị đè/lồng vào nhau.
/// </summary>
public static class PoissonDiskSampler
{
  /// <summary>
  /// Lấy danh sách điểm ngẫu nhiên trong vùng hình chữ nhật (xMin, yMin) đến (xMax, yMax)
  /// </summary>
  /// <param name="xMin">Tọa độ X nhỏ nhất</param>
  /// <param name="yMin">Tọa độ Z/Y nhỏ nhất</param>
  /// <param name="xMax">Tọa độ X lớn nhất</param>
  /// <param name="yMax">Tọa độ Z/Y lớn nhất</param>
  /// <param name="radius">Khoảng cách tối thiểu giữa 2 điểm</param>
  /// <param name="k">Số lần thử sinh điểm xung quanh trước khi từ bỏ (mặc định 30)</param>
  /// <param name="isPositionAllowed">Hàm kiểm tra điều kiện hợp lệ (né cửa, né cầu thang)</param>
  public static List<Vector2> Sample(float xMin, float yMin, float xMax, float yMax, float radius, int k = 30, Func<Vector2, bool> isPositionAllowed = null)
  {
    List<Vector2> points = new List<Vector2>();

    float width = xMax - xMin;
    float height = yMax - yMin;

    if (width <= radius || height <= radius) return points;

    // Kích thước ô lưới phụ trợ để kiểm tra O(1) lân cận
    float cellSize = radius / Mathf.Sqrt(2f);
    int gridWidth = Mathf.CeilToInt(width / cellSize);
    int gridHeight = Mathf.CeilToInt(height / cellSize);

    int[,] grid = new int[gridWidth, gridHeight];
    for (int x = 0; x < gridWidth; x++)
      for (int y = 0; y < gridHeight; y++)
        grid[x, y] = -1; // -1 nghĩa là ô trống

    List<Vector2> activeList = new List<Vector2>();

    // 1. Sinh điểm mầm đầu tiên
    Vector2 firstPoint = new Vector2(UnityEngine.Random.Range(xMin + radius, xMax - radius), UnityEngine.Random.Range(yMin + radius, yMax - radius));
    if (isPositionAllowed == null || isPositionAllowed(firstPoint))
    {
      points.Add(firstPoint);
      activeList.Add(firstPoint);
      int gx = Mathf.Clamp(Mathf.FloorToInt((firstPoint.x - xMin) / cellSize), 0, gridWidth - 1);
      int gy = Mathf.Clamp(Mathf.FloorToInt((firstPoint.y - yMin) / cellSize), 0, gridHeight - 1);
      grid[gx, gy] = 0;
    }

    // 2. Vòng lặp lan tỏa điểm từ danh sách active
    while (activeList.Count > 0)
    {
      int randomIndex = UnityEngine.Random.Range(0, activeList.Count);
      Vector2 currentPoint = activeList[randomIndex];
      bool foundValid = false;

      // Thử k lần sinh điểm mới trong vành khuyên [radius, 2 * radius]
      for (int i = 0; i < k; i++)
      {
        float angle = UnityEngine.Random.value * Mathf.PI * 2f;
        float distance = UnityEngine.Random.Range(radius, 2f * radius);
        Vector2 candidate = currentPoint + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

        // Kiểm tra xem candidate có nằm trong bounds không
        if (candidate.x >= xMin && candidate.x <= xMax && candidate.y >= yMin && candidate.y <= yMax)
        {
          if (isPositionAllowed != null && !isPositionAllowed(candidate)) continue;

          // Kiểm tra khoảng cách với các điểm lân cận trong lưới
          if (IsValid(candidate, xMin, yMin, cellSize, radius, gridWidth, gridHeight, grid, points))
          {
            points.Add(candidate);
            activeList.Add(candidate);

            int gx = Mathf.Clamp(Mathf.FloorToInt((candidate.x - xMin) / cellSize), 0, gridWidth - 1);
            int gy = Mathf.Clamp(Mathf.FloorToInt((candidate.y - yMin) / cellSize), 0, gridHeight - 1);
            grid[gx, gy] = points.Count - 1;

            foundValid = true;
            break;
          }
        }
      }

      if (!foundValid)
      {
        activeList.RemoveAt(randomIndex);
      }
    }

    return points;
  }

  private static bool IsValid(Vector2 candidate, float xMin, float yMin, float cellSize, float radius, int gridWidth, int gridHeight, int[,] grid, List<Vector2> points)
  {
    int cellX = Mathf.FloorToInt((candidate.x - xMin) / cellSize);
    int cellY = Mathf.FloorToInt((candidate.y - yMin) / cellSize);

    int startX = Mathf.Max(0, cellX - 2);
    int endX = Mathf.Min(gridWidth - 1, cellX + 2);
    int startY = Mathf.Max(0, cellY - 2);
    int endY = Mathf.Min(gridHeight - 1, cellY + 2);

    float radiusSqr = radius * radius;

    for (int x = startX; x <= endX; x++)
    {
      for (int y = startY; y <= endY; y++)
      {
        int pointIndex = grid[x, y];
        if (pointIndex != -1)
        {
          Vector2 other = points[pointIndex];
          if ((candidate - other).sqrMagnitude < radiusSqr)
          {
            return false;
          }
        }
      }
    }

    return true;
  }
}
