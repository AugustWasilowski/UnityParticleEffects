using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class ParticlePlexus : MonoBehaviour
{
    public float maxDistance = 1f;

    public int maxConnections = 5;
    public int maxLineRenderers = 100;

    new ParticleSystem particleSystem;
    ParticleSystem.Particle[] particles;

    ParticleSystem.MainModule particleSystemMainModule;

    public LineRenderer lineRendererTemplate;
    List<LineRenderer> lineRenderers = new List<LineRenderer>();

    Transform _transform;

	void Start ()
    {
        particleSystem = GetComponent<ParticleSystem>();
        particleSystemMainModule = particleSystem.main;

        _transform = transform;
	}
	
	void LateUpdate ()
    {
        if (maxLineRenderers <= 0 || maxDistance <= 0f || maxConnections <= 0)
        {
            return;
        }

        int maxParticles = particleSystemMainModule.maxParticles;

        if (particles == null || particles.Length < maxParticles)
        {
            particles = new ParticleSystem.Particle[maxParticles];
        }

        int lrIndex = 0;
        int lineRendererCount = lineRenderers.Count;

        if (lineRendererCount > maxLineRenderers)
        {
            for (int i = maxLineRenderers; i < lineRendererCount; i++)
            {
                if (lineRenderers[i].gameObject != null)
                {
                    Destroy(lineRenderers[i].gameObject);
                }                
            }

            int removedCount = lineRendererCount - maxLineRenderers;
            lineRenderers.RemoveRange(maxLineRenderers, removedCount);
            lineRendererCount -= removedCount;   
        }

        if (maxConnections > 0 && maxLineRenderers > 0)
        {
            particleSystem.GetParticles(particles);
            int particleCount = particleSystem.particleCount;

            float maxDistancsSqr = maxDistance * maxDistance;

            switch (particleSystemMainModule.simulationSpace)
            {
                case ParticleSystemSimulationSpace.Local:
                    _transform = transform;
                    lineRendererTemplate.useWorldSpace = false;
                    break;
                case ParticleSystemSimulationSpace.World:
                    _transform = transform;
                    lineRendererTemplate.useWorldSpace = true;
                    break;
                case ParticleSystemSimulationSpace.Custom:
                    _transform = particleSystemMainModule.customSimulationSpace;
                    lineRendererTemplate.useWorldSpace = false;
                    break;
                default:
                    throw new System.NotSupportedException(
                        string.Format("Unsupported simulation space '{0].", System.Enum.GetName(typeof(ParticleSystemSimulationSpace), particleSystemMainModule.simulationSpace)));
            }

            for (int i = 0; i < particleCount; i++)
            {
                if (lrIndex == maxLineRenderers)
                {
                    break;
                }
                Vector3 p1_position = particles[i].position;

                int connections = 0; 

                for (int j = i + 1; j < particleCount; j++)
                {
                    Vector3 p2_position = particles[j].position;
                    float distanceSqr = Vector3.SqrMagnitude(p1_position - p2_position);

                    if (distanceSqr <= maxDistancsSqr)
                    {
                        LineRenderer lr;
                        if (lrIndex == lineRendererCount)
                        {
                            lr = Instantiate(lineRendererTemplate, transform, false);
                            lineRenderers.Add(lr);
                            lineRendererCount++;
                        }

                        lr = lineRenderers[lrIndex];

                        lr.enabled = true;

                        lr.SetPosition(0, p1_position);
                        lr.SetPosition(1, p2_position);

                        lrIndex++;
                        connections++;

                        if (connections == maxConnections || lrIndex == maxLineRenderers)
                        {
                            break;
                        }
                    }
                }
            }
        }

        for (int i = lrIndex; i < lineRendererCount; i++)
        {
            lineRenderers[i].enabled = false;
        }
	}
}
