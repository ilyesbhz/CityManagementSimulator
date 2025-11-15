using System;


namespace CityManagementSimulator.Models
{
    public class Person
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
        public int Age { get; set; }
        public int? BuildingId { get; set; }
    }
}