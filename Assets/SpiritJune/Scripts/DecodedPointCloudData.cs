using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecodedPointCloudData
{
    public int NPoints;
    public List<Vector3> Points;
    public List<Color32> Colors;

    public DecodedPointCloudData(List<Vector3> points, List<Color32> colors)
    {
        NPoints = points.Count;
        Points = points;
        Colors = colors;
    }
}
