using System.ComponentModel.DataAnnotations;

namespace AuPairGo.API.DTOs
{
    public class UpdateOneSignalDeviceDto
    {
        [Required(ErrorMessage = "OneSignalPlayerId is required.")]
        [StringLength(200)]
        public string OneSignalPlayerId { get; set; } = string.Empty;
    }
}