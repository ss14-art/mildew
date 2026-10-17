using System;
using Content.Shared.Chemistry;
using Robust.Shared.Serialization;

namespace Content.Shared._Art.Chemistry;

[Serializable, NetSerializable]
public enum SolutionHeaterUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class SolutionHeaterSetTargetTemperatureMessage(float targetTemperature) : BoundUserInterfaceMessage
{
    public readonly float TargetTemperature = targetTemperature;
}

[Serializable, NetSerializable]
public sealed class SolutionHeaterToggleAutoEjectMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class SolutionHeaterToggleEnabledMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class SolutionHeaterEjectContainerMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class SolutionHeaterBoundUserInterfaceState(
    ContainerInfo? containerInfo,
    float targetTemperature,
    float currentTemperature,
    bool autoEject,
    bool enabled) : BoundUserInterfaceState
{
    public readonly ContainerInfo? ContainerInfo = containerInfo;
    public readonly float TargetTemperature = targetTemperature;
    public readonly float CurrentTemperature = currentTemperature;
    public readonly bool AutoEject = autoEject;
    public readonly bool Enabled = enabled;
}
