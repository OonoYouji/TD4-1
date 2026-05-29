#pragma once

/// engine
#include "../../EditorViewCollection.h"
#include "Engine/Asset/Assets/Animation/AnimationClip.h"

namespace Editor {

/// ///////////////////////////////////////////////////
/// アニメーション編集用ウィンドウ
/// ///////////////////////////////////////////////////
class AnimationEditorWindow : public IEditorWindow {
public:
    AnimationEditorWindow();
    ~AnimationEditorWindow() override = default;

    void ShowImGui() override;

private:
    void DrawTimeline();
    void DrawTrack(ONEngine::Asset::AnimationTrack& track);

    std::string windowName_ = "Animation Editor";
    std::string currentClipPath;
    float currentTimelineTime = 0.0f;
    int selectedTrackIndex = -1;
};

} /// namespace Editor
