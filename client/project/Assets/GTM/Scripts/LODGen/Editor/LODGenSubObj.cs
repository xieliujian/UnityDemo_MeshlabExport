
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace gtm.Scene.LODGen
{
    public class LODGenSubObj
    {
        public string MeshName;

        public List<Material> MatList = new List<Material>();

        public List<string> MatNameList = new List<string>();

        public Renderer Render;

        public int FaceCount;

        public void CalcMat(Material[] matarray)
        {
            MatList.AddRange(matarray);

            MatNameList.Clear();
            foreach(var mat in matarray)
            {
                var path = AssetDatabase.GetAssetPath(mat);
                MatNameList.Add(path);
            }
        }

        public void CalcFace()
        {
            MeshFilter meshfilter = Render.GetComponent<MeshFilter>();
            if (meshfilter == null)
                return;

            Mesh mesh = meshfilter.sharedMesh;
            if (mesh == null)
                return;

            FaceCount = mesh.triangles.Length;
        }

        public void SaveAsPrefab(string path, string name)
        {
            MeshFilter srcmeshfilter = Render.GetComponent<MeshFilter>();
            if (srcmeshfilter == null)
                return;

            var prefabmesh = srcmeshfilter.sharedMesh;

            GameObject go = new GameObject(name);
            GameObject meshgo = new GameObject("mesh");
            meshgo.transform.SetParent(go.transform);
            var meshfilter = meshgo.AddComponent<MeshFilter>();
            var meshrender = meshgo.AddComponent<MeshRenderer>();

            // 1. 加载模型
            meshfilter.mesh = prefabmesh;

            // 2. 加载材质
            meshrender.materials = MatList.ToArray();

            var filepath = path + name + LODGenUtil.PREFAB_SUFFIX;
            PrefabUtility.SaveAsPrefabAsset(go, filepath);

            AssetDatabase.Refresh();

            GameObject.DestroyImmediate(go);
        }
    }
}


