using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TPoseRestorer : MonoBehaviour
{
  [Tooltip("Kéo file model .fbx gốc trong Project vào đây")]
  [SerializeField] private GameObject originalFbxModel;

  [ContextMenu("Restore T-Pose From FBX")]
  public void RestoreTPose()
  {
    if (originalFbxModel == null)
    {
      Debug.LogError("Vui lòng kéo file .fbx gốc vào ô Original Fbx Model!");
      return;
    }

    // Tạo một bản sao chép tạm từ file FBX gốc để lấy lại tư thế T-Pose ban đầu
    GameObject tempInstance = Instantiate(originalFbxModel);
    tempInstance.hideFlags = HideFlags.HideAndDontSave;

    Transform[] sourceBones = tempInstance.GetComponentsInChildren<Transform>();
    Transform[] targetBones = GetComponentsInChildren<Transform>();

    int restoredCount = 0;
    foreach (Transform targetBone in targetBones)
    {
      foreach (Transform sourceBone in sourceBones)
      {
        if (targetBone.name == sourceBone.name)
        {
#if UNITY_EDITOR
          Undo.RecordObject(targetBone, "Restore T-Pose");
#endif
          targetBone.localPosition = sourceBone.localPosition;
          targetBone.localRotation = sourceBone.localRotation;
          targetBone.localScale = sourceBone.localScale;
          restoredCount++;
          break;
        }
      }
    }

    DestroyImmediate(tempInstance);

#if UNITY_EDITOR
    EditorUtility.SetDirty(gameObject);
#endif

    Debug.Log($"<color=green>[Thành công]</color> Đã phục hồi T-Pose cho {restoredCount} khớp xương!");
  }
}