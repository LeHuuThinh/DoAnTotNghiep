using System.Collections;
using UnityEngine;

/// <summary>
/// Quản lý đấu trường giao chiến giữa Người Chơi (Player) và Boss Ice Golem (ML-Agents).
/// Cung cấp cơ chế đấu hiệp liên hoàn (Endless Sparring Rounds):
/// - Khi Player sắp cạn máu (<= 5 HP), hệ thống chặn lại, trao chiến thắng cho Boss,
///   và tự động phục hồi máu cho cả hai rồi xếp lại vị trí để bắt đầu hiệp mới.
/// - Khi Boss cạn máu (<= 0 HP), hệ thống trao chiến thắng cho Player,
///   phục hồi máu cho Boss và xếp lại vị trí.
/// - ĐẢM BẢO TUYỆT ĐỐI: Không bao giờ làm chết kẹt Player, không sinh lỗi Game Over,
///   và không cần phải tải lại Scene (Reload Scene).
/// </summary>
public class IceGolemBattleArena : MonoBehaviour
{
    [Header("=== THAM CHIẾU CHIẾN ĐẤU ===")]
    [Tooltip("Agent ML của Boss Ice Golem")]
    [SerializeField] private IceGolemAgent bossAgent;
    [Tooltip("Transform của Boss")]
    [SerializeField] private Transform bossTransform;
    [Tooltip("Transform của Player")]
    [SerializeField] private Transform playerTransform;

    [Header("=== ĐIỂM XUẤT PHÁT HIỆP ĐẤU ===")]
    [Tooltip("Vị trí xuất phát của Boss ở đầu mỗi hiệp đấu")]
    [SerializeField] private Transform bossSpawnPoint;
    [Tooltip("Vị trí xuất phát của Player ở đầu mỗi hiệp đấu")]
    [SerializeField] private Transform playerSpawnPoint;
    [Tooltip("Khoảng cách mặc định giữa Player và Boss nếu không đặt SpawnPoint (mét)")]
    [SerializeField] private float defaultResetDistance = 12f;

    [Header("=== CÀI ĐẶT HỒI SINH (RESPAWN) ===")]
    [Tooltip("Nếu false, khi Boss chết sẽ nằm gục luôn, không hồi sinh để tiếp tục train")]
    [SerializeField] private bool respawnBossOnDeath = true;

    [Header("=== CÀI ĐẶT HIỆP ĐẤU ===")]
    [Tooltip("Ngưỡng máu bảo hiểm của Player (dưới ngưỡng này tính là Boss thắng hiệp)")]
    [SerializeField] private float playerDefeatHpThreshold = 5f;
    [Tooltip("Thời gian nghỉ giữa 2 hiệp đấu (giây)")]
    [SerializeField] private float roundResetDelay = 1.2f;

    private Health bossHealth;
    private Health playerHealth;
    private bool isRoundTransitioning = false;
    private int roundCount = 1;

    private void Start()
    {
        AutoFindReferences();
    }

    private void AutoFindReferences()
    {
        if (bossAgent == null) bossAgent = FindFirstObjectByType<IceGolemAgent>();
        if (bossAgent != null)
        {
            bossTransform = bossAgent.transform;
            bossHealth = bossAgent.BossHealth;
            if (bossHealth == null) bossHealth = bossAgent.GetComponent<Health>();
        }

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null) playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
                playerHealth = playerObj.GetComponent<Health>();
            }
        }
        else if (playerHealth == null)
        {
            playerHealth = playerTransform.GetComponent<Health>();
        }
    }

    private void Update()
    {
        if (isRoundTransitioning) return;

        if (bossHealth == null || playerHealth == null)
        {
            AutoFindReferences();
            return;
        }

        // 1. Kiểm tra Boss thắng hiệp (Player cạn máu)
        if (playerHealth.CurrentHealth <= playerDefeatHpThreshold)
        {
            StartCoroutine(RoundEndRoutine(isBossWinner: true));
        }
        // 2. Kiểm tra Player thắng hiệp (Boss cạn máu)
        else if (bossHealth.CurrentHealth <= 0.1f)
        {
            if (respawnBossOnDeath)
            {
                StartCoroutine(RoundEndRoutine(isBossWinner: false));
            }
            else
            {
                // Boss chết thì chết luôn, không hồi sinh để tiếp tục train
                isRoundTransitioning = true;
                enabled = false;
            }
        }
    }

    private IEnumerator RoundEndRoutine(bool isBossWinner)
    {
        isRoundTransitioning = true;

        if (isBossWinner)
        {
            Debug.Log($"<color=cyan>[Đấu Trường Hiệp {roundCount}]</color> 🏆 Boss Ice Golem đã đánh bại Player!");
            if (bossAgent != null)
            {
                bossAgent.AddReward(5.0f);
            }
        }
        else
        {
            Debug.Log($"<color=yellow>[Đấu Trường Hiệp {roundCount}]</color> ⚔️ Bạn đã hạ gục Boss Ice Golem!");
            if (bossAgent != null)
            {
                bossAgent.AddReward(-3.0f);
            }
        }

        yield return new WaitForSeconds(roundResetDelay);

        // Hồi phục toàn bộ máu cho Player và Boss
        if (playerHealth != null)
        {
            playerHealth.Heal(playerHealth.MaxHealth);
        }

        if (bossHealth != null)
        {
            bossHealth.Heal(bossHealth.MaxHealth);
        }

        // Reset vị trí của Player và Boss
        ResetPositions();

        roundCount++;
        Debug.Log($"<color=green>[Đấu Trường]</color> 👉 BẮT ĐẦU HIỆP ĐẤU THỨ {roundCount}! Chúc may mắn!");

        // Thông báo cho ML-Agent bắt đầu Episode mới
        if (bossAgent != null)
        {
            bossAgent.EndEpisode();
        }

        isRoundTransitioning = false;
    }

    public void ResetPositions()
    {
        if (bossSpawnPoint != null && bossTransform != null)
        {
            bossTransform.position = bossSpawnPoint.position;
            bossTransform.rotation = bossSpawnPoint.rotation;
        }

        if (playerSpawnPoint != null && playerTransform != null)
        {
            CharacterController cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            playerTransform.position = playerSpawnPoint.position;
            playerTransform.rotation = playerSpawnPoint.rotation;
            if (cc != null) cc.enabled = true;
        }
        else if (bossTransform != null && playerTransform != null)
        {
            // Nếu không có SpawnPoint, xếp 2 đối thủ đứng cách nhau defaultResetDistance
            Vector3 center = transform.position;
            Vector3 bossPos = center + Vector3.forward * (defaultResetDistance * 0.5f);
            Vector3 playerPos = center - Vector3.forward * (defaultResetDistance * 0.5f);

            bossTransform.position = bossPos;
            bossTransform.LookAt(new Vector3(playerPos.x, bossPos.y, playerPos.z));

            CharacterController cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            playerTransform.position = playerPos;
            playerTransform.LookAt(new Vector3(bossPos.x, playerPos.y, bossPos.z));
            if (cc != null) cc.enabled = true;
        }
    }
}
