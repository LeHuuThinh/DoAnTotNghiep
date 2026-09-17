using UnityEngine;

[RequireComponent(typeof(Animator))]
public class RandomIdleOffset : MonoBehaviour
{
    private void Start()
    {
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            // Bốc một số ngẫu nhiên từ 0.0 đến 1.0 (tương ứng từ 0% đến 100% vòng lặp animation)
            float randomOffset = Random.Range(0f, 1f);
            animator.SetFloat("Offset", randomOffset);
        }
    }
}