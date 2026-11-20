using UnityEngine;

namespace InfiniteLabyrinth.Audio
{
    /// <summary>
    /// Gắn lên ngọn đuốc trong hầm ngục để phát âm thanh lửa cháy lách tách (3D Spatial Loop)
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SpatialTorchAudio : MonoBehaviour
    {
        [Tooltip("Cự ly tối thiểu âm thanh giữ nguyên âm lượng")]
        [SerializeField] private float minDistance = 3.0f;

        [Tooltip("Cự ly tối đa nghe thấy tiếng đuốc")]
        [SerializeField] private float maxDistance = 18.0f;

        [Range(0f, 1f)]
        [SerializeField] private float volume = 0.85f;

        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.spatialBlend = 1.0f; // 100% 3D Spatial Audio
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.dopplerLevel = 0f;
        }

        private void Start()
        {
            if (AudioManager.HasInstance && AudioManager.Instance.TorchClip != null)
            {
                audioSource.clip = AudioManager.Instance.TorchClip;
                audioSource.volume = GetTargetVolume();
                audioSource.Play();
            }
        }

        private void Update()
        {
            if (audioSource == null || !audioSource.isPlaying) return;

            float targetVol = GetTargetVolume();
            if (Mathf.Abs(audioSource.volume - targetVol) > 0.005f)
            {
                audioSource.volume = targetVol;
            }
        }

        private float GetTargetVolume()
        {
            if (!AudioManager.HasInstance) return volume;
            float baseVol = AudioManager.Instance.VolTorch;
            return baseVol * AudioManager.Instance.SfxVolume * AudioManager.Instance.MasterVolume;
        }

        private void OnEnable()
        {
            if (audioSource != null && audioSource.clip != null && !audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
    }
}
