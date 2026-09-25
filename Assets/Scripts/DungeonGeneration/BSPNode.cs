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
}