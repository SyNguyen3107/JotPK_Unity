using UnityEngine;
using System.Collections;

public class VendorController : MonoBehaviour
{
    [Header("Settings")]
    public GameObject shopTablePrefab; // Kéo Prefab cái bàn vào đây
    public float moveSpeed = 3f;
    public float centerY = 0f; // Toạ độ Y giữa màn hình (nơi NPC dừng lại)
    public float spawnY = 7f;  // Toạ độ Y tại cổng trên (nơi xuất phát/biến mất)

    [Header("References")]
    public VendorVisuals visuals; // Script Visuals/Animator ta đã làm ở Bước 2

    private GameObject currentTable;
    private bool isLeaving = false;

    void Start()
    {
        // Bắt đầu quy trình vào chợ
        StartCoroutine(ShopRoutine());
    }

    // Đăng ký lắng nghe sự kiện
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

    // Hàm này tự động chạy khi Player mua đồ thành công
    void HandlePurchaseComplete()
    {
        // Xóa cái bàn
        if (currentTable != null) Destroy(currentTable);
        if (!isLeaving) StartCoroutine(LeaveRoutine());
    }

    // --- QUY TRÌNH ĐI VÀO (START) ---
    IEnumerator ShopRoutine()
    {
        // 1. Đặt vị trí xuất phát (Trên cổng)
        transform.position = new Vector3(0, spawnY, 0);

        // 2. Đi xuống giữa map
        if (visuals != null) visuals.SetWalking(true, -1f); // -1 là đi xuống

        while (transform.position.y > centerY)
        {
            transform.position += Vector3.down * moveSpeed * Time.deltaTime;
            yield return null;
        }

        // 3. Dừng lại & Spawn Bàn
        if (visuals != null) visuals.SetWalking(false, 0f);

        // Spawn bàn ngay trước mặt NPC (lệch xuống 1 chút)
        Vector3 tablePos = transform.position + new Vector3(0, -1.5f, 0);
        if (shopTablePrefab != null)
        {
            currentTable = Instantiate(shopTablePrefab, tablePos, Quaternion.identity);
        }

        // Lúc này Shop đã mở, chờ Player mua...
        // Khi mua xong, hàm HandlePurchaseComplete sẽ được gọi tự động.
    }

    // --- QUY TRÌNH RỜI ĐI (END) ---
    IEnumerator LeaveRoutine()
    {
        isLeaving = true;

        // 1. Chờ Player diễn hoạt cảnh "Giơ tay" (2 giây)
        yield return new WaitForSeconds(2f);



        // 3. NPC Đi ngược về cổng trên
        if (visuals != null) visuals.SetWalking(true, 1f); // 1 là đi lên

        while (transform.position.y < spawnY)
        {
            transform.position += Vector3.up * moveSpeed * Time.deltaTime;
            yield return null;
        }
        // 4. Về đến nơi -> Kết thúc màn Shop

        // 5. Tự hủy NPC
        Destroy(gameObject);
    }
}