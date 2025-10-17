using System.ComponentModel.DataAnnotations;

namespace ProperTea.Identity.Service.DTOs;

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required] string NewPassword
);