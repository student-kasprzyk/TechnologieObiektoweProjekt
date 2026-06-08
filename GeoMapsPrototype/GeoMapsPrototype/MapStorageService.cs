using System.Text.Json;

namespace GeoMapsPrototype
{
    internal class MapStorageService
    {
        private static string MapsDirectory => Path.Combine(FileSystem.AppDataDirectory, "maps");

        public static void Save(string mapName, IEnumerable<CustomPin> pins)
        {
            Directory.CreateDirectory(MapsDirectory);
            var data = pins.Select(p => new PinData
            {
                Label = p.Label,
                Address = p.Address ?? string.Empty,
                Latitude = p.Location.Latitude,
                Longitude = p.Location.Longitude,
                Radius = p.Radius,
                TaskType = p.TaskData?.TaskType ?? "generic",
                TaskDescription = p.TaskData?.Description ?? string.Empty
            }).ToList();

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(MapsDirectory, $"{mapName}.json"), json);
        }

        public static List<CustomPin>? Load(string mapName)
        {
            var path = Path.Combine(MapsDirectory, $"{mapName}.json");
            if (!File.Exists(path)) return null;

            var data = JsonSerializer.Deserialize<List<PinData>>(File.ReadAllText(path));
            if (data == null) return null;

            return data.Select(d => new CustomPin
            {
                Label = d.Label,
                Address = d.Address,
                Location = new Location(d.Latitude, d.Longitude),
                Radius = d.Radius,
                ImageSource = ImageSource.FromFile("pin_red.png"),
                TaskData = new PinTask { TaskType = d.TaskType, Description = d.TaskDescription }
            }).ToList();
        }

        public static List<string> GetSavedMaps()
        {
            Directory.CreateDirectory(MapsDirectory);
            return Directory.GetFiles(MapsDirectory, "*.json")
                .Select(f => Path.GetFileNameWithoutExtension(f)!)
                .ToList();
        }

        private class PinData
        {
            public string Label { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public double Radius { get; set; } = 50;
            public string TaskType { get; set; } = "generic";
            public string TaskDescription { get; set; } = string.Empty;
        }
    }
}
