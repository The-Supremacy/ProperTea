using System.ComponentModel.DataAnnotations;

namespace ProperTea.Identity.Api.DTOs;

public record ForgotPasswordRequest(
    [Required] [EmailAddress] string Email
);