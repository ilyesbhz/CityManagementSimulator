using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CityManagementSimulator.Models; // ensure present

namespace CityManagementSimulator
{
    public partial class MainForm
    {
        // Debounce timer for smoother viewport updates on container resize
        private Timer _viewportResizeTimer;

        private void MapPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var clip = e.ClipRectangle;

            int startCol = Math.Max(0, clip.Left / tileSize);
            int endCol = Math.Min(gridWidth - 1, clip.Right / tileSize + 1);
            int startRow = Math.Max(0, clip.Top / tileSize);
            int endRow = Math.Min(gridHeight - 1, clip.Bottom / tileSize + 1);

            using (var pen = new Pen(Color.Gainsboro))
            {
                for (int c = startCol; c <= endCol + 1; c++)
                {
                    int x = c * tileSize;
                    g.DrawLine(pen, x, startRow * tileSize, x, (endRow + 1) * tileSize);
                }
                for (int r = startRow; r <= endRow + 1; r++)
                {
                    int y = r * tileSize;
                    g.DrawLine(pen, startCol * tileSize, y, (endCol + 1) * tileSize, y);
                }
            }

            using (var f = new Font("Segoe UI", Math.Max(6, tileSize / 12f)))
            using (var br = new SolidBrush(Color.Gray))
            {
                for (int c = startCol; c <= endCol; c++)
                    for (int r = startRow; r <= endRow; r++)
                    {
                        int x = c * tileSize + 2;
                        int y = r * tileSize + 2;
                        g.DrawString($"{c},{r}", f, br, x, y);
                    }
            }

            DrawRoads(g);
        }

        // Keep the same center cell in view when tile size changes
        private void ResizeMapSurface(bool preserveView = true, int? oldTileSizeOverride = null)
        {
            if (mapContainer == null || mapPanel == null) return;

            int oldTs = oldTileSizeOverride ?? tileSize;
            var newSize = new Size(gridWidth * tileSize, gridHeight * tileSize);

            // current view (positive values)
            int viewX = Math.Max(0, -mapContainer.AutoScrollPosition.X);
            int viewY = Math.Max(0, -mapContainer.AutoScrollPosition.Y);

            // avoid div by zero when form is minimized/very small
            int clientW = Math.Max(1, mapContainer.ClientSize.Width);
            int clientH = Math.Max(1, mapContainer.ClientSize.Height);

            // center cell based on previous tile size
            int centerPixelX = viewX + clientW / 2;
            int centerPixelY = viewY + clientH / 2;
            int centerCellX = Math.Max(0, Math.Min(gridWidth - 1, centerPixelX / oldTs));
            int centerCellY = Math.Max(0, Math.Min(gridHeight - 1, centerPixelY / oldTs));

            // set new surface size
            mapPanel.Size = newSize;
            mapContainer.AutoScrollMinSize = newSize;

            if (preserveView)
            {
                int newCenterPixelX = centerCellX * tileSize + tileSize / 2;
                int newCenterPixelY = centerCellY * tileSize + tileSize / 2;
                int targetScrollX = Math.Max(0, newCenterPixelX - clientW / 2);
                int targetScrollY = Math.Max(0, newCenterPixelY - clientH / 2);

                // clamp to max scroll
                int maxX = Math.Max(0, newSize.Width - clientW);
                int maxY = Math.Max(0, newSize.Height - clientH);
                if (targetScrollX > maxX) targetScrollX = maxX;
                if (targetScrollY > maxY) targetScrollY = maxY;

                mapContainer.AutoScrollPosition = new Point(targetScrollX, targetScrollY);
            }

            // keep child aligned with the scroll offset to avoid jitter
            mapPanel.Location = mapContainer.AutoScrollPosition;
        }

        // Keep child location synced on any scroll
        private void MapContainer_Scroll(object sender, ScrollEventArgs e)
        {
            mapPanel.Location = mapContainer.AutoScrollPosition;
        }

        // Debounced resize handling of the viewport (form/panel resize)
        private void MapContainer_Resize(object sender, EventArgs e)
        {
            if (_viewportResizeTimer == null)
            {
                _viewportResizeTimer = new Timer { Interval = 120 };
                _viewportResizeTimer.Tick += ViewportResizeTimer_Tick;
            }
            _viewportResizeTimer.Stop();
            _viewportResizeTimer.Start();
        }

        private void ViewportResizeTimer_Tick(object sender, EventArgs e)
        {
            _viewportResizeTimer.Stop();
            // Client size changed; keep current center cell in view without changing tile size
            ResizeMapSurface(true, tileSize);
            mapPanel.Invalidate();
        }

        private void MapPanel_MouseWheel(object sender, MouseEventArgs e)
        {
            // Zoom disabled: ignoring mouse wheel input.
            // (Line kept to avoid removing event subscription; safe no-op.)
            return;
        }

        private void ApplyTileSizeChange(int oldTileSize)
        {
            ResizeMapSurface(true, oldTileSize);
            RenderAllBuildings();
            mapPanel.Invalidate();

            if (comboTileSize.Items.Contains(tileSize.ToString()))
                comboTileSize.SelectedItem = tileSize.ToString();
        }

        private void DrawRoads(Graphics g)
        {
            var roads = _roadRepo.GetAll();
            if (roads.Count == 0) return;

            foreach (var r in roads)
            {
                int x = r.CellX * tileSize;
                int y = r.CellY * tileSize;
                var dest = new Rectangle(x, y, tileSize, tileSize);

                using (var asphalt = new SolidBrush(Color.FromArgb(64, 64, 64)))
                using (var outline = new Pen(Color.FromArgb(90, 90, 90), 1))
                {
                    g.FillRectangle(asphalt, dest);
                    g.DrawRectangle(outline, new Rectangle(dest.X, dest.Y, dest.Width - 1, dest.Height - 1));
                }

                int cx = dest.Left + dest.Width / 2;
                int cy = dest.Top + dest.Height / 2;
                int margin = Math.Max(4, tileSize / 10);
                int lineWidth = Math.Max(1, tileSize / 40);

                using (var mark = new Pen(Color.FromArgb(230, 230, 230), lineWidth))
                {
                    mark.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    g.DrawLine(mark, dest.Left + margin, cy, dest.Right - margin, cy);
                    g.DrawLine(mark, cx, dest.Top + margin, cx, dest.Bottom - margin);
                }

                using (var nameFont = new Font("Segoe UI", Math.Max(7, tileSize / 10f), FontStyle.Bold))
                using (var nameBrush = new SolidBrush(Color.White))
                {
                    var sz = g.MeasureString(r.Name, nameFont);
                    var namePoint = new PointF(dest.Left + (dest.Width - sz.Width) / 2f, dest.Top + (dest.Height - sz.Height) / 2f);
                    g.DrawString(r.Name, nameFont, nameBrush, namePoint);
                }
            }
        }

        private void RenderAllBuildings()
        {
            foreach (var kv in buildingControls)
            {
                var c = kv.Value;
                if (mapPanel.Controls.Contains(c)) mapPanel.Controls.Remove(c);
            }
            buildingControls.Clear();

            var buildings = _buildingRepo.GetAll();
            foreach (var b in buildings)
            {
                if (b.CellX.HasValue && b.CellY.HasValue)
                    RenderBuilding(b, b.Id);
            }
        }

        private void RenderBuilding(Building b, int id)
        {
            if (!b.CellX.HasValue || !b.CellY.HasValue) return;

            if (buildingControls.TryGetValue(id, out var old))
            {
                if (mapPanel.Controls.Contains(old)) mapPanel.Controls.Remove(old);
                buildingControls.Remove(id);
            }

            var ctrl = new Panel
            {
                Size = new Size(tileSize, tileSize),
                Location = new Point(b.CellX.Value * tileSize, b.CellY.Value * tileSize),
                BackColor = GetColorByType(b.Type),
                Tag = id,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = Padding.Empty
            };

            if (buildingImages.TryGetValue(b.Type, out var img))
            {
                ctrl.BackgroundImage = img;
                ctrl.BackgroundImageLayout = ImageLayout.Stretch;
            }

            var lbl = new Label
            {
                Text = $"{b.Type}\n{b.Name}",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomCenter,
                Font = new Font("Segoe UI", 8),
                BackColor = Color.FromArgb(96, Color.White)
            };
            ctrl.Controls.Add(lbl);

            var badge = new Label
            {
                Text = GetOccupantCountForBadge(id).ToString(),
                AutoSize = false,
                Size = new Size(28, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(255, 255, 200),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Location = new Point(Math.Max(0, ctrl.Width - 28 - 2), 2),
                Name = "badge"
            };
            ctrl.Controls.Add(badge);
            badge.BringToFront();

            var tip = new ToolTip();
            tip.SetToolTip(ctrl, $"{b.Name} ({b.Type})");

            ctrl.MouseDown += BuildingControl_MouseDown;
            ctrl.MouseMove += BuildingControl_MouseMove;
            ctrl.MouseUp += BuildingControl_MouseUp;

            lbl.MouseDown += (s, e) => BuildingControl_MouseDown(ctrl, e);
            lbl.MouseMove += (s, e) => BuildingControl_MouseMove(ctrl, e);
            lbl.MouseUp += (s, e) => BuildingControl_MouseUp(ctrl, e);
            badge.MouseDown += (s, e) => BuildingControl_MouseDown(ctrl, e);
            badge.MouseMove += (s, e) => BuildingControl_MouseMove(ctrl, e);
            badge.MouseUp += (s, e) => BuildingControl_MouseUp(ctrl, e);

            mapPanel.Controls.Add(ctrl);
            buildingControls[id] = ctrl;
        }

        private int GetOccupantCountForBadge(int buildingId)
        {
            return _personRepo.GetAll().Count(p => p.BuildingId == buildingId);
        }

        private Color GetColorByType(string type)
        {
            switch (type)
            {
                case "House": return Color.FromArgb(173, 216, 230);
                case "Factory": return Color.FromArgb(240, 128, 128);
                case "Park": return Color.FromArgb(144, 238, 144);
                case "Commerce": return Color.FromArgb(255, 228, 181);
                default: return Color.LightGray;
            }
        }
    }
}
