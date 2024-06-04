using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class CubeCheck : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject cubeMiddle;
    public GameObject leftCenter;
    public GameObject leftFar;
    public GameObject rightCenter;
    public GameObject rightFar;

    private Renderer middleRenderer;
    private Renderer leftCenterRenderer;
    private Renderer rightCenterRenderer;
    private Renderer leftFarRenderer;
    private Renderer rightFarRenderer;

    void Start()
    {
        middleRenderer = cubeMiddle.GetComponent<Renderer>();
        leftCenterRenderer = leftCenter.GetComponent<Renderer>();
        rightCenterRenderer = rightCenter.GetComponent<Renderer>();
        leftFarRenderer = leftFar.GetComponent<Renderer>();
        rightFarRenderer = rightFar.GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public int GetVisRating()
    {
        int nvis = 0;
        nvis += middleRenderer.isVisible ? 1 : 0;
        nvis += leftCenterRenderer.isVisible ? 1 : 0;
        nvis += rightCenterRenderer.isVisible ? 1 : 0;
        nvis += leftFarRenderer.isVisible ? 1 : 0;
        nvis += rightFarRenderer.isVisible ? 1 : 0;
        if(nvis >= 5)
        {
            return 0;
        } else if(nvis >= 3)
        {
            return 1;
        } else if (nvis > 0)
        {
            return 2;
        }
        return 3;

    }
}
