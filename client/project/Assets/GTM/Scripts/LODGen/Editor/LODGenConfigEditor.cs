using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace gtm.Scene.LODGen
{
    [CustomEditor(typeof(LODGenConfig))]
    public class LODGenConfigEditor : Editor
    {
        bool[] _folds = new bool[16];

        /// <summary>
        /// 基本参数
        /// </summary>
        SerializedProperty m_MinReduceFaceNumProp;

        SerializedProperty m_UnReduceMeshListProp;

        SerializedProperty m_LevelListProp;     

        GUIContent m_MinReduceFaceNumDesc = new GUIContent("最小减面数", "模型大于这个面数才会减面, 小于这个面数就使用原始的模型");

        GUIContent m_UnReduceMeshListDesc = new GUIContent("不减面模型", "在列表里面的模型不会减面, LOD都用原始模型");

        GUIContent m_LevelListDesc = new GUIContent("LOD等级", "LOD等级列表");

        // 减面参数
        SerializedProperty m_QualitythrProp;
        SerializedProperty m_PreserveBoundaryProp;
        SerializedProperty m_BoundaryWeightProp;
        SerializedProperty m_PreserveNormalProp;
        SerializedProperty m_PreserveTopologyProp;
        SerializedProperty m_OptimalplacementProp;
        SerializedProperty m_PlanarquadricProp;
        SerializedProperty m_PlanarWeightProp;
        SerializedProperty m_QualityWeightProp;
        SerializedProperty m_AutoCleanProp;

        GUIContent m_QualitythrDesc = new GUIContent("质量阈值",
            "Quality threshold: Quality threshold for penalizing bad shaped faces." +
            "The value is in the range[0..1]" +
            "0 accept any kind of face(no penalties)," +
            "0.5 penalize faces with quality < 0.5, proportionally to their shape"
            );

        GUIContent m_PreserveBoundaryDesc = new GUIContent("保留网格边界",
            "Preserve Boundary of the mesh: The simplification process tries to do not affect mesh boundaries during simplification"
            );

        GUIContent m_BoundaryWeightDesc = new GUIContent("边界保留权重",
            "Boundary Preserving Weight: The importance of the boundary during simplification. " +
            "Default (1.0) means that the boundary has the same importance of the rest. " +
            "Values greater than 1.0 raise boundary importance and has the effect of removing less vertices on the border. " +
            "Admitted range of values (0,+inf)."
        );

        GUIContent m_PreserveNormalDesc = new GUIContent("保留法线",
            "Preserve Normal: Try to avoid face flipping effects and try to preserve the original orientation of the surface"
        );

        GUIContent m_PreserveTopologyDesc = new GUIContent("保留拓扑",
            "Preserve Topology: Avoid all the collapses that should cause a topology change in the mesh (like closing holes, squeezing handles, etc). " +
            "If checked the genus of the mesh should stay unchanged."
        );

        GUIContent m_OptimalplacementDesc = new GUIContent("简化顶点的最佳位置",
            "Optimal position of simplified vertices: Each collapsed vertex is placed in the position minimizing the quadric error." +
            "It can fail(creating bad spikes) in case of very flat areas." +
            "If disabled edges are collapsed onto one of the two original vertices and the final mesh is composed by a subset of the original vertices."
        );

        GUIContent m_PlanarquadricDesc = new GUIContent("平面简化",
            "Planar Simplification: Add additional simplification constraints that improves the quality of the simplification of the planar portion of the mesh, " +
            "as a side effect, more triangles will be preserved in flat areas (allowing better shaped triangles)."
        );

        GUIContent m_PlanarWeightDesc = new GUIContent("平面简化 权重",
            "Planar Simp. Weight: How much we should try to preserve the triangles in the planar regions. " +
            "If you lower this value planar areas will be simplified more."
        );

        GUIContent m_QualityWeightDesc = new GUIContent("加权简化",
            "Weighted Simplification: Use the Per-Vertex quality as a weighting factor for the simplification. " +
            "The weight is used as a error amplification value, " +
            "so a vertex with a high quality value will not be simplified and a portion of the mesh with low quality values will be aggressively simplified."
        );

        GUIContent m_AutoCleanDesc = new GUIContent("简化后清理",
            "Post-simplification cleaning: After the simplification an additional set of steps is performed to clean the mesh " +
            "(unreferenced vertices, bad faces, etc)"
        );

        void OnEnable()
        {
            m_MinReduceFaceNumProp = serializedObject.FindProperty("minReduceFaceNum");
            m_UnReduceMeshListProp = serializedObject.FindProperty("unReduceMeshList");
            m_LevelListProp = serializedObject.FindProperty("levelList");

            m_QualitythrProp = serializedObject.FindProperty("meshlabReduceParam.qualityThr");
            m_PreserveBoundaryProp = serializedObject.FindProperty("meshlabReduceParam.preserveBoundary");
            m_BoundaryWeightProp = serializedObject.FindProperty("meshlabReduceParam.boundaryWeight");
            m_PreserveNormalProp = serializedObject.FindProperty("meshlabReduceParam.preserveNormal");
            m_PreserveTopologyProp = serializedObject.FindProperty("meshlabReduceParam.preserveTopology");
            m_OptimalplacementProp = serializedObject.FindProperty("meshlabReduceParam.optimalplacement");
            m_PlanarquadricProp = serializedObject.FindProperty("meshlabReduceParam.planarQuadric");
            m_PlanarWeightProp = serializedObject.FindProperty("meshlabReduceParam.planarWeight");
            m_QualityWeightProp = serializedObject.FindProperty("meshlabReduceParam.qualityWeight");
            m_AutoCleanProp = serializedObject.FindProperty("meshlabReduceParam.autoClean");
        }

        public override void OnInspectorGUI()
        {
            LODGenConfig lodcfg = target as LODGenConfig;
            if (lodcfg == null)
                return;

            // 最小的减面数
            EditorGUILayout.PropertyField(m_MinReduceFaceNumProp, m_MinReduceFaceNumDesc);

            // 不减面模型列表
            EditorGUILayout.PropertyField(m_UnReduceMeshListProp, m_UnReduceMeshListDesc);

            // LOD等级
            var levellist = lodcfg.levelList;
            if (levellist.Count <= 0)
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

            EditorGUILayout.PropertyField(m_LevelListProp, m_LevelListDesc);

            // 减面参数设置
            if (BeginCenteredGroup("Meshlab减面参数设置", ref _folds[0]))
            {
                EditorGUILayout.PropertyField(m_QualitythrProp, m_QualitythrDesc);
                Toggle(m_PreserveBoundaryDesc, m_PreserveBoundaryProp, 2);
                EditorGUILayout.PropertyField(m_BoundaryWeightProp, m_BoundaryWeightDesc);
                Toggle(m_PreserveNormalDesc, m_PreserveNormalProp, 2);
                Toggle(m_PreserveTopologyDesc, m_PreserveTopologyProp, 2);
                Toggle(m_OptimalplacementDesc, m_OptimalplacementProp, 2);
                Toggle(m_PlanarquadricDesc, m_PlanarquadricProp, 2);
                EditorGUILayout.PropertyField(m_PlanarWeightProp, m_PlanarWeightDesc);
                Toggle(m_QualityWeightDesc, m_QualityWeightProp, 2);
                Toggle(m_AutoCleanDesc, m_AutoCleanProp, 2);
            }

            EndCenteredGroup();

            if (GUILayout.Button("生成并替换LOD"))
            {
                LODGenCreate.GenerateObj(lodcfg);
            }

            // Apply changes to the serializedProperty - always do this at the end of OnInspectorGUI.
            if (serializedObject != null)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        bool BeginCenteredGroup(string label, ref bool groupFoldState)
        {
            if (GUILayout.Button(label, EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).button))
            {
                groupFoldState = !groupFoldState;
            }

            GUILayout.BeginHorizontal(); GUILayout.Space(12);
            GUILayout.BeginVertical();
            return groupFoldState;
        }

        void EndCenteredGroup()
        {
            GUILayout.EndVertical();
            GUILayout.Space(12);
            GUILayout.EndHorizontal();
        }

        static void Toggle(GUIContent label, SerializedProperty prop, int toggleType = 0)
        {
            GUILayout.BeginHorizontal();

            var inValue = prop;

            switch (toggleType)
            {

                case 0:
                    EditorGUILayout.PropertyField(inValue, label);
                    break;

                case 1:
                    if (inValue.hasMultipleDifferentValues)
                    {
                        var result = EditorGUILayout.Popup(label, -1, new string[] { "Enabled", "Disabled" });

                        if (result > -1)
                        {
                            inValue.boolValue = result == 0;
                        }
                    }
                    else
                    {
                        inValue.boolValue = EditorGUILayout.Popup(label, inValue.boolValue ? 0 : 1, new string[] { "Enabled", "Disabled" }) == 0;
                    }
                    break;

                case 2:
                    if (inValue.hasMultipleDifferentValues)
                    {
                        var result = EditorGUILayout.Popup(label, -1, new string[] { "True", "False" });

                        if (result > -1)
                        {
                            inValue.boolValue = result == 0;
                        }
                    }
                    else
                    {
                        inValue.boolValue = EditorGUILayout.Popup(label, inValue.boolValue ? 0 : 1, new string[] { "True", "False" }) == 0;
                    }

                    break;
            }

            GUILayout.EndHorizontal();
        }
    }
}

