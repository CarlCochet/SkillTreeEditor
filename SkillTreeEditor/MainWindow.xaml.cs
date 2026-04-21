using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SkillTreeEditor.Enums;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using ComboBox = System.Windows.Controls.ComboBox;
using Image = System.Windows.Controls.Image;
using ListBox = System.Windows.Controls.ListBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Panel = System.Windows.Controls.Panel;
using Point = System.Windows.Point;
using SharpImage = SixLabors.ImageSharp.Image;
using SixLabors.ImageSharp.PixelFormats;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using RichTextBox = System.Windows.Controls.RichTextBox;

namespace SkillTreeEditor;

public partial class MainWindow : Window
{
    private const double PanThreshold = 3.0;
    private const double TileSize = 40.0;
    private const double KeyboardPanStep = 500.0;
    
    private readonly double[] _zoomSteps = [ 0.1, 0.2, 0.4, 0.8, 1.6, 3.2 ];
    private readonly Dictionary<int, ImageSource> _images = [];
    private readonly HashSet<Key> _pressedKeys = [];
    
    private readonly Dictionary<(int X, int Y), List<UIElement>> _tiles = [];
    private UIElement? _selectedSphereMarker;
    private UIElement? _selectedTeleportMarker;
    
    private int _currentZoomStepIndex = 1;
    private bool _isPanning;
    private bool _isPaintingSpheres;
    private bool _isRemovingSpheres;
    private bool _isUpdatingSphereBoardControls;
    private bool _isUpdatingSphereControls;
    private bool _isUpdatingEffectControls;
    
    private EditorMode _sphereEditionMode = EditorMode.Select;
    private Point _panStartMousePosition;
    private Point _panStartCanvasOffset;
    private SphereBoardData? _selectedSphereBoard;
    private SphereData? _selectedSphere;
    private EffectData? _selectedEffect;
    private SphereData? _copiedSphere;
    private TimeSpan? _lastRenderingTime;
    
    private static App App => (App)Application.Current;
    
    private const int WM_GETMINMAXINFO = 0x0024;
    
    public MainWindow()
    {
        InitializeComponent();
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
        RefreshSphereBoardSelector();
        LoadBreedSelector();
        RefreshSpellSelectors(0);
        LoadActionSelector();
        LoadAreaShapeSelector();
        LoadTriggerSelectors();
        LoadTargetSelector();
        
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
        _selectedSphereBoard = App.CreateSphereBoard();
        _selectedSphere = null;
        _selectedEffect = null;
        RefreshSphereBoardSelector();
        UpdateSphereControlsFromSelectedSphere();
        UpdateEffectControlsFromSelectedEffect();
        DrawSphereBoard();
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
        App.LoadProjectFolder(selectedFolder);
        
        RefreshSphereBoardSelector();
        if (App.SphereBoards.Count == 0)
            return;
        
        _selectedSphereBoard = App.SphereBoards[0];
        DrawSphereBoard();

        foreach (var sphereBoard in App.SphereBoards)
        {
            var fighter = new Fighter(sphereBoard, App);
            App.Fighters.Add(sphereBoard.Id, fighter);
        }

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
        App.SaveProjectFolder(selectedFile);
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
        var clickedX = (int)(position.X / TileSize);
        var clickedY = (int)((SkillTreeCanvas.Height - position.Y) / TileSize) + 1;

        switch(_sphereEditionMode)
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
        var clickedX = (int)(position.X / TileSize);
        var clickedY = (int)((SkillTreeCanvas.Height - position.Y) / TileSize) + 1;

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

        SetSelectedSphereBoard(App.SphereBoards.FirstOrDefault(board => board.Id == selectedSphereBoardId));

        if (_selectedSphereBoard is not null)
        {
            DrawSphereBoard();
            UpdateFighterStatsOverlay();
        }
    }

    private void BreedSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_selectedSphereBoard is null || _isUpdatingSphereBoardControls)
            return;
        
        if (BreedSelector.SelectedValue is not int breedId)
            return;
        
        _selectedSphereBoard.BreedId = breedId;
        RefreshSpellSelectors(breedId);

        var defaultSpellIds = App.SpellCards
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
        
        var oldSphere = App.Spheres.FirstOrDefault(s =>
            s.SphereBoardId == _selectedSphereBoard.Id &&
            s.XPosition == oldStartX &&
            s.YPosition == oldStartY);

        if (oldSphere is not null)
            DrawSphereAtCurrentPosition(oldSphere);
        else
            DrawTile(oldStartX, oldStartY);

        DrawTile(_selectedSphereBoard.StartX, _selectedSphereBoard.StartY, Brushes.Lime);
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
        DrawSphereAtCurrentPosition(_selectedSphere);
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
                App.Fighters[_selectedSphereBoard.Id].ComputeStats();
                UpdateFighterStatsOverlay();
            }
        }
        
        if (int.TryParse(SphereFighterCardListIdTextBox.Text, out var fighterCardListId))
            _selectedSphere.FighterCardListId = fighterCardListId;

        if (int.TryParse(SphereTeleportXTextBox.Text, out var teleportX))
            _selectedSphere.TeleportXPosition = teleportX;

        if (int.TryParse(SphereTeleportYTextBox.Text, out var teleportY))
            _selectedSphere.TeleportYPosition = teleportY;

        if (int.TryParse(SphereXPositionTextBox.Text, out var x))
            _selectedSphere.XPosition = x;

        if (int.TryParse(SphereYPositionTextBox.Text, out var y))
            _selectedSphere.YPosition = y;

        DrawSphereAtCurrentPosition(_selectedSphere);

        if (oldX != _selectedSphere.XPosition || oldY != _selectedSphere.YPosition)
            DrawTile(oldX, oldY);
    }
    
    private void EffectAdd_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSphereBoard is null || _selectedSphere is null)
            return;

        var effect = App.CreateEffect(_selectedSphere);
        RefreshEffectSelector();
        SetSelectedEffect(effect);
        App.Fighters[_selectedSphereBoard.Id].ComputeStats();
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
        App.Fighters[_selectedSphereBoard.Id].ComputeStats();
        UpdateFighterStatsOverlay();
        DrawSphereAtCurrentPosition(_selectedSphere);
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
        App.Fighters[_selectedSphereBoard.Id].ComputeStats();
        UpdateFighterStatsOverlay();
        DrawSphereAtCurrentPosition(_selectedSphere);
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
        App.Fighters[_selectedSphereBoard.Id].ComputeStats();
        UpdateFighterStatsOverlay();
    }

    private void EffectParamRemove_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedSphereBoard is null || _isUpdatingEffectControls)
            return;
        
        RemoveEffectListItem(EffectParamsListBox, _selectedEffect?.Params);
        App.Fighters[_selectedSphereBoard.Id].ComputeStats();
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
        
        var sphere = App.Spheres.FirstOrDefault(sphere => sphere.SphereBoardId == _selectedSphereBoard.Id && 
                                                          sphere.XPosition == x && 
                                                          sphere.YPosition == y) 
                     ?? App.CreateSphere(x, y, _selectedSphereBoard.Id);
        sphere.Reset();
        SetSelectedSphere(sphere);
        DrawSphereAtCurrentPosition(sphere);
    }

    private void RemoveSphere(int x, int y)
    {
        if (_selectedSphereBoard is null)
            return;
        if (!App.Spheres.Any(sphere => sphere.SphereBoardId == _selectedSphereBoard.Id && sphere.XPosition == x && sphere.YPosition == y))
            return;
        
        App.RemoveSphere(x, y, _selectedSphereBoard.Id);
        _selectedSphere = null;
        DrawTile(x, y);
    }

    private void SelectSphere(int x, int y)
    {
        if (_selectedSphereBoard is null)
            return;

        var previousSelectedSphere = _selectedSphere;
        var previousMarker = _selectedSphereMarker;
        var previousTeleportMarker = _selectedTeleportMarker;

        var clickedSphere = App.Spheres.FirstOrDefault(sphere =>
            sphere.SphereBoardId == _selectedSphereBoard.Id &&
            sphere.XPosition == x &&
            sphere.YPosition == y);

        if (clickedSphere is null)
            return;

        if (previousMarker is not null)
        {
            SkillTreeCanvas.Children.Remove(previousMarker);
            _selectedSphereMarker = null;
        }

        if (previousTeleportMarker is not null)
        {
            SkillTreeCanvas.Children.Remove(previousTeleportMarker);
            _selectedTeleportMarker = null;
        }

        if (previousSelectedSphere is not null)
            DrawSphereAtCurrentPosition(previousSelectedSphere);

        SetSelectedSphere(clickedSphere);
        DrawSphereAtCurrentPosition(clickedSphere);
        _selectedSphereMarker = DrawSelectCircle(x, y, Brushes.Red);
        SkillTreeCanvas.Children.Add(_selectedSphereMarker);

        if (clickedSphere is { TeleportXPosition: 0, TeleportYPosition: 0 })
            return;
        
        _selectedTeleportMarker = DrawTeleportLine(clickedSphere);
        SkillTreeCanvas.Children.Add(_selectedTeleportMarker);
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

        DrawSphereAtCurrentPosition(targetSphere);
        if (oldX != targetSphere.XPosition || oldY != targetSphere.YPosition)
            DrawTile(oldX, oldY);

        SetSelectedSphere(targetSphere);

        if (_selectedSphereBoard is null)
            return;
        
        App.Fighters[_selectedSphereBoard.Id].ComputeStats();
        UpdateFighterStatsOverlay();
    }

    private void ApplySphereCopy(SphereData target, SphereData source)
    {
        target.XpNumber = source.XpNumber;
        target.SpellId = source.SpellId;
        target.FighterCardListId = source.FighterCardListId;
        target.BarrierCoachCards = [.. source.BarrierCoachCards];
        target.TeleportXPosition = source.TeleportXPosition;
        target.TeleportYPosition = source.TeleportYPosition;
        target.Impassable = source.Impassable;
        
        target.Effects.Clear();
        foreach (var sourceEffect in source.Effects)
        {
            App.CreateEffectCopy(target, sourceEffect);
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
                SphereFighterCardListIdTextBox.Text = string.Empty;
                SphereSpellSelector.SelectedIndex = -1;
                SphereImpassableCheckBox.IsChecked = false;
                EffectSelector.ItemsSource = null;
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
            SphereFighterCardListIdTextBox.Text = _selectedSphere.FighterCardListId.ToString();
            SphereSpellSelector.SelectedValue = _selectedSphere.SpellId;
            SphereImpassableCheckBox.IsChecked = _selectedSphere.Impassable;
            RefreshEffectSelector();
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
        SphereBoardSelector.ItemsSource = App.SphereBoards.Select(board => board.Id).ToList();

        if (App.SphereBoards.Count > 0)
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
        var items = App.SpellCards
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
    
    private void DrawSphereBoard()
    {
        SkillTreeCanvas.Children.Clear();
        _tiles.Clear();

        if (_selectedSphereBoard is null)
            return;

        foreach (var sphere in App.Spheres)
        {
            if (sphere.SphereBoardId != _selectedSphereBoard.Id)
                continue;
            
            DrawSphereAtCurrentPosition(sphere);
        }
        
        DrawTile(_selectedSphereBoard.StartX, _selectedSphereBoard.StartY, Brushes.Lime);
    }
    
    private Line DrawTeleportLine(SphereData sphere)
    {
        var line = new Line
        {
            X1 = sphere.XPosition * TileSize + TileSize / 2,
            Y1 = SkillTreeCanvas.Height - (sphere.YPosition - 1) * TileSize - TileSize / 2,
            X2 = sphere.TeleportXPosition * TileSize + TileSize / 2,
            Y2 = SkillTreeCanvas.Height - (sphere.TeleportYPosition - 1) * TileSize - TileSize / 2,
            Stroke = Brushes.DeepSkyBlue,
            StrokeThickness = 5,
            SnapsToDevicePixels = true,
            IsHitTestVisible = false
        };

        Panel.SetZIndex(line, 1000);
        return line;
    }

    private Ellipse DrawSelectCircle(int x, int y, SolidColorBrush brush)
    {
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
        Canvas.SetTop(circle, SkillTreeCanvas.Height - y * TileSize - (diameter - TileSize) / 2);
        Panel.SetZIndex(circle, 900);
        return circle;
    }

    private void DrawSphereAtCurrentPosition(SphereData sphere)
    {
        var iconId = Helper.GetIconIdFromSphere(sphere);
        _images.TryGetValue(iconId, out var icon);
        DrawTile(sphere.XPosition, sphere.YPosition, Brushes.BurlyWood, icon);

        if (_selectedSphereBoard is not null &&
            sphere.XPosition == _selectedSphereBoard.StartX &&
            sphere.YPosition == _selectedSphereBoard.StartY)
        {
            DrawTile(_selectedSphereBoard.StartX, _selectedSphereBoard.StartY, Brushes.Lime);
        }
    }

    private void DrawTile(int x, int y, SolidColorBrush? brush = null, ImageSource? icon = null)
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
            Canvas.SetTop(tile, SkillTreeCanvas.Height - y * TileSize);
            elements.Add(tile);
            SkillTreeCanvas.Children.Add(tile);
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
            Canvas.SetTop(image, SkillTreeCanvas.Height - y * TileSize);
            elements.Add(image);
            SkillTreeCanvas.Children.Add(image);
        }

        if (elements.Count > 0)
            _tiles[(x, y)] = elements;
    }
    
    private void RemoveTile(int x, int y)
    {
        if (!_tiles.TryGetValue((x, y), out var elements))
            return;

        foreach (var element in elements)
            SkillTreeCanvas.Children.Remove(element);

        _tiles.Remove((x, y));
    }
    
    private void UpdateFighterStatsOverlay()
    {
        if (_selectedSphereBoard is null || !App.Fighters.TryGetValue(_selectedSphereBoard.Id, out var fighter))
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
