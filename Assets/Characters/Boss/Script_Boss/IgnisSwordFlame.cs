using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý hiệu ứng ngọn lửa bao quanh thanh kiếm của Red Ignis (Ignis_Sword_Flame).
/// Bao gồm: Bầu lửa ở chuôi/đốc kiếm (Hilt Aura), lửa ôm sát thân kiếm (Blade Flame),
/// tia lửa (Sparks), khói (Smoke) và nguồn sáng lửa (Point Light).
/// Tự động bốc cháy khi xuất chiêu và dập tắt khi trở về trạng thái bình thường hoặc bị hạ gục.
/// </summary>
public class IgnisSwordFlame : MonoBehaviour
{
    [Header("=== SWORD FLAME COMPONENTS ===")]
    [Tooltip("Tất cả các hệ thống hạt ParticleSystem trên kiếm")]
    [SerializeField] private ParticleSystem[] particleSystems;

    [Tooltip("Nguồn sáng lửa phát quang trên đốc kiếm")]
    [SerializeField] private Light swordLight;

    [Header("=== THÔNG SỐ ÁNH SÁNG & HIỆU ỨNG ===")]
    [Tooltip("Cường độ ánh sáng tối đa khi kiếm bốc cháy rực rỡ")]
    [SerializeField] private float maxLightIntensity = 2.0f;

    [Tooltip("Thời gian làm mờ/chuyển đổi ánh sáng khi bật/tắt (giây)")]
    [SerializeField] private float lightTransitionDuration = 0.3f;

    [Tooltip("Tự động bốc cháy liên tục ngay khi game bắt đầu để xem trước hiệu ứng (Tắt để giữ kiếm thường trước 50% máu)")]
    [SerializeField] private bool autoIgniteOnStart = false;

    private bool isIgnited = false;
    private Coroutine lightFadeRoutine;

    private void Awake()
    {
        CacheComponents();

        if (autoIgniteOnStart)
        {
            SetFlame(true, instant: true);
        }
        else
        {
            SetFlame(false, instant: true);
        }
    }

    /// <summary>
    /// Thu thập các component cần thiết nếu chưa được gán thủ công trong Inspector
    /// </summary>
    public void CacheComponents()
    {
        if (particleSystems == null || particleSystems.Length == 0)
        {
            particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        }

        if (swordLight == null)
        {
            swordLight = GetComponentInChildren<Light>(true);
        }
    }

    /// <summary>
    /// Bật hoặc tắt ngọn lửa bao quanh thanh kiếm.
    /// </summary>
    /// <param name="active">True = bốc cháy rực rỡ, False = tắt lửa</param>
    /// <param name="instant">True = dập tắt lập tức, False = tàn dần tự nhiên</param>
    public void SetFlame(bool active, bool instant = false)
    {
        isIgnited = active;
        CacheComponents();

        if (particleSystems != null)
        {
            foreach (var ps in particleSystems)
            {
                if (ps == null) continue;

                if (active)
                {
                    if (!ps.isPlaying)
                    {
                        ps.Play(true);
                    }
                }
                else
                {
                    if (instant)
                    {
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    }
                    else
                    {
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    }
                }
            }
        }

        if (swordLight != null)
        {
            if (instant)
            {
                if (lightFadeRoutine != null)
                {
                    StopCoroutine(lightFadeRoutine);
                    lightFadeRoutine = null;
                }
                swordLight.intensity = active ? maxLightIntensity : 0f;
                swordLight.enabled = active;
            }
            else
            {
                if (lightFadeRoutine != null) StopCoroutine(lightFadeRoutine);
                lightFadeRoutine = StartCoroutine(FadeLightRoutine(active ? maxLightIntensity : 0f));
            }
        }
    }

    private IEnumerator FadeLightRoutine(float targetIntensity)
    {
        if (swordLight == null) yield break;

        if (targetIntensity > 0f) swordLight.enabled = true;

        float startIntensity = swordLight.intensity;
        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, lightTransitionDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            swordLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, elapsed / duration);
            yield return null;
        }

        swordLight.intensity = targetIntensity;
        if (targetIntensity <= 0.01f)
        {
            swordLight.enabled = false;
        }
        lightFadeRoutine = null;
    }

    /// <summary>
    /// Kích hoạt thanh kiếm bốc cháy
    /// </summary>
    [ContextMenu("Test Ignite (Bốc cháy)")]
    public void Ignite()
    {
        SetFlame(true, false);
    }

    /// <summary>
    /// Tắt ngọn lửa trên kiếm
    /// </summary>
    [ContextMenu("Test Extinguish (Tắt lửa)")]
    public void Extinguish(bool instant = false)
    {
        SetFlame(false, instant);
    }

    public bool IsIgnited => isIgnited;
}
