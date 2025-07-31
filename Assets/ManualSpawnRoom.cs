using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManualSpawnRoom : MonoBehaviour
{

    public Texture2D chosenRoom;
    public GameObject chosenDecor;
    public RoomGenerator roomGenerator;

    // Start is called before the first frame update
    void Start()
    {
        if (chosenRoom != null) {
            roomGenerator.setManual(chosenRoom, chosenDecor);
            roomGenerator.CallGenerateTest();
        }    
    }



    // Update is called once per frame
    void Update()
    {
        
    }
}
