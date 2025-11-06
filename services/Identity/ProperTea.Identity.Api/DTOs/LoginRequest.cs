using System.ComponentModel.DataAnnotations;

namespace ProperTea.Identity.Api.DTOs;

public record LoginRequest(
    [Required] [EmailAddress] string Email,
    [Required] string Password
);