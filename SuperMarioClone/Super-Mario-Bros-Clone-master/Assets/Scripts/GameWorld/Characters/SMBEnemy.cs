using UnityEngine;
using System.Collections;



public class SMBEnemy : SMBCharacter {

    //Rigidbody
    public Rigidbody2D rb;

    //detection variable
    public bool isDetected = false;

    //belief stack variables
    public string Agent;
    public string Action;
    public string Interaction;
    public string Result;

    //tuple
    public string[] beliefArray;

    //state variables
    public bool isDead = false;


    private SMBConstants.EnemyState _state;



	void Start() {
        rb = gameObject.AddComponent<Rigidbody2D>();
        rb.isKinematic = true;
		_state = SMBConstants.EnemyState.Move;

        //setting up array
        beliefArray = new string[4];
	}

	// Update is called once per frame
	override protected void Update () {

        beliefArray[0] = Agent;
        beliefArray[1] = Action;
        beliefArray[2] = Interaction;
        beliefArray[3] = Result;

        if (_state == SMBConstants.EnemyState.Dead)
			return;

		SMBConstants.MoveDirection side = SMBConstants.MoveDirection.Forward;
		if(isFlipped())
			side = SMBConstants.MoveDirection.Backward;
		
		Move (xSpeed * (float)side);

		base.Update ();
	}

	void Die() {

        isDead = true;

		_body.velocity = Vector2.zero;
		_body.ApplyForce (Vector2.up);
		_body.ApplyForce (Vector2.right * Random.Range(-2f, 2f) * Time.fixedDeltaTime);

		_collider.applyHorizCollision = false;
		_collider.applyVertCollision = false;

		gameObject.layer = LayerMask.NameToLayer ("Ignore Raycast");

		_state = SMBConstants.EnemyState.Dead;
		_animator.SetTrigger ("triggerDie");

		Invoke ("DestroyEnemy", 4f);
	}

	void DestroyEnemy() {

		Destroy (gameObject);
	}

	override protected void OnVerticalCollisionEnter(Collider2D collider) {
				
		float side = (float)SMBConstants.MoveDirection.Forward;
		if(isFlipped())
			side = (float)SMBConstants.MoveDirection.Backward;

		Vector2 yRayOrigin = _collider.Collider.bounds.max - 
			Vector3.up * _collider.Collider.bounds.size.y - Vector3.up * SMBConstants.playerSkin;

		if (side == (float)SMBConstants.MoveDirection.Backward)
			yRayOrigin.x -= _collider.Collider.bounds.size.x;

		RaycastHit2D yRay = Physics2D.Raycast(yRayOrigin, Vector2.down, SMBConstants.playerSkin);
		if (!yRay.collider) {

			_body.velocity.x = 0f;
			_renderer.flipX = !_renderer.flipX;

			transform.position = transform.position - Vector3.right * side * SMBConstants.playerSkin;
		}

		base.OnVerticalCollisionEnter (collider);
	}

	void OnHorizontalCollisionEnter(Collider2D collider) {

		if (collider.tag == "Player")
			collider.SendMessage("OnHorizontalTriggerEnter", _collider.Collider, SendMessageOptions.RequireReceiver);
		else
			_renderer.flipX = !_renderer.flipX;
	}
}
