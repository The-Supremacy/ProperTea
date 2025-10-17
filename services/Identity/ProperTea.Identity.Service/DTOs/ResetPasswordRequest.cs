using System.ComponentModel.DataAnnotations;

namespace ProperTea.Identity.Service.DTOs;

public record ResetPasswordRequest(
    [Required] [EmailAddress] string Email,
    [Required] string Token,
    [Required] string NewPassword
);