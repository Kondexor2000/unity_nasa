using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns orbital state and advances it with a kick-drift-kick integrator.
/// </summary>
public sealed class OrbitalSimulation
{
    private const float MaximumStep = 0.0125f;
    private const int MinimumBodyCount = 3;
    private const int MaximumBodyCount = 80;

    private OrbitalBodyState[] bodies = new OrbitalBodyState[0];
    private Vector3[] firstAccelerations = new Vector3[0];
    private Vector3[] secondAccelerations = new Vector3[0];
    private float gravitationalConstant;
    private float softening;
    private float magneticStrength;
    private float waveStrength;
    private float waveFrequency;

    public int BodyCount
    {
        get { return bodies.Length; }
    }

    public float ElapsedTime { get; private set; }

    public void Configure(float gravity, float forceSoftening, float magnetic, float wave, float frequency)
    {
        gravitationalConstant = gravity;
        softening = forceSoftening;
        magneticStrength = magnetic;
        waveStrength = wave;
        waveFrequency = frequency;
    }

    public OrbitalBodyState GetBody(int index)
    {
        return bodies[index];
    }

    public void Reset(int bodyCount)
    {
        int count = Mathf.Clamp(bodyCount, MinimumBodyCount, MaximumBodyCount);
        OrbitalBodyState[] initialBodies = new OrbitalBodyState[count];

        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            float ring = 4.2f + (i % 4) * 1.35f;
            Vector3 position = new Vector3(Mathf.Cos(angle) * ring, 0f, Mathf.Sin(angle) * ring);
            float centralSpeed = Mathf.Sqrt(gravitationalConstant * 15f / ring);
            Vector3 tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            initialBodies[i] = new OrbitalBodyState(
                position,
                tangent * centralSpeed * UnityEngine.Random.Range(0.83f, 1.14f),
                UnityEngine.Random.Range(0.6f, 1.4f),
                i % 2 == 0 ? 1f : -1f,
                UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                0.12f + (i % 4) * 0.035f);
        }

        SetBodies(initialBodies);
        ElapsedTime = 0f;
    }

    public void SetBodies(IList<OrbitalBodyState> initialBodies)
    {
        if (initialBodies == null) throw new ArgumentNullException("initialBodies");

        bodies = new OrbitalBodyState[initialBodies.Count];
        for (int i = 0; i < initialBodies.Count; i++)
        {
            bodies[i] = initialBodies[i];
        }

        firstAccelerations = new Vector3[bodies.Length];
        secondAccelerations = new Vector3[bodies.Length];
        ElapsedTime = 0f;
    }

    public void Step(float deltaTime)
    {
        if (deltaTime <= 0f) return;

        int steps = Mathf.Max(1, Mathf.CeilToInt(deltaTime / MaximumStep));
        float step = deltaTime / steps;
        for (int i = 0; i < steps; i++)
        {
            Integrate(step);
        }

        ElapsedTime += deltaTime;
    }

    private void Integrate(float deltaTime)
    {
        CalculateAccelerations(firstAccelerations);
        for (int i = 0; i < bodies.Length; i++)
        {
            OrbitalBodyState body = bodies[i];
            body.Velocity += firstAccelerations[i] * (deltaTime * 0.5f);
            body.Position += body.Velocity * deltaTime;
            bodies[i] = body;
        }

        CalculateAccelerations(secondAccelerations);
        for (int i = 0; i < bodies.Length; i++)
        {
            OrbitalBodyState body = bodies[i];
            body.Velocity += secondAccelerations[i] * (deltaTime * 0.5f);
            bodies[i] = body;
        }
    }

    private void CalculateAccelerations(Vector3[] accelerations)
    {
        Array.Clear(accelerations, 0, accelerations.Length);
        Vector3 magneticAxis = Vector3.up;

        for (int i = 0; i < bodies.Length; i++)
        {
            OrbitalBodyState body = bodies[i];
            Vector3 radial = body.Position;
            float coreR2 = radial.sqrMagnitude + softening * softening;
            accelerations[i] += -gravitationalConstant * 15f * radial / (coreR2 * Mathf.Sqrt(coreR2));

            for (int j = i + 1; j < bodies.Length; j++)
            {
                OrbitalBodyState other = bodies[j];
                Vector3 delta = other.Position - body.Position;
                float distanceSquared = delta.sqrMagnitude + softening * softening;
                Vector3 force = gravitationalConstant * delta / (distanceSquared * Mathf.Sqrt(distanceSquared));
                accelerations[i] += force * other.Mass;
                accelerations[j] -= force * body.Mass;
            }

            Vector3 magneticField = magneticStrength *
                (3f * radial.y * radial / (coreR2 * coreR2) -
                 magneticAxis / (coreR2 * Mathf.Sqrt(coreR2)));
            accelerations[i] += body.Charge * Vector3.Cross(body.Velocity, magneticField);

            float wave = waveStrength * Mathf.Sin(
                waveFrequency * ElapsedTime - radial.magnitude * 0.7f + body.Phase);
            accelerations[i] += radial.normalized * wave;
        }
    }
}
