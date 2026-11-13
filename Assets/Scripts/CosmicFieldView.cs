using System;
using UnityEngine;

/// <summary>
/// Creates and updates the scene objects used to display the simulation.
/// </summary>
public sealed class CosmicFieldView : MonoBehaviour
{
    private static readonly Color CoreColor = new Color(1f, 0.48f, 0.12f);
    private static readonly Color CoronaColor = new Color(0.25f, 0.55f, 1f);
    private static readonly Color PositiveBodyColor = new Color(0.15f, 0.78f, 1f);
    private static readonly Color NegativeBodyColor = new Color(1f, 0.24f, 0.58f);

    private OrbitalSimulation simulation;
    private Transform fieldLines;
    private Transform[] bodyTransforms;
    private TrailRenderer[] trails;
    private Material lineMaterial;
    private Material coreMaterial;
    private Material coronaMaterial;
    private Material positiveBodyMaterial;
    private Material negativeBodyMaterial;
    private bool initialized;

    public void Initialize(OrbitalSimulation orbitalSimulation)
    {
        if (orbitalSimulation == null) throw new ArgumentNullException("orbitalSimulation");

        simulation = orbitalSimulation;
        CreateMaterials();
        ConfigureCamera();
        ConfigureLight();
        CreateCore();
        CreateFieldLines();
        CreateBodies();
        initialized = true;
    }

    public void Render()
    {
        if (!initialized) return;

        for (int i = 0; i < bodyTransforms.Length; i++)
        {
            OrbitalBodyState body = simulation.GetBody(i);
            bodyTransforms[i].position = body.Position;
            bodyTransforms[i].localScale = Vector3.one * body.Radius * 2f *
                (1f + 0.08f * Mathf.Sin(simulation.ElapsedTime * 4f + body.Phase));
        }
    }

    public void SetFieldVisible(bool visible)
    {
        if (fieldLines != null) fieldLines.gameObject.SetActive(visible);
    }

    public void SetTrailsVisible(bool visible)
    {
        if (trails == null) return;

        for (int i = 0; i < trails.Length; i++)
        {
            trails[i].emitting = visible;
            trails[i].Clear();
        }
    }

    public void ResetBodies(bool trailsVisible)
    {
        if (!initialized) return;

        if (bodyTransforms.Length != simulation.BodyCount)
        {
            for (int i = 0; i < bodyTransforms.Length; i++)
            {
                if (bodyTransforms[i] != null) Destroy(bodyTransforms[i].gameObject);
            }
            CreateBodies();
        }
        else
        {
            for (int i = 0; i < bodyTransforms.Length; i++)
            {
                OrbitalBodyState body = simulation.GetBody(i);
                bodyTransforms[i].position = body.Position;
                trails[i].Clear();
            }
        }

        SetTrailsVisible(trailsVisible);
    }

    private void CreateMaterials()
    {
        lineMaterial = CreateMaterial("Sprites/Default", Color.white, 0f);
        coreMaterial = CreateMaterial("Standard", CoreColor, 1.2f);
        coronaMaterial = CreateMaterial("Standard", CoronaColor, 0.25f);
        positiveBodyMaterial = CreateMaterial("Standard", PositiveBodyColor, 0.35f);
        negativeBodyMaterial = CreateMaterial("Standard", NegativeBodyColor, 0.35f);
    }

    private void ConfigureCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Simulation Camera");
            cameraObject.transform.SetParent(transform, false);
            camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
        }

        camera.transform.position = new Vector3(0f, 13f, -24f);
        camera.transform.LookAt(Vector3.zero);
        camera.fieldOfView = 48f;
        camera.backgroundColor = new Color(0.008f, 0.014f, 0.035f);
    }

    private void ConfigureLight()
    {
        if (FindObjectOfType<Light>() != null) return;

        GameObject lightObject = new GameObject("Star Light");
        lightObject.transform.SetParent(transform, false);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 50f;
        light.intensity = 2f;
        lightObject.transform.position = new Vector3(-5f, 8f, -8f);
    }

    private void CreateCore()
    {
        GameObject star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        star.name = "Magnetic Core";
        star.transform.SetParent(transform, false);
        star.transform.localScale = Vector3.one * 2.25f;
        star.GetComponent<Renderer>().sharedMaterial = coreMaterial;

        GameObject corona = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        corona.name = "Core Corona";
        corona.transform.SetParent(transform, false);
        corona.transform.localScale = Vector3.one * 2.75f;
        corona.GetComponent<Renderer>().sharedMaterial = coronaMaterial;
        Collider coronaCollider = corona.GetComponent<Collider>();
        if (coronaCollider != null) Destroy(coronaCollider);
    }

    private void CreateFieldLines()
    {
        GameObject lines = new GameObject("Magnetic Field Lines");
        lines.transform.SetParent(transform, false);
        fieldLines = lines.transform;
        CreateFieldLineRenderers();
    }

    private void CreateFieldLineRenderers()
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
            renderer.sharedMaterial = lineMaterial;
            renderer.startColor = new Color(0.18f, 0.62f, 1f, 0.42f);
            renderer.endColor = new Color(0.7f, 0.25f, 1f, 0.08f);

            for (int pointIndex = 0; pointIndex < renderer.positionCount; pointIndex++)
            {
                float t = (pointIndex / 79f - 0.5f) * 2.65f;
                float cosh = (float)Math.Cosh(t);
                float sinh = (float)Math.Sinh(t);
                float radius = 3.1f * cosh * cosh;
                float latitude = Mathf.Atan(2f * sinh);
                Vector3 point = new Vector3(
                    Mathf.Cos(longitude) * Mathf.Cos(latitude),
                    Mathf.Sin(latitude),
                    Mathf.Sin(longitude) * Mathf.Cos(latitude)) * radius;
                renderer.SetPosition(pointIndex, point);
            }
        }
    }

    private void CreateBodies()
    {
        bodyTransforms = new Transform[simulation.BodyCount];
        trails = new TrailRenderer[simulation.BodyCount];

        for (int i = 0; i < simulation.BodyCount; i++)
        {
            OrbitalBodyState body = simulation.GetBody(i);
            GameObject bodyObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bodyObject.name = "Charged Body " + (i + 1).ToString("00");
            bodyObject.transform.SetParent(transform, false);
            bodyObject.transform.position = body.Position;
            bodyObject.transform.localScale = Vector3.one * body.Radius * 2f;
            bodyObject.GetComponent<Renderer>().sharedMaterial =
                i % 2 == 0 ? positiveBodyMaterial : negativeBodyMaterial;

            TrailRenderer trail = bodyObject.AddComponent<TrailRenderer>();
            trail.time = 8f;
            trail.startWidth = body.Radius * 0.7f;
            trail.endWidth = 0f;
            trail.sharedMaterial = lineMaterial;
            trail.startColor = i % 2 == 0 ? PositiveBodyColor : NegativeBodyColor;
            Color bodyColor = i % 2 == 0 ? PositiveBodyColor : NegativeBodyColor;
            trail.endColor = new Color(bodyColor.r, bodyColor.g, bodyColor.b, 0f);

            bodyTransforms[i] = bodyObject.transform;
            trails[i] = trail;
        }
    }

    private static Material CreateMaterial(string shaderName, Color color, float emission)
    {
        Shader shader = Shader.Find(shaderName);
        if (shader == null) throw new InvalidOperationException("Required shader was not found: " + shaderName);

        Material material = new Material(shader);
        material.color = color;
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * emission);
        return material;
    }

    private void OnDestroy()
    {
        DestroyMaterial(lineMaterial);
        DestroyMaterial(coreMaterial);
        DestroyMaterial(coronaMaterial);
        DestroyMaterial(positiveBodyMaterial);
        DestroyMaterial(negativeBodyMaterial);
    }

    private static void DestroyMaterial(Material material)
    {
        if (material == null) return;
        if (Application.isPlaying) Destroy(material);
        else DestroyImmediate(material);
    }
}
