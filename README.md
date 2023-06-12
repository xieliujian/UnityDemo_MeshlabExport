# Meshlab减面

## 使用界面

`最小减面数` 小于这个面数使用原始模型，大于这个模型使用meshlab减面

`不减面模型` 不减面的MeshRenderer

`LOD等级` 如例所示，第一级30% LODPercent 0.4 第二级15% LODPercent 0.1

`Meshlab减面参数` [pyMeshlab文档](https://pymeshlab.readthedocs.io/en/latest/filter_list.html?highlight=preserveBoundary#meshing_decimation_quadric_edge_collapse_with_texture)

![github](https://github.com/xieliujian/UnityDemo_MeshlabExport/blob/main/video/1.png?raw=true)

## 工具代码

导出Fbx

```cs

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

```

meshlab减面，源文件fbx

```cs

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

```

## pyMeshlab导出的python工具

python导出，使用函数`simplification_quadric_edge_collapse_decimation`

```python

ms.simplification_quadric_edge_collapse_decimation(     
    targetperc=reducepercent,
    qualitythr=qualitythr,
    preserveboundary=preserveboundary,
    boundaryweight=boundaryweight,
    preservenormal=preservenormal,
    preservetopology=preservetopology,
    optimalplacement=optimalplacement,
    planarquadric=planarquadric,
    planarweight=planarweight,
    qualityweight=qualityweight,
    autoclean=autoclean
    )

```

通过`pyinstaller`打包成`exe`
