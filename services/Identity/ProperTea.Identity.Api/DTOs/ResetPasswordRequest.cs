using System.ComponentModel.DataAnnotations;

namespace ProperTea.Identity.Api.DTOs;

public record ResetPasswordRequest(
    [Required] [EmailAddress] string Email,
    [Required] string Token,
    [Required] string NewPassword
);