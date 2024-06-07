using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class DecodedPointCloudData
{
    public int NPoints;
    public int MaxDescriptions;
    public int CurrentNDescriptions;
    public List<Vector3> Points;
    public List<Color32> Colors;
    private Mutex mut = new Mutex();
    public DecodedPointCloudData(int nPoints, int maxDescriptions)
    {
        NPoints = nPoints;
        Points = new(nPoints);
        Colors = new(nPoints);
        MaxDescriptions = maxDescriptions;
    }
    public void LockClass() { 
        mut.WaitOne();
    }
    public void UnlockClass()
    {
        mut.ReleaseMutex();
    }

}
