#include "AnimationEditorWindow.h"

/// std
#include <fstream>
#include <filesystem>
#include <algorithm>

/// external
#define IMGUI_DEFINE_MATH_OPERATORS
#include <imgui.h>
#include <imgui_internal.h>
#include <dialog/ImGuiFileDialog.h>
#include <nlohmann/json.hpp>

/// engine
#include "Engine/Asset/Collection/AssetCollection.h"
#include "Engine/Asset/Assets/Animation/AnimationClip.h"
#include "Engine/Core/Utility/FileSystem/FileSystem.h"

using namespace Editor;
using namespace ONEngine;

namespace {
    std::string NormalizePath(const std::string& _path) {
        std::string path = _path;
        std::replace(path.begin(), path.end(), '\\', '/');
        // エンジン内部では "./Assets/..." 形式が多いため、それに合わせる
        if (!path.starts_with("./") && !path.starts_with("/") && (path.starts_with("Assets") || path.starts_with("Packages"))) {
            path = "./" + path;
        }
        return path;
    }
}

// -------------------------------------------------------------
// Sequence Wrapper Custom Draw for Keyframes
// -------------------------------------------------------------
void AnimationSequenceWrapper::CustomDraw(int index, ImDrawList* draw_list, const ImRect& rc, const ImRect& /*legendRect*/, const ImRect& clippingRect, const ImRect& /*legendClippingRect*/) {
    if (!clip || index < 0 || index >= (int)clip->tracks.size()) return;
    
    const auto& track = clip->tracks[index];
    
    // X軸のピクセルからフレーム/時間への変換
    float framesPerPixel = (float)(mFrameMax - mFrameMin) / rc.GetWidth();
    
    draw_list->PushClipRect(clippingRect.Min, clippingRect.Max, true);
    
    for (const auto& key : track.keyframes) {
        // time to frame
        int frame = static_cast<int>(key.time * 60.0f); // 60fps
        float pX = rc.Min.x + (frame - mFrameMin) / framesPerPixel;
        
        if (pX >= clippingRect.Min.x && pX <= clippingRect.Max.x) {
            // Draw diamond for keyframe
            ImVec2 center(pX, rc.Min.y + rc.GetHeight() * 0.5f);
            float s = 4.0f;
            ImVec2 pts[4] = {
                {center.x, center.y - s},
                {center.x + s, center.y},
                {center.x, center.y + s},
                {center.x - s, center.y}
            };
            draw_list->AddConvexPolyFilled(pts, 4, IM_COL32(255, 255, 0, 255));
        }
    }
    
    draw_list->PopClipRect();
}

// -------------------------------------------------------------
// Animation Editor Window
// -------------------------------------------------------------
AnimationEditorWindow::AnimationEditorWindow() {
}

void AnimationEditorWindow::ShowImGui() {
    if (!ImGui::Begin(windowName_.c_str())) {
        ImGui::End();
        return;
    }

    // ツールバー
    if (ImGui::Button("Open Clip")) {
        std::filesystem::path animPath = std::filesystem::absolute("./Assets/Anims");
        std::filesystem::create_directories(animPath);

        IGFD::FileDialogConfig config;
        config.path = animPath.string();
        ImGuiFileDialog::Instance()->OpenDialog("OpenAnimDialog", "Choose AnimationClip", ".anim", config);
    }
    ImGui::SameLine();
    if (ImGui::Button("New Clip")) {
        std::filesystem::path animPath = std::filesystem::absolute("./Assets/Anims");
        std::filesystem::create_directories(animPath);

        IGFD::FileDialogConfig config;
        config.path = animPath.string();
        ImGuiFileDialog::Instance()->OpenDialog("NewAnimDialog", "Create New AnimationClip", ".anim", config);
    }

    ImGui::Text("Current Path: %s", currentClipPath.c_str());

    auto* ac = ONEngine::Asset::AssetCollection::GetInstance();

    // ダイアログ処理
    if (ImGuiFileDialog::Instance()->Display("OpenAnimDialog")) {
        if (ImGuiFileDialog::Instance()->IsOk()) {
            std::string fullPath = ImGuiFileDialog::Instance()->GetFilePathName();
            std::string relative = std::filesystem::relative(fullPath, std::filesystem::current_path()).string();
            currentClipPath = NormalizePath(relative);
            ac->ReloadAsset(currentClipPath);
        }
        ImGuiFileDialog::Instance()->Close();
    }

    if (ImGuiFileDialog::Instance()->Display("NewAnimDialog")) {
        if (ImGuiFileDialog::Instance()->IsOk()) {
            std::string fullPath = ImGuiFileDialog::Instance()->GetFilePathName();
            if (std::filesystem::path(fullPath).extension() != ".anim") {
                fullPath += ".anim";
            }
            std::string relative = std::filesystem::relative(fullPath, std::filesystem::current_path()).string();
            currentClipPath = NormalizePath(relative);

            std::filesystem::path fsPath(currentClipPath);
            if (fsPath.has_parent_path()) {
                std::filesystem::create_directories(fsPath.parent_path());
            }

            nlohmann::json j;
            j["name"] = fsPath.stem().string();
            j["startFrame"] = 0;
            j["endFrame"] = 60;
            j["loop"] = true;
            j["tracks"] = nlohmann::json::array();
            j["tracks"].push_back({ {"component", "Transform"}, {"property", "position"}, {"keyframes", nlohmann::json::array()} });

            std::ofstream ofs(currentClipPath);
            if (ofs.is_open()) {
                ofs << j.dump(4);
                ofs.close();
                ac->ReloadAsset(currentClipPath);
            }
        }
        ImGuiFileDialog::Instance()->Close();
    }

    auto* clip = ac->GetAnimationClip(currentClipPath);

    if (clip) {
        ONEngine::Asset::AnimationClip* mutableClip = const_cast<ONEngine::Asset::AnimationClip*>(clip);
        
        // クリップが切り替わった、またはトラック数・範囲が変わった時だけ初期化
        if (sequence.clip != mutableClip || sequence.GetItemCount() != (int)mutableClip->tracks.size() || 
            sequence.mFrameMin != mutableClip->startFrame || sequence.mFrameMax != mutableClip->endFrame) {
            sequence.mFrameMin = mutableClip->startFrame;
            sequence.mFrameMax = mutableClip->endFrame;
            sequence.SetClip(mutableClip);
        }

        // Control Panel
        ImGui::Separator();
        ImGui::Text("Clip: %s", mutableClip->name.c_str());

        ImGui::BeginGroup();
        bool changed = false;
        changed |= ImGui::DragInt("Start Frame", &mutableClip->startFrame, 1, 0, mutableClip->endFrame - 1);
        changed |= ImGui::DragInt("End Frame", &mutableClip->endFrame, 1, mutableClip->startFrame + 1, 10000);
        if (changed) {
            mutableClip->duration = mutableClip->endFrame / 60.0f;
            sequence.mFrameMin = mutableClip->startFrame;
            sequence.mFrameMax = mutableClip->endFrame;
            sequence.SetClip(mutableClip);
        }
        ImGui::Checkbox("Looping", &mutableClip->isLooping);
        ImGui::EndGroup();

        ImGui::SameLine();
        ImGui::SetCursorPosX(ImGui::GetWindowWidth() - 150);
        if (ImGui::Button("Add Track...", ImVec2(130, 40))) {
            ImGui::OpenPopup("AddTrackPopup");
        }

        if (ImGui::BeginPopup("AddTrackPopup")) {
            if (ImGui::MenuItem("Transform/Position (Vec3)")) {
                mutableClip->tracks.push_back({ "Transform", "position", { {mutableClip->startFrame / 60.0f, Vector3(0,0,0), "Linear"} } });
                sequence.SetClip(mutableClip);
                selectedEntry = (int)mutableClip->tracks.size() - 1; 
            }
            if (ImGui::MenuItem("Transform/Rotation (Vec3 Euler)")) {
                mutableClip->tracks.push_back({ "Transform", "rotation", { {mutableClip->startFrame / 60.0f, Vector3(0,0,0), "Linear"} } });
                sequence.SetClip(mutableClip);
                selectedEntry = (int)mutableClip->tracks.size() - 1;
            }
            if (ImGui::MenuItem("Transform/Scale (Vec3)")) {
                mutableClip->tracks.push_back({ "Transform", "scale", { {mutableClip->startFrame / 60.0f, Vector3(1,1,1), "Linear"} } });
                sequence.SetClip(mutableClip);
                selectedEntry = (int)mutableClip->tracks.size() - 1;
            }
            ImGui::Separator();
            if (ImGui::MenuItem("Custom (Float)")) {
                mutableClip->tracks.push_back({ "Transform", "position.x", { {mutableClip->startFrame / 60.0f, 0.0f, "Linear"} } });
                sequence.SetClip(mutableClip);
                selectedEntry = (int)mutableClip->tracks.size() - 1;
            }
            ImGui::EndPopup();
        }

        if (ImGui::Button("Save Clip")) {
            nlohmann::json j;
            j["name"] = mutableClip->name;
            j["startFrame"] = mutableClip->startFrame;
            j["endFrame"] = mutableClip->endFrame;
            j["duration"] = mutableClip->duration;
            j["loop"] = mutableClip->isLooping;
            j["tracks"] = nlohmann::json::array();
            for (const auto& track : mutableClip->tracks) {
                nlohmann::json t;
                t["component"] = track.componentName;
                t["property"] = track.propertyPath;
                t["keyframes"] = nlohmann::json::array();
                for (const auto& key : track.keyframes) {
                    nlohmann::json k;
                    k["t"] = key.time;
                    k["in"] = key.interpolation;
                    std::visit([&k](auto&& arg) { k["v"] = arg; }, key.value);
                    t["keyframes"].push_back(k);
                }
                j["tracks"].push_back(t);
            }
            std::ofstream ofs(currentClipPath);
            if (ofs.is_open()) {
                ofs << j.dump(4);
                ofs.close();
                ONEngine::Console::Log("Saved AnimationClip to: " + currentClipPath);
                ac->ReloadAsset(currentClipPath);
            }
        }

        ImGui::Separator();
        
        // --- トラック選択とタイムラインを横並びにする ---
        ImGui::BeginChild("SequencerRegion", ImVec2(0, 300), true);
        {
            // 左側：トラック名リスト
            ImGui::BeginGroup();
            ImGui::Text("Tracks");
            ImGui::BeginChild("TrackList", ImVec2(150, 0), true);
            for (int i = 0; i < (int)mutableClip->tracks.size(); ++i) {
                bool isSelected = (selectedEntry == i);
                if (ImGui::Selectable(mutableClip->tracks[i].propertyPath.c_str(), isSelected)) {
                    selectedEntry = i;
                }
            }
            ImGui::EndChild();
            ImGui::EndGroup();

            ImGui::SameLine();

            // 右側：タイムライン本体
            ImGui::BeginGroup();
            DrawTimeline();
            ImGui::EndGroup();
        }
        ImGui::EndChild();

        // 下部：詳細編集領域
        if (selectedEntry >= 0 && selectedEntry < (int)mutableClip->tracks.size()) {
            ImGui::Separator();

            if (ImGui::Button("Add Keyframe at Current Time", ImVec2(-1, 35))) {
                auto& track = mutableClip->tracks[selectedEntry];
                std::variant<float, Vector2, Vector3, Vector4> defaultValue = 0.0f;
                if (!track.keyframes.empty()) defaultValue = track.keyframes[0].value;

                bool found = false;
                for (auto& k : track.keyframes) {
                    if (std::abs(k.time - currentTimelineTime) < 0.001f) {
                        found = true; break;
                    }
                }
                if (!found) {
                    track.keyframes.push_back({currentTimelineTime, defaultValue, "Linear"});
                    std::sort(track.keyframes.begin(), track.keyframes.end(), [](const ONEngine::Asset::AnimationKeyframe& a, const ONEngine::Asset::AnimationKeyframe& b) {
                        return a.time < b.time;
                    });
                }
            }

            ImGui::Text("Track Details: %d (%s)", selectedEntry, mutableClip->tracks[selectedEntry].propertyPath.c_str());
            
            ImGui::BeginChild("DetailsRegion", ImVec2(0, 0), false);
            DrawTrackProperties(mutableClip->tracks[selectedEntry]);
            ImGui::EndChild();
        }

    } else if (!currentClipPath.empty()) {
        ImGui::TextColored(ImVec4(1, 0.5f, 0, 1), "File not found in Collection: %s", currentClipPath.c_str());
        if (ImGui::Button("Try Load Manually")) {
            ac->ReloadAsset(currentClipPath);
        }
    }

    ImGui::End();
}

void AnimationEditorWindow::DrawTimeline() {
    int sequenceOptions = ImSequencer::SEQUENCER_EDIT_ALL | ImSequencer::SEQUENCER_CHANGE_FRAME;
    
    // Timeline GUI 描画
    ImSequencer::Sequencer(&sequence, &currentFrame, &expanded, &selectedEntry, &firstFrame, sequenceOptions);

    // 時間の同期 (Frame -> Time)
    currentTimelineTime = (float)currentFrame / 60.0f;
}

void AnimationEditorWindow::DrawTrackProperties(ONEngine::Asset::AnimationTrack& track) {
    char compBuf[64], propBuf[64];
    strncpy_s(compBuf, track.componentName.c_str(), sizeof(compBuf));
    strncpy_s(propBuf, track.propertyPath.c_str(), sizeof(propBuf));

    if (ImGui::InputText("Component", compBuf, sizeof(compBuf))) track.componentName = compBuf;
    if (ImGui::InputText("Property", propBuf, sizeof(propBuf))) track.propertyPath = propBuf;

    // 型の切り替え機能
    const char* typeItems[] = { "Float", "Vector2", "Vector3", "Vector4" };
    int currentType = 0;
    if (!track.keyframes.empty()) {
        if (std::holds_alternative<float>(track.keyframes[0].value)) currentType = 0;
        else if (std::holds_alternative<Vector2>(track.keyframes[0].value)) currentType = 1;
        else if (std::holds_alternative<Vector3>(track.keyframes[0].value)) currentType = 2;
        else if (std::holds_alternative<Vector4>(track.keyframes[0].value)) currentType = 3;
    }

    if (ImGui::Combo("Value Type", &currentType, typeItems, IM_ARRAYSIZE(typeItems))) {
        for (auto& key : track.keyframes) {
            if (currentType == 0) key.value = 0.0f;
            else if (currentType == 1) key.value = Vector2(0, 0);
            else if (currentType == 2) key.value = Vector3(0, 0, 0);
            else if (currentType == 3) key.value = Vector4(0, 0, 0, 1);
        }
    }

    if (ImGui::TreeNodeEx("Keyframes Detail", ImGuiTreeNodeFlags_DefaultOpen)) {
        for (int i = 0; i < (int)track.keyframes.size(); ++i) {
            ImGui::PushID(i);
            auto& key = track.keyframes[i];
            
            int frame = static_cast<int>(key.time * 60.0f);
            if (ImGui::DragInt("Frame", &frame, 1.0f, 0, sequence.mFrameMax)) {
                key.time = (float)frame / 60.0f;
            }
            
            if (std::holds_alternative<float>(key.value)) {
                float v = std::get<float>(key.value);
                if (ImGui::DragFloat("Value", &v, 0.1f)) key.value = v;
            } else if (std::holds_alternative<Vector3>(key.value)) {
                Vector3 v = std::get<Vector3>(key.value);
                if (ImGui::DragFloat3("Value", &v.x, 0.1f)) key.value = v;
            } else if (std::holds_alternative<Vector2>(key.value)) {
                Vector2 v = std::get<Vector2>(key.value);
                if (ImGui::DragFloat2("Value", &v.x, 0.1f)) key.value = v;
            } else if (std::holds_alternative<Vector4>(key.value)) {
                Vector4 v = std::get<Vector4>(key.value);
                if (ImGui::DragFloat4("Value", &v.x, 0.1f)) key.value = v;
            }
            
            const char* items[] = { "Linear", "Step" };
            int current_item = (key.interpolation == "Step") ? 1 : 0;
            if (ImGui::Combo("Interpolation", &current_item, items, IM_ARRAYSIZE(items))) {
                key.interpolation = items[current_item];
            }

            if (ImGui::Button("Remove")) {
                track.keyframes.erase(track.keyframes.begin() + i);
                ImGui::PopID();
                break;
            }
            ImGui::Separator();
            ImGui::PopID();
        }
        ImGui::TreePop();
    }
}
