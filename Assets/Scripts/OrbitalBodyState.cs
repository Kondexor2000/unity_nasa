using UnityEngine;

/// <summary>
/// Position, velocity, and physical parameters for one simulated body.
/// </summary>
public struct OrbitalBodyState
{
    public Vector3 Position;
    public Vector3 Velocity;
    public float Mass;
    public float Charge;
    public float Phase;
    public float Radius;

    public OrbitalBodyState(Vector3 position, Vector3 velocity, float mass, float charge, float phase, float radius)
    {
        Position = position;
        Velocity = velocity;
        Mass = mass;
        Charge = charge;
        Phase = phase;
        Radius = radius;
    }
}
