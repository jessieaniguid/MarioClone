using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SMBRigidBody))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Animator))]
public class SMBItem : MonoBehaviour {

	protected SMBRigidBody _body;
	protected AudioSource _audio;
	protected Animator _animator;

    //detection variable
    public bool isDetected = false;

    //belief stack variables
    public bool printed;
    public bool beliefArrayComplete;
    public string Agent = "empty";
    public string Action = "empty";
    public string Interaction = "empty";
    public string Result = "empty";
    public SMBPlayer player;

    public bool enteredFromRight;
    public float initialX, finalX;

    //tuple
    public string[] beliefArray;

    //state variables
    public bool isDead = false;

    public void Reset()
    {
        for (int i = 0; i < beliefArray.Length; i++)
        {
            beliefArray[i] = "empty";
        }

        beliefArrayComplete = false;
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<SMBPlayer>();

        beliefArray = new string[4];
        updateBeliefArray();
    }

    public void updateBeliefArray()
    {
        beliefArray[0] = Agent;
        beliefArray[1] = Action;
        beliefArray[2] = Interaction;
        beliefArray[3] = Result;
    }

    virtual protected void Awake() {

		_body = GetComponent<SMBRigidBody> ();
		_audio = GetComponent<AudioSource> ();
		_animator = GetComponent<Animator> ();
	}
		
	virtual protected void OnInteraction() {

        Interaction = "GrowUp";
        Action = "Eat";
        updateBeliefArray();
        player.printArray(beliefArray);


        Destroy (gameObject);
	}

	void OnPauseGame() {

		_body.enabled = false;
		_animator.enabled = false;
	}

	void OnResumeGame() {

		_body.enabled = true;
		_animator.enabled = true;
	}
}