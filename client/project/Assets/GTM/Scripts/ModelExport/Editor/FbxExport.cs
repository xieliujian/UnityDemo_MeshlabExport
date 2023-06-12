using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Formats.Fbx.Exporter;
using gtm.Scene.LODGen;
using Unity.Collections;

namespace gtm.Scene.ModelExport
{
    public class FbxExport
    {
        const string FBX_SUFFIX = "_Fbx.fbx";

        const string FBX_SUFFIX_2 = ".fbx";

        const string PREFAB_EXTENSION = ".prefab";

        /// <summary>
        /// .
        /// </summary>
        /// <param name="savepath"></param>
        /// <param name="savefilename"></param>
        /// <param name="extrasuffix"></param>
        /// <returns></returns>
        public static string ExportLOD(string savepath, string savefilename, string extrasuffix)
        {
            return ExportLOD(savepath, savefilename, HLODMaterialType.Empty, false, false, extrasuffix);
        }

        /// <summary>
        /// .
        /// </summary>
        /// <param name="savepath"></param>
        /// <param name="savefilename"></param>
        /// <param name="mattype"></param>
        /// <param name="isusemattypename"></param>
        /// <param name="usefbxsuffix"></param>
        /// <param name="extrasuffix"></param>
        /// <returns></returns>
        public static string ExportLOD(string savepath, string savefilename, HLODMaterialType mattype, bool isusemattypename, bool usefbxsuffix, string extrasuffix)
        {
            var filename = LODGenUtil.CombineFileName(PREFAB_EXTENSION, savepath, savefilename, mattype, isusemattypename);

            GameObject obj = AssetDatabase.LoadAssetAtPath(filename, typeof(GameObject)) as GameObject;
            if (obj == null)
                return "";

            GameObject objgo = ScaleGameObject(obj, 0.01f);
            if (objgo == null)
                return "";

            if (!string.IsNullOrEmpty(extrasuffix) && extrasuffix != "")
            {
                savefilename += extrasuffix;
            }

            var newfilename = savepath + savefilename + FBX_SUFFIX;
            if (!usefbxsuffix)
            {
                newfilename = savepath + savefilename + FBX_SUFFIX_2;
            }

            Object[] objarray = { objgo };

            ExportModelSettingsSerialize exportSettings = new ExportModelSettingsSerialize();
            exportSettings.SetExportFormat(ExportSettings.ExportFormat.Binary);
            exportSettings.SetModelAnimIncludeOption(ExportSettings.Include.Model);
            exportSettings.SetLODExportType(ExportSettings.LODExportType.Highest);
            exportSettings.SetObjectPosition(ExportSettings.ObjectPosition.LocalCentered);
            exportSettings.SetAnimatedSkinnedMesh(false);
            exportSettings.SetUseMayaCompatibleNames(true);
            exportSettings.SetExportUnredererd(true);
            exportSettings.SetPreserveImportSettings(false);

            // 不适用厘米单位，使用米单位
            if (ModelExporter.ExportObjects(newfilename, objarray, exportSettings, null, false) != null)
            {
                // refresh the asset database so that the file appears in the asset folder view.
                AssetDatabase.Refresh();
            }

            GameObject.DestroyImmediate(objgo);

            return newfilename;
        }

        /// <summary>
        /// .
        /// </summary>
        /// <param name="srcobj"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        static GameObject ScaleGameObject(GameObject srcobj, float scale)
        {
            GameObject objgo = GameObject.Instantiate(srcobj) as GameObject;
            if (objgo == null)
                return null;

            MeshFilter[] meshfilterarray = objgo.GetComponentsInChildren<MeshFilter>();
            if (meshfilterarray == null)
                return null;

            foreach (var meshfilter in meshfilterarray)
            {
                if (meshfilter == null)
                    continue;

                var mesh = meshfilter.sharedMesh;
                if (mesh == null)
                    continue;

                var workmesh = mesh.ToWorkingMesh(Allocator.Persistent);
                if (workmesh == null)
                    continue;

                Mesh newmesh = workmesh.ToMesh();

                // 缩放顶点
                int vertexnum = newmesh.vertexCount;
                List<Vector3> newvertlist = new List<Vector3>(vertexnum);

                for (int i = 0; i < vertexnum; i++)
                {
                    var vertex = newmesh.vertices[i];

                    vertex *= scale;
                    newvertlist.Add(vertex);
                }

                newmesh.SetVertices(newvertlist);

                meshfilter.mesh = newmesh;
                workmesh.Dispose();
            }

            return objgo;
        }
    }
}

