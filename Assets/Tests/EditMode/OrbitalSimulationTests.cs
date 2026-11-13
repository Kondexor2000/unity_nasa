using NUnit.Framework;
using UnityEngine;

public sealed class OrbitalSimulationTests
{
    [Test]
    public void StepWithoutForcesPreservesConstantVelocity()
    {
        OrbitalSimulation simulation = CreateSimulation();
        OrbitalBodyState initial = new OrbitalBodyState(
            new Vector3(5f, 0f, 0f),
            new Vector3(0f, 0f, 1f),
            1f,
            0f,
            0f,
            0.2f);
        simulation.SetBodies(new[] { initial });

        simulation.Step(0.1f);

        OrbitalBodyState result = simulation.GetBody(0);
        Assert.That(Vector3.Distance(result.Position, new Vector3(5f, 0f, 0.1f)), Is.LessThan(0.0001f));
        Assert.That(Vector3.Distance(result.Velocity, initial.Velocity), Is.LessThan(0.0001f));
        Assert.That(simulation.ElapsedTime, Is.EqualTo(0.1f).Within(0.0001f));
    }

    [Test]
    public void CentralGravityAcceleratesBodyTowardOrigin()
    {
        OrbitalSimulation simulation = CreateSimulation();
        simulation.Configure(2.2f, 0.35f, 0f, 0f, 0f);
        simulation.SetBodies(new[]
        {
            new OrbitalBodyState(new Vector3(5f, 0f, 0f), Vector3.zero, 1f, 0f, 0f, 0.2f)
        });

        simulation.Step(0.1f);

        OrbitalBodyState result = simulation.GetBody(0);
        Assert.That(result.Position.x, Is.LessThan(5f));
        Assert.That(float.IsNaN(result.Position.x), Is.False);
        Assert.That(float.IsInfinity(result.Position.x), Is.False);
    }

    [Test]
    public void ResetClampsBodyCountAndClearsElapsedTime()
    {
        OrbitalSimulation simulation = CreateSimulation();
        simulation.Reset(22);
        simulation.Step(0.1f);

        simulation.Reset(100);

        Assert.That(simulation.BodyCount, Is.EqualTo(80));
        Assert.That(simulation.ElapsedTime, Is.Zero);
    }

    private static OrbitalSimulation CreateSimulation()
    {
        OrbitalSimulation simulation = new OrbitalSimulation();
        simulation.Configure(0f, 0.35f, 0f, 0f, 0f);
        return simulation;
    }
}
