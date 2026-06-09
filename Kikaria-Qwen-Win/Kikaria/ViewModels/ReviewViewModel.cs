using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kikaria.Models;

namespace Kikaria.ViewModels
{
    public partial class ReviewViewModel : ObservableObject
    {
        private readonly MainViewModel _mainVM;

        [ObservableProperty]
        private Guid currentPointID;

        [ObservableProperty]
        private bool isShowingHint;

        [ObservableProperty]
        private bool isShowingContent;

        [ObservableProperty]
        private List<Guid> reviewQueue;

        [ObservableProperty]
        private int reviewQueueIndex;

        [ObservableProperty]
        private ReviewMode mode;

        [ObservableProperty]
        private bool isShowingScopePanel;

        [ObservableProperty]
        private string toastMessage;

        [ObservableProperty]
        private bool isTransitioning;

        [ObservableProperty]
        private double cardOpacity;

        public ReviewViewModel(MainViewModel mainVM, ReviewMode mode = ReviewMode.Normal)
        {
            _mainVM = mainVM;
            Mode = mode;
            CurrentPointID = Guid.Empty;
            IsShowingHint = false;
            IsShowingContent = false;
            ReviewQueue = new List<Guid>();
            ReviewQueueIndex = 0;
            IsShowingScopePanel = false;
            ToastMessage = string.Empty;
            IsTransitioning = false;
            CardOpacity = 1.0;

            RebuildQueue();
        }

        public KnowledgePoint? CurrentPoint
        {
            get
            {
                if (CurrentPointID == Guid.Empty)
                    return null;
                return _mainVM.KnowledgePoints.FirstOrDefault(p => p.Id == CurrentPointID);
            }
        }

        public List<Guid> MatchingPointIDs
        {
            get
            {
                var points = _mainVM.KnowledgePoints.AsEnumerable();

                if (_mainVM.SelectedTags.Count > 0)
                {
                    points = points.Where(p => p.Tags.Any(t => _mainVM.SelectedTags.Contains(t)));
                }

                return Mode switch
                {
                    ReviewMode.Reinforcement => points.Where(p => p.IsReinforced && !p.IsMastered).Select(p => p.Id).ToList(),
                    ReviewMode.Mastered => points.Where(p => p.IsMastered).Select(p => p.Id).ToList(),
                    _ => points.Where(p => !p.IsMastered).Select(p => p.Id).ToList()
                };
            }
        }

        public void RebuildQueue()
        {
            var ids = MatchingPointIDs;
            var random = new Random();
            ReviewQueue = ids.OrderBy(_ => random.Next()).ToList();
            ReviewQueueIndex = 0;

            if (ReviewQueue.Count > 0)
            {
                CurrentPointID = ReviewQueue[0];
                IsShowingHint = false;
                IsShowingContent = false;
            }
            else
            {
                CurrentPointID = Guid.Empty;
            }

            OnPropertyChanged(nameof(CurrentPoint));
            OnPropertyChanged(nameof(ReviewQueue));
            OnPropertyChanged(nameof(ReviewQueueIndex));
        }

        public void MoveToNext()
        {
            if (ReviewQueue.Count == 0) return;

            ReviewQueueIndex++;
            if (ReviewQueueIndex >= ReviewQueue.Count)
            {
                ReviewQueueIndex = 0;
            }

            CurrentPointID = ReviewQueue[ReviewQueueIndex];
            IsShowingHint = false;
            IsShowingContent = false;
            OnPropertyChanged(nameof(CurrentPoint));
            OnPropertyChanged(nameof(ReviewQueueIndex));
        }

        public void GoBack()
        {
            if (ReviewQueue.Count == 0) return;

            ReviewQueueIndex--;
            if (ReviewQueueIndex < 0)
            {
                ReviewQueueIndex = ReviewQueue.Count - 1;
            }

            CurrentPointID = ReviewQueue[ReviewQueueIndex];
            IsShowingHint = false;
            IsShowingContent = false;
            OnPropertyChanged(nameof(CurrentPoint));
            OnPropertyChanged(nameof(ReviewQueueIndex));
        }

        public void RevealHint()
        {
            if (!IsShowingHint)
            {
                IsShowingHint = true;

                var point = CurrentPoint;
                if (point != null)
                {
                    _mainVM.RecordStudyActivity(StudyActivityType.ViewedHint, point);
                }
            }
        }

        public void RevealContent()
        {
            if (!IsShowingContent)
            {
                IsShowingContent = true;

                var point = CurrentPoint;
                if (point != null)
                {
                    _mainVM.RecordStudyActivity(StudyActivityType.ReviewedAnswer, point);
                }
            }
        }

        public void AdvanceToNextPoint()
        {
            if (IsShowingContent)
            {
                TransitionToNextPoint();
            }
            else if (IsShowingHint)
            {
                RevealContent();
            }
            else
            {
                RevealHint();
            }
        }

        public async void TransitionToNextPoint()
        {
            if (IsTransitioning) return;
            await FadeAndSwap(true);
        }

        public async void TransitionToPreviousPoint()
        {
            if (IsTransitioning) return;
            await FadeAndSwap(false);
        }

        private async Task FadeAndSwap(bool forward)
        {
            IsTransitioning = true;
            CardOpacity = 0.0;
            OnPropertyChanged(nameof(CardOpacity));

            await Task.Delay(200);

            if (forward)
                MoveToNext();
            else
                GoBack();

            CardOpacity = 1.0;
            OnPropertyChanged(nameof(CardOpacity));

            await Task.Delay(200);
            IsTransitioning = false;
        }

        public void AddToReinforcement()
        {
            var point = CurrentPoint;
            if (point == null) return;

            point.AddReinforcement();
            _mainVM.RecordStudyActivity(StudyActivityType.AddedReinforcement, point);
            ShowToast($"Added \"{point.Title}\" to reinforcement");
        }

        public void MarkAsMastered()
        {
            var point = CurrentPoint;
            if (point == null) return;

            point.IsMastered = true;
            point.UpdatedAt = DateTime.Now;
            _mainVM.RecordStudyActivity(StudyActivityType.MarkedMastered, point);
            ShowToast($"Mastered \"{point.Title}\"!");

            if (Mode == ReviewMode.Normal || Mode == ReviewMode.Reinforcement)
            {
                ReviewQueue.Remove(point.Id);
                if (ReviewQueueIndex >= ReviewQueue.Count && ReviewQueue.Count > 0)
                    ReviewQueueIndex = 0;

                if (ReviewQueue.Count > 0)
                {
                    CurrentPointID = ReviewQueue[ReviewQueueIndex];
                    IsShowingHint = false;
                    IsShowingContent = false;
                    OnPropertyChanged(nameof(CurrentPoint));
                }
                else
                {
                    CurrentPointID = Guid.Empty;
                    OnPropertyChanged(nameof(CurrentPoint));
                }
            }
        }

        public void RemoveFromReinforcement()
        {
            var point = CurrentPoint;
            if (point == null) return;

            point.ClearReinforcement();
            _mainVM.RecordStudyActivity(StudyActivityType.RemovedReinforcement, point);
            ShowToast($"Removed \"{point.Title}\" from reinforcement");

            if (Mode == ReviewMode.Reinforcement)
            {
                ReviewQueue.Remove(point.Id);
                if (ReviewQueueIndex >= ReviewQueue.Count && ReviewQueue.Count > 0)
                    ReviewQueueIndex = 0;

                if (ReviewQueue.Count > 0)
                {
                    CurrentPointID = ReviewQueue[ReviewQueueIndex];
                    IsShowingHint = false;
                    IsShowingContent = false;
                    OnPropertyChanged(nameof(CurrentPoint));
                }
                else
                {
                    CurrentPointID = Guid.Empty;
                    OnPropertyChanged(nameof(CurrentPoint));
                }
            }
        }

        public void RemoveFromMastered()
        {
            var point = CurrentPoint;
            if (point == null) return;

            point.IsMastered = false;
            point.UpdatedAt = DateTime.Now;
            _mainVM.RecordStudyActivity(StudyActivityType.RemovedMastered, point);
            ShowToast($"Removed \"{point.Title}\" from mastered");

            if (Mode == ReviewMode.Mastered)
            {
                ReviewQueue.Remove(point.Id);
                if (ReviewQueueIndex >= ReviewQueue.Count && ReviewQueue.Count > 0)
                    ReviewQueueIndex = 0;

                if (ReviewQueue.Count > 0)
                {
                    CurrentPointID = ReviewQueue[ReviewQueueIndex];
                    IsShowingHint = false;
                    IsShowingContent = false;
                    OnPropertyChanged(nameof(CurrentPoint));
                }
                else
                {
                    CurrentPointID = Guid.Empty;
                    OnPropertyChanged(nameof(CurrentPoint));
                }
            }
        }

        public void HandleSwipeLeft()
        {
            switch (Mode)
            {
                case ReviewMode.Normal:
                    AddToReinforcement();
                    break;
                case ReviewMode.Reinforcement:
                    RemoveFromReinforcement();
                    break;
                case ReviewMode.Mastered:
                    RemoveFromMastered();
                    break;
            }
        }

        public void HandleSwipeRight()
        {
            IsShowingScopePanel = !IsShowingScopePanel;
        }

        public void HandleSwipeUp()
        {
            if (IsShowingContent)
            {
                TransitionToNextPoint();
            }
            else
            {
                AdvanceToNextPoint();
            }
        }

        public void HandleKeyboardShortcut(Windows.System.VirtualKey key)
        {
            switch (key)
            {
                case Windows.System.VirtualKey.Space:
                    AdvanceToNextPoint();
                    break;

                case Windows.System.VirtualKey.Enter:
                    if (IsShowingContent)
                        TransitionToNextPoint();
                    else
                        RevealContent();
                    break;

                case Windows.System.VirtualKey.K:
                case Windows.System.VirtualKey.M:
                    if (Mode == ReviewMode.Mastered)
                        RemoveFromMastered();
                    else
                        MarkAsMastered();
                    break;

                case Windows.System.VirtualKey.L:
                case (Windows.System.VirtualKey)186:
                case (Windows.System.VirtualKey)222:
                    if (Mode == ReviewMode.Reinforcement)
                        RemoveFromReinforcement();
                    else
                        AddToReinforcement();
                    break;
            }
        }

        private async void ShowToast(string message)
        {
            ToastMessage = message;
            OnPropertyChanged(nameof(ToastMessage));

            await Task.Delay(2500);

            ToastMessage = string.Empty;
            OnPropertyChanged(nameof(ToastMessage));
        }

        [RelayCommand]
        private void ToggleScopePanel()
        {
            IsShowingScopePanel = !IsShowingScopePanel;
        }

        [RelayCommand]
        private void Next()
        {
            AdvanceToNextPoint();
        }

        [RelayCommand]
        private void Previous()
        {
            TransitionToPreviousPoint();
        }

        [RelayCommand]
        private void Reinforce()
        {
            AddToReinforcement();
        }

        [RelayCommand]
        private void Master()
        {
            MarkAsMastered();
        }

        [RelayCommand]
        private void Remove()
        {
            switch (Mode)
            {
                case ReviewMode.Reinforcement:
                    RemoveFromReinforcement();
                    break;
                case ReviewMode.Mastered:
                    RemoveFromMastered();
                    break;
                default:
                    AddToReinforcement();
                    break;
            }
        }
    }
}
