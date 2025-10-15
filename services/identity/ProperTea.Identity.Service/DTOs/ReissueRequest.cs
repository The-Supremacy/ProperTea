using System.ComponentModel.DataAnnotations;

namespace ProperTea.Identity.Service.DTOs;

public record ReissueRequest(
    [Required] string ExpiredToken
);