using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SMBPipe : MonoBehaviour {

    //detection variable
    public bool isDetected = false;

    //belief stack variables
    public bool printed;
    public bool beliefArrayComplete;
    public string Agent = "empty";
    public string Action = "empty";
    public string Interaction = "empty";
    public string Result = "empty";

    public bool enteredFromRight;
    public float initialX, finalX;

    //tuple
    public string[] beliefArray;

    public void updateBeliefArray()
    {
        beliefArray = new string[4];

        beliefArray[0] = Agent;
        beliefArray[1] = Action;
        beliefArray[2] = Interaction;
        beliefArray[3] = Result;
    }

    public void Reset()
    {
        for (int i = 0; i < beliefArray.Length; i++)
        {
            beliefArray[i] = "empty";
        }

        beliefArrayComplete = false;
    }

    // Use this for initialization
    void Start () {
        updateBeliefArray();
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
