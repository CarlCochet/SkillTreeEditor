using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Image = System.Windows.Controls.Image;
using Panel = System.Windows.Controls.Panel;

namespace SkillTreeEditor.Rendering;

public class SphereBoardRenderer
{
    public const double TileSize = 40.0;

    private readonly Canvas _canvas;
    private readonly Dictionary<int, ImageSource> _images;
    private readonly Dictionary<(int X, int Y), List<UIElement>> _tiles = [];

    public Dictionary<(int X, int Y), Brush>? CostBrushes { get; set; }

    private UIElement? _selectionMarker;
    private UIElement? _teleportLine;

    public SphereBoardRenderer(Canvas canvas, Dictionary<int, ImageSource> images)
    {
        _canvas = canvas;
        _images = images;
    }

    public void Clear()
    {
        _canvas.Children.Clear();
        _tiles.Clear();
        _selectionMarker = null;
        _teleportLine = null;
    }

    public void DrawBoard(SphereBoardData board, IEnumerable<SphereData> spheres)
    {
        Clear();

        foreach (var sphere in spheres)
        {
            if (sphere.SphereBoardId == board.Id)
                DrawSphere(sphere, false);
        }

        DrawTile(board.StartX, board.StartY, Brushes.Lime);
    }

    public void DrawSphere(SphereData sphere, bool isStartPosition)
    {
        var iconId = Helper.GetIconIdFromSphere(sphere);
        _images.TryGetValue(iconId, out var icon);

        Brush brush = Brushes.BurlyWood;
        if (CostBrushes is not null && CostBrushes.TryGetValue((sphere.XPosition, sphere.YPosition), out var costBrush))
            brush = costBrush;

        DrawTile(sphere.XPosition, sphere.YPosition, brush, icon);

        if (isStartPosition)
            DrawTile(sphere.XPosition, sphere.YPosition, Brushes.Lime);
    }

    public void DrawTile(int x, int y, Brush? brush = null, ImageSource? icon = null)
    {
        RemoveTile(x, y);

        var elements = new List<UIElement>();

        if (brush is not null)
        {
            var tile = new Border
            {
                Width = TileSize,
                Height = TileSize,
                Background = brush,
                BorderBrush = Brushes.Transparent
            };

            Canvas.SetLeft(tile, x * TileSize);
            Canvas.SetTop(tile, _canvas.Height - y * TileSize);
            elements.Add(tile);
            _canvas.Children.Add(tile);
        }

        if (icon is not null)
        {
            var image = new Image
            {
                Width = TileSize,
                Height = TileSize,
                Source = icon,
                Stretch = Stretch.Fill,
                SnapsToDevicePixels = true,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(image, x * TileSize);
            Canvas.SetTop(image, _canvas.Height - y * TileSize);
            elements.Add(image);
            _canvas.Children.Add(image);
        }

        if (elements.Count > 0)
            _tiles[(x, y)] = elements;
    }

    public void RemoveTile(int x, int y)
    {
        if (!_tiles.TryGetValue((x, y), out var elements))
            return;

        foreach (var element in elements)
            _canvas.Children.Remove(element);

        _tiles.Remove((x, y));
    }

    public void ShowSelection(int x, int y, Brush brush)
    {
        HideSelection();

        const double diameter = TileSize + 10;
        var circle = new Ellipse
        {
            Width = diameter,
            Height = diameter,
            Fill = Brushes.Transparent,
            Stroke = brush,
            StrokeThickness = 4,
            IsHitTestVisible = false,
            SnapsToDevicePixels = true
        };

        Canvas.SetLeft(circle, x * TileSize - (diameter - TileSize) / 2);
        Canvas.SetTop(circle, _canvas.Height - y * TileSize - (diameter - TileSize) / 2);
        Panel.SetZIndex(circle, 900);
        _canvas.Children.Add(circle);
        _selectionMarker = circle;
    }

    public void HideSelection()
    {
        if (_selectionMarker is null)
            return;

        _canvas.Children.Remove(_selectionMarker);
        _selectionMarker = null;
    }

    public void ShowTeleportLine(SphereData sphere)
    {
        HideTeleportLine();

        var line = new Line
        {
            X1 = sphere.XPosition * TileSize + TileSize / 2,
            Y1 = _canvas.Height - (sphere.YPosition - 1) * TileSize - TileSize / 2,
            X2 = sphere.TeleportXPosition * TileSize + TileSize / 2,
            Y2 = _canvas.Height - (sphere.TeleportYPosition - 1) * TileSize - TileSize / 2,
            Stroke = Brushes.DeepSkyBlue,
            StrokeThickness = 5,
            SnapsToDevicePixels = true,
            IsHitTestVisible = false
        };

        Panel.SetZIndex(line, 1000);
        _canvas.Children.Add(line);
        _teleportLine = line;
    }

    public void HideTeleportLine()
    {
        if (_teleportLine is null)
            return;

        _canvas.Children.Remove(_teleportLine);
        _teleportLine = null;
    }
}
