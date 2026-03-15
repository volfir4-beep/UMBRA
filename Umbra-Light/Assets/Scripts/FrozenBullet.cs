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
    private TrailRenderer trail;

    public void SetShooter(PlayerShooting ps)
    {
        shooter = ps;
    }

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

    void Update()
    {
        if (hasHit) return;

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

        if (!isFrozen)
            CheckWallByRaycast();

        previousPosition = transform.position;
        UpdateTrail();
    }

    void SetupTrail()
    {
        trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.15f;

        AnimationCurve widthCurve = new AnimationCurve();
        widthCurve.AddKey(0f, 0.08f);
        widthCurve.AddKey(1f, 0.0f);
        trail.widthCurve = widthCurve;

        Gradient gradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[3];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[3];

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

    void UpdateTrail()
    {
        if (trail == null) return;

        float ts = WorldTimeController.Instance != null
            ? WorldTimeController.Instance.worldTimeScale : 1f;

        if (isFrozen)
            trail.emitting = false;
        else
        {
            trail.emitting = true;
            trail.time = Mathf.Lerp(0.05f, 0.15f, ts);
        }
    }

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

            // ── TARGET BOARD ──────────────────────
            // Check this BEFORE environment
            // so board is destroyed not just stopped
            if (!isEnemyBullet && tag == "Target")
            {
                hasHit = true;
                TargetBoard tb =
                    hit.collider.GetComponent<TargetBoard>() ??
                    hit.collider.GetComponentInParent<TargetBoard>();
                if (tb != null) tb.GetShot();
                NotifyShooter();
                Destroy(gameObject);
                return;
            }

            // ── WALL OR FLOOR ──────────────────────
            if (tag == "Environment" || layer == envLayer)
            {
                hasHit = true;
                NotifyShooter();
                Destroy(gameObject);
                return;
            }

            // ── ENEMY ─────────────────────────────
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

            // ── PLAYER ────────────────────────────
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

    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // Target board via trigger
        if (!isEnemyBullet && other.CompareTag("Target"))
        {
            hasHit = true;
            TargetBoard tb =
                other.GetComponent<TargetBoard>() ??
                other.GetComponentInParent<TargetBoard>();
            if (tb != null) tb.GetShot();
            NotifyShooter();
            Destroy(gameObject);
            return;
        }

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

    void NotifyShooter()
    {
        if (!isEnemyBullet && shooter != null)
        {
            shooter.BulletDestroyed();
            shooter = null;
        }
    }

    void OnDestroy()
    {
        NotifyShooter();
    }
}