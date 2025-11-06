using System.ComponentModel.DataAnnotations;

namespace ProperTea.Identity.Api.DTOs;

public record ReissueRequest(
    [Required] string ExpiredToken
);