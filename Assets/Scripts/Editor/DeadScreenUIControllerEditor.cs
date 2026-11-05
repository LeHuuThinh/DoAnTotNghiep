#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using InfiniteLabyrinth.UI;

namespace InfiniteLabyrinth.Editor
{
    [CustomEditor(typeof(DeadScreenUIController))]
    public class DeadScreenUIControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DeadScreenUIController controller = (DeadScreenUIController)target;

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "💀 Task GM-02: Màn Hình Tử Trận & Vòng Lặp Hồi Sinh Tại Chuông Làng\n" +
                "• Tự động lắng nghe Player chết (Health.onDeath).\n" +
                "• Chờ 1.2s -> Mở khóa con trỏ chuột -> Fade In 'YOU DIED'.\n" +
                "• Nút RESPAWN: Tăng Day + 1, chuyển về LandScene và đặt vị trí cạnh Chuông Làng, hồi phục 100% HP/Stamina.",
                MessageType.Info);
            EditorGUILayout.Space(6);

            DrawDefaultInspector();

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("⚡ CÔNG CỤ TỰ ĐỘNG HÓA & TEST (1-CLICK TOOLS)", EditorStyles.boldLabel);

            if (GUILayout.Button("⚡ Auto-Wire References (Tự động liên kết tham chiếu)", GUILayout.Height(32)))
            {
                Undo.RecordObject(controller, "Auto Wire DeadScreenUI References");
                controller.EnsureReferences();
                controller.HideImmediate();
                EditorUtility.SetDirty(controller);
                Debug.Log("<color=green>[DeadScreenUIControllerEditor]</color> ✅ Đã tự động dò tìm và liên kết tham chiếu DeadScreenUI hoàn chỉnh!");
            }

            EditorGUILayout.Space(4);

            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("💀 Test Kích Hoạt Tử Trận (Play Mode)", GUILayout.Height(30)))
            {
                controller.TestTriggerDeath();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("👁️ Bật Hiển Thị (Force Show)", GUILayout.Height(26)))
            {
                controller.TestForceShow();
            }
            if (GUILayout.Button("🙈 Ẩn Đi (Hide)", GUILayout.Height(26)))
            {
                controller.TestHide();
            }
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("💡 Hãy nhấn Play Mode để test đầy đủ vòng lặp tử trận và hồi sinh trực quan.", MessageType.None);
            }
        }

        [MenuItem("Infinite Labyrinth/Core Loop & UI/2. Cấu Hình DeadScreenUI (GM-02)", false, 2)]
        [MenuItem("Infinite Labyrinth/UI/8. Cấu Hình DeadScreenUI & Respawn Loop (Task GM-02)", false, 8)]
        [MenuItem("Tools/Infinite Labyrinth/Core Loop/2. Setup DeadScreenUI (GM-02)", false, 2)]
        public static void SetupDeadScreenUIInScene()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Thông báo", "Không tìm thấy Canvas trong Scene hiện tại!", "OK");
                return;
            }

            DeadScreenUIController controller = canvas.GetComponentInChildren<DeadScreenUIController>(true);
            if (controller == null)
            {
                Transform deadObj = canvas.transform.Find("DeadScreenUI");
                if (deadObj != null)
                {
                    controller = deadObj.gameObject.AddComponent<DeadScreenUIController>();
                }
                else
                {
                    controller = canvas.gameObject.AddComponent<DeadScreenUIController>();
                }
                Undo.RegisterCreatedObjectUndo(controller, "Add DeadScreenUIController");
            }

            controller.EnsureReferences();
            controller.HideImmediate();
            EditorUtility.SetDirty(controller);
            Selection.activeGameObject = controller.gameObject;

            Debug.Log("<color=green><b>[DeadScreenUI Setup]</b></color> 🎉 Đã cấu hình DeadScreenUIController thành công trên: " + controller.gameObject.name);
        }
    }
}
#endif
