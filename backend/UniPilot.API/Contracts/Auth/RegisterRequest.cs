using System.ComponentModel.DataAnnotations;

namespace UniPilot.API.Contracts.Auth;

public sealed record RegisterRequest(
    [Required]
    [StringLength(100, MinimumLength = 2)]
    string FullName,

    [Required]
    [EmailAddress]
    [StringLength(255)]
    string Email,

    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    string Password);