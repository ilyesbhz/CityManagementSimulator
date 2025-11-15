using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityManagementSimulator.Models
{
    public class CityState
    {
        public int Id { get; set; }
        public string CityName { get; set; } = "MaVille";
        public double Budget { get; set; }
        public double EnergyPool { get; set; }
        public double WaterPool { get; set; }
        public int DayCounter { get; set; }
    }
}
