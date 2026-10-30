using UnityEngine;

public class BSPNode
{
  // Không gian của Node này (tọa độ x, y và kích thước width, height)
  public RectInt bounds;

  // Không gian của căn phòng thực tế sẽ được đào bên trong Node này
  public RectInt roomBounds;

  // Hai Node con sau khi bị cắt đôi
  public BSPNode leftChild;
  public BSPNode rightChild;

  // Đánh dấu đây là Node dành riêng cho Đấu Trường Boss (không bị cắt nhỏ tiếp)
  public bool isBossNode = false;

  // Constructor để dễ dàng tạo Node mới
  public BSPNode(RectInt bounds)
  {
    this.bounds = bounds;
  }

  // Hàm kiểm tra xem Node này có phải là Node lá (không bị cắt nữa) hay không
  public bool IsLeaf()
  {
    return leftChild == null && rightChild == null;
  }

  // Hàm kiểm tra xem Node này hoặc bất kỳ con cháu nào có phải là Boss Node không
  public bool ContainsBossNode()
  {
    if (isBossNode) return true;
    if (leftChild != null && leftChild.ContainsBossNode()) return true;
    if (rightChild != null && rightChild.ContainsBossNode()) return true;
    return false;
  }
}
