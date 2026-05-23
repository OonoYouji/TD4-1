#include "ParticleSystemUpdateSystem.h"
#include "Engine/ECS/EntityComponentSystem/ECSGroup.h"
#include "Engine/ECS/Component/Components/ComputeComponents/ParticleSystem/ParticleSystem.h"
#include "Engine/ECS/Component/Components/ComputeComponents/Transform/Transform.h"
#include "Engine/Core/Utility/Time/Time.h"
#include "Engine/Core/Utility/Tools/Random.h"
#include <numbers>
#include <cmath>
#include <algorithm>

namespace ONEngine {

    ParticleSystemUpdateSystem::ParticleSystemUpdateSystem() : ECSISystem() {}

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
            default:
                outPos = Vector3::Zero;
                outDir = Vector3(0, 0, 1);
                break;
        }
    }

    void ParticleSystemUpdateSystem::RuntimeUpdate(ECSGroup* _ecs) {
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
                if (ps->emission.rateOverTime > 0.0f) {
                    ps->emitAccumulator += ps->emission.rateOverTime * dt;
                    int emitCount = static_cast<int>(ps->emitAccumulator);
                    ps->emitAccumulator -= static_cast<float>(emitCount);

                    for (int i = 0; i < emitCount; ++i) {
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
                        p.color = GetMinMaxColor(ps->main.startColor);
                        p.size = GetMinMaxFloat(ps->main.startSize);
                        p.rotation = GetMinMaxFloat(ps->main.startRotation);
                    }
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
                                    p.color = GetMinMaxColor(ps->main.startColor);
                                    p.size = GetMinMaxFloat(ps->main.startSize);
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
                        p.velocity += gravity * dt;
                        p.position += p.velocity * dt;
                        i++;
                    }
                }
            }
        }
    }

}
