using System.ComponentModel.DataAnnotations;

namespace ProperTea.Identity.Service.Models;

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required] string NewPassword
);