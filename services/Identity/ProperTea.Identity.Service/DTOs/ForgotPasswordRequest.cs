using System.ComponentModel.DataAnnotations;

namespace ProperTea.Identity.Service.DTOs;

public record ForgotPasswordRequest(
    [Required] [EmailAddress] string Email
);