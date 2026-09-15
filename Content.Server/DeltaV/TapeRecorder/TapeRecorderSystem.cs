using Content.Server.Chat.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Speech.Components;
using Content.Shared.Chat;
using Content.Shared.DeltaV.TapeRecorder;
using Content.Shared.DeltaV.TapeRecorder.Components;
using Content.Shared.DeltaV.TapeRecorder.Systems;
using Content.Shared._WL.Barks; // WL-Changes
using Content.Shared.Paper;
using Content.Shared.Speech;
using Robust.Shared.Prototypes;
using System.Text;

namespace Content.Server.DeltaV.TapeRecorder;

public sealed class TapeRecorderSystem : SharedTapeRecorderSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PaperSystem _paper = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TapeRecorderComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<TapeRecorderComponent, PrintTapeRecorderMessage>(OnPrintMessage);
    }

    /// <summary>
    /// Given a time range, play all messages on a tape within said range, [start, end).
    /// Split into this system as shared does not have ChatSystem access
    /// </summary>
    protected override void ReplayMessagesInSegment(Entity<TapeRecorderComponent> ent, TapeCassetteComponent tape, float segmentStart, float segmentEnd)
    {
        var voice = EnsureComp<VoiceOverrideComponent>(ent);
        var speech = EnsureComp<SpeechComponent>(ent);

        foreach (var message in tape.RecordedData)
        {
            if (message.Timestamp < tape.CurrentPosition || message.Timestamp >= segmentEnd)
                continue;

            //Change the voice to match the speaker
            voice.NameOverride = message.Name ?? ent.Comp.DefaultName;
            var verb = message.Verb ?? SharedChatSystem.DefaultSpeechVerb;
            speech.SpeechVerb = _proto.Index<SpeechVerbPrototype>(verb);

            // WL-Changes-Start: Speech barks
            if (!string.IsNullOrEmpty(message.BarkVoice) &&
                _proto.HasIndex<BarkPrototype>(message.BarkVoice))
            {
                EnsureComp<SpeechBarksComponent>(ent);
                voice.BarkVoiceOverride = message.BarkVoice;
                voice.BarkPitchOverride = message.BarkPitch;
                voice.BarkMinDelayOverride = message.BarkMinDelay;
                voice.BarkMaxDelayOverride = message.BarkMaxDelay;
            }
            else
            {
                RemComp<SpeechBarksComponent>(ent);
                voice.BarkVoiceOverride = null;
                voice.BarkPitchOverride = null;
                voice.BarkMinDelayOverride = null;
                voice.BarkMaxDelayOverride = null;
            }
            // WL-Changes-End

            //Play the message
            _chat.TrySendInGameICMessage(ent, message.Message, InGameICChatType.Speak, false);
        }
    }

    /// <summary>
    /// Whenever someone speaks within listening range, record it to tape
    /// </summary>
    private void OnListen(Entity<TapeRecorderComponent> ent, ref ListenEvent args)
    {
        // mode should never be set when it isn't active but whatever
        if (ent.Comp.Mode != TapeRecorderMode.Recording || !HasComp<ActiveTapeRecorderComponent>(ent))
            return;

        // No feedback loops
        if (args.Source == ent.Owner)
            return;

        if (!TryGetTapeCassette(ent, out var cassette))
            return;

        //Handle someone using a voice changer
        var nameEv = new TransformSpeakerNameEvent(args.Source, Name(args.Source));
        RaiseLocalEvent(args.Source, nameEv);

        //Add a new entry to the tape
        var verb = _chat.GetSpeechVerb(args.Source, args.Message);
        var name = nameEv.VoiceName;

        // WL-Changes-Start
        var barkVoice = string.Empty;
        var barkPitch = SpeechBarksComponent.DefaultPitch;
        var barkMinDelay = SpeechBarksComponent.DefaultMinDelay;
        var barkMaxDelay = SpeechBarksComponent.DefaultMaxDelay;

        if (TryComp<SpeechBarksComponent>(args.Source, out var barkComp))
        {
            var barkTransform = new TransformSpeakerBarkEvent(
                args.Source,
                barkComp.Voice,
                barkComp.Pitch,
                barkComp.MinDelay,
                barkComp.MaxDelay);
            RaiseLocalEvent(args.Source, barkTransform);
            barkVoice = barkTransform.Voice;
            barkPitch = barkTransform.Pitch;
            barkMinDelay = barkTransform.MinDelay;
            barkMaxDelay = barkTransform.MaxDelay;
        }
        // WL-Changes: added speech bark support
        cassette.Comp.Buffer.Add(new TapeCassetteRecordedMessage(
            cassette.Comp.CurrentPosition,
            name,
            verb,
            args.Message,
            barkVoice,
            barkPitch,
            barkMinDelay,
            barkMaxDelay));
        // WL-Changes-end
    }

    private void OnPrintMessage(Entity<TapeRecorderComponent> ent, ref PrintTapeRecorderMessage args)
    {
        var (uid, comp) = ent;

        if (comp.CooldownEndTime > Timing.CurTime)
            return;

        if (!TryGetTapeCassette(ent, out var cassette))
            return;

        var text = new StringBuilder();
        var paper = Spawn(comp.PaperPrototype, Transform(ent).Coordinates);

        // Sorting list by time for overwrite order
        var data = cassette.Comp.RecordedData;
        data.Sort((x, y) => x.Timestamp.CompareTo(y.Timestamp));

        // Looking if player's entity exists to give paper in its hand
        var player = args.Actor;
        if (Exists(player))
            _hands.PickupOrDrop(player, paper, checkActionBlocker: false);

        if (!TryComp<PaperComponent>(paper, out var paperComp))
            return;

        Audio.PlayPvs(comp.PrintSound, ent);

        text.AppendLine(Loc.GetString("tape-recorder-print-start-text"));
        text.AppendLine();
        foreach (var message in cassette.Comp.RecordedData)
        {
            var name = message.Name ?? ent.Comp.DefaultName;
            var time = TimeSpan.FromSeconds((double)message.Timestamp);

            text.AppendLine(Loc.GetString("tape-recorder-print-message-text",
                ("time", time.ToString(@"hh\:mm\:ss")),
                ("source", name),
                ("message", message.Message)));
        }
        text.AppendLine();
        text.Append(Loc.GetString("tape-recorder-print-end-text"));

        _paper.SetContent((paper, paperComp), text.ToString());

        comp.CooldownEndTime = Timing.CurTime + comp.PrintCooldown;
    }
}
