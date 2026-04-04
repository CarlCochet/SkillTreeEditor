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
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

namespace SkillTreeEditor;

public partial class MainWindow : Window
{
    private sealed record BreedItem(int Id, string Name);
    private sealed record SpellItem(int Id, string Name);
    
    private const double PanThreshold = 3.0;
    
    private readonly double[] _zoomSteps = [ 0.1, 0.2, 0.4, 0.8, 1.6, 3.2 ];
    private int _currentZoomStepIndex = 1;
    private bool _isPanning;
    private bool _isUpdatingSphereBoardControls;
    private Point _panStartMousePosition;
    private Point _panStartCanvasOffset;
    private SphereBoardData? _selectedSphereBoard;
    
    private static App App => (App)Application.Current;
    
    private const int WM_GETMINMAXINFO = 0x0024;
    
    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += OnSourceInitialized;
        SizeChanged += OnSizeChanged;
        Loaded += (_, _) => InitWidgets();
        FitCanvasToHost();
    }

    private void InitWidgets()
    {
        RefreshSphereBoardSelector();
        RefreshBreedSelector();
        RefreshInitialSpellSelectors(0);
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

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
    
    private void New_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Create a new project/document
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
        
        DrawSphereBoard(App.SphereBoards[0].Id);
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
    }

    private void SkillTreeCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning || e.RightButton != MouseButtonState.Pressed)
            return;

        var currentPosition = e.GetPosition(this);
        var delta = currentPosition - _panStartMousePosition;

        if (Math.Abs(delta.X) < PanThreshold && Math.Abs(delta.Y) < PanThreshold)
            return;

        SetCanvasTranslation(_panStartCanvasOffset.X + delta.X, _panStartCanvasOffset.Y + delta.Y);

        e.Handled = true;
    }

    private void SkillTreeCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        
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
            DrawSphereBoard(_selectedSphereBoard.Id);
    }

    private void BreedSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_selectedSphereBoard is null || _isUpdatingSphereBoardControls)
            return;

        if (BreedSelector.SelectedValue is int breedId)
            _selectedSphereBoard.BreedId = breedId;
    }

    private void InitialSpellSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_selectedSphereBoard is null || _isUpdatingSphereBoardControls)
            return;

        _selectedSphereBoard.InitialSpellIds =
        [
            GetNullableIntFromComboBox(InitialSpell1Selector) ?? 0,
            GetNullableIntFromComboBox(InitialSpell2Selector) ?? 0,
            GetNullableIntFromComboBox(InitialSpell3Selector) ?? 0
        ];
    }

    private void StartCoordinateTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_selectedSphereBoard is null || _isUpdatingSphereBoardControls)
            return;

        if (int.TryParse(StartXTextBox.Text, out var startX))
            _selectedSphereBoard.StartX = startX;

        if (int.TryParse(StartYTextBox.Text, out var startY))
            _selectedSphereBoard.StartY = startY;
    }
    
    private void SetSelectedSphereBoard(SphereBoardData? sphereBoard)
    {
        _selectedSphereBoard = sphereBoard;
        UpdateSphereBoardControlsFromSelectedBoard();
    }

    private void UpdateSphereBoardControlsFromSelectedBoard()
    {
        if (_selectedSphereBoard is null)
            return;

        _isUpdatingSphereBoardControls = true;
        try
        {
            BreedSelector.SelectedValue = _selectedSphereBoard.BreedId;
            RefreshInitialSpellSelectors(_selectedSphereBoard.BreedId);

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

    private static int? GetNullableIntFromComboBox(ComboBox comboBox)
    {
        return comboBox.SelectedItem switch
        {
            int value => value,
            _ => null
        };
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

    private void RefreshSphereBoardSelector()
    {
        SphereBoardSelector.ItemsSource = App.SphereBoards.Select(board => board.Id).ToList();

        if (App.SphereBoards.Count > 0)
            SphereBoardSelector.SelectedIndex = 0;
    }
    
    private void RefreshBreedSelector()
    {
        BreedSelector.ItemsSource = Enum.GetValues<Breeds>()
            .Select(breed => new BreedItem((int)breed, breed.ToString()))
            .ToList();
    }

    private void RefreshInitialSpellSelectors(int breedId)
    {
        var items = App.SpellCards
            .Where(spell => (int)spell.Category == breedId)
            .Select(spell => new SpellItem(spell.Id, spell.Name))
            .OrderBy(spell => spell.Name)
            .ToList();
        
        InitialSpell1Selector.ItemsSource = items;
        InitialSpell2Selector.ItemsSource = items;
        InitialSpell3Selector.ItemsSource = items;
    }

    private void DrawSphereBoard(int sphereBoardId)
    {
        SkillTreeCanvas.Children.Clear();

        foreach (var sphere in App.Spheres)
        {
            if (sphere.SphereBoardId != sphereBoardId)
                continue;

            var background = sphere.Impassable
                ? Brushes.Red
                : sphere.Effects.Count > 0
                    ? Brushes.Blue
                    : Brushes.White;

            var tile = new Border
            {
                Width = 40,
                Height = 40,
                Background = background,
                BorderBrush = Brushes.Transparent
            };

            Canvas.SetLeft(tile, sphere.XPosition * 40);
            Canvas.SetTop(tile, SkillTreeCanvas.Height - sphere.YPosition * 40);
            SkillTreeCanvas.Children.Add(tile);
        }
    }
}
