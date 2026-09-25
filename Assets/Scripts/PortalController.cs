using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // 1. Thêm namespace này

public class PortalController : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string targetSceneName = "DungeonScene";

    [Header("Hold Settings")]
    [SerializeField] private float requiredHoldTime = 2.0f;

    [Header("UI Feedback (Tùy chọn)")]
    [SerializeField] private GameObject interactionUI;
    [SerializeField] private Image fillProgressImage;

    private float currentHoldTimer = 0f;
    private bool isPlayerInRange = false;

    private void Start()
    {
        if (interactionUI != null)
            interactionUI.SetActive(false);

        if (fillProgressImage != null)
            fillProgressImage.fillAmount = 0f;
    }

    private void Update()
    {
        if (!isPlayerInRange) return;

        // 2. Kiểm tra bàn phím và trạng thái đè phím F bằng New Input System
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.fKey.isPressed)
        {
            currentHoldTimer += Time.deltaTime;

            if (fillProgressImage != null)
            {
                fillProgressImage.fillAmount = Mathf.Clamp01(currentHoldTimer / requiredHoldTime);
            }

            if (currentHoldTimer >= requiredHoldTime)
            {
                LoadDungeonScene();
            }
        }
        else
        {
            if (currentHoldTimer > 0f)
            {
                currentHoldTimer = 0f;
                if (fillProgressImage != null)
                    fillProgressImage.fillAmount = 0f;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            if (interactionUI != null)
                interactionUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            currentHoldTimer = 0f;

            if (interactionUI != null)
                interactionUI.SetActive(false);

            if (fillProgressImage != null)
                fillProgressImage.fillAmount = 0f;
        }
    }

    private void LoadDungeonScene()
    {
        isPlayerInRange = false;
        SceneManager.LoadScene(targetSceneName);
    }
}