using UnityEngine;

public class DeathEffect : MonoBehaviour
{
    public float existTime = 10f;

    void Start()
    {
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