using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SkillTreeEditor.Enums;
using SkillTreeEditor.Rendering;
using SkillTreeEditor.Services;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using ComboBox = System.Windows.Controls.ComboBox;
using ListBox = System.Windows.Controls.ListBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using SharpImage = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp.PixelFormats;
using SkillTreeEditor.Data;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Color = System.Windows.Media.Color;
using RichTextBox = System.Windows.Controls.RichTextBox;

namespace SkillTreeEditor;

public partial class MainWindow : Window
{
    private const double PanThreshold = 3.0;
    private const double KeyboardPanStep = 500.0;

    private readonly double[] _zoomSteps = [0.1, 0.2, 0.4, 0.8, 1.6, 3.2];
    private readonly Dictionary<int, ImageSource> _images = [];
    private readonly HashSet<Key> _pressedKeys = [];

    private readonly ProjectStore _store;
    private readonly ProjectService _service;
    private SphereBoardRenderer _renderer = null!;

    private int _currentZoomStepIndex = 1;
    private bool _isPanning;
    private bool _isPaintingSpheres;
    private bool _isRemovingSpheres;
    private bool _isUpdatingSphereBoardControls;
    private bool _isUpdatingSphereControls;
    private bool _isUpdatingEffectControls;

    private EditorMode _sphereEditionMode = EditorMode.Select;
    private bool _showCostOverlay;
    private Point _panStartMousePosition;
    private Point _panStartCanvasOffset;
    private SphereBoardData? _selectedSphereBoard;
    private SphereData? _selectedSphere;
    private EffectData? _selectedEffect;
    private SphereData? _copiedSphere;
    private TimeSpan? _lastRenderingTime;

    private const int WM_GETMINMAXINFO = 0x0024;

    public MainWindow()
    {
        InitializeComponent();
        _store = ((App)Application.Current).Store;
        _service = ((App)Application.Current).Service;
        SourceInitialized += OnSourceInitialized;
        SizeChanged += OnSizeChanged;
        Loaded += (_, _) => InitWidgets();
        Loaded += (_, _) => CompositionTarget.Rendering += OnRendering;
        Closed += (_, _) => CompositionTarget.Rendering -= OnRendering;
        FitCanvasToHost();
    }

    private void InitWidgets()
    {
        LoadImages();
        _renderer = new SphereBoardRenderer(SkillTreeCanvas, _images);
        RefreshSphereBoardSelector();
        LoadBreedSelector();
        RefreshSpellSelectors(0);
        LoadActionSelector();
        LoadAreaShapeSelector();
        LoadTriggerSelectors();
        LoadTargetSelector();
        RefreshFighterCardSelector();

        UpdateSphereControlsFromSelectedSphere();
        UpdateEffectControlsFromSelectedEffect();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        FitCanvasToHost();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        var source = HwndSource.FromHwnd(handle);
        source?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_GETMINMAXINFO)
        {
            WindowsApi.WmGetMinMaxInfo(hwnd, lParam);
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        _selectedSphereBoard = _service.CreateSphereBoard();
        _selectedSphere = null;
        _selectedEffect = null;
        RefreshSphereBoardSelector();
        UpdateSphereControlsFromSelectedSphere();
        UpdateEffectControlsFromSelectedEffect();
        _renderer.DrawBoard(_selectedSphereBoard, _store.Spheres);

        if (_showCostOverlay)
            UpdateCostOverlay();

        UpdateFighterStatsOverlay();
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        dialog.Description = "Select the folder you want to open";
        dialog.UseDescriptionForTitle = true;
        dialog.ShowNewFolderButton = false;

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;

        var selectedFolder = dialog.SelectedPath;
        _service.LoadProjectFolder(selectedFolder);

        RefreshFighterCardSelector();
        RefreshSphereBoardSelector();
        if (_store.SphereBoards.Count == 0)
            return;

        _selectedSphereBoard = _store.SphereBoards[0];
        _renderer.DrawBoard(_selectedSphereBoard, _store.Spheres);

        if (_showCostOverlay)
            UpdateCostOverlay();

        _service.InitializeFighters();
        UpdateFighterStatsOverlay();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        dialog.Description = "Save all skill trees";
        dialog.UseDescriptionForTitle = true;
        dialog.ShowNewFolderButton = true;

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            return;

        var selectedFile = dialog.SelectedPath;
        _service.SaveProjectFolder(selectedFile);
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SkillTreeCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        ClearInputFocus();

        if (_selectedSphereBoard is null)
            return;

        var position = e.GetPosition(SkillTreeCanvas);
        var clickedX = (int)(position.X / SphereBoardRenderer.TileSize);
        var clickedY = (int)((SkillTreeCanvas.Height - position.Y) / SphereBoardRenderer.TileSize) + 1;

        switch (_sphereEditionMode)
        {
            case EditorMode.Add:
                _isPaintingSpheres = true;
                AddSphere(clickedX, clickedY);
                SkillTreeCanvas.CaptureMouse();
                break;
            case EditorMode.Remove:
                _isRemovingSpheres = true;
                RemoveSphere(clickedX, clickedY);
                SkillTreeCanvas.CaptureMouse();
                break;
            case EditorMode.Select:
                SelectSphere(clickedX, clickedY);
                break;
        }

        e.Handled = true;
    }

    private void SkillTreeCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isPanning && e.RightButton == MouseButtonState.Pressed)
        {
            var currentPosition = e.GetPosition(this);
            var delta = currentPosition - _panStartMousePosition;

            if (Math.Abs(delta.X) >= PanThreshold || Math.Abs(delta.Y) >= PanThreshold)
                SetCanvasTranslation(_panStartCanvasOffset.X + delta.X, _panStartCanvasOffset.Y + delta.Y);

            e.Handled = true;
        }

        if (_selectedSphereBoard is null || e.LeftButton != MouseButtonState.Pressed)
            return;

        var position = e.GetPosition(SkillTreeCanvas);
        var clickedX = (int)(position.X / SphereBoardRenderer.TileSize);
        var clickedY = (int)((SkillTreeCanvas.Height - position.Y) / SphereBoardRenderer.TileSize) + 1;

        if (_isPaintingSpheres)
            AddSphere(clickedX, clickedY);
        else if (_isRemovingSpheres)
            RemoveSphere(clickedX, clickedY);

        e.Handled = true;
    }

    private void SkillTreeCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isPaintingSpheres && !_isRemovingSpheres)
            return;

        _isPaintingSpheres = false;
        _isRemovingSpheres = false;
        SkillTreeCanvas.ReleaseMouseCapture();
        e.Handled = true;
    }

    private void SkillTreeCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var mousePosition = e.GetPosition(SkillTreeCanvasHost);

        var oldZoom = SkillTreeCanvasScale.ScaleX;

        _currentZoomStepIndex = e.Delta > 0 ? _currentZoomStepIndex + 1 : _currentZoomStepIndex - 1;
        _currentZoomStepIndex = Math.Clamp(_currentZoomStepIndex, 0, _zoomSteps.Length - 1);
        var newZoom = _zoomSteps[_currentZoomStepIndex];

        if (Math.Abs(newZoom - oldZoom) < double.Epsilon)
            return;

        var contentX = (mousePosition.X - SkillTreeCanvasTranslate.X) / oldZoom;
        var contentY = (mousePosition.Y - SkillTreeCanvasTranslate.Y) / oldZoom;

        SkillTreeCanvasScale.ScaleX = newZoom;
        SkillTreeCanvasScale.ScaleY = newZoom;

        SetCanvasTranslation(mousePosition.X - contentX * newZoom, mousePosition.Y - contentY * newZoom);

        e.Handled = true;
    }

    private void SkillTreeCanvas_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        ClearInputFocus();

        _isPanning = true;
        _panStartMousePosition = e.GetPosition(this);
        _panStartCanvasOffset = new Point(SkillTreeCanvasTranslate.X, SkillTreeCanvasTranslate.Y);

        SkillTreeCanvas.CaptureMouse();
        e.Handled = true;
    }

    private void SkillTreeCanvas_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isPanning)
            return;

        _isPanning = false;
        SkillTreeCanvas.ReleaseMouseCapture();
        e.Handled = true;
    }

    private void SphereBoardSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SphereBoardSelector.SelectedItem is not int selectedSphereBoardId)
        {
            SetSelectedSphereBoard(null);
            return;
        }

        SetSelectedSphereBoard(_store.SphereBoards.FirstOrDefault(board => board.Id == selectedSphereBoardId));

        if (_selectedSphereBoard is not null)
        {
            _renderer.DrawBoard(_selectedSphereBoard, _store.Spheres);
            UpdateFighterStatsOverlay();

            if (_showCostOverlay)
                UpdateCostOverlay();
        }
    }

    private void ClearCurrentBoardData_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSphereBoard is null)
            return;

        foreach (var sphere in _store.Spheres.Where(s => s.SphereBoardId == _selectedSphereBoard.Id))
        {
            sphere.Reset();
        }

        _renderer.DrawBoard(_selectedSphereBoard, _store.Spheres);

        if (_showCostOverlay)
            UpdateCostOverlay();

        if (_store.Fighters.TryGetValue(_selectedSphereBoard.Id, out var fighter))
        {
            fighter.ComputeStats();
        }
        UpdateFighterStatsOverlay();
    }

    private void CostOverlayCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        _showCostOverlay = CostOverlayCheckBox.IsChecked == true;
        UpdateCostOverlay();
    }

    private void UpdateCostOverlay()
    {
        if (_selectedSphereBoard is null)
            return;

        if (_showCostOverlay)
        {
            var costs = ComputeCostsForAllSpheres();
            var reachableCosts = costs.Values.Where(c => c != int.MaxValue).ToList();
            var maxCost = reachableCosts.Count > 0 ? reachableCosts.Max() : 0;
            _renderer.CostBrushes = costs.ToDictionary(kvp => kvp.Key, kvp => GetCostColor(kvp.Value, maxCost));
        }
        else
        {
            _renderer.CostBrushes = null;
        }

        _renderer.DrawBoard(_selectedSphereBoard, _store.Spheres);
    }

    private Dictionary<(int X, int Y), int> ComputeCostsForAllSpheres()
    {
        var costs = new Dictionary<(int X, int Y), int>();

        var sphereByPosition = _store.Spheres
            .Where(s => s.SphereBoardId == _selectedSphereBoard!.Id && !s.Impassable)
            .ToDictionary(s => (s.XPosition, s.YPosition));

        var start = (_selectedSphereBoard!.StartX, _selectedSphereBoard.StartY);

        foreach (var pos in sphereByPosition.Keys)
            costs[pos] = int.MaxValue;

        var queue = new Queue<(int X, int Y)>();
        var visited = new HashSet<(int X, int Y)>();
        var accumulatedCost = new Dictionary<(int X, int Y), int>();

        queue.Enqueue(start);
        visited.Add(start);
        accumulatedCost[start] = sphereByPosition.TryGetValue(start, out var startS) ? startS.XpNumber : 0;

        (int Dx, int Dy)[] directions = [(0, -1), (1, 0), (0, 1), (-1, 0)];

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var currentCost = accumulatedCost[current];

            if (costs.ContainsKey(current))
                costs[current] = Math.Min(costs[current], currentCost);

            foreach (var (dx, dy) in directions)
            {
                var next = (current.X + dx, current.Y + dy);
                if (!visited.Contains(next) && sphereByPosition.ContainsKey(next))
                {
                    visited.Add(next);
                    accumulatedCost[next] = currentCost + sphereByPosition[next].XpNumber;
                    queue.Enqueue(next);
                }
            }

            if (sphereByPosition.TryGetValue(current, out var currentSphere) &&
                (currentSphere.TeleportXPosition != 0 || currentSphere.TeleportYPosition != 0))
            {
                var tpDest = (currentSphere.TeleportXPosition, currentSphere.TeleportYPosition);
                if (!visited.Contains(tpDest) && sphereByPosition.ContainsKey(tpDest))
                {
                    visited.Add(tpDest);
                    accumulatedCost[tpDest] = currentCost + sphereByPosition[tpDest].XpNumber;
                    queue.Enqueue(tpDest);
                }
            }
        }

        return costs;
    }

    private static System.Windows.Media.Brush GetCostColor(int cost, int maxCost)
    {
        if (cost == int.MaxValue)
            return new SolidColorBrush(Color.FromRgb(100, 100, 100));

        if (maxCost <= 0)
            return new SolidColorBrush(Color.FromRgb(76, 175, 80));

        var t = Math.Min(1.0, Math.Sqrt((double)cost / maxCost));

        Color c1, c2;
        double ft;

        if (t < 0.5)
        {
            c1 = Color.FromRgb(76, 175, 80);
            c2 = Color.FromRgb(255, 235, 59);
            ft = t / 0.5;
        }
        else
        {
            c1 = Color.FromRgb(255, 235, 59);
            c2 = Color.FromRgb(244, 67, 54);
            ft = (t - 0.5) / 0.5;
        }

        var r = (byte)(c1.R + (c2.R - c1.R) * ft);
        var g = (byte)(c1.G + (c2.G - c1.G) * ft);
        var b = (byte)(c1.B + (c2.B - c1.B) * ft);

        return new SolidColorBrush(Color.FromRgb(r, g, b));
    }

    private void BreedSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_selectedSphereBoard is null || _isUpdatingSphereBoardControls)
            return;

        if (BreedSelector.SelectedValue is not int breedId)
            return;

        _selectedSphereBoard.BreedId = breedId;

        if (_store.Fighters.TryGetValue(_selectedSphereBoard.Id, out var fighter))
        {
            fighter.RefreshBreed();
            fighter.ComputeStats();
        }
        UpdateFighterStatsOverlay();

        RefreshSpellSelectors(breedId);

        var defaultSpellIds = _store.SpellCards
            .Where(spell => (int)spell.Category == breedId)
            .OrderBy(spell => spell.Name)
            .Take(3)
            .Select(spell => spell.Id)
            .ToList();

        _isUpdatingSphereBoardControls = true;
        try
        {
            _selectedSphereBoard.InitialSpellIds =
            [
                defaultSpellIds.ElementAtOrDefault(0),
                defaultSpellIds.ElementAtOrDefault(1),
                defaultSpellIds.ElementAtOrDefault(2)
            ];

            InitialSpell1Selector.SelectedValue = _selectedSphereBoard.InitialSpellIds.ElementAtOrDefault(0);
            InitialSpell2Selector.SelectedValue = _selectedSphereBoard.InitialSpellIds.ElementAtOrDefault(1);
            InitialSpell3Selector.SelectedValue = _selectedSphereBoard.InitialSpellIds.ElementAtOrDefault(2);
        }
        finally
        {
            _isUpdatingSphereBoardControls = false;
        }
    }

    private void InitialSpellSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_selectedSphereBoard is null || _isUpdatingSphereBoardControls)
            return;

        _selectedSphereBoard.InitialSpellIds =
        [
            ControlHandler.GetNullableIntFromComboBox(InitialSpell1Selector) ?? 0,
            ControlHandler.GetNullableIntFromComboBox(InitialSpell2Selector) ?? 0,
            ControlHandler.GetNullableIntFromComboBox(InitialSpell3Selector) ?? 0
        ];
    }

    private void StartCoordinateTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_selectedSphereBoard is null || _isUpdatingSphereBoardControls)
            return;

        var oldStartX = _selectedSphereBoard.StartX;
        var oldStartY = _selectedSphereBoard.StartY;

        if (int.TryParse(StartXTextBox.Text, out var startX))
            _selectedSphereBoard.StartX = startX;

        if (int.TryParse(StartYTextBox.Text, out var startY))
            _selectedSphereBoard.StartY = startY;

        var oldSphere = _store.Spheres.FirstOrDefault(s =>
            s.SphereBoardId == _selectedSphereBoard.Id &&
            s.XPosition == oldStartX &&
            s.YPosition == oldStartY);

        if (oldSphere is not null)
            _renderer.DrawSphere(oldSphere, false);
        else
            _renderer.RemoveTile(oldStartX, oldStartY);

        _renderer.DrawTile(_selectedSphereBoard.StartX, _selectedSphereBoard.StartY, Brushes.Lime);
    }

    private void SphereModeAdd_Click(object sender, RoutedEventArgs e)
    {
        _sphereEditionMode = EditorMode.Add;
    }

    private void SphereModeRemove_Click(object sender, RoutedEventArgs e)
    {
        _sphereEditionMode = EditorMode.Remove;
    }

    private void SphereModeSelect_Click(object sender, RoutedEventArgs e)
    {
        _sphereEditionMode = EditorMode.Select;
    }

    private void SphereSpellSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_selectedSphere is null || _isUpdatingSphereControls)
            return;

        if (SphereSpellSelector.SelectedValue is int spellId)
            _selectedSphere.SpellId = spellId;
    }

    private void SphereImpassableCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_selectedSphere is null || _isUpdatingSphereControls)
            return;

        _selectedSphere.Impassable = SphereImpassableCheckBox.IsChecked == true;
        _renderer.DrawSphere(_selectedSphere, IsStartPosition(_selectedSphere));
    }

    private void FighterCardAdd_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSphere is null || _isUpdatingSphereControls)
            return;

        if (SphereFighterCardSelector.SelectedValue is not int cardId)
            return;

        if (_selectedSphere.FighterCardsIds.Contains(cardId))
            return;

        _selectedSphere.FighterCardsIds.Add(cardId);
        UpdateSphereControlsFromSelectedSphere();
    }

    private void FighterCardRemove_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSphere is null || _isUpdatingSphereControls)
            return;

        if (SphereFighterCardsListBox.SelectedItem is not Helper.EnumItem item)
            return;

        _selectedSphere.FighterCardsIds.Remove(item.Id);
        UpdateSphereControlsFromSelectedSphere();
    }

    private void SphereValueTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_selectedSphere is null || _isUpdatingSphereControls)
            return;

        var oldX = _selectedSphere.XPosition;
        var oldY = _selectedSphere.YPosition;

        if (int.TryParse(SphereXpNumberTextBox.Text, out var xpNumber))
        {
            var updateOverlay = xpNumber != _selectedSphere.XpNumber;
            _selectedSphere.XpNumber = xpNumber;
            if (updateOverlay && _selectedSphereBoard is not null)
            {
                _store.Fighters[_selectedSphereBoard.Id].ComputeStats();
                UpdateFighterStatsOverlay();
            }
            UpdateTotalXpDisplay();
            if (_showCostOverlay)
                UpdateCostOverlay();
        }

        if (int.TryParse(SphereTeleportXTextBox.Text, out var teleportX))
            _selectedSphere.TeleportXPosition = teleportX;

        if (int.TryParse(SphereTeleportYTextBox.Text, out var teleportY))
            _selectedSphere.TeleportYPosition = teleportY;

        if (int.TryParse(SphereXPositionTextBox.Text, out var x))
            _selectedSphere.XPosition = x;

        if (int.TryParse(SphereYPositionTextBox.Text, out var y))
            _selectedSphere.YPosition = y;

        _renderer.DrawSphere(_selectedSphere, IsStartPosition(_selectedSphere));

        if (oldX != _selectedSphere.XPosition || oldY != _selectedSphere.YPosition)
            _renderer.RemoveTile(oldX, oldY);
    }

    private void EffectAdd_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSphereBoard is null || _selectedSphere is null)
            return;

        var effect = _service.CreateEffect(_selectedSphere);
        RefreshEffectSelector();
        SetSelectedEffect(effect);
        _store.Fighters[_selectedSphereBoard.Id].ComputeStats();
        UpdateFighterStatsOverlay();
    }

    private void EffectRemove_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSphereBoard is null || _selectedSphere is null || _selectedEffect is null)
            return;

        var index = _selectedSphere.Effects.IndexOf(_selectedEffect);
        if (index < 0)
            return;

        _selectedSphere.Effects.RemoveAt(index);
        RefreshEffectSelector();

        var nextIndex = Math.Min(index, _selectedSphere.Effects.Count - 1);
        SetSelectedEffect(nextIndex >= 0 ? _selectedSphere.Effects[nextIndex] : null);
        _store.Fighters[_selectedSphereBoard.Id].ComputeStats();
        UpdateFighterStatsOverlay();
        _renderer.DrawSphere(_selectedSphere, IsStartPosition(_selectedSphere));
    }

    private void EffectSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdatingEffectControls)
            return;

        SetSelectedEffect(EffectSelector.SelectedItem as EffectData);
    }

    private void ActionIdSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_selectedSphereBoard is null || _selectedEffect is null || _isUpdatingEffectControls || _selectedSphere is null)
            return;

        if (ActionIdSelector.SelectedValue is int actionId)
            _selectedEffect.ActionId = actionId;

        EffectSelector.Items.Refresh();
        _store.Fighters[_selectedSphereBoard.Id].ComputeStats();
        UpdateFighterStatsOverlay();
        _renderer.DrawSphere(_selectedSphere, IsStartPosition(_selectedSphere));
    }

    private void AreaShapeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_selectedEffect is null || _isUpdatingEffectControls)
            return;

        if (AreaShapeSelector.SelectedValue is int areaShape)
            _selectedEffect.AreaShape = areaShape;
    }

    private void TargetTriggerSelfCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_selectedEffect is null || _isUpdatingEffectControls)
            return;

        _selectedEffect.TargetTriggerSelf = TargetTriggerSelfCheckBox.IsChecked == true;
    }

    private void AreaSizeTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_selectedEffect is null || _isUpdatingEffectControls)
            return;

        _selectedEffect.AreaSize =
        [
            Helper.ParseIntOrDefault(AreaSize0TextBox.Text),
            Helper.ParseIntOrDefault(AreaSize1TextBox.Text)
        ];
    }

    private void DurationTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_selectedEffect is null || _isUpdatingEffectControls)
            return;

        _selectedEffect.Duration =
        [
            Helper.ParseIntOrDefault(DurationTextBox.Text),
            0
        ];
    }

    private void TriggeredWithDurationCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_selectedEffect is null || _isUpdatingEffectControls)
            return;

        _selectedEffect.TriggeredWithDuration = TriggeredWithDurationCheckBox.IsChecked == true;
    }

    private void EffectParamAdd_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSphereBoard is null || _selectedEffect is null || _isUpdatingEffectControls)
            return;

        if (!double.TryParse(EffectParamNewValueTextBox.Text, out var value))
            return;

        _selectedEffect.Params.Add(value);
        UpdateEffectControlsFromSelectedEffect();
        _store.Fighters[_selectedSphereBoard.Id].ComputeStats();
        UpdateFighterStatsOverlay();
    }

    private void EffectParamRemove_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSphereBoard is null || _isUpdatingEffectControls)
            return;

        RemoveEffectListItem(EffectParamsListBox, _selectedEffect?.Params);
        _store.Fighters[_selectedSphereBoard.Id].ComputeStats();
        UpdateFighterStatsOverlay();
    }

    private void EffectTriggerBeforeAdd_Click(object sender, RoutedEventArgs e)
    {
        AddEnumEffectListItem(EffectTriggerBeforeSelector, effect => effect.TriggersBefore);
    }

    private void EffectTriggerBeforeRemove_Click(object sender, RoutedEventArgs e)
    {
        RemoveEffectListItem(EffectTriggersBeforeListBox, _selectedEffect?.TriggersBefore);
    }

    private void EffectTriggerAfterAdd_Click(object sender, RoutedEventArgs e)
    {
        AddEnumEffectListItem(EffectTriggerAfterSelector, effect => effect.TriggersAfter);
    }

    private void EffectTriggerAfterRemove_Click(object sender, RoutedEventArgs e)
    {
        RemoveEffectListItem(EffectTriggersAfterListBox, _selectedEffect?.TriggersAfter);
    }

    private void EffectEndTriggerAdd_Click(object sender, RoutedEventArgs e)
    {
        AddEnumEffectListItem(EffectEndTriggerSelector, effect => effect.EndTriggers);
    }

    private void EffectEndTriggerRemove_Click(object sender, RoutedEventArgs e)
    {
        RemoveEffectListItem(EffectEndTriggersListBox, _selectedEffect?.EndTriggers);
    }

    private void EffectServerSideTriggerAdd_Click(object sender, RoutedEventArgs e)
    {
        AddEnumEffectListItem(EffectServerSideTriggerSelector, effect => effect.ServerSideTriggers);
    }

    private void EffectServerSideTriggerRemove_Click(object sender, RoutedEventArgs e)
    {
        RemoveEffectListItem(EffectServerSideTriggersListBox, _selectedEffect?.ServerSideTriggers);
    }

    private void EffectTargetAdd_Click(object sender, RoutedEventArgs e)
    {
        AddEnumEffectListItem(EffectTargetSelector, effect => effect.Targets);
    }

    private void EffectTargetRemove_Click(object sender, RoutedEventArgs e)
    {
        RemoveEffectListItem(EffectTargetsListBox, _selectedEffect?.Targets);
    }

    private void AddSphere(int x, int y)
    {
        if (_selectedSphereBoard is null)
            return;

        var sphere = _store.Spheres.FirstOrDefault(sphere => sphere.SphereBoardId == _selectedSphereBoard.Id &&
                                                             sphere.XPosition == x &&
                                                             sphere.YPosition == y)
                     ?? _service.CreateSphere(x, y, _selectedSphereBoard.Id);
        sphere.Reset();
        SetSelectedSphere(sphere);
        _renderer.DrawSphere(sphere, IsStartPosition(sphere));
        UpdateFighterStatsOverlay();
    }

    private void RemoveSphere(int x, int y)
    {
        if (_selectedSphereBoard is null)
            return;
        if (!_store.Spheres.Any(sphere => sphere.SphereBoardId == _selectedSphereBoard.Id && sphere.XPosition == x && sphere.YPosition == y))
            return;

        _service.RemoveSphere(x, y, _selectedSphereBoard.Id);
        _selectedSphere = null;
        _renderer.RemoveTile(x, y);
        UpdateFighterStatsOverlay();
    }

    private void SelectSphere(int x, int y)
    {
        if (_selectedSphereBoard is null)
            return;

        var previousSelectedSphere = _selectedSphere;

        var clickedSphere = _store.Spheres.FirstOrDefault(sphere =>
            sphere.SphereBoardId == _selectedSphereBoard.Id &&
            sphere.XPosition == x &&
            sphere.YPosition == y);

        if (clickedSphere is null)
            return;

        _renderer.HideSelection();
        _renderer.HideTeleportLine();

        if (previousSelectedSphere is not null)
            _renderer.DrawSphere(previousSelectedSphere, IsStartPosition(previousSelectedSphere));

        SetSelectedSphere(clickedSphere);
        _renderer.DrawSphere(clickedSphere, IsStartPosition(clickedSphere));
        _renderer.ShowSelection(x, y, Brushes.Red);

        if (clickedSphere is { TeleportXPosition: 0, TeleportYPosition: 0 })
            return;

        _renderer.ShowTeleportLine(clickedSphere);
    }

    private bool IsStartPosition(SphereData sphere)
    {
        return _selectedSphereBoard is not null &&
               sphere.XPosition == _selectedSphereBoard.StartX &&
               sphere.YPosition == _selectedSphereBoard.StartY;
    }

    private void CopySelectedSphere()
    {
        if (_selectedSphere is null)
            return;

        _copiedSphere = _selectedSphere.Copy();
    }

    private void PasteCopiedSphere()
    {
        if (_selectedSphere is null || _copiedSphere is null)
            return;

        var targetSphere = _selectedSphere;
        var oldX = targetSphere.XPosition;
        var oldY = targetSphere.YPosition;

        ApplySphereCopy(targetSphere, _copiedSphere);

        _renderer.DrawSphere(targetSphere, IsStartPosition(targetSphere));
        if (oldX != targetSphere.XPosition || oldY != targetSphere.YPosition)
            _renderer.RemoveTile(oldX, oldY);

        SetSelectedSphere(targetSphere);

        if (_selectedSphereBoard is null)
            return;

        _store.Fighters[_selectedSphereBoard.Id].ComputeStats();
        UpdateFighterStatsOverlay();
    }

    private void ApplySphereCopy(SphereData target, SphereData source)
    {
        target.XpNumber = source.XpNumber;
        target.SpellId = source.SpellId;
        target.FighterCardsIds = [.. source.FighterCardsIds];
        target.BarrierCoachCards = [.. source.BarrierCoachCards];
        target.TeleportXPosition = source.TeleportXPosition;
        target.TeleportYPosition = source.TeleportYPosition;
        target.Impassable = source.Impassable;

        target.Effects.Clear();
        foreach (var sourceEffect in source.Effects)
        {
            _service.CreateEffectCopy(target, sourceEffect);
        }
    }

    private void SetSelectedSphereBoard(SphereBoardData? sphereBoard)
    {
        _selectedSphereBoard = sphereBoard;
        UpdateSphereBoardControlsFromSelectedBoard();
    }

    private void SetSelectedSphere(SphereData? sphere)
    {
        _selectedSphere = sphere;
        UpdateSphereControlsFromSelectedSphere();
        RefreshEffectSelector();

        if (_selectedSphere?.Effects.Count > 0)
        {
            SetSelectedEffect(_selectedSphere.Effects[0]);
            EffectSelector.SelectedIndex = 0;
        }
        else
            SetSelectedEffect(null);
    }

    private void SetSelectedEffect(EffectData? effect)
    {
        _selectedEffect = effect;
        UpdateEffectControlsFromSelectedEffect();
    }

    private void UpdateSphereBoardControlsFromSelectedBoard()
    {
        if (_selectedSphereBoard is null)
            return;

        _isUpdatingSphereBoardControls = true;
        try
        {
            BreedSelector.SelectedValue = _selectedSphereBoard.BreedId;
            RefreshSpellSelectors(_selectedSphereBoard.BreedId);

            InitialSpell1Selector.SelectedValue = _selectedSphereBoard.InitialSpellIds.ElementAtOrDefault(0);
            InitialSpell2Selector.SelectedValue = _selectedSphereBoard.InitialSpellIds.ElementAtOrDefault(1);
            InitialSpell3Selector.SelectedValue = _selectedSphereBoard.InitialSpellIds.ElementAtOrDefault(2);

            StartXTextBox.Text = _selectedSphereBoard.StartX.ToString();
            StartYTextBox.Text = _selectedSphereBoard.StartY.ToString();
        }
        finally
        {
            _isUpdatingSphereBoardControls = false;
        }
    }

    private void UpdateTotalXpDisplay()
    {
        if (_selectedSphereBoard is null || _selectedSphere is null)
        {
            SphereTotalXpTextBlock.Text = string.Empty;
            return;
        }

        var sphereByPosition = _store.Spheres
            .Where(s => s.SphereBoardId == _selectedSphereBoard.Id && !s.Impassable)
            .ToDictionary(s => (s.XPosition, s.YPosition));

        var start = (_selectedSphereBoard.StartX, _selectedSphereBoard.StartY);
        var target = (_selectedSphere.XPosition, _selectedSphere.YPosition);

        if (!sphereByPosition.ContainsKey(target))
        {
            SphereTotalXpTextBlock.Text = string.Empty;
            return;
        }

        var queue = new Queue<(int X, int Y)>();
        var visited = new HashSet<(int X, int Y)>();
        var parent = new Dictionary<(int X, int Y), (int X, int Y)?>();

        queue.Enqueue(start);
        visited.Add(start);
        parent[start] = null;

        (int Dx, int Dy)[] directions = [(0, -1), (1, 0), (0, 1), (-1, 0)];

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current == target)
            {
                var totalXp = 0;
                var pathNode = current;
                while (pathNode != start)
                {
                    if (sphereByPosition.TryGetValue(pathNode, out var sphere))
                        totalXp += sphere.XpNumber;
                    if (parent[pathNode] is not null)
                        pathNode = parent[pathNode]!.Value;
                    else
                        break;
                }
                if (sphereByPosition.TryGetValue(start, out var startSphere))
                    totalXp += startSphere.XpNumber;
                SphereTotalXpTextBlock.Text = totalXp.ToString();
                return;
            }

            foreach (var (dx, dy) in directions)
            {
                var next = (current.X + dx, current.Y + dy);
                if (!visited.Contains(next) && sphereByPosition.ContainsKey(next))
                {
                    visited.Add(next);
                    parent[next] = current;
                    queue.Enqueue(next);
                }
            }

            if (sphereByPosition.TryGetValue(current, out var currentSphere) &&
                (currentSphere.TeleportXPosition != 0 || currentSphere.TeleportYPosition != 0))
            {
                var tpDest = (currentSphere.TeleportXPosition, currentSphere.TeleportYPosition);
                if (!visited.Contains(tpDest) && sphereByPosition.ContainsKey(tpDest))
                {
                    visited.Add(tpDest);
                    parent[tpDest] = current;
                    queue.Enqueue(tpDest);
                }
            }
        }

        SphereTotalXpTextBlock.Text = "0";
    }

    private void UpdateSphereControlsFromSelectedSphere()
    {
        if (_selectedSphere is null)
        {
            _isUpdatingSphereControls = true;
            try
            {
                SphereXpNumberTextBox.Text = string.Empty;
                SphereTeleportXTextBox.Text = string.Empty;
                SphereTeleportYTextBox.Text = string.Empty;
                SphereXPositionTextBox.Text = string.Empty;
                SphereYPositionTextBox.Text = string.Empty;
                SphereFighterCardsListBox.ItemsSource = null;
                SphereFighterCardSelector.SelectedIndex = -1;
                SphereSpellSelector.SelectedIndex = -1;
                SphereImpassableCheckBox.IsChecked = false;
                EffectSelector.ItemsSource = null;
                SphereTotalXpTextBlock.Text = string.Empty;
            }
            finally
            {
                _isUpdatingSphereControls = false;
            }
            return;
        }

        _isUpdatingSphereControls = true;
        try
        {
            SphereXpNumberTextBox.Text = _selectedSphere.XpNumber.ToString();
            SphereTeleportXTextBox.Text = _selectedSphere.TeleportXPosition.ToString();
            SphereTeleportYTextBox.Text = _selectedSphere.TeleportYPosition.ToString();
            SphereXPositionTextBox.Text = _selectedSphere.XPosition.ToString();
            SphereYPositionTextBox.Text = _selectedSphere.YPosition.ToString();
            SphereFighterCardsListBox.ItemsSource = _selectedSphere.FighterCardsIds
                .Select(id => _store.FighterCards.FirstOrDefault(fc => fc.Id == id))
                .Where(fc => fc is not null)
                .Select(fc => new Helper.EnumItem(fc!.Id, fc.Name))
                .ToList();
            SphereSpellSelector.SelectedValue = _selectedSphere.SpellId;
            SphereImpassableCheckBox.IsChecked = _selectedSphere.Impassable;
            RefreshEffectSelector();
            UpdateTotalXpDisplay();
        }
        finally
        {
            _isUpdatingSphereControls = false;
        }
    }

    private void UpdateEffectControlsFromSelectedEffect()
    {
        _isUpdatingEffectControls = true;
        try
        {
            if (_selectedEffect is null)
            {
                ActionIdSelector.SelectedIndex = -1;
                AreaShapeSelector.SelectedIndex = -1;
                TargetTriggerSelfCheckBox.IsChecked = false;
                AreaSize0TextBox.Text = string.Empty;
                AreaSize1TextBox.Text = string.Empty;
                DurationTextBox.Text = string.Empty;
                TriggeredWithDurationCheckBox.IsChecked = false;
                EffectParamsListBox.ItemsSource = null;
                EffectTriggersBeforeListBox.ItemsSource = null;
                EffectTriggersAfterListBox.ItemsSource = null;
                EffectEndTriggersListBox.ItemsSource = null;
                EffectServerSideTriggersListBox.ItemsSource = null;
                EffectTargetsListBox.ItemsSource = null;
                return;
            }

            ActionIdSelector.SelectedValue = _selectedEffect.ActionId;
            AreaShapeSelector.SelectedValue = _selectedEffect.AreaShape;
            TargetTriggerSelfCheckBox.IsChecked = _selectedEffect.TargetTriggerSelf;
            AreaSize0TextBox.Text = _selectedEffect.AreaSize.ElementAtOrDefault(0).ToString();
            AreaSize1TextBox.Text = _selectedEffect.AreaSize.ElementAtOrDefault(1).ToString();
            DurationTextBox.Text = _selectedEffect.Duration.ElementAtOrDefault(0).ToString();
            TriggeredWithDurationCheckBox.IsChecked = _selectedEffect.TriggeredWithDuration;

            EffectParamsListBox.ItemsSource = _selectedEffect.Params.ToList();
            EffectTriggersBeforeListBox.ItemsSource = Helper.CreateEnumItems<TriggerType>(_selectedEffect.TriggersBefore);
            EffectTriggersAfterListBox.ItemsSource = Helper.CreateEnumItems<TriggerType>(_selectedEffect.TriggersAfter);
            EffectEndTriggersListBox.ItemsSource = Helper.CreateEnumItems<TriggerType>(_selectedEffect.EndTriggers);
            EffectServerSideTriggersListBox.ItemsSource = Helper.CreateEnumItems<TriggerType>(_selectedEffect.ServerSideTriggers);
            EffectTargetsListBox.ItemsSource = Helper.CreateEnumItems<TargetType>(_selectedEffect.Targets);
        }
        finally
        {
            _isUpdatingEffectControls = false;
        }
    }

    private void RemoveEffectListItem<T>(ListBox listBox, List<T>? items)
    {
        if (_selectedEffect is null || _isUpdatingEffectControls || items is null)
            return;

        if (listBox.SelectedIndex < 0 || listBox.SelectedIndex >= items.Count)
            return;

        items.RemoveAt(listBox.SelectedIndex);
        UpdateEffectControlsFromSelectedEffect();
    }

    private void AddEnumEffectListItem(ComboBox comboBox, Func<EffectData, List<int>> listSelector)
    {
        if (_selectedEffect is null || _isUpdatingEffectControls)
            return;

        if (comboBox.SelectedValue is not int value)
            return;

        listSelector(_selectedEffect).Add(value);
        UpdateEffectControlsFromSelectedEffect();
    }

    private void AddEnumEffectListItem(ComboBox comboBox, Func<EffectData, List<long>> listSelector)
    {
        if (_selectedEffect is null || _isUpdatingEffectControls)
            return;

        if (comboBox.SelectedValue is not int value)
            return;

        listSelector(_selectedEffect).Add(value);
        UpdateEffectControlsFromSelectedEffect();
    }

    private void ClearInputFocus()
    {
        Keyboard.ClearFocus();
        FocusManager.SetFocusedElement(this, null);
        Focus();
        Keyboard.Focus(this);
    }

    private void SetCanvasTranslation(double x, double y)
    {
        var hostWidth = SkillTreeCanvasHost.ActualWidth;
        var hostHeight = SkillTreeCanvasHost.ActualHeight;
        var contentWidth = SkillTreeCanvas.Width * SkillTreeCanvasScale.ScaleX;
        var contentHeight = SkillTreeCanvas.Height * SkillTreeCanvasScale.ScaleY;

        x = contentWidth <= hostWidth
            ? (hostWidth - contentWidth) / 2
            : Math.Clamp(x, hostWidth - contentWidth, 0);
        y = contentHeight <= hostHeight
            ? (hostHeight - contentHeight) / 2
            : Math.Clamp(y, hostHeight - contentHeight, 0);

        SkillTreeCanvasTranslate.X = x;
        SkillTreeCanvasTranslate.Y = y;
    }

    private void PanCanvas(double deltaX, double deltaY)
    {
        SetCanvasTranslation(SkillTreeCanvasTranslate.X + deltaX, SkillTreeCanvasTranslate.Y + deltaY);
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (IsTextInputFocused())
            return;

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.C)
        {
            CopySelectedSphere();
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.V)
        {
            PasteCopiedSphere();
            e.Handled = true;
            return;
        }

        if (e.Key is not (Key.W or Key.A or Key.S or Key.D))
            return;

        _pressedKeys.Add(e.Key);
        e.Handled = true;
    }

    private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (IsTextInputFocused())
            return;
        if (e.Key is not (Key.W or Key.A or Key.S or Key.D))
            return;

        _pressedKeys.Remove(e.Key);
        e.Handled = true;
    }

    private bool IsTextInputFocused()
    {
        return Keyboard.FocusedElement is TextBoxBase
               || Keyboard.FocusedElement is PasswordBox
               || Keyboard.FocusedElement is ComboBox
               || Keyboard.FocusedElement is RichTextBox;
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        if (_pressedKeys.Count == 0)
        {
            _lastRenderingTime = null;
            return;
        }

        if (e is not RenderingEventArgs renderingEventArgs)
            return;

        var currentRenderingTime = renderingEventArgs.RenderingTime;

        if (_lastRenderingTime is null)
        {
            _lastRenderingTime = currentRenderingTime;
            return;
        }

        var deltaTime = currentRenderingTime - _lastRenderingTime.Value;
        _lastRenderingTime = currentRenderingTime;

        var deltaSeconds = deltaTime.TotalSeconds;
        if (deltaSeconds <= 0)
            return;

        var deltaX = 0.0;
        var deltaY = 0.0;

        if (_pressedKeys.Contains(Key.A))
            deltaX += KeyboardPanStep * deltaSeconds;

        if (_pressedKeys.Contains(Key.D))
            deltaX -= KeyboardPanStep * deltaSeconds;

        if (_pressedKeys.Contains(Key.W))
            deltaY += KeyboardPanStep * deltaSeconds;

        if (_pressedKeys.Contains(Key.S))
            deltaY -= KeyboardPanStep * deltaSeconds;

        if (deltaX != 0 || deltaY != 0)
            PanCanvas(deltaX, deltaY);
    }

    private void FitCanvasToHost()
    {
        var hostWidth = SkillTreeCanvasHost.ActualWidth;
        var hostHeight = SkillTreeCanvasHost.ActualHeight;

        if (hostWidth <= 0 || hostHeight <= 0)
            return;

        var contentWidth = SkillTreeCanvas.Width;
        var contentHeight = SkillTreeCanvas.Height;

        if (contentWidth <= 0 || contentHeight <= 0)
            return;

        var scale = Math.Min(hostWidth / contentWidth, hostHeight / contentHeight);
        SkillTreeCanvasScale.ScaleX = scale;
        SkillTreeCanvasScale.ScaleY = scale;

        SetCanvasTranslation(0, 0);
    }

    private void LoadImages()
    {
        _images.Clear();

        foreach (var imageId in new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 30, 31, 32, 33, 34, 50 })
        {
            _images[imageId] = LoadSphereIconSource(imageId);
        }
    }

    private void RefreshSphereBoardSelector()
    {
        SphereBoardSelector.ItemsSource = _store.SphereBoards.Select(board => board.Id).ToList();

        if (_store.SphereBoards.Count > 0)
            SphereBoardSelector.SelectedIndex = 0;
    }

    private void LoadBreedSelector()
    {
        BreedSelector.ItemsSource = Enum.GetValues<Breeds>()
            .Select(breed => new Helper.EnumItem((int)breed, breed.ToString()))
            .ToList();
    }

    private void RefreshSpellSelectors(int breedId)
    {
        var items = _store.SpellCards
            .Where(spell => (int)spell.Category == breedId)
            .Select(spell => new Helper.EnumItem(spell.Id, spell.Name))
            .OrderBy(spell => spell.Name)
            .ToList();

        InitialSpell1Selector.ItemsSource = items;
        InitialSpell2Selector.ItemsSource = items;
        InitialSpell3Selector.ItemsSource = items;
        SphereSpellSelector.ItemsSource = items;
    }

    private void LoadActionSelector()
    {
        ActionIdSelector.ItemsSource = Enum.GetValues<ActionType>()
            .Select(actionType => new Helper.EnumItem((int)actionType, actionType.ToString()))
            .OrderBy(item => item.Name)
            .ToList();
    }

    private void LoadAreaShapeSelector()
    {
        AreaShapeSelector.ItemsSource = Enum.GetValues<AreaShape>()
            .Select(areaShape => new Helper.EnumItem((int)areaShape, areaShape.ToString()))
            .OrderBy(item => item.Name)
            .ToList();
    }

    private void LoadTriggerSelectors()
    {
        var items = Enum.GetValues<TriggerType>()
            .Select(triggerType => new Helper.EnumItem((int)triggerType, triggerType.ToString()))
            .OrderBy(item => item.Name)
            .ToList();

        EffectTriggerBeforeSelector.ItemsSource = items;
        EffectTriggerAfterSelector.ItemsSource = items;
        EffectEndTriggerSelector.ItemsSource = items;
        EffectServerSideTriggerSelector.ItemsSource = items;
    }

    private void LoadTargetSelector()
    {
        EffectTargetSelector.ItemsSource = Enum.GetValues<TargetType>()
            .Select(targetType => new Helper.EnumItem((int)targetType, targetType.ToString()))
            .OrderBy(item => item.Name)
            .ToList();
    }

    private void RefreshEffectSelector()
    {
        EffectSelector.ItemsSource = _selectedSphere?.Effects;
        EffectSelector.Items.Refresh();
    }

    private void RefreshFighterCardSelector()
    {
        SphereFighterCardSelector.ItemsSource = _store.FighterCards
            .Select(fc => new Helper.EnumItem(fc.Id, fc.Name))
            .OrderBy(item => item.Name)
            .ToList();
    }

    private void UpdateFighterStatsOverlay()
    {
        if (_selectedSphereBoard is not null)
            _service.ComputeLinkedSpheresForBoard(_selectedSphereBoard);

        if (_selectedSphereBoard is null || !_store.Fighters.TryGetValue(_selectedSphereBoard.Id, out var fighter))
        {
            FighterStatsText.Text = string.Empty;
            FighterStatsPanel.Visibility = Visibility.Collapsed;
            return;
        }

        FighterStatsText.Text = fighter.GetStatsText();
        FighterStatsPanel.Visibility = Visibility.Visible;
    }

    private static ImageSource LoadSphereIconSource(int imageId)
    {
        var uri = new Uri($"pack://application:,,,/Assets/Spheres/{imageId}.tga", UriKind.Absolute);
        var streamInfo = Application.GetResourceStream(uri);
        if (streamInfo?.Stream is null)
            throw new FileNotFoundException("Sphere icon resource was not found.", uri.ToString());

        using var image = SharpImage.Load<Rgba32>(streamInfo.Stream);

        var pixelData = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(pixelData);

        for (var i = 0; i < pixelData.Length; i += 4)
        {
            (pixelData[i], pixelData[i + 2]) = (pixelData[i + 2], pixelData[i]);
        }

        var bitmapSource = BitmapSource.Create(
            image.Width,
            image.Height,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            pixelData,
            image.Width * 4);

        bitmapSource.Freeze();
        return bitmapSource;
    }
}
