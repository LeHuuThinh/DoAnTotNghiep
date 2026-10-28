using System;
using UnityEngine;

namespace InfiniteLabyrinth.Interaction
{
  /// <summary>
  /// Trigger zone inside the Boss Arena. When the player crosses this threshold after
  /// opening the Grand Gate, it seals the gate behind them with the Fog Gate and
  /// raises the event OnPlayerEnteredArena (for Task D02-4: Arena Director & Boss JumpDown).
  /// </summary>
  public class BossArenaTrigger : MonoBehaviour
  {
    [Tooltip("Tham chiếu đến BossGateController điều khiển cổng")]
    [SerializeField] private BossGateController gateController;

    public static event Action OnPlayerEnteredArena;

    public void SetGateController(BossGateController controller)
    {
      gateController = controller;
    }

    private bool hasTriggered = false;

    private void Awake()
    {
      if (gateController == null)
      {
        gateController = GetComponentInParent<BossGateController>() ?? FindAnyObjectByType<BossGateController>();
      }
    }

    private void OnTriggerEnter(Collider other)
    {
      if (hasTriggered) return;
      if (!other.CompareTag("Player")) return;

      if (gateController == null)
      {
        gateController = GetComponentInParent<BossGateController>() ?? FindAnyObjectByType<BossGateController>();
      }

      // Chỉ kích hoạt khóa đấu trường khi cổng đã được mở và người chơi tiến vào trong
      if (gateController != null && gateController.IsOpened)
      {
        hasTriggered = true;
        gateController.LockArena();
        OnPlayerEnteredArena?.Invoke();
        Debug.Log("<color=cyan>[BossArenaTrigger]</color> ⚔️ Player stepped across the arena threshold! Lock-in engaged.");
        gameObject.SetActive(false);
      }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
      Gizmos.color = new Color(0.8f, 0.1f, 0.1f, 0.35f);
      BoxCollider col = GetComponent<BoxCollider>();
      if (col != null)
      {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(col.center, col.size);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(col.center, col.size);
      }
    }
#endif
  }
}
