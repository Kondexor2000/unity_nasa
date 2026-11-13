using UnityEngine;

/// <summary>
/// Coordinates the simulation model, scene view, and user controls.
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

    private OrbitalSimulation simulation;
    private CosmicFieldView view;
    private bool paused;

    public int BodyCount
    {
        get { return simulation == null ? 0 : simulation.BodyCount; }
    }

    public float ElapsedTime
    {
        get { return simulation == null ? 0f : simulation.ElapsedTime; }
    }

    public bool IsPaused
    {
        get { return paused; }
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        simulation = new OrbitalSimulation();
        ConfigureSimulation();
        simulation.Reset(particleCount);

        view = gameObject.AddComponent<CosmicFieldView>();
        view.Initialize(simulation);
        view.SetFieldVisible(showField);
        view.SetTrailsVisible(showTrails);

        CosmicFieldControls controls = gameObject.AddComponent<CosmicFieldControls>();
        controls.Initialize(this);
    }

    private void Update()
    {
        if (simulation == null) return;

        ConfigureSimulation();
        if (!paused)
        {
            float deltaTime = Mathf.Min(Time.deltaTime, 0.04f) * simulationSpeed;
            simulation.Step(deltaTime);
        }

        view.Render();
        view.SetFieldVisible(showField);
    }

    private void ConfigureSimulation()
    {
        simulation.Configure(gravitationalConstant, softening, magneticStrength, waveStrength, waveFrequency);
    }

    public void TogglePause()
    {
        paused = !paused;
    }

    public void ToggleField()
    {
        showField = !showField;
        if (view != null) view.SetFieldVisible(showField);
    }

    public void ToggleWave()
    {
        showWave = !showWave;
    }

    public void ToggleTrails()
    {
        showTrails = !showTrails;
        if (view != null) view.SetTrailsVisible(showTrails);
    }

    public void ResetSimulation()
    {
        if (simulation == null) return;

        ConfigureSimulation();
        simulation.Reset(particleCount);
        if (view != null) view.ResetBodies(showTrails);
    }
}
