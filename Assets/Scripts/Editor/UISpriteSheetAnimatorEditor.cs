#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using InfiniteLabyrinth.UI;

namespace InfiniteLabyrinth.Editor
{
  [CustomEditor(typeof(UISpriteSheetAnimator))]
  public class UISpriteSheetAnimatorEditor : UnityEditor.Editor
  {
    public override void OnInspectorGUI()
    {
      UISpriteSheetAnimator animator = (UISpriteSheetAnimator)target;

      EditorGUILayout.Space(6);
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);

      GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
      {
        fontSize = 13,
        alignment = TextAnchor.MiddleCenter
      };
      EditorGUILayout.LabelField("🎬 ĐIỀU KHIỂN HOẠT ẢNH SPRITE SHEET", titleStyle);

      EditorGUILayout.HelpBox(
        $"• Số lượng frames: {animator.FrameCount}\n" +
        $"• Tốc độ: {animator.Fps} FPS\n" +
        $"• Trạng thái: {(animator.IsPlaying ? "Đang chạy ▶" : "Tạm dừng ⏸")}",
        MessageType.Info
      );

      // Playback Controls
      EditorGUILayout.BeginHorizontal();
      GUI.backgroundColor = animator.IsPlaying ? new Color(0.4f, 1f, 0.4f) : Color.white;
      if (GUILayout.Button(animator.IsPlaying ? "▶ Đang Chạy" : "▶ Phát (Play)", GUILayout.Height(30)))
      {
        animator.Play();
        EditorUtility.SetDirty(animator);
      }

      GUI.backgroundColor = !animator.IsPlaying ? new Color(1f, 0.85f, 0.3f) : Color.white;
      if (GUILayout.Button("⏸ Tạm Dừng (Pause)", GUILayout.Height(30)))
      {
        animator.Pause();
        EditorUtility.SetDirty(animator);
      }

      GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
      if (GUILayout.Button("⏹ Dừng Lại (Stop)", GUILayout.Height(30)))
      {
        animator.Stop();
        EditorUtility.SetDirty(animator);
      }
      GUI.backgroundColor = Color.white;
      EditorGUILayout.EndHorizontal();

      EditorGUILayout.Space(6);

      // Frame Scrubber
      if (animator.FrameCount > 1)
      {
        EditorGUI.BeginChangeCheck();
        int newFrame = EditorGUILayout.IntSlider("Xem Frame (Scrubber)", animator.CurrentFrameIndex, 0, animator.FrameCount - 1);
        if (EditorGUI.EndChangeCheck())
        {
          Undo.RecordObject(animator, "Scrub Frame");
          animator.SetFrame(newFrame);
          EditorUtility.SetDirty(animator);
        }
      }

      EditorGUILayout.Space(6);

      // 1-Click Load Buttons
      string activePath = LoadingScreenSetupEditor.ResolvedMemePath;
      GUI.backgroundColor = new Color(0.4f, 0.85f, 1f);
      if (GUILayout.Button($"🎬 Nạp Frames từ {(activePath.Contains("2_") ? "2_MemeSpriteSheet (190 Frames)" : "MemeSpriteSheet.png")} (1-Click)", GUILayout.Height(30)))
      {
        Undo.RecordObject(animator, "Load MemeSpriteSheet Frames");
        animator.LoadFramesFromAssetPath(activePath);
        EditorUtility.SetDirty(animator);
      }
      GUI.backgroundColor = Color.white;

      EditorGUILayout.EndVertical();
      EditorGUILayout.Space(10);

      DrawDefaultInspector();
    }
  }
}
#endif
