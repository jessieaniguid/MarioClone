using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;


[RequireComponent (typeof (SMBParticleSystem))]
public class SMBPlayer : SMBCharacter {

	enum SoundEffects {
		Jump,
		Kick,
		GrowUp
	}

    //Text reading
    //got help from:
    //https://support.unity3d.com/hc/en-us/articles/115000341143-How-do-I-read-and-write-data-from-a-text-file-
    private StreamWriter stream;
    private float positionInterval;

    //adding circle collider
    public CircleCollider2D circleCollider;
    public float circleRadius;

    //stack
    public Stack<string[]> beliefStack;

    //belief stack variables
    public string Agent = "Mario";
    public string Action;
    public string Interaction;
    public string Result;
    public bool isAlive = true;
    public string[] marioBeliefArray;

    // Custom components
    private SMBParticleSystem _particleSystem;
		
    //Mario's current states
	private bool 	_isCoasting;
	private bool 	_isInvincible;
	private bool    _lockController;
	private float   _jumpTimer;
	private float   _runningTimer;
	private float   _blinkTimer;
	private int     _blinkAmount;
	private Bounds  _originalCollider;
	private Vector2 _velocityBeforeGrowUp;

	private SMBConstants.PlayerState _state;
	public SMBConstants.PlayerState State { get { return _state; } }


    //variables that affect Mario's current state
	public float blinkTime = 0.1f;
	public float runTime = 1f;
	public float longJumpTime = 1f;
	public float longJumpWeight = 0.1f;
	public float runningMultiplyer = 2f;
	public float minVelocityToCoast = 0.25f;

	public Bounds grownUpColliderSize;

	public AudioClip[] soundEffects;

	override protected void Awake() {

		_particleSystem = GetComponent<SMBParticleSystem> ();
		base.Awake ();

	}

	void Start() {
        beliefStack = new Stack<string[]>();

        marioBeliefArray = new string[4];
        marioBeliefArray[0] = Agent;
        marioBeliefArray[1] = Action;
        marioBeliefArray[2] = Interaction;
        marioBeliefArray[3] = Result;

        //https://answers.unity.com/questions/185268/adding-a-box-collider-to-an-object-in-csharp-scrip.html
        gameObject.AddComponent<CircleCollider2D>();
        circleCollider = GetComponent<CircleCollider2D>();
        circleRadius = 1;
        circleCollider.radius = circleRadius;
        circleCollider.isTrigger = true;

        //string path = "Assets/Scripts/Utils/newCoordinates.txt";

        //text editing
        //writer = new StreamWriter(path, false);

        //makes it so the player starts out as the tiny mario
        _state = SMBConstants.PlayerState.Short;

        //no particles shot
		_particleSystem._shootParticles = false;


		_originalCollider = _collider.GetSize();
		_originalCollider.center = Vector3.zero;
	}

    //override because we want to replace update function
	override protected void Update () {

        marioBeliefArray[0] = Agent;
        marioBeliefArray[1] = Action;
        marioBeliefArray[2] = Interaction;
        marioBeliefArray[3] = Result;

        //always returns true?
        if (_lockController)
			return;

        //Map Mario's position every .5 seconds
        positionInterval += Time.fixedDeltaTime;

        if(positionInterval > 0.5f)
        {
            mapPosition(transform.position.x, transform.position.y);
            positionInterval = 0;
        }

        SetInvincible (_isInvincible);
		float speed = DefineMoveSpeed ();

		Jump ();
		PlayJumpAnimation (speed);

        //if left key is pressed, move mario backwards
        //call coast
        //"MoveDirection.Backward" is just -1
		if (Input.GetKey (KeyCode.LeftArrow)) {

			Move (speed * (float)SMBConstants.MoveDirection.Backward);
			PlayMoveAnimation (speed, (float)SMBConstants.MoveDirection.Backward);
			Coast (SMBConstants.MoveDirection.Backward);
		} 
		else if (Input.GetKey (KeyCode.RightArrow)) {

			Move (speed * (float)SMBConstants.MoveDirection.Forward);
			PlayMoveAnimation (speed, (float)SMBConstants.MoveDirection.Forward);
			Coast (SMBConstants.MoveDirection.Forward);
		} 

        //this deals with when mario isn't running or coasting
        //if mario's x velocity is less than stopping speed, 
        //set mario's velocity to 0 and play "Idle" animation
		else {

			_body.velocity.x = Mathf.Lerp (_body.velocity.x, 0f, momentum * Time.fixedDeltaTime);
			_runningTimer = 0f;

			if (_isOnGround && !_isCoasting)
				_animator.Play ("Move");

			if (Mathf.Abs (_body.velocity.x) <= SMBConstants.stopingSpeed) {

				if (_isOnGround)
					_animator.Play ("Idle");

				_isCoasting = false;
				_body.velocity.x = 0f;
			}
		}
			
		// Check if mario is at the bottom of the screen
		if (transform.position.y < -0.2f)
			Die (0.4f, false);


        //calls the update function from SMBCharacter which
        //changer player position with new vector 3s???
		base.Update ();
	}

    //pushes the name of the object mario interacted with
    //when an enemy enters the collider
    //I attached a bool called detected in
    //SMBEnemy script so that we won't encounter them more than
    //once
    void OnTriggerEnter2D(Collider2D collision)
    {
        GameObject discoveredObject = collision.gameObject;
        if (discoveredObject.name == "g")
        {
            if (discoveredObject.GetComponent<SMBEnemy>().isDetected == false)
            {
                discoveredObject.GetComponent<SMBEnemy>().Agent = "Goomba";
                discoveredObject.GetComponent<SMBEnemy>().isDetected = true;
                discoveredObject.GetComponent<SMBEnemy>().initialX = discoveredObject.transform.position.x;
                if(discoveredObject.GetComponent<SMBEnemy>().initialX > transform.position.x)
                {
                    discoveredObject.GetComponent<SMBEnemy>().enteredFromRight = true;
                }else if(discoveredObject.GetComponent<SMBEnemy>().initialX < transform.position.x)
                {
                    discoveredObject.GetComponent<SMBEnemy>().enteredFromRight = false;
                }


            }
        }else if(discoveredObject.name == "e")
        {
            if (discoveredObject.GetComponent<SMBBlockBreakable>().isDetected == false)
            {
                discoveredObject.GetComponent<SMBBlockBreakable>().Agent = "BreakableBlock";
                discoveredObject.GetComponent<SMBBlockBreakable>().isDetected = true;
                discoveredObject.GetComponent<SMBBlockBreakable>().initialX = discoveredObject.transform.position.x;
                if (discoveredObject.GetComponent<SMBBlockBreakable>().initialX > transform.position.x)
                {
                    discoveredObject.GetComponent<SMBBlockBreakable>().enteredFromRight = true;
                }
                else if (discoveredObject.GetComponent<SMBBlockBreakable>().initialX < transform.position.x)
                {
                    discoveredObject.GetComponent<SMBBlockBreakable>().enteredFromRight = false;
                }
            }
        }
    }

    //When object is in Mario's collider, and when they die
    //and when Mario jumps, then action is jump and interaction is
    //kill
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (isAlive == false)
        {
            Interaction = "Take Damage";
            beliefStack.Push(marioBeliefArray);
            printArray(marioBeliefArray);
            return;
        }

        GameObject discoveredObject = collision.gameObject;


        if (discoveredObject.name == "g")
        {
            //These are so that once an array has already been printed
            //it won't be printed again

            if (discoveredObject.gameObject.GetComponent<SMBEnemy>().printed == true) return;

            if (discoveredObject.GetComponent<SMBEnemy>().isDetected == true 
                && transform.position.y > 0 && discoveredObject.GetComponent<SMBEnemy>().isDead == true)
            {
                discoveredObject.GetComponent<SMBEnemy>().Agent = "Goomba";
                discoveredObject.GetComponent<SMBEnemy>().Action = "Jump";
                discoveredObject.GetComponent<SMBEnemy>().Interaction = "Kill";
                beliefStack.Push(discoveredObject.GetComponent<SMBEnemy>().beliefArray);
                discoveredObject.GetComponent<SMBEnemy>().updateBeliefArray();
                printArray(discoveredObject.GetComponent<SMBEnemy>().beliefArray);
                discoveredObject.gameObject.GetComponent<SMBEnemy>().printed = true;
            }
        }else if(discoveredObject.name == "e")
        {
            if (discoveredObject.gameObject.GetComponent<SMBBlockBreakable>().printed == true) return;

            if (discoveredObject.GetComponent<SMBBlockBreakable>().isDetected == true
                && transform.position.y > 0 && discoveredObject.GetComponent<SMBBlockBreakable>().isDead == true)
            {
                discoveredObject.GetComponent<SMBBlockBreakable>().Agent = "Brick";
                discoveredObject.GetComponent<SMBBlockBreakable>().Action = "Jump";
                discoveredObject.GetComponent<SMBBlockBreakable>().Interaction = "Break";
                beliefStack.Push(discoveredObject.GetComponent<SMBBlockBreakable>().beliefArray);
                discoveredObject.GetComponent<SMBBlockBreakable>().updateBeliefArray();
                printArray(discoveredObject.GetComponent<SMBBlockBreakable>().beliefArray);
                discoveredObject.gameObject.GetComponent<SMBBlockBreakable>().printed = true;
            }
        }


    }

    //When Mario's x position is greater than the object, and 
    //when the object leaves the collider, interaction is no effect
    //***do we want to turn off their detected booleans in case
    //Mario goes back and fights them?
    private void OnTriggerExit2D(Collider2D collision)
    {

        if (isAlive == false) return;

        GameObject discoveredObject = collision.gameObject;

        //This if else is for when Mario ignores things
        if (discoveredObject.transform.position.x < transform.position.x)
        {
            if (discoveredObject.name == "g")
            {
                if (discoveredObject.gameObject.GetComponent<SMBEnemy>().isDead == false)
                {
                    if (discoveredObject.gameObject.GetComponent<SMBEnemy>().enteredFromRight == true)
                    {
                        discoveredObject.GetComponent<SMBEnemy>().Interaction = "No Effect";
                        discoveredObject.GetComponent<SMBEnemy>().Action = "Walk Right";
                        discoveredObject.GetComponent<SMBEnemy>().isDetected = false;
                        beliefStack.Push(discoveredObject.GetComponent<SMBEnemy>().beliefArray);
                        Debug.Log("walk right function");
                        discoveredObject.GetComponent<SMBEnemy>().updateBeliefArray();
                        printArray(discoveredObject.GetComponent<SMBEnemy>().beliefArray);
                        discoveredObject.GetComponent<SMBEnemy>().Reset();

                    }
                }
            } else if (discoveredObject.name == "e")
            {
                if (discoveredObject.gameObject.GetComponent<SMBBlockBreakable>().isDead == false)
                {
                    if (discoveredObject.gameObject.GetComponent<SMBBlockBreakable>().enteredFromRight == true)
                    {
                        discoveredObject.GetComponent<SMBBlockBreakable>().Interaction = "No Effect";
                        discoveredObject.GetComponent<SMBBlockBreakable>().Action = "Walk Right";
                        discoveredObject.GetComponent<SMBBlockBreakable>().isDetected = false;
                        beliefStack.Push(discoveredObject.GetComponent<SMBBlockBreakable>().beliefArray);
                        Debug.Log("walk right function");
                        discoveredObject.GetComponent<SMBBlockBreakable>().updateBeliefArray();
                        printArray(discoveredObject.GetComponent<SMBBlockBreakable>().beliefArray);
                        discoveredObject.GetComponent<SMBBlockBreakable>().Reset();

                    }
                }
            }

        }
        else if (discoveredObject.transform.position.x > transform.position.x)
        {
            if (discoveredObject.name == "g")
            {
                if (discoveredObject.gameObject.GetComponent<SMBEnemy>().isDead == false)
                {
                    if (discoveredObject.gameObject.GetComponent<SMBEnemy>().enteredFromRight == false)
                    {
                        discoveredObject.GetComponent<SMBEnemy>().Interaction = "No Effect";
                        discoveredObject.GetComponent<SMBEnemy>().Action = "Walk Left";
                        discoveredObject.GetComponent<SMBEnemy>().isDetected = false;
                        beliefStack.Push(discoveredObject.GetComponent<SMBEnemy>().beliefArray);
                        Debug.Log("walk left function");
                        discoveredObject.GetComponent<SMBEnemy>().updateBeliefArray();
                        printArray(discoveredObject.GetComponent<SMBEnemy>().beliefArray);
                        discoveredObject.GetComponent<SMBEnemy>().Reset();
                    }
                }
            } else if (discoveredObject.name == "e")
            {
                if (discoveredObject.gameObject.GetComponent<SMBEnemy>().enteredFromRight == false)
                {
                    discoveredObject.GetComponent<SMBEnemy>().Interaction = "No Effect";
                    discoveredObject.GetComponent<SMBEnemy>().Action = "Walk Left";
                    discoveredObject.GetComponent<SMBEnemy>().isDetected = false;
                    beliefStack.Push(discoveredObject.GetComponent<SMBEnemy>().beliefArray);
                    Debug.Log("walk left function");
                    discoveredObject.GetComponent<SMBEnemy>().updateBeliefArray();
                    printArray(discoveredObject.GetComponent<SMBEnemy>().beliefArray);
                    discoveredObject.GetComponent<SMBEnemy>().Reset();
                }
            }
        }   
    }



    void printArray(string[] beliefArray)
    {
        for (int i = 0; i < beliefArray.Length; i++)
        {
            Debug.Log(beliefArray[i].ToString());
        }

        string path = "Assets/Scripts/Utils/newCoordinates.txt";

        stream = new StreamWriter(path, true);

        stream.WriteLine("Agent: " + beliefArray[0] + " Action: " + beliefArray[1] + " Interaction: " + beliefArray[2]
            + " Result: " + beliefArray[3]);

       stream.Close();

        AssetDatabase.ImportAsset(path);

        //TextAsset textAsset = (TextAsset)Resources.Load("newCoordinates");


    }

    void mapPosition(float previousX, float previousY)
    {

        //writer.WriteLine("("+transform.position.x + ", " + transform.position.y + ")");
        

      
    }

    //defines speed. deals with accelleration if player has ran long enough
    //relies on runningTimer 
	float DefineMoveSpeed() {

		float speed = xSpeed;
		if (Input.GetKey (KeyCode.Z)) {

			speed *= runningMultiplyer;

			if (_isOnGround)
				_runningTimer += Time.fixedDeltaTime;

			_runningTimer = Mathf.Clamp (_runningTimer, 0f, runTime);

			if (_runningTimer >= runTime)
				speed *= runningMultiplyer * 0.625f;
		} 
		else if (Input.GetKeyUp (KeyCode.Z)) {

			_runningTimer = 0f;
		}

		return speed;
	}


    //deals heavily with mario's movement animation 
	void PlayMoveAnimation(float speed, float direction) {

		float xDirection = _body.velocity.x >= 0f ? 1f : -1f;
		float sDirection = speed * direction >= 0f ? 1f : -1f;

		if (_isCoasting && xDirection != sDirection)
			return;

		if (_isOnGround) {

			if(speed == 0) 
				_animator.Play ("Idle");

			else if (speed == xSpeed)
				_animator.Play ("Move");

			else if (speed == xSpeed * runningMultiplyer)
				_animator.Play ("MoveFaster");

			else
				_animator.Play ("Run");
		}

		_particleSystem._shootParticles = false;
	}

    //only works if player is on the ground
    //if the x velocity of Mario is greater than
    //the minimum value required to coast AND
    //if xDirection is opposite from the current direction
    //then set _isCoasting to true
	void Coast(SMBConstants.MoveDirection direction) {

		if (!_isOnGround)
			return;

		float xDirection = _body.velocity.x >= 0f ? 1f : -1f;

		if (Mathf.Abs (_body.velocity.x) > minVelocityToCoast && xDirection == -(float)direction) {

			_animator.Play ("Coasting");

			_isCoasting = true;
			_runningTimer = 0f;

			_particleSystem._shootParticles = true;
		}
	}


    //player is rendered invincible until mario blinks 40 times
    //this function turns off _renderer 
    //at the end of this function, _renderer is enabled and 
    //_isInvincible is set back to false
	void Blink() {

		_blinkTimer += Time.fixedDeltaTime;

		if (_blinkTimer >= blinkTime) {
			_renderer.enabled = !_renderer.enabled;
			_blinkTimer = 0f;
			_blinkAmount++;
		}

		if (_blinkAmount == 10)
			blinkTime *= 0.8f;

		else if (_blinkAmount == 20)
			blinkTime *= 0.8f;

		else if (_blinkAmount >= 40) {

			_blinkAmount = 0;
			_blinkTimer = 0f;

			_isInvincible = false;
			_renderer.enabled = true;
		}
	}

    //makes mario invincible but 
    //what does the layermask do????
	void SetInvincible(bool invincible) {

		int enemies = LayerMask.NameToLayer ("Enemy");

		if (invincible) {


            //Blink turns invincible false
            //this means that the collider on the player
            //is once again able to collide with things
			Blink ();


            //turns the collider attached to the player
            //into a trigger. this means the player can't be
            //collided with by anything. What about ground?
			_collider.SetIsTrigger (true);
			_collider.horizontalMask &= ~(1 << enemies);
		} 
		else {
			
			_collider.SetIsTrigger (false);
			_collider.horizontalMask |= (1 << enemies);
		}
			
	}

	void Die(float timeToDie, bool animate = true) {
        isAlive = false;


        Interaction = "Take Damage";
        beliefStack.Push(marioBeliefArray);


        if (_isInvincible)
			return;

		_state = SMBConstants.PlayerState.Dead;

		_lockController = true;

		_particleSystem._shootParticles = false;

		_collider.applyHorizCollision = false;
		_collider.applyVertCollision = false;

		gameObject.layer = LayerMask.NameToLayer ("Ignore Raycast");

		_body.velocity = Vector2.zero;
		_body.acceleration = Vector2.zero;
		_body.applyGravity = false;

		_animator.SetTrigger ("triggerDie");

		if(animate)
			Invoke("PlayDeadAnimation", timeToDie);
	}


    //turns gravity on for mario
    //last line makes him jump and then fall 
	void PlayDeadAnimation() {

        printArray(marioBeliefArray);
		_body.applyGravity = true;
		_body.gravityFactor = 0.5f;
		_body.ApplyForce (Vector2.up * 2.5f);
	}
				
	void Jump() {


        //if sprite is on the ground and x is pressed, jump
        //mario moves by changing the velocity y to ySpeed times time elapsed
        //NO IDEA HOW SOUND PLAYS
		if (_isOnGround && Input.GetKeyDown(KeyCode.X)){

			_jumpTimer = longJumpTime;
			_body.velocity.y = ySpeed * Time.fixedDeltaTime;

			_audio.PlayOneShot (soundEffects[(int)SoundEffects.Jump]);
		}


        //these deal with changing the strength of the jump
        //if the player presses the button longer(?) or if 
        //the player has a running start affect the behaviour 
        //of the jump
		if (_jumpTimer > 0f) {

            //resets jumpTimer
			if (Input.GetKeyUp(KeyCode.X)) {

				_jumpTimer = 0f;

			}
            //if you keep pressing x then it will be decremented by 
            //Time.fixedDeltaTime. This invokes the second if statement
            //underneath and the jump gets a multiplier, making Mario
            //jump higher
			else if(_body.velocity.y > 0f && Input.GetKey(KeyCode.X)) {

				float runningBoost = 1f;
                //if you've been running long enough, runningBoost increases
				if (_runningTimer >= runTime)
					runningBoost = 1.5f;

				_jumpTimer -= Time.fixedDeltaTime;
				if (_jumpTimer <= longJumpTime/2f)
					_body.velocity.y += ySpeed * longJumpWeight * runningBoost * Time.fixedDeltaTime;
			}
		}
	}


    //depending on the speed, Jump or FastJump is played
	void PlayJumpAnimation(float speed) {

		if (!_isOnGround) {

			if(speed == 0) 
				_animator.Play ("Jump");

			else if (speed == xSpeed)
				_animator.Play ("Jump");

			else if (speed == xSpeed * runningMultiplyer)
				_animator.Play ("Jump");

			else
				_animator.Play ("FastJump");
		}
	}

	void GrowUp() {

		if (_state == SMBConstants.PlayerState.GrownUp)
			return;	

		SMBGameWorld.Instance.PauseGame (false);

		_animator.SetTrigger("triggerGrownUp");
		_animator.SetLayerWeight (0, 0);
		_animator.SetLayerWeight (1, 1);

		_collider.SetSize (grownUpColliderSize);

		_velocityBeforeGrowUp = _body.velocity;

		_lockController = true;
		_body.applyGravity = false;
		_body.velocity = Vector2.zero;

		_audio.PlayOneShot (soundEffects[(int)SoundEffects.GrowUp]);

		_state = SMBConstants.PlayerState.GrownUp;
	}

	void TakeDamage() {

		SMBGameWorld.Instance.PauseGame (false);

		_animator.SetTrigger("triggerDamage");
		_animator.SetLayerWeight (0, 1);
		_animator.SetLayerWeight (1, 0);

		_collider.SetSize (_originalCollider);

		_lockController = true;
		_isInvincible = true;

		_body.applyGravity = false;
		_body.velocity = Vector2.zero;
		_velocityBeforeGrowUp = Vector2.zero;

		_audio.PlayOneShot (soundEffects[(int)SoundEffects.GrowUp]);

		_state = SMBConstants.PlayerState.Short;
	}

	void UnlockController() {

		_lockController = false;
		_body.applyGravity = true;
		_body.velocity = _velocityBeforeGrowUp;

		SMBGameWorld.Instance.ResumeGame();
	}

	void KillEnemy(GameObject enemy) {

		_body.acceleration = Vector2.zero;
		_body.velocity.y = 0f;

		_body.ApplyForce (Vector2.up * 2.5f);
		_audio.PlayOneShot (soundEffects[(int)SoundEffects.Kick]);

		enemy.SendMessage ("Die", SendMessageOptions.DontRequireReceiver);
	}
		
	override protected void OnVerticalCollisionEnter(Collider2D collider) {

		if (collider.tag == "Block") {

			if (collider.bounds.center.y > transform.position.y)
				collider.SendMessage ("OnInteraction", this, SendMessageOptions.DontRequireReceiver);
		}

		base.OnVerticalCollisionEnter (collider);
	}

	void OnVerticalTriggerEnter(Collider2D collider) {

		if (collider.tag == "Item") {

			collider.SendMessage ("OnInteraction", SendMessageOptions.DontRequireReceiver);

			if (collider.name == "r")
				GrowUp ();
		}
		else if (collider.tag == "Enemy") {

			KillEnemy (collider.gameObject);
		}
		else if (collider.tag == "End") {

			SMBGameWorld.Instance.ReloadLevel ();
		}
	}

	void OnHorizontalCollisionEnter(Collider2D collider) {

		_runningTimer = 0f;
	}

	void OnHorizontalTriggerEnter(Collider2D collider) {

		if (collider.tag == "Item") {

			collider.SendMessage ("OnInteraction", SendMessageOptions.DontRequireReceiver);

			if (collider.name == "r")
				GrowUp ();
		}
		else if (collider.tag == "Enemy") {

			if (transform.position.y > collider.transform.position.y + 0.1f) {

				KillEnemy (collider.gameObject);
				return;
			}
				
			if (_state == SMBConstants.PlayerState.GrownUp)

				TakeDamage ();
			else
				Die (0.2f);
		}
	}
}
