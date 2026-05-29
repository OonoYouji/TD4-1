#include "AnimationClipLoader.h"

/// std
#include <fstream>
#include <filesystem>

/// external
#include <nlohmann/json.hpp>

using namespace ONEngine::Asset;

std::optional<AnimationClip> AssetLoader<AnimationClip>::Load(const std::string& _filepath, typename Meta<AnimationClip::MetaData> /*meta*/) {
    std::ifstream file(_filepath);
    if (!file.is_open()) return std::nullopt;

    nlohmann::json j;
    try {
        file >> j;
    } catch (...) {
        return std::nullopt;
    }

    AnimationClip clip;
    clip.name = j.value("name", "");
    clip.duration = j.value("duration", 0.0f);
    clip.isLooping = j.value("loop", false);

    if (j.contains("tracks")) {
        for (const auto& trackJson : j["tracks"]) {
            AnimationTrack track;
            track.componentName = trackJson.value("component", "");
            track.propertyPath = trackJson.value("property", "");

            if (trackJson.contains("keyframes")) {
                for (const auto& keyJson : trackJson["keyframes"]) {
                    AnimationKeyframe key;
                    key.time = keyJson.value("t", 0.0f);
                    key.interpolation = keyJson.value("in", "Linear");

                    auto v = keyJson["v"];
                    if (v.is_number()) {
                        key.value = v.get<float>();
                    } else if (v.is_array()) {
                        if (v.size() == 2) key.value = Vector2(v[0], v[1]);
                        else if (v.size() == 3) key.value = Vector3(v[0], v[1], v[2]);
                        else if (v.size() == 4) key.value = Vector4(v[0], v[1], v[2], v[3]);
                    }
                    track.keyframes.push_back(key);
                }
            }
            clip.tracks.push_back(track);
        }
    }

    return clip;
}

std::optional<AnimationClip> AssetLoader<AnimationClip>::Reload(const std::string& _filepath, AnimationClip* /*_src*/, typename Meta<AnimationClip::MetaData> meta) {
    return Load(_filepath, meta);
}

Meta<typename AnimationClip::MetaData> AssetLoader<AnimationClip>::GetMetaData(const std::string& /*_filepath*/) {
    return {};
}
