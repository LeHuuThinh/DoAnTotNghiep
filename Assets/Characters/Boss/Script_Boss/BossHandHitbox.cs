using UnityEngine;

/// <summary>
/// Component gắn trực tiếp vào hai bàn tay Boss (BossHitboxT và BossHitboxP).
/// Bắt va chạm thực tế khi bàn tay đập xuống đất / sàn đấu để kích hoạt hiệu ứng nổ và sát thương chiêu 2 (GumoGumo).
/// Hoạt động song hành cùng hệ thống dò vết độ cao xương tay theo thời gian thực trong IceGolemController.
/// </summary>
public class BossHandHitbox : MonoBehaviour
{
    [SerializeField] private IceGolemController bossController;
    [SerializeField] private int handIndex; // 0 = Tay trái, 1 = Tay phải

    public void Initialize(IceGolemController controller, int index)
    {
        bossController = controller;
        handIndex = index;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryTriggerGroundSlam(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision != null)
        {
            TryTriggerGroundSlam(collision.collider);
        }
    }

    private void TryTriggerGroundSlam(Collider other)
    {
        if (bossController == null || !bossController.IsGumoGumoAttacking) return;
        if (other == null || other.transform.root == transform.root) return;

        // Bỏ qua Player (sát thương được tính diện rộng xung quanh vị trí bàn tay tiếp đất)
        if (bossController.TargetPlayer != null && other.transform.root == bossController.TargetPlayer.root) return;

        // Lấy vị trí sàn chuẩn xác sát mặt đất
        Vector3 groundPos = bossController.GetAccurateGroundPosition(transform.position);
        bossController.TriggerHandSlam(handIndex, groundPos);
    }
}
