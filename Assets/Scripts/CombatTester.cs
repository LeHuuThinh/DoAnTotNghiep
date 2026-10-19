using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

/// <summary>
/// Script hỗ trợ Test chiến đấu nhóm trong Scene CombatTest:
/// Bấm phím [T] -> Triệu hồi đội hình 4 quái vật xếp theo 2x2:
/// - Hàng trước: SkeletonWarrior (Trái) & SkeletonMinion (Phải)
/// - Hàng sau:   SkeletonRogue (Trái) & SkeletonMage (Phải)
/// Tất cả đều xoay mặt trực diện về phía Player sẵn sàng giao tranh!
/// </summary>
public class CombatTester : MonoBehaviour
{
  [Header("Monster Squad Prefabs (Đội hình 4 quái)")]
  [Tooltip("Quái cận chiến kiếm khiên (Hàng trước bên trái)")]
  [SerializeField] private GameObject warriorPrefab;

  [Tooltip("Quái cận chiến lính rìu (Hàng trước bên phải)")]
  [SerializeField] private GameObject minionPrefab;

  [Tooltip("Quái cung thủ đánh xa (Hàng sau bên trái)")]
  [SerializeField] private GameObject roguePrefab;

  [Tooltip("Quái pháp sư thả chiêu (Hàng sau bên phải)")]
  [SerializeField] private GameObject magePrefab;

  [Header("Fallback (Quái đơn lẻ cũ)")]
  [SerializeField] private GameObject enemyPrefab;

  [Header("Formation 2x2 Settings")]
  [Tooltip("Khoảng cách từ Player tới tâm đội hình (m)")]
  [SerializeField] private float spawnDistance = 14f;

  [Tooltip("Khoảng cách giữa hàng trước và hàng sau (m)")]
  [SerializeField] private float rowSpacing = 3.5f;

  [Tooltip("Khoảng cách giữa 2 quái cùng hàng (m)")]
  [SerializeField] private float columnSpacing = 3.2f;

  [Tooltip("Tự động xoay mặt quái về hướng Player khi vừa spawn")]
  [SerializeField] private bool facePlayerOnSpawn = true;

  private void Update()
  {
    if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
    {
      SpawnSquadFormation2x2();
    }
  }

  /// <summary>
  /// Triệu hồi đội hình 2x2:
  /// Hàng trước (Front): Warrior (Trái) | Minion (Phải)
  /// Hàng sau (Back):    Rogue (Trái)   | Mage (Phải)
  /// </summary>
  [ContextMenu("Spawn 2x2 Monster Squad")]
  public void SpawnSquadFormation2x2()
  {
    Vector3 playerPos = transform.position;
    Vector3 forward = transform.forward;
    forward.y = 0f;
    if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
    forward.Normalize();

    Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

    Vector3 squadCenter = playerPos + forward * spawnDistance;

    float halfCol = columnSpacing * 0.5f;
    float halfRow = rowSpacing * 0.5f;

    // Hàng trước (gần Player hơn)
    Vector3 frontRowCenter = squadCenter - forward * halfRow;
    Vector3 warriorPos = frontRowCenter - right * halfCol;
    Vector3 minionPos = frontRowCenter + right * halfCol;

    // Hàng sau (xa Player hơn)
    Vector3 backRowCenter = squadCenter + forward * halfRow;
    Vector3 roguePos = backRowCenter - right * halfCol;
    Vector3 magePos = backRowCenter + right * halfCol;

    int spawnedCount = 0;

    // Spawn 1: Warrior (Hàng trước - Trái)
    GameObject actualWarrior = warriorPrefab != null ? warriorPrefab : enemyPrefab;
    if (SpawnMonster(actualWarrior, warriorPos, playerPos, "Skeleton Warrior (Hàng trước - Trái)")) spawnedCount++;

    // Spawn 2: Minion (Hàng trước - Phải)
    GameObject actualMinion = minionPrefab != null ? minionPrefab : enemyPrefab;
    if (SpawnMonster(actualMinion, minionPos, playerPos, "Skeleton Minion (Hàng trước - Phải)")) spawnedCount++;

    // Spawn 3: Rogue (Hàng sau - Trái)
    GameObject actualRogue = roguePrefab != null ? roguePrefab : enemyPrefab;
    if (SpawnMonster(actualRogue, roguePos, playerPos, "Skeleton Rogue (Hàng sau - Trái)")) spawnedCount++;

    // Spawn 4: Mage (Hàng sau - Phải)
    GameObject actualMage = magePrefab != null ? magePrefab : enemyPrefab;
    if (SpawnMonster(actualMage, magePos, playerPos, "Skeleton Mage (Hàng sau - Phải)")) spawnedCount++;

    Debug.Log($"<color=cyan>[CombatTester]</color> Đã triệu hồi thành công đội hình 2x2 gồm {spawnedCount}/4 quái vật!");
  }

  private bool SpawnMonster(GameObject prefab, Vector3 targetPos, Vector3 playerPos, string roleName)
  {
    if (prefab == null) return false;

    // Tìm bề mặt NavMesh hợp lệ gần vị trí tính toán
    if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 4.0f, NavMesh.AllAreas))
    {
      Quaternion rotation = Quaternion.identity;
      if (facePlayerOnSpawn)
      {
        Vector3 lookDir = (playerPos - hit.position);
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.001f)
        {
          rotation = Quaternion.LookRotation(lookDir);
        }
      }

      Instantiate(prefab, hit.position, rotation);
      return true;
    }
    else
    {
      Debug.LogWarning($"<color=orange>[CombatTester]</color> Không tìm thấy NavMesh hợp lệ cho {roleName} tại {targetPos}");
      return false;
    }
  }

#if UNITY_EDITOR
  private void Reset()
  {
    AutoFindPrefabs();
  }

  [ContextMenu("Auto Find Monster Prefabs")]
  public void AutoFindPrefabs()
  {
    if (warriorPrefab == null)
      warriorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Monsters/SkeletonWarrior.prefab");
    if (minionPrefab == null)
      minionPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Monsters/SkeletonMinion.prefab");
    if (roguePrefab == null)
      roguePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Monsters/SkeletonRogue.prefab");
    if (magePrefab == null)
      magePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Monsters/SkeletonMage.prefab");
  }

  private void OnDrawGizmosSelected()
  {
    Vector3 forward = transform.forward;
    forward.y = 0f;
    if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
    forward.Normalize();
    Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

    Vector3 squadCenter = transform.position + forward * spawnDistance;
    float halfCol = columnSpacing * 0.5f;
    float halfRow = rowSpacing * 0.5f;

    Vector3 frontLeft = squadCenter - forward * halfRow - right * halfCol;
    Vector3 frontRight = squadCenter - forward * halfRow + right * halfCol;
    Vector3 backLeft = squadCenter + forward * halfRow - right * halfCol;
    Vector3 backRight = squadCenter + forward * halfRow + right * halfCol;

    Gizmos.color = Color.red; // Hàng trước cận chiến
    Gizmos.DrawWireSphere(frontLeft, 0.6f);
    Gizmos.DrawWireSphere(frontRight, 0.6f);

    Gizmos.color = Color.magenta; // Hàng sau tầm xa
    Gizmos.DrawWireSphere(backLeft, 0.6f);
    Gizmos.DrawWireSphere(backRight, 0.6f);

    Gizmos.color = Color.yellow;
    Gizmos.DrawLine(frontLeft, frontRight);
    Gizmos.DrawLine(backLeft, backRight);
    Gizmos.DrawLine(frontLeft, backLeft);
    Gizmos.DrawLine(frontRight, backRight);
  }
#endif
}