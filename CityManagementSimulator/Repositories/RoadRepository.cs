using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using CityManagementSimulator.Data;
using CityManagementSimulator.Models;

namespace CityManagementSimulator.Repositories
{
    public class RoadRepository
    {
        private readonly DatabaseHelper _db = new DatabaseHelper();

        public List<Road> GetAll()
        {
            var list = new List<Road>();
            var dt = _db.ExecuteQuery("SELECT Id, CellX, CellY FROM Road");
            foreach (DataRow r in dt.Rows)
            {
                list.Add(new Road
                {
                    Id = Convert.ToInt32(r["Id"]),
                    CellX = Convert.ToInt32(r["CellX"]),
                    CellY = Convert.ToInt32(r["CellY"])
                });
            }
            return list;
        }

        public int Add(Road r)
        {
            var obj = _db.ExecuteScalar(
                "INSERT INTO Road (CellX, CellY) VALUES (@x,@y); SELECT SCOPE_IDENTITY();",
                new SqlParameter("@x", r.CellX),
                new SqlParameter("@y", r.CellY));
            return Convert.ToInt32(obj);
        }

        public void Delete(int id) =>
            _db.ExecuteNonQuery("DELETE FROM Road WHERE Id=@id", new SqlParameter("@id", id));
    }
}
