using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Surface : MonoBehaviour
{
    public float[] m_surfaceValues { get; private set; }


    public GameObject surfaceChunkPrefab;
    void Start()
    {
        m_surfaceValues = new float[27] { 1f, -1f, 1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f };
        GameObject obj = Instantiate(surfaceChunkPrefab);
        SurfaceChunk surfaceChunk = obj.GetComponent<SurfaceChunk>();
        surfaceChunk.Initalize(0, 2, this);

        surfaceChunk.Refresh();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
