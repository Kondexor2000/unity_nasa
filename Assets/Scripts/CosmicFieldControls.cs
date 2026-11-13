using UnityEngine;

/// <summary>
/// Handles keyboard input and the on-screen controls.
/// </summary>
public sealed class CosmicFieldControls : MonoBehaviour
{
    private CosmicFieldSimulation simulation;

    public void Initialize(CosmicFieldSimulation owner)
    {
        simulation = owner;
    }

    private void Update()
    {
        if (simulation == null) return;

        if (Input.GetKeyDown(KeyCode.Space)) simulation.TogglePause();
        if (Input.GetKeyDown(KeyCode.F)) simulation.ToggleField();
        if (Input.GetKeyDown(KeyCode.W)) simulation.ToggleWave();
        if (Input.GetKeyDown(KeyCode.T)) simulation.ToggleTrails();
        if (Input.GetKeyDown(KeyCode.R)) simulation.ResetSimulation();
    }

    private void OnGUI()
    {
        if (simulation == null) return;

        GUI.color = new Color(0.78f, 0.91f, 1f);
        GUI.Label(new Rect(22, 18, 420, 32), "MAGNETIC N-BODY  /  ORBITAL LAB");
        GUI.color = Color.white;
        GUI.Label(new Rect(22, 48, 500, 24), "Grawitacja + Lorentz + perturbacja falowa  |  model demonstracyjny");
        GUI.Label(new Rect(22, 76, 500, 24),
            "Ciała: " + simulation.BodyCount + "   Czas symulacji: " +
            simulation.ElapsedTime.ToString("F1") + " s   " + (simulation.IsPaused ? "PAUZA" : "RUN"));
        GUI.Label(new Rect(22, Screen.height - 42, 700, 24),
            "SPACJA pauza   F pole   W fala   T ślady: " +
            (simulation.showTrails ? "ON" : "OFF") + "   R reset");
        if (GUI.Button(new Rect(Screen.width - 150, 20, 128, 34),
            "Ślady: " + (simulation.showTrails ? "WŁĄCZONE" : "WYŁĄCZONE")))
        {
            simulation.ToggleTrails();
        }

        if (simulation.showWave)
        {
            float y = Screen.height - 115;
            GUI.color = new Color(0.14f, 0.77f, 1f);
            GUI.DrawTexture(new Rect(22, y, 220, 1), Texture2D.whiteTexture);
            for (int x = 0; x < 220; x += 2)
            {
                float phase = (x / 220f * 4f * Mathf.PI) + simulation.ElapsedTime * simulation.waveFrequency;
                float height = Mathf.Sin(phase) * 13f;
                GUI.DrawTexture(new Rect(22 + x, y - height, 2, 2), Texture2D.whiteTexture);
            }
        }
    }
}
