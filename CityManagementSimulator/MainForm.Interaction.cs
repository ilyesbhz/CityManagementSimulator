using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CityManagementSimulator.Models; // ADDED

namespace CityManagementSimulator
{
    public partial class MainForm
    {
        private void MapPanel_MouseClick(object sender, MouseEventArgs e)
        {
            if (selectedTool == "Move") return;

            int cellX = e.X / tileSize;
            int cellY = e.Y / tileSize;

            var buildingOnTile = _buildingRepo.GetAll()
                .FirstOrDefault(building => building.CellX == cellX && building.CellY == cellY);

            if (buildingOnTile != null)
            {
                ShowBuildingOnRight(buildingOnTile.Id);
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                if (selectedTool == "Road")
                {
                    var existing = _roadRepo.GetAll().Find(r => r.CellX == cellX && r.CellY == cellY);
                    if (existing == null)
                    {
                        _roadRepo.Add(new Road { CellX = cellX, CellY = cellY, Name = GenerateName("Road") });
                        mapPanel.Invalidate();
                    }
                    return;
                }

                if (string.IsNullOrEmpty(selectedTool) || !definitions.ContainsKey(selectedTool))
                {
                    MessageBox.Show("Select House, Factory, Park or Commerce first.");
                    return;
                }

                var def = definitions[selectedTool];

                var newBuilding = new Building
                {
                    Name = GenerateName(def.Type),
                    Type = def.Type,
                    CapacityPopulation = def.CapacityPopulation,
                    Cost = def.Cost,
                    Income = def.Income,
                    EnergyConsumption = def.EnergyConsumption,
                    WaterConsumption = def.WaterConsumption,
                    Pollution = def.Pollution,
                    CellX = cellX,
                    CellY = cellY
                };

                // After adding a building (LEFT CLICK block), append CenterViewOnContent():
                int id = _buildingRepo.Add(newBuilding);
                newBuilding.Id = id;

                RenderAllBuildings();
                ShowBuildingOnRight(id);
                return;
            }

            if (e.Button == MouseButtons.Right)
            {
                var cm = new ContextMenuStrip();
                foreach (var kv in definitions)
                {
                    var item = cm.Items.Add("Add " + kv.Key);
                    item.Name = kv.Key;
                }

                cm.ItemClicked += (ss, ee2) =>
                {
                    string type = ee2.ClickedItem.Name;
                    if (!definitions.ContainsKey(type)) return;
                    var def = definitions[type];

                    var newBuilding = new Building
                    {
                        Name = GenerateName(def.Type),
                        Type = def.Type,
                        CapacityPopulation = def.CapacityPopulation,
                        Cost = def.Cost,
                        Income = def.Income,
                        EnergyConsumption = def.EnergyConsumption,
                        WaterConsumption = def.WaterConsumption,
                        Pollution = def.Pollution,
                        CellX = cellX,
                        CellY = cellY
                    };

                    int id = _buildingRepo.Add(newBuilding);
                    newBuilding.Id = id;

                    RenderAllBuildings();
                    ShowBuildingOnRight(id);
                };

                cm.Show(Cursor.Position);
            }
        }

        private void BuildingControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (!(sender is Control ctrl)) return;

            if (selectedTool == "Move" && e.Button == MouseButtons.Left)
            {
                isPanning = true;
                panStartClient = mapPanel.PointToClient(Cursor.Position);
                panStartScroll = new Point(-mapContainer.AutoScrollPosition.X, -mapContainer.AutoScrollPosition.Y);
                mapPanel.Cursor = Cursors.SizeAll;
                return;
            }

            if (!(ctrl.Tag is int bid)) return;

            var building = _buildingRepo.GetAll().FirstOrDefault(x => x.Id == bid);
            if (building == null) return;

            if (e.Button == MouseButtons.Right)
            {
                var cm = new ContextMenuStrip();
                cm.Items.Add("Edit").Name = "edit";
                cm.Items.Add("Delete").Name = "delete";
                cm.Items.Add("Show occupants").Name = "occupants";
                cm.ItemClicked += (ss, ee) =>
                {
                    if (ee.ClickedItem.Name == "edit") EditBuilding(bid);
                    else if (ee.ClickedItem.Name == "delete") DeleteBuilding(bid);
                    else if (ee.ClickedItem.Name == "occupants") ShowOccupants(bid);
                };
                cm.Show(Cursor.Position);
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                draggingControl = ctrl;
                dragStartClient = mapPanel.PointToClient(Cursor.Position);
                dragStartControlLocation = ctrl.Location;
                isDragging = false;
                didDrag = false;
            }
        }

        private void BuildingControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (isPanning && selectedTool == "Move")
            {
                var cur = mapPanel.PointToClient(Cursor.Position);
                var delta = new Point(cur.X - panStartClient.X, cur.Y - panStartClient.Y);

                int newX = Math.Max(0, panStartScroll.X - delta.X);
                int newY = Math.Max(0, panStartScroll.Y - delta.Y);

                int maxX = Math.Max(0, mapPanel.Width - mapContainer.ClientSize.Width);
                int maxY = Math.Max(0, mapPanel.Height - mapContainer.ClientSize.Height);

                if (newX > maxX) newX = maxX;
                if (newY > maxY) newY = maxY;

                mapContainer.AutoScrollPosition = new Point(newX, newY);
                mapPanel.Location = mapContainer.AutoScrollPosition; // ADDED
                return;
            }

            if (draggingControl == null) return;

            var curClient = mapPanel.PointToClient(Cursor.Position);
            var deltaMove = new Point(curClient.X - dragStartClient.X, curClient.Y - dragStartClient.Y);

            if (!isDragging)
            {
                int threshX = SystemInformation.DragSize.Width / 2;
                int threshY = SystemInformation.DragSize.Height / 2;
                if (Math.Abs(deltaMove.X) < threshX && Math.Abs(deltaMove.Y) < threshY) return;
                isDragging = true;
            }

            didDrag = true;
            var newLoc = new Point(dragStartControlLocation.X + deltaMove.X, dragStartControlLocation.Y + deltaMove.Y);
            newLoc.X = Math.Max(2, Math.Min(mapPanel.Width - draggingControl.Width - 2, newLoc.X));
            newLoc.Y = Math.Max(2, Math.Min(mapPanel.Height - draggingControl.Height - 2, newLoc.Y));
            draggingControl.Location = newLoc;
            draggingControl.BringToFront();
        }

        private void BuildingControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (isPanning && selectedTool == "Move" && e.Button == MouseButtons.Left)
            {
                isPanning = false;
                mapPanel.Cursor = Cursors.Hand;
                return;
            }

            if (!(sender is Control ctrl)) return;
            if (!(ctrl.Tag is int bid)) return;
            if (draggingControl != ctrl) return;

            if (isDragging && didDrag)
            {
                int newCellX = Math.Max(0, (draggingControl.Location.X - 2 + tileSize / 2) / tileSize);
                int newCellY = Math.Max(0, (draggingControl.Location.Y - 2 + tileSize / 2) / tileSize);

                var other = _buildingRepo.GetAll().FirstOrDefault(b => b.Id != bid && b.CellX == newCellX && b.CellY == newCellY);
                if (other != null)
                {
                    var oldBuilding = _buildingRepo.GetAll().FirstOrDefault(b => b.Id == bid);
                    if (oldBuilding != null && oldBuilding.CellX.HasValue && oldBuilding.CellY.HasValue)
                        draggingControl.Location = new Point(oldBuilding.CellX.Value * tileSize + 2, oldBuilding.CellY.Value * tileSize + 2);

                    MessageBox.Show("Cannot move: target tile is occupied.", "Move failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    var building = _buildingRepo.GetAll().FirstOrDefault(x => x.Id == bid);
                    if (building != null)
                    {
                        building.CellX = newCellX;
                        building.CellY = newCellY;
                        _buildingRepo.Update(building);
                        draggingControl.Location = new Point(newCellX * tileSize + 2, newCellY * tileSize + 2);
                    }
                }
            }
            else
            {
                ShowBuildingOnRight(bid);
            }

            isDragging = false;
            didDrag = false;
            draggingControl = null;
        }

        private void MapPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (selectedTool == "Move" && e.Button == MouseButtons.Left)
            {
                isPanning = true;
                panStartClient = e.Location;
                panStartScroll = new Point(-mapContainer.AutoScrollPosition.X, -mapContainer.AutoScrollPosition.Y);
                mapPanel.Cursor = Cursors.SizeAll;
            }
        }

        private void MapPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isPanning || selectedTool != "Move") return;

            var delta = new Point(e.Location.X - panStartClient.X, e.Location.Y - panStartClient.Y);
            int newX = Math.Max(0, panStartScroll.X - delta.X);
            int newY = Math.Max(0, panStartScroll.Y - delta.Y);

            int maxX = Math.Max(0, mapPanel.Width - mapContainer.ClientSize.Width);
            int maxY = Math.Max(0, mapPanel.Height - mapContainer.ClientSize.Height);

            if (newX > maxX) newX = maxX;
            if (newY > maxY) newY = maxY;

            mapContainer.AutoScrollPosition = new Point(newX, newY);
            mapPanel.Location = mapContainer.AutoScrollPosition; // ADDED
        }

        private void MapPanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (isPanning && e.Button == MouseButtons.Left)
            {
                isPanning = false;
                mapPanel.Cursor = (selectedTool == "Move") ? Cursors.Hand : Cursors.Default;
            }
        }
    }
}
