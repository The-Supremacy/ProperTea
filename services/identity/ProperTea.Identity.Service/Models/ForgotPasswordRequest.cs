using System.ComponentModel.DataAnnotations;

namespace ProperTea.Identity.Service.Models;

public record ForgotPasswordRequest(
    [Required][EmailAddress] string Email
);