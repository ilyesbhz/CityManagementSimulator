using System;
using System.Data;
using System.Data.SqlClient;
using CityManagementSimulator.Data;
using CityManagementSimulator.Models;

namespace CityManagementSimulator.Repositories
{
    public class CityRepository
    {
        private readonly DatabaseHelper _db = new DatabaseHelper();

        public CityState GetState()
        {
            var dt = _db.ExecuteQuery("SELECT TOP 1 * FROM CityState ORDER BY Id");
            if (dt.Rows.Count == 0) return null;
            var r = dt.Rows[0];
            return new CityState
            {
                Id = Convert.ToInt32(r["Id"]),
                CityName = r["CityName"].ToString(),
                Budget = r["Budget"] == DBNull.Value ? 0 : Convert.ToDouble(r["Budget"]),
                EnergyPool = r["EnergyPool"] == DBNull.Value ? 0 : Convert.ToDouble(r["EnergyPool"]),
                WaterPool = r["WaterPool"] == DBNull.Value ? 0 : Convert.ToDouble(r["WaterPool"]),
                DayCounter = r["DayCounter"] == DBNull.Value ? 0 : Convert.ToInt32(r["DayCounter"])
            };
        }

        public void UpdateState(CityState s)
        {
            _db.ExecuteNonQuery("UPDATE CityState SET CityName=@n, Budget=@b, EnergyPool=@e, WaterPool=@w, DayCounter=@d",
                new SqlParameter("@n", s.CityName),
                new SqlParameter("@b", s.Budget),
                new SqlParameter("@e", s.EnergyPool),
                new SqlParameter("@w", s.WaterPool),
                new SqlParameter("@d", s.DayCounter));
        }

        public void IncrementDay() => _db.ExecuteNonQuery("UPDATE CityState SET DayCounter = DayCounter + 1");

        public int GetDay() => Convert.ToInt32(_db.ExecuteScalar("SELECT DayCounter FROM CityState") ?? 0);

        public void LogDay(int day, int population, double income, double expenses, double balance, double pollution, double energy, double water, double happiness)
        {
            string sql = @"INSERT INTO EconomyLog (DayNumber, TotalPopulation, TotalIncome, TotalExpenses, Balance, TotalPollution, TotalEnergy, TotalWater, Happiness)
                           VALUES (@day,@pop,@inc,@exp,@bal,@pol,@eng,@wat,@hap)";
            var p = new[] {
                new SqlParameter("@day", day), new SqlParameter("@pop", population), new SqlParameter("@inc", income),
                new SqlParameter("@exp", expenses), new SqlParameter("@bal", balance), new SqlParameter("@pol", pollution),
                new SqlParameter("@eng", energy), new SqlParameter("@wat", water), new SqlParameter("@hap", happiness)
            };
            _db.ExecuteNonQuery(sql, p);
        }

        public DataTable GetEconomyLog() => _db.ExecuteQuery("SELECT * FROM EconomyLog ORDER BY LogId");
    }
}
