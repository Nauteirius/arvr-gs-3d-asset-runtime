
using GaussianSplatting.Runtime;

using System.IO;
using Unity.Collections;
using UnityEditor;

using UnityEngine;
public class GaussianCreator : MonoBehaviour
{
    public GaussianSplatRuntimeAssetCreator GaussianSplatRuntimeAssetCreator;
    public GaussianSplatRuntimeAsset GaussianSplatRuntimeAsset;
    GaussianSplatAsset gaussianSplatAsset;
    public GaussianSplatRenderer renderer;

    public TextAsset TextAsset;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        
        //GaussianSplatRuntimeAssetCreator = new GaussianSplatRuntimeAssetCreator();
        //GaussianSplatRuntimeAssetCreator.CreateAsset(GaussianSplatRuntimeAssetCreator);

        string ply_path = "C:/models/UnityGaussianSplatting/projects/Auto/output.ply";

        GaussianSplatRuntimeAssetCreator.SetQualityLevel(GaussianSplatting.Runtime.Utils.DataQuality.Medium);
        //renderer = gameObject.AddComponent<GaussianSplatRenderer>();
    }
    void Start()
    {
        //renderer.m_Asset = (GaussianSplatAsset)AssetDatabase.LoadAssetAtPath("Assets/GaussianSplatting/output.asset", typeof(GaussianSplatAsset));
            TextAsset file = (TextAsset)AssetDatabase.LoadAssetAtPath("C:\\stu\\GaussianExample\\GaussianExample\\GaussianExample\\Assets/output.ply", typeof(TextAsset));
            GaussianSplatRuntimeAsset = GaussianSplatRuntimeAssetCreator.CreateAsset("test.asset", "C:/stu/GaussianExample/GaussianExample/GaussianExample/Assets/output.ply", true);
            //gaussianSplatAsset = new GaussianSplatAsset();
            //gaussianSplatAsset.SetDataHash(GaussianSplatRuntimeAsset.dataHash);



            WriteFile(Path.Combine(Application.dataPath+"/Resources/", "chunk.txt"), GaussianSplatRuntimeAsset.chunkData.GetData<byte>());
            WriteFile(Path.Combine(Application.dataPath + "/Resources/", "pos.txt"), GaussianSplatRuntimeAsset.posData.GetData<byte>());
            WriteFile(Path.Combine(Application.dataPath + "/Resources/", "other.txt"), GaussianSplatRuntimeAsset.otherData.GetData<byte>());
            WriteFile(Path.Combine(Application.dataPath + "/Resources/", "color.txt"), GaussianSplatRuntimeAsset.colorData.GetData<byte>());
            WriteFile(Path.Combine(Application.dataPath + "/Resources/", "sh.txt"), GaussianSplatRuntimeAsset.shData.GetData<byte>());



        GaussianSplatRuntimeAsset.SetAssetFiles(
                Resources.Load<TextAsset>("chunk"),
                Resources.Load<TextAsset>("pos"),
                Resources.Load<TextAsset>("other"),
                Resources.Load<TextAsset>("color"),
                Resources.Load<TextAsset>("sh"));

/*        TextAsset = Resources.Load<TextAsset>("chunk");
        print(Application.dataPath + "/chunk.txt");
        print(TextAsset.text);*/
        GaussianSplatRuntimeAsset.Initialize(GaussianSplatRuntimeAsset.splatCount, GaussianSplatRuntimeAsset.posFormat, GaussianSplatRuntimeAsset.scaleFormat, GaussianSplatRuntimeAsset.colorFormat, GaussianSplatRuntimeAsset.shFormat, GaussianSplatRuntimeAsset.boundsMin, GaussianSplatRuntimeAsset.boundsMax, GaussianSplatRuntimeAsset.cameras);

    }

    public void injectAsset()
    {
        renderer.InjectAsset(GaussianSplatRuntimeAsset);
    }
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))//can be bool
        {
            print("pressed");


            print(GaussianSplatRuntimeAsset);

            renderer.InjectAsset( GaussianSplatRuntimeAsset);
        }
    }
    void WriteFile(string path, NativeArray<byte> data)
    {
        using var fs = new FileStream(path, System.IO.FileMode.Create, FileAccess.Write);
        fs.Write(data);
        data.Dispose();
    }
}
