using Content.Client._WL.Barks.UI; // WL-Changes
using Content.Client._WL.Barks; // WL-Changes
using Content.Shared._WL.Barks; // WL-Changes
using Content.Shared._WL.CCVars; // WL-Changes
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls; // WL-Changes

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    // WL-Changes-Start: Speech barks
    private BarkTab? _barkTab;
    private OptionButton? _speechModeButton;

    private void RefreshVoiceTab()
    {
        _barkTab = new BarkTab();
        var speechTabs = new TabContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            Margin = new Thickness(5, 0, 5, 5),
        };

        var tabIndex = 0;

        speechTabs.AddChild(_barkTab);
        speechTabs.SetTabTitle(tabIndex, Loc.GetString("ui-options-speech-mode-barks"));
        //  Art-changes start
        // _speechModeButton = new OptionButton
        // {
        //     HorizontalAlignment = HAlignment.Right,
        //     MinWidth = 180,
        // };

        // _speechModeButton.AddItem(Loc.GetString("ui-options-speech-mode-barks"), (int) SpeechMode.Barks);
        // _speechModeButton.AddItem(Loc.GetString("ui-options-speech-mode-disabled"), (int) SpeechMode.Disabled);

        // var speechMode = _cfgManager.GetCVar(WLCVars.SpeechMode);

        // _speechModeButton.SelectId((int) speechMode);
        // _speechModeButton.OnItemSelected += args =>
        // {
        //     _speechModeButton.SelectId(args.Id);
        //     _cfgManager.SetCVar(WLCVars.SpeechMode, (SpeechMode) args.Id);
        // };
        // Art-changes end

        var modeText = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
        };
        modeText.AddChild(new Label
        {
            Text = Loc.GetString("ui-options-speech-mode"),
            StyleClasses = { "LabelHeading" },
        });
        modeText.AddChild(new Label
        {
            Text = Loc.GetString("humanoid-profile-editor-speech-mode-description"),
        });

        var modeRow = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            Margin = new Thickness(10, 7),
            VerticalAlignment = VAlignment.Center,
        };
        modeRow.AddChild(modeText);
        // modeRow.AddChild(_speechModeButton);

        var voiceContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        voiceContainer.AddChild(modeRow);
        voiceContainer.AddChild(speechTabs);

        var children = new List<Control>();
        foreach (var child in TabContainer.Children)
            children.Add(child);

        TabContainer.RemoveAllChildren();

        for (int i = 0; i < children.Count; i++)
        {
            if (i == 1) // Set the tab to the 2nd place.
            {
                TabContainer.AddChild(voiceContainer);
            }
            TabContainer.AddChild(children[i]);
        }

        TabContainer.SetTabTitle(1, Loc.GetString("humanoid-profile-editor-voice-tab"));

        _barkTab.OnBarkSelected += barkId =>
        {
            Profile = Profile?.WithBarkVoice(barkId);
            SetDirty();
        };
        _barkTab.OnPitchChanged += pitch =>
        {
            Profile = Profile?.WithBarkPitch(pitch);
            SetDirty();
        };
        _barkTab.OnMinVarChanged += delay =>
        {
            Profile = Profile?.WithBarkMinDelay(delay);
            SetDirty();
        };
        _barkTab.OnMaxVarChanged += delay =>
        {
            Profile = Profile?.WithBarkMaxDelay(delay);
            SetDirty();
        };
    }

    private void UpdateTTSVoicesControls()
    {
        if (Profile is null)
            return;

        _barkTab?.SetSelectedBark(
            Profile.BarkVoice,
            Profile.BarkPitch,
            Profile.BarkMinDelay,
            Profile.BarkMaxDelay);
    }
    // WL-Changes-End
}
