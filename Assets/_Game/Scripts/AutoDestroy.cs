using UnityEngine;
public class AutoDestroy : MonoBehaviour
{
    public float delay = 0.5f; // Thời gian tồn tại bằng thời gian Animation
    void Start() { Destroy(gameObject, delay); }
}