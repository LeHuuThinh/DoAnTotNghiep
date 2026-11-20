#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace InfiniteLabyrinth.Editor
{
  /// <summary>
  /// Custom Editor giao diện đẹp cho PlayerSlashVFX với các nút bấm Test đòn chém trực quan và công cụ điều chỉnh Slow-motion.
  /// </summary>
  [CustomEditor(typeof(PlayerSlashVFX))]
  public class PlayerSlashVFXEditor : UnityEditor.Editor
  {
    public override void OnInspectorGUI()
    {
      DrawDefaultInspector();

      PlayerSlashVFX slashScript = (PlayerSlashVFX)target;
      if (slashScript == null) return;

      EditorGUILayout.Space(12);
      EditorGUILayout.LabelField("⏱️ ĐIỀU CHỈNH TỐC ĐỘ GAME (SLOW MOTION TOOL)", EditorStyles.boldLabel);
      EditorGUILayout.BeginVertical(EditorStyles.helpBox);

      float currentScale = slashScript.GameSpeed;
      EditorGUILayout.LabelField($"Tốc độ hiện tại: {currentScale:F2}x {(currentScale < 0.99f ? "(Đang Slow-Mo)" : "(Bình thường)")}", EditorStyles.miniBoldLabel);

      EditorGUI.BeginChangeCheck();
      float sliderScale = EditorGUILayout.Slider("Tốc độ (TimeScale)", currentScale, 0.05f, 1.0f);
      if (EditorGUI.EndChangeCheck())
      {
        slashScript.ApplyTimeScale(sliderScale);
        EditorUtility.SetDirty(slashScript);
      }

      EditorGUILayout.Space(4);
      EditorGUILayout.BeginHorizontal();
      if (GUILayout.Button("0.1x (Siêu Chậm)"))
      {
        slashScript.ApplyTimeScale(0.1f);
        EditorUtility.SetDirty(slashScript);
      }
      if (GUILayout.Button("0.25x (Chậm)"))
      {
        slashScript.ApplyTimeScale(0.25f);
        EditorUtility.SetDirty(slashScript);
      }
      if (GUILayout.Button("0.5x (Vừa)"))
      {
        slashScript.ApplyTimeScale(0.5f);
        EditorUtility.SetDirty(slashScript);
      }
      if (GUILayout.Button("1.0x (Chuẩn)"))
      {
        slashScript.ApplyTimeScale(1.0f);
        EditorUtility.SetDirty(slashScript);
      }
      EditorGUILayout.EndHorizontal();

      EditorGUILayout.HelpBox("Phím tắt trong Game:\n• [ : Giảm tốc độ (1.0 -> 0.5 -> 0.25 -> 0.1 -> 0.05)\n• ] : Tăng tốc độ (0.05 -> 0.1 -> 0.25 -> 0.5 -> 1.0)", MessageType.Info);
      EditorGUILayout.EndVertical();

      EditorGUILayout.Space(12);
      EditorGUILayout.LabelField("⚔️ THỬ NGHIỆM ĐÒN CHÉM (TEST SLASH VFX)", EditorStyles.boldLabel);

      if (slashScript.DefaultPrefab == null)
      {
        EditorGUILayout.HelpBox("Chưa gán 'defaultSlashVfxPrefab'. Hãy kéo Prefab 'Slash VFX' vào ô trên!", MessageType.Warning);
      }

      EditorGUILayout.BeginVertical(EditorStyles.helpBox);

      if (GUILayout.Button("⚡ Test Attack 1 (Chém chéo phải xuống trái)", GUILayout.Height(28)))
      {
        slashScript.TestAttack1();
      }

      if (GUILayout.Button("⚡ Test Attack 2 (Chém vát trái lên phải)", GUILayout.Height(28)))
      {
        slashScript.TestAttack2();
      }

      if (GUILayout.Button("⚡ Test Attack 3 (Chém quét ngang phẳng kết combo)", GUILayout.Height(28)))
      {
        slashScript.TestAttack3();
      }

      EditorGUILayout.Space(4);

      if (GUILayout.Button("💥 Test Charge Hit 1 (Chém mở màn)", GUILayout.Height(26)))
      {
        slashScript.TestCharge1();
      }

      if (GUILayout.Button("💥 Test Charge Hit 2 (Bổ dọc từ trên xuống)", GUILayout.Height(26)))
      {
        slashScript.TestCharge2();
      }

      EditorGUILayout.Space(4);

      GUI.backgroundColor = new Color(1f, 0.8f, 0.3f);
      if (GUILayout.Button("🔥 Test Counter Attack (Phản công uy lực)", GUILayout.Height(30)))
      {
        slashScript.TestCounter();
      }
      GUI.backgroundColor = Color.white;

      EditorGUILayout.Space(6);
      GUI.backgroundColor = new Color(0.25f, 0.85f, 1f);
      if (GUILayout.Button("🌀 Test Skill E Slash (Chém Kiếm Khí)", GUILayout.Height(30)))
      {
        slashScript.TestSkillE();
      }
      GUI.backgroundColor = new Color(1f, 0.65f, 0.2f);
      if (GUILayout.Button("💥 Test Skill Q Slash (Đập Đất AOE)", GUILayout.Height(30)))
      {
        slashScript.TestSkillQ();
      }
      GUI.backgroundColor = new Color(1f, 0.25f, 0.25f);
      if (GUILayout.Button("⚔️ Test Skill Ult Slash (Chém Ngang Khổng Lồ)", GUILayout.Height(32)))
      {
        slashScript.TestSkillUlt();
      }
      GUI.backgroundColor = Color.white;

      EditorGUILayout.Space(6);
      GUI.backgroundColor = new Color(1f, 0.45f, 0.2f);
      if (GUILayout.Button("💥 Test Thử Hit Impact VFX (Chém Trúng Đích)", GUILayout.Height(30)))
      {
        slashScript.TestImpact();
      }
      GUI.backgroundColor = Color.white;

      EditorGUILayout.Space(4);
      GUI.backgroundColor = new Color(0.35f, 0.85f, 1f);
      if (GUILayout.Button("🛡️ Test Thử Parry VFX (Gạt Đòn Lưỡi Kiếm)", GUILayout.Height(30)))
      {
        slashScript.TestParry();
      }
      GUI.backgroundColor = Color.white;

      if (!Application.isPlaying)
      {
        EditorGUILayout.Space(6);
        if (GUILayout.Button("⏹️ Dọn Dẹp / Tắt Preview Ngay", GUILayout.Height(24)))
        {
          PlayerSlashVFX.StopActiveEditorPreview();
        }
      }

      EditorGUILayout.EndVertical();
    }
  }
}
#endif
