using UnityEngine;

/// <summary>
/// Lớp AI quái vật cận chiến kế thừa SkeletonWarriorAI.
/// Giữ lại lớp này để tương thích 100% với các Prefab SkeletonWarrior/Minion đã tạo trước đó trong dự án.
/// Khuyên dùng: Sử dụng trực tiếp [SkeletonWarriorAI] cho quái cận chiến và [SkeletonRogueAI] cho quái bắn cung.
/// </summary>
[AddComponentMenu("Combat/Monsters/Enemy AI (Legacy Melee)")]
public class EnemyAI : SkeletonWarriorAI
{
}