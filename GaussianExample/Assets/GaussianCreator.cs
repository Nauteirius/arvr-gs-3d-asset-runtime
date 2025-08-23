using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GaussianSplatting;
using GaussianSplatting.Runtime;
using UnityEditor.SearchService;
public class GaussianCreator : MonoBehaviour
{
    GaussianSplatRuntimeAssetCreator GaussianSplatRuntimeAssetCreator;
    GaussianSplatRenderer renderer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        
        GaussianSplatRuntimeAssetCreator = new GaussianSplatRuntimeAssetCreator();
        //GaussianSplatRuntimeAssetCreator.CreateAsset(GaussianSplatRuntimeAssetCreator);

        string ply_path = "C:/models/UnityGaussianSplatting/projects/Auto/output.ply";


        renderer = gameObject.AddComponent<GaussianSplatRenderer>();
    }
    void Start()
    {
        //renderer.m_Asset = (GaussianSplatAsset)AssetDatabase.LoadAssetAtPath("Assets/GaussianSplatting/output.asset", typeof(GaussianSplatAsset));

    }
}
