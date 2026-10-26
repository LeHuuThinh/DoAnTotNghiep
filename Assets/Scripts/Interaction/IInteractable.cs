using UnityEngine;

namespace InfiniteLabyrinth.Interaction
{
  /// <summary>
  /// Giao diện chuẩn cho tất cả đối tượng có thể tương tác trong game (Portal, Rương, NPC, Đòn bẩy...)
  /// </summary>
  public interface IInteractable
  {
    /// <summary>
    /// Câu gợi ý hiển thị trên giao diện (ví dụ: "Vào Hầm Ngục", "Mở Rương", "Nói chuyện")
    /// </summary>
    string InteractionPrompt { get; }

    /// <summary>
    /// Tên của đối tượng (ví dụ: "Cổng Không Gian", "Rương Kho Báu", "Thợ Rèn")
    /// </summary>
    string TargetName { get; }

    /// <summary>
    /// Có yêu cầu đè giữ phím F không (true: Hold, false: Tap)
    /// </summary>
    bool RequiresHold { get; }

    /// <summary>
    /// Thời gian cần đè giữ phím (giây). Nếu RequiresHold == false thì trả về 0f
    /// </summary>
    float HoldDuration { get; }

    /// <summary>
    /// Bán kính tối đa để người chơi có thể tương tác (mét)
    /// </summary>
    float InteractionRadius { get; }

    /// <summary>
    /// Transform tọa độ thế giới của đối tượng để đo khoảng cách và hướng nhìn
    /// </summary>
    Transform InteractionTransform { get; }

    /// <summary>
    /// Kiểm tra điều kiện xem đối tượng có thể tương tác lúc này không
    /// </summary>
    bool CanInteract(GameObject player);

    /// <summary>
    /// Kích hoạt hành động tương tác khi đã thỏa mãn (vừa bấm Tap hoặc giữ đủ thời gian Hold)
    /// </summary>
    void OnInteract(GameObject player);
  }
}
