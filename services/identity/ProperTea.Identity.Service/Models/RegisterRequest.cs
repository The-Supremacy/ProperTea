using System.ComponentModel.DataAnnotations;

namespace ProperTea.Identity.Service.Models;

public record RegisterRequest(
    [Required][EmailAddress] string Email, 
    [Required] string Password
);