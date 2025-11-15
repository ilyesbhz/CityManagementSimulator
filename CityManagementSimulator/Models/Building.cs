namespace CityManagementSimulator.Models
{
    public class Building
    {
        public int Id { get; set; }

        // Map position
        public int? CellX { get; set; }
        public int? CellY { get; set; }

        // Basic info
        public string Name { get; set; }
        public string Type { get; set; }

        // Population handling
        public int CapacityPopulation { get; set; }   // Maximum allowed
        public int CurrentCitizens { get; set; }      // Optional if you use PersonRepository

        // Economy & resources
        public double Cost { get; set; }
        public double Income { get; set; }
        public double EnergyConsumption { get; set; }
        public double WaterConsumption { get; set; }
        public double Pollution { get; set; }
    }
}
