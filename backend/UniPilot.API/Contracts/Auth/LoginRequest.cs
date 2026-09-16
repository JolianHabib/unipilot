using System.ComponentModel.DataAnnotations;

namespace UniPilot.API.Contracts.Auth;

public sealed record LoginRequest(
    [Required]
    [EmailAddress]
    string Email,

    [Required]
    string Password);