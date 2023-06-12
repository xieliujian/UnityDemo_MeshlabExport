using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using Unity.Collections;
using System.Reflection;
using gtm.Scene.ModelExport;

namespace gtm.Scene.LODGen
{
    public class LODGenCreate
    {
        /// <summary>
        /// meta后缀
        /// </summary>
        public const string META_SUFFIX = ".meta";

        /// <summary>
        /// LOD后缀
        /// </summary>
        public const string LOD_SUFFIX = "_LOD";

        /// <summary>
        /// 最小一级的LOD百分比
        /// </summary>
        private const float MIN_LOD_PERCENT = 0.01f;

        /// <summary>
        /// 预览保存目录
        /// </summary>
        public const string PREVIEW_SAVE_DIR = "Assets/Temp/LODGen/";

        public class LODInfo
        {
            public LODInfo()
            {

            }

            public LODInfo(LODGenLevel _level, float _reduce, float _lodpercent)
            {
                level = _level;
                reduce = _reduce;
                lodpercent = _lodpercent;
            }

            public LODGenLevel level;
            public float reduce;
            public float lodpercent;
        }

        /// <summary>
        /// 生成目录管理类
        /// </summary>
        public class GenDirManager
        {
            public GenDirManager()
            {

            }

            /// <summary>
            /// Prefab路径
            /// </summary>
            public string prefabPath;

            /// <summary>
            /// 模型路径
            /// </summary>
            public string modelPath;

            /// <summary>
            /// 材质路径
            /// </summary>
            public string matPath;

            /// <summary>
            /// 贴图路径
            /// </summary>
            public string texPath;

            /// <summary>
            /// 源prefab路径
            /// </summary>
            public string srcPrefabPath;

            /// <summary>
            /// .
            /// </summary>
            /// <param name="rootdir"></param>
            /// <param name="tempdir"></param>
            public void ProcessDir(string rootdir, string tempdir)
            {
                if (Directory.Exists(tempdir))
                {
                    Directory.Delete(tempdir, true);
                }

                Directory.CreateDirectory(tempdir);
            }

            /// <summary>
            /// .
            /// </summary>
            /// <param name="go"></param>
            public void Refresh(GameObject go)
            {
                var assetpath = LODGenUtil.GetObjectAssetPath(go);
                if (assetpath == "")
                {
                    assetpath = PREVIEW_SAVE_DIR;
                }

                srcPrefabPath = assetpath;

                assetpath = Path.GetDirectoryName(assetpath);

                prefabPath = assetpath + "/";

                List<string> splitparamlist = new List<string>();
                splitparamlist.Add("\\");
                splitparamlist.Add("/");
                var splitarray = assetpath.Split(splitparamlist.ToArray(), StringSplitOptions.None);

                var newprafabpath = "";
                for (int i = 0; i < (splitarray.Length - 1); i++)
                {
                    var split = splitarray[i];
                    newprafabpath += split + "/";
                }

                modelPath = newprafabpath + "Model/";
                matPath = newprafabpath + "materials/";
                texPath = newprafabpath + "textures/";

                if (!Directory.Exists(modelPath))
                {
                    Directory.CreateDirectory(modelPath);
                }

                if (!Directory.Exists(matPath))
                {
                    Directory.CreateDirectory(matPath);
                }

                if (!Directory.Exists(texPath))
                {
                    Directory.CreateDirectory(texPath);
                }
            }
        }

        /// <summary>
        /// 目录管理类
        /// </summary>
        static GenDirManager m_DirMgr = new GenDirManager();

        /// <summary>
        /// 被保存的prefab列表
        /// </summary>
        static List<string> m_SavePrefabList = new List<string>();

        /// <summary>
        /// 生成单个物件
        /// </summary>
        /// <param name="lodcfg"></param>
        /// <param name="previewMode">是否预览模式</param>
        public static void GenerateObj(LODGenConfig lodcfg)
        {
            if (lodcfg == null)
                return;

            var go = lodcfg.gameObject;
            if (go == null)
                return;

            var cancollect = CanLODCfgCollect(lodcfg);
            if (!cancollect)
                return;

            var newgo = CollectObj(go, go);
            if (newgo == null)
                return;

            ClearSavePrefbList();
            GenObjLOD(newgo, true, false);
        }


        /// <summary>
        /// .
        /// </summary>
        /// <param name="defaultinfolist"></param>
        /// <returns></returns>
        static List<LODInfo> CopyLODInfoList(List<LODInfo> defaultinfolist)
        {
            List<LODInfo> newinfolist = new List<LODInfo>();

            var infocount = defaultinfolist.Count;
            for (int i = 0; i < infocount; i++)
            {
                newinfolist.Add(defaultinfolist[i]);
            }

            return newinfolist;
        }

        /// <summary>
        /// .
        /// </summary>
        /// <param name="lodcfg"></param>
        /// <returns></returns>
        static List<LODInfo> GetDefaultLODInfoList(LODGenConfig lodcfg)
        {
            List<LODInfo> lodinfolist = new List<LODInfo>();
            if (lodcfg == null)
            {
                return lodinfolist;
            }

            var levellist = lodcfg.levelList;
            for (int i = 0; i < levellist.Count; i++)
            {
                LODGenConfig.LODGenLevel level = levellist[i];
                if (level == null)
                    continue;

                if (i >= (int)LODGenLevel.LODCombine)
                    continue;

                LODInfo lodinfo = new LODInfo();
                lodinfo.level = (LODGenLevel)i;
                lodinfo.reduce = level.reducePercent;
                lodinfo.lodpercent = level.lodpercent;
                lodinfolist.Add(lodinfo);
            }

            return lodinfolist;
        }

        /// <summary>
        /// 是否LOD配置可以被收集
        /// </summary>
        /// <param name="lodcfg"></param>
        /// <returns></returns>
        static bool CanLODCfgCollect(LODGenConfig lodcfg)
        {
            if (lodcfg.IsLevelEmpty())
            {
                UnityEditor.EditorUtility.DisplayDialog("LODGen提示", "没有LOD等级，不能生成，最小1级", "确认");
                return false;
            }

            var lodgroup = lodcfg.GetComponentInChildren<LODGroup>();
            if (lodgroup != null)
            {
                UnityEditor.EditorUtility.DisplayDialog("LODGen提示", "不能有LODGroup组件，要删除生成", "确认");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 绑定所有的物件配置成分
        /// </summary>
        /// <param name="golist"></param>
        static void BindAllObjConfigCom(List<KeyValuePair<GameObject, GameObject>> golist, bool lod4layergen)
        {
            foreach (var gopair in golist)
            {
                var go = gopair.Key;
                if (go == null)
                    continue;

                var lodcfgcom = go.GetComponent<LODGenConfig>();
                if (lodcfgcom == null)
                {
                    lodcfgcom = go.AddComponent<LODGenConfig>();

                    // LOD等级
                    var levellist = lodcfgcom.levelList;
                    if (levellist.Count <= 0)
                    {
                        if (lod4layergen)
                        {
                            LODGenConfig.LODGenLevel level1 = new LODGenConfig.LODGenLevel();
                            level1.reducePercent = 0.6f;
                            level1.lodpercent = 0.9f;
                            levellist.Add(level1);

                            LODGenConfig.LODGenLevel level2 = new LODGenConfig.LODGenLevel();
                            level2.reducePercent = 0.4f;
                            level2.lodpercent = 0.7f;
                            levellist.Add(level2);

                            LODGenConfig.LODGenLevel level3 = new LODGenConfig.LODGenLevel();
                            level3.reducePercent = 0.2f;
                            level3.lodpercent = 0.5f;
                            levellist.Add(level3);

                            //LODGenConfig.LODGenLevel level4 = new LODGenConfig.LODGenLevel();
                            //level4.reducePercent = 0.1f;
                            //level4.lodpercent = 0.3f;
                            //levellist.Add(level4);
                        }
                        else
                        {
                            LODGenConfig.LODGenLevel level1 = new LODGenConfig.LODGenLevel();
                            level1.reducePercent = 0.3f;
                            level1.lodpercent = 0.4f;
                            levellist.Add(level1);

                            LODGenConfig.LODGenLevel level2 = new LODGenConfig.LODGenLevel();
                            level2.reducePercent = 0.15f;
                            level2.lodpercent = 0.1f;
                            levellist.Add(level2);
                        }
                    }

                    //// Test
                    //lodcfgcom.m_meshlabReduceParam.preserveboundary = true;
                    //lodcfgcom.m_meshlabReduceParam.boundaryweight = 1000.0f;
                    //lodcfgcom.m_meshlabReduceParam.preservetopology = true;
                }
            }
        }

        /// <summary>
        /// .
        /// </summary>
        /// <param name="golist"></param>
        /// <param name="deltempdir"></param>
        static void Generate(List<GameObject> golist, bool deltempdir)
        {
            List<KeyValuePair<GameObject, GameObject>> newgolist = new List<KeyValuePair<GameObject, GameObject>>();
            foreach(var go in golist)
            {
                if (go == null)
                    continue;

                newgolist.Add(new KeyValuePair<GameObject, GameObject>(go, go));
            }

            // 1. 绑定所有的物件配置成分
            BindAllObjConfigCom(newgolist, false);

            // 2. 收集物件列表
            var newobjlist = CollectObjList(newgolist);

            // 3. 生成所有物体得LOD
            GenAllObjLOD(newobjlist, deltempdir, false);
        }

        /// <summary>
        /// .
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="deltempdir"></param>
        /// <param name="scenegen"></param>
        static void GenObjLOD(LODGenObj obj, bool deltempdir, bool scenegen)
        {
            var saveprefablist = m_SavePrefabList;

            m_DirMgr.Refresh(obj.obj);

            var objprefabname = obj.prefabName;

            var newsavepath = m_DirMgr.prefabPath;

            obj.newPrefabPath = newsavepath + objprefabname + LODGenUtil.PREFAB_SUFFIX;

            var lodcfg = obj.lodCfg;
            if (lodcfg == null)
                return;

            List<LODInfo> defaultlodinfolist = GetDefaultLODInfoList(lodcfg);
            if (defaultlodinfolist.Count <= 0)
                return;

            float minReduceFaceNum = lodcfg.minReduceFaceNum;

            var temppath = newsavepath + LODGenUtil.TEMP_DIR_PREFIX + "/";

            // 场景生成的情况下，已经存在不再生成，避免加载时间过长
            if (scenegen)
            {
                if (Directory.Exists(newsavepath))
                {
                    return;
                }
            }

            m_DirMgr.ProcessDir(newsavepath, temppath);

            var lodinfolist = CopyLODInfoList(defaultlodinfolist);

            // 存放lod的prefab资源， 作为后面处理
            Dictionary<string, string> lodprefabdict = new Dictionary<string, string>();

            // 1.1 处理每个子Mesh
            foreach (var subobj in obj.subObjList)
            {
                if (subobj == null)
                    continue;

                var subobjname = subobj.meshName;
                var canreduceface = lodcfg.CanReduceFace(subobj.faceCount, subobj.render as MeshRenderer);

                subobj.SaveAsPrefab(temppath, subobjname);

                // a. Fbx模型导出
                if (canreduceface)
                {
                    FbxExport.ExportLOD(temppath, subobjname, true, "");
                }

                // b. meshlab文件导出
                foreach (var lodinfo in lodinfolist)
                {
                    if (lodinfo == null)
                        continue;

                    var lodlevel = lodinfo.level;
                    var lodreduce = lodinfo.reduce;
                    var lodsuffix = "_" + lodlevel.ToString();
                    var meshlabpbpath = "";

                    if (!canreduceface)
                    {
                        Debug.Log($"[xlj][LODGen] {objprefabname} 的子模型 FaceCount < {minReduceFaceNum}");

                        var newname = subobjname + lodsuffix + LODGenUtil.MESHLAB_SUFFIX_WITHOUT_EXTENSION;
                        subobj.SaveAsPrefab(temppath, newname);

                        meshlabpbpath = temppath + newname + LODGenUtil.PREFAB_SUFFIX;
                    }
                    else
                    {
                        // Meshlab减面操作
                        meshlabpbpath = MeshlabExport.ExportLOD(temppath, subobjname, lodreduce, subobj.matNameList, lodsuffix, lodcfg.meshlabReduceParam);
                    }

                    // 存储数据供自定义模型生成读取
                    var lodname = subobjname + lodsuffix;
                    if (!lodprefabdict.ContainsKey(lodname))
                    {
                        lodprefabdict.Add(lodname, meshlabpbpath);
                    }

                    // 存取LOD prefab数据供后期读取
                    obj.AddLODPrefabDict((int)lodlevel, meshlabpbpath);
                }
            }

            // 2. 导出*.lod模型prefab文件
            ExportLODModelPrefab(obj, newsavepath, lodprefabdict, lodinfolist);

            // 3. 删除临时目录
            if (deltempdir)
            {
                temppath = newsavepath + LODGenUtil.TEMP_DIR_PREFIX;

                if (Directory.Exists(temppath))
                {
                    var rootmetafile = temppath + META_SUFFIX;
                    if (File.Exists(rootmetafile))
                    {
                        File.Delete(rootmetafile);
                    }

                    Directory.Delete(temppath, true);
                    AssetDatabase.Refresh();
                }
            }

            saveprefablist.Add(m_DirMgr.srcPrefabPath);
        }

        /// <summary>
        /// .
        /// </summary>
        static void ClearSavePrefbList()
        {
            m_SavePrefabList.Clear();
        }

        /// <summary>
        /// 生成所有的物件LOD
        /// </summary>
        /// <param name="rootpath"></param>
        /// <param name="objlist"></param>
        /// <param name="defaultlodinfolist"></param>
        /// <param name="deltempdir"></param>
        /// <param name="scenegen">是否场景生成</param>
        static void GenAllObjLOD(List<LODGenObj> objlist, bool deltempdir, bool scenegen)
        {
            foreach (var obj in objlist)
            {
                if (obj == null)
                    continue;

                GenObjLOD(obj, deltempdir, scenegen);
            }
        }

        /// <summary>
        /// .
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="lodprefabdict"></param>
        /// <param name="lodinfolist"></param>
        /// <returns></returns>
        static string ExportLODPrefab(LODGenObj obj, Dictionary<string, string> lodprefabdict, List<LODInfo> lodinfolist)
        {
            if (lodinfolist.Count <= 0)
                return "";

            // 导出prefab数据
            var objname = obj.prefabName;

            var prefabpath = m_DirMgr.prefabPath + objname + LODGenUtil.PREFAB_SUFFIX;
            var fbxpath = m_DirMgr.modelPath + objname + LOD_SUFFIX + LODGenUtil.FBX_SUFFIX;

            GameObject go = new GameObject(objname);

            LODGroup lodgroup = go.AddComponent<LODGroup>();
            List<LOD> lodlist = new List<LOD>();

            // 1. 源模型
            GameObject srcgo = GameObject.Instantiate(obj.obj);
            srcgo.name = objname;
            srcgo.transform.SetParent(go.transform);
            LODGenUtil.ResetTransform(srcgo.transform);

            // 删除LODGenConfig
            var lodcfgscript = srcgo.GetComponentInChildren<LODGenConfig>();
            if (lodcfgscript != null)
            {
                GameObject.DestroyImmediate(lodcfgscript);
            }

            var minlodpercent = lodinfolist[0].lodpercent;
            LOD srclod = LODGenUtil.GetLOD(srcgo, minlodpercent);
            lodlist.Add(srclod);

            LODGenUtil.RemoveLODGroup(srcgo);

            // 2. lod模型

            // 减面数值列表
            List<float> lodreducelist = new List<float>();
            for (int i = 1; i < lodinfolist.Count; i++)
            {
                var lodinfo = lodinfolist[i];
                if (lodinfo == null)
                    continue;

                lodreducelist.Add(lodinfo.lodpercent);
            }
            lodreducelist.Add(MIN_LOD_PERCENT);

            for (int i = 0; i < lodinfolist.Count; i++)
            {
                var lodinfo = lodinfolist[i];
                if (lodinfo == null)
                    continue;

                var lodlevel = lodinfo.level;
                var reduce = lodreducelist[i];
                var lodsuffix = "_" + lodlevel.ToString();

                GameObject lodgo = new GameObject(objname + lodsuffix);
                lodgo.transform.SetParent(go.transform);
                LODGenUtil.ResetTransform(lodgo.transform);

                var pbpathlist = obj.lODPrefabDict[(int)lodinfo.level];
                if (pbpathlist != null)
                {
                    foreach (var pbpath in pbpathlist)
                    {
                        if (pbpath == null)
                            continue;

                        var pb = AssetDatabase.LoadAssetAtPath<GameObject>(pbpath);
                        if (pb == null)
                            continue;

                        GameObject pbgo = GameObject.Instantiate(pb);
                        if (pbgo == null)
                            continue;

                        MeshFilter meshfilter = pbgo.GetComponentInChildren<MeshFilter>();
                        if (meshfilter == null)
                        {
                            GameObject.DestroyImmediate(pbgo);
                            continue;
                        }

                        var filename = Path.GetFileNameWithoutExtension(pbpath);
                        filename = filename.Replace(LODGenUtil.MESHLAB_SUFFIX_WITHOUT_EXTENSION, "");

                        UnityEngine.Object findfile = null;
                        var allfbxfile = AssetDatabase.LoadAllAssetsAtPath(fbxpath);
                        foreach(var fbxfile in allfbxfile)
                        {
                            if (fbxfile == null)
                                continue;

                            Mesh fbxmeshfile = fbxfile as Mesh;
                            if (fbxmeshfile == null)
                                continue;

                            if (fbxmeshfile.name.Contains(filename))
                            {
                                findfile = fbxfile;
                                break;
                            }
                        }

                        if (findfile == null)
                        {
                            GameObject.DestroyImmediate(pbgo);
                            continue;
                        }

                        Mesh fbxmesh = findfile as Mesh;
                        if (fbxmesh == null)
                        {
                            GameObject.DestroyImmediate(pbgo);
                            continue;
                        }

                        GameObject newpbgo = GameObject.Instantiate(meshfilter.gameObject);
                        if (newpbgo == null)
                        {
                            GameObject.DestroyImmediate(pbgo);
                            continue;
                        }

                        newpbgo.name = filename;
                        newpbgo.transform.SetParent(lodgo.transform, false);
                        LODGenUtil.ResetTransform(newpbgo.transform);

                        MeshFilter newmeshfilter = newpbgo.GetComponentInChildren<MeshFilter>();
                        if (newmeshfilter != null)
                        {
                            newmeshfilter.mesh = fbxmesh;
                        }

                        GameObject.DestroyImmediate(pbgo);
                    }
                }

                LOD lodlod = LODGenUtil.GetLOD(lodgo, reduce);
                lodlist.Add(lodlod);
            }

            // 设置LODGroup信息
            lodgroup.SetLODs(lodlist.ToArray());

            // 设置为静态，需要烘培lightmap
            // 这里不能GetComponentsInChildren<GameObject>() 来获取成分， 会报错， 应该获取Transform
            go.isStatic = true;
            var allstatictrans = go.GetComponentsInChildren<Transform>();
            foreach (var statictrans in allstatictrans)
            {
                if (statictrans == null || statictrans.gameObject == null)
                    continue;

                statictrans.gameObject.isStatic = true;
            }

            //// Test
            //PrefabUtility.ApplyPrefabInstance(go, InteractionMode.AutomatedAction);

            PrefabUtility.SaveAsPrefabAsset(go, prefabpath);

            GameObject.DestroyImmediate(go);

            return prefabpath;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="lodprefabdict"></param>
        /// <param name="lodinfolist"></param>
        static void ExportLODModel(LODGenObj obj, Dictionary<string, string> lodprefabdict, List<LODInfo> lodinfolist)
        {
            // 导出prefab
            var objname = obj.prefabName;
            var savepath = m_DirMgr.modelPath;

            string modelpbpath = savepath + objname + LODGenUtil.PREFAB_SUFFIX;

            GameObject go = new GameObject(objname);

            foreach (var iter in lodprefabdict)
            {
                var lodpbpath = iter.Value;
                var lodpb = AssetDatabase.LoadAssetAtPath<GameObject>(lodpbpath);
                if (lodpb == null)
                    continue;

                GameObject lodgo = GameObject.Instantiate(lodpb);
                if (lodgo == null)
                    continue;

                MeshRenderer meshrender = lodgo.GetComponentInChildren<MeshRenderer>();
                if (meshrender == null)
                    continue;

                var filename = Path.GetFileNameWithoutExtension(lodpbpath);
                filename = filename.Replace(LODGenUtil.MESHLAB_SUFFIX_WITHOUT_EXTENSION, "");

                lodgo.name = filename + "_Go";
                meshrender.name = filename;

                lodgo.transform.SetParent(go.transform);
                LODGenUtil.ResetTransform(lodgo.transform);
            }

            PrefabUtility.SaveAsPrefabAsset(go, modelpbpath);
            GameObject.DestroyImmediate(go);

            // 导出fbx
            var modelfbxpath = FbxExport.ExportLOD(savepath, objname, LOD_SUFFIX);

            ModelImporter modelimport = AssetImporter.GetAtPath(modelfbxpath) as ModelImporter;
            if (modelimport != null)
            {
                modelimport.materialImportMode = ModelImporterMaterialImportMode.None;
                modelimport.importAnimation = false;
                modelimport.animationType = ModelImporterAnimationType.None;
                modelimport.generateSecondaryUV = true;

                // 模型导入法线使用计算模式
                modelimport.importNormals = ModelImporterNormals.Calculate;

                modelimport.SaveAndReimport();
            }

            // 删除数据
            File.Delete(modelpbpath);
            AssetDatabase.Refresh();

        }

        /// <summary>
        /// 新的prefab导出格式，可以支持lightmap生成
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="savepath"></param>
        /// <param name="lodprefabdict"></param>
        /// <param name="lodinfolist"></param>
        /// <returns></returns>
        static string ExportLODModelPrefab(LODGenObj obj, string savepath, Dictionary<string, string> lodprefabdict, List<LODInfo> lodinfolist)
        {
            ExportLODModel(obj, lodprefabdict, lodinfolist);
            return ExportLODPrefab(obj, lodprefabdict, lodinfolist);
        }

        /// <summary>
        /// 收集单个物件
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        static LODGenObj CollectObj(GameObject obj, GameObject srcobj)
        {
            var lodcfg = obj.GetComponentInChildren<LODGenConfig>();
            if (lodcfg == null)
                return null;

            Renderer[] renders = LODGenUtil.GetLodRenderArray(obj);
            if (renders == null || renders.Length <= 0)
                return null;

            List<MeshRenderer> renderlist = LODGenUtil.GetUsedRenderArray(renders);
            if (renderlist == null || renderlist.Count <= 0)
                return null;

            var objpbname = LODGenUtil.GetPrefabName(obj.name);

            LODGenObj newobj = new LODGenObj();
            newobj.obj = obj;
            newobj.srcObj = srcobj;
            newobj.lodCfg = lodcfg;
            newobj.prefabName = objpbname;

            foreach (var meshrender in renderlist)
            {
                LODGenSubObj newsubobj = new LODGenSubObj();
                newsubobj.meshName = LODGenUtil.GetPrefabName(meshrender.name);
                newsubobj.render = meshrender;
                newsubobj.CalcMat(meshrender.sharedMaterials);
                newsubobj.CalcFace();
                newobj.AddSubObj(newsubobj);
            }

            return newobj;
        }

        /// <summary>
        /// .
        /// </summary>
        /// <param name="golist"></param>
        /// <returns></returns>
        static List<LODGenObj> CollectObjList(List<KeyValuePair<GameObject, GameObject>> golist)
        {
            List<LODGenObj> newobjlist = new List<LODGenObj>();

            foreach (var gopair in golist)
            {
                var obj = gopair.Key;
                var srcobj = gopair.Value;
                if (obj == null)
                    continue;

                var newobj = CollectObj(obj, srcobj);
                if (newobj == null)
                    continue;

                if (newobj.subObjList.Count > 0)
                {
                    newobjlist.Add(newobj);
                }
            }

            return newobjlist;
        }
    }
}
