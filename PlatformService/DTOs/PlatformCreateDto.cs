namespace PlatformService.DTOs
{
    public class PlatformCreateDto
    {
        public required string Name { get; set; }
        public required string Publisher { get; set; }
        public required string Cost { get; set; }
    }
}