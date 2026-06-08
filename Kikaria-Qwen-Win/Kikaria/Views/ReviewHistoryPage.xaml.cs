using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI;
using Kikaria.Helpers;
using Kikaria.Models;

namespace Kikaria.Views
{
    public sealed partial class ReviewHistoryPage : Page
    {
        private Kikaria.Models.KikariaAppState? _appState;
        private PresetStudyState? _currentState;
        private DateTime _displayMonth;
        private DateTime? _selectedDate;
        private Dictionary<DateTime, int> _dailyActivityCounts = new();
        private List<StudyActivityRecord> _allRecords = new();

        private static readonly string[] DayHeaders = { "周一", "周二", "周三", "周四", "周五", "周六", "周日" };

        public ReviewHistoryPage()
        {
            InitializeComponent();
            _displayMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            Loaded += OnPageLoaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is Kikaria.Models.KikariaAppState state)
            {
                _appState = state;
            }
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            ApplyTheme();
            LoadData();
            BuildCalendar();
        }

        private void ApplyTheme()
        {
            bool isDark = ActualTheme == ElementTheme.Dark;
            RootGrid.Background = KikariaTheme.PageGradient(isDark);
            TitleText.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);
            CalendarCard.IsDarkMode = isDark;
            DaySummaryCard.IsDarkMode = isDark;
            EmptyDayCard.IsDarkMode = isDark;
        }

        private void LoadData()
        {
            if (_appState == null) return;
            _currentState = _appState.CurrentPresetState;

            if (_currentState != null)
            {
                _allRecords = _currentState.ActivityRecords;
                _dailyActivityCounts = new ActivitySummary(_allRecords).DailyActivityCounts();
            }
        }

        private void BuildCalendar()
        {
            bool isDark = ActualTheme == ElementTheme.Dark;

            DayHeadersGrid.Children.Clear();
            for (int i = 0; i < 7; i++)
            {
                var header = new TextBlock
                {
                    Text = DayHeaders[i],
                    FontSize = 12,
                    FontWeight = Microsoft.UI.Text.FontWeights.Medium,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = KikariaTheme.GetBrush(KikariaThemeColor.SoftText, isDark),
                    FontFamily = KikariaTypography.ChineseCaptionFont
                };
                Grid.SetColumn(header, i);
                DayHeadersGrid.Children.Add(header);
            }

            CalendarGrid.Children.Clear();
            CalendarGrid.RowDefinitions.Clear();

            DateTime firstDay = new DateTime(_displayMonth.Year, _displayMonth.Month, 1);
            int daysInMonth = DateTime.DaysInMonth(_displayMonth.Year, _displayMonth.Month);

            int startDayOfWeek = ((int)firstDay.DayOfWeek + 6) % 7;

            int totalCells = startDayOfWeek + daysInMonth;
            int rows = (int)Math.Ceiling(totalCells / 7.0);

            for (int r = 0; r < rows; r++)
            {
                CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            }

            for (int day = 1; day <= daysInMonth; day++)
            {
                int cellIndex = startDayOfWeek + day - 1;
                int col = cellIndex % 7;
                int row = cellIndex / 7;

                DateTime date = new DateTime(_displayMonth.Year, _displayMonth.Month, day);
                int activityCount = _dailyActivityCounts.TryGetValue(date, out int count) ? count : 0;

                var dayCell = CreateDayCell(day, date, activityCount, isDark);
                Grid.SetRow(dayCell, row);
                Grid.SetColumn(dayCell, col);
                CalendarGrid.Children.Add(dayCell);
            }

            MonthYearTitle.Text = $"{_displayMonth.Year}年{_displayMonth.Month}月";
        }

        private Border CreateDayCell(int day, DateTime date, int activityCount, bool isDark)
        {
            Color bgColor = GetDayCellColor(activityCount, isDark);
            bool isToday = date.Date == DateTime.Today;
            bool isSelected = _selectedDate.HasValue && date.Date == _selectedDate.Value.Date;

            Color strokeColor = Colors.Transparent;
            double strokeThickness = 0;

            if (isSelected)
            {
                strokeColor = KikariaTheme.GetColor(KikariaThemeColor.Sky, isDark);
                strokeThickness = 2.5;
            }
            else if (isToday)
            {
                strokeColor = KikariaTheme.GetColor(KikariaThemeColor.Sky, isDark);
                strokeThickness = 1.5;
            }

            var border = new Border
            {
                Background = new SolidColorBrush(bgColor),
                CornerRadius = new CornerRadius(8),
                BorderBrush = new SolidColorBrush(strokeColor),
                BorderThickness = new Thickness(strokeThickness),
                Height = 40,
                Tag = date
            };

            var textBlock = new TextBlock
            {
                Text = day.ToString(),
                FontSize = 13,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark),
                FontFamily = KikariaTypography.NumberFont
            };

            border.Child = textBlock;
            border.Tapped += DayCell_Tapped;
            border.PointerEntered += DayCell_PointerEntered;
            border.PointerExited += DayCell_PointerExited;

            return border;
        }

        private Color GetDayCellColor(int count, bool isDark)
        {
            if (count == 0)
                return isDark ? Color.FromArgb(30, 255, 255, 255) : Color.FromArgb(20, 0, 0, 0);
            if (count <= 2)
                return isDark ? Color.FromArgb(80, 72, 216, 236) : Color.FromArgb(60, 48, 196, 216);
            if (count <= 5)
                return isDark ? Color.FromArgb(120, 88, 176, 255) : Color.FromArgb(100, 56, 152, 236);
            return isDark ? Color.FromArgb(160, 72, 219, 109) : Color.FromArgb(140, 52, 199, 89);
        }

        private void DayCell_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is Border border && border.Tag is DateTime date)
            {
                _selectedDate = date;
                BuildCalendar();
                ShowDaySummary(date);
            }
        }

        private void DayCell_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border border)
            {
                border.Opacity = 0.8;
            }
        }

        private void DayCell_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border border)
            {
                border.Opacity = 1.0;
            }
        }

        private void ShowDaySummary(DateTime date)
        {
            bool isDark = ActualTheme == ElementTheme.Dark;
            var records = _allRecords.Where(r => r.Date.Date == date.Date).ToList();

            if (records.Count == 0)
            {
                DaySummaryCard.Visibility = Visibility.Collapsed;
                EmptyDayCard.Visibility = Visibility.Visible;
                return;
            }

            EmptyDayCard.Visibility = Visibility.Collapsed;
            DaySummaryCard.Visibility = Visibility.Visible;

            SummaryDateText.Text = date.ToString("yyyy年M月d日");
            SummaryDateText.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark);
            SummaryTotalText.Text = $"共 {records.Count} 条记录";
            SummaryTotalText.Foreground = KikariaTheme.GetBrush(KikariaThemeColor.SoftText, isDark);

            var summary = new ActivitySummary(records);

            BreakdownGrid.Children.Clear();

            AddBreakdownRow(0, "查看提示", summary.HintViews, isDark);
            AddBreakdownRow(1, "查看答案", summary.AnswerReviews, isDark);
            AddBreakdownRow(2, "已掌握", summary.MasteredCount, isDark);
            AddBreakdownRow(3, "强化", summary.AddedReinforcementCount, isDark);
        }

        private void AddBreakdownRow(int row, string label, int count, bool isDark)
        {
            var labelText = new TextBlock
            {
                Text = label,
                FontSize = 13,
                Foreground = KikariaTheme.GetBrush(KikariaThemeColor.SoftText, isDark),
                FontFamily = KikariaTypography.ChineseCaptionFont
            };
            Grid.SetRow(labelText, row);
            Grid.SetColumn(labelText, 0);
            BreakdownGrid.Children.Add(labelText);

            var countText = new TextBlock
            {
                Text = count.ToString(),
                FontSize = 14,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = KikariaTheme.GetBrush(KikariaThemeColor.DeepText, isDark),
                FontFamily = KikariaTypography.NumberFont
            };
            Grid.SetRow(countText, row);
            Grid.SetColumn(countText, 1);
            BreakdownGrid.Children.Add(countText);
        }

        private void PrevMonth_Click(object sender, RoutedEventArgs e)
        {
            _displayMonth = _displayMonth.AddMonths(-1);
            BuildCalendar();
            DaySummaryCard.Visibility = Visibility.Collapsed;
            EmptyDayCard.Visibility = Visibility.Collapsed;
            _selectedDate = null;
        }

        private void NextMonth_Click(object sender, RoutedEventArgs e)
        {
            _displayMonth = _displayMonth.AddMonths(1);
            BuildCalendar();
            DaySummaryCard.Visibility = Visibility.Collapsed;
            EmptyDayCard.Visibility = Visibility.Collapsed;
            _selectedDate = null;
        }
    }
}
