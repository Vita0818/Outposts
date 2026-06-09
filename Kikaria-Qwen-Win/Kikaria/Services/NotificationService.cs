using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using Kikaria.Models;

namespace Kikaria.Services
{
    public struct StudyProgressWarning
    {
        public int MasteredCount { get; set; }
        public int ExpectedMasteredCount { get; set; }
        public double DangerPercent { get; set; }
        public int RemainingDays { get; set; }

        public readonly bool IsBehind => DangerPercent > 0;
        public readonly string DangerPercentDisplay => $"{DangerPercent:F0}%";
    }

    public class NotificationService
    {
        private const string StudyProgressTagPrefix = "study_progress_";
        private const string DebugTestTag = "debug_test_notification";

        private readonly ToastNotifier _notifier;

        public NotificationService()
        {
            _notifier = ToastNotificationManager.CreateToastNotifier();
        }

        public async Task<bool> RequestAuthorization()
        {
            try
            {
                var settings = _notifier.Setting;
                return settings == NotificationSetting.Enabled;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotificationService] Authorization check failed: {ex.Message}");
                return false;
            }
        }

        public void CancelStudyProgressWarning(string presetId)
        {
            try
            {
                string tag = StudyProgressTagPrefix + presetId;
                var scheduled = _notifier.GetScheduledToastNotifications();
                foreach (var notification in scheduled.Where(n => n.Tag == tag))
                {
                    _notifier.RemoveFromSchedule(notification);
                }

                var history = ToastNotificationManager.History;
                try
                {
                    history.Remove(tag);
                }
                catch
                {
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotificationService] Failed to cancel notification for preset {presetId}: {ex.Message}");
            }
        }

        public void CancelAllNotifications()
        {
            try
            {
                var scheduled = _notifier.GetScheduledToastNotifications();
                foreach (var notification in scheduled)
                {
                    _notifier.RemoveFromSchedule(notification);
                }

                ToastNotificationManager.History.Clear();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NotificationService] Failed to cancel all notifications: {ex.Message}");
            }
        }

        public void RescheduleAllStudyProgressWarnings(
            Dictionary<string, PresetStudyState> presetStates,
            Dictionary<string, string> presetNames)
        {
            foreach (var kvp in presetStates)
            {
                string presetId = kvp.Key;
                PresetStudyState state = kvp.Value;

                CancelStudyProgressWarning(presetId);

                string presetName = presetNames.TryGetValue(presetId, out var name) ? name : "Unknown Preset";
                RescheduleStudyProgressWarning(state, presetName);
            }
        }

        public void RescheduleStudyProgressWarning(PresetStudyState state, string presetName)
        {
            try
            {
                var warning = EvaluateStudyProgressWarning(state);
                if (!warning.IsBehind)
                    return;

                DateTime now = DateTime.Now;
                DateTime scheduleTime = now.Date.AddHours(20);
                if (scheduleTime <= now)
                    scheduleTime = scheduleTime.AddDays(1);

                string title = $"Study Progress Warning - {presetName}";
                string body = warning.RemainingDays > 0
                    ? $"You're {warning.DangerPercentDisplay} behind schedule with {warning.RemainingDays} days remaining. " +
                      $"Mastered {warning.MasteredCount}/{warning.ExpectedMasteredCount} expected cards."
                    : $"You're {warning.DangerPercentDisplay} behind schedule. " +
                      $"Mastered {warning.MasteredCount}/{warning.ExpectedMasteredCount} expected cards.";

                var toastContent = new ToastContent
                {
                    Visual = new ToastVisual
                    {
                        BindingGeneric = new ToastBindingGeneric
                        {
                            Children =
                            {
                                new AdaptiveText { Text = title, HintStyle = AdaptiveTextStyle.Title },
                                new AdaptiveText { Text = body, HintStyle = AdaptiveTextStyle.Body }
                            },
                            AppLogoOverride = new ToastGenericAppLogo
                            {
                                Source = "ms-appx:///Assets/StoreLogo.png",
                                HintCrop = ToastGenericAppLogoCrop.Circle
                            }
                        }
                    },
                    Actions = new ToastActionsCustom
                    {
                        Buttons =
                        {
                            new ToastButton("Study Now", $"kikaria://study/{state.PresetId}")
                            {
                                ActivationType = ToastActivationType.Protocol
                            },
                            new ToastButton("Dismiss", "dismiss")
                            {
                                ActivationType = ToastActivationType.Background
                            }
                        }
                    },
                    Launch = $"kikaria://study/{state.PresetId}",
                    Scenario = ToastScenario.Reminder
                };

                var doc = toastContent.GetXml();
                var toast = new ScheduledToastNotification(doc, scheduleTime)
                {
                    Tag = StudyProgressTagPrefix + state.PresetId,
                    Group = "study_progress",
                    ExpirationTime = scheduleTime.AddDays(1)
                };

                _notifier.AddToSchedule(toast);

                System.Diagnostics.Debug.WriteLine(
                    $"[NotificationService] Scheduled warning for '{presetName}' at {scheduleTime:yyyy-MM-dd HH:mm}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[NotificationService] Failed to reschedule warning for '{presetName}': {ex.Message}");
            }
        }

        public StudyProgressWarning EvaluateStudyProgressWarning(PresetStudyState state)
        {
            var warning = new StudyProgressWarning();

            if (state.CountdownEndDate == null || state.DailyGoal <= 0 || state.TotalCount <= 0)
                return warning;

            DateTime now = DateTime.Now;
            DateTime startDate = state.CountdownStartDate ?? DateTime.Today;
            DateTime endDate = state.CountdownEndDate.Value;

            if (now >= endDate)
                return warning;

            int totalDays = Math.Max(1, (int)(endDate - startDate).TotalDays);
            int elapsedDays = Math.Max(0, (int)(now - startDate).TotalDays);
            int remainingDays = Math.Max(0, (int)(endDate - now).TotalDays);

            double expectedProgress = (double)elapsedDays / totalDays;
            int expectedMasteredCount = (int)(expectedProgress * state.TotalCount);

            warning.MasteredCount = state.MasteredCount;
            warning.ExpectedMasteredCount = expectedMasteredCount;
            warning.RemainingDays = remainingDays;

            if (expectedMasteredCount > 0 && state.MasteredCount < expectedMasteredCount)
            {
                int deficit = expectedMasteredCount - state.MasteredCount;
                warning.DangerPercent = Math.Min(100.0, (double)deficit / expectedMasteredCount * 100.0);
            }
            else
            {
                warning.DangerPercent = 0;
            }

            return warning;
        }

        public void ScheduleDebugTestNotification(string presetName)
        {
            try
            {
                DateTime scheduleTime = DateTime.Now.AddSeconds(5);

                var toastContent = new ToastContent
                {
                    Visual = new ToastVisual
                    {
                        BindingGeneric = new ToastBindingGeneric
                        {
                            Children =
                            {
                                new AdaptiveText
                                {
                                    Text = $"[DEBUG] Test Notification",
                                    HintStyle = AdaptiveTextStyle.Title
                                },
                                new AdaptiveText
                                {
                                    Text = $"This is a debug test notification for preset: {presetName}. " +
                                           $"If you see this, notifications are working correctly.",
                                    HintStyle = AdaptiveTextStyle.Body
                                }
                            }
                        }
                    },
                    Launch = "kikaria://debug/test",
                    Scenario = ToastScenario.Default
                };

                var doc = toastContent.GetXml();
                var toast = new ScheduledToastNotification(doc, scheduleTime)
                {
                    Tag = DebugTestTag,
                    Group = "debug",
                    ExpirationTime = scheduleTime.AddMinutes(5)
                };

                _notifier.AddToSchedule(toast);

                System.Diagnostics.Debug.WriteLine(
                    $"[NotificationService] Scheduled debug test notification for '{presetName}' at {scheduleTime:HH:mm:ss}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[NotificationService] Failed to schedule debug test notification: {ex.Message}");
            }
        }
    }
}
