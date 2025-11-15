using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using CityManagementSimulator.Data;
using CityManagementSimulator.Models;

namespace CityManagementSimulator.Repositories
{
    public class PersonRepository
    {
        private readonly DatabaseHelper _db = new DatabaseHelper();

        public List<Person> GetAll()
        {
            var list = new List<Person>();
            var dt = _db.ExecuteQuery("SELECT * FROM Person");
            foreach (DataRow r in dt.Rows)
                list.Add(new Person
                {
                    Id = Convert.ToInt32(r["Id"]),
                    FullName = r["FullName"].ToString(),
                    Age = r["Age"] == DBNull.Value ? 0 : Convert.ToInt32(r["Age"]),
                    BuildingId = r["BuildingId"] == DBNull.Value ? (int?)null : Convert.ToInt32(r["BuildingId"])
                });
            return list;
        }

        public int Add(Person p)
        {
            string sql = "INSERT INTO Person (FullName, Age, BuildingId) VALUES (@n,@age,@b); SELECT SCOPE_IDENTITY();";
            var pars = new[] {
                new SqlParameter("@n", p.FullName),
                new SqlParameter("@age", p.Age),
                new SqlParameter("@b", (object)p.BuildingId ?? DBNull.Value)
            };
            var obj = _db.ExecuteScalar(sql, pars);
            return Convert.ToInt32(obj);
        }

        public void Update(Person p)
        {
            string sql = "UPDATE Person SET FullName=@n, Age=@age, BuildingId=@b WHERE Id=@id";
            var pars = new[] {
                new SqlParameter("@n", p.FullName),
                new SqlParameter("@age", p.Age),
                new SqlParameter("@b", (object)p.BuildingId ?? DBNull.Value),
                new SqlParameter("@id", p.Id)
            };
            _db.ExecuteNonQuery(sql, pars);
        }

        public void Delete(int id) => _db.ExecuteNonQuery("DELETE FROM Person WHERE Id=@id", new SqlParameter("@id", id));

        public int CountAll() => Convert.ToInt32(_db.ExecuteScalar("SELECT COUNT(*) FROM Person") ?? 0);
    }
}
