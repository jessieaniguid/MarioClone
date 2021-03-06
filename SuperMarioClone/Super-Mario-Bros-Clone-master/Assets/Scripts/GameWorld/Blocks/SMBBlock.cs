﻿using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(BoxCollider2D))]
public class SMBBlock : MonoBehaviour {

    //attaching rigidbodies
    public Rigidbody2D rb;

    //detection variables
    public bool isDetected = false;

    //belief stack variables
    public bool printed;
    public bool beliefArrayComplete;
    public string Agent = "empty";
    public string Action = "empty";
    public string Interaction = "empty";
    public string Result = "empty";

    //belief array
    public string[] beliefArray;

    //state
    public bool isDead = false;

    public bool enteredFromRight;
    public float initialX, finalX;



    enum BounceState {
		None,
		Up,
		Down
	}
		
	private Vector3 _posBeforeBounce;
	private BounceState _bounceState;

	protected bool  _isDestroyed;

	protected Animator       _animator;
	protected BoxCollider2D  _collider;

	public float _bounceVelocity = 1f;
	public float _bounceYDist = 0.15f;

	void Awake() {

		_animator = GetComponent<Animator> ();
		_collider = GetComponent<BoxCollider2D> ();
	}

    public void updateBeliefArray()
    {
        beliefArray[0] = Agent;
        beliefArray[1] = Action;
        beliefArray[2] = Interaction;
        beliefArray[3] = Result;
    }

    private void Start()
    {
        rb = gameObject.AddComponent<Rigidbody2D>();
        rb.isKinematic = true;
        isDetected = false;

        //setting up array
        beliefArray = new string[4];
        beliefArray[0] = Agent;
        beliefArray[1] = Action;
        beliefArray[2] = Interaction;
        beliefArray[3] = Result;
    }

    void Update() {
        isDead = _isDestroyed;

		if (_bounceState != BounceState.None)
			Bounce ();

        updateBeliefArray();
    }

	void OnInteraction(SMBPlayer player) {

		if (_bounceState == BounceState.None && !_isDestroyed) {
            isDead = true;
			DestroyBlock (player);

			_posBeforeBounce = transform.position;
			_bounceState = BounceState.Up;
		}
	}

	private void Bounce() {

		Vector3 currentPos = transform.position;

		if (_bounceState == BounceState.Up) {

			if (currentPos.y <= _posBeforeBounce.y + _bounceYDist) {

				transform.Translate (_bounceVelocity * Vector2.up * Time.fixedDeltaTime);
			}
			else  {
				
				_bounceState = BounceState.Down;
			}
		}
		else if (_bounceState == BounceState.Down) {

			if (currentPos.y >= _posBeforeBounce.y) {

				transform.Translate (_bounceVelocity * Vector2.down * Time.fixedDeltaTime);
			}
			else {

				_bounceState = BounceState.None;
				transform.position = _posBeforeBounce;

				GivePrize ();
			}
		}
	}

    public void Reset()
    {
        for (int i = 0; i < beliefArray.Length; i++)
        {
            beliefArray[i] = "empty";
        }

        beliefArrayComplete = false;
    }

    protected virtual void DestroyBlock (SMBPlayer player) {
		
	}

	protected virtual void GivePrize () {

	}

	void OnPauseGame() {

		_animator.enabled = false;
	}

	void OnResumeGame() {

		_animator.enabled = true;
	}
}
