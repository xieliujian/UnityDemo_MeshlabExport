using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace gtm.Scene.LODGen
{
    public enum LODGenLevel
    {
        LOD1,
        LOD2,
        LOD3,
        LOD4,
        LOD5,
        LOD6,
        LOD7,
        LOD8,
        LOD9,
        LODNumMax,
        LODCombine,     // 房子的组合模型
    }

    /// <summary>
    /// Meshlab的减面参数，使用的是 simplification_quadric_edge_collapse_decimation 命令
    /// </summary>
    [System.Serializable]
    public class MeshlabReduceParam
    {
        /// <summary>
        /// Quality threshold: Quality threshold for penalizing bad shaped faces.
        /// The value is in the range[0..1]
        /// 0 accept any kind of face(no penalties),
        // 0.5 penalize faces with quality < 0.5, proportionally to their shape
        /// </summary>
        public float qualitythr = 0.3f;

        /// <summary>
        /// Preserve Boundary of the mesh: The simplification process tries to do not affect mesh boundaries during simplification
        /// </summary>
        public bool preserveboundary = false;

        /// <summary>
        /// Boundary Preserving Weight: The importance of the boundary during simplification. 
        /// Default (1.0) means that the boundary has the same importance of the rest. 
        /// Values greater than 1.0 raise boundary importance and has the effect of removing less vertices on the border. 
        /// Admitted range of values (0,+inf).
        /// </summary>
        public float boundaryweight = 1.0f;

        /// <summary>
        /// Preserve Normal: Try to avoid face flipping effects and try to preserve the original orientation of the surface
        /// </summary>
        public bool preservenormal = false;

        /// <summary>
        /// Preserve Topology: Avoid all the collapses that should cause a topology change in the mesh (like closing holes, squeezing handles, etc). 
        /// If checked the genus of the mesh should stay unchanged.
        /// </summary>
        public bool preservetopology = false;

        /// <summary>
        /// Optimal position of simplified vertices: Each collapsed vertex is placed in the position minimizing the quadric error.
        // It can fail(creating bad spikes) in case of very flat areas.
        // If disabled edges are collapsed onto one of the two original vertices and the final mesh is composed by a subset of the original vertices.
        /// </summary>
        public bool optimalplacement = true;

        /// <summary>
        /// Planar Simplification: Add additional simplification constraints that improves the quality of the simplification of the planar portion of the mesh, 
        /// as a side effect, more triangles will be preserved in flat areas (allowing better shaped triangles).
        /// </summary>
        public bool planarquadric = false;

        /// <summary>
        /// Planar Simp. Weight: How much we should try to preserve the triangles in the planar regions. 
        /// If you lower this value planar areas will be simplified more.
        /// </summary>
        public float planarweight = 0.001f;

        /// <summary>
        /// Weighted Simplification: Use the Per-Vertex quality as a weighting factor for the simplification. 
        /// The weight is used as a error amplification value, 
        /// so a vertex with a high quality value will not be simplified and a portion of the mesh with low quality values will be aggressively simplified.
        /// </summary>
        public bool qualityweight = false;

        /// <summary>
        /// Post-simplification cleaning: After the simplification an additional set of steps is performed to clean the mesh 
        /// (unreferenced vertices, bad faces, etc)
        /// </summary>
        public bool autoclean = true;
    }

    [ExecuteAlways]
    public class LODGenConfig : MonoBehaviour
    {
        [System.Serializable]
        public class LODGenLevel
        {
            /// <summary>
            /// 减面百分比
            /// </summary>
            public float reducePercent;

            /// <summary>
            /// LOD的百分比
            /// </summary>
            public float lodpercent;
        }

        /// <summary>
        /// 最小可以减少的面数，大于这个面数才会走减面流程
        /// </summary>
        public int m_minReduceFaceNum = 200;

        /// <summary>
        /// 不减面的模型列表
        /// </summary>
        public List<MeshRenderer> m_unreduceMeshList = new List<MeshRenderer>();

        /// <summary>
        /// LOD等级列表
        /// </summary>
        public List<LODGenLevel> m_levelList = new List<LODGenLevel>();

        /// <summary>
        /// Meshlab的减面参数
        /// </summary>
        public MeshlabReduceParam m_meshlabReduceParam = new MeshlabReduceParam();

        /// <summary>
        /// 初始化
        /// </summary>
        private void Awake()
        {

        }

        public bool CanReduceFace(int meshface, MeshRenderer srcrender)
        {
            // 是否面数满足
            var isfaceenough = meshface >= m_minReduceFaceNum;

            // 不是不减面mesh
            bool isreducemodel = true;
            if (srcrender != null)
            {
                foreach(var render in m_unreduceMeshList)
                {
                    if (render == null)
                        continue;

                    if (render == srcrender)
                    {
                        isreducemodel = false;
                        break;
                    }
                }
            }

            return isfaceenough && isreducemodel;
        }

        public bool IsLevelEmpty()
        {
            return m_levelList.Count <= 0;
        }
    }
}
