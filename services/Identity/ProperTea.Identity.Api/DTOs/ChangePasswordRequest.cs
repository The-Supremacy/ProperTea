using System.ComponentModel.DataAnnotations;

namespace ProperTea.Identity.Api.DTOs;

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required] string NewPassword
);