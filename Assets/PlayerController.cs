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
using System.Runtime.CompilerServices;
using System.Xml;
using Unity.VisualScripting;

enum RenderMode
{
    SELF,
    CONFERENCE,
}
public class PlayerController : MonoBehaviour
{
    [DllImport("DracoDLLTest")]
    static extern int init(string addr, UInt32 port, bool input, bool output);
    [DllImport("DracoDLLTest")]
    static extern int next_frame();
    [DllImport("DracoDLLTest")]
    static extern int set_frame_data(byte[] b);
    [DllImport("DracoDLLTest")]
    static extern int set_self_data(byte[] b); 
    [DllImport("DracoDLLTest")]
    static extern void set_camera_far(float far);
    [DllImport("DracoDLLTest")]
    static extern void set_self_render(bool render_mode);
    [DllImport("DracoDLLTest")]
    static extern int next_self_frame();
    [DllImport("DracoDLLTest")]
    static extern void send_data_to_server(byte[] b);
    /*delegate int init(string addr, UInt32 port, bool input, bool output);
    delegate int next_frame();
    delegate int set_frame_data(byte[] b);
    delegate int set_self_data(byte[] b);
    delegate void set_camera_far(float far);
    delegate void set_render_mode(int render_mode);
    delegate int next_self_frame();
    delegate void set_self_render(bool self_render);*/
    static IntPtr nativeLibraryPtr;

    public UInt32 ProxyPort = 8000;

    private int num;
    private bool isDecoding = false;
    private bool frameReady = false;
    private byte[] data;
    private Mesh.MeshDataArray meshDataArray;
    private List<Task<DracoMeshLoader.DecodeResult>> decodeTasks;
    private List<int> sizes;
    private int nLayers;
    private Mesh mesh;

    public SelfRenderer selfRenderer;

    public UserRenderer user1Rend;
    public UserRenderer user2Rend;
    public UserRenderer user3Rend;
    public UserRenderer user4Rend;

    private Dictionary<UInt32, UserRenderer> userRenderers;

    private int quality = 0;

    private RenderMode renderMode = RenderMode.CONFERENCE;

    public GameObject user1Cam;
    public GameObject user2Cam;
    public GameObject user3Cam;
    public GameObject user4Cam;

    public CubeCheck cube1;
    public CubeCheck cube2;
    public CubeCheck cube3;
    public CubeCheck cube4;

    private int cl;
    private float fovCounter = 0.0f;
    // Start is called before the first frame update
    void Awake()
    {
        /*   if (nativeLibraryPtr != IntPtr.Zero) return;
           nativeLibraryPtr = Native.LoadLibrary("DracoDLLTest");
           if (nativeLibraryPtr == IntPtr.Zero)
           {
               Debug.LogError("Failed to load native library");
           }*/
        string[] args = System.Environment.GetCommandLineArgs();
        userRenderers = new Dictionary<UInt32, UserRenderer>();
        bool foundCl = false;
        for (int i = 0; i < args.Length; i++)
        {
            if(!foundCl)
            {
                if (args[i].Contains("-cl1"))
                {
                    user1Cam.SetActive(true);
                    userRenderers.Add(2, user2Rend);
                    userRenderers.Add(3, user3Rend);
                    userRenderers.Add(4, user4Rend);
                    foundCl = true;
                    cl = 1;
                }
                else if (args[i].Contains("-cl2"))
                {
                    user2Cam.SetActive(true);
                    userRenderers.Add(1, user1Rend);
                    userRenderers.Add(3, user3Rend);
                    userRenderers.Add(4, user4Rend);
                    foundCl = true;
                    cl = 2;
                }
                else if (args[i].Contains("-cl3"))
                {
                    user3Cam.SetActive(true);
                    userRenderers.Add(1, user1Rend);
                    userRenderers.Add(2, user2Rend);
                    userRenderers.Add(4, user4Rend);
                    foundCl = true;
                    cl = 3;
                }
                else
                {

                }
            }
            
        }
        if (!foundCl)
        {
            user4Cam.SetActive(true);
            userRenderers.Add(1, user1Rend);
            userRenderers.Add(2, user2Rend);
            userRenderers.Add(3, user3Rend);
            foundCl = true;
            cl = 4;
        }
        foreach (var userRend in userRenderers.Values)
        {
            userRend.gameObject.SetActive(true);
        }
    }
    void Start()
    {
        init("127.0.0.1", ProxyPort, true, false);
       // set_self_render(false);
        sizes = new();

    }

    // Update is called once per frame
      void Update()
      {
          switch (renderMode)
          {
              case RenderMode.SELF:
                  RenderSelf(); 
                  break;
              case RenderMode.CONFERENCE:
                  RenderConference(); 
                  break;
          }
        fovCounter += Time.deltaTime;
        if(fovCounter >= 0.1)
        {
            byte[] d = new byte[20];
            byte[] ctrlP = BitConverter.GetBytes(1);
            byte[] nvis1 = BitConverter.GetBytes(cube1.GetVisRating());
            byte[] nvis2 = BitConverter.GetBytes(cube2.GetVisRating());
            byte[] nvis3 = BitConverter.GetBytes(cube3.GetVisRating());
            byte[] nvis4 = BitConverter.GetBytes(cube4.GetVisRating());
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(ctrlP);
                Array.Reverse(nvis1);
                Array.Reverse(nvis2);
                Array.Reverse(nvis3);
                Array.Reverse(nvis4);
            }

            Array.Copy(ctrlP, 0, d, 0, ctrlP.Length);
            Array.Copy(nvis1, 0, d, 4, nvis1.Length);
            Array.Copy(nvis2, 0, d, 8, nvis2.Length);
            Array.Copy(nvis3, 0, d, 12, nvis3.Length);
            Array.Copy(nvis4, 0, d, 16, nvis4.Length);

            send_data_to_server(d);
            fovCounter = 0;

        }
      }
      void RenderSelf()
      {
          int self_num = next_self_frame();
        Debug.Log(self_num);
          if (self_num > 0)
          {
              byte[] data = new byte[self_num];
              set_self_data(data);
              selfRenderer.SetData(data);
          }
      }
      void RenderConference()
      {
        bool isAllReady = true;
        foreach (var userRend in userRenderers.Values)
        {
            isAllReady = isAllReady & userRend.CheckIfReady();
        }
        if(isAllReady)
        {
            sizes.Clear();
        }  
              
        if (!frameReady)
        {
            num = next_frame();
            Debug.Log(num);
            int prevNum = num;
            if (num > 0)
            {
                frameReady = true;
            }

        }
          if (num > 50 && isAllReady)
          {
              quality = 0;
              frameReady = false;
              isDecoding = true;
              data = new byte[num];
              set_frame_data(data);
            //12
            int offset = 0;
              UInt32 nFrames = BitConverter.ToUInt32(data, offset);
            offset += 4;
            for(int j = 0; j < nFrames; j++)
            {
                offset += 4; // FrameLen
                UInt32 clientID = BitConverter.ToUInt32(data, offset);
                offset += 4;
                nLayers = (int)BitConverter.ToUInt32(data, offset);
                userRenderers[clientID].CreateDataArray(nLayers);
                Debug.Log("NFrames: " + nFrames + " ClientID: " + clientID + " NLayers: " + nLayers);
                offset += 7 * 4;
                for (int i = 0; i < nLayers; i++)
                {
                    UInt32 layerId = BitConverter.ToUInt32(data, offset);
                    if (layerId == 1 || layerId == 0)
                    {
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
                    }
                    offset += 4;
                    UInt32 layerSize = BitConverter.ToUInt32(data, offset);
                    offset += 4;
                    byte[] layerData = new byte[layerSize];
                    Array.Copy(data, offset, layerData, 0, layerSize);
                    offset += (int)layerSize;
                    sizes.Add((int)layerSize);

                    userRenderers[clientID].SetQuality(quality);
                    userRenderers[clientID].AddDecodeTask(layerData, i);
                }
              }
           
              num = 0;
          }
      }

}
