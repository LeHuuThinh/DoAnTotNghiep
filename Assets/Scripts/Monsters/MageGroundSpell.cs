using System.Collections;
using UnityEngine;

/// <summary>
/// Vòng tròn ma pháp dưới chân Player phong cách Souls-like:
/// - Khi xuất hiện: Khung viền đỏ (outer ring) cố định kích thước bán kính.
/// - Vùng đỏ bên trong (inner fill) nở to dần từ tâm ra ngoài theo thời gian niệm phép (chargeDuration).
/// - Khi chạm viền đỏ -> Bộc phát sát thương toàn vùng + kích hoạt VFX chiêu thức (cột sáng / nổ phép).
/// - Người chơi nhìn thấy vòng tròn cảnh báo có đủ thời gian để lăn (Roll) hoặc chạy thoát ra ngoài!
/// </summary>
public class MageGroundSpell : MonoBehaviour
{
  [Header("Spell Settings")]
  [SerializeField] private float radius = 2.8f;
  [SerializeField] private float chargeDuration = 1.3f;
  [SerializeField] private float damage = 35f;
  [SerializeField] private float knockback = 2.0f;
  [Tooltip("Layer mục tiêu nhận sát thương (Bắt buộc chọn Player)")]
  [SerializeField] private LayerMask targetLayers;
  [Tooltip("Layer mặt sàn để vòng tròn tự dính sát mặt đất")]
  [SerializeField] private LayerMask groundLayers = 1; // Default layer

  [Header("Explosion VFX")]
  [Tooltip("Prefab hiệu ứng bộc phát khi vòng tròn nạp đầy (ví dụ: FX_LightPillar hoặc cột nổ phép)")]
  [SerializeField] private GameObject explosionVfxPrefab;

  [Header("Telegraph Visuals (Tùy chọn)")]
  [SerializeField] private Color borderColor = new Color(1f, 0.15f, 0.15f, 0.95f);
  [SerializeField] private Color fillColor = new Color(1f, 0.15f, 0.15f, 0.35f);
  [Tooltip("Nếu để trống, code tự tạo LineRenderer khung viền đỏ chuẩn")]
  [SerializeField] private Transform customOuterRing;
  [Tooltip("Nếu để trống, code tự tạo đĩa tròn đỏ nở to dần")]
  [SerializeField] private Transform customInnerFill;

  private Transform attacker;
  private float timer = 0f;
  private bool hasExploded = false;
  private Transform outerRingTransform;
  private Transform innerFillTransform;

  private void Awake()
  {
    // 1. Tự động tìm Layer Player nếu inspector chưa chọn
    if (targetLayers.value == 0)
    {
      int playerLayer = LayerMask.NameToLayer("Player");
      if (playerLayer != -1) targetLayers = 1 << playerLayer;
    }

    // 2. Thiết lập Visual cho Khung viền và Vùng nở bên trong
    SetupVisuals();
  }

  public void Initialize(Transform attackerRoot, float customRadius = -1f, float customDuration = -1f, float customDamage = -1f)
  {
    attacker = attackerRoot;

    if (customRadius > 0f) radius = customRadius;
    if (customDuration > 0f) chargeDuration = customDuration;
    if (customDamage > 0f) damage = customDamage;

    // Cập nhật lại khung viền theo bán kính mới nếu đã khởi tạo
    if (outerRingTransform != null)
    {
      LineRenderer line = outerRingTransform.GetComponent<LineRenderer>();
      if (line != null)
      {
        UpdateRingVertices(line, radius);
      }
      else
      {
        outerRingTransform.localScale = new Vector3(radius * 2f, 1f, radius * 2f);
      }
    }

    SnapToGround();
  }

  /// <summary>
  /// Bắn tia xuống sàn để vòng tròn nằm phẳng và dính sát mặt đất, chống bay lơ lửng hay chìm vào sàn
  /// </summary>
  private void SnapToGround()
  {
    if (Physics.Raycast(transform.position + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, 4.0f, groundLayers))
    {
      transform.position = hit.point + hit.normal * 0.02f;
      transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
    }
  }

  private void SetupVisuals()
  {
    // A. Khung viền đỏ (Outer Border)
    if (customOuterRing != null)
    {
      outerRingTransform = customOuterRing;
      outerRingTransform.localScale = new Vector3(radius * 2f, 1f, radius * 2f);
    }
    else
    {
      outerRingTransform = CreateProceduralOuterRing();
    }

    // B. Vùng ruột đỏ nở to dần (Inner Fill)
    if (customInnerFill != null)
    {
      innerFillTransform = customInnerFill;
    }
    else
    {
      innerFillTransform = CreateProceduralInnerFill();
    }

    if (innerFillTransform != null)
    {
      innerFillTransform.localScale = Vector3.zero;
    }
  }

  private void Update()
  {
    if (hasExploded) return;

    timer += Time.deltaTime;
    float progress = Mathf.Clamp01(timer / chargeDuration);

    // Mở rộng phần ruột đỏ bên trong từ tâm chạm dần tới khung viền ngoài
    if (innerFillTransform != null)
    {
      float currentR = Mathf.Max(progress * radius, 0.001f);
      innerFillTransform.localScale = new Vector3(currentR, 1f, currentR);
    }

    // Khi chạm khung đỏ -> Kích hoạt kỹ năng bộc phát!
    if (timer >= chargeDuration)
    {
      TriggerExplosion();
    }
  }

  private void TriggerExplosion()
  {
    hasExploded = true;

    // 1. Quét gây sát thương lên người chơi nếu còn đứng trong vòng tròn
    Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius, targetLayers);
    foreach (Collider col in hitColliders)
    {
      if (attacker != null && col.transform.root == attacker) continue;

      IDamageable damageable = col.GetComponentInParent<IDamageable>();
      if (damageable != null)
      {
        Vector3 hitPoint = col.ClosestPoint(transform.position);
        Vector3 knockbackDir = (col.transform.position - transform.position).normalized;
        knockbackDir.y = 0.35f; // Hất nhẹ người chơi lên

        damageable.TakeDamage(damage, hitPoint, knockbackDir, knockback);
      }
    }

    // 2. Kích hoạt hiệu ứng bộc phát (VFX)
    if (explosionVfxPrefab != null)
    {
      Instantiate(explosionVfxPrefab, transform.position, Quaternion.identity);
    }

    // 3. Hủy vòng tròn cảnh báo
    Destroy(gameObject);
  }

  // =========================================================================
  // TẠO HÌNH THỂ PROCEDURAL TỰ ĐỘNG NẾU KHÔNG CÓ PREFAB CON
  // =========================================================================

  private Transform CreateProceduralOuterRing()
  {
    GameObject ringObj = new GameObject("ProceduralOuterRing");
    ringObj.transform.SetParent(transform, false);
    ringObj.transform.localPosition = new Vector3(0f, 0.025f, 0f);

    LineRenderer line = ringObj.AddComponent<LineRenderer>();
    line.useWorldSpace = false;
    line.loop = true;
    line.positionCount = 64;
    line.startWidth = 0.09f;
    line.endWidth = 0.09f;

    Material mat = GetTransparentUnlitMaterial();
    ApplyColorToMaterial(mat, borderColor);
    line.material = mat;
    line.startColor = borderColor;
    line.endColor = borderColor;

    UpdateRingVertices(line, radius);

    return ringObj.transform;
  }

  private void UpdateRingVertices(LineRenderer line, float r)
  {
    if (line == null) return;
    float angleStep = 360f / 64f;
    for (int i = 0; i < 64; i++)
    {
      float rad = Mathf.Deg2Rad * (i * angleStep);
      line.SetPosition(i, new Vector3(Mathf.Cos(rad) * r, 0f, Mathf.Sin(rad) * r));
    }
  }

  private Transform CreateProceduralInnerFill()
  {
    GameObject fillObj = new GameObject("ProceduralInnerFill");
    fillObj.transform.SetParent(transform, false);
    fillObj.transform.localPosition = new Vector3(0f, 0.02f, 0f);

    MeshFilter mf = fillObj.AddComponent<MeshFilter>();
    MeshRenderer mr = fillObj.AddComponent<MeshRenderer>();

    int segments = 64;
    Vector3[] vertices = new Vector3[segments + 1];
    Vector3[] normals = new Vector3[segments + 1];
    Vector2[] uvs = new Vector2[segments + 1];
    Color[] colors = new Color[segments + 1];
    int[] triangles = new int[segments * 3];

    // Tạo hình đĩa tròn đơn vị (radius = 1.0f) để Update scale theo bán kính chính xác
    vertices[0] = Vector3.zero; // Tâm hình tròn
    normals[0] = Vector3.up;
    uvs[0] = new Vector2(0.5f, 0.5f);
    colors[0] = fillColor;

    float angleStep = 360f / segments;
    for (int i = 0; i < segments; i++)
    {
      float rad = Mathf.Deg2Rad * (i * angleStep);
      float cos = Mathf.Cos(rad);
      float sin = Mathf.Sin(rad);

      vertices[i + 1] = new Vector3(cos, 0f, sin);
      normals[i + 1] = Vector3.up;
      uvs[i + 1] = new Vector2(cos * 0.5f + 0.5f, sin * 0.5f + 0.5f);
      colors[i + 1] = fillColor;
    }

    // Đảo thứ tự winding để tam giác quay mặt lên trên (+Y / Clockwise trong hệ toạ độ Unity)
    for (int i = 0; i < segments; i++)
    {
      triangles[i * 3] = 0;
      triangles[i * 3 + 1] = (i + 1) >= segments ? 1 : i + 2;
      triangles[i * 3 + 2] = i + 1;
    }

    Mesh mesh = new Mesh();
    mesh.name = "UnitDiscMesh";
    mesh.vertices = vertices;
    mesh.normals = normals;
    mesh.uv = uvs;
    mesh.colors = colors;
    mesh.triangles = triangles;
    mesh.RecalculateBounds();

    mf.mesh = mesh;

    Material mat = GetTransparentUnlitMaterial();
    ApplyColorToMaterial(mat, fillColor);
    mr.material = mat;

    return fillObj.transform;
  }

  private Material GetTransparentUnlitMaterial()
  {
    Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
    if (shader == null) shader = Shader.Find("Sprites/Default");
    if (shader == null) shader = Shader.Find("Unlit/Color");
    if (shader == null) shader = Shader.Find("UI/Default");

    Material mat = new Material(shader);

    // Cấu hình vật liệu trong suốt chuẩn cho cả URP và Built-in Pipeline
    if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // 1 = Transparent
    if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0f); // 0 = Alpha blend
    if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
    if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
    if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);
    if (mat.HasProperty("_Cull")) mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off); // Hiển thị cả 2 mặt

    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

    return mat;
  }

  private void ApplyColorToMaterial(Material mat, Color col)
  {
    if (mat == null) return;
    mat.color = col;
    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
    if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
  }

  private void OnDrawGizmosSelected()
  {
    Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.3f);
    Gizmos.DrawSphere(transform.position, radius);
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, radius);
  }
}
