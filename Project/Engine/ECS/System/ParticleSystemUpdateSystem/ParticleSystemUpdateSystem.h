#pragma once

#include "../Interface/ECSISystem.h"

namespace ONEngine {

    class ParticleSystemUpdateSystem : public ECSISystem {
    public:
        ParticleSystemUpdateSystem();
        ~ParticleSystemUpdateSystem() override = default;

        void RuntimeUpdate(class ECSGroup* _ecs) override;
    };

}
