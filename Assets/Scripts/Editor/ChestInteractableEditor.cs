using UnityEngine;
using UnityEditor;
using InfiniteLabyrinth.Inventory;

namespace InfiniteLabyrinth.EditorScripts
{
  [CustomEditor(typeof(ChestInteractable))]
  public class ChestInteractableEditor : UnityEditor.Editor
  {
    public override void OnInspectorGUI()
    {
      DrawDefaultInspector();

      ChestInteractable chest = (ChestInteractable)target;

      EditorGUILayout.Space(10);
      EditorGUILayout.LabelField("=== CÔNG CỤ KIỂM THỬ RUNTIME (PLAY MODE) ===", EditorStyles.boldLabel);

      using (new EditorGUI.DisabledScope(!Application.isPlaying))
      {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("🔓 Mở Rương (Test VFX & Loot)", GUILayout.Height(32)))
        {
          chest.OpenChest();
        }

        if (GUILayout.Button("🔒 Đóng Lại (Reset)", GUILayout.Height(32)))
        {
          chest.ResetChestInEditor();
        }

        EditorGUILayout.EndHorizontal();
      }

      if (!Application.isPlaying)
      {
        EditorGUILayout.HelpBox("Nhấn Play Mode để sử dụng các nút kiểm thử mở rương và kích nổ VFX trực tiếp trong Scene/Game View.", MessageType.Info);
      }
    }
  }
}
