using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor.Search;
using UnityEngine;

public class PCSelf : MonoBehaviour
{
    System.Threading.Thread workerThread;
    bool keep_working = true;

    

    // Start is called before the first frame update
    void Start()
    {
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

    void pollFrames()
    {
        WebRTCInvoker.wait_for_peer();
        while (keep_working)
        {          
            IntPtr frame = Realsense2Invoker.poll_next_point_cloud();
            if (frame != IntPtr.Zero)
            {
                IntPtr encoderPtr = DracoInvoker.encode_pc(frame);
                Realsense2Invoker.free_point_cloud(frame);
                IntPtr rawDataPtr = DracoInvoker.get_raw_data(encoderPtr);
                UInt32 encodedSize = DracoInvoker.get_encoded_size(encoderPtr);
                // TODO 
                //      * Add frame header
                int nSend = WebRTCInvoker.send_tile(rawDataPtr, encodedSize, 0);
                DracoInvoker.free_encoder(encoderPtr);
                if(nSend == -1)
                {
                    keep_working = false;
                    Debug.Log("Stop capturing");
                }
            }
            else
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
