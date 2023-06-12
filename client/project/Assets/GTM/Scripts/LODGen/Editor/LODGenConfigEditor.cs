using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace gtm.Scene.LODGen
{
    [CustomEditor(typeof(LODGenConfig))]
    public class LODGenConfigEditor : Editor
    {
        private bool[] _folds = new bool[16];

        // 基本参数
        private SerializedProperty m_minReduceFaceNumProp;

        private SerializedProperty m_unreduceMeshListProp;

        private SerializedProperty m_levelListProp;     

        private GUIContent m_isBuildingDesc = new GUIContent("是否建筑物", "建筑物最小层级会合并成整模处理");

        private GUIContent m_minReduceFaceNumDesc = new GUIContent("最小减面数", "模型大于这个面数才会减面, 小于这个面数就使用原始的模型");

        private GUIContent m_unreduceMeshListDesc = new GUIContent("不减面模型", "在列表里面的模型不会减面, LOD都用原始模型");

        private GUIContent m_levelListDesc = new GUIContent("LOD等级", "LOD等级列表");

        // 减面参数
        private SerializedProperty m_qualitythrProp;
        private SerializedProperty m_preserveboundaryProp;
        private SerializedProperty m_boundaryweightProp;
        private SerializedProperty m_preservenormalProp;
        private SerializedProperty m_preservetopologyProp;
        private SerializedProperty m_optimalplacementProp;
        private SerializedProperty m_planarquadricProp;
        private SerializedProperty m_planarweightProp;
        private SerializedProperty m_qualityweightProp;
        private SerializedProperty m_autocleanProp;

        private GUIContent m_qualitythrDesc = new GUIContent("质量阈值",
            "Quality threshold: Quality threshold for penalizing bad shaped faces." +
            "The value is in the range[0..1]" +
            "0 accept any kind of face(no penalties)," +
            "0.5 penalize faces with quality < 0.5, proportionally to their shape"
            );

        private GUIContent m_preserveboundaryDesc = new GUIContent("保留网格边界",
            "Preserve Boundary of the mesh: The simplification process tries to do not affect mesh boundaries during simplification"
            );

        private GUIContent m_boundaryweightDesc = new GUIContent("边界保留权重",
            "Boundary Preserving Weight: The importance of the boundary during simplification. " +
            "Default (1.0) means that the boundary has the same importance of the rest. " +
            "Values greater than 1.0 raise boundary importance and has the effect of removing less vertices on the border. " +
            "Admitted range of values (0,+inf)."
        );

        private GUIContent m_preservenormalDesc = new GUIContent("保留法线",
            "Preserve Normal: Try to avoid face flipping effects and try to preserve the original orientation of the surface"
        );

        private GUIContent m_preservetopologyDesc = new GUIContent("保留拓扑",
            "Preserve Topology: Avoid all the collapses that should cause a topology change in the mesh (like closing holes, squeezing handles, etc). " +
            "If checked the genus of the mesh should stay unchanged."
        );

        private GUIContent m_optimalplacementDesc = new GUIContent("简化顶点的最佳位置",
            "Optimal position of simplified vertices: Each collapsed vertex is placed in the position minimizing the quadric error." +
            "It can fail(creating bad spikes) in case of very flat areas." +
            "If disabled edges are collapsed onto one of the two original vertices and the final mesh is composed by a subset of the original vertices."
        );

        private GUIContent m_planarquadricDesc = new GUIContent("平面简化",
            "Planar Simplification: Add additional simplification constraints that improves the quality of the simplification of the planar portion of the mesh, " +
            "as a side effect, more triangles will be preserved in flat areas (allowing better shaped triangles)."
        );

        private GUIContent m_planarweightDesc = new GUIContent("平面简化 权重",
            "Planar Simp. Weight: How much we should try to preserve the triangles in the planar regions. " +
            "If you lower this value planar areas will be simplified more."
        );

        private GUIContent m_qualityweightDesc = new GUIContent("加权简化",
            "Weighted Simplification: Use the Per-Vertex quality as a weighting factor for the simplification. " +
            "The weight is used as a error amplification value, " +
            "so a vertex with a high quality value will not be simplified and a portion of the mesh with low quality values will be aggressively simplified."
        );

        private GUIContent m_autocleanDesc = new GUIContent("简化后清理",
            "Post-simplification cleaning: After the simplification an additional set of steps is performed to clean the mesh " +
            "(unreferenced vertices, bad faces, etc)"
        );

        private void OnEnable()
        {
            m_minReduceFaceNumProp = serializedObject.FindProperty("m_minReduceFaceNum");
            m_unreduceMeshListProp = serializedObject.FindProperty("m_unreduceMeshList");
            m_levelListProp = serializedObject.FindProperty("m_levelList");

            m_qualitythrProp = serializedObject.FindProperty("m_meshlabReduceParam.qualitythr");
            m_preserveboundaryProp = serializedObject.FindProperty("m_meshlabReduceParam.preserveboundary");
            m_boundaryweightProp = serializedObject.FindProperty("m_meshlabReduceParam.boundaryweight");
            m_preservenormalProp = serializedObject.FindProperty("m_meshlabReduceParam.preservenormal");
            m_preservetopologyProp = serializedObject.FindProperty("m_meshlabReduceParam.preservetopology");
            m_optimalplacementProp = serializedObject.FindProperty("m_meshlabReduceParam.optimalplacement");
            m_planarquadricProp = serializedObject.FindProperty("m_meshlabReduceParam.planarquadric");
            m_planarweightProp = serializedObject.FindProperty("m_meshlabReduceParam.planarweight");
            m_qualityweightProp = serializedObject.FindProperty("m_meshlabReduceParam.qualityweight");
            m_autocleanProp = serializedObject.FindProperty("m_meshlabReduceParam.autoclean");
        }

        public override void OnInspectorGUI()
        {
            LODGenConfig lodcfg = target as LODGenConfig;
            if (lodcfg == null)
                return;

            // 最小的减面数
            EditorGUILayout.PropertyField(m_minReduceFaceNumProp, m_minReduceFaceNumDesc);

            // 不减面模型列表
            EditorGUILayout.PropertyField(m_unreduceMeshListProp, m_unreduceMeshListDesc);

            // LOD等级
            var levellist = lodcfg.m_levelList;
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

            EditorGUILayout.PropertyField(m_levelListProp, m_levelListDesc);

            // 减面参数设置
            if (BeginCenteredGroup("Meshlab减面参数设置", ref _folds[0]))
            {
                EditorGUILayout.PropertyField(m_qualitythrProp, m_qualitythrDesc);
                Toggle(m_preserveboundaryDesc, m_preserveboundaryProp, 2);
                EditorGUILayout.PropertyField(m_boundaryweightProp, m_boundaryweightDesc);
                Toggle(m_preservenormalDesc, m_preservenormalProp, 2);
                Toggle(m_preservetopologyDesc, m_preservetopologyProp, 2);
                Toggle(m_optimalplacementDesc, m_optimalplacementProp, 2);
                Toggle(m_planarquadricDesc, m_planarquadricProp, 2);
                EditorGUILayout.PropertyField(m_planarweightProp, m_planarweightDesc);
                Toggle(m_qualityweightDesc, m_qualityweightProp, 2);
                Toggle(m_autocleanDesc, m_autocleanProp, 2);
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

        private bool BeginCenteredGroup(string label, ref bool groupFoldState)
        {
            if (GUILayout.Button(label, EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).button))
            {
                groupFoldState = !groupFoldState;
            }

            GUILayout.BeginHorizontal(); GUILayout.Space(12);
            GUILayout.BeginVertical();
            return groupFoldState;
        }

        private void EndCenteredGroup()
        {
            GUILayout.EndVertical();
            GUILayout.Space(12);
            GUILayout.EndHorizontal();
        }

        private static void Toggle(GUIContent label, SerializedProperty prop, int toggleType = 0)
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

