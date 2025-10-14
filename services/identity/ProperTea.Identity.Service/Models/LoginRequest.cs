using System.ComponentModel.DataAnnotations;

namespace ProperTea.Identity.Service.Models;

public record LoginRequest(
    [Required] [EmailAddress] string Email,
    [Required] string Password
);