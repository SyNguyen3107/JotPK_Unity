using UnityEngine;
using UnityEditor;
using System.IO;

public class SaveSpriteToPNG
{
    [MenuItem("Assets/Save Sprite As PNG")]
    static void SaveSprite()
    {
        // Lấy object đang được chọn
        Object obj = Selection.activeObject;
        if (obj == null || !(obj is Sprite))
        {
            Debug.LogError("Vui lòng chọn một Sprite (không phải Texture) trong Project Window.");
            return;
        }

        Sprite sprite = (Sprite)obj;
        Texture2D texture = sprite.texture;

        // Kiểm tra xem Texture có cho phép đọc dữ liệu không (Read/Write Enabled)
        string texturePath = AssetDatabase.GetAssetPath(texture);
        TextureImporter ti = AssetImporter.GetAtPath(texturePath) as TextureImporter;

        bool wasReadable = ti.isReadable;
        if (!wasReadable)
        {
            // Tạm thời bật Read/Write để đọc pixel
            ti.isReadable = true;
            ti.SaveAndReimport();
        }

        try
        {
            // Tạo Texture mới chỉ chứa phần cắt của Sprite
            // Dùng (int)sprite.rect để lấy đúng tọa độ cắt trong ảnh to
            Rect r = sprite.rect;
            Texture2D newTex = new Texture2D((int)r.width, (int)r.height);

            // Lấy pixel từ ảnh gốc
            Color[] pixels = texture.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height);
            newTex.SetPixels(pixels);
            newTex.Apply();

            // Mã hóa thành PNG
            byte[] bytes = newTex.EncodeToPNG();

            // Lưu file ra thư mục gốc của Assets
            string path = Application.dataPath + "/" + sprite.name + "_extracted.png";
            File.WriteAllBytes(path, bytes);

            Debug.Log("Đã lưu ảnh tại: " + path);
            AssetDatabase.Refresh(); // Refresh để hiện file mới trong Unity
        }
        catch (System.Exception e)
        {
            Debug.LogError("Lỗi khi lưu ảnh: " + e.Message);
        }
        finally
        {
            // Trả lại trạng thái Read/Write như cũ nếu cần
            if (!wasReadable)
            {
                ti.isReadable = false;
                ti.SaveAndReimport();
            }
        }
    }
}