import sys

import pymeshlab

# pymeshlab是在python3.9下面下载，如果出现项目无法运行的情况，请用python3.9

if __name__ == '__main__':
    # Test
    # meshpath = "D:/douluodalu/art/resource/scene/Assets/scene/10000/res/build/Model/"
    # meshnameprefix = "NDXY_zhuzi_002_1"
    # reducepercent = 0.3
    # fbxsuffix = "_Fbx.fbx"
    # meshlabsuffix = "_Meshlab.obj"
    # reduceparamlist = "0.3;0;1;0;0;1;0;0.001;0;1"

    meshpath = str(sys.argv[1])
    meshnameprefix = str(sys.argv[2])
    reducepercent = float(sys.argv[3])
    fbxsuffix = str(sys.argv[4])
    meshlabsuffix = str(sys.argv[5])
    reduceparamlist = str(sys.argv[6])

    loadmeshname = meshnameprefix + fbxsuffix
    savemeshname = meshnameprefix + meshlabsuffix

    print(meshpath)
    print(meshnameprefix)
    print(reducepercent)

    # 减面参数解析
    splitarray = reduceparamlist.split(";")
    qualitythr = float(splitarray[0])
    preserveboundary = bool(int(splitarray[1]))
    boundaryweight = float(splitarray[2])
    preservenormal = bool(int(splitarray[3]))
    preservetopology = bool(int(splitarray[4]))
    optimalplacement = bool(int(splitarray[5]))
    planarquadric = bool(int(splitarray[6]))
    planarweight = float(splitarray[7])
    qualityweight = bool(int(splitarray[8]))
    autoclean = bool(int(splitarray[9]))

    print("MeshlabReduceParam qualitythr : " + str(qualitythr))
    print("MeshlabReduceParam preserveboundary : " + str(preserveboundary))
    print("MeshlabReduceParam boundaryweight : " + str(boundaryweight))
    print("MeshlabReduceParam preservenormal : " + str(preservenormal))
    print("MeshlabReduceParam preservetopology : " + str(preservetopology))
    print("MeshlabReduceParam optimalplacement : " + str(optimalplacement))
    print("MeshlabReduceParam planarquadric : " + str(planarquadric))
    print("MeshlabReduceParam planarweight : " + str(planarweight))
    print("MeshlabReduceParam qualityweight : " + str(qualityweight))
    print("MeshlabReduceParam autoclean : " + str(autoclean))

    # github项目地址 https://github.com/cnr-isti-vclab/PyMeshLab
    # 文档地址 https://pymeshlab.readthedocs.io/en/latest/tutorials/apply_filter_parameters.html

    ms = pymeshlab.MeshSet()

    ms.load_new_mesh(file_name=meshpath + loadmeshname)

    ms.set_current_mesh(0)

    ms.simplification_quadric_edge_collapse_decimation(targetperc=reducepercent,
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

    ms.save_current_mesh(meshpath + savemeshname)
