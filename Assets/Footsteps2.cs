using UnityEngine;
using FMODUnity;

/// <summary>
/// Zarz¹dza odtwarzaniem dŸwiêków kroków, skoków i l¹dowania w zale¿noœci od powierzchni.
/// </summary>
public class Footsteps2 : MonoBehaviour
{
    // FMOD - Instancje zdarzeñ.
    private FMOD.Studio.EventInstance footstepsSoundInstance;
    private FMOD.Studio.EventInstance runSoundInstance; // Dodano instancjê dla biegu
    private FMOD.Studio.EventInstance jumpSoundInstance;
    private FMOD.Studio.EventInstance landSoundInstance;

    // Publiczne referencje do zdarzeñ FMOD.
    public EventReference footstepsEvent;
    public EventReference runEvent; // Dodano miejsce na podpiêcie eventu biegu w Inspektorze
    public EventReference jumpEvent;
    public EventReference landEvent;

    private float lastFootstepTime = 0f;
    private float distToGround;

    [SerializeField]
    private bool isGrounded = true;
    [SerializeField]
    private bool isJumping = false;

    // Referencja do kontrolera, aby mierzyæ faktyczn¹ prêdkoœæ
    private CharacterController characterController;

    void Start()
    {
        distToGround = GetComponent<Collider>().bounds.extents.y;
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Sprawdza, czy gracz skacze, u¿ywaj¹c spacji.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlayJump();
        }
    }

    void FixedUpdate()
    {
        HandleFootsteps();
    }

    /// <summary>
    /// Obs³uguje logikê odtwarzania dŸwiêków kroków.
    /// </summary>
    private void HandleFootsteps()
    {
        float currentSpeed = 0f;

        // Obliczanie poziomej prêdkoœci gracza z CharacterController
        if (characterController != null)
        {
            Vector3 horizontalVelocity = new Vector3(characterController.velocity.x, 0, characterController.velocity.z);
            currentSpeed = horizontalVelocity.magnitude;
        }

        // Gracz siê porusza, jeœli jego prêdkoœæ jest wiêksza ni¿ minimalny margines b³êdu
        bool isMoving = currentSpeed > 0.1f;

        // Zgodnie z Twoim pomys³em - odpalamy bieg, gdy prêdkoœæ przekroczy 5
        bool isRunning = currentSpeed > 5f;

        if (isMoving && IsGrounded())
        {
            // Ustawia interwa³ na podstawie tego, czy gracz biegnie.
            float footstepInterval = isRunning ? 0.25f : 0.5f;

            if (Time.time - lastFootstepTime > footstepInterval)
            {
                lastFootstepTime = Time.time;
                PlayFootsteps(isRunning); // Przekazujemy informacjê o biegu dalej
            }
        }
    }

    /// <summary>
    /// Odtwarza dŸwiêk kroków w zale¿noœci od powierzchni i prêdkoœci.
    /// </summary>
    private void PlayFootsteps(bool isRunning)
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, distToGround + 0.5f))
        {
            string surfaceTag = hit.collider.tag;

            // Wybór odpowiedniego eventu na podstawie tego, czy biegniemy
            EventReference currentEvent = isRunning ? runEvent : footstepsEvent;
            FMOD.Studio.EventInstance currentInstance = isRunning ? runSoundInstance : footstepsSoundInstance;

            PlaySurfaceSound(currentInstance, currentEvent, surfaceTag);
        }
    }

    /// <summary>
    /// Odtwarza dŸwiêk skoku.
    /// </summary>
    private void PlayJump()
    {
        if (IsGrounded())
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, distToGround + 0.5f))
            {
                string surfaceTag = hit.collider.tag;
                PlaySurfaceSound(jumpSoundInstance, jumpEvent, surfaceTag);
            }
            isGrounded = false;
            isJumping = true;
        }
    }

    /// <summary>
    /// Obs³uguje dŸwiêk l¹dowania po skoku.
    /// </summary>
    private void OnCollisionEnter(Collision col)
    {
        if (!isGrounded && isJumping)
        {
            PlayLanding();
        }
    }

    /// <summary>
    /// Odtwarza dŸwiêk l¹dowania.
    /// </summary>
    private void PlayLanding()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, distToGround + 0.5f))
        {
            string surfaceTag = hit.collider.tag;
            PlaySurfaceSound(landSoundInstance, landEvent, surfaceTag);
        }
        isGrounded = true;
        isJumping = false;
    }

    /// <summary>
    /// Ogólna metoda do odtwarzania dŸwiêku na podstawie tagu powierzchni.
    /// </summary>
    private void PlaySurfaceSound(FMOD.Studio.EventInstance soundInstance, EventReference eventRef, string surfaceTag)
    {
        string surfaceParameter = null;

        switch (surfaceTag)
        {
            case "Stone":
            case "Inside_stone":
            case "Outside":
                surfaceParameter = "stone";
                break;

            case "Wood":
            case "Inside_wood":
                surfaceParameter = "wood";
                break;

            case "Stairs":
                surfaceParameter = "stairs";
                break;

            case "Lamp":
                surfaceParameter = "lamp";
                break;
        }

        if (surfaceParameter != null)
        {
            soundInstance = RuntimeManager.CreateInstance(eventRef);
            soundInstance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject.transform));
            soundInstance.setParameterByNameWithLabel("footsteps_parameter", surfaceParameter);
            soundInstance.start();
            soundInstance.release();
        }
    }

    /// <summary>
    /// Sprawdza, czy gracz znajduje siê na pod³o¿u.
    /// </summary>
    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, distToGround + 0.5f);
    }
}