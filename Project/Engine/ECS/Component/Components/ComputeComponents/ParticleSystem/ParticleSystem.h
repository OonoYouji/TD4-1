#pragma once

#include "../../Interface/IComponent.h"
#include "ParticleSystemData.h"

namespace ONEngine {

    // CPU-side particle state
    struct Particle {
        Vector3 position;
        Vector3 velocity;
        Color color;
        float startLifetime;
        float remainingLifetime;
        float size;
        float rotation;
    };

    class ParticleSystem : public IComponent {
    public:
        ParticleSystem();
        ~ParticleSystem() override = default;

        // --- Controls ---
        void Play();
        void Stop();
        void Clear();
        void Pause();

        void UpdateTime(float _dt) { playbackTime_ += _dt; }
        void ResetTime(float _t = 0.0f) { playbackTime_ = _t; }

        // --- Getters ---
        bool IsPlaying() const { return isPlaying_; }
        bool IsPaused() const { return isPaused_; }
        float GetTime() const { return playbackTime_; }

        // --- Modules ---
        ParticleSystemMain main;
        ParticleSystemEmission emission;
        ParticleSystemShape shape;
        ParticleSystemColorOverLifetime colorOverLifetime;
        ParticleSystemSizeOverLifetime sizeOverLifetime;
        ParticleSystemRenderer renderer;

        // --- CPU Simulation State ---
        std::vector<Particle> particles;
        size_t aliveCount = 0;
        float emitAccumulator = 0.0f;
        std::vector<int> burstCycleCounts; // Track how many times a burst has fired

    private:
        bool isPlaying_ = false;
        bool isPaused_ = false;
        float playbackTime_ = 0.0f;

        // GPU related resources will be added here in Phase 4
    };

}
