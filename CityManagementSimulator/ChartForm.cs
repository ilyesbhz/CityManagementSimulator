using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using CityManagementSimulator.Repositories;

namespace CityManagementSimulator
{
    public class ChartForm : Form
    {
        private readonly CityRepository _cityRepo = new CityRepository();
        private Chart chart;
        private Button btnRefresh;

        public ChartForm()
        {
            Text = "City Economy Charts";
            Width = 800;
            Height = 600;
            StartPosition = FormStartPosition.CenterParent;
            InitializeUI();
        }

        private void InitializeUI()
        {
            chart = new Chart { Dock = DockStyle.Fill, BackColor = Color.White };
            var area = new ChartArea("area") { BackColor = Color.WhiteSmoke };
            area.AxisX.Title = "Day";
            area.AxisY.Title = "Population";
            area.AxisY2.Title = "Budget";
            chart.ChartAreas.Add(area);
            chart.Legends.Add(new Legend());
            chart.Series.Add(new Series("Population") { ChartType = SeriesChartType.Line, BorderWidth = 3 });
            chart.Series.Add(new Series("Budget") { ChartType = SeriesChartType.Line, YAxisType = AxisType.Secondary, BorderWidth = 2 });

            btnRefresh = new Button { Text = "Refresh", Dock = DockStyle.Top, Height = 36 };
            btnRefresh.Click += (s, e) => LoadChartData();

            Controls.Add(chart);
            Controls.Add(btnRefresh);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadChartData();
        }

        private void LoadChartData()
        {
            var dt = _cityRepo.GetEconomyLog();
            chart.Series["Population"].Points.Clear();
            chart.Series["Budget"].Points.Clear();

            foreach (DataRow r in dt.Rows)
            {
                int day = Convert.ToInt32(r["DayNumber"]);
                int pop = Convert.ToInt32(r["TotalPopulation"]);
                double bal = Convert.ToDouble(r["Balance"]);
                chart.Series["Population"].Points.AddXY(day, pop);
                chart.Series["Budget"].Points.AddXY(day, bal);
            }
        }
    }
}