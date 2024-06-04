using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Rendering;

public class PCReceiver : MonoBehaviour
{
    public uint ClientID;
    public int NDescriptions;
    
    private List<System.Threading.Thread> workerThreads = new List<System.Threading.Thread>();
    private List<ConcurrentQueue<DecodedPointCloudData>> queues = new List<ConcurrentQueue<DecodedPointCloudData>>();

    private bool keep_working = true;

    // ####################### Unity GameObjects #########################
    Mesh currentMesh;
    private MeshFilter meshFilter;

    // Start is called before the first frame update
    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        for (int i = 0; i < NDescriptions; i++)
        {
            queues.Add(new ConcurrentQueue<DecodedPointCloudData>());
            workerThreads.Add(new System.Threading.Thread(() =>
            {
                pollDescription((uint)i);
            }));
            workerThreads[i].Start();
        }
    }

    // Update is called once per frame
    void Update()
    {
        for(int i = 0; i < NDescriptions;i++)
        {
            if(!queues[i].IsEmpty)
            {
                DecodedPointCloudData dec;
                bool succes = queues[i].TryDequeue(out dec);
                if (succes)
                {
                    Debug.Log("Dequeue Successful!");
                    Destroy(currentMesh);
                    currentMesh = new Mesh();
                    currentMesh.indexFormat = dec.NPoints > 65535 ?
                            IndexFormat.UInt32 : IndexFormat.UInt16;
                    currentMesh.SetVertices(dec.Points);
                    currentMesh.SetColors(dec.Colors);
                    currentMesh.SetIndices(
                        Enumerable.Range(0, currentMesh.vertexCount).ToArray(),
                        MeshTopology.Points, 0
                    );
                    currentMesh.UploadMeshData(true);
                    meshFilter.mesh = currentMesh;
                }
            }
        }
    }

    private void pollDescription(uint descriptionID)
    {
        while(keep_working)
        {
            int descriptionSize = WebRTCInvoker.get_tile_size(ClientID, descriptionID);
            byte[] messageBuffer = new byte[descriptionSize];
            IntPtr decoderPtr = IntPtr.Zero;
            unsafe
            {
                fixed (byte* ptr = messageBuffer)
                {
                    WebRTCInvoker.retrieve_tile(ptr, (uint)descriptionSize, ClientID, descriptionID);
                    decoderPtr = DracoInvoker.decode_pc(ptr, (uint)descriptionSize);

                    if (decoderPtr == IntPtr.Zero)
                    {
                        Debug.Log($"Debug error at client {ClientID} for description {descriptionID}");
                        continue;
                    }
                    UInt32 nDecodedPoints = DracoInvoker.get_n_points(decoderPtr);
                    Debug.Log($"Number of points after decoding: {nDecodedPoints}");
                    IntPtr pointsPtr = DracoInvoker.get_point_array(decoderPtr);
                    IntPtr colorPtr = DracoInvoker.get_color_array(decoderPtr);
                    float* pointsUnsafePtr = (float*)pointsPtr;
                    byte* colorsUnsafePtr = (byte*)colorPtr;

                    // TODO
                    //      * Get header from frame
                    //      * Check if frame exists
                    //      * If not 

                    // Header:
                    //          * FrameNr
                    //          * NPointsFrame
                    //          * DescriptionNr

                    // Frame:
                    //          * NCompleted
                    //          * Quality

                    // TODO check if frame exists
                    List<Vector3> points = new((int)nDecodedPoints);
                    List<Color32> colors = new((int)nDecodedPoints);
                    for (int i = 0; i < nDecodedPoints; i++)
                    {
                        points.Add(new Vector3(pointsUnsafePtr[(i * 3)], pointsUnsafePtr[(i * 3) + 1], pointsUnsafePtr[(i * 3) + 2]));
                        colors.Add(new Color32(colorsUnsafePtr[(i * 3)], colorsUnsafePtr[(i * 3) + 1], colorsUnsafePtr[(i * 3) + 2], 255));
                    }

                    queues[(int)descriptionID].Enqueue(new DecodedPointCloudData(points, colors));
                    DracoInvoker.free_decoder(decoderPtr);
                }
            }
            
            
        }
    }

}
