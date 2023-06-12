
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace gtm.Scene.LODGen
{
    public class LODGenSubObj
    {
        /// <summary>
        /// .
        /// </summary>
        public string meshName;

        /// <summary>
        /// .
        /// </summary>
        public List<Material> matList = new List<Material>();

        /// <summary>
        /// .
        /// </summary>
        public List<string> matNameList = new List<string>();

        /// <summary>
        /// .
        /// </summary>
        public Renderer render;

        /// <summary>
        /// .
        /// </summary>
        public int faceCount;

        /// <summary>
        /// .
        /// </summary>
        /// <param name="matarray"></param>
        public void CalcMat(Material[] matarray)
        {
            matList.AddRange(matarray);

            matNameList.Clear();
            foreach(var mat in matarray)
            {
                var path = AssetDatabase.GetAssetPath(mat);
                matNameList.Add(path);
            }
        }

        /// <summary>
        /// .
        /// </summary>
        public void CalcFace()
        {
            MeshFilter meshfilter = render.GetComponent<MeshFilter>();
            if (meshfilter == null)
                return;

            Mesh mesh = meshfilter.sharedMesh;
            if (mesh == null)
                return;

            faceCount = mesh.triangles.Length;
        }

        /// <summary>
        /// .
        /// </summary>
        /// <param name="path"></param>
        /// <param name="name"></param>
        public void SaveAsPrefab(string path, string name)
        {
            MeshFilter srcmeshfilter = render.GetComponent<MeshFilter>();
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
            meshrender.materials = matList.ToArray();

            var filepath = path + name + LODGenUtil.PREFAB_SUFFIX;
            PrefabUtility.SaveAsPrefabAsset(go, filepath);

            AssetDatabase.Refresh();

            GameObject.DestroyImmediate(go);
        }
    }
}


