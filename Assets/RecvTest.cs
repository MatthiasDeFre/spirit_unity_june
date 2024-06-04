using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

using Draco;
using System.Threading.Tasks;
using Unity.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.IO;
using System.Linq;
using UnityEngine.Rendering;

public class RecvTest : MonoBehaviour
{
    [DllImport("DracoDLLTest")]
    static extern int init(string addr, UInt32 port, bool input, bool output);
    [DllImport("DracoDLLTest")]
    static extern int next_frame();
    [DllImport("DracoDLLTest")]
    static extern int set_frame_data(byte[] b);

    private MeshFilter meshFilter;
    private int num;
    private int numTemp;
    private int currentFrame = 0;
    private bool isDecoding = false;
    private bool frameReady = false;
    private byte[] data;
    private Mesh.MeshDataArray meshDataArray;
    private List<Task<DracoMeshLoader.DecodeResult>> decodeTasks;
    private List<int> sizes;
    private int nLayers;
    private Mesh mesh;
    private long previousAnimationTime;
    private long decodeStartTime;
    private long frameReadyTime;
    private long frameIdleTime;

    private StreamWriter writer;

    public Text animLatency;
    public Text decodeLatency;
    public Text frameReadyLatency;
    public Text temp;

    public GameObject HQ;
    public GameObject MQ;
    public GameObject LQ;

    private MeshFilter hqFilter;
    private MeshFilter mqFilter;
    private MeshFilter lqFilter;

    private int quality = 0;


    // Start is called before the first frame update
    void Start()
    {
        init("127.0.0.1", 8000, true, false);

        meshFilter = GetComponent<MeshFilter>();
        hqFilter = HQ.GetComponent<MeshFilter>();
        mqFilter = MQ.GetComponent<MeshFilter>();
        lqFilter = LQ.GetComponent<MeshFilter>();
        decodeTasks = new();
        sizes = new();
        previousAnimationTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

    }

    // Update is called once per frame
    void Update()
    {
        if (decodeTasks.Count() > 0 && decodeTasks.All(t => t.IsCompleted))
        {
            float decodeEnd = Time.realtimeSinceStartup;
            isDecoding = false;
            if (decodeTasks.All(t => t.Result.success))
            {
                int totalSize = 0;
                for (int i = 0; i < nLayers; i++)
                {
                    totalSize += meshDataArray[i].vertexCount;
                }
                Destroy(mesh);
                mesh = new Mesh();
                var col = new NativeArray<Color32>(totalSize, Allocator.TempJob);
                var pos = new NativeArray<Vector3>(totalSize, Allocator.TempJob);
                int offset = 0;
                for (int i = 0; i < nLayers; i++)
                {
                    meshDataArray[i].GetColors(col.GetSubArray(offset, meshDataArray[i].vertexCount));
                    meshDataArray[i].GetVertices(pos.GetSubArray(offset, meshDataArray[i].vertexCount));
                    offset += meshDataArray[i].vertexCount;
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

                HQ.SetActive(false);
                MQ.SetActive(false);
                LQ.SetActive(false);
                if (quality >= 60)
                {
                    HQ.SetActive(true);
                    hqFilter.mesh = mesh;
                    Debug.Log("HQ");
                }
                else if (quality >= 40)
                {
                    MQ.SetActive(true);
                    mqFilter.mesh = mesh;
                    Debug.Log("MQ");
                }
                else
                {
                    LQ.SetActive(true);
                    lqFilter.mesh = mesh;
                    Debug.Log("LQ");
                }
                col.Dispose();
                pos.Dispose();
                long currAnimationTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                long interframeLatency = currAnimationTime - previousAnimationTime;
                long decodeEndTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                animLatency.text = interframeLatency.ToString() + "ms";
                long decodeLatencyL = (decodeEndTime - decodeStartTime);
                decodeLatency.text = decodeLatencyL.ToString() + "ms";
                previousAnimationTime = currAnimationTime;
            }
            meshDataArray.Dispose();
            decodeTasks.Clear();
            sizes.Clear();
        }
        if (!frameReady)
        {
            num = next_frame();
            int prevNum = num;
            if (num > 0)
            {
                frameReady = true;
                frameReadyTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }
     
        }
        temp.text = num.ToString();
        if (num > 50 && !isDecoding)
        {
            numTemp = num;
            quality = 0;
            decodeStartTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            frameIdleTime = decodeStartTime - frameReadyTime;
            frameReadyLatency.text = frameIdleTime.ToString() + "ms";
            currentFrame++;
            frameReady = false;
            isDecoding = true;
            data = new byte[num];
            set_frame_data(data);
            //12
            int offset = 0;
            nLayers = (int)BitConverter.ToUInt32(data, offset);
            Debug.Log(nLayers);
            //nLayers = 1;
            //UInt32 layerId = BitConverter.ToUInt32(data, 28);
            meshDataArray = Mesh.AllocateWritableMeshData(nLayers);
            // Debug.Log("start decoding " + data.Length.ToString());
            offset += 7 * 4;
            for (int i = 0; i < nLayers; i++)
            {
                UInt32 layerId = BitConverter.ToUInt32(data, offset);
                switch (layerId)
                {
                    case 0:
                        quality += 60;
                        break;
                    case 1:
                        quality += 25;
                        break;
                    case 2:
                        quality += 15;
                        break;
                }
                offset += 4;
                UInt32 layerSize = BitConverter.ToUInt32(data, offset);
                offset += 4;
                byte[] layerData = new byte[layerSize];
                Array.Copy(data, offset, layerData, 0, layerSize);
                offset += (int)layerSize;
                sizes.Add((int)layerSize);
                var draco = new DracoMeshLoader();
                decodeTasks.Add(draco.ConvertDracoMeshToUnity(
                    meshDataArray[i],
                    layerData,
                    false, // Set to true if you require normals. If Draco data does not contain them, they are allocated and we have to calculate them below
                    false// Retrieve tangents is not supported, but this will ensure they are allocated and can be calculated later (see below)
                ));
            }
            num = 0;
            // Async decoding has to start on the main thread and spawns multiple C# jobs.
            //decodeTask = 
        }
    }
}
