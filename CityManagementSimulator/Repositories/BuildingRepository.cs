using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using CityManagementSimulator.Data;
using CityManagementSimulator.Models;

namespace CityManagementSimulator.Repositories
{
    public class BuildingRepository
    {
        private readonly DatabaseHelper _db = new DatabaseHelper();

        public List<Building> GetAll()
        {
            var list = new List<Building>();
            var dt = _db.ExecuteQuery("SELECT * FROM Building");
            foreach (DataRow r in dt.Rows)
            {
                list.Add(new Building
                {
                    Id = Convert.ToInt32(r["Id"]),
                    Name = r["Name"].ToString(),
                    Type = r["Type"].ToString(),
                    CapacityPopulation = r["CapacityPopulation"] == DBNull.Value ? 0 : Convert.ToInt32(r["CapacityPopulation"]),
                    Cost = r["Cost"] == DBNull.Value ? 0 : Convert.ToDouble(r["Cost"]),
                    Income = r["Income"] == DBNull.Value ? 0 : Convert.ToDouble(r["Income"]),
                    EnergyConsumption = r["EnergyConsumption"] == DBNull.Value ? 0 : Convert.ToDouble(r["EnergyConsumption"]),
                    WaterConsumption = r["WaterConsumption"] == DBNull.Value ? 0 : Convert.ToDouble(r["WaterConsumption"]),
                    Pollution = r["Pollution"] == DBNull.Value ? 0 : Convert.ToDouble(r["Pollution"]),
                    CellX = r["CellX"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["CellX"]),
                    CellY = r["CellY"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["CellY"])
                });
            }
            return list;
        }

        public int Add(Building b)
        {
            string sql = @"INSERT INTO Building (Name,Type,CapacityPopulation,Cost,Income,EnergyConsumption,WaterConsumption,Pollution,CellX,CellY)
                           VALUES (@n,@t,@cap,@cost,@inc,@eng,@wat,@pol,@x,@y); SELECT SCOPE_IDENTITY();";
            var p = new[]
            {
                new SqlParameter("@n", b.Name),
                new SqlParameter("@t", b.Type),
                new SqlParameter("@cap", b.CapacityPopulation),
                new SqlParameter("@cost", b.Cost),
                new SqlParameter("@inc", b.Income),
                new SqlParameter("@eng", b.EnergyConsumption),
                new SqlParameter("@wat", b.WaterConsumption),
                new SqlParameter("@pol", b.Pollution),
                new SqlParameter("@x", (object)b.CellX ?? DBNull.Value),
                new SqlParameter("@y", (object)b.CellY ?? DBNull.Value)
            };
            var obj = _db.ExecuteScalar(sql, p);
            return Convert.ToInt32(obj);
        }

        public void Update(Building b)
        {
            string sql = @"UPDATE Building SET Name=@n, Type=@t, CapacityPopulation=@cap, Cost=@cost, Income=@inc,
                           EnergyConsumption=@eng, WaterConsumption=@wat, Pollution=@pol, CellX=@x, CellY=@y WHERE Id=@id";
            var p = new[]
            {
                new SqlParameter("@n", b.Name),
                new SqlParameter("@t", b.Type),
                new SqlParameter("@cap", b.CapacityPopulation),
                new SqlParameter("@cost", b.Cost),
                new SqlParameter("@inc", b.Income),
                new SqlParameter("@eng", b.EnergyConsumption),
                new SqlParameter("@wat", b.WaterConsumption),
                new SqlParameter("@pol", b.Pollution),
                new SqlParameter("@x", (object)b.CellX ?? DBNull.Value),
                new SqlParameter("@y", (object)b.CellY ?? DBNull.Value),
                new SqlParameter("@id", b.Id)
            };
            _db.ExecuteNonQuery(sql, p);
        }

        public void Delete(int id) => _db.ExecuteNonQuery("DELETE FROM Building WHERE Id=@id", new SqlParameter("@id", id));
    }
}
