
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;


public class SelfRenderer : MonoBehaviour
{
    public GameObject HQ;
    private MeshFilter hqFilter;
    private Mesh mesh;
    private Mesh.MeshDataArray meshDataArray;
    // Start is called before the first frame update
    void Start()
    {
        hqFilter = HQ.GetComponent<MeshFilter>();
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void SetData(byte[] pcData)
    {
            Destroy(mesh);
            mesh = new Mesh();
            int numPoints = pcData.Length / 15 * 3;
            int numColors = pcData.Length / 15 * 3;
            float[] p = new float[numPoints];
            byte[] c = new byte[numColors];
            // Bytes are bytes we can just copy them over to the float array to change their representation
            Buffer.BlockCopy(pcData, 0, p, 0, numPoints * sizeof(float));
            Buffer.BlockCopy(pcData, numPoints * sizeof(float), c, 0, numColors);

            // Convert to Unity data structures
            Vector3[] pp = new Vector3[numPoints / 3];
            Color32[] cc = new Color32[numColors / 3];
            for (int i = 0; i < numPoints; i += 3)
            {
            if (p[i] > 1000 || p[i+1] > 1000 || p[i+2] > 1000)
                Debug.Log(i + " " + p[i] + " " + p[i+1] + " " + p[i+2]);
                Vector3 v = new Vector3(p[i], p[i + 1], p[i + 2]);
              Color32 c32 = new Color32(c[i], c[i + 1], c[i + 2], 255);
           // Color32 c32 = new Color32(0, 0, 0, 255);
            pp[i / 3] = v;
                cc[i / 3] = c32;
            }
            mesh.indexFormat = numPoints > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(pp);
            mesh.SetColors(cc);
            mesh.SetIndices(
                    Enumerable.Range(0, mesh.vertexCount).ToArray(),
                    MeshTopology.Points, 0
            );

            // Use the resulting mesh
            mesh.UploadMeshData(true);

            hqFilter.mesh = mesh;
    }
}
