using UnityEngine;
using UnityEditor;
using System.IO;

public static class CauldronSetupEditor
{
    private const string CAULDRON_PREFAB_PATH = "Assets/Prefabs/Dungeon/DungeonPrefabs/Cauldron_Fire.prefab";

    [MenuItem("Infinite Labyrinth/Boss Arena/Cấu Hình Prefab Vạc Lửa (Cauldron_Fire)")]
    public static void EnsureCauldronSetup()
    {
        if (!File.Exists(CAULDRON_PREFAB_PATH)) return;

        GameObject prefabContents = PrefabUtility.LoadPrefabContents(CAULDRON_PREFAB_PATH);
        if (prefabContents == null) return;

        try
        {
            bool isDirty = false;

            // 1. Cấu hình đèn Point Light tại child "Light"
            Transform lightChild = prefabContents.transform.Find("Light");
            if (lightChild != null)
            {
                // Đảm bảo Light có scale 1,1,1
                if (lightChild.localScale != Vector3.one)
                {
                    lightChild.localScale = Vector3.one;
                    isDirty = true;
                }

                Light lightComp = lightChild.GetComponent<Light>();
                if (lightComp == null)
                {
                    lightComp = lightChild.gameObject.AddComponent<Light>();
                    isDirty = true;
                }

                if (lightComp.type != LightType.Point || lightComp.range < 30f || lightComp.intensity < 10f)
                {
                    lightComp.type = LightType.Point;
                    lightComp.color = new Color(1.0f, 0.58f, 0.16f, 1.0f); // Warm Amber Fire
                    lightComp.range = 35.0f;
                    lightComp.intensity = 18.0f;
                    isDirty = true;
                }
            }

            // 2. Cấu hình CapsuleCollider chống người chơi và quái đi xuyên qua vạc
            BoxCollider oldBox = prefabContents.GetComponent<BoxCollider>();
            if (oldBox != null)
            {
                Object.DestroyImmediate(oldBox, true);
                isDirty = true;
            }

            CapsuleCollider col = prefabContents.GetComponent<CapsuleCollider>();
            if (col == null)
            {
                col = prefabContents.AddComponent<CapsuleCollider>();
                isDirty = true;
            }

            float scaleFactor = Mathf.Max(prefabContents.transform.localScale.x, 1f);
            float targetRadius = 1.25f / scaleFactor;
            float targetHeight = 3.2f / scaleFactor;
            Vector3 targetCenter = new Vector3(0f, 1.6f / scaleFactor, 0f);

            if (Mathf.Abs(col.radius - targetRadius) > 0.01f || Mathf.Abs(col.height - targetHeight) > 0.01f)
            {
                col.radius = targetRadius;
                col.height = targetHeight;
                col.center = targetCenter;
                isDirty = true;
            }

            // Đặt lại localPosition của prefab về (0,0,0) nếu đang bị lệch
            if (prefabContents.transform.localPosition != Vector3.zero)
            {
                prefabContents.transform.localPosition = Vector3.zero;
                isDirty = true;
            }

            // 3. Cấu hình NavMeshModifier đục lỗ NavMesh tại chân vạc
            var navMod = prefabContents.GetComponent<Unity.AI.Navigation.NavMeshModifier>();
            if (navMod == null)
            {
                navMod = prefabContents.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
                navMod.overrideArea = true;
                navMod.area = 1; // 1 = Not Walkable
                isDirty = true;
            }

            if (isDirty)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabContents, CAULDRON_PREFAB_PATH);
                Debug.Log("<color=green>[CauldronSetupEditor]</color> Đã cấu hình thành công đèn Point Light vàng cam (range 35m, intensity 18) và Collider cho Cauldron_Fire.prefab!");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CauldronSetupEditor] Lỗi cấu hình Cauldron_Fire: {ex.Message}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }
    }
}
