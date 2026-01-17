using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement; // Dùng để thoát game hoặc về menu

public class EndGameCutsceneManager : MonoBehaviour
{
    [Header("Timeline Settings")]
    public float timeBeforeWalk = 15f; // 15s đầu chỉ nghe nhạc + Girl hát
    public float timeBeforeKiss = 29f; // Tại giây 29 thì hôn (tổng thời gian)
    public float kissDuration = 3f;    // Hôn 3s rồi tối dần

    [Header("Actors")]
    public Transform playerActorRoot;   // Cái vỏ Actor tổng (để di chuyển)
    public Animator playerLegAnimator;  // Animator ở chân (để chỉnh IsMoving)
    public Transform girlActor;         // Cô gái
    public GameObject kissSpriteObject; // Ảnh hôn nhau

    [Header("Movement")]
    public Transform stopPosition;      // Vị trí đứng cạnh cô gái (Tạo 1 object trống để đánh dấu)
    public float walkSpeed = 3f;

    [Header("System")]
    public SceneFader sceneFader;       // Script màn hình đen
    public AudioSource musicSource;     // Audio Source trong scene này

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.canPause = false;
        }
        // Đảm bảo trạng thái ban đầu
        if (kissSpriteObject != null) kissSpriteObject.SetActive(false);
        if (playerLegAnimator != null) playerLegAnimator.SetBool("IsMoving", false);

        // Bắt đầu kịch bản
        StartCoroutine(CutsceneTimeline());
    }

    IEnumerator CutsceneTimeline()
    {
        // --- GIAI ĐOẠN 1: MỞ MÀN (0s - 15s) ---
        // SceneFader đã tự Fade In ở Start()
        Debug.Log("Scene Start. Music Playing...");

        // Chờ 15 giây
        yield return new WaitForSeconds(timeBeforeWalk);

        // --- GIAI ĐOẠN 2: PLAYER ĐI VÀO (15s -> Đến đích) ---
        Debug.Log("Player starts walking...");

        if (playerLegAnimator != null) playerLegAnimator.SetBool("IsMoving", true);

        // Di chuyển Player từ vị trí hiện tại đến Stop Position
        while (Vector3.Distance(playerActorRoot.position, stopPosition.position) > 0.05f)
        {
            playerActorRoot.position = Vector3.MoveTowards(
                playerActorRoot.position,
                stopPosition.position,
                walkSpeed * Time.deltaTime
            );
            yield return null; // Chờ frame tiếp theo
        }

        // Đã đến nơi -> Dừng chân
        Debug.Log("Player reached Girl.");
        if (playerLegAnimator != null) playerLegAnimator.SetBool("IsMoving", false);

        // --- GIAI ĐOẠN 3: CHỜ NHẠC ĐẾN ĐOẠN CAO TRÀO (Đến giây 29) ---
        // Tính thời gian còn lại cần chờ
        // (Time.timeSinceLevelLoad là thời gian tính từ lúc load scene)
        float timeElapsed = Time.timeSinceLevelLoad;
        float waitTime = timeBeforeKiss - timeElapsed;

        if (waitTime > 0)
        {
            yield return new WaitForSeconds(waitTime);
        }

        // --- GIAI ĐOẠN 4: HÔN NHAU (Giây 29) ---
        Debug.Log("KISS!");

        // Ẩn 2 nhân vật
        playerActorRoot.gameObject.SetActive(false);
        girlActor.gameObject.SetActive(false);

        // Hiện ảnh hôn
        kissSpriteObject.SetActive(true);

        // --- GIAI ĐOẠN 5: KẾT THÚC (3s sau) ---
        yield return new WaitForSeconds(kissDuration);

        Debug.Log("Fade Out...");
        // Tối dần
        if (sceneFader != null) yield return StartCoroutine(sceneFader.FadeOut());

        // Giảm âm lượng nhạc dần dần (Tuỳ chọn)
        float startVolume = musicSource.volume;
        while (musicSource.volume > 0)
        {
            musicSource.volume -= startVolume * Time.deltaTime; // Giảm trong 1s
            yield return null;
        }

        // --- HẾT GAME -> CHUYỂN SANG CREDIT ---
        Debug.Log("THE END. Loading Credit...");
        SceneManager.LoadScene("Credit");
    }
}