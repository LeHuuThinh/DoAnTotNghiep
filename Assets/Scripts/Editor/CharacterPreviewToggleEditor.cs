#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using InfiniteLabyrinth.UI;

namespace InfiniteLabyrinth.Editor
{
  [CustomEditor(typeof(CharacterPreviewToggleUI))]
  public class CharacterPreviewToggleEditor : UnityEditor.Editor
  {
    public override void OnInspectorGUI()
    {
      CharacterPreviewToggleUI toggle = (CharacterPreviewToggleUI)target;

      EditorGUILayout.Space(6);
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);

      GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
      {
        fontSize = 13,
        alignment = TextAnchor.MiddleCenter
      };
      EditorGUILayout.LabelField("🔄 ĐIỀU KHIỂN TOGGLE 2D / 3D PREVIEW", titleStyle);

      EditorGUILayout.HelpBox(
        $"• Chế độ hiện tại: {(toggle.CurrentMode == CharacterPreviewToggleUI.PreviewMode.Mode3D ? "3D (Model Preview)" : "2D (Splash Art / Meme)")}\n" +
        $"• Trạng thái: {(toggle.gameObject.activeInHierarchy ? "Active trong Scene" : "Inactive")}",
        MessageType.Info
      );

      // Testing Buttons
      EditorGUILayout.BeginHorizontal();
      GUI.backgroundColor = (toggle.CurrentMode == CharacterPreviewToggleUI.PreviewMode.Mode3D) ? new Color(0.4f, 1f, 0.4f) : Color.white;
      if (GUILayout.Button("🧊 Chuyển sang 3D", GUILayout.Height(30)))
      {
        Undo.RecordObject(toggle, "Switch To 3D");
        toggle.SwitchTo3D();
        EditorUtility.SetDirty(toggle);
      }

      GUI.backgroundColor = (toggle.CurrentMode == CharacterPreviewToggleUI.PreviewMode.Mode2D) ? new Color(0.4f, 1f, 0.4f) : Color.white;
      if (GUILayout.Button("🖼 Chuyển sang 2D", GUILayout.Height(30)))
      {
        Undo.RecordObject(toggle, "Switch To 2D");
        toggle.SwitchTo2D();
        EditorUtility.SetDirty(toggle);
      }
      GUI.backgroundColor = Color.white;
      EditorGUILayout.EndHorizontal();

      EditorGUILayout.Space(6);

      // 1-Click Setup & Wire Button
      GUI.backgroundColor = new Color(0.3f, 0.85f, 1f);
      if (GUILayout.Button("⚡ 1-Click Tự Động Kết Nối & Nạp 190 Frame Meme", GUILayout.Height(32)))
      {
        SetupAndWireToggle(toggle);
      }
      GUI.backgroundColor = Color.white;

      EditorGUILayout.EndVertical();
      EditorGUILayout.Space(10);

      DrawDefaultInspector();
    }

    [MenuItem("Infinite Labyrinth/UI/6. Cấu Hình & Kết Nối Toggle 2D-3D Preview (1-Click)", priority = 6)]
    public static void MenuSetupToggle()
    {
      CharacterPreviewToggleUI toggle = Object.FindAnyObjectByType<CharacterPreviewToggleUI>();
      if (toggle == null)
      {
        // Tìm GameObject Toggle2D3D
        GameObject toggleObj = GameObject.Find("Toggle2D3D");
        if (toggleObj != null)
        {
          toggle = toggleObj.AddComponent<CharacterPreviewToggleUI>();
          Undo.RegisterCreatedObjectUndo(toggle, "Add CharacterPreviewToggleUI");
        }
        else
        {
          EditorUtility.DisplayDialog("Thông báo", "Không tìm thấy GameObject 'Toggle2D3D' trong Scene hiện tại!", "OK");
          return;
        }
      }

      SetupAndWireToggle(toggle);
      EditorUtility.DisplayDialog("Thành công", "Đã cấu hình & kết nối hoàn tất Toggle 2D/3D Preview cùng 190 frames meme!", "OK");
    }

    public static void SetupAndWireToggle(CharacterPreviewToggleUI toggle)
    {
      if (toggle == null) return;
      Undo.RecordObject(toggle, "Setup Toggle 2D/3D");

      toggle.AutoFindReferences();

      // Nạp 190 frame meme vào Character2DBox
      SerializedObject so = new SerializedObject(toggle);
      SerializedProperty spAnimator = so.FindProperty("character2DAnimator");
      UISpriteSheetAnimator animator = spAnimator != null ? spAnimator.objectReferenceValue as UISpriteSheetAnimator : null;

      if (animator == null)
      {
        GameObject c2d = GameObject.Find("Character2DBox");
        if (c2d != null)
        {
          animator = c2d.GetComponent<UISpriteSheetAnimator>();
        }
      }

      if (animator != null)
      {
        Undo.RecordObject(animator, "Load Meme Frames");
        string memePath = LoadingScreenSetupEditor.ResolvedMemePath;
        animator.LoadFramesFromAssetPath(memePath);
        EditorUtility.SetDirty(animator);
      }

      // Đảm bảo Image giữ đúng tỷ lệ hình
      SerializedProperty spImage = so.FindProperty("character2DImage");
      Image img = spImage != null ? spImage.objectReferenceValue as Image : null;
      if (img != null)
      {
        Undo.RecordObject(img, "Set Preserve Aspect");
        img.preserveAspect = true;
        if (animator != null && animator.Frames != null && animator.Frames.Length > 0)
        {
          img.sprite = animator.Frames[0];
        }
        EditorUtility.SetDirty(img);
      }

      // Kết nối với InventoryUIController nếu có
      InventoryUIController inventoryUI = Object.FindAnyObjectByType<InventoryUIController>();
      if (inventoryUI != null)
      {
        Undo.RecordObject(inventoryUI, "Wire previewModeToggle");
        SerializedObject soInv = new SerializedObject(inventoryUI);
        SerializedProperty spToggle = soInv.FindProperty("previewModeToggle");
        if (spToggle != null)
        {
          spToggle.objectReferenceValue = toggle;
          soInv.ApplyModifiedProperties();
        }
        EditorUtility.SetDirty(inventoryUI);
      }

      so.ApplyModifiedProperties();
      EditorUtility.SetDirty(toggle);
      UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(toggle.gameObject.scene);

      Debug.Log("<color=green>[CharacterPreviewToggleEditor]</color> Đã thiết lập hoàn tất Toggle 2D/3D Preview và nạp 190 frame meme!");
    }
  }
}
#endif
