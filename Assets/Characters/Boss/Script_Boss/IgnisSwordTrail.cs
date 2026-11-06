using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hiệu ứng vệt chém đại kiếm (Melee Weapon Trail) gán trực tiếp trên thân thanh đại kiếm của Boss Red Ignis.
/// Tạo ra dải ribbon phát sáng màu ĐỎ THẪM NHẠT kết hợp viền ĐEN KHÓI bám sát 100% chuyển động 3D thực tế của lưỡi kiếm.
/// Tự động tan biến nhanh gọn (0.16s), tích hợp bộ lọc chống rung giật lên xuống (Anti-Jitter Smoothing) và Catmull-Rom Spline.
/// CHỈ kích hoạt khi xuất chiêu; hoàn toàn TẮT khi đứng yên hoặc di chuyển.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class IgnisSwordTrail : MonoBehaviour
{
    private struct TrailPoint
    {
        public Vector3 basePos;
        public Vector3 tipPos;
        public float time;

        public TrailPoint(Vector3 b, Vector3 t, float tm)
        {
            basePos = b;
            tipPos = t;
            time = tm;
        }
    }

    [Header("=== TỌA ĐỘ LƯỠI KIẾM (BLADE SPAN) ===")]
    [Tooltip("Điểm gốc lưỡi kiếm (gần chuôi). Nếu để trống sẽ dùng baseOffset")]
    [SerializeField] private Transform bladeBase;

    [Tooltip("Điểm mũi kiếm (đầu nhọn). Nếu để trống sẽ dùng tipOffset")]
    [SerializeField] private Transform bladeTip;

    [Tooltip("Tọa độ cục bộ của gốc lưỡi kiếm (gần đốc kiếm/chuôi)")]
    [SerializeField] private Vector3 baseOffset = new Vector3(0f, 0f, 0.35f);

    [Tooltip("Tọa độ cục bộ của mũi kiếm")]
    [SerializeField] private Vector3 tipOffset = new Vector3(0f, 0f, 1.45f);

    [Header("=== CẤU HÌNH VỆT KIẾM (TRAIL SETTINGS) ===")]
    [Tooltip("Thời gian tồn tại của vệt kiếm trong không khí trước khi biến mất (giây). Mặc định: 0.16s cho độ biến mất nhanh và gọn")]
    [Range(0.05f, 0.60f)]
    [SerializeField] private float trailDuration = 0.16f;

    [Tooltip("Khoảng cách di chuyển tối thiểu giữa 2 điểm lấy mẫu (mét)")]
    [SerializeField] private float minVertexDistance = 0.02f;

    [Tooltip("Vật liệu vệt chém lửa đỏ thẫm viền đen (Ignis_Sword_Trail.mat)")]
    [SerializeField] private Material trailMaterial;

    [Tooltip("Dải màu gradient từ lưỡi kiếm đến đuôi vệt chém: Đỏ thẫm nhạt -> Đỏ đậm -> Đen khói")]
    [SerializeField] private Gradient trailGradient;

    [Header("=== CHỐNG RUNG GIẬT & LÀM MƯỢT (ANTI-JITTER SMOOTHING) ===")]
    [Tooltip("Mức độ làm mượt dao động tay kiếm (0 = bám 100% thô, 0.9 = mượt tối đa). Lọc sạch các rung động micro-jitter của animation tay")]
    [Range(0f, 0.90f)]
    [SerializeField] private float motionSmoothing = 0.45f;

    [Tooltip("Mức độ triệt tiêu rung giật lên xuống theo trục thẳng đứng (Y-Axis Jitter Damping). Giúp vệt chém phẳng đều, không bị nhấp nhô")]
    [Range(0f, 1.0f)]
    [SerializeField] private float verticalJitterDamping = 0.35f;

    [Tooltip("Sử dụng đường cong mượt Catmull-Rom Spline để nối các điểm lấy mẫu, tạo vệt chém uốn lượn hình vòng cung không bị gãy khúc")]
    [SerializeField] private bool useSplineSmoothing = true;

    [Tooltip("Số bước phân chia nội suy spline giữa 2 frame (2 - 4 bước mang lại vệt chém uốn lượn hoàn hảo)")]
    [Range(1, 5)]
    [SerializeField] private int splineSubdivisions = 3;

    [Tooltip("Ngưỡng vận tốc di chuyển tối thiểu của mũi kiếm để bắt đầu ghi nhận vệt (m/s). Tránh sinh vệt vụn khi tay chỉ rung nhẹ tại chỗ")]
    [SerializeField] private float minVelocityThreshold = 0.35f;

    [Header("=== HIỆU ỨNG ĐẦU MŨI KIẾM (TÙY CHỌN) ===")]
    [Tooltip("TrailRenderer bổ trợ ở đầu mũi kiếm (tùy chọn)")]
    [SerializeField] private TrailRenderer tipTrail;

    // Trạng thái runtime
    private bool isEmitting = false;
    private readonly List<TrailPoint> rawSamples = new List<TrailPoint>(128);
    private readonly List<TrailPoint> smoothPoints = new List<TrailPoint>(256);
    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Coroutine activePlayRoutine;

    // Lọc làm mượt vị trí
    private Vector3 lastSmoothedBase;
    private Vector3 lastSmoothedTip;
    private bool hasValidLastSmoothed = false;

    // Bộ đệm vertex tái sử dụng để tránh garbage collection
    private readonly List<Vector3> vertices = new List<Vector3>(512);
    private readonly List<Vector2> uvs = new List<Vector2>(512);
    private readonly List<Color> colors = new List<Color>(512);
    private readonly List<int> triangles = new List<int>(1024);

    public bool IsEmitting => isEmitting;
    public float TrailDuration { get => trailDuration; set => trailDuration = Mathf.Clamp(value, 0.05f, 0.6f); }
    public float MotionSmoothing { get => motionSmoothing; set => motionSmoothing = Mathf.Clamp01(value); }
    public float VerticalJitterDamping { get => verticalJitterDamping; set => verticalJitterDamping = Mathf.Clamp01(value); }

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();

        mesh = new Mesh
        {
            name = "IgnisSwordTrail_Mesh"
        };
        mesh.MarkDynamic();
        meshFilter.mesh = mesh;

        if (trailMaterial != null && meshRenderer != null)
        {
            meshRenderer.material = trailMaterial;
        }

        // Tạo gradient mặc định: ĐỎ THẪM NHẠT -> ĐỎ ĐẬM -> ĐEN KHÓI
        bool isDefaultWhite = trailGradient != null && trailGradient.colorKeys != null && trailGradient.colorKeys.Length > 0
            && trailGradient.colorKeys[0].color.r > 0.9f && trailGradient.colorKeys[0].color.g > 0.9f && trailGradient.colorKeys[0].color.b > 0.9f;

        if (trailGradient == null || trailGradient.colorKeys == null || trailGradient.colorKeys.Length <= 2 || isDefaultWhite)
        {
            trailGradient = CreateDefaultCrimsonGradient();
        }

        if (tipTrail != null)
        {
            tipTrail.emitting = false;
        }
    }

    private static Gradient CreateDefaultCrimsonGradient()
    {
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.72f, 0.16f, 0.16f), 0.0f),  // Đỏ thẫm nhạt ở lưỡi kiếm (không bị neon chói)
                new GradientColorKey(new Color(0.42f, 0.05f, 0.05f), 0.35f), // Đỏ máu sẫm trong thân vệt chém
                new GradientColorKey(new Color(0.10f, 0.02f, 0.02f), 0.70f), // Đen khói / than tối
                new GradientColorKey(new Color(0.03f, 0.00f, 0.00f), 1.0f)   // Đen mờ ở đuôi
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.80f, 0.0f),  // Mềm mại, dịu mắt
                new GradientAlphaKey(0.65f, 0.35f),
                new GradientAlphaKey(0.40f, 0.70f),
                new GradientAlphaKey(0.0f, 1.0f)    // Tan biến hoàn toàn về 0
            }
        );
        return g;
    }

    private void OnDisable()
    {
        ClearTrail();
    }

    /// <summary>
    /// Bật hoặc tắt phát vệt kiếm.
    /// Khi tắt, các vệt kiếm đã sinh ra sẽ tiếp tục tan biến tự nhiên rồi biến mất.
    /// </summary>
    public void SetTrailActive(bool active)
    {
        isEmitting = active;

        if (tipTrail != null)
        {
            tipTrail.emitting = active;
        }

        if (active)
        {
            // Khởi tạo điểm lấy mẫu đầu tiên khi bắt đầu đòn chém
            rawSamples.Clear();
            smoothPoints.Clear();

            Vector3 basePos = GetRawBasePosition();
            Vector3 tipPos = GetRawTipPosition();

            lastSmoothedBase = basePos;
            lastSmoothedTip = tipPos;
            hasValidLastSmoothed = true;

            TrailPoint firstPt = new TrailPoint(basePos, tipPos, Time.time);
            rawSamples.Add(firstPt);
            smoothPoints.Add(firstPt);
        }
        else
        {
            hasValidLastSmoothed = false;
        }
    }

    /// <summary>
    /// Kích hoạt vệt kiếm trong khoảng thời gian duration (giây), sau đó tự động tắt.
    /// </summary>
    public void PlayTrailFor(float duration)
    {
        if (activePlayRoutine != null)
        {
            StopCoroutine(activePlayRoutine);
        }
        activePlayRoutine = StartCoroutine(PlayTrailRoutine(duration));
    }

    private IEnumerator PlayTrailRoutine(float duration)
    {
        SetTrailActive(true);
        yield return new WaitForSeconds(duration);
        SetTrailActive(false);
        activePlayRoutine = null;
    }

    /// <summary>
    /// Xóa tức thì toàn bộ vệt kiếm.
    /// </summary>
    public void ClearTrail()
    {
        isEmitting = false;
        rawSamples.Clear();
        smoothPoints.Clear();
        hasValidLastSmoothed = false;

        if (mesh != null)
        {
            mesh.Clear();
        }
        if (tipTrail != null)
        {
            tipTrail.emitting = false;
            tipTrail.Clear();
        }
        if (activePlayRoutine != null)
        {
            StopCoroutine(activePlayRoutine);
            activePlayRoutine = null;
        }
    }

    private void LateUpdate()
    {
        float currentTime = Time.time;

        // 1. LẤY MẪU & LỌC CHỐNG RUNG GIẬT (ANTI-JITTER FILTER)
        if (isEmitting)
        {
            Vector3 rawBase = GetRawBasePosition();
            Vector3 rawTip = GetRawTipPosition();

            Vector3 filteredBase = rawBase;
            Vector3 filteredTip = rawTip;

            if (hasValidLastSmoothed)
            {
                // Bộ lọc làm mượt dao động tay (Low-pass filter)
                if (motionSmoothing > 0.001f)
                {
                    float factor = 1f - motionSmoothing;
                    filteredBase = Vector3.Lerp(lastSmoothedBase, rawBase, factor);
                    filteredTip = Vector3.Lerp(lastSmoothedTip, rawTip, factor);
                }

                // Bộ lọc triệt tiêu rung giật lên xuống ngoài ý muốn (Y-Axis Damping)
                if (verticalJitterDamping > 0.001f)
                {
                    float dampFactor = 1f - verticalJitterDamping;
                    filteredTip.y = Mathf.Lerp(lastSmoothedTip.y, filteredTip.y, dampFactor);
                    filteredBase.y = Mathf.Lerp(lastSmoothedBase.y, filteredBase.y, dampFactor);
                }
            }

            lastSmoothedBase = filteredBase;
            lastSmoothedTip = filteredTip;
            hasValidLastSmoothed = true;

            float dt = Mathf.Max(0.0001f, Time.deltaTime);
            float tipSpeed = 0f;
            if (rawSamples.Count > 0)
            {
                tipSpeed = Vector3.Distance(filteredTip, rawSamples[rawSamples.Count - 1].tipPos) / dt;
            }

            // Chỉ ghi nhận mẫu khi lưỡi kiếm di chuyển đủ nhanh và xa để tránh rung vụn
            if (rawSamples.Count == 0 || (tipSpeed >= minVelocityThreshold && Vector3.Distance(filteredTip, rawSamples[rawSamples.Count - 1].tipPos) >= minVertexDistance))
            {
                rawSamples.Add(new TrailPoint(filteredBase, filteredTip, currentTime));

                // Sinh điểm uốn lượn mượt mà bằng Catmull-Rom Spline
                if (useSplineSmoothing && rawSamples.Count >= 2)
                {
                    AddSplineSegment(rawSamples, splineSubdivisions);
                }
                else
                {
                    smoothPoints.Add(new TrailPoint(filteredBase, filteredTip, currentTime));
                }
            }
        }

        // 2. DỌN DẸP CÁC ĐIỂM HẾT HẠN THEO trailDuration (0.16s)
        while (rawSamples.Count > 0 && (currentTime - rawSamples[0].time) > trailDuration)
        {
            rawSamples.RemoveAt(0);
        }

        while (smoothPoints.Count > 0 && (currentTime - smoothPoints[0].time) > trailDuration)
        {
            smoothPoints.RemoveAt(0);
        }

        // 3. TẠO LƯỚI 3D (RIBBON MESH) CHO VỆT KIẾM
        int count = smoothPoints.Count;
        if (count >= 2)
        {
            GenerateTrailMesh(count, currentTime);
        }
        else
        {
            if (mesh != null && mesh.vertexCount > 0)
            {
                mesh.Clear();
            }
        }
    }

    /// <summary>
    /// Nội suy Catmull-Rom Spline giữa 2 điểm mẫu mới nhất để đường chém tròn cong hoàn hảo
    /// </summary>
    private void AddSplineSegment(List<TrailPoint> samples, int steps)
    {
        int n = samples.Count;
        if (n < 2) return;

        TrailPoint p1 = samples[n - 2];
        TrailPoint p2 = samples[n - 1];
        TrailPoint p0 = (n >= 3) ? samples[n - 3] : p1;
        TrailPoint p3 = p2;

        int stepCount = Mathf.Max(1, steps);
        for (int i = 1; i <= stepCount; i++)
        {
            float t = (float)i / stepCount;
            Vector3 b = CatmullRom(p0.basePos, p1.basePos, p2.basePos, p3.basePos, t);
            Vector3 tp = CatmullRom(p0.tipPos, p1.tipPos, p2.tipPos, p3.tipPos, t);
            float tm = Mathf.Lerp(p1.time, p2.time, t);
            smoothPoints.Add(new TrailPoint(b, tp, tm));
        }
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    private void GenerateTrailMesh(int count, float currentTime)
    {
        vertices.Clear();
        uvs.Clear();
        colors.Clear();
        triangles.Clear();

        // Duyệt từ điểm cũ nhất (progress=1, tan biến ở đuôi) đến mới nhất (progress=0, tại mép kiếm)
        for (int i = 0; i < count; i++)
        {
            TrailPoint p = smoothPoints[i];
            float age = currentTime - p.time;
            float rawT = Mathf.Clamp01(age / trailDuration);

            // Đường cong làm mờ phi tuyến (Quadratic Ease-out) giúp vệt tan biến siêu mượt
            float progress = rawT * rawT;
            float u = 1f - rawT;

            Vector3 localBase = transform.InverseTransformPoint(p.basePos);
            Vector3 localTip = transform.InverseTransformPoint(p.tipPos);

            vertices.Add(localBase);
            vertices.Add(localTip);

            uvs.Add(new Vector2(u, 0f));
            uvs.Add(new Vector2(u, 1f));

            // Màu gradient đỏ thẫm nhạt viền đen khói
            Color gradColor = trailGradient.Evaluate(rawT);

            // Gốc kiếm (base) hơi tối màu hơn tạo chiều sâu khối bóng đen
            Color baseColor = gradColor * new Color(0.7f, 0.7f, 0.7f, 1f);
            baseColor.a = gradColor.a * (1f - progress);

            Color tipColor = gradColor;
            tipColor.a = gradColor.a * (1f - progress);

            colors.Add(baseColor);
            colors.Add(tipColor);
        }

        // Sinh tam giác 2 mặt (Double-sided)
        for (int i = 0; i < count - 1; i++)
        {
            int b0 = 2 * i;
            int t0 = 2 * i + 1;
            int b1 = 2 * (i + 1);
            int t1 = 2 * (i + 1) + 1;

            // Mặt trước
            triangles.Add(b0);
            triangles.Add(t0);
            triangles.Add(t1);

            triangles.Add(b0);
            triangles.Add(t1);
            triangles.Add(b1);

            // Mặt sau
            triangles.Add(b0);
            triangles.Add(t1);
            triangles.Add(t0);

            triangles.Add(b0);
            triangles.Add(b1);
            triangles.Add(t1);
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);

        // Thiết lập Bounds rộng để không bị Camera culling khi xoay góc
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 50f);
    }

    private Vector3 GetRawBasePosition()
    {
        return (bladeBase != null) ? bladeBase.position : transform.TransformPoint(baseOffset);
    }

    private Vector3 GetRawTipPosition()
    {
        return (bladeTip != null) ? bladeTip.position : transform.TransformPoint(tipOffset);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 b = GetRawBasePosition();
        Vector3 t = GetRawTipPosition();

        Gizmos.color = new Color(0.8f, 0.15f, 0.15f, 1f);
        Gizmos.DrawWireSphere(b, 0.05f);
        Gizmos.color = new Color(0.2f, 0.05f, 0.05f, 1f);
        Gizmos.DrawWireSphere(t, 0.08f);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(b, t);
    }
}
