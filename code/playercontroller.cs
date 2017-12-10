using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Prime31;

public class playercontroller: MonoBehaviour {

    //what the collision state was in the last frame
    public CharacterController2D.CharacterCollisionState2D flags;

    //player control parameters
    public float walkSpeed = 6.0f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f; //how quickly character falls to the floor
    public float doubleJumpSpeed = 4.0f;
    public float wallJumpXAmount = 1.5f;
    public float wallJumpYAmount = 1.5f;
    public float wallRunAmount = 2f;
    public float slopeSlideSpeed = 4f;
    public float glideAmount = 2f;
    public float glideTimer = 2f;

    //player ability toggles
    public bool canDoubleJump = true;
    public bool canWallJump = true;
    public bool canWallRun = true;
    public bool canRunAfterWallJump = true;
    public bool canGlide = true;

    //player state variable
    public bool isGrounded;
    public bool isJumping;
    public bool facingRight;
    public bool doubleJumped;
    public bool wallJumped;
    public bool isWallRunning;
    public bool isSlopeSliding;
    public bool isGliding;

    public LayerMask layerMask;

    // private variable
    private Vector3 _moveDirection = Vector3.zero;
    private CharacterController2D _characterController;
    private bool _lastJumpWasLeft;
    private float _slopeAngle; //how steep the slope is
    private Vector3 _slopeGradient = Vector3.zero; //which direction is the slope pointing
    private bool _startGlide;
    private float _currentGlideTimer;
    private Animator _anim;


    void Start() {
        _characterController = GetComponent<CharacterController2D>();
        facingRight = true;
        _currentGlideTimer = glideTimer;
        _anim = GetComponent<Animator>();
    }

    void Update()  {

        if (!wallJumped)
        {
            _moveDirection.x = Input.GetAxis("Horizontal");
            _moveDirection.x *= walkSpeed;
        }

        RaycastHit2D hit = Physics2D.Raycast(transform.position, -Vector3.up, 1f, layerMask);

        if (hit)
        {
            _slopeAngle = Vector2.Angle(hit.normal, Vector2.up); //normal vector vs upward vector
            _slopeGradient = hit.normal;

            if (_slopeAngle > _characterController.slopeLimit)
            {
                isSlopeSliding = true;
            }
            else
            {
                isSlopeSliding = false;
            }
        }

        if (isGrounded){ //while player is on the ground
            _currentGlideTimer = glideTimer;
            _moveDirection.y = 0;
            isJumping = false;
            doubleJumped = false;
            if(_moveDirection.x < 0){ //players moving left
                transform.eulerAngles = new Vector3 (0,180,0); //rotate players on the y axis
                facingRight = false;
                _anim.SetInteger("State", 1);

            } else if (_moveDirection.x > 0){
                transform.eulerAngles = new Vector3 (0,0,0);
                facingRight = true;
                _anim.SetInteger("State", 1);
            }
            else
            {
                _anim.SetInteger("State", 0);
            }

            if (isSlopeSliding)
            {
                _moveDirection = new Vector3(_slopeGradient.x * slopeSlideSpeed, -_slopeGradient.y * slopeSlideSpeed, 0f);
            }
            
            if (Input.GetButtonDown("Jump")){
                _moveDirection.y = jumpSpeed;
                isJumping = true;
                isWallRunning = true;
                _anim.SetInteger("State", 2);
            }
        }
        else{ //while player is in the air
            if (Input.GetButtonUp("Jump")){
                if (_moveDirection.y > 0){
                    _moveDirection.y = _moveDirection.y * 0.5f;
                    _anim.SetInteger("State", 3);
                }
            }

            if (Input.GetButtonDown("Jump"))
            {
                if (canDoubleJump)
                    {
                        if (!doubleJumped)
                        {
                            _anim.SetInteger("State", 2);
                            _moveDirection.y = doubleJumpSpeed;
                            doubleJumped = true;
                        }
                    }
            }
        }

        //Handle Gliding
        if (canGlide && Input.GetAxis("Vertical") > 0.5f && _characterController.velocity.y < 0.2f && !isGrounded)
        {
            if (_currentGlideTimer > 0)
            {
                isGliding = true;
                if (_startGlide)
                {
                    _moveDirection.y = 0;
                    _startGlide = false;
                }
                _moveDirection.y -= glideAmount * Time.deltaTime;
                _currentGlideTimer -= Time.deltaTime;
                _anim.SetInteger("State", 3);
            }
            else
            {
                isGliding = false;
                _moveDirection.y -= gravity * Time.deltaTime;
                _anim.SetInteger("State", 3);
            }
        }
        else
        {
            isGliding = false;
            _startGlide = true;
            _moveDirection.y -= gravity * Time.deltaTime;
        }

        _characterController.move(_moveDirection * Time.deltaTime);
        // to make sure that it is frame rate independent;

        flags = _characterController.collisionState;
        //get collisions informations

        isGrounded = flags.below;

        if (flags.above){
            _moveDirection.y -= gravity * Time.deltaTime;
        }

        if (flags.left || flags.right)
        {
            if (canWallRun)
            {
                if (Input.GetAxis("Vertical") > 0 && isWallRunning)
                {
                    _moveDirection.y = jumpSpeed / wallRunAmount;
                    StartCoroutine(wallRunWaiter());
                }
            }

            if (canWallJump)
            {
                if (Input.GetButtonDown("Jump") && !wallJumped && !isGrounded)
                {
                    //perform wall jump
                    if (_moveDirection.x < 0)
                    {
                        //moving to the left
                        _moveDirection.x = jumpSpeed * wallJumpXAmount;
                        _moveDirection.y = jumpSpeed * wallJumpYAmount;
                        transform.eulerAngles = new Vector3(0, 0, 0);
                        _lastJumpWasLeft = false;
                    } else if (_moveDirection.x > 0)
                    {
                        //moving to the right
                        _moveDirection.x = -jumpSpeed * wallJumpXAmount;
                        _moveDirection.y = jumpSpeed * wallJumpYAmount;
                        transform.eulerAngles = new Vector3(0, 180, 0);
                        _lastJumpWasLeft = true;
                    }

                    StartCoroutine(wallJumpWaiter());
                }
            }
        }
        else
        {
            if (canRunAfterWallJump)
            {
                StopCoroutine(wallRunWaiter());
                isWallRunning = true;
            }
        }

    }

    IEnumerator wallJumpWaiter()
    {
        wallJumped = true; //just performed a wall jumped
        yield return new WaitForSeconds(2f);
        wallJumped = false;
    }

    IEnumerator wallRunWaiter()
    {
        isWallRunning = true;
        yield return new WaitForSeconds(2f);
        isWallRunning = false;
    }


}
