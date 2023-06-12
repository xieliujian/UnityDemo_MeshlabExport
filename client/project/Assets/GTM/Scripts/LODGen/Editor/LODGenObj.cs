
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace gtm.Scene.LODGen
{
    public class LODGenObj
    {
        public GameObject Obj;

        public GameObject SrcObj;

        public string PrefabName;

        public List<LODGenSubObj> SubObjList = new List<LODGenSubObj>();

        /// <summary>
        /// �����ɵ�prefab·��
        /// </summary>
        public string NewPrefabPath;

        /// <summary>
        /// LOD��prefab�б�
        /// </summary>
        public Dictionary<int, List<string>> LODPrefabDict = new Dictionary<int, List<string>>();

        /// <summary>
        /// LOD������
        /// </summary>
        public LODGenConfig LODCfg;


        public void AddSubObj(LODGenSubObj subobj)
        {
            SubObjList.Add(subobj);
        }

        public void AddLODPrefabDict(int id, string prefabpath)
        {
            if (!LODPrefabDict.ContainsKey(id))
            {
                LODPrefabDict.Add(id, new List<string>());
            }

            var pathlist = LODPrefabDict[id];
            if (pathlist != null)
            {
                pathlist.Add(prefabpath);
            }
        }
    }
}






