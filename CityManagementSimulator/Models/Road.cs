using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityManagementSimulator.Models
{
    public class Road
    {
        public int Id { get; set; }
        public int CellX { get; set; }
        public int CellY { get; set; }
        // Added name to differentiate roads
        public string Name { get; set; }
    }
}