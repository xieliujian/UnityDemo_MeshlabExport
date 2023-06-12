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
        /// meta��׺
        /// </summary>
        public const string META_SUFFIX = ".meta";

        /// <summary>
        /// LOD��׺
        /// </summary>
        public const string LOD_SUFFIX = "_LOD";

        /// <summary>
        /// ��Сһ����LOD�ٷֱ�
        /// </summary>
        private const float MIN_LOD_PERCENT = 0.01f;

        /// <summary>
        /// Ԥ������Ŀ¼
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
        /// ����Ŀ¼������
        /// </summary>
        public class GenDirManager
        {
            public GenDirManager()
            {

            }

            /// <summary>
            /// Prefab·��
            /// </summary>
            public string prefabPath;

            /// <summary>
            /// ģ��·��
            /// </summary>
            public string modelPath;

            /// <summary>
            /// ����·��
            /// </summary>
            public string matPath;

            /// <summary>
            /// ��ͼ·��
            /// </summary>
            public string texPath;

            /// <summary>
            /// Դprefab·��
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
        /// Ŀ¼������
        /// </summary>
        static GenDirManager m_DirMgr = new GenDirManager();

        /// <summary>
        /// �������prefab�б�
        /// </summary>
        static List<string> m_SavePrefabList = new List<string>();

        /// <summary>
        /// ���ɵ������
        /// </summary>
        /// <param name="lodcfg"></param>
        /// <param name="previewMode">�Ƿ�Ԥ��ģʽ</param>
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
        /// �Ƿ�LOD���ÿ��Ա��ռ�
        /// </summary>
        /// <param name="lodcfg"></param>
        /// <returns></returns>
        static bool CanLODCfgCollect(LODGenConfig lodcfg)
        {
            if (lodcfg.IsLevelEmpty())
            {
                UnityEditor.EditorUtility.DisplayDialog("LODGen��ʾ", "û��LOD�ȼ����������ɣ���С1��", "ȷ��");
                return false;
            }

            var lodgroup = lodcfg.GetComponentInChildren<LODGroup>();
            if (lodgroup != null)
            {
                UnityEditor.EditorUtility.DisplayDialog("LODGen��ʾ", "������LODGroup�����Ҫɾ������", "ȷ��");
                return false;
            }

            return true;
        }

        /// <summary>
        /// �����е�������óɷ�
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

                    // LOD�ȼ�
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

            // 1. �����е�������óɷ�
            BindAllObjConfigCom(newgolist, false);

            // 2. �ռ�����б�
            var newobjlist = CollectObjList(newgolist);

            // 3. �������������LOD
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

            // �������ɵ�����£��Ѿ����ڲ������ɣ��������ʱ�����
            if (scenegen)
            {
                if (Directory.Exists(newsavepath))
                {
                    return;
                }
            }

            m_DirMgr.ProcessDir(newsavepath, temppath);

            var lodinfolist = CopyLODInfoList(defaultlodinfolist);

            // ���lod��prefab��Դ�� ��Ϊ���洦��
            Dictionary<string, string> lodprefabdict = new Dictionary<string, string>();

            // 1.1 ����ÿ����Mesh
            foreach (var subobj in obj.subObjList)
            {
                if (subobj == null)
                    continue;

                var subobjname = subobj.meshName;
                var canreduceface = lodcfg.CanReduceFace(subobj.faceCount, subobj.render as MeshRenderer);

                subobj.SaveAsPrefab(temppath, subobjname);

                // a. Fbxģ�͵���
                if (canreduceface)
                {
                    FbxExport.ExportLOD(temppath, subobjname, true, "");
                }

                // b. meshlab�ļ�����
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
                        Debug.Log($"[xlj][LODGen] {objprefabname} ����ģ�� FaceCount < {minReduceFaceNum}");

                        var newname = subobjname + lodsuffix + LODGenUtil.MESHLAB_SUFFIX_WITHOUT_EXTENSION;
                        subobj.SaveAsPrefab(temppath, newname);

                        meshlabpbpath = temppath + newname + LODGenUtil.PREFAB_SUFFIX;
                    }
                    else
                    {
                        // Meshlab�������
                        meshlabpbpath = MeshlabExport.ExportLOD(temppath, subobjname, lodreduce, subobj.matNameList, lodsuffix, lodcfg.meshlabReduceParam);
                    }

                    // �洢���ݹ��Զ���ģ�����ɶ�ȡ
                    var lodname = subobjname + lodsuffix;
                    if (!lodprefabdict.ContainsKey(lodname))
                    {
                        lodprefabdict.Add(lodname, meshlabpbpath);
                    }

                    // ��ȡLOD prefab���ݹ����ڶ�ȡ
                    obj.AddLODPrefabDict((int)lodlevel, meshlabpbpath);
                }
            }

            // 2. ����*.lodģ��prefab�ļ�
            ExportLODModelPrefab(obj, newsavepath, lodprefabdict, lodinfolist);

            // 3. ɾ����ʱĿ¼
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
        /// �������е����LOD
        /// </summary>
        /// <param name="rootpath"></param>
        /// <param name="objlist"></param>
        /// <param name="defaultlodinfolist"></param>
        /// <param name="deltempdir"></param>
        /// <param name="scenegen">�Ƿ񳡾�����</param>
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

            // ����prefab����
            var objname = obj.prefabName;

            var prefabpath = m_DirMgr.prefabPath + objname + LODGenUtil.PREFAB_SUFFIX;
            var fbxpath = m_DirMgr.modelPath + objname + LOD_SUFFIX + LODGenUtil.FBX_SUFFIX;

            GameObject go = new GameObject(objname);

            LODGroup lodgroup = go.AddComponent<LODGroup>();
            List<LOD> lodlist = new List<LOD>();

            // 1. Դģ��
            GameObject srcgo = GameObject.Instantiate(obj.obj);
            srcgo.name = objname;
            srcgo.transform.SetParent(go.transform);
            LODGenUtil.ResetTransform(srcgo.transform);

            // ɾ��LODGenConfig
            var lodcfgscript = srcgo.GetComponentInChildren<LODGenConfig>();
            if (lodcfgscript != null)
            {
                GameObject.DestroyImmediate(lodcfgscript);
            }

            var minlodpercent = lodinfolist[0].lodpercent;
            LOD srclod = LODGenUtil.GetLOD(srcgo, minlodpercent);
            lodlist.Add(srclod);

            LODGenUtil.RemoveLODGroup(srcgo);

            // 2. lodģ��

            // ������ֵ�б�
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

            // ����LODGroup��Ϣ
            lodgroup.SetLODs(lodlist.ToArray());

            // ����Ϊ��̬����Ҫ����lightmap
            // ���ﲻ��GetComponentsInChildren<GameObject>() ����ȡ�ɷ֣� �ᱨ�� Ӧ�û�ȡTransform
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
            // ����prefab
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

            // ����fbx
            var modelfbxpath = FbxExport.ExportLOD(savepath, objname, LOD_SUFFIX);

            ModelImporter modelimport = AssetImporter.GetAtPath(modelfbxpath) as ModelImporter;
            if (modelimport != null)
            {
                modelimport.materialImportMode = ModelImporterMaterialImportMode.None;
                modelimport.importAnimation = false;
                modelimport.animationType = ModelImporterAnimationType.None;
                modelimport.generateSecondaryUV = true;

                // ģ�͵��뷨��ʹ�ü���ģʽ
                modelimport.importNormals = ModelImporterNormals.Calculate;

                modelimport.SaveAndReimport();
            }

            // ɾ������
            File.Delete(modelpbpath);
            AssetDatabase.Refresh();

        }

        /// <summary>
        /// �µ�prefab������ʽ������֧��lightmap����
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
        /// �ռ��������
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
