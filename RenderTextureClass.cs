using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;


public class RenderTextureClass : MonoBehaviour
{
    // Start is called before the first frame update

    public RenderTexture rt = null;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            SaveRTToFile();
        }
    }

    //[MenuItem("Assets/Save RenderTexture to file")]
    [ContextMenu("Assets/Save RenderTexture to file")]


    public void SaveRTToFile()
    {
       // RenderTexture rt = Selection.activeObject as RenderTexture;

        
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        RenderTexture.active = null;

        byte[] bytes;
        bytes = tex.EncodeToPNG();

        string path = "C:\\stu\\praca magisterska\\screenshots\\screenshot.png"; // //AssetDatabase.GetAssetPath(rt) + ".png";
    
        System.IO.File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);
        //Debug.Log("Saved to " + path);
    }

    [MenuItem("Assets/Save RenderTexture to file", true)]
    public static bool SaveRTToFileValidation()
    {
        return Selection.activeObject is RenderTexture;
    }
}
