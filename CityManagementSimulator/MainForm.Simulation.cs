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

            double income = buildings.Sum(b => b.Income);
            double totalEnergyDemand = buildings.Sum(b => b.EnergyConsumption) + population * 0.2;
            double totalWaterDemand = buildings.Sum(b => b.WaterConsumption) + population * 0.3;
            double pollution = buildings.Sum(b => b.Pollution) + population * 0.1;
            double expenses = totalEnergyDemand * 0.5 + totalWaterDemand * 0.2 + population * 0.1;

            double energyConsumed = Math.Min(state.EnergyPool, totalEnergyDemand);
            double waterConsumed = Math.Min(state.WaterPool, totalWaterDemand);
            state.EnergyPool -= energyConsumed;
            state.WaterPool -= waterConsumed;

            state.Budget = state.Budget + income - expenses;
            state.DayCounter += 1;
            _cityRepo.UpdateState(state);

            double parks = buildings.Count(b => b.Type == "Park");
            double happiness = 70 - pollution * 0.4 + parks * 5 + (state.Budget / 10000.0) * 5;
            happiness = Math.Max(0, Math.Min(100, happiness));

            _cityRepo.LogDay(state.DayCounter, population, income, expenses, state.Budget, pollution, energyConsumed, waterConsumed, happiness);

            RefreshDashboard();
            MessageBox.Show("Day simulated.");
        }

        private void RefreshDashboard()
        {
            var state = _cityRepo.GetState();

            lblDay.Text = state.DayCounter.ToString();
            lblBudget.Text = Math.Round(state.Budget, 2).ToString();
            lblEnergy.Text = Math.Round(state.EnergyPool, 2).ToString();
            lblWater.Text = Math.Round(state.WaterPool, 2).ToString();
            lblPopulation.Text = _personRepo.CountAll().ToString();

            var buildings = _buildingRepo.GetAll();
            double pollution = buildings.Sum(b => b.Pollution) + _personRepo.CountAll() * 0.1;
            lblPollution.Text = Math.Round(pollution, 2).ToString();

            double parks = buildings.Count(b => b.Type == "Park");
            double happiness = 70 - pollution * 0.4 + parks * 5 + (state.Budget / 10000.0) * 5;
            lblHappiness.Text = Math.Round(happiness, 2).ToString();

            foreach (var kv in buildingControls)
            {
                var ctrl = kv.Value;
                var badge = ctrl.Controls.OfType<Control>().FirstOrDefault(c => c.Name == "badge");
                if (badge != null)
                {
                    badge.Text = GetOccupantCountForBadge(kv.Key).ToString();
                }
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
