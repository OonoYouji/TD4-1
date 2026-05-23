#include "ParticleSystem.h"

namespace ONEngine {

    ParticleSystem::ParticleSystem() {
        // Initialize default state
    }

    void ParticleSystem::Play() {
        isPlaying_ = true;
        isPaused_ = false;
        playbackTime_ = 0.0f;
    }

    void ParticleSystem::Stop() {
        isPlaying_ = false;
        isPaused_ = false;
    }

    void ParticleSystem::Clear() {
        playbackTime_ = 0.0f;
        // Also clear GPU buffers in the future
    }

    void ParticleSystem::Pause() {
        isPaused_ = true;
    }

}
