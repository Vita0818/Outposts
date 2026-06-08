using System;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Kikaria.Models;
using Kikaria.ViewModels;

namespace Kikaria.Views;

public sealed partial class ReviewPage : Page
{
    private MainViewModel VM => App.MainViewModel;

    public ReviewPage()
    {
        InitializeComponent();
        Loaded += ReviewPage_Loaded;
    }

    private void ReviewPage_Loaded(object sender, RoutedEventArgs e)
    {
        VM.PropertyChanged += VM_PropertyChanged;
        UpdateUI();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        VM.PropertyChanged += VM_PropertyChanged;
        if (VM.IsReviewQueueEmpty) return;
        if (VM.CurrentReviewPoint == null) VM.BuildReviewQueue();
        UpdateUI();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        VM.PropertyChanged -= VM_PropertyChanged;
    }

    private void VM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var name = e.PropertyName;
        if (name == nameof(MainViewModel.CurrentReviewPoint) ||
            name == nameof(MainViewModel.IsHintRevealed) ||
            name == nameof(MainViewModel.IsAnswerRevealed) ||
            name == nameof(MainViewModel.IsReviewQueueEmpty) ||
            name == nameof(MainViewModel.ToastMessage) ||
            name == nameof(MainViewModel.TodayReviewedAnswerCount))
        {
            DispatcherQueue.TryEnqueue(UpdateUI);
        }
    }

    private void UpdateUI()
    {
        ModeLabel.Text = VM.ReviewMode switch
        {
            ReviewMode.Reinforcement => "重点背诵",
            ReviewMode.Mastered => "已掌握复习",
            _ => "背诵"
        };

        TodayCountText.Text = $"今日 {VM.TodayReviewedAnswerCount + VM.TodayViewedHintCount}";

        bool empty = VM.IsReviewQueueEmpty;
        EmptyState.Visibility = VM.ShowReviewEmptyState ? Visibility.Visible : Visibility.Collapsed;
        ReinforcementCompleteState.Visibility = VM.ShowReinforcementComplete ? Visibility.Visible : Visibility.Collapsed;
        PointContent.Visibility = (!empty && VM.HasCurrentReviewPoint) ? Visibility.Visible : Visibility.Collapsed;
        ActionBar.Visibility = (!empty && VM.HasCurrentReviewPoint) ? Visibility.Visible : Visibility.Collapsed;

        if (VM.HasCurrentReviewPoint && VM.CurrentReviewPoint != null)
        {
            PointTitle.Text = VM.CurrentReviewPoint.Title;
            TagRepeater.ItemsSource = VM.CurrentReviewPoint.Tags;
            HintMathText.Text = VM.CurrentReviewPoint.Hint;
            AnswerMathText.Text = VM.CurrentReviewPoint.Content;
        }

        HintCard.Visibility = VM.IsHintRevealed ? Visibility.Visible : Visibility.Collapsed;
        AnswerCard.Visibility = VM.IsAnswerRevealed ? Visibility.Visible : Visibility.Collapsed;

        RevealActions.Visibility = VM.ShowRevealActions ? Visibility.Visible : Visibility.Collapsed;
        PostRevealActions.Visibility = VM.ShowPostRevealActions ? Visibility.Visible : Visibility.Collapsed;

        Action1Text.Text = VM.ModeActionLabel1;
        Action2Text.Text = VM.ModeActionLabel2;

        if (VM.ReviewMode == ReviewMode.Reinforcement)
        {
            Action1Icon.Glyph = "\uE711";
            Action1Shortcut.Text = "M";
            Action2Icon.Glyph = "\uE73E";
            Action2Shortcut.Text = "L";
        }
        else if (VM.ReviewMode == ReviewMode.Mastered)
        {
            Action1Icon.Glyph = "\uE73E";
            Action1Shortcut.Text = "M";
            Action2Icon.Glyph = "\uE711";
            Action2Shortcut.Text = "L";
        }
        else
        {
            Action1Icon.Glyph = "\uE73E";
            Action1Shortcut.Text = "M";
            Action2Icon.Glyph = "\uE73E";
            Action2Shortcut.Text = "L";
        }

        UpdateToast();
    }

    private void UpdateToast()
    {
        if (!string.IsNullOrEmpty(VM.ToastMessage))
        {
            ToastText.Text = VM.ToastMessage;
            ToastBorder.Visibility = Visibility.Visible;
        }
        else
        {
            ToastBorder.Visibility = Visibility.Collapsed;
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack) Frame.GoBack();
    }

    private void ShowHint_Click(object sender, RoutedEventArgs e) => VM.ShowHintAction();

    private void ShowAnswer_Click(object sender, RoutedEventArgs e) => VM.ShowAnswerAction();

    private void Action1_Click(object sender, RoutedEventArgs e)
    {
        if (VM.ReviewMode == ReviewMode.Reinforcement)
            VM.RemoveFromReinforcementAction();
        else
            VM.AddToReinforcementAction();
    }

    private void Action2_Click(object sender, RoutedEventArgs e)
    {
        if (VM.ReviewMode == ReviewMode.Mastered)
            VM.RemoveFromMasteredAction();
        else
            VM.MarkMasteredAction();
    }

    private void Next_Click(object sender, RoutedEventArgs e) => VM.NextReviewPoint();

    private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case Windows.System.VirtualKey.Space:
                if (VM.ShowRevealActions && !VM.IsHintRevealed)
                    VM.ShowHintAction();
                else if (VM.ShowRevealActions && VM.IsHintRevealed && !VM.IsAnswerRevealed)
                    VM.ShowAnswerAction();
                else if (VM.ShowPostRevealActions)
                    VM.NextReviewPoint();
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.Enter:
                if (VM.ShowRevealActions && !VM.IsAnswerRevealed)
                    VM.ShowAnswerAction();
                else if (VM.ShowPostRevealActions)
                    VM.NextReviewPoint();
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.K:
            case Windows.System.VirtualKey.M:
                if (VM.ShowPostRevealActions) Action2_Click(sender, e);
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.L:
                if (VM.ShowPostRevealActions) Action1_Click(sender, e);
                e.Handled = true;
                break;
            default:
                if (e.Key == (Windows.System.VirtualKey)186 || e.Key == (Windows.System.VirtualKey)222)
                {
                    if (VM.ShowPostRevealActions) Action1_Click(sender, e);
                    e.Handled = true;
                }
                break;
        }
    }

    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        bool isWide = e.NewSize.Width >= 700;
        ContentRoot.MaxWidth = isWide ? 1000 : 700;
        ActionContent.MaxWidth = isWide ? 1000 : 700;
    }

    private void Page_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
    {
        var translation = e.Cumulative.Translation;
        double horizontal = Math.Abs(translation.X);
        double vertical = Math.Abs(translation.Y);
        double threshold = 80;
        double dominance = 1.4;

        if (horizontal > threshold && horizontal > vertical * dominance)
        {
            if (translation.X > 0)
            {
                // Swipe right - open scope panel (future)
            }
            else
            {
                HandleSwipeLeft();
            }
        }
        else if (vertical > threshold && vertical > horizontal * dominance)
        {
            if (translation.Y < 0)
            {
                HandleSwipeUp();
            }
            else
            {
                HandleSwipeDown();
            }
        }
    }

    private void HandleSwipeLeft()
    {
        var vm = App.MainViewModel;
        if (vm == null) return;

        if (vm.ReviewMode == ReviewMode.Reinforcement)
        {
            vm.RemoveFromReinforcementAction();
            vm.NextReviewPoint();
        }
        else if (vm.ReviewMode == ReviewMode.Mastered)
        {
            vm.RemoveFromMasteredAction();
            vm.NextReviewPoint();
        }
        else
        {
            vm.AddToReinforcementAction();
        }
    }

    private void HandleSwipeUp()
    {
        var vm = App.MainViewModel;
        if (vm == null) return;

        if (vm.IsAnswerRevealed)
        {
            vm.NextReviewPoint();
        }
        else if (vm.IsHintRevealed)
        {
            vm.ShowAnswerAction();
        }
        else
        {
            vm.ShowHintAction();
        }
    }

    private void HandleSwipeDown()
    {
        var vm = App.MainViewModel;
        vm?.PreviousReviewPoint();
    }
}
