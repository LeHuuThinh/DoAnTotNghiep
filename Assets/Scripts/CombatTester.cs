using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class CombatTester : MonoBehaviour
{
  [SerializeField] private GameObject enemyPrefab;
  [SerializeField] private float spawnDistance = 4f;

  private void Update()
  {
    if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
    {
      SpawnEnemyInFront();
    }
  }

  private void SpawnEnemyInFront()
  {
    if (enemyPrefab == null) return;

    Vector3 spawnPos = transform.position + transform.forward * spawnDistance;

    if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 3f, NavMesh.AllAreas))
    {
      Instantiate(enemyPrefab, hit.position, Quaternion.identity);
      Debug.Log("<color=green>[Test]</color> Đã spawn 1 quái vật thành công!");
    }
    else
    {
      Debug.LogWarning("Không tìm thấy bề mặt NavMesh hợp lệ ở phía trước!");
    }
  }
}