using System.ComponentModel.DataAnnotations;

namespace ProperTea.Identity.Api.DTOs;

public record RegisterRequest(
    [Required] [EmailAddress] string Email,
    [Required] string Password
);