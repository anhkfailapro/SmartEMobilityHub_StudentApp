namespace SmartEMobilityHub.Core.Domain;

/// <summary>Tài khoản người vận hành (Sub 2 §6.2).</summary>
public sealed class Operator
{
    public string OperatorId { get; init; } = "";
    public string Name { get; init; } = "";
    public string Role { get; init; } = "";
    public string ShiftLabel { get; init; } = "";

    public Operator Clone() => new()
    {
        OperatorId = OperatorId,
        Name = Name,
        Role = Role,
        ShiftLabel = ShiftLabel
    };
}
