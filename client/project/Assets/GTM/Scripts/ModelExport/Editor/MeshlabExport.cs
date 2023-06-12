using gtm.Scene.LODGen;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace gtm.Scene.ModelExport
{
    public class MeshlabExport
    {
        /// <summary>
        /// .
        /// </summary>
        const string MESHLAB_SUFFIX_WITHOUT_EXTENSION = "_Meshlab";

        /// <summary>
        /// MeshlabExport路径
        /// </summary>
        const string MESHLAB_EXE_PATH = "/../tools/meshlabexport/dist/main.exe";

        /// <summary>
        /// 分号分隔符
        /// </summary>
        const string SEMICOLON_SPLIT_SYMBOL = ";";

        /// <summary>
        /// 
        /// </summary>
        const string MESHLAB_SUFFIX = "_Meshlab.obj";

        /// <summary>
        /// .
        /// </summary>
        const string FBX_SUFFIX = "_Fbx.fbx";

        /// <summary>
        /// 
        /// </summary>
        const string PREFAB_EXTENSION = ".prefab";

        /// <summary>
        /// .
        /// </summary>
        /// <param name="savepath"></param>
        /// <param name="savefilename"></param>
        /// <param name="reducepercent"></param>
        /// <param name="appointmatpathlist"></param>
        /// <param name="extrasuffix"></param>
        /// <param name="reduceparam"></param>
        /// <returns></returns>
        public static string ExportLOD(string savepath, string savefilename, float reducepercent,
            List<string> appointmatpathlist, string extrasuffix, MeshlabReduceParam reduceparam)
        {
            ExportMesh(savepath, savefilename, reducepercent, extrasuffix, reduceparam);
            return ExportPrefab(savepath, savefilename, reducepercent, appointmatpathlist, extrasuffix);
        }

        /// <summary>
        /// .
        /// </summary>
        /// <param name="savepath"></param>
        /// <param name="savefilename"></param>
        /// <param name="reducepercent"></param>
        /// <param name="appointmatpathlist"></param>
        /// <param name="extrasuffix"></param>
        /// <returns></returns>
        static string ExportPrefab(string savepath, string savefilename, float reducepercent, List<string> appointmatpathlist, string extrasuffix)
        {
            var filename = savefilename + MESHLAB_SUFFIX_WITHOUT_EXTENSION;

            GameObject go = new GameObject(filename);
            GameObject meshgo = new GameObject("mesh");
            meshgo.transform.SetParent(go.transform);
            var meshfilter = meshgo.AddComponent<MeshFilter>();
            var meshrender = meshgo.AddComponent<MeshRenderer>();

            // 1. 加载模型
            var meshlabfilename = savepath + savefilename + extrasuffix + MESHLAB_SUFFIX;
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshlabfilename);
            meshfilter.mesh = mesh;

            // 2. 加载材质
            if (appointmatpathlist == null || appointmatpathlist.Count <= 0)
            {
                var filelist = LODGenUtil.FindDirFileList(savepath, ".mat");

                List<Material> matlist = new List<Material>();
                foreach (var name in filelist)
                {
                    if (name == null) continue;
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(savepath + "/" + name);
                    if (mat == null) continue;

                    matlist.Add(mat);
                }

                meshrender.materials = matlist.ToArray();
            }
            else
            {
                List<Material> matlist = new List<Material>();
                foreach (var matpath in appointmatpathlist)
                {
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(matpath);
                    matlist.Add(mat);
                }

                meshrender.materials = matlist.ToArray();
            }

            var filepath = savepath + savefilename + extrasuffix + MESHLAB_SUFFIX_WITHOUT_EXTENSION + PREFAB_EXTENSION;
            PrefabUtility.SaveAsPrefabAsset(go, filepath);

            AssetDatabase.Refresh();

            GameObject.DestroyImmediate(go);

            return filepath;
        }

        /// <summary>
        /// .
        /// </summary>
        /// <param name="savepath"></param>
        /// <param name="savefilename"></param>
        /// <param name="reducepercent"></param>
        /// <param name="extrasuffix"></param>
        /// <param name="reduceparam"></param>
        static void ExportMesh(string savepath, string savefilename, float reducepercent, string extrasuffix, MeshlabReduceParam reduceparam)
        {
            var curdir = System.Environment.CurrentDirectory;
            var exepath = curdir + MESHLAB_EXE_PATH;
            var meshpath = "\"" + curdir + "/" + savepath + "\"";
            var meshname = "\"" + savefilename + "\"";

            // 减面参数
            var strreduceparam = "";
            strreduceparam += (reduceparam.qualityThr + SEMICOLON_SPLIT_SYMBOL);
            strreduceparam += (Convert.ToInt32(reduceparam.preserveBoundary) + SEMICOLON_SPLIT_SYMBOL);
            strreduceparam += (reduceparam.boundaryWeight + SEMICOLON_SPLIT_SYMBOL);
            strreduceparam += (Convert.ToInt32(reduceparam.preserveNormal) + SEMICOLON_SPLIT_SYMBOL);
            strreduceparam += (Convert.ToInt32(reduceparam.preserveTopology) + SEMICOLON_SPLIT_SYMBOL);
            strreduceparam += (Convert.ToInt32(reduceparam.optimalplacement) + SEMICOLON_SPLIT_SYMBOL);
            strreduceparam += (Convert.ToInt32(reduceparam.planarQuadric) + SEMICOLON_SPLIT_SYMBOL);
            strreduceparam += (reduceparam.planarWeight + SEMICOLON_SPLIT_SYMBOL);
            strreduceparam += (Convert.ToInt32(reduceparam.qualityWeight) + SEMICOLON_SPLIT_SYMBOL);
            strreduceparam += Convert.ToInt32(reduceparam.autoClean);
            strreduceparam = "\"" + strreduceparam + "\"";

            // 减面
            var meshlabsuffixpath = extrasuffix + MESHLAB_SUFFIX;
            if (string.IsNullOrEmpty(extrasuffix) || extrasuffix == "")
            {
                meshlabsuffixpath = MESHLAB_SUFFIX;
            }
            var meshlabsuffix = "\"" + meshlabsuffixpath + "\"";

            using (Process myProcess = new Process())
            {
                myProcess.StartInfo.UseShellExecute = false;
                myProcess.StartInfo.FileName = exepath;
                myProcess.StartInfo.CreateNoWindow = true;

                string args = meshpath + " " + meshname + " " + reducepercent + " " +
                            FBX_SUFFIX + " " + meshlabsuffix + " " + strreduceparam;

                myProcess.StartInfo.Arguments = args;

                myProcess.Start();

                // 等待exe程序执行完成再执行下面的代码
                myProcess.WaitForExit();
            }

            AssetDatabase.Refresh();

            var meshrelativepath = savepath;
            var modelimportpath = meshrelativepath + savefilename + meshlabsuffixpath;
            AssetDatabase.ImportAsset(modelimportpath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
        }
    }
}
