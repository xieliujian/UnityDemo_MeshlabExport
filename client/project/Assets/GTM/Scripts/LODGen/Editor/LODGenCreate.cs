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
        public const string GMetaSuffix = ".meta";

        /// <summary>
        /// LOD��׺
        /// </summary>
        public const string GLODSuffix = "_LOD";

        /// <summary>
        /// ��Сһ����LOD�ٷֱ�
        /// </summary>
        private const float GMinLODPercent = 0.01f;

        // Ԥ������Ŀ¼
        public const string GPreviewSaveDir = "Assets/Temp/LODGen/";

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
            public string PrefabPath;

            /// <summary>
            /// ģ��·��
            /// </summary>
            public string ModelPath;

            /// <summary>
            /// ����·��
            /// </summary>
            public string MatPath;

            /// <summary>
            /// ��ͼ·��
            /// </summary>
            public string TexPath;

            /// <summary>
            /// Դprefab·��
            /// </summary>
            public string SrcPrefabPath;

            public void ProcessDir(string rootdir, string tempdir)
            {
                if (Directory.Exists(tempdir))
                {
                    Directory.Delete(tempdir, true);
                }

                Directory.CreateDirectory(tempdir);
            }

            public void Refresh(GameObject go)
            {
                var assetpath = LODGenUtil.GetObjectAssetPath(go);
                if (assetpath == "")
                {
                    assetpath = GPreviewSaveDir;
                }

                SrcPrefabPath = assetpath;

                assetpath = Path.GetDirectoryName(assetpath);

                PrefabPath = assetpath + "/";

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

                ModelPath = newprafabpath + "Model/";
                MatPath = newprafabpath + "materials/";
                TexPath = newprafabpath + "textures/";

                if (!Directory.Exists(ModelPath))
                {
                    Directory.CreateDirectory(ModelPath);
                }

                if (!Directory.Exists(MatPath))
                {
                    Directory.CreateDirectory(MatPath);
                }

                if (!Directory.Exists(TexPath))
                {
                    Directory.CreateDirectory(TexPath);
                }
            }
        }

        /// <summary>
        /// Ŀ¼������
        /// </summary>
        private static GenDirManager m_dirMgr = new GenDirManager();

        public static List<LODInfo> CopyLODInfoList(List<LODInfo> defaultinfolist)
        {
            List<LODInfo> newinfolist = new List<LODInfo>();

            var infocount = defaultinfolist.Count;
            for (int i = 0; i < infocount; i++)
            {
                newinfolist.Add(defaultinfolist[i]);
            }

            return newinfolist;
        }

        public static List<LODInfo> GetDefaultLODInfoList(LODGenConfig lodcfg)
        {
            List<LODInfo> lodinfolist = new List<LODInfo>();
            if (lodcfg == null)
            {
                return lodinfolist;
            }

            var levellist = lodcfg.m_levelList;
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
        private static bool CanLODCfgCollect(LODGenConfig lodcfg)
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
        /// �����е�������óɷ�
        /// </summary>
        /// <param name="golist"></param>
        private static void BindAllObjConfigCom(List<KeyValuePair<GameObject, GameObject>> golist, bool lod4layergen)
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
                    var levellist = lodcfgcom.m_levelList;
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

        public static void Generate(List<GameObject> golist, bool deltempdir)
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
        /// ���ɲ��滻�����е�obj instance
        /// </summary>
        /// <param name="rootpath"></param>
        /// <param name="golist"></param>
        /// <param name="deltempdir"></param>
        public static void GenAndReplaceObjInstance(string rootpath, List<GameObject> golist, bool deltempdir)
        {
            rootpath = rootpath + "/";

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
            GenAllObjLOD(newobjlist, deltempdir, true);
            ModifyAllSceneObj(newobjlist);
        }

        /// <summary>
        /// �޸����еĳ���obj
        /// </summary>
        /// <param name="objlist"></param>
        private static void ModifyAllSceneObj(List<LODGenObj> objlist)
        {
            foreach (var obj in objlist)
            {
                if (obj == null)
                    continue;

                var srcobj = obj.Obj;
                if (srcobj == null)
                    continue;

                var newpbpath = obj.NewPrefabPath;
                if (string.IsNullOrEmpty(newpbpath))
                    continue;

                var newpb = AssetDatabase.LoadAssetAtPath<GameObject>(newpbpath);
                if (newpb == null)
                    continue;

                var newpbgo = PrefabUtility.InstantiatePrefab(newpb) as GameObject;
                if (newpbgo == null)
                    continue;

                newpbgo.name = srcobj.name;

                LODGenUtil.CopyTransform(newpbgo.transform, srcobj.transform);

                GameObject.DestroyImmediate(srcobj);
            }
        }

        private static void GenObjLOD(LODGenObj obj, bool deltempdir, bool scenegen)
        {
            var saveprefablist = m_saveprefablist;

            m_dirMgr.Refresh(obj.Obj);

            var objprefabname = obj.PrefabName;

            var newsavepath = m_dirMgr.PrefabPath;

            obj.NewPrefabPath = newsavepath + objprefabname + LODGenUtil.PREFAB_SUFFIX;

            var lodcfg = obj.LODCfg;
            if (lodcfg == null)
                return;

            List<LODInfo> defaultlodinfolist = GetDefaultLODInfoList(lodcfg);
            if (defaultlodinfolist.Count <= 0)
                return;

            float minReduceFaceNum = lodcfg.m_minReduceFaceNum;

            var temppath = newsavepath + LODGenUtil.TEMP_DIR_PREFIX + "/";

            // �������ɵ�����£��Ѿ����ڲ������ɣ��������ʱ�����
            if (scenegen)
            {
                if (Directory.Exists(newsavepath))
                {
                    return;
                }
            }

            m_dirMgr.ProcessDir(newsavepath, temppath);

            var lodinfolist = CopyLODInfoList(defaultlodinfolist);

            // ���lod��prefab��Դ�� ��Ϊ���洦��
            Dictionary<string, string> lodprefabdict = new Dictionary<string, string>();

            // 1.1 ����ÿ����Mesh
            foreach (var subobj in obj.SubObjList)
            {
                if (subobj == null)
                    continue;

                var subobjname = subobj.MeshName;
                var canreduceface = lodcfg.CanReduceFace(subobj.FaceCount, subobj.Render as MeshRenderer);

                subobj.SaveAsPrefab(temppath, subobjname);

                // a. Fbxģ�͵���
                if (canreduceface)
                {
                    FbxExport.ExportLOD(temppath, subobjname, HLODMaterialType.Empty, false, true, "");
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
                        meshlabpbpath = MeshlabExport.ExportLOD(temppath, subobjname, lodreduce, subobj.MatNameList, lodsuffix, lodcfg.m_meshlabReduceParam);
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
            ExportLODModelPrefabNew(obj, newsavepath, lodprefabdict, lodinfolist);

            // 3. ɾ����ʱĿ¼
            if (deltempdir)
            {
                temppath = newsavepath + LODGenUtil.TEMP_DIR_PREFIX;

                if (Directory.Exists(temppath))
                {
                    var rootmetafile = temppath + GMetaSuffix;
                    if (File.Exists(rootmetafile))
                    {
                        File.Delete(rootmetafile);
                    }

                    Directory.Delete(temppath, true);
                    AssetDatabase.Refresh();
                }
            }

            saveprefablist.Add(m_dirMgr.SrcPrefabPath);
        }

        /// <summary>
        /// �������prefab�б�
        /// </summary>
        public static List<string> m_saveprefablist = new List<string>();

        public static void ClearSavePrefbList()
        {
            m_saveprefablist.Clear();
        }

        /// <summary>
        /// �������е����LOD
        /// </summary>
        /// <param name="rootpath"></param>
        /// <param name="objlist"></param>
        /// <param name="defaultlodinfolist"></param>
        /// <param name="deltempdir"></param>
        /// <param name="scenegen">�Ƿ񳡾�����</param>
        private static void GenAllObjLOD(List<LODGenObj> objlist, bool deltempdir, bool scenegen)
        {
            foreach (var obj in objlist)
            {
                if (obj == null)
                    continue;

                GenObjLOD(obj, deltempdir, scenegen);
            }
        }

        /// <summary>
        /// ����ָ��meshlab��prefab�Ĳ��ʺ���ͼ
        /// </summary>
        private static void ReassignMeshlabPrefabMatAndTex(string meshlabpbpath, string savepath, string savename)
        {
            var meshlabpb = AssetDatabase.LoadAssetAtPath<GameObject>(meshlabpbpath);
            if (meshlabpb == null)
                return;

            MeshRenderer meshrender = meshlabpb.GetComponentInChildren<MeshRenderer>();
            if (meshrender == null)
                return;

            Material mat = meshrender.sharedMaterial;
            if (mat == null)
                return;

            Texture2D maintex = mat.mainTexture as Texture2D;
            if (maintex == null)
                return;

            // ������ͼ
            var srctexpath = AssetDatabase.GetAssetPath(maintex);
            var newtexpathdir = m_dirMgr.TexPath + "/";
            string newtexfilepath = newtexpathdir + savename + ".png";

            File.Copy(srctexpath, newtexfilepath, true);

            AssetDatabase.Refresh();

            var texImporter = AssetImporter.GetAtPath(newtexfilepath) as TextureImporter;
            if (texImporter != null)
            {
                TextureImporterPlatformSettings platformset = new TextureImporterPlatformSettings();

                // Android
                platformset.overridden = true;
                platformset.name = "Android";
                platformset.maxTextureSize = 256;
                platformset.format = TextureImporterFormat.ASTC_4x4;
                texImporter.SetPlatformTextureSettings(platformset);

                platformset.overridden = true;
                platformset.name = "iPhone";
                platformset.maxTextureSize = 256;
                platformset.format = TextureImporterFormat.ASTC_4x4;
                texImporter.SetPlatformTextureSettings(platformset);

                texImporter.SaveAndReimport();
            }

            AssetDatabase.Refresh();

            // ���Ʋ���
            Material newmat = new Material(mat);

            var storedTexture = AssetDatabase.LoadAssetAtPath<Texture>(newtexfilepath);
            // �������д���ģ���shader�������޸ĵ�ʱ����ܳ�����
            newmat.SetTexture("_BaseMap", storedTexture);

            var newmatpathdir = m_dirMgr.MatPath + "/";
            string newmatfilepath = newmatpathdir + savename + ".mat";

            AssetDatabase.CreateAsset(newmat, newmatfilepath);
            AssetDatabase.ImportAsset(newmatfilepath);

            newmat = AssetDatabase.LoadAssetAtPath<Material>(newmatfilepath);
            meshrender.material = newmat;

            AssetDatabase.Refresh();
        }

        private static string ExportLODPrefabNew(LODGenObj obj, Dictionary<string, string> lodprefabdict, List<LODInfo> lodinfolist)
        {
            if (lodinfolist.Count <= 0)
                return "";

            // ����prefab����
            var objname = obj.PrefabName;

            var prefabpath = m_dirMgr.PrefabPath + objname + LODGenUtil.PREFAB_SUFFIX;
            var fbxpath = m_dirMgr.ModelPath + objname + GLODSuffix + LODGenUtil.FBX_SUFFIX;

            GameObject go = new GameObject(objname);

            LODGroup lodgroup = go.AddComponent<LODGroup>();
            List<LOD> lodlist = new List<LOD>();

            // 1. Դģ��
            GameObject srcgo = GameObject.Instantiate(obj.Obj);
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
            lodreducelist.Add(GMinLODPercent);

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

                var pbpathlist = obj.LODPrefabDict[(int)lodinfo.level];
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

        private static void ExportLODModelNew(LODGenObj obj, Dictionary<string, string> lodprefabdict, List<LODInfo> lodinfolist)
        {
            // ����prefab
            var objname = obj.PrefabName;
            var savepath = m_dirMgr.ModelPath;

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
            var modelfbxpath = FbxExport.ExportLOD(savepath, objname, GLODSuffix);

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
        private static string ExportLODModelPrefabNew(LODGenObj obj, string savepath, Dictionary<string, string> lodprefabdict, List<LODInfo> lodinfolist)
        {
            ExportLODModelNew(obj, lodprefabdict, lodinfolist);
            return ExportLODPrefabNew(obj, lodprefabdict, lodinfolist);
        }

#if false

        /// <summary>
        /// �ɰ汾prefab������ʽ����Ҫ�Զ���ģ�������ļ���Ŀǰ�������Զ������ɵ�ģ���ļ�����������ȷ�Ķ���uv
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="savepath"></param>
        /// <param name="lodprefabdict"></param>
        /// <param name="lodinfolist"></param>
        /// <returns></returns>
        private static string ExportLODModelPrefab(LODGenObj obj, string savepath, Dictionary<string, string> lodprefabdict, List<LODInfo> lodinfolist)
        {
            // 1. ����ģ������
            string filename = savepath + obj.PrefabName + "." + LODGenUtil.LOD_MODEL_SUFFIX;
            HLODData.TextureCompressionData compressionData = HLODUtility.GetTextureCompressionData();

            using (Stream stream = new FileStream(filename, FileMode.Create))
            {
                HLODData data = new HLODData();
                data.CompressionData = compressionData;

                foreach (var iter in lodprefabdict)
                {
                    var lodpbname = iter.Key;
                    var lodpbpath = iter.Value;

                    var lodpb = AssetDatabase.LoadAssetAtPath<GameObject>(lodpbpath);

                    // ���ֲ���Ҫ_Meshlab��׺
                    lodpb.name = lodpb.name.Replace(LODGenUtil.MESHLAB_SUFFIX_WITHOUT_EXTENSION, "");

                    if (lodpb != null)
                    {
                        data.AddFromGameObject(lodpb, false);
                    }
                }

                HLODDataSerializer.Write(stream, data);
            }

            AssetDatabase.ImportAsset(filename, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();

            // 2. ����prefab����
            var objname = obj.PrefabName;
            var prefabpath = savepath + objname + LODGenUtil.PREFAB_SUFFIX;
            GameObject go = new GameObject(objname);

            LODGroup lodgroup = go.AddComponent<LODGroup>();
            List<LOD> lodlist = new List<LOD>();

            // 1. Դģ��
            GameObject srcgo = GameObject.Instantiate(obj.Obj);
            srcgo.name = objname;
            srcgo.transform.SetParent(go.transform);
            LODGenUtil.ResetTransform(srcgo.transform);

            LOD srclod = LODGenUtil.GetLOD(srcgo, 0.4f);
            lodlist.Add(srclod);

            LODGenUtil.RemoveLODGroup(srcgo);

            // 2. lodģ��
            RootData rootData = AssetDatabase.LoadAssetAtPath<RootData>(filename);

            var maxobjsize = LODGenUtil.GetObjBoundsMaxSize(srcgo);

            // ������ֵ�б�
            List<float> lodreducelist = new List<float>();
            lodreducelist.Add(0.1f);
            lodreducelist.Add(0.01f);

            for (int i = 0; i < lodinfolist.Count; i++)
            {
                var lodinfo = lodinfolist[i];
                if (lodinfo == null)
                    continue;

                // ��ֹ����Խ��
                if (i > (lodreducelist.Count - 1))
                    continue;

                var lodlevel = lodinfo.level;
                var reduce = lodreducelist[i];
                var lodsuffix = "_" + lodlevel.ToString();

                GameObject lodgo = new GameObject(objname + lodsuffix);
                lodgo.transform.SetParent(go.transform);
                LODGenUtil.ResetTransform(lodgo.transform);
                //lodgo.transform.localPosition = new Vector3(maxobjsize * (i + 1), 0.0f, 0.0f);

                foreach (var keypair in rootData.rootObjects)
                {
                    var prefab = keypair.Value;
                    var prefabname = prefab.name;

                    if (prefabname.Contains(lodsuffix))
                    {
                        GameObject lodmeshgo = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                        lodmeshgo.transform.SetParent(lodgo.transform, false);
                    }
                }

                LOD lodlod = LODGenUtil.GetLOD(lodgo, reduce);
                lodlist.Add(lodlod);
            }

            // ����LODGroup��Ϣ
            lodgroup.SetLODs(lodlist.ToArray());

            PrefabUtility.SaveAsPrefabAsset(go, prefabpath);
            GameObject.DestroyImmediate(go);

            return prefabpath;
        }

#endif

        /// <summary>
        /// �ռ��������
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static LODGenObj CollectObj(GameObject obj, GameObject srcobj)
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
            newobj.Obj = obj;
            newobj.SrcObj = srcobj;
            newobj.LODCfg = lodcfg;
            newobj.PrefabName = objpbname;

            foreach (var meshrender in renderlist)
            {
                LODGenSubObj newsubobj = new LODGenSubObj();
                newsubobj.MeshName = LODGenUtil.GetPrefabName(meshrender.name);
                newsubobj.Render = meshrender;
                newsubobj.CalcMat(meshrender.sharedMaterials);
                newsubobj.CalcFace();
                newobj.AddSubObj(newsubobj);
            }

            return newobj;
        }

        private static List<LODGenObj> CollectObjList(List<KeyValuePair<GameObject, GameObject>> golist)
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

                if (newobj.SubObjList.Count > 0)
                {
                    newobjlist.Add(newobj);
                }
            }

            return newobjlist;
        }
    }
}
