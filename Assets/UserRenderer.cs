using Draco;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class UserRenderer : MonoBehaviour
{
    public GameObject Q15;
    public GameObject Q25;
    public GameObject Q40;
    public GameObject Q60;
    public GameObject Q75;

    private MeshFilter f15;
    private MeshFilter f25;
    private MeshFilter f40;
    private MeshFilter f60;
    private MeshFilter f75;

    private Mesh.MeshDataArray meshDataArray;
    private List<Task<DracoMeshLoader.DecodeResult>> decodeTasks;

    private int nLayers;
    private Mesh mesh;

    private int quality = 0;
    private bool isDecoding = false;
    // Start is called before the first frame update
    void Start()
    {
        f15 = Q15.GetComponent<MeshFilter>();
        f25 = Q25.GetComponent<MeshFilter>();
        f40 = Q40.GetComponent<MeshFilter>();
        f60 = Q60.GetComponent<MeshFilter>();
        f75 = Q75.GetComponent<MeshFilter>();
        decodeTasks = new();
    }

    // Update is called once per frame
    public bool CheckIfReady()
    {
        if (decodeTasks.Count() > 0 && decodeTasks.All(t => t.IsCompleted))
        {
          
            if (decodeTasks.All(t => t.Result.success))
            {
                int totalSize = 0;
                for (int i = 0; i < nLayers; i++)
                {
                    if (i == 1 || i == 0)
                        totalSize += meshDataArray[i].vertexCount;
                }
                Destroy(mesh);
                mesh = new Mesh();
                var col = new NativeArray<Color32>(totalSize, Allocator.TempJob);
                var pos = new NativeArray<Vector3>(totalSize, Allocator.TempJob);
                int offset = 0;
                for (int i = 0; i < nLayers; i++)
                {
                    if (i == 1 || i == 0)
                    {
                        meshDataArray[i].GetColors(col.GetSubArray(offset, meshDataArray[i].vertexCount));
                        meshDataArray[i].GetVertices(pos.GetSubArray(offset, meshDataArray[i].vertexCount));
                        offset += meshDataArray[i].vertexCount;
                    }


                }
                mesh.indexFormat = totalSize > 65535 ?
                        IndexFormat.UInt32 : IndexFormat.UInt16;

                mesh.SetVertices(pos);
                mesh.SetColors(col);

                mesh.SetIndices(
                    Enumerable.Range(0, mesh.vertexCount).ToArray(),
                    MeshTopology.Points, 0
                );

                // Use the resulting mesh
                mesh.UploadMeshData(true);

                Q15.SetActive(false);
                Q25.SetActive(false);
                Q40.SetActive(false);
                Q60.SetActive(false);
                Q75.SetActive(false);
                if (quality >= 75)
                {
                    Q75.SetActive(true);
                    f75.mesh = mesh;

                }
                else if (quality >= 60)
                {
                    Q60.SetActive(true);
                    f60.mesh = mesh;

                }
                else if (quality >= 40)
                {
                    Q40.SetActive(true);
                    f40.mesh = mesh;

                }
                else if (quality >= 25)
                {
                    Q25.SetActive(true);
                    f25.mesh = mesh;
                }
                else
                {
                    Q15.SetActive(true);
                    f15.mesh = mesh;
                }
                col.Dispose();
                pos.Dispose();
            }
            meshDataArray.Dispose();
            decodeTasks.Clear();
            isDecoding = false;
            return true;
        } else
        {
            return false || !isDecoding;
        }

    }
    public void CreateDataArray(int _nLayers)
    {
        isDecoding = true;
        nLayers = _nLayers;
        meshDataArray = Mesh.AllocateWritableMeshData(_nLayers);
    }
    public void SetQuality(int _quality)
    {
        quality = _quality;
    }
    public void AddDecodeTask(byte[] data, int layerId)
    {
        var draco = new DracoMeshLoader();
        decodeTasks.Add(draco.ConvertDracoMeshToUnity(
            meshDataArray[layerId],
            data,
            false, // Set to true if you require normals. If Draco data does not contain them, they are allocated and we have to calculate them below
            false// Retrieve tangents is not supported, but this will ensure they are allocated and can be calculated later (see below)
        ));
    }
}
