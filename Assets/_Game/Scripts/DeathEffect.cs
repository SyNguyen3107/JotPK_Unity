using UnityEngine;

public class DeathEffect : MonoBehaviour
{
    public float existTime = 10f; // Thời gian xác chết tồn tại (giây)

    void Start()
    {
        // Animation sẽ tự chạy (do Animator mặc định).
        // Frame cuối sẽ tự giữ nguyên (do đã bỏ Loop Time).
        // Chúng ta chỉ cần đếm ngược 10s để xóa object này đi.
        Destroy(gameObject, existTime);
    }

    public void PlaySound(AudioClip clip)
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}