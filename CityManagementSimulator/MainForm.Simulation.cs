using System;
using System.Linq;
using System.Windows.Forms;
using CityManagementSimulator.Models; // ADDED

namespace CityManagementSimulator
{
    public partial class MainForm
    {
        private void BtnNextDay_Click(object sender, EventArgs e)
        {
            var buildings = _buildingRepo.GetAll();
            int population = _personRepo.CountAll();
            var state = _cityRepo.GetState();

            // Demand
            double income = buildings.Sum(b => b.Income);
            double totalEnergyDemand = buildings.Sum(b => b.EnergyConsumption) + population * 0.2;
            double totalWaterDemand  = buildings.Sum(b => b.WaterConsumption)  + population * 0.3;
            double pollution         = buildings.Sum(b => b.Pollution) + population * 0.1;

            // Actual consumption limited by pools
            double energyConsumed = Math.Min(state.EnergyPool, totalEnergyDemand);
            double waterConsumed  = Math.Min(state.WaterPool,  totalWaterDemand);
            double unmetEnergy    = totalEnergyDemand - energyConsumed;
            double unmetWater     = totalWaterDemand  - waterConsumed;

            state.EnergyPool -= energyConsumed;
            state.WaterPool  -= waterConsumed;

            // Expenses charged on what was actually consumed
            double expensesEnergy      = energyConsumed * 0.5;
            double expensesWater       = waterConsumed  * 0.2;
            double expensesPopulation  = population     * 0.1;
            double expenses            = expensesEnergy + expensesWater + expensesPopulation;

            state.Budget = state.Budget + income - expenses;
            state.DayCounter += 1;
            _cityRepo.UpdateState(state);

            // Happiness: base - pollution + parks + budget, then penalty for shortages
            int parks = buildings.Count(b => b.Type == "Park");
            double happiness = 70
                - pollution * 0.4
                + parks * 5
                + (state.Budget / 10000.0) * 5;

            // Shortage penalty (scale by unmet fraction)
            if (totalEnergyDemand > 0)
                happiness -= (unmetEnergy / totalEnergyDemand) * 20.0;
            if (totalWaterDemand > 0)
                happiness -= (unmetWater / totalWaterDemand) * 20.0;

            happiness = Math.Max(0, Math.Min(100, happiness));

            // Note: columns TotalEnergy/TotalWater store consumption
            _cityRepo.LogDay(state.DayCounter, population, income, expenses, state.Budget, pollution, energyConsumed, waterConsumed, happiness);

            RefreshDashboard();
            MessageBox.Show("Day simulated.");
        }

        private void RefreshDashboard()
        {
            var state = _cityRepo.GetState();

            lblDay.Text        = state.DayCounter.ToString();
            lblBudget.Text     = Math.Round(state.Budget, 2).ToString();
            lblEnergy.Text     = Math.Round(state.EnergyPool, 2).ToString();
            lblWater.Text      = Math.Round(state.WaterPool, 2).ToString();
            lblPopulation.Text = _personRepo.CountAll().ToString();

            var buildings = _buildingRepo.GetAll();
            double pollution = buildings.Sum(b => b.Pollution) + _personRepo.CountAll() * 0.1;
            lblPollution.Text = Math.Round(pollution, 2).ToString();

            int parks = buildings.Count(b => b.Type == "Park");
            double happiness = 70 - pollution * 0.4 + parks * 5 + (state.Budget / 10000.0) * 5;
            happiness = Math.Max(0, Math.Min(100, happiness)); // keep UI consistent with logs
            lblHappiness.Text = Math.Round(happiness, 2).ToString();

            foreach (var kv in buildingControls)
            {
                var ctrl = kv.Value;
                var badge = ctrl.Controls.OfType<Control>().FirstOrDefault(c => c.Name == "badge");
                if (badge != null)
                    badge.Text = GetOccupantCountForBadge(kv.Key).ToString();
            }
        }

        private void RefreshChart()
        {
            // Moved to ChartForm
        }

        private void LoadState()
        {
            // Repositories fetch on demand
        }
    }
}
