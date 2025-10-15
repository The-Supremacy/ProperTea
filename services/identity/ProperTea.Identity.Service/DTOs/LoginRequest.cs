using System.ComponentModel.DataAnnotations;

namespace ProperTea.Identity.Service.DTOs;

public record LoginRequest(
    [Required] [EmailAddress] string Email,
    [Required] string Password
);