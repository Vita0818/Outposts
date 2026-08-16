using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Kikaria.App.Controls;

/// <summary>
/// 数字 / 日期滚轮(ListBox 简化实现):
/// SetItems(items, selectedIndex) 填充;SelectionChanged 事件 + SelectedValue 读取。
/// </summary>
public sealed partial class WheelPicker : UserControl
{
    /// <summary>选中项变化时触发(参数:选中索引)。</summary>
    public event EventHandler<int>? SelectionCommitted;

    private bool _suppressEvents;

    public WheelPicker()
    {
        InitializeComponent();
    }

    /// <summary>当前选中索引;-1 表示未选择。</summary>
    public int SelectedIndex => ItemList.SelectedIndex;

    /// <summary>当前选中项文本(未选择为 null)。</summary>
    public string? SelectedItem => ItemList.SelectedItem as string;

    /// <summary>填充选项并定位到选中项(不触发事件)。</summary>
    public void SetItems(IReadOnlyList<string> items, int selectedIndex)
    {
        _suppressEvents = true;
        ItemList.Items.Clear();
        foreach (var item in items)
        {
            ItemList.Items.Add(item);
        }

        if (selectedIndex >= 0 && selectedIndex < ItemList.Items.Count)
        {
            ItemList.SelectedIndex = selectedIndex;
        }
        else if (ItemList.Items.Count > 0)
        {
            ItemList.SelectedIndex = 0;
        }

        _suppressEvents = false;

        if (ItemList.SelectedItem is not null)
        {
            ItemList.ScrollIntoView(ItemList.SelectedItem);
        }
    }

    /// <summary>以指定索引为选中项构造数字序列。</summary>
    public static IReadOnlyList<string> Numbers(int from, int to, string? suffix = null)
    {
        var values = new List<string>();
        if (from <= to)
        {
            for (var value = from; value <= to; value++)
            {
                values.Add(suffix is null ? value.ToString() : value + suffix);
            }
        }
        else
        {
            for (var value = from; value >= to; value--)
            {
                values.Add(suffix is null ? value.ToString() : value + suffix);
            }
        }

        return values;
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents)
        {
            return;
        }

        if (ItemList.SelectedItem is not null)
        {
            ItemList.ScrollIntoView(ItemList.SelectedItem);
        }

        SelectionCommitted?.Invoke(this, ItemList.SelectedIndex);
    }
}
