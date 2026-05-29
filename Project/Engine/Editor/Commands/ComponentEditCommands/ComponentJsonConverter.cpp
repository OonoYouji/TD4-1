#include "ComponentJsonConverter.h"

/// std
#include <unordered_map>
#include <typeindex>
#include <functional>

#include "Engine/ECS/Component/Collection/ComponentHash.h"

/// engine/compute
#include "Engine/ECS/Component/Components/ComputeComponents/Terrain/Terrain.h"
#include "Engine/ECS/Component/Components/ComputeComponents/Terrain/Grass/GrassField.h"
#include "Engine/ECS/Component/Components/ComputeComponents/Terrain/TerrainCollider.h"
#include "Engine/ECS/Component/Components/ComputeComponents/VoxelTerrain/VoxelTerrain.h"
#include "Engine/ECS/Component/Components/ComputeComponents/Camera/CameraComponent.h"
#include "Engine/ECS/Component/Components/ComputeComponents/Collision/SphereCollider.h"
#include "Engine/ECS/Component/Components/ComputeComponents/Collision/BoxCollider.h"
#include "Engine/ECS/Component/Components/ComputeComponents/Script/Script.h"
#include "Engine/ECS/Component/Components/ComputeComponents/Audio/AudioSource.h"
#include "Engine/ECS/Component/Components/ComputeComponents/ParticleSystem/ParticleSystem.h"
#include "Engine/ECS/Component/Components/ComputeComponents/Variables/Variables.h"
#include "Engine/ECS/Component/Components/ComputeComponents/ShadowCaster/ShadowCaster.h"
#include "Engine/ECS/Component/Components/ComputeComponents/Agent/AgentIntentComponent.h"
#include "Engine/ECS/Component/Components/ComputeComponents/Animator/Animator.h"
#include "Engine/ECS/Component/Components/ComputeComponents/Animation/AnimationPlayer.h"

/// engine/renderer
#include "Engine/ECS/Component/Components/RendererComponents/Skybox/Skybox.h"
#include "Engine/ECS/Component/Components/RendererComponents/Sprite/SpriteRenderer.h"
#include "Engine/ECS/Component/Components/RendererComponents/Mesh/MeshRenderer.h"
#include "Engine/ECS/Component/Components/RendererComponents/Mesh/DissolveMeshRenderer.h"
#include "Engine/ECS/Component/Components/RendererComponents/SkinMesh/SkinMeshRenderer.h"
#include "Engine/ECS/Component/Components/RendererComponents/ScreenPostEffectTag/ScreenPostEffectTag.h"


using namespace ONEngine;


/// //////////////////////////////////////////////////
/// ComponentJsonConverter
/// //////////////////////////////////////////////////

namespace {
	class JsonConverter {
	public:

		JsonConverter() {

			/// compute
			Register<Transform>();
			Register<Variables>();
			Register<DirectionalLight>();
			Register<AudioSource>();
			Register<Effect>();
			Register<ParticleSystem>();
			Register<Script>();
			Register<Terrain>();
			Register<GrassField>();
			Register<TerrainCollider>();
			Register<CameraComponent>();
			Register<ShadowCaster>();
			Register<VoxelTerrain>();
			Register<AgentIntentComponent>();
			Register<Animator>();
			Register<AnimationPlayer>();

			/// renderer
			Register<SpriteRenderer>();
			Register<CustomMeshRenderer>();
			Register<MeshRenderer>();
			Register<DissolveMeshRenderer>();
			Register<SkinMeshRenderer>();
			Register<Line2DRenderer>();
			Register<Line3DRenderer>();
			Register<ScreenPostEffectTag>();
			Register<Skybox>();

			/// collision
			Register<SphereCollider>();
			Register<BoxCollider>();
		}

		template <typename T>
		void Register() {
			std::string typeName = GetComponentTypeName<T>();

			converters_[typeName] = [](const IComponent* _component) {
				return *static_cast<const T*>(_component);
				};

			fromJsonConverters_[typeName] = [this](IComponent* _component, const nlohmann::json& _j) {
				uint32_t id = _component->id;
 				*static_cast<T*>(_component) = _j.get<T>();
				_component->id = id;
				};
		}

		using Converter = std::function<nlohmann::json(const IComponent*)>;
		using FromJsonConverter = std::function<void(IComponent*, const nlohmann::json&)>;
		std::unordered_map<std::string, Converter> converters_;
		std::unordered_map<std::string, FromJsonConverter> fromJsonConverters_;

	};

	JsonConverter jsonConverter;
}

nlohmann::json ComponentJsonConverter::ToJson(const IComponent* _component) {
	std::string name = GetComponentTypeName(_component);

	auto itr = jsonConverter.converters_.find(name);
	if (itr == jsonConverter.converters_.end()) {
		return nlohmann::json{};
	}

	return itr->second(_component);
}

void ComponentJsonConverter::FromJson(const nlohmann::json& _j, IComponent* _component) {
	std::string name = GetComponentTypeName(_component);

	auto itr = jsonConverter.fromJsonConverters_.find(name);
	if (itr == jsonConverter.fromJsonConverters_.end()) {
		Console::Log("ComponentJsonConverter: " + name + "の変換関数が登録されていません。");
		return;
	}

	itr->second(_component, _j);
}

void ONEngine::from_json(const nlohmann::json& _j, Quaternion& _q) {
	_q.x = _j.at("x").get<float>();
	_q.y = _j.at("y").get<float>();
	_q.z = _j.at("z").get<float>();
	_q.w = _j.at("w").get<float>();
}

void ONEngine::to_json(nlohmann::json& _j, const Quaternion& _q) {
	_j = nlohmann::json{ { "x", _q.x }, { "y", _q.y }, { "z", _q.z }, { "w", _q.w } };
}

void ONEngine::from_json(const nlohmann::json& _j, Color& _c) {
	_c.r = _j.at("r").get<float>();
	_c.g = _j.at("g").get<float>();
	_c.b = _j.at("b").get<float>();
	_c.a = _j.at("a").get<float>();
}

void ONEngine::to_json(nlohmann::json& _j, const Color& _c) {
	_j = nlohmann::json{ { "r", _c.r }, { "g", _c.g }, { "b", _c.b }, { "a", _c.a } };
}


void ONEngine::from_json(const nlohmann::json& _j, DirectionalLight& _l) {
	_l.SetIntensity(_j.at("intensity").get<float>());
	_l.SetDirection(_j.at("direction").get<Vector3>());
	_l.SetColor(_j.at("color").get<Vector4>());
}

void ONEngine::to_json(nlohmann::json& _j, const DirectionalLight& _l) {
	_j = nlohmann::json{
		{ "type", "DirectionalLight" },
		{ "enable", _l.enable },
		{ "intensity", _l.GetIntensity() },
		{ "direction", _l.GetDirection() },
		{ "color", _l.GetColor() }
	};
}

void ONEngine::from_json(const nlohmann::json& _j, Effect& _e) {
	_e.enable = _j.at("enable").get<int>();
	_e.SetIsCreateParticle(_j.at("isCreateParticle").get<bool>());
	_e.SetMeshPath(_j.at("meshPath").get<std::string>());
	_e.SetTexturePath(_j.at("texturePath").get<std::string>());
	_e.SetMainModule(_j.at("mainModule").get<EffectMainModule>());
	_e.SetEmitShape(_j.at("emitShape").get<EffectEmitShape>());
	_e.SetEmitType(static_cast<Effect::EmitType>(_j.at("emitType").get<int>()));
	_e.SetEmitTypeDistance(_j.at("distanceEmitData").get<Effect::DistanceEmitData>());
	_e.SetEmitTypeTime(_j.at("timeEmitData").get<Effect::TimeEmitData>());
	_e.SetMaxEffectCount(_j.at("maxEffectCount").get<size_t>());
	_e.SetEmitInstanceCount(_j.at("emitInstanceCount").get<size_t>());
	_e.SetBlendMode(static_cast<Effect::BlendMode>(_j.at("blendMode").get<int>()));
}

void ONEngine::to_json(nlohmann::json& _j, const Effect& _e) {
	_j = nlohmann::json{
		{ "type", "Effect" },
		{ "enable", _e.enable },
		{ "isCreateParticle", _e.IsCreateParticle() },
		{ "meshPath", _e.GetMeshPath() },
		{ "texturePath", _e.GetTexturePath() },
		{ "mainModule", _e.GetMainModule() },
		{ "emitShape", _e.GetEmitShape() },
		{ "emitType", _e.GetEmitType() },
		{ "distanceEmitData", _e.GetDistanceEmitData() },
		{ "timeEmitData", _e.GetTimeEmitData() },
		{ "maxEffectCount", _e.GetMaxEffectCount() },
		{ "emitInstanceCount", _e.GetEmitInstanceCount() },
		{ "blendMode", static_cast<int>(_e.GetBlendMode()) },
	};
}

void ONEngine::from_json(const nlohmann::json& _j, Effect::DistanceEmitData& _e) {
	_e.emitDistance = _j.at("emitDistance").get<float>();
	_e.emitInterval = _j.at("emitInterval").get<float>();
}

void ONEngine::to_json(nlohmann::json& _j, const Effect::DistanceEmitData& _e) {
	_j = nlohmann::json{
		{ "emitDistance", _e.emitDistance },
		{ "emitInterval", _e.emitInterval }
	};
}

void ONEngine::from_json(const nlohmann::json& _j, Effect::TimeEmitData& _e) {
	_e.emitTime = _j.at("emitTime").get<float>();
	_e.emitInterval = _j.at("emitInterval").get<float>();
}

void ONEngine::to_json(nlohmann::json& _j, const Effect::TimeEmitData& _e) {
	_j = nlohmann::json{
		{ "emitTime", _e.emitTime },
		{ "emitInterval", _e.emitInterval }
	};
}

void ONEngine::from_json(const nlohmann::json& _j, EffectMainModule& _e) {
	_e.SetLifeLeftTime(_j.at("lifeLeftTime").get<float>());
	_e.SetSpeedStartData(_j.at("startSpeed").get<std::pair<float, float>>());
	_e.SetSizeStartData(_j.at("startSize").get<std::pair<Vector3, Vector3>>());
	_e.SetRotateStartData(_j.at("startRotate").get<std::pair<Vector3, Vector3>>());
	_e.SetColorStartData(_j.at("startColor").get<std::pair<Color, Color>>());
	_e.SetGravityModifier(_j.at("gravityModifier").get<float>());
}

void ONEngine::to_json(nlohmann::json& _j, const EffectMainModule& _e) {
	_j = nlohmann::json{
		{ "lifeLeftTime", _e.GetLifeLeftTime() },
		{ "startSpeed", _e.GetSpeedStartData() },
		{ "startSize", _e.GetSizeStartData() },
		{ "startRotate", _e.GetRotateStartData() },
		{ "startColor", _e.GetColorStartData() },
		{ "gravityModifier", _e.GetGravityModifier() }
	};
}

void ONEngine::from_json(const nlohmann::json& _j, EffectEmitShape& _e) {
	int type = _j.at("type").get<int>();
	switch (type) {
	case static_cast<int>(EffectEmitShape::ShapeType::Sphere):
		_e.SetSphere(_j.at("sphere").get<Sphere>());
		break;
	case static_cast<int>(EffectEmitShape::ShapeType::Cube):
		_e.SetCube(_j.at("cube").get<Cube>());
		break;
	case static_cast<int>(EffectEmitShape::ShapeType::Cone):
		_e.SetCone(_j.at("cone").get<Cone>());
		break;
	default:
		break;
	}
}

void ONEngine::to_json(nlohmann::json& _j, const EffectEmitShape& _e) {
	_j = nlohmann::json{
		{ "type", static_cast<int>(_e.GetType()) },
		{ "sphere", _e.GetSphere() },
		{ "cube", _e.GetCube() },
		{ "cone", _e.GetCone() }
	};
}


void ONEngine::from_json(const nlohmann::json& _j, CustomMeshRenderer& _m) {
	_m.enable = _j.at("enable").get<int>();
	_m.SetTexturePath(_j.at("texturePath").get<std::string>());
	_m.SetColor(_j.at("color").get<Color>());
}
void ONEngine::to_json(nlohmann::json& _j, const CustomMeshRenderer& _m) {
	_j = nlohmann::json{
		{ "type", "CustomMeshRenderer" },
		{ "enable", _m.enable },
		{ "texturePath", _m.GetTexturePath() },
		{ "color", _m.GetColor() }
	};
}

void ONEngine::from_json(const nlohmann::json& _j, Line2DRenderer& _l) {
	_l.enable = _j.at("enable").get<int>();
}

void ONEngine::to_json(nlohmann::json& _j, const Line2DRenderer& _l) {
	_j = nlohmann::json{
		{ "type", "Line2DRenderer" },
		{ "enable", _l.enable }
	};
}

void ONEngine::from_json(const nlohmann::json& _j, Line3DRenderer& _l) {
	if (!_j.contains("enable")) {
		_l.enable = _j.at("enable").get<int>();
	}
}

void ONEngine::to_json(nlohmann::json& _j, const Line3DRenderer& _l) {
	_j = nlohmann::json{
		{ "type", "Line3DRenderer" },
		{ "enable", _l.enable }
	};
}

// ParticleSystem

void ONEngine::from_json(const nlohmann::json& _j, ParticleSystem& _p) {
	_p.enable = _j.at("enable").get<int>();
	_p.main = _j.at("main").get<ParticleSystemMain>();
	_p.emission = _j.at("emission").get<ParticleSystemEmission>();
	_p.shape = _j.at("shape").get<ParticleSystemShape>();
	_p.colorOverLifetime = _j.at("colorOverLifetime").get<ParticleSystemColorOverLifetime>();
	_p.sizeOverLifetime = _j.at("sizeOverLifetime").get<ParticleSystemSizeOverLifetime>();
	_p.renderer = _j.at("renderer").get<ParticleSystemRenderer>();
}

void ONEngine::to_json(nlohmann::json& _j, const ParticleSystem& _p) {
	_j = nlohmann::json{
		{ "type", "ParticleSystem" },
		{ "enable", _p.enable },
		{ "main", _p.main },
		{ "emission", _p.emission },
		{ "shape", _p.shape },
		{ "colorOverLifetime", _p.colorOverLifetime },
		{ "sizeOverLifetime", _p.sizeOverLifetime },
		{ "renderer", _p.renderer },
	};
}

void ONEngine::from_json(const nlohmann::json& _j, MinMaxFloat& _m) {
	_m.state = static_cast<MinMaxState>(_j.at("state").get<uint8_t>());
	_m.constant = _j.at("constant").get<float>();
	_m.minVal = _j.at("min").get<float>();
	_m.maxVal = _j.at("max").get<float>();
}

void ONEngine::to_json(nlohmann::json& _j, const MinMaxFloat& _m) {
	_j = nlohmann::json{
		{ "state", static_cast<uint8_t>(_m.state) },
		{ "constant", _m.constant },
		{ "min", _m.minVal },
		{ "max", _m.maxVal }
	};
}

void ONEngine::from_json(const nlohmann::json& _j, MinMaxColor& _m) {
	_m.state = static_cast<MinMaxState>(_j.at("state").get<uint8_t>());
	_m.constant = _j.at("constant").get<Color>();
	_m.minVal = _j.at("min").get<Color>();
	_m.maxVal = _j.at("max").get<Color>();
}

void ONEngine::to_json(nlohmann::json& _j, const MinMaxColor& _m) {
	_j = nlohmann::json{
		{ "state", static_cast<uint8_t>(_m.state) },
		{ "constant", _m.constant },
		{ "min", _m.minVal },
		{ "max", _m.maxVal }
	};
}

void ONEngine::from_json(const nlohmann::json& _j, ParticleSystemMain& _m) {
	_m.duration = _j.at("duration").get<float>();
	_m.looping = _j.at("looping").get<bool>();
	_m.prewarm = _j.at("prewarm").get<bool>();
	_m.startDelay = _j.at("startDelay").get<MinMaxFloat>();
	_m.startLifetime = _j.at("startLifetime").get<MinMaxFloat>();
	_m.startSpeed = _j.at("startSpeed").get<MinMaxFloat>();
	_m.startSize = _j.at("startSize").get<MinMaxFloat>();
	_m.startRotation = _j.at("startRotation").get<MinMaxFloat>();
	_m.startColor = _j.at("startColor").get<MinMaxColor>();
	_m.gravityModifier = _j.at("gravityModifier").get<float>();
	_m.maxParticles = _j.at("maxParticles").get<int>();
	_m.playOnAwake = _j.at("playOnAwake").get<bool>();
}

void ONEngine::to_json(nlohmann::json& _j, const ParticleSystemMain& _m) {
	_j = nlohmann::json{
		{ "duration", _m.duration },
		{ "looping", _m.looping },
		{ "prewarm", _m.prewarm },
		{ "startDelay", _m.startDelay },
		{ "startLifetime", _m.startLifetime },
		{ "startSpeed", _m.startSpeed },
		{ "startSize", _m.startSize },
		{ "startRotation", _m.startRotation },
		{ "startColor", _m.startColor },
		{ "gravityModifier", _m.gravityModifier },
		{ "maxParticles", _m.maxParticles },
		{ "playOnAwake", _m.playOnAwake }
	};
}

void ONEngine::from_json(const nlohmann::json& _j, ParticleSystemEmission& _e) {
	_e.enabled = _j.at("enabled").get<bool>();
	_e.rateOverTime = _j.at("rateOverTime").get<float>();
	_e.rateOverDistance = _j.at("rateOverDistance").get<float>();
	_e.bursts = _j.at("bursts").get<std::vector<ParticleSystemEmission::Burst>>();
}

void ONEngine::to_json(nlohmann::json& _j, const ParticleSystemEmission& _e) {
	_j = nlohmann::json{
		{ "enabled", _e.enabled },
		{ "rateOverTime", _e.rateOverTime },
		{ "rateOverDistance", _e.rateOverDistance },
		{ "bursts", _e.bursts }
	};
}

void ONEngine::from_json(const nlohmann::json& _j, ParticleSystemEmission::Burst& _b) {
	_b.time = _j.at("time").get<float>();
	_b.count = _j.at("count").get<int>();
	_b.cycles = _j.at("cycles").get<int>();
	_b.interval = _j.at("interval").get<float>();
	_b.probability = _j.at("probability").get<float>();
}

void ONEngine::to_json(nlohmann::json& _j, const ParticleSystemEmission::Burst& _b) {
	_j = nlohmann::json{
		{ "time", _b.time },
		{ "count", _b.count },
		{ "cycles", _b.cycles },
		{ "interval", _b.interval },
		{ "probability", _b.probability }
	};
}

void ONEngine::from_json(const nlohmann::json& _j, ParticleSystemShape& _s) {
	_s.enabled = _j.at("enabled").get<bool>();
	_s.type = static_cast<ParticleSystemShapeType>(_j.at("type").get<uint8_t>());
	_s.radius = _j.at("radius").get<float>();
	_s.radiusThickness = _j.at("radiusThickness").get<float>();
	_s.arc = _j.at("arc").get<float>();
	_s.angle = _j.at("angle").get<float>();
	_s.boxScale = _j.at("boxScale").get<Vector3>();
}

void ONEngine::to_json(nlohmann::json& _j, const ParticleSystemShape& _s) {
	_j = nlohmann::json{
		{ "enabled", _s.enabled },
		{ "type", static_cast<uint8_t>(_s.type) },
		{ "radius", _s.radius },
		{ "radiusThickness", _s.radiusThickness },
		{ "arc", _s.arc },
		{ "angle", _s.angle },
		{ "boxScale", _s.boxScale }
	};
}

void ONEngine::from_json(const nlohmann::json& _j, ParticleSystemRenderer& _r) {
	_r.renderMode = static_cast<ParticleSystemRenderer::RenderMode>(_j.at("renderMode").get<uint8_t>());
	_r.materialGuid = _j.at("materialGuid").get<std::string>();
	_r.meshGuid = _j.at("meshGuid").get<std::string>();
}

void ONEngine::to_json(nlohmann::json& _j, const ParticleSystemRenderer& _r) {
	_j = nlohmann::json{
		{ "renderMode", static_cast<uint8_t>(_r.renderMode) },
		{ "materialGuid", _r.materialGuid },
		{ "meshGuid", _r.meshGuid }
	};
}

void ONEngine::from_json(const nlohmann::json& _j, ParticleSystemColorOverLifetime& _c) {
	_c.enabled = _j.at("enabled").get<bool>();
}

void ONEngine::to_json(nlohmann::json& _j, const ParticleSystemColorOverLifetime& _c) {
	_j = nlohmann::json{ { "enabled", _c.enabled } };
}

void ONEngine::from_json(const nlohmann::json& _j, ParticleSystemSizeOverLifetime& _s) {
	_s.enabled = _j.at("enabled").get<bool>();
}

void ONEngine::to_json(nlohmann::json& _j, const ParticleSystemSizeOverLifetime& _s) {
	_j = nlohmann::json{ { "enabled", _s.enabled } };
}

