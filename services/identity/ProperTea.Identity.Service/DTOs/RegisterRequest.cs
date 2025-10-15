using System.ComponentModel.DataAnnotations;

namespace ProperTea.Identity.Service.DTOs;

public record RegisterRequest(
    [Required] [EmailAddress] string Email,
    [Required] string Password
);