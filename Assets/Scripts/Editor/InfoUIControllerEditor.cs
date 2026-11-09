#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using InfiniteLabyrinth.UI;

namespace InfiniteLabyrinth.Editor
{
    [CustomEditor(typeof(InfoUIController))]
    public class InfoUIControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            InfoUIController controller = (InfoUIController)target;

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "🎮 Task GM-01: Hệ Thống Thông Tin Thế Giới & Biểu Ngữ Vùng Đất\n" +
                "• TopCornerInfo: Hiển thị ngày (Day N), tầng (Floor X), và địa danh góc trên phải.\n" +
                "• MidScreenInfo: Hoạt cảnh Cinematic Fade In / Hold / Fade Out giới thiệu khu vực (Dark Souls style).",
                MessageType.Info);
            EditorGUILayout.Space(6);

            DrawDefaultInspector();

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("⚡ CÔNG CỤ TỰ ĐỘNG HÓA & TEST (1-CLICK TOOLS)", EditorStyles.boldLabel);

            if (GUILayout.Button("⚡ Auto-Wire References (Tự động liên kết tham chiếu)", GUILayout.Height(32)))
            {
                Undo.RecordObject(controller, "Auto Wire InfoUI References");
                controller.EnsureReferences();
                controller.UpdateTopCornerDisplay();
                EditorUtility.SetDirty(controller);
                Debug.Log("<color=green>[InfoUIControllerEditor]</color> ✅ Đã tự động dò tìm và liên kết tham chiếu hoàn chỉnh!");
            }

            EditorGUILayout.Space(4);

            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("▶ Test Area Intro Banner (Play Mode)", GUILayout.Height(30)))
            {
                controller.TriggerCurrentAreaIntro();
            }

            if (GUILayout.Button("🔄 Cập Nhật Hiển Thị Top Corner (Play Mode)", GUILayout.Height(26)))
            {
                controller.UpdateTopCornerDisplay();
            }
            GUI.enabled = true;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("💡 Hãy nhấn Play Mode để test hiệu ứng biểu ngữ chuyển cảnh trực quan.", MessageType.None);
            }
        }

        [MenuItem("Infinite Labyrinth/Core Loop & UI/1. Cấu Hình InfoUI (GM-01)", false, 1)]
        public static void SetupInfoUIInScene()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Thông báo", "Không tìm thấy Canvas trong Scene hiện tại!", "OK");
                return;
            }

            InfoUIController controller = canvas.GetComponentInChildren<InfoUIController>(true);
            if (controller == null)
            {
                Transform topCorner = canvas.transform.Find("TopCornerInfo");
                if (topCorner != null)
                {
                    controller = topCorner.gameObject.AddComponent<InfoUIController>();
                }
                else
                {
                    controller = canvas.gameObject.AddComponent<InfoUIController>();
                }
                Undo.RegisterCreatedObjectUndo(controller, "Add InfoUIController");
            }

            controller.EnsureReferences();
            controller.UpdateTopCornerDisplay();
            EditorUtility.SetDirty(controller);
            Selection.activeGameObject = controller.gameObject;

            Debug.Log("<color=green><b>[InfoUI Setup]</b></color> 🎉 Đã cấu hình InfoUIController thành công trên GameObject: " + controller.gameObject.name);
        }
    }
}
#endif

