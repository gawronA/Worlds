using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Surface : MonoBehaviour
{
    public float[] m_surfaceValues { get; private set; }


    public GameObject surfaceChunkPrefab;
    void Start()
    {
        GameObject obj = Instantiate(surfaceChunkPrefab);
        SurfaceChunk surfaceChunk = obj.GetComponent<SurfaceChunk>();
        surfaceChunk.Initalize(0, 1, this);

        surfaceChunk.Refresh();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
