using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace gtm.Scene.LODGen
{
    public enum HLODMaterialType
    {
        Opacity,        // 不透明
        AlphaTest,      // AlphaTest
        BOM,            // Building Opacity_Meshlab,    // 不透明 + Meshlab减面
        BOMC,           // Building Opacity_Meshlab_Combine,  // 不透明 + Meshlab减面 + 合并材质
        LODGen_BOM,     // LODGen 的 Building Opacity_Meshlab,    // 不透明 + Meshlab减面
        Empty,          // 空
    }

    public class LODGenUtil
    {
        public const string PREFAB_SUFFIX = ".prefab";

        /// <summary>
        /// 临时目录前缀
        /// </summary>
        public const string TEMP_DIR_PREFIX = "/Temp";

        /// <summary>
        /// .
        /// </summary>
        public const string LOD_MODEL_SUFFIX = "lodgen";

        /// <summary>
        /// .
        /// </summary>
        public const string MESHLAB_SUFFIX_WITHOUT_EXTENSION = "_Meshlab";

        /// <summary>
        /// .
        /// </summary>
        public const string FBX_SUFFIX = ".fbx";

        /// <summary>
        /// .
        /// </summary>
        /// <param name="suffix"></param>
        /// <param name="savepath"></param>
        /// <param name="savefilename"></param>
        /// <param name="mattype"></param>
        /// <param name="isusemattypename"></param>
        /// <returns></returns>
        public static string CombineFileName(string suffix, string savepath, string savefilename, HLODMaterialType mattype, bool isusemattypename)
        {
            string filename = savepath + savefilename + suffix;
            if (isusemattypename)
            {
                filename = savepath + savefilename + "_" + mattype.ToString() + suffix;
            }

            return filename;
        }

        /// <summary>
        /// 得到被使用的渲染成分列表
        /// </summary>
        /// <param name="renders"></param>
        /// <returns></returns>
        public static List<MeshRenderer> GetUsedRenderArray(Renderer[] renders)
        {
            List<MeshRenderer> renderlist = new List<MeshRenderer>();

            for (int j = 0; j < renders.Length; ++j)
            {
                var meshrender = renders[j] as MeshRenderer;
                if (meshrender == null)
                    continue;

                var meshfilter = meshrender.GetComponent<MeshFilter>();
                if (meshfilter == null)
                    continue;

                var mesh = meshfilter.sharedMesh;
                if (mesh == null)
                    continue;

                // 是否显示开关开启
                if (!meshrender.enabled)
                    continue;

                var mat = meshrender.sharedMaterial;
                if (mat == null)
                    continue;

                renderlist.Add(meshrender);
            }

            return renderlist;
        }

        /// <summary>
        /// .
        /// </summary>
        /// <param name="_name"></param>
        /// <returns></returns>
        public static string GetPrefabName(string _name)
        {
            var splitarray = _name.Split(new char[] { ' ' });
            var name = splitarray[0];
            return name;
        }

        /// <summary>
        /// .
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="flitersuffix"></param>
        /// <returns></returns>
        public static List<string> FindDirFileList(string fullPath, string flitersuffix)
        {
            List<string> filelist = new List<string>();

            //获取指定路径下面的所有资源文件  
            if (Directory.Exists(fullPath))
            {
                DirectoryInfo direction = new DirectoryInfo(fullPath);
                FileInfo[] files = direction.GetFiles("*", SearchOption.TopDirectoryOnly);

                for (int i = 0; i < files.Length; i++)
                {
                    var filename = files[i].Name;
                    if (filename.EndsWith(".meta"))
                        continue;

                    if (filename.EndsWith(flitersuffix))
                    {
                        filelist.Add(filename);
                    }
                }
            }

            return filelist;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static Bounds GetObjBoundsNew(GameObject parent)
        {
            // https://www.xuanyusong.com/archives/3461

            Renderer[] renders = parent.GetComponentsInChildren<Renderer>();
            if (renders == null || renders.Length <= 0)
            {
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            Vector3 center = Vector3.zero;
            foreach (Renderer child in renders)
            {
                center += child.bounds.center;
            }

            center /= renders.Length;
            Bounds bounds = new Bounds(center, Vector3.zero);

            foreach (Renderer child in renders)
            {
                bounds.Encapsulate(child.bounds);
            }

            return bounds;
        }

        public static float GetObjBoundsMaxSize(GameObject parent)
        {
            Bounds bounds = GetObjBoundsNew(parent);

            float maxsize = -10000.0f;

            if (bounds.size.x > maxsize)
            {
                maxsize = bounds.size.x;
            }

            if (bounds.size.y > maxsize)
            {
                maxsize = bounds.size.y;
            }

            if (bounds.size.z > maxsize)
            {
                maxsize = bounds.size.z;
            }

            return maxsize;
        }

        /// <summary>
        /// 复制transform
        /// </summary>
        /// <param name="dsttrans"></param>
        /// <param name="srctrans"></param>
        public static void CopyTransform(Transform dsttrans, Transform srctrans)
        {
            var parenttrans = srctrans.parent;

            if (dsttrans != null && srctrans != null && parenttrans != null)
            {
                dsttrans.SetParent(parenttrans);
                ResetTransform(dsttrans);
                dsttrans.localPosition = srctrans.localPosition;
                dsttrans.localScale = srctrans.localScale;
                dsttrans.localRotation = srctrans.localRotation;
            }
        }

        public static void RemoveLODGroup(GameObject go)
        {
            if (go == null)
                return;

            LODGroup[] comlist = go.GetComponentsInChildren<LODGroup>();
            if (comlist == null || comlist.Length <= 0)
                return;

            foreach (var com in comlist)
            {
                if (com == null)
                    continue;

                GameObject.DestroyImmediate(com);
            }
        }

        /// <summary>
        /// .
        /// </summary>
        /// <param name="trans"></param>
        public static void ResetTransform(Transform trans)
        {
            trans.localPosition = Vector3.zero;
            trans.localRotation = Quaternion.identity;
            trans.localScale = Vector3.one;
        }

        /// <summary>
        /// 得到GameObject的lod
        /// </summary>
        /// <param name="go"></param>
        /// <param name="percent"></param>
        /// <returns></returns>
        public static LOD GetLOD(GameObject obj, float percent)
        {
            Renderer[] renders = GetLodRenderArray(obj);

            LOD lod = new LOD(percent, renders);
            return lod;
        }

        /// <summary>
        /// 获取LOD的真实可渲染对象
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static Renderer[] GetLodRenderArray(GameObject obj)
        {
            Renderer[] renders = null;
            LODGroup lodgroup = obj.GetComponent<LODGroup>();
            if (lodgroup != null)
            {
                LOD[] lodarray = lodgroup.GetLODs();
                if (lodarray != null || lodarray.Length > 0)
                {
                    LOD maxlod = lodarray[0];
                    renders = maxlod.renderers;
                }
            }

            bool hasrender = false;

            if (renders == null || renders.Length <= 0)
            {
                hasrender = false;
            }
            else
            {
                foreach (var render in renders)
                {
                    if (render == null)
                        continue;

                    hasrender = true;
                }
            }

            if (!hasrender)
            {
                renders = obj.GetComponentsInChildren<Renderer>();

                hasrender = false;

                if (renders == null || renders.Length <= 0)
                {
                    hasrender = false;
                }
                else
                {
                    foreach (var render in renders)
                    {
                        if (render == null)
                            continue;

                        hasrender = true;
                    }
                }

                if (!hasrender)
                {
                    return null;
                }
            }

            return renders;
        }
    }
}
