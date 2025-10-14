using System.ComponentModel.DataAnnotations;

namespace ProperTea.Identity.Service.Models;

public record ReissueRequest(
    [Required] string ExpiredToken
);