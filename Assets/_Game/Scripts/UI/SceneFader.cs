using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SceneFader : MonoBehaviour
{
    #region Configuration & Settings
    public Image blackPanel;
    public float fadeSpeed = 1f;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        if (blackPanel != null)
        {
            blackPanel.color = Color.black;
            blackPanel.gameObject.SetActive(true);

            StartCoroutine(FadeIn());
        }
    }
    #endregion

    #region Coroutines
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
    #endregion
}