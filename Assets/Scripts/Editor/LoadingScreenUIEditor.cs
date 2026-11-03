#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using InfiniteLabyrinth.UI;

namespace InfiniteLabyrinth.Editor
{
  [CustomEditor(typeof(LoadingScreenUI))]
  public class LoadingScreenUIEditor : UnityEditor.Editor
  {
    public override void OnInspectorGUI()
    {
      LoadingScreenUI ui = (LoadingScreenUI)target;

      EditorGUILayout.Space(6);
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);

      GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
      {
        fontSize = 13,
        alignment = TextAnchor.MiddleCenter
      };
      EditorGUILayout.LabelField("👁️ BẢNG ĐIỀU KHIỂN XEM TRƯỚC (LOADING SCREEN PREVIEW)", titleStyle);

      EditorGUILayout.HelpBox(
        "Màn hình Loading mặc định có CanvasGroup Alpha = 0 (trong suốt) để không che khuất màn hình game khi đang chơi.\n" +
        "Bấm nút 'Bật Xem Trước' để hiện toàn bộ ảnh nền, mẹo, thanh loading và tinh chỉnh trực tiếp ngay trong Scene & Game View!",
        MessageType.Info
      );

      EditorGUILayout.BeginHorizontal();
      GUI.backgroundColor = ui.EditorPreviewMode ? new Color(0.4f, 1f, 0.4f) : Color.white;
      if (GUILayout.Button(ui.EditorPreviewMode ? "✅ Đang Bật Xem Trước (Alpha = 1)" : "👁️ Bật Xem Trước (Alpha = 1)", GUILayout.Height(34)))
      {
        ui.ShowPreviewInEditor();
        EditorUtility.SetDirty(ui);
      }

      GUI.backgroundColor = !ui.EditorPreviewMode ? new Color(1f, 0.7f, 0.7f) : Color.white;
      if (GUILayout.Button("🙈 Ẩn Màn Hình (Alpha = 0)", GUILayout.Height(34)))
      {
        ui.HidePreviewInEditor();
        EditorUtility.SetDirty(ui);
      }
      GUI.backgroundColor = Color.white;
      EditorGUILayout.EndHorizontal();

      EditorGUILayout.Space(6);

      if (GUILayout.Button("🎲 Đổi Ngẫu Nhiên Mẹo & Ảnh Nền (Randomize Content)", GUILayout.Height(28)))
      {
        ui.RandomizePreviewInEditor();
        EditorUtility.SetDirty(ui);
      }

      EditorGUILayout.Space(4);
      EditorGUI.BeginChangeCheck();
      float newProg = EditorGUILayout.Slider("Thanh Tiến Trình (Preview %)", ui.PreviewProgress, 0f, 1f);
      if (EditorGUI.EndChangeCheck())
      {
        Undo.RecordObject(ui, "Change Preview Progress");
        ui.SetPreviewProgressDirect(newProg);
        EditorUtility.SetDirty(ui);
      }

      EditorGUILayout.Space(4);
      if (GUILayout.Button("📂 Mở File Cấu Hình Tips & Backgrounds (LoadingScreenData)", GUILayout.Height(24)))
      {
        var data = Resources.Load<LoadingScreenData>("LoadingScreenData");
        if (data != null) Selection.activeObject = data;
        else LoadingScreenSetupEditor.InitializeLoadingScreenData();
      }

      EditorGUILayout.EndVertical();

      EditorGUILayout.Space(6);
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);
      EditorGUILayout.LabelField("🎬 HOẠT ẢNH SPINNER / SPRITE SHEET", titleStyle);

      EditorGUILayout.HelpBox(
        "Thay thế icon xoay tròn bằng hoạt ảnh Sprite Sheet chuyển động cạnh số % (VD: Meme, Chibi, Lửa ma mị).\n" +
        $"• Số Frame hiện có: {ui.AnimationFrameCount} frames\n" +
        $"• Trạng thái xoay 360°: {(ui.EnableSpinnerRotation ? "Đang BẬT" : "Đã TẮT (Chuẩn cho Sprite Sheet)")}\n" +
        $"• Trạng thái hoạt ảnh: {(ui.EnableSpriteAnimation ? "Đang BẬT" : "Đang TẮT")}",
        MessageType.None
      );

      string activePath = LoadingScreenSetupEditor.ResolvedMemePath;
      GUI.backgroundColor = new Color(0.4f, 0.85f, 1f);
      if (GUILayout.Button($"🎬 Tự Động Nạp Frames từ {(activePath.Contains("2_") ? "2_MemeSpriteSheet (190 Frames)" : "MemeSpriteSheet")} (1-Click)", GUILayout.Height(32)))
      {
        Undo.RecordObject(ui, "Load MemeSpriteSheet Frames");
        ui.LoadFramesFromSpriteSheet(activePath);
        EditorUtility.SetDirty(ui);
      }
      GUI.backgroundColor = Color.white;

      EditorGUILayout.EndVertical();
      EditorGUILayout.Space(10);

      // Default Inspector
      DrawDefaultInspector();
    }
  }
}
#endif
