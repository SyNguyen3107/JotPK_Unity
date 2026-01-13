using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SceneFader : MonoBehaviour
{
    public Image blackPanel;
    public float fadeSpeed = 1f;

    void Start()
    {
        if (blackPanel != null)
        {
            // Đảm bảo bắt đầu là màu đen
            blackPanel.color = Color.black;
            blackPanel.gameObject.SetActive(true);

            // Tự động Fade In (Sáng dần) khi scene bắt đầu
            StartCoroutine(FadeIn());
        }
    }

    public IEnumerator FadeIn()
    {
        float alpha = 1f;
        while (alpha > 0f)
        {
            alpha -= Time.deltaTime * fadeSpeed;
            if (blackPanel != null)
                blackPanel.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        if (blackPanel != null) blackPanel.gameObject.SetActive(false);
    }

    public IEnumerator FadeOut()
    {
        if (blackPanel != null) blackPanel.gameObject.SetActive(true);
        float alpha = 0f;
        while (alpha < 1f)
        {
            alpha += Time.deltaTime * fadeSpeed;
            if (blackPanel != null)
                blackPanel.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
    }
}