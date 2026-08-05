using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TestTileScript : MonoBehaviour
{
    [SerializeField]
    private Tilemap WorldMap;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("x : " + WorldMap.size.x);
            Debug.Log("y : " + WorldMap.size.y);
        }
    }
}
