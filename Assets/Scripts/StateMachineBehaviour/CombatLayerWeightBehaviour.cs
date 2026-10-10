using UnityEngine;

public class CombatLayerWeightBehaviour : StateMachineBehaviour
{
    [Tooltip("Index của Layer chứa Avatar Mask tay phải (Mặc định Base Layer là 0, Layer tiếp theo là 1)")]
    public int armLayerIndex = 1;

    // Chạy DUY NHẤT 1 LẦN khi nhân vật bước vào Sub-State Machine Melee Combat
    public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    {
        animator.SetLayerWeight(armLayerIndex, 0f);
    }

    // Chạy DUY NHẤT 1 LẦN khi nhân vật rời hẳn Sub-State Machine ra cổng Exit
    public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    {
        animator.SetLayerWeight(armLayerIndex, 1f);
    }
}