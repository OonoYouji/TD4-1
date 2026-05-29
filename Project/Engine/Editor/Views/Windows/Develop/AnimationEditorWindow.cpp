#include "AnimationEditorWindow.h"

/// std
#include <fstream>
#include <filesystem>

/// external
#include <imgui.h>
#include <dialog/ImGuiFileDialog.h>
#include <nlohmann/json.hpp>

/// engine
#include "Engine/Asset/Collection/AssetCollection.h"
#include "Engine/Asset/Assets/Animation/AnimationClip.h"
#include "Engine/Core/Utility/FileSystem/FileSystem.h"

using namespace Editor;
using namespace ONEngine;

AnimationEditorWindow::AnimationEditorWindow() {
}

void AnimationEditorWindow::ShowImGui() {
    if (!ImGui::Begin(windowName_.c_str())) {
        ImGui::End();
        return;
    }

    // ツールバー
    if (ImGui::Button("Open Clip")) {
        IGFD::FileDialogConfig config;
        config.path = "./Assets/Anims";
        ImGuiFileDialog::Instance()->OpenDialog("OpenAnimDialog", "Choose AnimationClip", ".anim", config);
    }
    ImGui::SameLine();
    if (ImGui::Button("New Clip")) {
        IGFD::FileDialogConfig config;
        config.path = "./Assets/Anims";
        ImGuiFileDialog::Instance()->OpenDialog("NewAnimDialog", "Create New AnimationClip", ".anim", config);
    }

    ImGui::Text("Current Path: %s", currentClipPath.c_str());

    // ダイアログ処理
    if (ImGuiFileDialog::Instance()->Display("OpenAnimDialog")) {
        if (ImGuiFileDialog::Instance()->IsOk()) {
            currentClipPath = ImGuiFileDialog::Instance()->GetFilePathName();
            // 相対パスに変換（もし可能なら）
            std::string relative = std::filesystem::relative(currentClipPath, std::filesystem::current_path()).string();
            if (!relative.empty()) currentClipPath = "./" + relative;
        }
        ImGuiFileDialog::Instance()->Close();
    }

    if (ImGuiFileDialog::Instance()->Display("NewAnimDialog")) {
        if (ImGuiFileDialog::Instance()->IsOk()) {
            currentClipPath = ImGuiFileDialog::Instance()->GetFilePathName();
            if (std::filesystem::path(currentClipPath).extension() != ".anim") {
                currentClipPath += ".anim";
            }
            // 相対パスに変換
            std::string relative = std::filesystem::relative(currentClipPath, std::filesystem::current_path()).string();
            if (!relative.empty()) currentClipPath = "./" + relative;

            // 新規作成
            std::filesystem::path fsPath(currentClipPath);
            if (fsPath.has_parent_path()) {
                std::filesystem::create_directories(fsPath.parent_path());
            }

            nlohmann::json j;
            j["name"] = fsPath.stem().string();
            j["duration"] = 1.0f;
            j["loop"] = true;
            j["tracks"] = nlohmann::json::array();

            std::ofstream ofs(currentClipPath);
            if (ofs.is_open()) {
                ofs << j.dump(4);
                ofs.close();
                ONEngine::Console::Log("Created new AnimationClip at: " + currentClipPath);
                
                // AssetCollectionに追加
                ONEngine::Asset::AnimationClip newClip;
                newClip.name = j["name"];
                newClip.duration = 1.0f;
                newClip.isLooping = true;
                ONEngine::Asset::AssetCollection::GetInstance()->AddAsset(currentClipPath, std::move(newClip));
            } else {
                ONEngine::Console::LogError("Failed to create AnimationClip at: " + currentClipPath);
            }
        }
        ImGuiFileDialog::Instance()->Close();
    }

    auto* ac = ONEngine::Asset::AssetCollection::GetInstance();
    auto* clip = const_cast<ONEngine::Asset::AnimationClip*>(ac->GetAsset<ONEngine::Asset::AnimationClip>(ac->GetAssetGuidFromPath(currentClipPath)));

    if (clip) {
        ImGui::Separator();
        ImGui::Text("Editing Clip: %s", clip->name.c_str());
        ImGui::DragFloat("Duration", &clip->duration, 0.1f, 0.0f, 100.0f);
        ImGui::Checkbox("Looping", &clip->isLooping);

        if (ImGui::Button("Save Clip")) {
            nlohmann::json j;
            j["name"] = clip->name;
            j["duration"] = clip->duration;
            j["loop"] = clip->isLooping;
            j["tracks"] = nlohmann::json::array();
            for (const auto& track : clip->tracks) {
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
            } else {
                ONEngine::Console::LogError("Failed to save AnimationClip to: " + currentClipPath);
            }
        }

        ImGui::Separator();
        if (ImGui::Button("Add Track")) {
            clip->tracks.push_back({"Transform", "position.x", {}});
        }

        for (int i = 0; i < (int)clip->tracks.size(); ++i) {
            ImGui::PushID(i);
            DrawTrack(clip->tracks[i]);
            if (ImGui::Button("Remove Track")) {
                clip->tracks.erase(clip->tracks.begin() + i);
                ImGui::PopID();
                break;
            }
            ImGui::PopID();
            ImGui::Separator();
        }
    } else if (!currentClipPath.empty()) {
        ImGui::TextColored(ImVec4(1, 0.5f, 0, 1), "Loading or File not found: %s", currentClipPath.c_str());
    }

    ImGui::End();
}

void AnimationEditorWindow::DrawTrack(ONEngine::Asset::AnimationTrack& track) {
    char compBuf[64], propBuf[64];
    strncpy_s(compBuf, track.componentName.c_str(), sizeof(compBuf));
    strncpy_s(propBuf, track.propertyPath.c_str(), sizeof(propBuf));

    if (ImGui::InputText("Component", compBuf, sizeof(compBuf))) track.componentName = compBuf;
    if (ImGui::InputText("Property", propBuf, sizeof(propBuf))) track.propertyPath = propBuf;

    if (ImGui::TreeNode("Keyframes")) {
        if (ImGui::Button("Add Keyframe")) {
            track.keyframes.push_back({0.0f, 0.0f, "Linear"});
        }
        for (int i = 0; i < (int)track.keyframes.size(); ++i) {
            ImGui::PushID(i);
            auto& key = track.keyframes[i];
            ImGui::DragFloat("Time", &key.time, 0.01f, 0.0f, 100.0f);
            
            // Value editing
            if (std::holds_alternative<float>(key.value)) {
                float v = std::get<float>(key.value);
                if (ImGui::DragFloat("Value", &v, 0.1f)) key.value = v;
            } else if (std::holds_alternative<Vector3>(key.value)) {
                Vector3 v = std::get<Vector3>(key.value);
                if (ImGui::DragFloat3("Value", &v.x, 0.1f)) key.value = v;
            }
            
            const char* items[] = { "Linear", "Step" };
            int current_item = (key.interpolation == "Step") ? 1 : 0;
            if (ImGui::Combo("Interpolation", &current_item, items, IM_ARRAYSIZE(items))) {
                key.interpolation = items[current_item];
            }

            if (ImGui::Button("Remove Keyframe")) {
                track.keyframes.erase(track.keyframes.begin() + i);
                ImGui::PopID();
                break;
            }
            ImGui::PopID();
        }
        ImGui::TreePop();
    }
}
