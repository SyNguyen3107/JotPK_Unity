using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class UIBlinker : MonoBehaviour
{
    #region Configuration & Settings
    [Header("Settings")]
    public float blinkSpeed = 2f;
    public float minAlpha = 0.2f;
    public float maxAlpha = 1f;
    #endregion

    #region Runtime Variables
    private CanvasGroup canvasGroup;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        if (canvasGroup != null) canvasGroup.alpha = maxAlpha;
    }

    void Update()
    {
        float alpha = Mathf.PingPong(Time.time * blinkSpeed, maxAlpha - minAlpha) + minAlpha;

        if (canvasGroup != null) canvasGroup.alpha = alpha;
    }
    #endregion
}