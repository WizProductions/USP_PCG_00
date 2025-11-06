using UnityEditor.Networking.PlayerConnection;
using UnityEngine;
using VTools.Grid;

public class CameraScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (GameObject.Find("PGG") is GameObject go)
        {
            if (go.GetComponent<BaseGridGenerator>() is var PGG)
            {
                int halfGridXValue = PGG._gridXValue / 2;
                Vector3 myNewPos = gameObject.transform.position;
                myNewPos.x = go.transform.position.x + (halfGridXValue * PGG._cellSize);
                myNewPos.z = go.transform.position.x + (halfGridXValue * PGG._cellSize);

                gameObject.transform.position = myNewPos;
                
                GetComponent<Camera>().orthographicSize = halfGridXValue + (8 + halfGridXValue / 10);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
