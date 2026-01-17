using UnityEngine;
using UnityEditor;
using System.IO;

public class SaveSpriteToPNG
{
    #region Editor Menu Item
    [MenuItem("Assets/Save Sprite As PNG")]
    static void SaveSprite()
    {
        Object obj = Selection.activeObject;
        if (obj == null || !(obj is Sprite))
        {
            return;
        }

        Sprite sprite = (Sprite)obj;
        Texture2D texture = sprite.texture;

        string texturePath = AssetDatabase.GetAssetPath(texture);
        TextureImporter ti = AssetImporter.GetAtPath(texturePath) as TextureImporter;

        bool wasReadable = ti.isReadable;
        if (!wasReadable)
        {
            ti.isReadable = true;
            ti.SaveAndReimport();
        }

        try
        {
            Rect r = sprite.rect;
            Texture2D newTex = new Texture2D((int)r.width, (int)r.height);

            Color[] pixels = texture.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height);
            newTex.SetPixels(pixels);
            newTex.Apply();

            byte[] bytes = newTex.EncodeToPNG();

            string path = Application.dataPath + "/" + sprite.name + "_extracted.png";
            File.WriteAllBytes(path, bytes);

            AssetDatabase.Refresh();
        }
        catch (System.Exception)
        {
        }
        finally
        {
            if (!wasReadable)
            {
                ti.isReadable = false;
                ti.SaveAndReimport();
            }
        }
    }
    #endregion
}