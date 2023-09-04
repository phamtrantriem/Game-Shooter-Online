using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(AudioSource))]
public class PlayerController : MonoBehaviourPunCallbacks, IDamageable
{
#pragma warning disable 649
    [Header("Arms")]
    [Tooltip("The transform component that holds the gun camera."), SerializeField]
    private Transform arms;

    [Tooltip("The position of the arms and gun camera relative to the fps controller GameObject."), SerializeField]
    private Vector3 armPosition;

    [Header("Audio Clips")]
    [Tooltip("The audio clip that is played while walking."), SerializeField]
    private AudioClip walkingSound;

    [Tooltip("The audio clip that is played while running."), SerializeField]
    private AudioClip runningSound;

    [Header("Movement Settings")]
    [Tooltip("How fast the player moves while walking and strafing."), SerializeField]
    private float walkingSpeed = 5f;

    [Tooltip("How fast the player moves while running."), SerializeField]
    private float runningSpeed = 9f;

    [Tooltip("Approximately the amount of time it will take for the player to reach maximum running or walking speed."), SerializeField]
    private float movementSmoothness = 0.125f;

    [Tooltip("Amount of force applied to the player when jumping."), SerializeField]
    private float jumpForce = 3f;

    [Header("Look Settings")]
    [Tooltip("Rotation speed of the fps controller."), SerializeField]
    private float mouseSensitivity = 7f;

    [Tooltip("Approximately the amount of time it will take for the fps controller to reach maximum rotation speed."), SerializeField]
    private float rotationSmoothness = 0.05f;

    [Tooltip("Minimum rotation of the arms and camera on the x axis."),
     SerializeField]
    private float minVerticalAngle = -90f;

    [Tooltip("Maximum rotation of the arms and camera on the axis."),
     SerializeField]
    private float maxVerticalAngle = 90f;

    [Tooltip("The names of the axes and buttons for Unity's Input Manager."), SerializeField]
    private FpsInput input;
    /*=======================================================*/
    [Header("Camera")]
    public Camera mainCamera;
    [Header("Camera FOV Settings")]
    public float zoomedFOV;
    public float defaultFOV;
    [Tooltip("How fast the camera zooms in")]
    public float fovSpeed;

    [SerializeField] private Animator anim;

    [Header("Weapon Settings")]
    public bool semi;
    public bool auto;

    [SerializeField] GameObject targetObject;
    //Used for fire rate
    private float lastFired;
    //How fast the weapon fires, higher value means faster rate of fire
    [Tooltip("How fast the weapon fires, higher value means faster rate of fire.")]
    public float fireRate;

    [Header("Weapon Components")]
    public ParticleSystem muzzleflashParticles;
    public Light muzzleflashLight;

    [Header("Prefabs")]
    public Transform casingPrefab;
    public Transform bulletPrefab;
    public float bulletForce = 100f;
    /*    public Transform grenadePrefab;
        public float grenadeSpawnDelay;*/

    [Header("Spawnpoints")]
    public Transform casingSpawnpoint;
    public Transform bulletSpawnpoint;
    /*    public Transform grenadeSpawnpoint;*/

    [Header("Audio Clips")]
    public AudioClip shootSound;

    [Header("Audio Sources")]
    public AudioSource shootAudioSource;
#pragma warning restore 649

    private Rigidbody _rigidbody;
    private CapsuleCollider _collider;
    private AudioSource _audioSource;
    private SmoothRotation _rotationX;
    private SmoothRotation _rotationY;
    private SmoothVelocity _velocityX;
    private SmoothVelocity _velocityZ;
    private bool _isGrounded;

    private readonly RaycastHit[] _groundCastResults = new RaycastHit[8];
    private readonly RaycastHit[] _wallCastResults = new RaycastHit[8];


    /*=================================*/

    // UI

    [SerializeField] TMP_Text _ammoText;
    [SerializeField] TMP_Text _playerNameText;
    //[SerializeField] GameObject cameraHolder;
    //[SerializeField] float mouseSensitivity, sprintSpeed, walkSpeed, jumpForce, smoothTime;
    [SerializeField] Item[] items;
    [SerializeField] Image healthbarImage;
    [SerializeField] GameObject healthbarUI;

    [SerializeField] GameObject meshObject;
    PlayerManager playerManager;

    int itemIndex;
    int previousItemIndex = -1;
    int currentAmmo;
    public int allAmmo = 31;
    private bool outOfAmmo = false;
    Vector3 moveAmount;


    PhotonView PV;

    const float maxHealth = 100f;
    float currentHealth = maxHealth;
    /*====================================*/
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
        _collider = GetComponent<CapsuleCollider>();
        //_audioSource = GetComponent<AudioSource>();
        arms = AssignCharactersCamera();
        //_audioSource.clip = walkingSound;
        //_audioSource.loop = true;
        _rotationX = new SmoothRotation(RotationXRaw);
        _rotationY = new SmoothRotation(RotationYRaw);
        _velocityX = new SmoothVelocity();
        _velocityZ = new SmoothVelocity();
        Cursor.lockState = CursorLockMode.Locked;
        ValidateRotationRestriction();


        //Assign animator component
        //anim = meshObject.GetComponent<Animator>();
        //Disable muzzleflash light at start
        muzzleflashLight.enabled = false;


        PV = GetComponent<PhotonView>();
        // assign playermanager variable // reference to playermanager
        // find give the game object with id
        playerManager = PhotonView.Find((int)PV.InstantiationData[0]).GetComponent<PlayerManager>();
    }

    private Transform AssignCharactersCamera()
    {
        var t = transform;
        arms.SetPositionAndRotation(t.position, t.rotation);
        return arms;
    }

    /// Clamps <see cref="minVerticalAngle"/> and <see cref="maxVerticalAngle"/> to valid values and
    /// ensures that <see cref="minVerticalAngle"/> is less than <see cref="maxVerticalAngle"/>.
    private void ValidateRotationRestriction()
    {
        minVerticalAngle = ClampRotationRestriction(minVerticalAngle, -90, 90);
        maxVerticalAngle = ClampRotationRestriction(maxVerticalAngle, -90, 90);
        if (maxVerticalAngle >= minVerticalAngle) return;
        Debug.LogWarning("maxVerticalAngle should be greater than minVerticalAngle.");
        var min = minVerticalAngle;
        minVerticalAngle = maxVerticalAngle;
        maxVerticalAngle = min;
    }

    private static float ClampRotationRestriction(float rotationRestriction, float min, float max)
    {
        if (rotationRestriction >= min && rotationRestriction <= max) return rotationRestriction;
        var message = string.Format("Rotation restrictions should be between {0} and {1} degrees.", min, max);
        Debug.LogWarning(message);
        return Mathf.Clamp(rotationRestriction, min, max);
    }


    private void Start()
    {
        // check if is mine them equip the fisrt fun in items
        if (PV.IsMine)
        {
            Debug.Log("Equip gun 0");
            EquipItem(0);
        }
        else
        // if not is mine, destroy camera view for signle one and rigidbody
        {
            Destroy(GetComponentInChildren<Camera>().gameObject);
            Destroy(_rigidbody);
            Destroy(healthbarUI);
            /*for (var i = gameObject.transform.childCount - 1; i >= 0; i--)
            {
                // only destroy tagged object
                if (gameObject.transform.GetChild(i).gameObject.name == "Event System")
                    Destroy(gameObject.transform.GetChild(i).gameObject);
            }*/
        }

        // set name above player
        _playerNameText.text = PV.Owner.NickName;
        currentAmmo = allAmmo;
    }


    private void Update()
    {
        //check if Pv not is mine then not control
        if (!PV.IsMine) { return; }

        if (Input.GetKeyDown(KeyCode.B))
        {
            semi = auto;
            auto = !auto;
        }

        if (currentAmmo == 0)
        {
            _ammoText.text = "Out of Ammo";
            //Toggle bool
            outOfAmmo = true;
        }
        else
        {
            _ammoText.text = currentAmmo.ToString() + "/" + allAmmo.ToString();
            //Toggle bool
            outOfAmmo = false;
        }
        // Moves the camera to the character, processes jumping and plays sounds every frame.
        arms.position = transform.position + transform.TransformVector(armPosition);
        Jump();
        //PlayFootstepSounds();

        //push number to change weapon
        for (int i = 0; i < items.Length; i++)
        {
            if (Input.GetKeyDown((i + 1).ToString()))
            {
                EquipItem(i);
                break;
            }
        }

        //scroll to change weapon
        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
        {
            if (itemIndex >= items.Length - 1)
            {
                EquipItem(items.Length - 1);
            }
            else
            {
                EquipItem(itemIndex + 1);
            }

        }
        else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
        {
            if (itemIndex < 1)
            {
                EquipItem(0);
            }
            else
            {
                EquipItem(itemIndex - 1);
            }
        }

        // Animation 
        if (!Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.D) && !Input.GetKey(KeyCode.W))
        {
            anim.SetFloat("Vertical", 0.0f, 0, Time.deltaTime);
            anim.SetFloat("Horizontal", 0.0f, 0, Time.deltaTime);
        }
        //Run forward
        if (Input.GetKeyDown(KeyCode.W))
        {
            anim.SetFloat("Vertical", 1.0f);
            anim.SetFloat("Horizontal", 0.0f);
            Debug.Log("WWWW " + anim.GetFloat("Vertical"));
        }
        //Run 45 up right
        if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.D))
        {
            anim.SetFloat("Vertical", 1.0f, 0, Time.deltaTime);
            anim.SetFloat("Horizontal", 1.0f, 0, Time.deltaTime);
        }
        //Run strafe right
        if (Input.GetKey(KeyCode.D))
        {
            anim.SetFloat("Vertical", 0.0f, 0, Time.deltaTime);
            anim.SetFloat("Horizontal", 1.0f, 0, Time.deltaTime);
        }
        //Run 45 back right
        if (Input.GetKey(KeyCode.D) && Input.GetKey(KeyCode.S))
        {
            anim.SetFloat("Vertical", -1.0f, 0, Time.deltaTime);
            anim.SetFloat("Horizontal", 1.0f, 0, Time.deltaTime);
        }
        //Run backwards
        if (Input.GetKey(KeyCode.S))
        {
            anim.SetFloat("Vertical", -1.0f, 0, Time.deltaTime);
            anim.SetFloat("Horizontal", 0.0f, 0, Time.deltaTime);
        }
        //Run 45 back left
        if (Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.A))
        {
            anim.SetFloat("Vertical", -1.0f, 0, Time.deltaTime);
            anim.SetFloat("Horizontal", -1.0f, 0, Time.deltaTime);
        }
        //Run strafe left
        if (Input.GetKey(KeyCode.A))
        {
            anim.SetFloat("Vertical", 0.0f, 0, Time.deltaTime);
            anim.SetFloat("Horizontal", -1.0f, 0, Time.deltaTime);
        }
        //Run 45 up left
        if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.A))
        {
            anim.SetFloat("Vertical", 1.0f, 0, Time.deltaTime);
            anim.SetFloat("Horizontal", -1.0f, 0, Time.deltaTime);
        }


        //Aim in with right click hold
        if (Input.GetMouseButton(1))
        {
            //Increase camera field of view
            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView,
                zoomedFOV, fovSpeed * Time.deltaTime);
        }
        else
        {
            //Restore camera field of view
            mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView,
                defaultFOV, fovSpeed * Time.deltaTime);
        }

        //Single fire with left click
        if (Input.GetMouseButtonDown(0) && outOfAmmo == false)
        {
            items[itemIndex].Use();

            currentAmmo -= 1;

            //Play shoot sound 
            shootAudioSource.clip = shootSound;
            shootAudioSource.Play();

            //Play from second layer, from the beginning
            anim.Play("Fire", 1, 0.0f);
            //Play muzzleflash particles
            muzzleflashParticles.Emit(1);
            //Play light flash
            StartCoroutine(MuzzleflashLight());

            //Spawn casing at spawnpoint
            Instantiate(casingPrefab,
                casingSpawnpoint.transform.position,
                casingSpawnpoint.transform.rotation);

            //Spawn bullet from bullet spawnpoint



            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            ray.origin = mainCamera.transform.position;
        }

        //AUtomatic fire
        //Left click hold 
        /*        if (Input.GetMouseButton(0) && outOfAmmo == false && auto == true)
                {
                    //Shoot automatic
                    if (Time.time - lastFired > 1 / fireRate)
                    {
                        lastFired = Time.time;

                        currentAmmo -= 1;

                        //Play shoot sound
                        shootAudioSource.clip = shootSound;
                        shootAudioSource.Play();

                        //Play from second layer, from the beginning
                        anim.Play("Fire", 1, 0.0f);
                        //Play muzzleflash particles
                        muzzleflashParticles.Emit(1);
                        //Play light flash
                        StartCoroutine(MuzzleflashLight());

                        //Spawn casing at spawnpoint
                        Instantiate(casingPrefab,
                            casingSpawnpoint.transform.position,
                            casingSpawnpoint.transform.rotation);

                        //Spawn bullet from bullet spawnpoint
                        var bullet = (Transform)Instantiate(
                            bulletPrefab,
                            bulletSpawnpoint.transform.position,
                            bulletSpawnpoint.transform.rotation);

                        //Add velocity to the bullet
                        bullet.GetComponent<Rigidbody>().velocity =
                            bullet.transform.forward * bulletForce;
                    }
                }
        */
        //Reload with R key for testing
        if (Input.GetKeyDown(KeyCode.R))
        {
            //Play reload animation
            //anim.Play("Reload", 1, 0.0f);
            Reload();
        }

        /*        //Throw grenade when pressing G key
                if (Input.GetKeyDown(KeyCode.G))
                {
                    StartCoroutine(GrenadeSpawnDelay());
                    //Play grenade throw animation
                    anim.Play("Grenade_Throw", 1, 0.0f);
                }*/

    }

    //Reload
    private void Reload()
    {
        //automaticfunscriptlpfp

        currentAmmo = allAmmo;
        outOfAmmo = false;
    }

    IEnumerator MuzzleflashLight()
    {
        muzzleflashLight.enabled = true;
        yield return new WaitForSeconds(0.02f);
        muzzleflashLight.enabled = false;
    }

    /// Processes the character movement and the camera rotation every fixed framerate frame.
    private void FixedUpdate()
    {
        // update is  called every frame while fixed update runs on fixed interval
        // so that movement speed isnt impacted by out fps

        // FixedUpdate is used instead of Update because this code is dealing with physics and smoothing.
        if (!PV.IsMine)
        {
            return;
        }
        //_rigidbody.MovePosition(_rigidbody.position + transform.TransformDirection(moveAmount) * Time.fixedDeltaTime);

        RotateCameraAndCharacter();
        MoveCharacter();
        _isGrounded = false;
    }

    private void PlayFootstepSounds()
    {
        if (_isGrounded && _rigidbody.velocity.sqrMagnitude > 0.1f)
        {
            _audioSource.clip = input.Run ? runningSound : walkingSound;
            if (!_audioSource.isPlaying)
            {
                _audioSource.Play();
            }
        }
        else
        {
            if (_audioSource.isPlaying)
            {
                _audioSource.Pause();
            }
        }
    }
    private void RotateCameraAndCharacter()
    {
        var rotationX = _rotationX.Update(RotationXRaw, rotationSmoothness);
        var rotationY = _rotationY.Update(RotationYRaw, rotationSmoothness);

        var moveLength = new Vector3(0, Input.GetAxis("Mouse Y") / 1.5f, 0);
        targetObject.transform.position += moveLength;

        if (targetObject.transform.position.y > 3) targetObject.transform.position = new Vector3(targetObject.transform.position.x, 3f, targetObject.transform.position.z);
        if (targetObject.transform.position.y < -3) targetObject.transform.position = new Vector3(targetObject.transform.position.x, -3f, targetObject.transform.position.z);

        var clampedY = RestrictVerticalRotation(rotationY);
        _rotationY.Current = clampedY;
        var worldUp = arms.InverseTransformDirection(Vector3.up);
        var rotation = arms.rotation *
                       Quaternion.AngleAxis(rotationX, worldUp) *
                       Quaternion.AngleAxis(clampedY, Vector3.left);
        transform.eulerAngles = new Vector3(0f, rotation.eulerAngles.y, 0f);
        arms.rotation = rotation;
    }

    /// Clamps the rotation of the camera around the x axis
    /// between the <see cref="minVerticalAngle"/> and <see cref="maxVerticalAngle"/> values.
    private float RestrictVerticalRotation(float mouseY)
    {
        var currentAngle = NormalizeAngle(arms.eulerAngles.x);
        var minY = minVerticalAngle + currentAngle;
        var maxY = maxVerticalAngle + currentAngle;
        return Mathf.Clamp(mouseY, minY + 0.01f, maxY - 0.01f);
    }

    /// Normalize an angle between -180 and 180 degrees.
    /// <param name="angleDegrees">angle to normalize</param>
    /// <returns>normalized angle</returns>
    private static float NormalizeAngle(float angleDegrees)
    {
        while (angleDegrees > 180f)
        {
            angleDegrees -= 360f;
        }

        while (angleDegrees <= -180f)
        {
            angleDegrees += 360f;
        }

        return angleDegrees;
    }

    private void MoveCharacter()
    {
        var direction = new Vector3(input.Move, 0f, input.Strafe).normalized;
        var worldDirection = transform.TransformDirection(direction);
        var velocity = worldDirection * (input.Run ? runningSpeed : walkingSpeed);
        //Checks for collisions so that the character does not stuck when jumping against walls.
        var intersectsWall = CheckCollisionsWithWalls(velocity);
        if (intersectsWall)
        {
            _velocityX.Current = _velocityZ.Current = 0f;
            return;
        }

        var smoothX = _velocityX.Update(velocity.x, movementSmoothness);
        var smoothZ = _velocityZ.Update(velocity.z, movementSmoothness);
        var rigidbodyVelocity = _rigidbody.velocity;
        var force = new Vector3(smoothX - rigidbodyVelocity.x, 0f, smoothZ - rigidbodyVelocity.z);
        moveAmount = force;
        _rigidbody.AddForce(force, ForceMode.VelocityChange);
    }

    private bool CheckCollisionsWithWalls(Vector3 velocity)
    {
        if (_isGrounded) return false;
        var bounds = _collider.bounds;
        var radius = _collider.radius;
        var halfHeight = _collider.height * 0.5f - radius * 1.0f;
        var point1 = bounds.center;
        point1.y += halfHeight;
        var point2 = bounds.center;
        point2.y -= halfHeight;
        Physics.CapsuleCastNonAlloc(point1, point2, radius, velocity.normalized, _wallCastResults,
            radius * 0.04f, ~0, QueryTriggerInteraction.Ignore);
        var collides = _wallCastResults.Any(hit => hit.collider != null && hit.collider != _collider);
        if (!collides) return false;
        for (var i = 0; i < _wallCastResults.Length; i++)
        {
            _wallCastResults[i] = new RaycastHit();
        }

        return true;
    }

    private void OnCollisionStay()
    {
        var bounds = _collider.bounds;
        var extents = bounds.extents;
        var radius = extents.x - 0.01f;
        Physics.SphereCastNonAlloc(bounds.center, radius, Vector3.down,
            _groundCastResults, extents.y - radius * 0.5f, ~0, QueryTriggerInteraction.Ignore);
        if (!_groundCastResults.Any(hit => hit.collider != null && hit.collider != _collider)) return;
        for (var i = 0; i < _groundCastResults.Length; i++)
        {
            _groundCastResults[i] = new RaycastHit();
        }

        _isGrounded = true;
    }

    /*// camera view
    void Look()
    {
        transform.Rotate(Vector3.up * Input.GetAxisRaw("Mouse X") * mouseSensitivity);
        verticalLookRotation += Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);

        cameraHolder.transform.localEulerAngles = Vector3.left * verticalLookRotation;
    }
    
    // move action
    void Move()
    {
        Vector3 moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
        moveAmount = Vector3.SmoothDamp(moveAmount, moveDir * (Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed), ref smoothMoveVelocity, smoothTime);
    }



    //set ground state attribute of player 
    */

    // jump action
    private void Jump()
    {
        if (!_isGrounded || !input.Jump) return;
        _isGrounded = false;
        _rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    /// Returns the target rotation of the camera around the y axis with no smoothing.
    private float RotationXRaw
    {
        get { return input.RotateX * mouseSensitivity; }
    }

    /// Returns the target rotation of the camera around the x axis with no smoothing.
    private float RotationYRaw
    {
        get { return input.RotateY * mouseSensitivity; }
    }




    // equip item
    void EquipItem(int _index)
    {
        if (_index == previousItemIndex)
        {
            return;
        }

        itemIndex = _index;

        items[itemIndex].itemGameObject.SetActive(true);
        Debug.Log("inside equip gun: " + _index);
        if (previousItemIndex != -1)
        {
            items[previousItemIndex].itemGameObject.SetActive(false);
        }

        previousItemIndex = itemIndex;

        // push properties to network for syncing
        if (PV.IsMine)
        {
            Hashtable hash = new Hashtable();
            hash.Add("itemIndex", itemIndex);
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        }
    }


    // sync weapon changing show from other player
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey("itemIndex") && !PV.IsMine && targetPlayer == PV.Owner)
        {
            EquipItem((int)changedProps["itemIndex"]);
        }
    }



    // That run on shooter's comupter
    public void TakeDamage(float damage)
    {
        PV.RPC(nameof(RPC_TakeDamage), PV.Owner, damage);
    }

    /// <summary>
    /// rpc is protocol that what call a request called client and the one answer request called host
    /// client or host can be each other device in the same network system or process in the same system
    /// </summary>
    [PunRPC] // declare as a rpc
    // Run on everyone's computer, but the !PV.isMine check makes it only run on the victim's computer
    void RPC_TakeDamage(float damage, PhotonMessageInfo info)
    {
        currentHealth -= damage;

        healthbarImage.fillAmount = currentHealth / maxHealth;

        if (currentHealth <= 0)
        {
            Die();
            PlayerManager.Find(info.Sender).GetKill();
        }
    }

    void Die()
    {
        playerManager.Die();
    }


    public void LeaveRoom()
    {
        Time.timeScale = 1;
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.LoadLevel(0);
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    /// Input mappings
    [Serializable]
    private class FpsInput
    {
        [Tooltip("The name of the virtual axis mapped to rotate the camera around the y axis."),
         SerializeField]
        private string rotateX = "Mouse X";

        [Tooltip("The name of the virtual axis mapped to rotate the camera around the x axis."),
         SerializeField]
        private string rotateY = "Mouse Y";

        [Tooltip("The name of the virtual axis mapped to move the character back and forth."),
         SerializeField]
        private string move = "Horizontal";

        [Tooltip("The name of the virtual axis mapped to move the character left and right."),
         SerializeField]
        private string strafe = "Vertical";

        [Tooltip("The name of the virtual button mapped to run."),
         SerializeField]
        private string run = "Fire3";

        [Tooltip("The name of the virtual button mapped to jump."),
         SerializeField]
        private string jump = "Jump";

        /// Returns the value of the virtual axis mapped to rotate the camera around the y axis.
        public float RotateX
        {
            get { return Input.GetAxisRaw(rotateX); }
        }

        /// Returns the value of the virtual axis mapped to rotate the camera around the x axis.        
        public float RotateY
        {
            get { return Input.GetAxisRaw(rotateY); }
        }

        /// Returns the value of the virtual axis mapped to move the character back and forth.        
        public float Move
        {
            get { return Input.GetAxisRaw(move); }
        }

        /// Returns the value of the virtual axis mapped to move the character left and right.         
        public float Strafe
        {
            get { return Input.GetAxisRaw(strafe); }
        }

        /// Returns true while the virtual button mapped to run is held down.          
        public bool Run
        {
            get { return Input.GetButton(run); }
        }

        /// Returns true during the frame the user pressed down the virtual button mapped to jump.          
        public bool Jump
        {
            get { return Input.GetButtonDown(jump); }
        }

        public float MouseX
        {
            get { return Input.GetAxis("Mouse X"); }
        }

        public float MouseY
        {
            get { return Input.GetAxis("Mouse Y"); }
        }
    }


    /// A helper for assistance with smoothing the camera rotation.
    private class SmoothRotation
    {
        private float _current;
        private float _currentVelocity;

        public SmoothRotation(float startAngle)
        {
            _current = startAngle;
        }

        /// Returns the smoothed rotation.
        public float Update(float target, float smoothTime)
        {
            return _current = Mathf.SmoothDampAngle(_current, target, ref _currentVelocity, smoothTime);
        }

        public float Current
        {
            set { _current = value; }
        }
    }

    /// A helper for assistance with smoothing the movement.
    private class SmoothVelocity
    {
        private float _current;
        private float _currentVelocity;

        /// Returns the smoothed velocity.
        public float Update(float target, float smoothTime)
        {
            return _current = Mathf.SmoothDamp(_current, target, ref _currentVelocity, smoothTime);
        }

        public float Current
        {
            set { _current = value; }
        }
    }
}
