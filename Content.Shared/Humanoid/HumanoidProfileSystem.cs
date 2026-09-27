using Content.Shared._WL.Barks; // WL-Changes
using Content.Shared.Examine;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.Preferences;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Enums; // Art-edit

namespace Content.Shared.Humanoid;

public sealed class HumanoidProfileSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly GrammarSystem _grammar = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidProfileComponent, ExaminedEvent>(OnExamined);
    }

    public void ApplyProfileTo(Entity<HumanoidProfileComponent?> ent, HumanoidCharacterProfile profile)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.Gender = profile.Gender;
        ent.Comp.Age = profile.Age;
        ent.Comp.Species = profile.Species;
        ent.Comp.Sex = profile.Sex;
        // WL-Changes-Start: Speech barks
        if (TryComp<SpeechBarksComponent>(ent, out var barks))
        {
            barks.Voice = profile.BarkVoice;
            barks.Pitch = profile.BarkPitch;
            barks.MinDelay = profile.BarkMinDelay;
            barks.MaxDelay = profile.BarkMaxDelay;
            Dirty(ent.Owner, barks);
        }
        // WL-Changes-End
        Dirty(ent);

        var sexChanged = new SexChangedEvent(ent.Comp.Sex, profile.Sex);
        RaiseLocalEvent(ent, ref sexChanged);

        if (TryComp<GrammarComponent>(ent, out var grammar))
        {
            _grammar.SetGender((ent, grammar), profile.Gender);
        }
    }

    private void OnExamined(Entity<HumanoidProfileComponent> ent, ref ExaminedEvent args)
    {
        var identity = Identity.Entity(ent, EntityManager);
        var species = GetSpeciesRepresentation(ent.Comp.Species).ToLower();
        var age = GetAgeRepresentation(ent.Comp.Species, ent.Comp.Age);

        args.PushText(Loc.GetString("humanoid-appearance-component-examine", ("user", identity), ("age", age), ("species", species)));
    }

    /// <summary>
    /// Takes ID of the species prototype, returns UI-friendly name of the species.
    /// </summary>
    public string GetSpeciesRepresentation(ProtoId<SpeciesPrototype> species)
    {
        if (_prototype.TryIndex(species, out var speciesPrototype))
            return Loc.GetString(speciesPrototype.Name);

        Log.Error("Tried to get representation of unknown species: {speciesId}");
        return Loc.GetString("humanoid-appearance-component-unknown-species");
    }

    // Art-start
	public string GetSpeciesRepresentation(EntityUid uid)
    {
        if (TryComp<HumanoidProfileComponent>(uid, out var profile))
            return GetSpeciesRepresentation(profile.Species);
        return Loc.GetString("humanoid-appearance-component-unknown-species");
    }

    public Gender GetGender(EntityUid uid)
    {
        return TryComp<HumanoidProfileComponent>(uid, out var profile)
            ? profile.Gender
            : Gender.Epicene;
    }
    // Art-end

    /// <summary>
    /// Takes ID of the species prototype and an age, returns an approximate description
    /// </summary>
    public string GetAgeRepresentation(ProtoId<SpeciesPrototype> species, int age)
    {
        if (!_prototype.TryIndex(species, out var speciesPrototype))
        {
            Log.Error("Tried to get age representation of species that couldn't be indexed: " + species);
            return Loc.GetString("identity-age-young");
        }

        if (age < speciesPrototype.YoungAge)
        {
            return Loc.GetString("identity-age-young");
        }

        if (age < speciesPrototype.OldAge)
        {
            return Loc.GetString("identity-age-middle-aged");
        }

        return Loc.GetString("identity-age-old");
    }
}
