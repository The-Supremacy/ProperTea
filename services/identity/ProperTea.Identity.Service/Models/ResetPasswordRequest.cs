using System.ComponentModel.DataAnnotations;

namespace ProperTea.Identity.Service.Models;

public record ResetPasswordRequest(
    [Required] [EmailAddress] string Email,
    [Required] string Token,
    [Required] string NewPassword
);