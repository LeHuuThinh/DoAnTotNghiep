#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using InfiniteLabyrinth.Interaction;

namespace InfiniteLabyrinth.EditorTools
{
  [InitializeOnLoad]
  public static class BossGateSetupEditor
  {
    private const string PREFAB_PATH = "Assets/Prefabs/Dungeon/Interactable/BossGate.prefab";

    static BossGateSetupEditor()
    {
      EditorApplication.delayCall += GenerateBossGatePrefab;
    }

    [MenuItem("Tools/Infinite Labyrinth/Generate Boss Gate Prefab")]
    public static void GenerateBossGatePrefab()
    {
      if (File.Exists(PREFAB_PATH))
      {
        return;
      }
      DoGenerate();
    }

    [MenuItem("Tools/Infinite Labyrinth/Force Regenerate Boss Gate Prefab")]
    public static void ForceGenerate()
    {
      DoGenerate();
    }

    private static void DoGenerate()
    {
      // 1. Tải tài nguyên mẫu
      Material textureMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Prefabs/Dungeon/DungeonPrefabs/texture.mat");
      Material smokeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Origin/Polygonal Particles/Assets/Materials/WhiteSmoke.mat")
                       ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Eric VFX Studio/Resource/Materials/FX_smoke_02.mat");

      GameObject doorwayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Dungeon/DungeonPrefabs/wall_doorway.prefab");
      GameObject pillarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Dungeon/DungeonPrefabs/pillar.prefab");
      GameObject torchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Dungeon/DungeonPrefabs/torch_wall_lit.prefab");

      Mesh doorwayMesh = doorwayPrefab != null ? doorwayPrefab.GetComponentInChildren<MeshFilter>()?.sharedMesh : null;
      if (doorwayMesh == null)
      {
        doorwayMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Prefabs/Dungeon/Assets/obj/wall_doorway.obj");
      }

      Mesh doorMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Prefabs/Dungeon/Assets/obj/wall_doorway_door.obj");
      Mesh pillarMesh = pillarPrefab != null ? pillarPrefab.GetComponentInChildren<MeshFilter>()?.sharedMesh : null;

      if (doorwayMesh == null || doorMesh == null)
      {
        Debug.LogError("[BossGateSetupEditor] Không tìm thấy mesh wall_doorway hoặc wall_doorway_door!");
        return;
      }

      // 2. Tạo Root GameObject
      GameObject root = new GameObject("BossGate");
      root.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);

      // NavMesh Modifier: Bỏ qua khi bake tĩnh để sàn dưới cửa luôn liền mạch
      var navMod = root.AddComponent<Unity.AI.Navigation.NavMeshModifier>();
      navMod.ignoreFromBuild = true;

      // BoxCollider chắn cửa
      BoxCollider doorCol = root.AddComponent<BoxCollider>();
      doorCol.center = new Vector3(0f, 1.4f, 0f);
      doorCol.size = new Vector3(2.2f, 2.8f, 0.6f);

      // NavMeshObstacle đục lỗ động khi cửa đóng
      NavMeshObstacle doorNav = root.AddComponent<NavMeshObstacle>();
      doorNav.center = new Vector3(0f, 1.4f, 0f);
      doorNav.size = new Vector3(2.2f, 2.8f, 0.6f);
      doorNav.carving = true;

      // 3. Khung cổng đá (Frame)
      GameObject frameObj = new GameObject("Frame");
      frameObj.transform.SetParent(root.transform, false);
      MeshFilter frameMf = frameObj.AddComponent<MeshFilter>();
      frameMf.sharedMesh = doorwayMesh;
      MeshRenderer frameMr = frameObj.AddComponent<MeshRenderer>();
      frameMr.sharedMaterial = textureMat;

      // 2 Cột đá hoành tráng kẹp 2 bên (Flanking Pillars) để bịt kín đại lộ 12m
      if (pillarMesh != null)
      {
        GameObject leftPillar = new GameObject("Left_Pillar");
        leftPillar.transform.SetParent(frameObj.transform, false);
        leftPillar.transform.localPosition = new Vector3(-2.1f, 0f, 0f);
        MeshFilter lpMf = leftPillar.AddComponent<MeshFilter>();
        lpMf.sharedMesh = pillarMesh;
        MeshRenderer lpMr = leftPillar.AddComponent<MeshRenderer>();
        lpMr.sharedMaterial = textureMat;
        BoxCollider lpCol = leftPillar.AddComponent<BoxCollider>();
        lpCol.center = new Vector3(0f, 2f, 0f);
        lpCol.size = new Vector3(1.2f, 4f, 1.2f);

        GameObject rightPillar = new GameObject("Right_Pillar");
        rightPillar.transform.SetParent(frameObj.transform, false);
        rightPillar.transform.localPosition = new Vector3(2.1f, 0f, 0f);
        MeshFilter rpMf = rightPillar.AddComponent<MeshFilter>();
        rpMf.sharedMesh = pillarMesh;
        MeshRenderer rpMr = rightPillar.AddComponent<MeshRenderer>();
        rpMr.sharedMaterial = textureMat;
        BoxCollider rpCol = rightPillar.AddComponent<BoxCollider>();
        rpCol.center = new Vector3(0f, 2f, 0f);
        rpCol.size = new Vector3(1.2f, 4f, 1.2f);

        // Gắn 2 ngọn đuốc rực sáng trên 2 cột đá
        if (torchPrefab != null)
        {
          GameObject leftTorch = (GameObject)PrefabUtility.InstantiatePrefab(torchPrefab, leftPillar.transform);
          leftTorch.name = "Pillar_Torch";
          leftTorch.transform.localPosition = new Vector3(0f, 2.2f, -0.45f);
          leftTorch.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

          GameObject rightTorch = (GameObject)PrefabUtility.InstantiatePrefab(torchPrefab, rightPillar.transform);
          rightTorch.name = "Pillar_Torch";
          rightTorch.transform.localPosition = new Vector3(0f, 2.2f, -0.45f);
          rightTorch.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        }
      }

      // 4. Cánh cửa đôi (Double Doors)
      // Cánh trái
      GameObject leftHinge = new GameObject("Left_Hinge");
      leftHinge.transform.SetParent(root.transform, false);
      leftHinge.transform.localPosition = new Vector3(-1.0f, 0f, 0f);

      GameObject leftDoor = new GameObject("Left_Door_Wing");
      leftDoor.transform.SetParent(leftHinge.transform, false);
      leftDoor.transform.localPosition = new Vector3(0.5f, 0f, 0f);
      leftDoor.transform.localScale = new Vector3(0.5f, 1f, 1f);
      MeshFilter ldMf = leftDoor.AddComponent<MeshFilter>();
      ldMf.sharedMesh = doorMesh;
      MeshRenderer ldMr = leftDoor.AddComponent<MeshRenderer>();
      ldMr.sharedMaterial = textureMat;

      // Cánh phải
      GameObject rightHinge = new GameObject("Right_Hinge");
      rightHinge.transform.SetParent(root.transform, false);
      rightHinge.transform.localPosition = new Vector3(1.0f, 0f, 0f);

      GameObject rightDoor = new GameObject("Right_Door_Wing");
      rightDoor.transform.SetParent(rightHinge.transform, false);
      rightDoor.transform.localPosition = new Vector3(-0.5f, 0f, 0f);
      rightDoor.transform.localScale = new Vector3(-0.5f, 1f, 1f);
      MeshFilter rdMf = rightDoor.AddComponent<MeshFilter>();
      rdMf.sharedMesh = doorMesh;
      MeshRenderer rdMr = rightDoor.AddComponent<MeshRenderer>();
      rdMr.sharedMaterial = textureMat;

      // 5. Điểm tương tác InteractionTransform
      GameObject interactPt = new GameObject("InteractionPoint");
      interactPt.transform.SetParent(root.transform, false);
      interactPt.transform.localPosition = new Vector3(0f, 1.2f, -1.0f);

      // 6. Cổng Sương Mù (Fog Gate Lock-in)
      GameObject fogGate = new GameObject("FogGate");
      fogGate.transform.SetParent(root.transform, false);
      fogGate.transform.localPosition = Vector3.zero;

      // Particle System tạo màn sương mù huyền bí cuồn cuộn
      GameObject mistObj = new GameObject("FogMist_Particles");
      mistObj.transform.SetParent(fogGate.transform, false);
      mistObj.transform.localPosition = new Vector3(0f, 1.3f, 0f);
      ParticleSystem ps = mistObj.AddComponent<ParticleSystem>();
      ParticleSystemRenderer psRenderer = mistObj.GetComponent<ParticleSystemRenderer>();
      if (smokeMat != null) psRenderer.sharedMaterial = smokeMat;

      var main = ps.main;
      main.loop = true;
      main.playOnAwake = true;
      main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 3.8f);
      main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
      main.startSize = new ParticleSystem.MinMaxCurve(1.2f, 2.0f);
      main.startColor = new Color(0.85f, 0.95f, 1.0f, 0.65f); // Sương trắng ánh băng huyền ảo
      main.maxParticles = 80;

      var emission = ps.emission;
      emission.rateOverTime = 30f;

      var shape = ps.shape;
      shape.shapeType = ParticleSystemShapeType.Box;
      shape.scale = new Vector3(2.0f, 2.5f, 0.4f);

      var velOverLifetime = ps.velocityOverLifetime;
      velOverLifetime.enabled = true;
      velOverLifetime.y = new ParticleSystem.MinMaxCurve(0.3f, 0.7f); // Khói bốc lên nhẹ nhàng

      var colOverLifetime = ps.colorOverLifetime;
      colOverLifetime.enabled = true;
      Gradient grad = new Gradient();
      grad.SetKeys(
        new GradientColorKey[] { new GradientColorKey(new Color(0.7f, 0.9f, 1.0f), 0f), new GradientColorKey(Color.white, 1f) },
        new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.7f, 0.4f), new GradientAlphaKey(0f, 1f) }
      );
      colOverLifetime.color = grad;

      // Ánh sáng xanh lơ lạnh lẽo xuyên qua màn sương
      GameObject fogLightObj = new GameObject("FogLight");
      fogLightObj.transform.SetParent(fogGate.transform, false);
      fogLightObj.transform.localPosition = new Vector3(0f, 1.5f, 0f);
      Light fLight = fogLightObj.AddComponent<Light>();
      fLight.type = LightType.Point;
      fLight.color = new Color(0.45f, 0.8f, 1.0f);
      fLight.range = 8f;
      fLight.intensity = 2.5f;

      // Collider rào chắn cổng sương mù
      BoxCollider fogCol = fogGate.AddComponent<BoxCollider>();
      fogCol.center = new Vector3(0f, 1.4f, 0f);
      fogCol.size = new Vector3(2.2f, 2.8f, 0.6f);

      // NavMeshObstacle của cổng sương mù
      NavMeshObstacle fogNav = fogGate.AddComponent<NavMeshObstacle>();
      fogNav.center = new Vector3(0f, 1.4f, 0f);
      fogNav.size = new Vector3(2.2f, 2.8f, 0.6f);
      fogNav.carving = true;

      // Ban đầu cổng sương mù tắt
      fogGate.SetActive(false);

      // 7. Arena Trigger (đặt cách cửa 2.6m trong local space = 6.5m trong world space vào bên trong phòng Boss)
      GameObject triggerObj = new GameObject("ArenaTrigger");
      triggerObj.transform.SetParent(root.transform, false);
      triggerObj.transform.localPosition = new Vector3(0f, 1.5f, 2.6f);

      BoxCollider trigCol = triggerObj.AddComponent<BoxCollider>();
      trigCol.isTrigger = true;
      trigCol.center = Vector3.zero;
      trigCol.size = new Vector3(4.8f, 3.5f, 2.0f); // Phủ trọn 12m chiều ngang đại lộ

      BossArenaTrigger arenaTrig = triggerObj.AddComponent<BossArenaTrigger>();
      arenaTrig.SetGateController(null); // Sẽ được gán qua controller

      // 8. Gán tham chiếu cho BossGateController
      BossGateController controller = root.AddComponent<BossGateController>();
      controller.InitializeReferences(
        leftHinge.transform, rightHinge.transform,
        doorCol, doorNav,
        fogGate, fogCol, fogNav,
        interactPt.transform
      );
      arenaTrig.SetGateController(controller);

      // Đồng bộ SerializedObject để lưu vĩnh viễn vào file Prefab
      SerializedObject so = new SerializedObject(controller);
      so.FindProperty("leftDoorHinge").objectReferenceValue = leftHinge.transform;
      so.FindProperty("rightDoorHinge").objectReferenceValue = rightHinge.transform;
      so.FindProperty("doorObstacleCollider").objectReferenceValue = doorCol;
      so.FindProperty("doorNavObstacle").objectReferenceValue = doorNav;
      so.FindProperty("fogGateRoot").objectReferenceValue = fogGate;
      so.FindProperty("fogBlockerCollider").objectReferenceValue = fogCol;
      so.FindProperty("fogNavObstacle").objectReferenceValue = fogNav;
      so.FindProperty("interactionTransform").objectReferenceValue = interactPt.transform;
      so.ApplyModifiedPropertiesWithoutUndo();

      SerializedObject soTrig = new SerializedObject(arenaTrig);
      soTrig.FindProperty("gateController").objectReferenceValue = controller;
      soTrig.ApplyModifiedPropertiesWithoutUndo();

      // 9. Lưu Prefab vào Assets
      string dir = Path.GetDirectoryName(PREFAB_PATH);
      if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

      PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
      Object.DestroyImmediate(root);

      AssetDatabase.SaveAssets();
      AssetDatabase.Refresh();

      Debug.Log($"<color=green>[BossGateSetupEditor]</color> ✅ Đã sinh thành công BossGate.prefab tại: {PREFAB_PATH}");
    }
  }
}
#endif
