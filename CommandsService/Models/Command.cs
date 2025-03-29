namespace CommandsService.Models
{
    public class Command
    {
        public int Id { get; set; }
        public required string HowTo { get; set; }
        public required string CommandLine { get; set; }
        public int PlatformId { get; set; }
        public required Platform Platform { get; set; }
    }
}