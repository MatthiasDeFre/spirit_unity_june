using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class SessionManager : MonoBehaviour
{
    public int LoggingLevel = 0;
    public int ClientID = 0;
    public int NDescriptions = 1;
    public int PeerUDPPort = 8000;
    public string PeerSFUAddress = "10.10.130.215:8094";

    private Process peerProcess;

    // ################## GameObjects ####################
    public List<GameObject> StartLocations;
    public PCSelf PCSelfPrefab;
    public PCReceiver PCReceiverPrefab;

    // ################# Private Variables ###############
    private PCSelf pcSelf;
    private List<PCReceiver> pcReceivers = new();
    // Start is called before the first frame update
    void Start()
    {
        // Init DLLs for logging
        DLLWrapper.LoggingInit(LoggingLevel);
        // TODO Start peer
        WebRTCInvoker.initialize("127.0.0.1", 8000, "127.0.0.1", 8000, (uint)NDescriptions, (uint)ClientID, "1.0");
        
        peerProcess = new Process();
        peerProcess.StartInfo.FileName = Application.dataPath + "/peer/webRTC-peer-win.exe";
        peerProcess.StartInfo.Arguments = $"-p :{PeerUDPPort} -i -o -sfu {PeerSFUAddress} -c {ClientID}";
        /*peerProcess.StartInfo.CreateNoWindow = !peerInWindow;
        if (peerInWindow && peerWindowDontClose)
        {
            peerProcess.StartInfo.Arguments = $"/K {peerProcess.StartInfo.FileName} {peerProcess.StartInfo.Arguments}";
            peerProcess.StartInfo.FileName = "CMD.EXE";
        }*/

        
        // Init WebRTC
        

        if (!peerProcess.Start())
        {
            Debug.LogError("Failed to start peer process");
            peerProcess = null;
            return;
        }
       
        
        // Make correct prefabs
        CreateStartPrefabs();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDestroy()
    {
        peerProcess?.Kill();
        WebRTCInvoker.clean_up();
        
    }

    // ############################# Private Functions #######################################
    private void CreateStartPrefabs()
    {
        for(int i = 0; i < StartLocations.Count; i++)
        {
            if(ClientID == i) // This user
            {
                pcSelf = Instantiate(PCSelfPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                pcSelf.transform.parent = StartLocations[i].transform;
            } else // Other users
            {
                PCReceiver pcReceiver = Instantiate(PCReceiverPrefab, new Vector3(0, 0, 0), Quaternion.identity);
                pcReceiver.transform.parent = StartLocations[i].transform;
                pcReceiver.ClientID = (uint)i;
                pcReceiver.NDescriptions = NDescriptions;
                pcReceivers.Add(pcReceiver);
            }
        }
    }
}
