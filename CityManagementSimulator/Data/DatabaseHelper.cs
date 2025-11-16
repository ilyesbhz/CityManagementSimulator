using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace CityManagementSimulator.Data
{
    public class DatabaseHelper
    {
        private static bool _initialized;
        private static readonly object _initLock = new object();

        private readonly string _conn;

        public DatabaseHelper()
        {
            _conn = ConfigurationManager.ConnectionStrings["CityDatabase"]?.ConnectionString
                    ?? throw new InvalidOperationException("Connection string 'CityDB' not found.");
            EnsureDatabaseAndSchema();
        }

        public SqlConnection GetConnection() => new SqlConnection(_conn);

        public DataTable ExecuteQuery(string sql, params SqlParameter[] pars)
        {
            using (var c = GetConnection())
            using (var cmd = new SqlCommand(sql, c))
            {
                if (pars != null) cmd.Parameters.AddRange(pars);
                using (var da = new SqlDataAdapter(cmd))
                {
                    var dt = new DataTable();
                    da.Fill(dt);
                    return dt;
                }
            }
        }

        public int ExecuteNonQuery(string sql, params SqlParameter[] pars)
        {
            using (var c = GetConnection())
            using (var cmd = new SqlCommand(sql, c))
            {
                if (pars != null) cmd.Parameters.AddRange(pars);
                c.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        public object ExecuteScalar(string sql, params SqlParameter[] pars)
        {
            using (var c = GetConnection())
            using (var cmd = new SqlCommand(sql, c))
            {
                if (pars != null) cmd.Parameters.AddRange(pars);
                c.Open();
                return cmd.ExecuteScalar();
            }
        }

        private void EnsureDatabaseAndSchema()
        {
            if (_initialized) return;
            lock (_initLock)
            {
                if (_initialized) return;

                var appCs = new SqlConnectionStringBuilder(_conn);
                var dbName = appCs.InitialCatalog;
                if (string.IsNullOrWhiteSpace(dbName))
                    throw new InvalidOperationException("Initial Catalog is missing from the CityDB connection string.");

                var masterCs = new SqlConnectionStringBuilder(appCs.ConnectionString) { InitialCatalog = "master" };

                using (var conn = new SqlConnection(masterCs.ConnectionString))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = @"
DECLARE @dbName sysname = @name;
IF DB_ID(@dbName) IS NULL
BEGIN
    DECLARE @sql nvarchar(max) = N'CREATE DATABASE ' + QUOTENAME(@dbName);
    EXEC (@sql);
END";
                    cmd.Parameters.AddWithValue("@name", dbName);
                    cmd.ExecuteNonQuery();
                }

                using (var conn = new SqlConnection(_conn))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();

                    // CityState
                    cmd.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CityState' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.CityState
    (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        CityName    NVARCHAR(200) NOT NULL,
        Budget      FLOAT NOT NULL DEFAULT(0),
        EnergyPool  FLOAT NOT NULL DEFAULT(0),
        WaterPool   FLOAT NOT NULL DEFAULT(0),
        DayCounter  INT   NOT NULL DEFAULT(0)
    );
END";
                    cmd.ExecuteNonQuery();

                    // Building
                    cmd.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Building' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.Building
    (
        Id                  INT IDENTITY(1,1) PRIMARY KEY,
        Name                NVARCHAR(200) NOT NULL,
        Type                NVARCHAR(50)  NOT NULL,
        CapacityPopulation  INT           NOT NULL DEFAULT(0),
        Cost                FLOAT         NOT NULL DEFAULT(0),
        Income              FLOAT         NOT NULL DEFAULT(0),
        EnergyConsumption   FLOAT         NOT NULL DEFAULT(0),
        WaterConsumption    FLOAT         NOT NULL DEFAULT(0),
        Pollution           FLOAT         NOT NULL DEFAULT(0),
        CellX               INT           NULL,
        CellY               INT           NULL
    );
END";
                    cmd.ExecuteNonQuery();

                    // Person
                    cmd.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Person' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.Person
    (
        Id         INT IDENTITY(1,1) PRIMARY KEY,
        FullName   NVARCHAR(200) NOT NULL,
        Age        INT           NOT NULL,
        BuildingId INT           NULL
            CONSTRAINT FK_Person_Building
            FOREIGN KEY REFERENCES dbo.Building(Id)
            ON DELETE SET NULL
    );
END";
                    cmd.ExecuteNonQuery();

                    // EconomyLog
                    cmd.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EconomyLog' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.EconomyLog
    (
        LogId           INT IDENTITY(1,1) PRIMARY KEY,
        DayNumber       INT   NOT NULL,
        TotalPopulation INT   NOT NULL,
        TotalIncome     FLOAT NOT NULL,
        TotalExpenses   FLOAT NOT NULL,
        Balance         FLOAT NOT NULL,
        TotalPollution  FLOAT NOT NULL,
        TotalEnergy     FLOAT NOT NULL,
        TotalWater      FLOAT NOT NULL,
        Happiness       FLOAT NOT NULL
    );
END";
                    cmd.ExecuteNonQuery();

                    // Seed CityState
                    cmd.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM dbo.CityState)
BEGIN
    INSERT INTO dbo.CityState (CityName, Budget, EnergyPool, WaterPool, DayCounter)
    VALUES (N'MaVille', 10000, 1000, 1000, 0);
END";
                    cmd.ExecuteNonQuery();

                    // Road (simple)
                    cmd.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Road' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.Road
    (
        Id    INT IDENTITY(1,1) PRIMARY KEY,
        CellX INT NOT NULL,
        CellY INT NOT NULL
    );
END";
                    cmd.ExecuteNonQuery();
                }

                _initialized = true;
            }
        }
    }
}