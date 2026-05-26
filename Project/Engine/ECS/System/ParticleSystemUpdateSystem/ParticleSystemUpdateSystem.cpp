#include "ParticleSystemUpdateSystem.h"
#include "Engine/ECS/EntityComponentSystem/ECSGroup.h"
#include "Engine/ECS/Component/Components/ComputeComponents/ParticleSystem/ParticleSystem.h"
#include "Engine/ECS/Component/Components/ComputeComponents/Transform/Transform.h"
#include "Engine/Core/Utility/Time/Time.h"
#include "Engine/Core/Utility/Tools/Random.h"
#include "Engine/Core/Utility/Tools/Gizmo.h"
#include <numbers>
#include <cmath>
#include <algorithm>

namespace ONEngine {

    ParticleSystemUpdateSystem::ParticleSystemUpdateSystem() : ECSISystem() {}

    void ParticleSystemUpdateSystem::OutsideOfRuntimeUpdate(class ECSGroup* _ecs) {
        DrawGizmos(_ecs);
    }

    void ParticleSystemUpdateSystem::DrawGizmos(class ECSGroup* _ecs) {
        auto& entities = _ecs->GetEntities();
        for (auto& entityPtr : entities) {
            GameEntity* entity = entityPtr.get();
            if (!entity || !entity->active) continue;

            auto* ps = entity->GetComponent<ParticleSystem>();
            if (!ps || !ps->enable) continue;

            auto* transform = entity->GetComponent<ONEngine::Transform>();
            if (!transform) continue;

            const auto& shape = ps->shape;
            if (!shape.enabled) continue;

            Matrix4x4 worldMat = transform->matWorld;
            Vector3 center(worldMat.m[3][0], worldMat.m[3][1], worldMat.m[3][2]);
            Vector4 color = Vector4(1.0f, 1.0f, 0.0f, 0.5f); // Yellowish

            switch (shape.type) {
                case ParticleSystemShapeType::Sphere:
                    Gizmo::DrawWireSphere(center, shape.radius, color);
                    break;
                case ParticleSystemShapeType::Hemisphere:
                    // Just draw sphere for now, maybe add half sphere later
                    Gizmo::DrawWireSphere(center, shape.radius, color);
                    break;
                case ParticleSystemShapeType::Box:
                    Gizmo::DrawWireCube(center, shape.boxScale, color);
                    break;
                case ParticleSystemShapeType::Cone: {
                    float angleRad = shape.angle * 3.14159f / 180.0f;
                    float sinAngle = std::sin(angleRad);
                    float cosAngle = std::cos(angleRad);
                    float topRadius = shape.radius + std::tan(angleRad); // Approximation
                    
                    // Draw circles and lines for cone
                    int segments = 16;
                    for (int i = 0; i < segments; ++i) {
                        float theta1 = (float)i / segments * 2.0f * 3.14159f;
                        float theta2 = (float)(i + 1) / segments * 2.0f * 3.14159f;

                        Vector3 p1 = Vector3(std::cos(theta1), std::sin(theta1), 0) * shape.radius;
                        Vector3 p2 = Vector3(std::cos(theta2), std::sin(theta2), 0) * shape.radius;
                        
                        Vector3 p3 = Vector3(std::cos(theta1), std::sin(theta1), 1) * topRadius;
                        Vector3 p4 = Vector3(std::cos(theta2), std::sin(theta2), 1) * topRadius;

                        Gizmo::DrawLine(Matrix4x4::Transform(p1, worldMat), Matrix4x4::Transform(p2, worldMat), color);
                        Gizmo::DrawLine(Matrix4x4::Transform(p3, worldMat), Matrix4x4::Transform(p4, worldMat), color);
                        if (i % 4 == 0) {
                            Gizmo::DrawLine(Matrix4x4::Transform(p1, worldMat), Matrix4x4::Transform(p3, worldMat), color);
                        }
                    }
                    break;
                }
                case ParticleSystemShapeType::Circle: {
                    int segments = 16;
                    float arcRad = shape.arc * 3.14159f / 180.0f;
                    for (int i = 0; i < segments; ++i) {
                        float theta1 = (float)i / segments * arcRad;
                        float theta2 = (float)(i + 1) / segments * arcRad;

                        Vector3 p1 = Vector3(std::cos(theta1), std::sin(theta1), 0) * shape.radius;
                        Vector3 p2 = Vector3(std::cos(theta2), std::sin(theta2), 0) * shape.radius;
                        Gizmo::DrawLine(Matrix4x4::Transform(p1, worldMat), Matrix4x4::Transform(p2, worldMat), color);
                    }
                    if (shape.arc < 360.0f) {
                        Gizmo::DrawLine(center, Matrix4x4::Transform(Vector3(shape.radius, 0, 0), worldMat), color);
                        Gizmo::DrawLine(center, Matrix4x4::Transform(Vector3(std::cos(arcRad), std::sin(arcRad), 0) * shape.radius, worldMat), color);
                    }
                    break;
                }
                case ParticleSystemShapeType::Edge: {
                    Vector3 p1 = Vector3(-shape.radius, 0, 0);
                    Vector3 p2 = Vector3(shape.radius, 0, 0);
                    Gizmo::DrawLine(Matrix4x4::Transform(p1, worldMat), Matrix4x4::Transform(p2, worldMat), color);
                    break;
                }
            }
        }
    }

    static float GetMinMaxFloat(const MinMaxFloat& mmf) {
        if (mmf.state == MinMaxState::Constant) return mmf.constant;
        return Random::Float(mmf.minVal, mmf.maxVal);
    }

    static Color GetMinMaxColor(const MinMaxColor& mmc) {
        if (mmc.state == MinMaxState::Constant) return mmc.constant;
        return Color(
            Random::Float(mmc.minVal.r, mmc.maxVal.r),
            Random::Float(mmc.minVal.g, mmc.maxVal.g),
            Random::Float(mmc.minVal.b, mmc.maxVal.b),
            Random::Float(mmc.minVal.a, mmc.maxVal.a)
        );
    }

    static float EvaluateMinMaxCurve(const MinMaxCurve& mmc, float time) {
        switch (mmc.state) {
            case MinMaxState::Constant: return mmc.constant;
            case MinMaxState::Curve: return mmc.curve.Evaluate(time);
            case MinMaxState::RandomBetweenTwoCurves: {
                float vMin = mmc.curveMin.Evaluate(time);
                float vMax = mmc.curveMax.Evaluate(time);
                return vMin + (vMax - vMin) * 0.5f; 
            }
            default: return mmc.constant;
        }
    }

    static Color EvaluateMinMaxGradient(const MinMaxGradient& mmg, float time) {
        switch (mmg.state) {
            case MinMaxState::Constant: return mmg.gradient.Evaluate(time);
            case MinMaxState::Curve: return mmg.gradient.Evaluate(time);
            case MinMaxState::RandomBetweenTwoCurves: {
                Color cMin = mmg.gradientMin.Evaluate(time);
                Color cMax = mmg.gradientMax.Evaluate(time);
                return Color(
                    cMin.r + (cMax.r - cMin.r) * 0.5f,
                    cMin.g + (cMax.g - cMin.g) * 0.5f,
                    cMin.b + (cMax.b - cMin.b) * 0.5f,
                    cMin.a + (cMax.a - cMin.a) * 0.5f
                );
            }
            default: return Color::kWhite;
        }
    }

    // Returns position relative to the shape center, and the starting velocity direction
    static void EvaluateShape(const ParticleSystemShape& shape, Vector3& outPos, Vector3& outDir) {
        if (!shape.enabled) {
            outPos = Vector3::Zero;
            outDir = Vector3(0, 0, 1);
            return;
        }

        switch (shape.type) {
            case ParticleSystemShapeType::Sphere: {
                outDir = Random::InsideUnitSphere();
                float lengthSq = outDir.LengthSquared();
                if (lengthSq > 0.0001f) {
                    outDir = outDir / std::sqrt(lengthSq);
                } else {
                    outDir = Vector3(0, 0, 1);
                }
                float t = Random::Float(1.0f - shape.radiusThickness, 1.0f);
                outPos = outDir * (shape.radius * t);
                break;
            }
            case ParticleSystemShapeType::Hemisphere: {
                outDir = Random::InsideUnitSphere();
                outDir.z = std::abs(outDir.z); 
                float lengthSq = outDir.LengthSquared();
                if (lengthSq > 0.0001f) {
                    outDir = outDir / std::sqrt(lengthSq);
                } else {
                    outDir = Vector3(0, 0, 1);
                }
                float t = Random::Float(1.0f - shape.radiusThickness, 1.0f);
                outPos = outDir * (shape.radius * t);
                break;
            }
            case ParticleSystemShapeType::Box: {
                outPos = Vector3(
                    Random::Float(-shape.boxScale.x * 0.5f, shape.boxScale.x * 0.5f),
                    Random::Float(-shape.boxScale.y * 0.5f, shape.boxScale.y * 0.5f),
                    Random::Float(-shape.boxScale.z * 0.5f, shape.boxScale.z * 0.5f)
                );
                outDir = Vector3(0, 0, 1); // Box normally emits in Z direction
                break;
            }
            case ParticleSystemShapeType::Cone: {
                float r = Random::Float(0.0f, 1.0f);
                float theta = Random::Float(0.0f, shape.arc) * 3.14159f / 180.0f;
                float angleRad = shape.angle * 3.14159f / 180.0f;
                
                float sinAngle = std::sin(angleRad);
                float cosAngle = std::cos(angleRad);

                outDir = Vector3(r * std::cos(theta) * sinAngle, r * std::sin(theta) * sinAngle, cosAngle).Normalize();
                outPos = outDir * (shape.radius * Random::Float(1.0f - shape.radiusThickness, 1.0f));
                break;
            }
            case ParticleSystemShapeType::Circle: {
                float theta = Random::Float(0.0f, shape.arc) * 3.14159f / 180.0f;
                float r = std::sqrt(Random::Float(0.0f, 1.0f)); 
                float t = Random::Float(1.0f - shape.radiusThickness, 1.0f);
                r *= t * shape.radius;
                
                outPos = Vector3(r * std::cos(theta), r * std::sin(theta), 0.0f);
                outDir = Vector3(0, 0, 1); 
                break;
            }
            case ParticleSystemShapeType::Edge: {
                outPos = Vector3(Random::Float(-shape.radius, shape.radius), 0, 0);
                outDir = Vector3(0, 0, 1);
                break;
            }
            default:
                outPos = Vector3::Zero;
                outDir = Vector3(0, 0, 1);
                break;
        }
    }

    void ParticleSystemUpdateSystem::RuntimeUpdate(ECSGroup* _ecs) {
        DrawGizmos(_ecs);

        float dt = Time::DeltaTime();
        if (dt <= 0.0f) return;

        auto& entities = _ecs->GetEntities();
        for (auto& entityPtr : entities) {
            GameEntity* entity = entityPtr.get();
            if (!entity || !entity->active) continue;

            auto* ps = entity->GetComponent<ParticleSystem>();
            if (!ps || !ps->enable) continue;

            auto* transform = entity->GetComponent<ONEngine::Transform>();
            if (!transform) continue;

            if (ps->main.playOnAwake && ps->GetTime() == 0.0f && !ps->IsPlaying() && !ps->IsPaused()) {
                ps->Play();
            }

            if (ps->IsPlaying() && !ps->IsPaused()) {
                ps->UpdateTime(dt);
            }

            bool isSimulating = ps->IsPlaying() && !ps->IsPaused();

            if (ps->particles.size() != static_cast<size_t>(ps->main.maxParticles)) {
                ps->particles.resize(ps->main.maxParticles);
                if (ps->aliveCount > ps->particles.size()) ps->aliveCount = ps->particles.size();
            }

            if (ps->burstCycleCounts.size() != ps->emission.bursts.size()) {
                ps->burstCycleCounts.resize(ps->emission.bursts.size(), 0);
            }

            float currentPlaybackTime = ps->GetTime();

            // --- Emission ---
            if (isSimulating && ps->emission.enabled && currentPlaybackTime > GetMinMaxFloat(ps->main.startDelay)) {
                    ps->emitAccumulator += ps->emission.rateOverTime * dt;
                    int emitCount = static_cast<int>(ps->emitAccumulator);
                    ps->emitAccumulator -= static_cast<float>(emitCount);

                    for (int i = 0; i < emitCount; ++i) {
                        if (ps->aliveCount >= ps->particles.size()) break;

                        Particle& p = ps->particles[ps->aliveCount++];
                        Vector3 shapePos, shapeDir;
                        EvaluateShape(ps->shape, shapePos, shapeDir);

                        // Interpolated Matrix for smoothing fast movement
                        float t_emit = (float)i / (float)(emitCount > 1 ? emitCount - 1 : 1);
                        Matrix4x4 currentMat = transform->matWorld;
                        Matrix4x4 emitMat = currentMat;
                        if (ps->hasPreviousWorldMat) {
                            // Simple linear interpolation of position for now
                            Vector3 prevPos(ps->previousWorldMat.m[3][0], ps->previousWorldMat.m[3][1], ps->previousWorldMat.m[3][2]);
                            Vector3 currPos(currentMat.m[3][0], currentMat.m[3][1], currentMat.m[3][2]);
                            Vector3 lerpedPos = Vector3::Lerp(prevPos, currPos, t_emit);
                            emitMat.m[3][0] = lerpedPos.x;
                            emitMat.m[3][1] = lerpedPos.y;
                            emitMat.m[3][2] = lerpedPos.z;
                        }

                        if (ps->main.simulationSpace == SimulationSpace::World) {
                            p.position = Matrix4x4::Transform(shapePos, emitMat);
                            p.velocity = Matrix4x4::TransformNormal(shapeDir, emitMat).Normalize() * GetMinMaxFloat(ps->main.startSpeed);
                        } else {
                            p.position = shapePos;
                            p.velocity = shapeDir * GetMinMaxFloat(ps->main.startSpeed);
                        }

                        p.baseVelocity = p.velocity;
                        p.startLifetime = GetMinMaxFloat(ps->main.startLifetime);
                        p.remainingLifetime = p.startLifetime;
                        p.startColor = GetMinMaxColor(ps->main.startColor);
                        p.color = p.startColor;
                        p.startSize = GetMinMaxFloat(ps->main.startSize);
                        p.size = p.startSize;
                        p.rotation = GetMinMaxFloat(ps->main.startRotation);
                    }

                for (size_t i = 0; i < ps->emission.bursts.size(); ++i) {
                    auto& burst = ps->emission.bursts[i];
                    int& cycleCount = ps->burstCycleCounts[i];
                    
                    float burstTime = burst.time;
                    if (currentPlaybackTime >= burstTime && cycleCount < burst.cycles) {
                        float nextBurstTime = burstTime + static_cast<float>(cycleCount) * burst.interval;
                        if (currentPlaybackTime >= nextBurstTime) {
                            if (Random::Float(0.0f, 1.0f) <= burst.probability) {
                                int emitCount = burst.count;
                                for (int e = 0; e < emitCount; ++e) {
                                    if (ps->aliveCount >= ps->particles.size()) break;
                                    Particle& p = ps->particles[ps->aliveCount++];
                                    Vector3 shapePos, shapeDir;
                                    EvaluateShape(ps->shape, shapePos, shapeDir);
                                    
                                    Matrix4x4 worldMat = transform->matWorld;
                                    if (ps->main.simulationSpace == SimulationSpace::World) {
                                        p.position = Matrix4x4::Transform(shapePos, worldMat);
                                        p.velocity = Matrix4x4::TransformNormal(shapeDir, worldMat).Normalize() * GetMinMaxFloat(ps->main.startSpeed);
                                    } else {
                                        p.position = shapePos;
                                        p.velocity = shapeDir * GetMinMaxFloat(ps->main.startSpeed);
                                    }

                                    p.startLifetime = GetMinMaxFloat(ps->main.startLifetime);
                                    p.remainingLifetime = p.startLifetime;
                                    p.startColor = GetMinMaxColor(ps->main.startColor);
                                    p.color = p.startColor;
                                    p.startSize = GetMinMaxFloat(ps->main.startSize);
                                    p.size = p.startSize;
                                    p.rotation = GetMinMaxFloat(ps->main.startRotation);
                                }
                            }
                            cycleCount++;
                        }
                    }
                }
            }

            if (isSimulating && currentPlaybackTime > ps->main.duration) {
                if (ps->main.looping) {
                    ps->ResetTime(currentPlaybackTime - ps->main.duration);
                    std::fill(ps->burstCycleCounts.begin(), ps->burstCycleCounts.end(), 0);
                } else {
                    ps->Stop();
                }
            }

            // --- Update ---
            if (!ps->IsPaused() && ps->IsPlaying()) {
                Vector3 gravity = Vector3(0.0f, -9.81f, 0.0f) * ps->main.gravityModifier;
                
                for (size_t i = 0; i < ps->aliveCount; ) {
                    Particle& p = ps->particles[i];
                    p.remainingLifetime -= dt;

                    if (p.remainingLifetime <= 0.0f) {
                        if (ps->aliveCount > 1) {
                            p = ps->particles[ps->aliveCount - 1];
                        }
                        ps->aliveCount--;
                    } else {
                        // Lifetime Modifiers
                        float normalizedTime = 1.0f - (p.remainingLifetime / p.startLifetime);
                        normalizedTime = std::clamp(normalizedTime, 0.0f, 1.0f);

                        // Velocity Over Lifetime
                        if (ps->velocityOverLifetime.enabled) {
                            Vector3 linearVelocity(
                                EvaluateMinMaxCurve(ps->velocityOverLifetime.x, normalizedTime),
                                EvaluateMinMaxCurve(ps->velocityOverLifetime.y, normalizedTime),
                                EvaluateMinMaxCurve(ps->velocityOverLifetime.z, normalizedTime)
                            );

                            float speedMultiplier = EvaluateMinMaxCurve(ps->velocityOverLifetime.speedModifier, normalizedTime);

                            if (ps->velocityOverLifetime.space == SimulationSpace::World) {
                                p.velocity = p.baseVelocity * speedMultiplier + linearVelocity;
                            } else {
                                // Transform linear velocity from local to world if space is local
                                Matrix4x4 worldMat = transform->matWorld;
                                Vector3 worldLinear = Matrix4x4::TransformNormal(linearVelocity, worldMat);
                                p.velocity = p.baseVelocity * speedMultiplier + worldLinear;
                            }
                        }

                        p.velocity += gravity * dt;
                        p.position += p.velocity * dt;

                        // Color Over Lifetime
                        if (ps->colorOverLifetime.enabled) {
                            Color overLifeColor = EvaluateMinMaxGradient(ps->colorOverLifetime.color, normalizedTime);
                            p.color.r = p.startColor.r * overLifeColor.r;
                            p.color.g = p.startColor.g * overLifeColor.g;
                            p.color.b = p.startColor.b * overLifeColor.b;
                            p.color.a = p.startColor.a * overLifeColor.a;
                        } else {
                            p.color = p.startColor;
                        }

                        // Size Over Lifetime
                        if (ps->sizeOverLifetime.enabled) {
                            float sizeMultiplier = EvaluateMinMaxCurve(ps->sizeOverLifetime.size, normalizedTime);
                            p.size = p.startSize * sizeMultiplier;
                        } else {
                            p.size = p.startSize;
                        }

                        i++;
                    }
                }
            }

            // Update previous transform for next frame's interpolation
            ps->previousWorldMat = transform->matWorld;
            ps->hasPreviousWorldMat = true;
        }
    }

}
