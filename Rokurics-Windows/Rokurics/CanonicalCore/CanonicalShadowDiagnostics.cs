using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalShadowNodeRole
    {
        iPhone,
        Mac
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalShadowMismatchCategory
    {
        legacyRecordingMissingInCanonical,
        canonicalObjectMissingInLegacy,
        studyItemOnlyWithoutReceiveRecord,
        receiveRecordOnlyWithoutStudyItem,
        legacyMetadataHashMismatchButCanonicalHashMatch,
        canonicalMetadataHashMismatch,
        canonicalMetadataHashConverged,
        canonicalCreatedAtIgnoredForMetadataHash,
        canonicalModifiedAtIgnoredProcessingState,
        canonicalMacUpdatedAtRejectedAsProcessingClock,
        canonicalBusinessModifiedAtUsed,
        canonicalAudioSameHashSameSize,
        canonicalAudioMissing,
        canonicalAudioUnknown,
        canonicalAudioConflict,
        peerUnknown,
        legacyWouldUploadMetadataButCanonicalNoOp,
        canonicalPlanUsed,
        canonicalPlanFallback,
        canonicalAudioBootstrapUpload,
        canonicalAudioPeerSameNoOp,
        canonicalAudioPeerUnknownDeferred,
        canonicalGeneratedArtifactPeerSameNoOp,
        canonicalGeneratedArtifactPeerUnknownDeferred,
        canonicalGeneratedArtifactConflict
    }

    public record CanonicalShadowLegacyObjectFact : IEquatable<CanonicalShadowLegacyObjectFact>
    {
        public string ObjectID { get; init; }
        public string? LegacyMetadataHashPrefix { get; init; }
        public string? AudioHashPrefix { get; init; }
        public long? AudioByteSize { get; init; }
        public string AudioAvailability { get; init; }
        public bool HasRecordingMetadata { get; init; }
        public bool HasReceiveRecord { get; init; }
        public bool HasStudyItem { get; init; }

        public CanonicalShadowLegacyObjectFact(
            string objectID,
            string? legacyMetadataHash = null,
            string? audioHash = null,
            long? audioByteSize = null,
            string audioAvailability = "unknown",
            bool hasRecordingMetadata = false,
            bool hasReceiveRecord = false,
            bool hasStudyItem = false)
        {
            ObjectID = objectID.Trim();
            LegacyMetadataHashPrefix = HashPrefix(legacyMetadataHash);
            AudioHashPrefix = HashPrefix(audioHash);
            AudioByteSize = audioByteSize;
            AudioAvailability = audioAvailability.Trim().NilIfEmpty() ?? "unknown";
            HasRecordingMetadata = hasRecordingMetadata;
            HasReceiveRecord = hasReceiveRecord;
            HasStudyItem = hasStudyItem;
        }

        public static List<CanonicalShadowLegacyObjectFact> Merged(
            List<CanonicalShadowLegacyObjectFact> facts)
        {
            return facts.GroupBy(f => f.ObjectID)
                .Select(g =>
                {
                    var values = g.ToList();
                    return new CanonicalShadowLegacyObjectFact(
                        g.Key,
                        legacyMetadataHash: values
                            .Select(v => v.LegacyMetadataHashPrefix)
                            .FirstOrDefault(h => h != null),
                        audioHash: values
                            .Select(v => v.AudioHashPrefix)
                            .FirstOrDefault(h => h != null),
                        audioByteSize: values
                            .Select(v => v.AudioByteSize)
                            .FirstOrDefault(b => b.HasValue),
                        audioAvailability: values
                            .FirstOrDefault(v => v.AudioAvailability != "unknown")?
                            .AudioAvailability ?? "unknown",
                        hasRecordingMetadata: values.Any(v => v.HasRecordingMetadata),
                        hasReceiveRecord: values.Any(v => v.HasReceiveRecord),
                        hasStudyItem: values.Any(v => v.HasStudyItem));
                })
                .OrderBy(f => f.ObjectID)
                .ToList();
        }

        private static string? HashPrefix(string? value)
        {
            var normalized = value?.Trim().ToLower();
            if (string.IsNullOrEmpty(normalized)) return null;
            return normalized.Length > 12 ? normalized[..12] : normalized;
        }
    }

    public record CanonicalShadowLegacySnapshot : IEquatable<CanonicalShadowLegacySnapshot>
    {
        public int RecordingCount { get; init; }
        public int StudyItemCount { get; init; }
        public int ArtifactCount { get; init; }
        public List<CanonicalShadowLegacyObjectFact> Objects { get; init; }

        public CanonicalShadowLegacySnapshot(
            int recordingCount,
            int studyItemCount,
            int artifactCount,
            List<CanonicalShadowLegacyObjectFact> objects)
        {
            RecordingCount = recordingCount;
            StudyItemCount = studyItemCount;
            ArtifactCount = artifactCount;
            Objects = CanonicalShadowLegacyObjectFact.Merged(objects);
        }
    }

    public record CanonicalShadowArtifactSummary : IEquatable<CanonicalShadowArtifactSummary>
    {
        public string ArtifactID { get; init; }
        public string ObjectID { get; init; }
        public string Kind { get; init; }
        public string Availability { get; init; }
        public string? HashPrefix { get; init; }
        public bool HasHash { get; init; }
        public bool HasByteSize { get; init; }
        public long? ByteSize { get; init; }
        public string? LogicalName { get; init; }
    }

    public record CanonicalShadowObjectSummary : IEquatable<CanonicalShadowObjectSummary>
    {
        public string Id => ObjectID;
        public string ObjectID { get; init; }
        public string? CanonicalMetadataHashPrefix { get; init; }
        public string? LegacyMetadataHashPrefix { get; init; }
        public DateTime? CreatedAt { get; init; }
        public DateTime? ModifiedAt { get; init; }
        public string AudioAvailability { get; init; }
        public string? AudioHashPrefix { get; init; }
        public bool AudioHashPresent { get; init; }
        public bool AudioByteSizePresent { get; init; }
        public long? AudioByteSize { get; init; }
        public bool HasRecordingMetadata { get; init; }
        public bool HasReceiveRecord { get; init; }
        public bool HasStudyItem { get; init; }
    }

    public record CanonicalShadowMismatch : IEquatable<CanonicalShadowMismatch>
    {
        public string Id => string.Join("|", new[]
        {
            Category.ToString(), ObjectID ?? "", ArtifactID ?? "", Detail ?? ""
        });
        public CanonicalShadowMismatchCategory Category { get; init; }
        public string? ObjectID { get; init; }
        public string? ArtifactID { get; init; }
        public string? Detail { get; init; }
        public string? LocalHashPrefix { get; init; }
        public string? PeerHashPrefix { get; init; }
        public long? LocalByteSize { get; init; }
        public long? PeerByteSize { get; init; }
    }

    public record CanonicalShadowLegacyComparison : IEquatable<CanonicalShadowLegacyComparison>
    {
        public int LegacyRecordingCount { get; init; }
        public int LegacyStudyItemCount { get; init; }
        public int LegacyArtifactCount { get; init; }
        public int CanonicalObjectCount { get; init; }
        public int CanonicalArtifactCount { get; init; }
        public List<string> MetadataHashConvergedObjectIDs { get; init; }
        public List<CanonicalShadowObjectSummary> ObjectSummaries { get; init; }
        public List<CanonicalShadowArtifactSummary> ArtifactSummaries { get; init; }
        public List<CanonicalShadowMismatch> Mismatches { get; init; }

        public bool Contains(CanonicalShadowMismatchCategory category, string? objectID = null)
        {
            return Mismatches.Any(m =>
                m.Category == category && (objectID == null || m.ObjectID == objectID));
        }
    }

    public record CanonicalShadowReport : IEquatable<CanonicalShadowReport>
    {
        public static readonly int CurrentSchemaVersion = 1;
        public int SchemaVersion { get; init; }
        public string RunID { get; init; }
        public string? SyncRunID { get; init; }
        public string? Trigger { get; init; }
        public string NodeID { get; init; }
        public CanonicalShadowNodeRole NodeRole { get; init; }
        public DateTime GeneratedAt { get; init; }
        public double DurationMs { get; init; }
        public string? ManifestHashPrefix { get; init; }
        public CanonicalShadowLegacyComparison Comparison { get; init; }

        public int LegacyRecordingCount => Comparison.LegacyRecordingCount;
        public int LegacyStudyItemCount => Comparison.LegacyStudyItemCount;
        public int LegacyArtifactCount => Comparison.LegacyArtifactCount;
        public int CanonicalObjectCount => Comparison.CanonicalObjectCount;
        public int CanonicalArtifactCount => Comparison.CanonicalArtifactCount;
    }

    public class CanonicalShadowReportBuilder
    {
        public CanonicalShadowReportBuilder() { }

        public CanonicalShadowReport Build(
            string? runID,
            string? syncRunID,
            string? trigger,
            string nodeID,
            CanonicalShadowNodeRole nodeRole,
            DateTime? generatedAt,
            double durationMs,
            CanonicalManifest manifest,
            CanonicalShadowLegacySnapshot legacy,
            CanonicalManifest? peerManifest = null,
            CanonicalShadowLegacySnapshot? peerLegacy = null)
        {
            var genAt = generatedAt ?? DateTime.UtcNow;
            var localObjectsByID = manifest.Objects.ToDictionary(o => o.ObjectID);
            var peerObjectsByID = (peerManifest?.Objects ?? new List<CanonicalRecordingObject>())
                .ToDictionary(o => o.ObjectID);
            var legacyByID = legacy.Objects.ToDictionary(o => o.ObjectID);
            var peerLegacyByID = (peerLegacy?.Objects ?? new List<CanonicalShadowLegacyObjectFact>())
                .ToDictionary(o => o.ObjectID);

            var objectIDs = localObjectsByID.Keys
                .Concat(legacyByID.Keys)
                .Concat(peerObjectsByID.Keys)
                .Concat(peerLegacyByID.Keys)
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            var convergedObjectIDs = new List<string>();
            var mismatches = new List<CanonicalShadowMismatch>();

            var objectSummaries = objectIDs.Select(objectID =>
            {
                var obj = localObjectsByID.GetValueOrDefault(objectID);
                var legacyFact = legacyByID.GetValueOrDefault(objectID);
                AppendStructuralMismatches(objectID, obj, legacyFact, mismatches);
                AppendMetadataMismatches(objectID, obj,
                    peerObjectsByID.GetValueOrDefault(objectID),
                    legacyFact, peerLegacyByID.GetValueOrDefault(objectID),
                    nodeRole, convergedObjectIDs, mismatches);
                AppendLocalSemanticEvents(objectID, obj, nodeRole, mismatches);
                AppendAudioMismatches(objectID, obj,
                    peerLegacyByID.GetValueOrDefault(objectID),
                    peerLegacy != null, mismatches);
                AppendGeneratedArtifactMismatches(objectID, obj,
                    peerObjectsByID.GetValueOrDefault(objectID),
                    peerManifest != null, mismatches);
                return MakeObjectSummary(objectID, obj, legacyFact);
            }).ToList();

            var artifactSummaries = manifest.Objects
                .SelectMany(o => o.Artifacts)
                .OrderBy(a => a.ArtifactID)
                .Select(MakeArtifactSummary)
                .ToList();

            var comparison = new CanonicalShadowLegacyComparison
            {
                LegacyRecordingCount = legacy.RecordingCount,
                LegacyStudyItemCount = legacy.StudyItemCount,
                LegacyArtifactCount = legacy.ArtifactCount,
                CanonicalObjectCount = manifest.Objects.Count,
                CanonicalArtifactCount = manifest.Objects.Sum(o => o.Artifacts.Count),
                MetadataHashConvergedObjectIDs = convergedObjectIDs.OrderBy(id => id).ToList(),
                ObjectSummaries = objectSummaries,
                ArtifactSummaries = artifactSummaries,
                Mismatches = UniqueMismatches(mismatches)
            };

            return new CanonicalShadowReport
            {
                SchemaVersion = 1,
                RunID = runID ?? syncRunID ?? Guid.NewGuid().ToString(),
                SyncRunID = syncRunID,
                Trigger = trigger?.Trim().NilIfEmpty(),
                NodeID = nodeID,
                NodeRole = nodeRole,
                GeneratedAt = genAt,
                DurationMs = Math.Max(0, durationMs),
                ManifestHashPrefix = HashPrefix(manifest.ManifestHash.Value),
                Comparison = comparison
            };
        }

        private static void AppendStructuralMismatches(
            string objectID,
            CanonicalRecordingObject? obj,
            CanonicalShadowLegacyObjectFact? legacyFact,
            List<CanonicalShadowMismatch> mismatches)
        {
            var hasCanonicalObject = obj != null;
            var hasLegacyFact = legacyFact != null;

            if (!hasCanonicalObject
                && (legacyFact?.HasRecordingMetadata == true
                    || legacyFact?.HasReceiveRecord == true))
                mismatches.Add(new CanonicalShadowMismatch
                {
                    Category = CanonicalShadowMismatchCategory.legacyRecordingMissingInCanonical,
                    ObjectID = objectID
                });
            if (hasCanonicalObject && !hasLegacyFact)
                mismatches.Add(new CanonicalShadowMismatch
                {
                    Category = CanonicalShadowMismatchCategory.canonicalObjectMissingInLegacy,
                    ObjectID = objectID
                });
            if (legacyFact?.HasStudyItem == true
                && legacyFact.HasReceiveRecord != true
                && legacyFact.HasRecordingMetadata != true)
                mismatches.Add(new CanonicalShadowMismatch
                {
                    Category = CanonicalShadowMismatchCategory.studyItemOnlyWithoutReceiveRecord,
                    ObjectID = objectID
                });
            if (legacyFact?.HasReceiveRecord == true
                && legacyFact.HasStudyItem != true)
                mismatches.Add(new CanonicalShadowMismatch
                {
                    Category = CanonicalShadowMismatchCategory.receiveRecordOnlyWithoutStudyItem,
                    ObjectID = objectID
                });
        }

        private static void AppendMetadataMismatches(
            string objectID,
            CanonicalRecordingObject? localObject,
            CanonicalRecordingObject? peerObject,
            CanonicalShadowLegacyObjectFact? localLegacy,
            CanonicalShadowLegacyObjectFact? peerLegacy,
            CanonicalShadowNodeRole nodeRole,
            List<string> convergedObjectIDs,
            List<CanonicalShadowMismatch> mismatches)
        {
            if (localObject == null || peerObject == null) return;
            var localCanonicalHash = localObject.MetadataHash.Value;
            var peerCanonicalHash = peerObject.MetadataHash.Value;

            if (localCanonicalHash == peerCanonicalHash)
            {
                convergedObjectIDs.Add(objectID);
                mismatches.Add(new CanonicalShadowMismatch
                {
                    Category = CanonicalShadowMismatchCategory.canonicalMetadataHashConverged,
                    ObjectID = objectID,
                    Detail = "metadataHashEqual",
                    LocalHashPrefix = HashPrefix(localCanonicalHash),
                    PeerHashPrefix = HashPrefix(peerCanonicalHash)
                });
                if (localObject.Metadata.CreatedAt != peerObject.Metadata.CreatedAt)
                    mismatches.Add(new CanonicalShadowMismatch
                    {
                        Category = CanonicalShadowMismatchCategory.canonicalCreatedAtIgnoredForMetadataHash,
                        ObjectID = objectID,
                        Detail = "createdAtExcluded",
                        LocalHashPrefix = HashPrefix(localCanonicalHash),
                        PeerHashPrefix = HashPrefix(peerCanonicalHash)
                    });
                if (localObject.ProcessingState != peerObject.ProcessingState)
                    mismatches.Add(new CanonicalShadowMismatch
                    {
                        Category = CanonicalShadowMismatchCategory.canonicalModifiedAtIgnoredProcessingState,
                        ObjectID = objectID,
                        Detail = "processingStateExcluded",
                        LocalHashPrefix = HashPrefix(localCanonicalHash),
                        PeerHashPrefix = HashPrefix(peerCanonicalHash)
                    });
                if (localLegacy?.LegacyMetadataHashPrefix != null
                    && peerLegacy?.LegacyMetadataHashPrefix != null
                    && localLegacy.LegacyMetadataHashPrefix != peerLegacy.LegacyMetadataHashPrefix)
                    mismatches.Add(new CanonicalShadowMismatch
                    {
                        Category = CanonicalShadowMismatchCategory.legacyMetadataHashMismatchButCanonicalHashMatch,
                        ObjectID = objectID,
                        LocalHashPrefix = localLegacy.LegacyMetadataHashPrefix,
                        PeerHashPrefix = peerLegacy.LegacyMetadataHashPrefix
                    });
            }
            else
            {
                mismatches.Add(new CanonicalShadowMismatch
                {
                    Category = CanonicalShadowMismatchCategory.canonicalMetadataHashMismatch,
                    ObjectID = objectID,
                    LocalHashPrefix = HashPrefix(localCanonicalHash),
                    PeerHashPrefix = HashPrefix(peerCanonicalHash)
                });
                if (localObject.Metadata.ModifiedAt != peerObject.Metadata.ModifiedAt)
                {
                    var direction = localObject.Metadata.ModifiedAt > peerObject.Metadata.ModifiedAt
                        ? "localNewer" : "peerNewer";
                    mismatches.Add(new CanonicalShadowMismatch
                    {
                        Category = CanonicalShadowMismatchCategory.canonicalBusinessModifiedAtUsed,
                        ObjectID = objectID,
                        Detail = direction,
                        LocalHashPrefix = HashPrefix(localCanonicalHash),
                        PeerHashPrefix = HashPrefix(peerCanonicalHash)
                    });
                }
            }
        }

        private static void AppendLocalSemanticEvents(
            string objectID,
            CanonicalRecordingObject? obj,
            CanonicalShadowNodeRole nodeRole,
            List<CanonicalShadowMismatch> mismatches)
        {
            if (nodeRole != CanonicalShadowNodeRole.Mac) return;
            if (obj == null) return;
            if (obj.Metadata.ModifiedAt != obj.Metadata.CreatedAt) return;
            if (!HasProcessingSignal(obj.ProcessingState)) return;
            mismatches.Add(new CanonicalShadowMismatch
            {
                Category = CanonicalShadowMismatchCategory.canonicalMacUpdatedAtRejectedAsProcessingClock,
                ObjectID = objectID,
                Detail = "processingClockExcluded",
                LocalHashPrefix = HashPrefix(obj.MetadataHash.Value)
            });
        }

        private static bool HasProcessingSignal(CanonicalProcessingState processingState)
        {
            return (processingState.Transcription != CanonicalProcessingStatus.unknown
                    && processingState.Transcription != CanonicalProcessingStatus.notStarted)
                || (processingState.Note != CanonicalProcessingStatus.unknown
                    && processingState.Note != CanonicalProcessingStatus.notStarted);
        }

        private static void AppendAudioMismatches(
            string objectID,
            CanonicalRecordingObject? localObject,
            CanonicalShadowLegacyObjectFact? peerLegacy,
            bool peerLegacyWasProvided,
            List<CanonicalShadowMismatch> mismatches)
        {
            if (localObject == null) return;
            var localAudio = localObject.AudioArtifact;
            if (localAudio == null)
            {
                mismatches.Add(new CanonicalShadowMismatch
                {
                    Category = CanonicalShadowMismatchCategory.canonicalAudioMissing,
                    ObjectID = objectID
                });
                return;
            }
            if (!localAudio.ProvesCanonicalAudioAvailability
                || localAudio.ContentHash?.Value == null
                || localAudio.ByteSize == null)
            {
                mismatches.Add(new CanonicalShadowMismatch
                {
                    Category = CanonicalShadowMismatchCategory.canonicalAudioUnknown,
                    ObjectID = objectID,
                    ArtifactID = localAudio.ArtifactID,
                    LocalHashPrefix = HashPrefix(localAudio.ContentHash?.Value),
                    LocalByteSize = localAudio.ByteSize
                });
                return;
            }
            if (!peerLegacyWasProvided) return;
            if (peerLegacy == null)
            {
                mismatches.Add(new CanonicalShadowMismatch
                {
                    Category = CanonicalShadowMismatchCategory.peerUnknown,
                    ObjectID = objectID,
                    ArtifactID = localAudio.ArtifactID,
                    LocalHashPrefix = HashPrefix(localAudio.ContentHash.Value),
                    LocalByteSize = localAudio.ByteSize
                });
                return;
            }
            if (peerLegacy.AudioHashPrefix == null || peerLegacy.AudioByteSize == null)
            {
                mismatches.Add(new CanonicalShadowMismatch
                {
                    Category = CanonicalShadowMismatchCategory.canonicalAudioUnknown,
                    ObjectID = objectID,
                    ArtifactID = localAudio.ArtifactID,
                    LocalHashPrefix = HashPrefix(localAudio.ContentHash.Value),
                    PeerHashPrefix = peerLegacy.AudioHashPrefix,
                    LocalByteSize = localAudio.ByteSize,
                    PeerByteSize = peerLegacy.AudioByteSize
                });
                return;
            }
            var localHashPrefix = HashPrefix(localAudio.ContentHash.Value);
            if (localHashPrefix == peerLegacy.AudioHashPrefix
                && localAudio.ByteSize == peerLegacy.AudioByteSize)
            {
                mismatches.Add(new CanonicalShadowMismatch
                {
                    Category = CanonicalShadowMismatchCategory.canonicalAudioSameHashSameSize,
                    ObjectID = objectID,
                    ArtifactID = localAudio.ArtifactID,
                    LocalHashPrefix = localHashPrefix,
                    PeerHashPrefix = peerLegacy.AudioHashPrefix,
                    LocalByteSize = localAudio.ByteSize,
                    PeerByteSize = peerLegacy.AudioByteSize
                });
            }
            else
            {
                mismatches.Add(new CanonicalShadowMismatch
                {
                    Category = CanonicalShadowMismatchCategory.canonicalAudioConflict,
                    ObjectID = objectID,
                    ArtifactID = localAudio.ArtifactID,
                    LocalHashPrefix = localHashPrefix,
                    PeerHashPrefix = peerLegacy.AudioHashPrefix,
                    LocalByteSize = localAudio.ByteSize,
                    PeerByteSize = peerLegacy.AudioByteSize
                });
            }
        }

        private static void AppendGeneratedArtifactMismatches(
            string objectID,
            CanonicalRecordingObject? localObject,
            CanonicalRecordingObject? peerObject,
            bool peerManifestWasProvided,
            List<CanonicalShadowMismatch> mismatches)
        {
            if (!peerManifestWasProvided) return;
            var localArtifacts = GeneratedArtifactsByKind(localObject);
            var peerArtifacts = GeneratedArtifactsByKind(peerObject);
            var kinds = localArtifacts.Keys
                .Union(peerArtifacts.Keys)
                .OrderBy(k => k.ToString())
                .ToList();

            foreach (var kind in kinds)
            {
                var localArtifact = localArtifacts.GetValueOrDefault(kind);
                var peerArtifact = peerArtifacts.GetValueOrDefault(kind);
                var artifactID = peerArtifact?.ArtifactID ?? localArtifact?.ArtifactID;
                var localProven = CanonicalProjectionContract.ProvesGeneratedArtifactAvailability(
                    localArtifact);
                var peerProven = CanonicalProjectionContract.ProvesGeneratedArtifactAvailability(
                    peerArtifact);

                if (!localProven || !peerProven || localArtifact == null || peerArtifact == null)
                {
                    mismatches.Add(new CanonicalShadowMismatch
                    {
                        Category = CanonicalShadowMismatchCategory.canonicalGeneratedArtifactPeerUnknownDeferred,
                        ObjectID = objectID,
                        ArtifactID = artifactID,
                        Detail = $"kind={kind}",
                        LocalHashPrefix = HashPrefix(localArtifact?.ContentHash?.Value),
                        PeerHashPrefix = HashPrefix(peerArtifact?.ContentHash?.Value),
                        LocalByteSize = localArtifact?.ByteSize,
                        PeerByteSize = peerArtifact?.ByteSize
                    });
                    continue;
                }
                if (CanonicalProjectionContract.SameContent(localArtifact, peerArtifact))
                {
                    mismatches.Add(new CanonicalShadowMismatch
                    {
                        Category = CanonicalShadowMismatchCategory.canonicalGeneratedArtifactPeerSameNoOp,
                        ObjectID = objectID,
                        ArtifactID = artifactID,
                        Detail = $"kind={kind}",
                        LocalHashPrefix = HashPrefix(localArtifact.ContentHash?.Value),
                        PeerHashPrefix = HashPrefix(peerArtifact.ContentHash?.Value),
                        LocalByteSize = localArtifact.ByteSize,
                        PeerByteSize = peerArtifact.ByteSize
                    });
                }
                else
                {
                    mismatches.Add(new CanonicalShadowMismatch
                    {
                        Category = CanonicalShadowMismatchCategory.canonicalGeneratedArtifactConflict,
                        ObjectID = objectID,
                        ArtifactID = artifactID,
                        Detail = $"kind={kind}",
                        LocalHashPrefix = HashPrefix(localArtifact.ContentHash?.Value),
                        PeerHashPrefix = HashPrefix(peerArtifact.ContentHash?.Value),
                        LocalByteSize = localArtifact.ByteSize,
                        PeerByteSize = peerArtifact.ByteSize
                    });
                }
            }
        }

        private static Dictionary<CanonicalArtifactKind, CanonicalArtifact> GeneratedArtifactsByKind(
            CanonicalRecordingObject? obj)
        {
            if (obj == null) return new Dictionary<CanonicalArtifactKind, CanonicalArtifact>();
            return obj.Artifacts
                .Where(a => CanonicalProjectionContract.GeneratedArtifactKinds.Contains(a.Kind))
                .ToDictionary(a => a.Kind);
        }

        private static CanonicalShadowObjectSummary MakeObjectSummary(
            string objectID,
            CanonicalRecordingObject? obj,
            CanonicalShadowLegacyObjectFact? legacyFact)
        {
            var audio = obj?.AudioArtifact;
            return new CanonicalShadowObjectSummary
            {
                ObjectID = objectID,
                CanonicalMetadataHashPrefix = obj != null ? HashPrefix(obj.MetadataHash.Value) : null,
                LegacyMetadataHashPrefix = legacyFact?.LegacyMetadataHashPrefix,
                CreatedAt = obj?.Metadata.CreatedAt?.Date,
                ModifiedAt = obj?.Metadata.ModifiedAt?.Date,
                AudioAvailability = audio?.Availability.ToString()
                    ?? legacyFact?.AudioAvailability ?? "unknown",
                AudioHashPrefix = audio != null ? HashPrefix(audio.ContentHash?.Value)
                    : legacyFact?.AudioHashPrefix,
                AudioHashPresent = audio?.ContentHash != null
                    || legacyFact?.AudioHashPrefix != null,
                AudioByteSizePresent = audio?.ByteSize != null
                    || legacyFact?.AudioByteSize != null,
                AudioByteSize = audio?.ByteSize ?? legacyFact?.AudioByteSize,
                HasRecordingMetadata = legacyFact?.HasRecordingMetadata ?? false,
                HasReceiveRecord = legacyFact?.HasReceiveRecord ?? false,
                HasStudyItem = legacyFact?.HasStudyItem ?? false
            };
        }

        private static CanonicalShadowArtifactSummary MakeArtifactSummary(
            CanonicalArtifact artifact)
        {
            return new CanonicalShadowArtifactSummary
            {
                ArtifactID = artifact.ArtifactID,
                ObjectID = artifact.ObjectID,
                Kind = artifact.Kind.ToString(),
                Availability = artifact.Availability.ToString(),
                HashPrefix = HashPrefix(artifact.ContentHash?.Value),
                HasHash = artifact.ContentHash != null,
                HasByteSize = artifact.ByteSize != null,
                ByteSize = artifact.ByteSize,
                LogicalName = LogicalName(artifact.LogicalName)
            };
        }

        private static List<CanonicalShadowMismatch> UniqueMismatches(
            List<CanonicalShadowMismatch> mismatches)
        {
            var seen = new HashSet<string>();
            return mismatches
                .Where(m =>
                {
                    var key = string.Join("|", new[]
                    {
                        m.Category.ToString(), m.ObjectID ?? "",
                        m.ArtifactID ?? "", m.Detail ?? ""
                    });
                    return seen.Add(key);
                })
                .OrderBy(m => m.Category.ToString())
                .ThenBy(m => m.ObjectID ?? "")
                .ToList();
        }

        private static string? HashPrefix(string? value)
        {
            var normalized = value?.Trim().ToLower();
            if (string.IsNullOrEmpty(normalized)) return null;
            return normalized.Length > 12 ? normalized[..12] : normalized;
        }

        private static string? LogicalName(string? value)
        {
            if (value == null) return null;
            var trimmed = value.Trim();
            if (trimmed.Length == 0) return null;
            var parts = trimmed.Split('/');
            return parts.Length > 0 ? parts[^1] : trimmed;
        }
    }

    public class CanonicalShadowReportJSONLWriter
    {
        private readonly int _maxReports;

        public CanonicalShadowReportJSONLWriter(int maxReports = 200)
        {
            _maxReports = Math.Max(1, maxReports);
        }

        public void Append(CanonicalShadowReport report, string logPath)
        {
            try
            {
                var dir = Path.GetDirectoryName(logPath);
                if (dir != null) Directory.CreateDirectory(dir);

                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };
                var encoded = JsonSerializer.Serialize(report, options);
                var line = string.IsNullOrEmpty(encoded) ? "{}" : encoded;

                var existingLines = new List<string>();
                if (File.Exists(logPath))
                {
                    try
                    {
                        var raw = File.ReadAllText(logPath);
                        existingLines = raw.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
                    }
                    catch { }
                }

                existingLines.Add(line);
                var nextLines = existingLines
                    .Skip(Math.Max(0, existingLines.Count - _maxReports))
                    .Take(_maxReports)
                    .ToList();

                File.WriteAllText(logPath, string.Join("\n", nextLines) + "\n");
            }
            catch { }
        }
    }
}
