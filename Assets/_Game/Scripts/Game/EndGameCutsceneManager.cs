using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndGameCutsceneManager : MonoBehaviour
{
    #region Configuration & Settings
    [Header("Timeline Settings")]
    public float timeBeforeWalk = 15f;
    public float timeBeforeKiss = 29f;
    public float kissDuration = 3f;

    [Header("Actors")]
    public Transform playerActorRoot;
    public Animator playerLegAnimator;
    public Transform girlActor;
    public GameObject kissSpriteObject;

    [Header("Movement")]
    public Transform stopPosition;
    public float walkSpeed = 3f;

    [Header("System")]
    public SceneFader sceneFader;
    public AudioSource musicSource;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.canPause = false;
        }
        if (kissSpriteObject != null) kissSpriteObject.SetActive(false);
        if (playerLegAnimator != null) playerLegAnimator.SetBool("IsMoving", false);

        StartCoroutine(CutsceneTimeline());
    }
    #endregion

    #region Coroutines
    IEnumerator CutsceneTimeline()
    {
        yield return new WaitForSeconds(timeBeforeWalk);

        if (playerLegAnimator != null) playerLegAnimator.SetBool("IsMoving", true);

        while (Vector3.Distance(playerActorRoot.position, stopPosition.position) > 0.05f)
        {
            playerActorRoot.position = Vector3.MoveTowards(
                playerActorRoot.position,
                stopPosition.position,
                walkSpeed * Time.deltaTime
            );
            yield return null;
        }

        if (playerLegAnimator != null) playerLegAnimator.SetBool("IsMoving", false);

        float timeElapsed = Time.timeSinceLevelLoad;
        float waitTime = timeBeforeKiss - timeElapsed;

        if (waitTime > 0)
        {
            yield return new WaitForSeconds(waitTime);
        }

        playerActorRoot.gameObject.SetActive(false);
        girlActor.gameObject.SetActive(false);

        kissSpriteObject.SetActive(true);

        yield return new WaitForSeconds(kissDuration);

        if (sceneFader != null) yield return StartCoroutine(sceneFader.FadeOut());

        float startVolume = musicSource.volume;
        while (musicSource.volume > 0)
        {
            musicSource.volume -= startVolume * Time.deltaTime;
            yield return null;
        }

        SceneManager.LoadScene("Credit");
    }
    #endregion
}