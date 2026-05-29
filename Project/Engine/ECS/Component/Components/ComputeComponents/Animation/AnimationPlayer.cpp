#include "AnimationPlayer.h"

/// external
#include <imgui.h>
#include <nlohmann/json.hpp>

using namespace ONEngine;

AnimationPlayer::AnimationPlayer() {
    Reset();
}

AnimationPlayer::~AnimationPlayer() = default;

void AnimationPlayer::Reset() {
    clipPath = "";
    currentTime = 0.0f;
    speed = 1.0f;
    isPlaying = false;
    isLooping = true;
    autoPlay = true;
    bindings.clear();
    isBound = false;
}

void AnimationPlayer::Play() {
    isPlaying = true;
}

void AnimationPlayer::Pause() {
    isPlaying = false;
}

void AnimationPlayer::Stop() {
    isPlaying = false;
    currentTime = 0.0f;
}

void AnimationPlayer::SetClip(const std::string& _path) {
    clipPath = _path;
    isBound = false; // クリップが変わったらバインドをやり直す
}

void ONEngine::from_json(const nlohmann::json& _j, AnimationPlayer& _a) {
    _a.clipPath = _j.value("clipPath", "");
    _a.currentTime = _j.value("currentTime", 0.0f);
    _a.speed = _j.value("speed", 1.0f);
    _a.isPlaying = _j.value("isPlaying", false);
    _a.isLooping = _j.value("isLooping", true);
    _a.autoPlay = _j.value("autoPlay", true);
}

void ONEngine::to_json(nlohmann::json& _j, const AnimationPlayer& _a) {
    _j = nlohmann::json{
        { "type", "AnimationPlayer" },
        { "clipPath", _a.clipPath },
        { "currentTime", _a.currentTime },
        { "speed", _a.speed },
        { "isPlaying", _a.isPlaying },
        { "isLooping", _a.isLooping },
        { "autoPlay", _a.autoPlay }
    };
}

void ComponentDebug::AnimationPlayerDebug(AnimationPlayer* _player) {
    if (!_player) return;

    ImGui::Text("Animation Player");
    
    char pathBuf[256];
    strncpy_s(pathBuf, _player->clipPath.c_str(), sizeof(pathBuf));
    if (ImGui::InputText("Clip Path", pathBuf, sizeof(pathBuf))) {
        _player->SetClip(pathBuf);
    }

    ImGui::DragFloat("Current Time", &_player->currentTime, 0.01f);
    ImGui::DragFloat("Speed", &_player->speed, 0.1f);
    ImGui::Checkbox("Is Playing", &_player->isPlaying);
    ImGui::Checkbox("Is Looping", &_player->isLooping);
    ImGui::Checkbox("Auto Play", &_player->autoPlay);

    if (ImGui::Button("Play")) _player->Play();
    ImGui::SameLine();
    if (ImGui::Button("Pause")) _player->Pause();
    ImGui::SameLine();
    if (ImGui::Button("Stop")) _player->Stop();
}
