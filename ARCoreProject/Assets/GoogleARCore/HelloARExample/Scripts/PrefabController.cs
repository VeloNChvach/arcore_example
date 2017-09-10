using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrefabController : MonoBehaviour {

    private GameObject btnUp;
    private GameObject btnDown;
    private GameObject btnDoor;

    public static PrefabController Instance;

    void Start ()
    {

        //Instance = this;
        GameObject.Find("Canvas_help").SetActive(false);


        //btnUp.SetActive(false);
        //btnDown.SetActive(false);
        //btnDoor.SetActive(false);
    }
    
    public void PublicMethod()
    {
        Debug.Log("Public method");
    }
}
