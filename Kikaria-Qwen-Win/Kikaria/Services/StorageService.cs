using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Kikaria.Models;
using Windows.Storage;

namespace Kikaria.Services
{
    public class StorageService
    {
        private const string AppStateFileName = "app_state.json";
        private const string WidgetSnapshotFileName = "widget_snapshot.json";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        private StorageFolder LocalFolder => ApplicationData.Current.LocalFolder;

        public async Task SaveAppState(KikariaAppState state)
        {
            try
            {
                string json = state.Serialize();
                StorageFile file = await LocalFolder.CreateFileAsync(
                    AppStateFileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StorageService] Failed to save app state: {ex.Message}");
            }
        }

        public async Task<KikariaAppState?> LoadAppState()
        {
            try
            {
                StorageFile file = await LocalFolder.GetFileAsync(AppStateFileName);
                string json = await FileIO.ReadTextAsync(file);
                return KikariaAppState.Deserialize(json);
            }
            catch (FileNotFoundException)
            {
                System.Diagnostics.Debug.WriteLine("[StorageService] No saved app state found.");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StorageService] Failed to load app state: {ex.Message}");
                return null;
            }
        }

        public async Task SaveWidgetSnapshot(WidgetSnapshot snapshot)
        {
            try
            {
                string json = JsonSerializer.Serialize(snapshot, JsonOptions);
                StorageFile file = await LocalFolder.CreateFileAsync(
                    WidgetSnapshotFileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StorageService] Failed to save widget snapshot: {ex.Message}");
            }
        }

        public async Task<WidgetSnapshot?> LoadWidgetSnapshot()
        {
            try
            {
                StorageFile file = await LocalFolder.GetFileAsync(WidgetSnapshotFileName);
                string json = await FileIO.ReadTextAsync(file);
                return JsonSerializer.Deserialize<WidgetSnapshot>(json, JsonOptions);
            }
            catch (FileNotFoundException)
            {
                System.Diagnostics.Debug.WriteLine("[StorageService] No saved widget snapshot found.");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StorageService] Failed to load widget snapshot: {ex.Message}");
                return null;
            }
        }
    }
}
