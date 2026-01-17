using UnityEngine;
using System.Collections;

public class VendorController : MonoBehaviour
{
    #region Configuration & Settings
    [Header("Settings")]
    public GameObject shopTablePrefab;
    public float moveSpeed = 3f;
    public float centerY = 1f;
    public float spawnY = 7f;

    [Header("Audio")]
    public AudioSource footStepAudioSource;
    public AudioClip footStepClip;

    [Header("References")]
    public VendorVisuals visuals;
    #endregion

    #region Runtime Variables
    private GameObject currentTable;
    private bool isLeaving = false;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        if (footStepAudioSource != null && footStepClip != null)
        {
            footStepAudioSource.clip = footStepClip;
            footStepAudioSource.loop = true;
        }

        StartCoroutine(ShopRoutine());
    }

    void OnEnable()
    {
        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnUpgradePurchased += HandlePurchaseComplete;
    }

    void OnDisable()
    {
        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.OnUpgradePurchased -= HandlePurchaseComplete;
    }
    #endregion

    #region Event Handlers
    void HandlePurchaseComplete()
    {
        if (currentTable != null) Destroy(currentTable);
        if (!isLeaving) StartCoroutine(LeaveRoutine());
    }
    #endregion

    #region Coroutines
    IEnumerator ShopRoutine()
    {
        // 1. Set Initial Position
        transform.position = new Vector3(0, spawnY, 0);

        // 2. Move Down
        if (visuals != null) visuals.SetWalking(true, -1f);
        if (footStepAudioSource != null) footStepAudioSource.Play();

        while (transform.position.y > centerY)
        {
            transform.position += Vector3.down * moveSpeed * Time.deltaTime;
            yield return null;
        }

        // 3. Stop & Spawn Table
        if (visuals != null) visuals.SetWalking(false, 0f);
        if (footStepAudioSource != null) footStepAudioSource.Stop();

        Vector3 tablePos = transform.position + new Vector3(0, -1.5f, 0);
        if (shopTablePrefab != null)
        {
            currentTable = Instantiate(shopTablePrefab, tablePos, Quaternion.identity);
            if (transform.parent != null)
            {
                currentTable.transform.SetParent(transform.parent);
            }
        }
    }

    IEnumerator LeaveRoutine()
    {
        isLeaving = true;

        // 1. Wait for Player Interaction Animation
        yield return new WaitForSeconds(2f);

        // 2. Move Up (Leave)
        if (visuals != null) visuals.SetWalking(true, 1f);
        if (footStepAudioSource != null) footStepAudioSource.Play();

        while (transform.position.y < spawnY)
        {
            transform.position += Vector3.up * moveSpeed * Time.deltaTime;
            yield return null;
        }

        // 3. Cleanup
        if (footStepAudioSource != null) footStepAudioSource.Stop();
        Destroy(gameObject);
    }
    #endregion
}