using AOT;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor.Search;
using UnityEngine;

public class PCSelf : MonoBehaviour
{
    System.Threading.Thread workerThread;
    static bool keep_working = true;
    [MonoPInvokeCallback(typeof(DracoInvoker.descriptionDoneCallback))]
    static void OnDescriptionDoneCallback(IntPtr dsc, IntPtr rawDataPtr, UInt32 dscSize, UInt32 frameNr, UInt32 dscNr)
    {
        if(!keep_working)
        {
            int nSend = WebRTCInvoker.send_tile(rawDataPtr, dscSize, dscNr);

            if (nSend == -1)
            {
               keep_working = false;
               Debug.Log("Stop capturing");
            }
        }
        DracoInvoker.free_description(dsc);

    }
    [MonoPInvokeCallback(typeof(DracoInvoker.freePCCallback))]
    static void OnFreePCCallback(IntPtr pc)
    {
        Realsense2Invoker.free_point_cloud(pc);
    }
    // Start is called before the first frame update
    void Start()
    {
        DracoInvoker.register_description_done_callback(OnDescriptionDoneCallback);
        DracoInvoker.register_free_pc_callback(OnFreePCCallback);
        DracoInvoker.initialize();
        int initCode = Realsense2Invoker.initialize(848, 480, 30, false);
        if (initCode == 0)
        {
            workerThread = new System.Threading.Thread(pollFrames);
            workerThread.Start();
        }
        else
        {
            Debug.Log($"Something went wrong inting the Realsense2: {initCode}");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void OnDestroy()
    {
        keep_working = false;
        workerThread.Join();
        DracoInvoker.clean_up();
    }
    void pollFrames()
    {
        keep_working = true;
        WebRTCInvoker.wait_for_peer();
       

         while(keep_working)
        {
            Debug.Log($"Poll next");
            IntPtr frame = Realsense2Invoker.poll_next_point_cloud();
            Debug.Log($"Poll done");
            if ( frame != IntPtr.Zero )
            {
                Debug.Log($"Get size");
                uint nPoints = Realsense2Invoker.get_point_cloud_size(frame);
                Debug.Log($"Number of points: {nPoints}");
                int returnCode = DracoInvoker.encode_pc(frame);
            } else
            {
                Debug.Log("No frame"); 
                keep_working = false;
            }
            
        }
        Realsense2Invoker.clean_up();
    }

    void dataEncodedCallback()
    {

    }
}
