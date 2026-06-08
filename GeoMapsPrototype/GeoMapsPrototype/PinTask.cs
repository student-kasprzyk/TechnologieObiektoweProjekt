namespace GeoMapsPrototype
{
    public class PinTask
    {
        public string TaskId { get; set; } = Guid.NewGuid().ToString();
        public string TaskType { get; set; } = "generic";
        public string Description { get; set; } = string.Empty;
    }
}
