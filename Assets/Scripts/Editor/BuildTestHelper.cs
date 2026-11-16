#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;

namespace InfiniteLabyrinth.Editor
{
    public static class BuildTestHelper
    {
        [MenuItem("Infinite Labyrinth/🛠️ Kiểm Tra Biên Dịch Bản Build (Test Player Compile)", false, 1000)]
        public static void TestCompilePlayerScripts()
        {
            Debug.Log("<color=cyan>[BuildTest]</color> Đang kiểm tra biên dịch C# Scripts cho bản Build Standalone...");

            string outputFolder = Path.Combine("Temp", "PlayerScriptBuildTest");
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            var settings = new ScriptCompilationSettings
            {
                group = BuildPipeline.GetBuildTargetGroup(BuildTarget.StandaloneWindows64),
                target = BuildTarget.StandaloneWindows64,
                options = ScriptCompilationOptions.None
            };

            var result = PlayerBuildInterface.CompilePlayerScripts(settings, outputFolder);
            
            // Nếu có lỗi biên dịch thì assemblies rỗng hoặc bị ném ngoại lệ
            if (result.assemblies != null && result.assemblies.Count > 0)
            {
                Debug.Log($"<color=green>[BuildTest]</color> ✅ TẤT CẢ C# SCRIPTS BIÊN DỊCH CHO STANDALONE PLAYER THÀNH CÔNG 100%! ({result.assemblies.Count} assemblies đã tạo, 0 Lỗi)");
            }
            else
            {
                Debug.LogError("<color=red>[BuildTest]</color> ❌ Biên dịch scripts thất bại!");
            }
        }
    }
}
#endif
