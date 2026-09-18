namespace UniPilot.API.Contracts.Tasks;

public sealed record MoveProjectTaskRequest(
    string Status,
    int Position);