using System;
using System.Linq;
using System.Numerics;
using Content.Server.Chemistry.Components;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._Art.Chemistry;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Placeable;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.EntitySystems;

public sealed class SolutionHeaterSystem : EntitySystem
{
    [Dependency] private readonly PowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionHeaterComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<SolutionHeaterComponent, ItemPlacedEvent>(OnItemPlaced);
        SubscribeLocalEvent<SolutionHeaterComponent, ItemRemovedEvent>(OnItemRemoved);
        SubscribeLocalEvent<SolutionHeaterComponent, BoundUIOpenedEvent>(OnBoundUiOpened);

        SubscribeLocalEvent<SolutionHeaterComponent, SolutionHeaterSetTargetTemperatureMessage>(OnSetTargetTemperature);
        SubscribeLocalEvent<SolutionHeaterComponent, SolutionHeaterToggleAutoEjectMessage>(OnToggleAutoEject);
        SubscribeLocalEvent<SolutionHeaterComponent, SolutionHeaterToggleEnabledMessage>(OnToggleEnabled);
        SubscribeLocalEvent<SolutionHeaterComponent, SolutionHeaterEjectContainerMessage>(OnEjectContainer);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveSolutionHeaterComponent, SolutionHeaterComponent, ItemPlacerComponent>();
        while (query.MoveNext(out var uid, out _, out var heater, out var placer))
        {
            if (!TryGetPlacedSolution((uid, placer), out var placedEntity, out var solution))
            {
                UpdateUiState((uid, heater));
                continue;
            }

            if (heater.Enabled)
            {
                ApplyTemperatureDelta((uid, heater), solution, frameTime);

                if (heater.AutoEject && ReachedTargetTemperature((uid, heater), solution.Comp.Solution.Temperature))
                    AutoEject((uid, heater), placedEntity);
            }

            UpdateUiState((uid, heater));
        }
    }

    private void OnBoundUiOpened(Entity<SolutionHeaterComponent> entity, ref BoundUIOpenedEvent args)
    {
        UpdateUiState(entity);
    }

    private void OnSetTargetTemperature(Entity<SolutionHeaterComponent> entity, ref SolutionHeaterSetTargetTemperatureMessage args)
    {
        // SS14-Art edit start
        entity.Comp.TargetTemperature = Math.Clamp(args.TargetTemperature, 0f, 1000f);
        RefreshState(entity);
        // SS14-Art edit end
    }

    private void OnToggleAutoEject(Entity<SolutionHeaterComponent> entity, ref SolutionHeaterToggleAutoEjectMessage args)
    {
        // SS14-Art edit start
        entity.Comp.AutoEject = !entity.Comp.AutoEject;
        UpdateUiState(entity);
        // SS14-Art edit end
    }

    private void OnToggleEnabled(Entity<SolutionHeaterComponent> entity, ref SolutionHeaterToggleEnabledMessage args)
    {
        // SS14-Art edit start
        entity.Comp.Enabled = !entity.Comp.Enabled;
        RefreshState(entity);
        // SS14-Art edit end
    }

    private void OnEjectContainer(Entity<SolutionHeaterComponent> entity, ref SolutionHeaterEjectContainerMessage args)
    {
        // SS14-Art edit start
        if (!TryComp<ItemPlacerComponent>(entity, out var placer) || placer.PlacedEntities.FirstOrDefault() is not { Valid: true } placed)
            return;

        _hands.PickupOrDrop(args.Actor, placed, checkActionBlocker: false);
        RefreshState(entity);
        // SS14-Art edit end
    }

    private void TurnOn(EntityUid uid)
    {
        _appearance.SetData(uid, SolutionHeaterVisuals.IsOn, true);
        EnsureComp<ActiveSolutionHeaterComponent>(uid);
    }

    private void TurnOff(EntityUid uid)
    {
        _appearance.SetData(uid, SolutionHeaterVisuals.IsOn, false);
        RemComp<ActiveSolutionHeaterComponent>(uid);
    }

    private void RefreshState(Entity<SolutionHeaterComponent> entity, ItemPlacerComponent? placer = null)
    {
        if (!Resolve(entity, ref placer))
            return;

        var active = entity.Comp.Enabled && placer.PlacedEntities.Count > 0 && _powerReceiver.IsPowered(entity);
        if (active)
            TurnOn(entity);
        else
            TurnOff(entity);

        UpdateUiState(entity);
    }

    private void OnPowerChanged(Entity<SolutionHeaterComponent> entity, ref PowerChangedEvent args)
    {
        RefreshState(entity);
    }

    private void OnItemPlaced(Entity<SolutionHeaterComponent> entity, ref ItemPlacedEvent args)
    {
        RefreshState(entity);
    }

    private void OnItemRemoved(Entity<SolutionHeaterComponent> entity, ref ItemRemovedEvent args)
    {
        RefreshState(entity);
    }

    private void UpdateUiState(Entity<SolutionHeaterComponent> entity)
    {
        var containerInfo = TryGetPlacedSolution((entity.Owner, Comp<ItemPlacerComponent>(entity)), out _, out var solution)
            ? BuildContainerInfo(Name(solution.Owner), solution.Comp.Solution)
            : null;

        var currentTemperature = solution.Comp?.Solution.Temperature ?? float.NaN;
        var state = new SolutionHeaterBoundUserInterfaceState(
            containerInfo,
            entity.Comp.TargetTemperature,
            currentTemperature,
            entity.Comp.AutoEject,
            entity.Comp.Enabled);

        _userInterface.SetUiState(entity.Owner, SolutionHeaterUiKey.Key, state);
    }

    private bool TryGetPlacedSolution(
        Entity<ItemPlacerComponent> entity,
        out EntityUid placedEntity,
        out Entity<SolutionComponent> solution)
    {
        placedEntity = EntityUid.Invalid;
        solution = default;

        foreach (var candidate in entity.Comp.PlacedEntities)
        {
            if (!TryComp<SolutionContainerManagerComponent>(candidate, out var container))
                continue;

            var firstSolution = _solutionContainer.EnumerateSolutions((candidate, container)).FirstOrDefault();
            if (firstSolution.Solution.Owner == default)
                continue;

            placedEntity = candidate;
            solution = firstSolution.Solution;
            return true;
        }
        return false;
    }

    private void ApplyTemperatureDelta(Entity<SolutionHeaterComponent> heater, Entity<SolutionComponent> solution, float frameTime)
    {
        // SS14-Art edit start
        var solutionData = solution.Comp.Solution;
        var current = solutionData.Temperature;
        var delta = heater.Comp.TargetTemperature - current;
        if (MathF.Abs(delta) <= heater.Comp.TemperatureTolerance)
            return;

        var heatCapacity = solutionData.GetHeatCapacity(_prototypeManager);
        if (heatCapacity <= 0f)
            return;

        var maxTemperatureDelta = heater.Comp.HeatPerSecond * frameTime / heatCapacity;
        var desired = MathF.Abs(delta) <= maxTemperatureDelta
            ? heater.Comp.TargetTemperature
            : current + MathF.Sign(delta) * maxTemperatureDelta;

        var energy = (desired - current) * heatCapacity;
        _solutionContainer.AddThermalEnergyClamped(
            solution,
            energy,
            MathF.Min(current, heater.Comp.TargetTemperature),
            MathF.Max(current, heater.Comp.TargetTemperature));
        // SS14-Art edit end
    }

    private bool ReachedTargetTemperature(Entity<SolutionHeaterComponent> heater, float currentTemperature)
    {
        return !float.IsNaN(currentTemperature) &&
               MathF.Abs(currentTemperature - heater.Comp.TargetTemperature) <= heater.Comp.TemperatureTolerance;
    }

    private void AutoEject(Entity<SolutionHeaterComponent> heater, EntityUid placedEntity)
    {
        // SS14-Art edit start
        _transform.SetCoordinates(placedEntity, Transform(heater).Coordinates.Offset(new Vector2(0.7f, 0f)));
        _transform.AttachToGridOrMap(placedEntity);
        RefreshState(heater);
        // SS14-Art edit end
    }

    private static ContainerInfo BuildContainerInfo(string name, Solution solution)
    {
        return new ContainerInfo(name, solution.Volume, solution.MaxVolume)
        {
            Reagents = solution.Contents
        };
    }
}