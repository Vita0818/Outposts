using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalRealDataShadowCopyKind
    {
        recordingMetadata,
        studyMetadata,
        receiveRecord,
        generatedArtifact,
        audioDescriptor,
        audioBytes
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalRealDataShadowCopyBytesMode
    {
        inlineBytes,
        fileBytes,
        descriptorOnly
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalRealDataShadowCopyHashPolicy
    {
        useProvidedHash,
        computeIfBounded,
        hashUnavailable
    }

    public enum CanonicalRealDataShadowCopyCleanupPolicyKind
    {
        cleanupImmediately,
        retainForDiagnostics,
        cleanupOnNextLaunch
    }

    public class CanonicalRealDataShadowCopyCleanupPolicy : IEquatable<CanonicalRealDataShadowCopyCleanupPolicy>
    {
        public CanonicalRealDataShadowCopyCleanupPolicyKind Kind { get; }
        public double MaxAge { get; }
        public long MaxBytes { get; }

        private CanonicalRealDataShadowCopyCleanupPolicy()
        {
            Kind = CanonicalRealDataShadowCopyCleanupPolicyKind.cleanupImmediately;
        }

        private CanonicalRealDataShadowCopyCleanupPolicy(CanonicalRealDataShadowCopyCleanupPolicyKind kind,
            double maxAge, long maxBytes)
        {
            Kind = kind;
            MaxAge = MaxAge;
            MaxBytes = maxBytes;
        }

        public static readonly CanonicalRealDataShadowCopyCleanupPolicy CleanupImmediately = new();
        public static readonly CanonicalRealDataShadowCopyCleanupPolicy CleanupOnNextLaunch =
            new() { Kind = CanonicalRealDataShadowCopyCleanupPolicyKind.cleanupOnNextLaunch };

        public static CanonicalRealDataShadowCopyCleanupPolicy RetainForDiagnostics(
            double maxAge, long maxBytes) =>
            new(CanonicalRealDataShadowCopyCleanupPolicyKind.retainForDiagnostics, maxAge, maxBytes);

        public bool Equals(CanonicalRealDataShadowCopyCleanupPolicy? other)
        {
            if (other == null) return false;
            return Kind == other.Kind && MaxAge == other.MaxAge && MaxBytes == other.MaxBytes;
        }

        public override bool Equals(object? obj) =>
            obj is CanonicalRealDataShadowCopyCleanupPolicy other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Kind, MaxAge, MaxBytes);
    }

    public record CanonicalRealDataShadowCopyPolicy : IEquatable<CanonicalRealDataShadowCopyPolicy>
    {
        public bool IsEnabled { get; init; }
        public long MaxMetadataBytes { get; init; }
        public long MaxGeneratedArtifactBytes { get; init; }
        public long MaxAudioBytes { get; init; }
        public bool CopyAudioBytesByDefault { get; init; }
        public bool AllowHashComputationForBoundedBytes { get; init; }
        public bool RequireHashForEqualityProof { get; init; }
        public CanonicalRealDataShadowCopyCleanupPolicy CleanupPolicy { get; init; }

        public CanonicalRealDataShadowCopyPolicy(
            bool isEnabled = false,
            long maxMetadataBytes = 1 * 1024 * 1024,
            long maxGeneratedArtifactBytes = 512 * 1024,
            long maxAudioBytes = 8 * 1024 * 1024,
            bool copyAudioBytesByDefault = false,
            bool allowHashComputationForBoundedBytes = true,
            bool requireHashForEqualityProof = true,
            CanonicalRealDataShadowCopyCleanupPolicy? cleanupPolicy = null)
        {
            IsEnabled = isEnabled;
            MaxMetadataBytes = Math.Max(0, maxMetadataBytes);
            MaxGeneratedArtifactBytes = Math.Max(0, maxGeneratedArtifactBytes);
            MaxAudioBytes = Math.Max(0, maxAudioBytes);
            CopyAudioBytesByDefault = copyAudioBytesByDefault;
            AllowHashComputationForBoundedBytes = allowHashComputationForBoundedBytes;
            RequireHashForEqualityProof = requireHashForEqualityProof;
            CleanupPolicy = cleanupPolicy ?? CanonicalRealDataShadowCopyCleanupPolicy.CleanupImmediately;
        }

        public static readonly CanonicalRealDataShadowCopyPolicy Disabled = new();

        public static CanonicalRealDataShadowCopyPolicy Enabled(
            CanonicalRealDataShadowCopyCleanupPolicy? cleanupPolicy = null)
        {
            return new CanonicalRealDataShadowCopyPolicy(
                isEnabled: true,
                cleanupPolicy: cleanupPolicy ?? CanonicalRealDataShadowCopyCleanupPolicy.CleanupImmediately);
        }

        public long MaxBytesFor(CanonicalRealDataShadowCopyKind kind)
        {
            return kind switch
            {
                CanonicalRealDataShadowCopyKind.recordingMetadata or CanonicalRealDataShadowCopyKind.studyMetadata
                    or CanonicalRealDataShadowCopyKind.receiveRecord or CanonicalRealDataShadowCopyKind.audioDescriptor
                    => MaxMetadataBytes,
                CanonicalRealDataShadowCopyKind.generatedArtifact => MaxGeneratedArtifactBytes,
                CanonicalRealDataShadowCopyKind.audioBytes => CopyAudioBytesByDefault ? MaxAudioBytes : 0,
                _ => 0
            };
        }
    }

    public record CanonicalRealDataShadowCopySource : IEquatable<CanonicalRealDataShadowCopySource>
    {
        public string SourceID { get; init; }
        public CanonicalRealDataShadowCopyKind Kind { get; init; }
        public string LogicalName { get; init; }
        public string TargetLogicalPathToken { get; init; }
        public string? ProductionRootPath { get; init; }
        public string? SourcePath { get; init; }
        public byte[]? InlineBytes { get; init; }
        public long? ByteSize { get; init; }
        public DateTime? ModifiedAt { get; init; }
        public CanonicalHash? ContentHash { get; init; }
        public CanonicalRealDataShadowCopyBytesMode BytesMode { get; init; }
        public CanonicalRealDataShadowCopyHashPolicy HashPolicy { get; init; }

        public CanonicalRealDataShadowCopySource(
            string sourceID,
            CanonicalRealDataShadowCopyKind kind,
            string logicalName,
            string targetLogicalPathToken,
            string? productionRootPath = null,
            string? sourcePath = null,
            byte[]? inlineBytes = null,
            long? byteSize = null,
            DateTime? modifiedAt = null,
            CanonicalHash? contentHash = null,
            CanonicalRealDataShadowCopyBytesMode bytesMode = default,
            CanonicalRealDataShadowCopyHashPolicy hashPolicy = CanonicalRealDataShadowCopyHashPolicy.computeIfBounded)
        {
            SourceID = CanonicalProductionRedaction.SafeIdentifier(sourceID, "source");
            Kind = kind;
            LogicalName = CanonicalProjectionContract.LogicalNameFrom(logicalName) ?? kind.ToString();
            TargetLogicalPathToken = targetLogicalPathToken.Trim();
            ProductionRootPath = productionRootPath != null
                ? Path.GetFullPath(productionRootPath) : null;
            SourcePath = sourcePath != null ? Path.GetFullPath(sourcePath) : null;
            InlineBytes = inlineBytes;
            ByteSize = byteSize.HasValue ? Math.Max(0, byteSize.Value) : null;
            ModifiedAt = modifiedAt;
            ContentHash = contentHash;
            BytesMode = bytesMode;
            HashPolicy = hashPolicy;
        }

        public static CanonicalRealDataShadowCopySource Inline(
            string sourceID,
            CanonicalRealDataShadowCopyKind kind,
            string logicalName,
            string targetLogicalPathToken,
            byte[] bytes,
            CanonicalHash? contentHash = null,
            DateTime? modifiedAt = null)
        {
            return new CanonicalRealDataShadowCopySource(
                sourceID, kind, logicalName, targetLogicalPathToken,
                inlineBytes: bytes, byteSize: bytes.Length,
                modifiedAt: modifiedAt, contentHash: contentHash,
                bytesMode: CanonicalRealDataShadowCopyBytesMode.inlineBytes);
        }

        public static CanonicalRealDataShadowCopySource File(
            string sourceID,
            CanonicalRealDataShadowCopyKind kind,
            string logicalName,
            string targetLogicalPathToken,
            string productionRootPath,
            string sourcePath,
            long? byteSize = null,
            DateTime? modifiedAt = null,
            CanonicalHash? contentHash = null)
        {
            return new CanonicalRealDataShadowCopySource(
                sourceID, kind, logicalName, targetLogicalPathToken,
                productionRootPath: productionRootPath, sourcePath: sourcePath,
                byteSize: byteSize, modifiedAt: modifiedAt, contentHash: contentHash,
                bytesMode: CanonicalRealDataShadowCopyBytesMode.fileBytes);
        }

        public static CanonicalRealDataShadowCopySource Descriptor(
            string sourceID,
            string logicalName,
            string targetLogicalPathToken,
            byte[] descriptorBytes,
            long? byteSize = null,
            CanonicalHash? contentHash = null)
        {
            return new CanonicalRealDataShadowCopySource(
                sourceID, CanonicalRealDataShadowCopyKind.audioDescriptor,
                logicalName, targetLogicalPathToken,
                inlineBytes: descriptorBytes, byteSize: byteSize,
                contentHash: contentHash,
                bytesMode: CanonicalRealDataShadowCopyBytesMode.descriptorOnly,
                hashPolicy: CanonicalRealDataShadowCopyHashPolicy.hashUnavailable);
        }
    }

    public record CanonicalRealDataShadowCopyTarget : IEquatable<CanonicalRealDataShadowCopyTarget>
    {
        public CanonicalRootToken RootToken { get; init; }
        public CanonicalShadowRootKind RootKind { get; init; }
        public string RootPath { get; init; }
        public string? ProhibitedProductionRootPath { get; init; }

        public CanonicalRealDataShadowCopyTarget(
            CanonicalRootToken rootToken,
            CanonicalShadowRootKind rootKind = CanonicalShadowRootKind.shadowCopy,
            string rootPath = "",
            string? prohibitedProductionRootPath = null)
        {
            RootToken = rootToken;
            RootKind = rootKind;
            RootPath = Path.GetFullPath(rootPath);
            ProhibitedProductionRootPath = prohibitedProductionRootPath != null
                ? Path.GetFullPath(prohibitedProductionRootPath) : null;
        }

        public CanonicalShadowRootBinding Binding => new CanonicalShadowRootBinding(
            RootToken, RootKind, RootPath, ProhibitedProductionRootPath);
    }

    public record CanonicalRealDataShadowCopyPlan : IEquatable<CanonicalRealDataShadowCopyPlan>
    {
        public string PlanID { get; init; }
        public List<CanonicalRealDataShadowCopySource> Sources { get; init; }
        public CanonicalRealDataShadowCopyTarget Target { get; init; }
        public CanonicalRealDataShadowCopyPolicy Policy { get; init; }

        public CanonicalRealDataShadowCopyPlan(
            string? planID = null,
            List<CanonicalRealDataShadowCopySource>? sources = null,
            CanonicalRealDataShadowCopyTarget? target = null,
            CanonicalRealDataShadowCopyPolicy? policy = null)
        {
            PlanID = CanonicalProductionRedaction.SafeIdentifier(
                planID ?? Guid.NewGuid().ToString(), "real-data-shadow-copy");
            Sources = (sources ?? new List<CanonicalRealDataShadowCopySource>())
                .OrderBy(s => s.SourceID).ToList();
            Target = target ?? new CanonicalRealDataShadowCopyTarget(
                new CanonicalRootToken("shadow-copy"));
            Policy = policy ?? CanonicalRealDataShadowCopyPolicy.Enabled();
        }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalRealDataShadowCopyFailure
    {
        disabled,
        sourceEqualsTarget,
        targetIsProductionRoot,
        targetInsideProductionRoot,
        targetPathInvalid,
        sourceOutsideProductionRoot,
        unsafeLogicalPathToken,
        sourceReadFailed,
        sourceTooLarge,
        writeFailed,
        verificationFailed,
        hashMismatch,
        hashUnavailableWhereRequired,
        cleanupFailed,
        unexpected
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalRealDataShadowCopyVerificationStatus
    {
        verified,
        evidenceOnly,
        hashUnavailable,
        mismatch,
        descriptorOnly,
        failed
    }

    public record CanonicalRealDataShadowCopyVerification : IEquatable<CanonicalRealDataShadowCopyVerification>
    {
        public string Id => SourceID;
        public string SourceID { get; init; }
        public CanonicalRealDataShadowCopyKind Kind { get; init; }
        public string LogicalName { get; init; }
        public long ByteSize { get; init; }
        public CanonicalTimestamp? ModifiedAt { get; init; }
        public string? HashPrefix { get; init; }
        public bool CopiedBytes { get; init; }
        public bool DescriptorOnly { get; init; }
        public bool EqualityProof { get; init; }
        public CanonicalRealDataShadowCopyVerificationStatus Status { get; init; }
        public string? Reason { get; init; }

        public CanonicalRealDataShadowCopyVerification(
            string sourceID,
            CanonicalRealDataShadowCopyKind kind,
            string logicalName,
            long byteSize,
            DateTime? modifiedAt = null,
            CanonicalHash? contentHash = null,
            bool copiedBytes = false,
            bool descriptorOnly = false,
            bool equalityProof = false,
            CanonicalRealDataShadowCopyVerificationStatus status = default,
            string? reason = null)
        {
            SourceID = CanonicalProductionRedaction.SafeIdentifier(sourceID, "source");
            Kind = kind;
            LogicalName = CanonicalProjectionContract.LogicalNameFrom(logicalName) ?? kind.ToString();
            ByteSize = Math.Max(0, byteSize);
            ModifiedAt = modifiedAt.HasValue ? new CanonicalTimestamp(modifiedAt.Value) : null;
            HashPrefix = contentHash != null
                ? CanonicalProductionRedaction.HashPrefix(contentHash.Value) : null;
            CopiedBytes = copiedBytes;
            DescriptorOnly = descriptorOnly;
            EqualityProof = equalityProof;
            Status = status;
            Reason = CanonicalShadowMigrationRedaction.SafeText(reason);
        }
    }

    public record CanonicalRealDataShadowCopyResult : IEquatable<CanonicalRealDataShadowCopyResult>
    {
        public string PlanID { get; init; }
        public string RootID { get; init; }
        public CanonicalShadowRootKind RootKind { get; init; }
        public bool Started { get; init; }
        public bool Completed { get; init; }
        public bool Unavailable { get; init; }
        public string VerificationStatus { get; init; }
        public int CopiedEntryCount { get; init; }
        public int DescriptorOnlyAudioCount { get; init; }
        public long BytesCopied { get; init; }
        public int HashUnavailableCount { get; init; }
        public int EqualityProofCount { get; init; }
        public CanonicalRealDataShadowCopyFailure? Failure { get; init; }
        public string? FailureReason { get; init; }
        public List<CanonicalRealDataShadowCopyVerification> Verifications { get; init; }

        public CanonicalRealDataShadowCopyResult(
            string planID,
            string rootID,
            CanonicalShadowRootKind rootKind,
            bool started,
            bool completed,
            bool unavailable = false,
            CanonicalRealDataShadowCopyFailure? failure = null,
            string? failureReason = null,
            List<CanonicalRealDataShadowCopyVerification>? verifications = null)
        {
            PlanID = CanonicalProductionRedaction.SafeIdentifier(planID, "real-data-shadow-copy");
            RootID = CanonicalProductionRedaction.SafeIdentifier(rootID, "shadow-root");
            RootKind = rootKind;
            Started = started;
            Completed = completed;
            Unavailable = unavailable;
            Failure = failure;
            FailureReason = CanonicalShadowMigrationRedaction.SafeText(failureReason);
            Verifications = (verifications ?? new List<CanonicalRealDataShadowCopyVerification>())
                .OrderBy(v => v.SourceID).ToList();
            CopiedEntryCount = Verifications.Count(v => v.CopiedBytes);
            DescriptorOnlyAudioCount = Verifications.Count(v => v.DescriptorOnly);
            BytesCopied = Verifications.Where(v => v.CopiedBytes).Sum(v => v.ByteSize);
            HashUnavailableCount = Verifications.Count(v => v.Status == CanonicalRealDataShadowCopyVerificationStatus.hashUnavailable);
            EqualityProofCount = Verifications.Count(v => v.EqualityProof);

            if (failure.HasValue)
                VerificationStatus = failure.Value.ToString();
            else if (Verifications.Any(v => v.Status == CanonicalRealDataShadowCopyVerificationStatus.mismatch
                || v.Status == CanonicalRealDataShadowCopyVerificationStatus.failed))
                VerificationStatus = "failed";
            else if (Verifications.Any(v => v.Status == CanonicalRealDataShadowCopyVerificationStatus.hashUnavailable))
                VerificationStatus = "hashUnavailable";
            else if (completed)
                VerificationStatus = "verified";
            else
                VerificationStatus = "notStarted";
        }

        public static CanonicalRealDataShadowCopyResult UnavailableResult(
            string planID, string rootID, CanonicalShadowRootKind rootKind,
            CanonicalRealDataShadowCopyFailure failure, string reason)
        {
            return new CanonicalRealDataShadowCopyResult(
                planID, rootID, rootKind, false, false,
                unavailable: true, failure: failure, failureReason: reason);
        }

        public string DiagnosticsSummary =>
            string.Join(",", new[]
            {
                $"realDataCopy={(Completed ? "completed" : (Unavailable ? "unavailable" : "failed"))}",
                $"rootKind={RootKind}",
                $"entries={CopiedEntryCount}",
                $"audioDescriptors={DescriptorOnlyAudioCount}",
                $"bytes={BytesCopied}",
                $"hashUnavailable={HashUnavailableCount}",
                $"equalityProofs={EqualityProofCount}",
                $"verification={VerificationStatus}",
                $"failure={Failure?.ToString() ?? "none"}"
            });
    }

    public class CanonicalRealDataShadowCopyRunner
    {
        public CanonicalRealDataShadowCopyRunner() { }

        public CanonicalRealDataShadowCopyResult Run(
            CanonicalRealDataShadowCopyPlan plan)
        {
            if (!plan.Policy.IsEnabled)
                return CanonicalRealDataShadowCopyResult.UnavailableResult(
                    plan.PlanID, plan.Target.RootToken.RawValue, plan.Target.RootKind,
                    CanonicalRealDataShadowCopyFailure.disabled,
                    "realDataShadowCopyDisabled");

            try
            {
                var targetRoot = ValidatedTargetRoot(plan.Target);
                Directory.CreateDirectory(targetRoot);
                var verifications = new List<CanonicalRealDataShadowCopyVerification>();
                foreach (var source in plan.Sources)
                {
                    var verification = Copy(source, targetRoot, plan.Policy);
                    verifications.Add(verification);
                    if (plan.Policy.RequireHashForEqualityProof
                        && verification.CopiedBytes
                        && verification.Status == CanonicalRealDataShadowCopyVerificationStatus.hashUnavailable)
                    {
                        return new CanonicalRealDataShadowCopyResult(
                            plan.PlanID, plan.Target.RootToken.RawValue, plan.Target.RootKind,
                            true, false,
                            failure: CanonicalRealDataShadowCopyFailure.hashUnavailableWhereRequired,
                            failureReason: "hashUnavailableWhereRequired",
                            verifications: verifications);
                    }
                }
                return new CanonicalRealDataShadowCopyResult(
                    plan.PlanID, plan.Target.RootToken.RawValue, plan.Target.RootKind,
                    true, true, verifications: verifications);
            }
            catch (RunnerFailureException rf)
            {
                return new CanonicalRealDataShadowCopyResult(
                    plan.PlanID, plan.Target.RootToken.RawValue, plan.Target.RootKind,
                    true, false,
                    failure: rf.Failure, failureReason: rf.Reason,
                    verifications: rf.Verifications);
            }
            catch (Exception ex)
            {
                return new CanonicalRealDataShadowCopyResult(
                    plan.PlanID, plan.Target.RootToken.RawValue, plan.Target.RootKind,
                    true, false,
                    failure: CanonicalRealDataShadowCopyFailure.unexpected,
                    failureReason: ex.ToString());
            }
        }

        private class RunnerFailureException : Exception
        {
            public CanonicalRealDataShadowCopyFailure Failure { get; }
            public string Reason { get; }
            public List<CanonicalRealDataShadowCopyVerification> Verifications { get; }

            public RunnerFailureException(CanonicalRealDataShadowCopyFailure failure, string reason,
                List<CanonicalRealDataShadowCopyVerification>? verifications = null)
                : base(reason)
            {
                Failure = failure;
                Reason = reason;
                Verifications = verifications ?? new List<CanonicalRealDataShadowCopyVerification>();
            }
        }

        private static string ValidatedTargetRoot(CanonicalRealDataShadowCopyTarget target)
        {
            try
            {
                return target.Binding.ValidatedShadowRootPath();
            }
            catch (Exception ex)
            {
                var reason = ex.Message;
                var failure = reason.Contains("shadowRootInsideProductionRootRejected")
                    ? CanonicalRealDataShadowCopyFailure.targetInsideProductionRoot
                    : CanonicalRealDataShadowCopyFailure.targetIsProductionRoot;
                throw new RunnerFailureException(failure, reason);
            }
        }

        private static CanonicalRealDataShadowCopyVerification Copy(
            CanonicalRealDataShadowCopySource source,
            string targetRoot,
            CanonicalRealDataShadowCopyPolicy policy)
        {
            var safeToken = CanonicalProjectionContract.SafeLogicalPathToken(source.TargetLogicalPathToken);
            if (safeToken == null)
                throw new RunnerFailureException(
                    CanonicalRealDataShadowCopyFailure.unsafeLogicalPathToken,
                    "unsafeLogicalPathToken");

            var destination = Path.Combine(targetRoot, safeToken);
            var targetRootFull = Path.GetFullPath(targetRoot).TrimEnd(Path.DirectorySeparatorChar);
            var destFull = Path.GetFullPath(destination);

            if (!destFull.StartsWith(targetRootFull + Path.DirectorySeparatorChar)
                && destFull != targetRootFull)
                throw new RunnerFailureException(
                    CanonicalRealDataShadowCopyFailure.targetPathInvalid,
                    "targetPathEscape");

            if (source.SourcePath != null)
            {
                var sourceFull = Path.GetFullPath(source.SourcePath);
                if (sourceFull == destFull)
                    throw new RunnerFailureException(
                        CanonicalRealDataShadowCopyFailure.sourceEqualsTarget,
                        "sourceEqualsTarget");
                if (source.ProductionRootPath != null)
                {
                    var prodFull = Path.GetFullPath(source.ProductionRootPath).TrimEnd(Path.DirectorySeparatorChar);
                    if (!sourceFull.StartsWith(prodFull + Path.DirectorySeparatorChar)
                        && sourceFull != prodFull)
                        throw new RunnerFailureException(
                            CanonicalRealDataShadowCopyFailure.sourceOutsideProductionRoot,
                            "sourceOutsideProductionRoot");
                }
            }

            var maxBytes = policy.MaxBytesFor(source.Kind);
            var descriptorOnly = source.BytesMode == CanonicalRealDataShadowCopyBytesMode.descriptorOnly
                || (source.Kind == CanonicalRealDataShadowCopyKind.audioBytes && !policy.CopyAudioBytesByDefault);
            byte[] loadedBytes;
            bool copiedBytes;
            long knownSize;

            if (descriptorOnly && source.InlineBytes != null)
            {
                loadedBytes = source.InlineBytes;
                copiedBytes = true;
                knownSize = loadedBytes.Length;
            }
            else if (source.BytesMode == CanonicalRealDataShadowCopyBytesMode.inlineBytes
                && source.InlineBytes != null)
            {
                if (maxBytes > 0 && source.InlineBytes.Length > maxBytes)
                    throw new RunnerFailureException(
                        CanonicalRealDataShadowCopyFailure.sourceTooLarge, "sourceTooLarge");
                loadedBytes = source.InlineBytes;
                copiedBytes = true;
                knownSize = loadedBytes.Length;
            }
            else if (source.BytesMode == CanonicalRealDataShadowCopyBytesMode.fileBytes
                && source.SourcePath != null)
            {
                knownSize = source.ByteSize ?? FileSize(source.SourcePath);
                if (maxBytes > 0 && knownSize > maxBytes)
                    throw new RunnerFailureException(
                        CanonicalRealDataShadowCopyFailure.sourceTooLarge, "sourceTooLarge");
                try
                {
                    loadedBytes = File.ReadAllBytes(source.SourcePath);
                    copiedBytes = true;
                }
                catch
                {
                    throw new RunnerFailureException(
                        CanonicalRealDataShadowCopyFailure.sourceReadFailed, "sourceReadFailed");
                }
            }
            else
            {
                throw new RunnerFailureException(
                    CanonicalRealDataShadowCopyFailure.sourceReadFailed, "sourceBytesUnavailable");
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.WriteAllBytes(destination, loadedBytes);
            }
            catch
            {
                throw new RunnerFailureException(
                    CanonicalRealDataShadowCopyFailure.writeFailed, "writeFailed");
            }

            byte[] actualBytes;
            try
            {
                actualBytes = File.ReadAllBytes(destination);
            }
            catch
            {
                throw new RunnerFailureException(
                    CanonicalRealDataShadowCopyFailure.verificationFailed, "verificationReadFailed");
            }
            if (actualBytes.Length != loadedBytes.Length)
                throw new RunnerFailureException(
                    CanonicalRealDataShadowCopyFailure.verificationFailed, "sizeMismatch");

            var actualHash = Hash(actualBytes, source, policy);
            if (source.ContentHash != null && actualHash != null
                && !Equals(source.ContentHash, actualHash))
                throw new RunnerFailureException(
                    CanonicalRealDataShadowCopyFailure.hashMismatch, "hashMismatch");

            var status = descriptorOnly
                ? CanonicalRealDataShadowCopyVerificationStatus.descriptorOnly
                : actualHash != null
                    ? CanonicalRealDataShadowCopyVerificationStatus.verified
                    : CanonicalRealDataShadowCopyVerificationStatus.hashUnavailable;
            var equalityProof = !descriptorOnly && actualHash != null;

            return new CanonicalRealDataShadowCopyVerification(
                source.SourceID, source.Kind, source.LogicalName, actualBytes.Length,
                source.ModifiedAt, actualHash ?? source.ContentHash,
                copiedBytes, descriptorOnly, equalityProof, status, status.ToString());
        }

        private static CanonicalHash? Hash(
            byte[] bytes,
            CanonicalRealDataShadowCopySource source,
            CanonicalRealDataShadowCopyPolicy policy)
        {
            return source.HashPolicy switch
            {
                CanonicalRealDataShadowCopyHashPolicy.useProvidedHash => source.ContentHash,
                CanonicalRealDataShadowCopyHashPolicy.computeIfBounded =>
                    policy.AllowHashComputationForBoundedBytes
                        ? InMemoryCanonicalFileStore.Hash(bytes, CanonicalHashPolicy.sha256)
                        : source.ContentHash,
                CanonicalRealDataShadowCopyHashPolicy.hashUnavailable => source.ContentHash,
                _ => source.ContentHash
            };
        }

        private static long FileSize(string path)
        {
            try
            {
                return new FileInfo(path).Length;
            }
            catch
            {
                return long.MaxValue;
            }
        }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CanonicalShadowRootCleanupStatus
    {
        removed,
        retainedForDiagnostics,
        retainedForNextLaunch,
        refusedProductionRoot,
        failed
    }

    public record CanonicalShadowRootRetentionRecord : IEquatable<CanonicalShadowRootRetentionRecord>
    {
        public string Id => RootID;
        public string RootID { get; init; }
        public CanonicalShadowRootKind RootKind { get; init; }
        public CanonicalTimestamp CreatedAt { get; init; }
        public long RetainedBytes { get; init; }
        public int EntryCount { get; init; }

        public CanonicalShadowRootRetentionRecord(
            string rootID,
            CanonicalShadowRootKind rootKind,
            DateTime? createdAt = null,
            long retainedBytes = 0,
            int entryCount = 0)
        {
            RootID = CanonicalProductionRedaction.SafeIdentifier(rootID, "shadow-root");
            RootKind = rootKind;
            CreatedAt = new CanonicalTimestamp(createdAt ?? DateTime.UtcNow);
            RetainedBytes = Math.Max(0, retainedBytes);
            EntryCount = Math.Max(0, entryCount);
        }
    }

    public record CanonicalShadowRootCleanupResult : IEquatable<CanonicalShadowRootCleanupResult>
    {
        public string RootID { get; init; }
        public CanonicalShadowRootKind RootKind { get; init; }
        public CanonicalShadowRootCleanupStatus Status { get; init; }
        public int RemovedRootCount { get; init; }
        public int RetainedRootCount { get; init; }
        public long RemovedBytes { get; init; }
        public long RetainedBytes { get; init; }
        public string? FailureReason { get; init; }

        public CanonicalShadowRootCleanupResult(
            string rootID,
            CanonicalShadowRootKind rootKind,
            CanonicalShadowRootCleanupStatus status,
            int removedRootCount = 0,
            int retainedRootCount = 0,
            long removedBytes = 0,
            long retainedBytes = 0,
            string? failureReason = null)
        {
            RootID = CanonicalProductionRedaction.SafeIdentifier(rootID, "shadow-root");
            RootKind = rootKind;
            Status = status;
            RemovedRootCount = Math.Max(0, removedRootCount);
            RetainedRootCount = Math.Max(0, retainedRootCount);
            RemovedBytes = Math.Max(0, removedBytes);
            RetainedBytes = Math.Max(0, retainedBytes);
            FailureReason = CanonicalShadowMigrationRedaction.SafeText(failureReason);
        }

        public string DiagnosticsSummary =>
            string.Join(",", new[]
            {
                $"rootKind={RootKind}",
                $"rootID={RootID}",
                $"cleanup={Status}",
                $"removedRoots={RemovedRootCount}",
                $"retainedRoots={RetainedRootCount}",
                $"removedBytes={RemovedBytes}",
                $"retainedBytes={RetainedBytes}",
                $"failure={FailureReason ?? "none"}"
            });
    }

    public record CanonicalShadowRootLifecycle : IEquatable<CanonicalShadowRootLifecycle>
    {
        public string RootID { get; init; }
        public CanonicalShadowRootKind RootKind { get; init; }
        public string RootPath { get; init; }
        public string? ProductionRootPath { get; init; }
        public DateTime CreatedAt { get; init; }

        public CanonicalShadowRootLifecycle(
            string? rootID = null,
            CanonicalShadowRootKind rootKind = default,
            string rootPath = "",
            string? productionRootPath = null,
            DateTime? createdAt = null)
        {
            RootID = CanonicalProductionRedaction.SafeIdentifier(
                rootID ?? Guid.NewGuid().ToString(), "shadow-root");
            RootKind = rootKind;
            RootPath = Path.GetFullPath(rootPath);
            ProductionRootPath = productionRootPath != null
                ? Path.GetFullPath(productionRootPath) : null;
            CreatedAt = createdAt ?? DateTime.UtcNow;
        }

        public CanonicalShadowRootRetentionRecord RetentionRecord =>
            new CanonicalShadowRootRetentionRecord(RootID, RootKind, CreatedAt);

        public CanonicalShadowRootCleanupResult Cleanup(
            CanonicalRealDataShadowCopyCleanupPolicy policy,
            DateTime? now = null)
        {
            var nowDt = now ?? DateTime.UtcNow;
            if (ProductionRootPath != null)
            {
                var shadowPath = Path.GetFullPath(RootPath).TrimEnd(Path.DirectorySeparatorChar);
                var productionPath = Path.GetFullPath(ProductionRootPath).TrimEnd(Path.DirectorySeparatorChar);
                if (shadowPath == productionPath
                    || shadowPath.StartsWith(productionPath + Path.DirectorySeparatorChar))
                {
                    return new CanonicalShadowRootCleanupResult(
                        RootID, RootKind, CanonicalShadowRootCleanupStatus.refusedProductionRoot,
                        failureReason: "productionRootRefused");
                }
            }

            switch (policy.Kind)
            {
                case CanonicalRealDataShadowCopyCleanupPolicyKind.cleanupImmediately:
                    var bytes = DirectorySize(RootPath);
                    try
                    {
                        if (Directory.Exists(RootPath))
                            Directory.Delete(RootPath, true);
                        else if (File.Exists(RootPath))
                            File.Delete(RootPath);
                        return new CanonicalShadowRootCleanupResult(
                            RootID, RootKind, CanonicalShadowRootCleanupStatus.removed,
                            removedRootCount: 1, removedBytes: bytes);
                    }
                    catch
                    {
                        return new CanonicalShadowRootCleanupResult(
                            RootID, RootKind, CanonicalShadowRootCleanupStatus.failed,
                            retainedRootCount: 1, retainedBytes: bytes,
                            failureReason: "cleanupFailed");
                    }
                case CanonicalRealDataShadowCopyCleanupPolicyKind.retainForDiagnostics:
                    var parent = Path.GetDirectoryName(RootPath);
                    var retainedBytes = DirectorySize(RootPath);
                    var purge = PurgeRetainedRoots(
                        parent ?? "", RootPath, policy.MaxAge, policy.MaxBytes, nowDt);
                    return new CanonicalShadowRootCleanupResult(
                        RootID, RootKind, CanonicalShadowRootCleanupStatus.retainedForDiagnostics,
                        removedRootCount: purge.RemovedCount,
                        retainedRootCount: 1,
                        removedBytes: purge.RemovedBytes,
                        retainedBytes: Math.Min(retainedBytes, policy.MaxBytes));
                case CanonicalRealDataShadowCopyCleanupPolicyKind.cleanupOnNextLaunch:
                    return new CanonicalShadowRootCleanupResult(
                        RootID, RootKind, CanonicalShadowRootCleanupStatus.retainedForNextLaunch,
                        retainedRootCount: Directory.Exists(RootPath) || File.Exists(RootPath) ? 1 : 0,
                        retainedBytes: DirectorySize(RootPath));
                default:
                    return new CanonicalShadowRootCleanupResult(
                        RootID, RootKind, CanonicalShadowRootCleanupStatus.failed,
                        failureReason: "unknownPolicy");
            }
        }

        private static (int RemovedCount, long RemovedBytes) PurgeRetainedRoots(
            string parentDirectory, string protectedRootPath,
            double maxAge, long maxBytes, DateTime now)
        {
            if (!Directory.Exists(parentDirectory))
                return (0, 0);

            string[] entries;
            try { entries = Directory.GetFileSystemEntries(parentDirectory); }
            catch { return (0, 0); }

            var protectedFull = Path.GetFullPath(protectedRootPath);
            var candidates = new List<(string Path, DateTime CreatedAt, long Bytes)>();
            foreach (var entry in entries)
            {
                try
                {
                    if (Path.GetFullPath(entry) == protectedFull) continue;
                    var createdAt = Directory.Exists(entry)
                        ? Directory.GetCreationTime(entry)
                        : File.GetCreationTime(entry);
                    var size = DirectorySize(entry);
                    candidates.Add((entry, createdAt, size));
                }
                catch { }
            }

            var removedCount = 0;
            long removedBytes = 0;

            foreach (var c in candidates.ToList())
            {
                if ((now - c.CreatedAt).TotalSeconds > maxAge)
                {
                    try
                    {
                        if (Directory.Exists(c.Path)) Directory.Delete(c.Path, true);
                        else File.Delete(c.Path);
                        removedCount++;
                        removedBytes += c.Bytes;
                    }
                    catch { }
                    candidates.RemoveAll(x => x.Path == c.Path);
                }
            }

            var totalBytes = DirectorySize(protectedRootPath)
                + candidates.Sum(c => c.Bytes);
            foreach (var c in candidates.OrderBy(c => c.CreatedAt))
            {
                if (totalBytes <= maxBytes) break;
                try
                {
                    if (Directory.Exists(c.Path)) Directory.Delete(c.Path, true);
                    else File.Delete(c.Path);
                    removedCount++;
                    removedBytes += c.Bytes;
                    totalBytes -= c.Bytes;
                }
                catch { }
            }
            return (removedCount, removedBytes);
        }

        private static long DirectorySize(string path)
        {
            try
            {
                if (File.Exists(path))
                    return new FileInfo(path).Length;
                if (!Directory.Exists(path))
                    return 0;
                var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                return files.Sum(f =>
                {
                    try { return new FileInfo(f).Length; }
                    catch { return 0; }
                });
            }
            catch
            {
                return 0;
            }
        }
    }
}
