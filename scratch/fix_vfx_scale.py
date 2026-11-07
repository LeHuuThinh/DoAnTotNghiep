import re

# 1. Update BossVictoryDirector.cs
bvd_path = "Assets/Scripts/Interaction/BossVictoryDirector.cs"
with open(bvd_path, "r", encoding="utf-8") as f:
    code = f.read()

# Replace field definition
old_field = '''    [Tooltip("Tỉ lệ phóng to vòng tròn ma thuật (mặc định 3.2f để bao phủ trọn vẹn cả Rương Boss và Cổng Dịch Chuyển)")]
    [SerializeField] private float lootSpawnVfxScale = 3.2f;'''

new_field = '''    [Tooltip("Hệ số nhân tỉ lệ scale của vòng tròn ma thuật (mặc định 1.0f để dùng trực tiếp kích thước thiết lập trong Prefab BossLootSpawnVFX)")]
    [SerializeField] private float lootSpawnVfxScaleMultiplier = 1.0f;'''

if old_field in code:
    code = code.replace(old_field, new_field)
    print("Replaced field in BossVictoryDirector.cs")
else:
    print("WARNING: old_field not found exactly, trying regex")
    code = re.sub(
        r'\[Tooltip\("Tỉ lệ phóng to vòng tròn ma thuật.*?"\)\]\s*\[SerializeField\]\s*private float lootSpawnVfxScale\s*=\s*[^;]+;',
        new_field,
        code
    )

# Replace localScale assignment
old_assign = '''            spawnedLootVfx.name = "BossLootSpawnVFX";
            spawnedLootVfx.transform.localScale = Vector3.one * lootSpawnVfxScale;
            spawnedLootVfxInstance = spawnedLootVfx;'''

new_assign = '''            spawnedLootVfx.name = "BossLootSpawnVFX";
            // Lấy trực tiếp scale thiết lập từ Prefab gốc (nếu bạn chỉnh scale trong Prefab thì sẽ tự động áp dụng)
            Vector3 basePrefabScale = bossLootSpawnVfxPrefab.transform.localScale;
            float scaleMult = lootSpawnVfxScaleMultiplier > 0f ? lootSpawnVfxScaleMultiplier : 1.0f;
            spawnedLootVfx.transform.localScale = basePrefabScale * scaleMult;
            spawnedLootVfxInstance = spawnedLootVfx;'''

if old_assign in code:
    code = code.replace(old_assign, new_assign)
    print("Replaced scale assignment in BossVictoryDirector.cs")
else:
    print("WARNING: old_assign not found exactly, trying regex")
    code = re.sub(
        r'spawnedLootVfx\.transform\.localScale\s*=\s*Vector3\.one\s*\*\s*lootSpawnVfxScale;',
        '''// Lấy trực tiếp scale thiết lập từ Prefab gốc (nếu bạn chỉnh scale trong Prefab thì sẽ tự động áp dụng)
            Vector3 basePrefabScale = bossLootSpawnVfxPrefab.transform.localScale;
            float scaleMult = lootSpawnVfxScaleMultiplier > 0f ? lootSpawnVfxScaleMultiplier : 1.0f;
            spawnedLootVfx.transform.localScale = basePrefabScale * scaleMult;''',
        code
    )

with open(bvd_path, "w", encoding="utf-8") as f:
    f.write(code)

print("Updated BossVictoryDirector.cs successfully.")

# 2. Update DungeonScene.unity
scene_path = "Assets/Scenes/DungeonScene.unity"
with open(scene_path, "r", encoding="utf-8") as f:
    scene_text = f.read()

scene_text = scene_text.replace("lootSpawnVfxScale: 3.2", "lootSpawnVfxScaleMultiplier: 1")

with open(scene_path, "w", encoding="utf-8") as f:
    f.write(scene_text)

print("Updated DungeonScene.unity successfully.")
