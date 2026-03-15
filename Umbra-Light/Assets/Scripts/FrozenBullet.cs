using UnityEngine;

public class FrozenBullet : MonoBehaviour
{
    public float bulletSpeed = 35f;
    public bool isEnemyBullet = false;

    private Rigidbody rb;
    private Vector3 travelDirection;
    private bool isFrozen = false;
    private bool hasHit = false;

    private PlayerShooting shooter = null;

    private float traveledTime = 0f;
    public float maxTravelTime = 6f;

    private Vector3 previousPosition;

    // Trail reference stored so we dont
    // call GetComponent every frame
    private TrailRenderer trail;

    // ─────────────────────────────────────────
    // CALLED BY PLAYERSHOOTING ON SPAWN
    // ─────────────────────────────────────────

    public void SetShooter(PlayerShooting ps)
    {
        shooter = ps;
    }

    // ─────────────────────────────────────────
    // SETUP
    // ─────────────────────────────────────────

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        travelDirection = transform.forward;
        rb.linearVelocity = travelDirection * bulletSpeed;

        previousPosition = transform.position;

        SetupTrail();
    }

    // ─────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────

    void Update()
    {
        if (hasHit) return;

        // Travel timer — only counts while moving
        if (!isFrozen)
        {
            traveledTime += Time.deltaTime;
            if (traveledTime >= maxTravelTime)
            {
                hasHit = true;
                NotifyShooter();
                Destroy(gameObject);
                return;
            }
        }

        HandleTimeScale();

        // Raycast wall check every frame
        if (!isFrozen)
            CheckWallByRaycast();

        previousPosition = transform.position;

        // Update trail every frame
        UpdateTrail();
    }

    // ─────────────────────────────────────────
    // TRAIL SETUP
    // ─────────────────────────────────────────

    void SetupTrail()
    {
        trail = gameObject.AddComponent<TrailRenderer>();

        trail.time = 0.15f;

        // Fat at front, razor sharp at tail
        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0f, 0.08f);
        widthCurve.AddKey(1f, 0.0f);
        trail.widthCurve = widthCurve;

        Gradient gradient = new Gradient();

        GradientColorKey[] colorKeys = new GradientColorKey[3];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[3];

        // Player bullet = red. Enemy bullet = orange.
        // Player reads instantly who fired what.
        if (!isEnemyBullet)
        {
            colorKeys[0].color = new Color(1f, 0.05f, 0.0f);
            colorKeys[1].color = new Color(0.9f, 0f, 0f);
            colorKeys[2].color = new Color(0.5f, 0f, 0f);
        }
        else
        {
            colorKeys[0].color = new Color(1f, 0.5f, 0.0f);
            colorKeys[1].color = new Color(0.9f, 0.3f, 0f);
            colorKeys[2].color = new Color(0.5f, 0.15f, 0f);
        }

        colorKeys[0].time = 0f;
        colorKeys[1].time = 0.4f;
        colorKeys[2].time = 1f;

        alphaKeys[0].alpha = 1f;
        alphaKeys[0].time = 0f;
        alphaKeys[1].alpha = 0.6f;
        alphaKeys[1].time = 0.5f;
        alphaKeys[2].alpha = 0f;
        alphaKeys[2].time = 1f;

        gradient.SetKeys(colorKeys, alphaKeys);
        trail.colorGradient = gradient;

        // Additive blending — trail glows on dark backgrounds
        trail.material = new Material(
            Shader.Find("Sprites/Default"));
        trail.material.SetInt("_SrcBlend",
            (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        trail.material.SetInt("_DstBlend",
            (int)UnityEngine.Rendering.BlendMode.One);

        trail.minVertexDistance = 0.01f;
        trail.shadowCastingMode =
            UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
    }

    // ─────────────────────────────────────────
    // TRAIL UPDATE
    // Called every frame from Update
    // Freezes trail when world is frozen
    // ─────────────────────────────────────────

    void UpdateTrail()
    {
        if (trail == null) return;

        float ts = WorldTimeController.Instance != null
            ? WorldTimeController.Instance.worldTimeScale : 1f;

        if (isFrozen)
        {
            // Stop emitting new trail points
            // Trail hangs perfectly still in air
            trail.emitting = false;
        }
        else
        {
            // Resume trail emission
            trail.emitting = true;

            // Trail length scales with world speed
            // Short at slow time, full at normal time
            trail.time = Mathf.Lerp(0.05f, 0.15f, ts);
        }
    }

    // ─────────────────────────────────────────
    // RAYCAST WALL DETECTION
    // ─────────────────────────────────────────

    void CheckWallByRaycast()
    {
        Vector3 moveThisFrame =
            transform.position - previousPosition;

        float distThisFrame = moveThisFrame.magnitude;
        float checkDist = distThisFrame + 0.2f;

        RaycastHit hit;

        if (Physics.Raycast(
            previousPosition,
            travelDirection,
            out hit,
            checkDist))
        {
            string tag = hit.collider.tag;
            int layer = hit.collider.gameObject.layer;
            int envLayer = LayerMask.NameToLayer("Environment");

            // Hit wall or floor
            if (tag == "Environment" || layer == envLayer)
            {
                hasHit = true;
                NotifyShooter();
                Destroy(gameObject);
                return;
            }

            // Player bullet hit enemy
            if (!isEnemyBullet && tag == "Enemy")
            {
                hasHit = true;
                Security sec =
                    hit.collider.GetComponent<Security>() ??
                    hit.collider.GetComponentInParent<Security>();
                if (sec != null) sec.Die();
                NotifyShooter();
                Destroy(gameObject);
                return;
            }

            // Enemy bullet hit player
            if (isEnemyBullet && tag == "Player")
            {
                hasHit = true;
                PlayerDeath pd =
                    hit.collider.GetComponent<PlayerDeath>() ??
                    hit.collider.GetComponentInParent<PlayerDeath>();
                if (pd != null) pd.Die();
                Destroy(gameObject);
                return;
            }
        }
    }

    // ─────────────────────────────────────────
    // TIME SCALE
    // ─────────────────────────────────────────

    void HandleTimeScale()
    {
        float timeScale = WorldTimeController.Instance != null
            ? WorldTimeController.Instance.worldTimeScale : 1f;

        if (timeScale < 0.05f)
        {
            if (!isFrozen)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
                isFrozen = true;
            }
        }
        else
        {
            if (isFrozen)
            {
                rb.isKinematic = false;
                rb.linearVelocity =
                    travelDirection * bulletSpeed * timeScale;
                isFrozen = false;
            }
            else
            {
                rb.linearVelocity =
                    travelDirection * bulletSpeed * timeScale;
            }
        }
    }

    // ─────────────────────────────────────────
    // TRIGGER — backup for enemy and player hits
    // ─────────────────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        if (!isEnemyBullet && other.CompareTag("Enemy"))
        {
            hasHit = true;
            Security sec =
                other.GetComponent<Security>() ??
                other.GetComponentInParent<Security>();
            if (sec != null) sec.Die();
            NotifyShooter();
            Destroy(gameObject);
            return;
        }

        if (isEnemyBullet && other.CompareTag("Player"))
        {
            hasHit = true;
            PlayerDeath pd =
                other.GetComponent<PlayerDeath>() ??
                other.GetComponentInParent<PlayerDeath>();
            if (pd != null) pd.Die();
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Environment"))
        {
            hasHit = true;
            NotifyShooter();
            Destroy(gameObject);
        }
    }

    // ─────────────────────────────────────────
    // NOTIFY SHOOTER
    // ─────────────────────────────────────────

    void NotifyShooter()
    {
        if (!isEnemyBullet && shooter != null)
        {
            shooter.BulletDestroyed();
            shooter = null;
        }
    }

    // ─────────────────────────────────────────
    // ONDESTROY — final safety net
    // ─────────────────────────────────────────

    void OnDestroy()
    {
        NotifyShooter();
    }
}