using UnityEngine;

public class CombatLayerWeightBehaviour : StateMachineBehaviour
{
    [Tooltip("Index của Layer chứa Avatar Mask tay phải (Mặc định Base Layer là 0, Layer tiếp theo là 1)")]
    public int armLayerIndex = 1;

    // Chạy ngay tại frame đầu tiên khi bắt đầu State chém
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Trả quyền điều khiển tay phải về lại cho Base Layer
        animator.SetLayerWeight(armLayerIndex, 0f);
    }

    // Chạy ngay tại frame cuối cùng khi rời khỏi State chém
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Ép tay phải quay lại dáng cầm kiếm tĩnh của Layer 1
        animator.SetLayerWeight(armLayerIndex, 1f);
    }
}