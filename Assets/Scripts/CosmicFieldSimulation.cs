using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A small, self-contained orbital/N-body playground. The magnetic and wave terms
/// are deliberately a visualized toy model, not a claim about cosmology.
/// </summary>
public sealed class CosmicFieldSimulation : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindObjectOfType<CosmicFieldSimulation>() != null) return;
        new GameObject("Cosmic Field Simulation").AddComponent<CosmicFieldSimulation>();
    }

    [Header("Simulation")]
    [Range(3, 80)] public int particleCount = 22;
    [Min(0.05f)] public float gravitationalConstant = 2.2f;
    [Min(0.01f)] public float softening = 0.35f;
    [Range(0f, 2f)] public float magneticStrength = 0.28f;
    [Range(0f, 2f)] public float waveStrength = 0.12f;
    [Range(0.1f, 3f)] public float waveFrequency = 0.8f;
    [Range(0.1f, 3f)] public float simulationSpeed = 1f;
    [Header("Display")]
    public bool showField = true;
    public bool showWave = true;
    public bool showTrails = true;

    private sealed class Body
    {
        public Transform transform;
        public TrailRenderer trail;
        public Vector3 position;
        public Vector3 velocity;
        public float mass;
        public float charge;
        public float phase;
        public float radius;
    }

    private readonly List<Body> bodies = new List<Body>();
    private Transform fieldLines;
    private Material lineMaterial;
    private float elapsed;
    private bool paused;

    private void Start()
    {
        Application.targetFrameRate = 60;
        CreateScene();
        CreateBodies();
    }

    private void CreateScene()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Simulation Camera");
            camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
        }
        camera.transform.position = new Vector3(0f, 13f, -24f);
        camera.transform.LookAt(Vector3.zero);
        camera.fieldOfView = 48f;
        camera.backgroundColor = new Color(0.008f, 0.014f, 0.035f);

        if (FindObjectOfType<Light>() == null)
        {
            GameObject lightObject = new GameObject("Star Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 50f;
            light.intensity = 2f;
            lightObject.transform.position = new Vector3(-5f, 8f, -8f);
        }

        GameObject star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        star.name = "Magnetic Core";
        star.transform.localScale = Vector3.one * 2.25f;
        SetMaterial(star, new Color(1f, 0.48f, 0.12f), 1.2f);
        GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glow.name = "Core Corona";
        glow.transform.localScale = Vector3.one * 2.75f;
        SetMaterial(glow, new Color(0.25f, 0.55f, 1f), 0.25f);
        Collider glowCollider = glow.GetComponent<Collider>();
        if (glowCollider != null) Destroy(glowCollider);

        GameObject lines = new GameObject("Magnetic Field Lines");
        fieldLines = lines.transform;
        lineMaterial = new Material(Shader.Find("Sprites/Default"));
        DrawField();
    }

    private void CreateBodies()
    {
        for (int i = 0; i < particleCount; i++)
        {
            float angle = i * Mathf.PI * 2f / particleCount;
            float ring = 4.2f + (i % 4) * 1.35f;
            Vector3 position = new Vector3(Mathf.Cos(angle) * ring, 0f, Mathf.Sin(angle) * ring);
            GameObject bodyObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bodyObject.name = "Charged Body " + (i + 1).ToString("00");
            float radius = 0.12f + (i % 4) * 0.035f;
            bodyObject.transform.position = position;
            bodyObject.transform.localScale = Vector3.one * radius * 2f;
            Color color = i % 2 == 0 ? new Color(0.15f, 0.78f, 1f) : new Color(1f, 0.24f, 0.58f);
            SetMaterial(bodyObject, color, 0.35f);

            TrailRenderer trail = bodyObject.AddComponent<TrailRenderer>();
            trail.time = 8f;
            trail.startWidth = radius * 0.7f;
            trail.endWidth = 0f;
            trail.material = lineMaterial;
            trail.startColor = color;
            trail.endColor = new Color(color.r, color.g, color.b, 0f);

            float centralSpeed = Mathf.Sqrt(gravitationalConstant * 15f / ring);
            Vector3 tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            bodies.Add(new Body
            {
                transform = bodyObject.transform,
                trail = trail,
                position = position,
                velocity = tangent * centralSpeed * Random.Range(0.83f, 1.14f),
                mass = Random.Range(0.6f, 1.4f),
                charge = i % 2 == 0 ? 1f : -1f,
                phase = Random.Range(0f, Mathf.PI * 2f),
                radius = radius
            });
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) paused = !paused;
        if (Input.GetKeyDown(KeyCode.F)) showField = !showField;
        if (Input.GetKeyDown(KeyCode.W)) showWave = !showWave;
        if (Input.GetKeyDown(KeyCode.T)) SetTrailsVisible(!showTrails);
        if (Input.GetKeyDown(KeyCode.R)) ResetSimulation();
        if (!paused)
        {
            float dt = Mathf.Min(Time.deltaTime, 0.04f) * simulationSpeed;
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt / 0.0125f));
            float step = dt / steps;
            for (int i = 0; i < steps; i++) Integrate(step);
            elapsed += dt;
        }
        if (fieldLines != null) fieldLines.gameObject.SetActive(showField);
    }

    private void SetTrailsVisible(bool visible)
    {
        showTrails = visible;
        foreach (Body body in bodies)
        {
            if (body.trail == null) continue;
            body.trail.emitting = visible;
            // Clearing on both transitions makes the toggle's effect immediate.
            body.trail.Clear();
        }
    }

    // Kick-drift-kick leapfrog: stable for orbital motion and approximately symplectic.
    private void Integrate(float dt)
    {
        Vector3[] first = CalculateAccelerations();
        for (int i = 0; i < bodies.Count; i++)
        {
            Body body = bodies[i];
            body.velocity += first[i] * (dt * 0.5f);
            body.position += body.velocity * dt;
        }
        Vector3[] second = CalculateAccelerations();
        for (int i = 0; i < bodies.Count; i++)
        {
            Body body = bodies[i];
            body.velocity += second[i] * (dt * 0.5f);
            body.transform.position = body.position;
            body.transform.localScale = Vector3.one * body.radius * 2f * (1f + 0.08f * Mathf.Sin(elapsed * 4f + body.phase));
        }
    }

    private Vector3[] CalculateAccelerations()
    {
        Vector3[] accelerations = new Vector3[bodies.Count];
        Vector3 magneticAxis = Vector3.up;
        for (int i = 0; i < bodies.Count; i++)
        {
            Body body = bodies[i];
            Vector3 radial = body.position;
            float coreR2 = radial.sqrMagnitude + softening * softening;
            accelerations[i] += -gravitationalConstant * 15f * radial / (coreR2 * Mathf.Sqrt(coreR2));
            for (int j = i + 1; j < bodies.Count; j++)
            {
                Vector3 delta = bodies[j].position - body.position;
                float r2 = delta.sqrMagnitude + softening * softening;
                Vector3 force = gravitationalConstant * delta / (r2 * Mathf.Sqrt(r2));
                accelerations[i] += force * bodies[j].mass;
                accelerations[j] -= force * body.mass;
            }

            // Dipole-like field with Lorentz force q(v x B); intentionally stylized.
            Vector3 bField = magneticStrength * (3f * radial.y * radial / (coreR2 * coreR2) - magneticAxis / (coreR2 * Mathf.Sqrt(coreR2)));
            accelerations[i] += body.charge * Vector3.Cross(body.velocity, bField);

            // A traveling radial wave acts as a weak, phase-shifted harmonic perturbation.
            float wave = waveStrength * Mathf.Sin(waveFrequency * elapsed - radial.magnitude * 0.7f + body.phase);
            accelerations[i] += radial.normalized * wave;
        }
        return accelerations;
    }

    private void DrawField()
    {
        for (int line = 0; line < 8; line++)
        {
            float longitude = line * Mathf.PI * 2f / 8f;
            GameObject path = new GameObject("Field Line " + line);
            path.transform.SetParent(fieldLines, false);
            LineRenderer renderer = path.AddComponent<LineRenderer>();
            renderer.useWorldSpace = true;
            renderer.positionCount = 80;
            renderer.widthMultiplier = 0.025f;
            renderer.material = lineMaterial;
            renderer.startColor = new Color(0.18f, 0.62f, 1f, 0.42f);
            renderer.endColor = new Color(0.7f, 0.25f, 1f, 0.08f);
            for (int p = 0; p < 80; p++)
            {
                float t = (p / 79f - 0.5f) * 2.65f;
                float cosh = (float)System.Math.Cosh(t);
                float sinh = (float)System.Math.Sinh(t);
                float r = 3.1f * cosh * cosh;
                float latitude = Mathf.Atan(2f * sinh);
                Vector3 point = new Vector3(Mathf.Cos(longitude) * Mathf.Cos(latitude), Mathf.Sin(latitude), Mathf.Sin(longitude) * Mathf.Cos(latitude)) * r;
                renderer.SetPosition(p, point);
            }
        }
    }

    private static void SetMaterial(GameObject target, Color color, float emission)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        Material material = new Material(Shader.Find("Standard"));
        material.color = color;
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * emission);
        renderer.material = material;
    }

    private void ResetSimulation()
    {
        elapsed = 0f;
        foreach (Body body in bodies) Destroy(body.transform.gameObject);
        bodies.Clear();
        CreateBodies();
    }

    private void OnGUI()
    {
        GUI.color = new Color(0.78f, 0.91f, 1f);
        GUI.Label(new Rect(22, 18, 420, 32), "MAGNETIC N-BODY  /  ORBITAL LAB");
        GUI.color = Color.white;
        GUI.Label(new Rect(22, 48, 500, 24), "Grawitacja + Lorentz + perturbacja falowa  |  model demonstracyjny");
        GUI.Label(new Rect(22, 76, 500, 24), "Ciała: " + bodies.Count + "   Czas symulacji: " + elapsed.ToString("F1") + " s   " + (paused ? "PAUZA" : "RUN"));
        GUI.Label(new Rect(22, Screen.height - 42, 700, 24), "SPACJA pauza   F pole   W fala   T ślady: " + (showTrails ? "ON" : "OFF") + "   R reset");
        if (GUI.Button(new Rect(Screen.width - 150, 20, 128, 34), "Ślady: " + (showTrails ? "WŁĄCZONE" : "WYŁĄCZONE")))
            SetTrailsVisible(!showTrails);

        if (showWave)
        {
            float y = Screen.height - 115;
            GUI.color = new Color(0.14f, 0.77f, 1f);
            GUI.DrawTexture(new Rect(22, y, 220, 1), Texture2D.whiteTexture);
            for (int x = 0; x < 220; x += 2)
            {
                float a = (x / 220f * 4f * Mathf.PI) + elapsed * waveFrequency;
                float h = Mathf.Sin(a) * 13f;
                GUI.DrawTexture(new Rect(22 + x, y - h, 2, 2), Texture2D.whiteTexture);
            }
        }
    }
}
