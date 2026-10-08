using UnityEngine;

public class UnlockMovementBehaviour : StateMachineBehaviour
{
    // Hàm này tự động chạy ngay khoảnh khắc State này kết thúc và chuyển sang State khác
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        PlayerController player = animator.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            player.UnlockMovement();
        }
    }
}