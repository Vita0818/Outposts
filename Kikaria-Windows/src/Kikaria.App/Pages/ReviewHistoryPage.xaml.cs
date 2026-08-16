using Kikaria.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace Kikaria.App.Pages;

/// <summary>复习历史:月历热力 + 当日记录(对齐 Apple 版 ReviewHistoryView)。</summary>
public sealed partial class ReviewHistoryPage : Page
{
    private DateTime _visibleMonth = DateTime.Today;
    private DateTime _selectedDate = DateTime.Today;

    public ReviewHistoryPage()
    {
        InitializeComponent();
        BuildWeekdayHeader();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        _visibleMonth = DateTime.Today;
        _selectedDate = DateTime.Today;
        RefreshUi();
    }

    private List<StudyActivityRecord> Records => AppSession.Current.CurrentPresetActivityRecords;

    private void RefreshUi()
    {
        MonthTitleText.Text = Localization.MonthTitle(_visibleMonth);
        BuildCalendar();

        var dayRecords = StudyLogic.RecordsOnDate(Records, _selectedDate);
        var summary = ActivitySummary.Make(dayRecords);

        SelectedDateText.Text = Localization.MonthDayTitle(_selectedDate);
        RecordCountText.Text = Localization.RecordCount(summary.TotalCount);

        if (summary.TotalCount == 0)
        {
            EmptyDayText.Visibility = Visibility.Visible;
            SummaryRowsPanel.Visibility = Visibility.Collapsed;
            SummaryRowsPanel.Children.Clear();
            return;
        }

        EmptyDayText.Visibility = Visibility.Collapsed;
        SummaryRowsPanel.Children.Clear();
        AddSummaryRow("查看提示", summary.ViewedHintCount);
        AddSummaryRow("查看答案", summary.ReviewedAnswerCount);
        AddSummaryRow("新增掌握", summary.MarkedMasteredCount);
        AddSummaryRow("加入重点", summary.AddedReinforcementCount);
        SummaryRowsPanel.Visibility = Visibility.Visible;
    }

    private void AddSummaryRow(string title, int count)
    {
        var row = new Grid { Padding = new Thickness(0, 4, 0, 4) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var titleText = new TextBlock
        {
            Text = title,
            Style = (Style)Application.Current.Resources["BodyStyle"],
            Foreground = Theme.ThemedBrush(this, "DeepTextBrush"),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(titleText, 0);

        var countText = new TextBlock
        {
            Text = count.ToString(),
            Style = (Style)Application.Current.Resources["NumberStyle"],
            FontSize = 17,
            Foreground = Theme.ThemedBrush(this, "SkyBrush"),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(countText, 1);

        row.Children.Add(titleText);
        row.Children.Add(countText);
        SummaryRowsPanel.Children.Add(row);
    }

    private void BuildWeekdayHeader()
    {
        WeekdayHeaderGrid.ColumnDefinitions.Clear();
        WeekdayHeaderGrid.Children.Clear();
        for (var i = 0; i < 7; i++)
        {
            WeekdayHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var symbol = new TextBlock
            {
                Text = Localization.WeekdaySymbols[i],
                Style = (Style)Application.Current.Resources["TagTextStyle"],
                Foreground = Theme.ThemedBrush(this, "SoftTextBrush"),
                TextAlignment = TextAlignment.Center
            };
            Grid.SetColumn(symbol, i);
            WeekdayHeaderGrid.Children.Add(symbol);
        }
    }

    private void BuildCalendar()
    {
        CalendarGrid.ColumnDefinitions.Clear();
        CalendarGrid.RowDefinitions.Clear();
        CalendarGrid.Children.Clear();
        for (var i = 0; i < 7; i++)
        {
            CalendarGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        var monthStart = new DateTime(_visibleMonth.Year, _visibleMonth.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(monthStart.Year, monthStart.Month);
        // Monday-first 前置空位(等价 Apple (weekday+5)%7)。
        var leadingBlanks = ((int)monthStart.DayOfWeek + 6) % 7;

        var cells = new List<DateTime?>();
        for (var i = 0; i < leadingBlanks; i++)
        {
            cells.Add(null);
        }

        for (var day = 1; day <= daysInMonth; day++)
        {
            cells.Add(new DateTime(monthStart.Year, monthStart.Month, day));
        }

        while (cells.Count % 7 != 0)
        {
            cells.Add(null);
        }

        for (var index = 0; index < cells.Count; index += 7)
        {
            CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(38) });
            var rowIndex = CalendarGrid.RowDefinitions.Count - 1;
            for (var col = 0; col < 7; col++)
            {
                var date = cells[index + col];
                var cell = MakeDayCell(date);
                Grid.SetRow(cell, rowIndex);
                Grid.SetColumn(cell, col);
                CalendarGrid.Children.Add(cell);
            }
        }
    }

    private Button MakeDayCell(DateTime? date)
    {
        var count = date is null
            ? 0
            : StudyLogic.RecordsOnDate(Records, date.Value).Count;

        Brush fill;
        if (date is null || count == 0)
        {
            fill = date is null
                ? new SolidColorBrush(Windows.UI.Colors.Transparent)
                : Theme.ThemedBrush(this, "CalendarIdleBrush");
        }
        else if (count <= 2)
        {
            fill = CopyOpacity(this, "CyanBrush", 0.42);
        }
        else if (count <= 5)
        {
            fill = CopyOpacity(this, "SkyBrush", 0.54);
        }
        else
        {
            fill = CopyOpacity(this, "MasteredGreenBrush", 0.62);
        }

        var isSelected = date is not null && date.Value.Date == _selectedDate.Date;
        var isToday = date is not null && date.Value.Date == DateTime.Today;

        var button = new Button
        {
            Style = (Style)Application.Current.Resources["CircleButtonStyle"],
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = fill,
            BorderBrush = isSelected
                ? CopyOpacity(this, "DeepTextBrush", 0.45)
                : isToday
                    ? CopyOpacity(this, "SkyBrush", 0.65)
                    : new SolidColorBrush(Windows.UI.Colors.Transparent),
            BorderThickness = new Thickness(isSelected ? 2 : 1.4),
            IsEnabled = date is not null,
            Content = date is null
                ? null
                : new TextBlock
                {
                    Text = date.Value.Day.ToString(),
                    Style = (Style)Application.Current.Resources["TagTextStyle"],
                    FontSize = 12,
                    Foreground = CopyOpacity(this, "DeepTextBrush", count == 0 ? 0.58 : 0.86)
                }
        };

        if (date is not null)
        {
            var captured = date.Value;
            button.Click += (_, _) =>
            {
                _selectedDate = captured;
                RefreshUi();
            };
        }

        return button;
    }

    /// <summary>基于主题画刷构造带不透明度的副本(不改共享画刷实例)。</summary>
    private static SolidColorBrush CopyOpacity(FrameworkElement element, string key, double opacity)
    {
        if (Theme.ThemedBrush(element, key) is SolidColorBrush source)
        {
            var color = source.Color;
            return new SolidColorBrush(Windows.UI.Color.FromArgb(
                (byte)Math.Clamp(color.A * opacity + 0.5, 0, 255), color.R, color.G, color.B));
        }

        return new SolidColorBrush(Windows.UI.Colors.Transparent);
    }

    private void OnPreviousMonthClick(object sender, RoutedEventArgs e)
    {
        _visibleMonth = _visibleMonth.AddMonths(-1);
        RefreshUi();
    }

    private void OnNextMonthClick(object sender, RoutedEventArgs e)
    {
        _visibleMonth = _visibleMonth.AddMonths(1);
        RefreshUi();
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        MainWindow.GoBack();
    }
}
