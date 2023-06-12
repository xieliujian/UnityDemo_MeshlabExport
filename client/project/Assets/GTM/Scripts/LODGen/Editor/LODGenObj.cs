
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace gtm.Scene.LODGen
{
    public class LODGenObj
    {
        /// <summary>
        /// 
        /// </summary>
        public GameObject obj;

        /// <summary>
        /// 
        /// </summary>
        public GameObject srcObj;

        /// <summary>
        /// 
        /// </summary>
        public string prefabName;

        /// <summary>
        /// 
        /// </summary>
        public List<LODGenSubObj> subObjList = new List<LODGenSubObj>();

        /// <summary>
        /// 新生成的prefab路径
        /// </summary>
        public string newPrefabPath;

        /// <summary>
        /// LOD的prefab列表
        /// </summary>
        public Dictionary<int, List<string>> lODPrefabDict = new Dictionary<int, List<string>>();

        /// <summary>
        /// LOD的配置
        /// </summary>
        public LODGenConfig lodCfg;

        /// <summary>
        /// .
        /// </summary>
        /// <param name="subobj"></param>
        public void AddSubObj(LODGenSubObj subobj)
        {
            subObjList.Add(subobj);
        }

        /// <summary>
        /// .
        /// </summary>
        /// <param name="id"></param>
        /// <param name="prefabpath"></param>
        public void AddLODPrefabDict(int id, string prefabpath)
        {
            if (!lODPrefabDict.ContainsKey(id))
            {
                lODPrefabDict.Add(id, new List<string>());
            }

            var pathlist = lODPrefabDict[id];
            if (pathlist != null)
            {
                pathlist.Add(prefabpath);
            }
        }
    }
}






