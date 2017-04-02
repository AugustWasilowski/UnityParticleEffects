﻿
// =================================	
// Namespaces.
// =================================

using UnityEngine;
using System.Collections.Generic;

// =================================	
// Define namespace.
// =================================

namespace MirzaBeig
{

    namespace Scripting
    {

        namespace Effects
        {

            // =================================	
            // Classes.
            // =================================

            [RequireComponent(typeof(ParticleSystem))]
            public class ParticlePlexus : MonoBehaviour
            {
                // =================================	
                // Nested classes and structures.
                // =================================

                // ...

                // =================================	
                // Variables.
                // =================================

                // ...

                public float maxDistance = 1.0f;

                public int maxConnections = 5;
                public int maxLineRenderers = 100;

                [Range(0.0f, 1.0f)]
                public float widthFromParticle = 0.125f;

                [Range(0.0f, 1.0f)]
                public float colourFromParticle = 1.0f;

                [Range(0.0f, 1.0f)]
                public float alphaFromParticle = 1.0f;

                new ParticleSystem particleSystem;

                ParticleSystem.Particle[] particles;
                ParticleSystem.MainModule particleSystemMainModule;

                public LineRenderer lineRendererTemplate;
                List<LineRenderer> lineRenderers = new List<LineRenderer>();

                Transform _transform;

                float timer;

                [Range(0.0f, 1.0f)]
                public float delay = 0.0f;

                bool visible;
                public bool alwaysUpdate = false;

                // =================================	
                // Functions.
                // =================================

                // ...

                void Start()
                {
                    particleSystem = GetComponent<ParticleSystem>();
                    particleSystemMainModule = particleSystem.main;

                    ParticleSystem.ShapeModule shape = particleSystem.shape;
                    shape.radius = 2.25f;

                    _transform = transform;
                }

                // ...

                void OnDisable()
                {
                    for (int i = 0; i < lineRenderers.Count; i++)
                    {
                        lineRenderers[i].enabled = false;
                    }
                }

                // ...

                void OnBecameVisible()
                {
                    visible = true;
                }
                void OnBecameInvisible()
                {
                    visible = false;
                }

                // ...

                void LateUpdate()
                {
                    int lineRenderersCount = lineRenderers.Count;

                    // In case max line renderers value is changed at runtime -> destroy extra.

                    if (lineRenderersCount > maxLineRenderers)
                    {
                        for (int i = maxLineRenderers; i < lineRenderersCount; i++)
                        {
                            Destroy(lineRenderers[i].gameObject);
                        }

                        lineRenderers.RemoveRange(maxLineRenderers, lineRenderersCount - maxLineRenderers);
                        lineRenderersCount -= lineRenderersCount - maxLineRenderers;
                    }

                    if (alwaysUpdate || visible)
                    {
                        // Prevent constant allocations so long as max particle count doesn't change.

                        int maxParticles = particleSystemMainModule.maxParticles;

                        if (particles == null || particles.Length < maxParticles)
                        {
                            particles = new ParticleSystem.Particle[maxParticles];
                        }

                        timer += Time.deltaTime;

                        if (timer >= delay)
                        {
                            timer = 0.0f;

                            int lrIndex = 0;

                            // Only update if drawing/making connections.

                            if (maxConnections > 0 && maxLineRenderers > 0)
                            {
                                particleSystem.GetParticles(particles);
                                int particleCount = particleSystem.particleCount;

                                float maxDistanceSqr = maxDistance * maxDistance;

                                ParticleSystemSimulationSpace simulationSpace = particleSystemMainModule.simulationSpace;
                                ParticleSystemScalingMode scalingMode = particleSystemMainModule.scalingMode;

                                Transform customSimulationSpaceTransform = particleSystemMainModule.customSimulationSpace;

                                Color lineRendererStartColour = lineRendererTemplate.startColor;
                                Color lineRendererEndColour = lineRendererTemplate.endColor;

                                float lineRendererStartWidth = lineRendererTemplate.startWidth * lineRendererTemplate.widthMultiplier;
                                float lineRendererEndWidth = lineRendererTemplate.endWidth * lineRendererTemplate.widthMultiplier;

                                // If in world space, there's no need to do any of the extra calculations... simplify the loop!

                                if (simulationSpace == ParticleSystemSimulationSpace.World)
                                {
                                    for (int i = 0; i < particleCount; i++)
                                    {
                                        if (lrIndex == maxLineRenderers)
                                        {
                                            break;
                                        }

                                        Color particleColour = particles[i].GetCurrentColor(particleSystem);

                                        Color lineStartColour = Color.LerpUnclamped(lineRendererStartColour, particleColour, colourFromParticle);
                                        lineStartColour.a = Mathf.LerpUnclamped(lineRendererStartColour.a, particleColour.a, alphaFromParticle);

                                        float lineStartWidth = Mathf.LerpUnclamped(lineRendererStartWidth, particles[i].GetCurrentSize(particleSystem), widthFromParticle);

                                        int connections = 0;

                                        for (int j = i + 1; j < particleCount; j++)
                                        {
                                            Vector3 p1p2_difference = new Vector3(

                                                particles[i].position.x - particles[j].position.x,
                                                particles[i].position.y - particles[j].position.y,
                                                particles[i].position.z - particles[j].position.z);

                                            float distanceSqr = Vector3.SqrMagnitude(p1p2_difference);

                                            if (distanceSqr <= maxDistanceSqr)
                                            {
                                                LineRenderer lr;

                                                if (lrIndex == lineRenderersCount)
                                                {
                                                    lr = Instantiate(lineRendererTemplate, _transform, false);

                                                    lineRenderers.Add(lr);
                                                    lineRenderersCount++;
                                                }

                                                lr = lineRenderers[lrIndex]; lr.enabled = true;

                                                lr.SetPosition(0, particles[i].position);
                                                lr.SetPosition(1, particles[j].position);

                                                lr.startColor = lineStartColour;

                                                particleColour = particles[j].GetCurrentColor(particleSystem);

                                                Color lineEndColour = Color.LerpUnclamped(lineRendererEndColour, particleColour, colourFromParticle);
                                                lineEndColour.a = Mathf.LerpUnclamped(lineRendererEndColour.a, particleColour.a, alphaFromParticle);

                                                lr.endColor = lineEndColour;

                                                float particleWidth = particles[i].GetCurrentSize(particleSystem);

                                                lr.startWidth = lineStartWidth;
                                                lr.endWidth = Mathf.LerpUnclamped(lineRendererEndWidth, particles[j].GetCurrentSize(particleSystem), widthFromParticle);

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
                                else
                                {
                                    Vector3 position = Vector3.zero;
                                    Quaternion rotation = Quaternion.identity;
                                    Vector3 localScale = Vector3.one;

                                    Transform simulationSpaceTransform = _transform;

                                    switch (simulationSpace)
                                    {
                                        case ParticleSystemSimulationSpace.Local:
                                            {
                                                position = simulationSpaceTransform.position;
                                                rotation = simulationSpaceTransform.rotation;
                                                localScale = simulationSpaceTransform.localScale;

                                                break;
                                            }
                                        case ParticleSystemSimulationSpace.Custom:
                                            {
                                                simulationSpaceTransform = customSimulationSpaceTransform;

                                                position = simulationSpaceTransform.position;
                                                rotation = simulationSpaceTransform.rotation;
                                                localScale = simulationSpaceTransform.localScale;

                                                break;
                                            }
                                        default:
                                            {
                                                throw new System.NotSupportedException(

                                                    string.Format("Unsupported scaling mode '{0}'.", simulationSpace));
                                            }
                                    }

                                    // I put these here so I can take out the default exception case.
                                    // Else I'd have a compiler error for potentially unassigned variables.

                                    Vector3 p1_position = Vector3.zero;
                                    Vector3 p2_position = Vector3.zero;

                                    for (int i = 0; i < particleCount; i++)
                                    {
                                        if (lrIndex == maxLineRenderers)
                                        {
                                            break;
                                        }

                                        switch (simulationSpace)
                                        {
                                            case ParticleSystemSimulationSpace.Local:
                                            case ParticleSystemSimulationSpace.Custom:
                                                {
                                                    switch (scalingMode)
                                                    {
                                                        case ParticleSystemScalingMode.Hierarchy:
                                                            {
                                                                p1_position = simulationSpaceTransform.TransformPoint(particles[i].position);

                                                                break;
                                                            }
                                                        case ParticleSystemScalingMode.Local:
                                                            {
                                                                // Order is important.

                                                                p1_position = Vector3.Scale(particles[i].position, localScale);

                                                                p1_position = rotation * p1_position;
                                                                p1_position = p1_position + position;

                                                                break;
                                                            }
                                                        case ParticleSystemScalingMode.Shape:
                                                            {
                                                                // Order is important.

                                                                p1_position = rotation * particles[i].position;
                                                                p1_position = p1_position + position;

                                                                break;
                                                            }
                                                        default:
                                                            {
                                                                throw new System.NotSupportedException(

                                                                    string.Format("Unsupported scaling mode '{0}'.", scalingMode));
                                                            }
                                                    }

                                                    break;
                                                }
                                        }

                                        Color particleColour = particles[i].GetCurrentColor(particleSystem);

                                        Color lineStartColour = Color.LerpUnclamped(lineRendererStartColour, particleColour, colourFromParticle);
                                        lineStartColour.a = Mathf.LerpUnclamped(lineRendererStartColour.a, particleColour.a, alphaFromParticle);

                                        float lineStartWidth = Mathf.LerpUnclamped(lineRendererStartWidth, particles[i].GetCurrentSize(particleSystem), widthFromParticle);

                                        int connections = 0;

                                        for (int j = i + 1; j < particleCount; j++)
                                        {
                                            // Note that because particles array is not sorted by distance,
                                            // but rather by spawn time (I think), the connections made are 
                                            // not necessarily the closest.

                                            switch (simulationSpace)
                                            {
                                                case ParticleSystemSimulationSpace.Local:
                                                case ParticleSystemSimulationSpace.Custom:
                                                    {
                                                        switch (scalingMode)
                                                        {
                                                            case ParticleSystemScalingMode.Hierarchy:
                                                                {
                                                                    p2_position = simulationSpaceTransform.TransformPoint(particles[j].position);

                                                                    break;
                                                                }
                                                            case ParticleSystemScalingMode.Local:
                                                                {
                                                                    // Order is important.

                                                                    p2_position = Vector3.Scale(particles[j].position, localScale);

                                                                    p2_position = rotation * p2_position;
                                                                    p2_position = p2_position + position;

                                                                    break;
                                                                }
                                                            case ParticleSystemScalingMode.Shape:
                                                                {
                                                                    // Order is important.

                                                                    p2_position = rotation * particles[j].position;
                                                                    p2_position = p2_position + position;

                                                                    break;
                                                                }
                                                            default:
                                                                {
                                                                    throw new System.NotSupportedException(

                                                                        string.Format("Unsupported scaling mode '{0}'.", scalingMode));
                                                                }
                                                        }

                                                        break;
                                                    }
                                            }

                                            Vector3 p1p2_difference = new Vector3(

                                                p1_position.x - p2_position.x,
                                                p1_position.y - p2_position.y,
                                                p1_position.z - p2_position.z);

                                            // Note that distance is always calculated in WORLD SPACE.
                                            // Scaling the particle system will stretch the distances
                                            // and may require adjusting the maxDistance value.

                                            // I could also do it in local space (which may actually make more
                                            // sense) by just getting the difference of the positions without
                                            // all the transformations. This also provides opportunity for 
                                            // optimization as I can limit the world space transform calculations
                                            // to only happen if a particle is within range. 

                                            // Think about: Putting in a bool to switch between the two?

                                            float distanceSqr = Vector3.SqrMagnitude(p1p2_difference);

                                            // If distance to particle within range, add new vertex position.

                                            // The larger the max distance, the quicker connections will
                                            // reach its max, terminating the loop earlier. So even though more lines have
                                            // to be drawn, it's still faster to have a larger maxDistance value because
                                            // the call to Vector3.Distance() is expensive.

                                            if (distanceSqr <= maxDistanceSqr)
                                            {
                                                LineRenderer lr;

                                                if (lrIndex == lineRenderersCount)
                                                {
                                                    lr = Instantiate(lineRendererTemplate, _transform, false);

                                                    lineRenderers.Add(lr);
                                                    lineRenderersCount++;
                                                }

                                                lr = lineRenderers[lrIndex]; lr.enabled = true;

                                                lr.SetPosition(0, p1_position);
                                                lr.SetPosition(1, p2_position);

                                                lr.startColor = lineStartColour;

                                                particleColour = particles[j].GetCurrentColor(particleSystem);

                                                Color lineEndColour = Color.LerpUnclamped(lineRendererEndColour, particleColour, colourFromParticle);
                                                lineEndColour.a = Mathf.LerpUnclamped(lineRendererEndColour.a, particleColour.a, alphaFromParticle);

                                                lr.endColor = lineEndColour;

                                                float particleWidth = particles[i].GetCurrentSize(particleSystem);

                                                lr.startWidth = lineStartWidth;
                                                lr.endWidth = Mathf.LerpUnclamped(lineRendererEndWidth, particles[j].GetCurrentSize(particleSystem), widthFromParticle);

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
                            }

                            // Disable remaining line renderers from the pool that weren't used.

                            for (int i = lrIndex; i < lineRenderersCount; i++)
                            {
                                if (lineRenderers[i].enabled)
                                {
                                    lineRenderers[i].enabled = false;
                                }
                            }
                        }
                    }
                }

                // =================================	
                // End functions.
                // =================================

            }

            // =================================	
            // End namespace.
            // =================================

        }

    }

}

// =================================	
// --END-- //
// =================================
