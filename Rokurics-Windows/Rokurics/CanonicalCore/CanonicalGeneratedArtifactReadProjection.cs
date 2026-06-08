using System.Text.Json.Serialization;

namespace Rokurics.CanonicalCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactReadProjectionSource
{
    legacy,
    canonical
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactReadProjectionFailureKind
{
    snapshotMissing,
    unsupportedArtifactKind,
    unsafePathToken,
    contentLeakRisk,
    audioConfusionRisk,
    tombstonedParentResurrectionRisk
}

public sealed record CanonicalGeneratedArtifactReadProjectionFailure : IEquatable<CanonicalGeneratedArtifactReadProjectionFailure>
{
    public string Id => string.Join("|", Kind.ToString(), ObjectID ?? "", ArtifactID ?? "", ArtifactKind?.ToString() ?? "");

    public CanonicalGeneratedArtifactReadProjectionFailureKind Kind { get; }
    public CanonicalGeneratedArtifactReadProjectionSource Source { get; }
    public string? ObjectID { get; }
    public string? ArtifactID { get; }
    public CanonicalArtifact.Kind? ArtifactKind { get; }
    public string Reason { get; }

    public CanonicalGeneratedArtifactReadProjectionFailure(
        CanonicalGeneratedArtifactReadProjectionFailureKind kind,
        CanonicalGeneratedArtifactReadProjectionSource source,
        string? objectID = null,
        string? artifactID = null,
        CanonicalArtifact.Kind? artifactKind = null,
        string reason = "")
    {
        Kind = kind;
        Source = source;
        ObjectID = objectID != null ? CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording") : null;
        ArtifactID = artifactID != null ? CanonicalProductionRedaction.SafeIdentifier(artifactID, "artifact:unknown") : null;
        ArtifactKind = artifactKind;
        Reason = CanonicalProductionRedaction.SafeDiagnosticText(reason) ?? kind.ToString();
    }

    public virtual bool Equals(CanonicalGeneratedArtifactReadProjectionFailure? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}

public sealed record CanonicalGeneratedArtifactReadProjectionArtifactFact : IEquatable<CanonicalGeneratedArtifactReadProjectionArtifactFact>
{
    public CanonicalArtifact Artifact { get; }
    public bool ParentTombstoned { get; }
    public bool LocalAvailability { get; }
    public bool PeerAuthoritativeAvailability { get; }
    public string? ProducerSummary { get; }
    public bool UnsafePathTokenObserved { get; }

    public CanonicalGeneratedArtifactReadProjectionArtifactFact(
        CanonicalArtifact artifact,
        bool parentTombstoned = false,
        bool localAvailability = false,
        bool peerAuthoritativeAvailability = false,
        string? producerSummary = null,
        bool unsafePathTokenObserved = false)
    {
        Artifact = artifact;
        ParentTombstoned = parentTombstoned;
        LocalAvailability = localAvailability;
        PeerAuthoritativeAvailability = peerAuthoritativeAvailability;
        ProducerSummary = producerSummary != null ? CanonicalProductionRedaction.SafeDiagnosticText(producerSummary) : null;
        UnsafePathTokenObserved = unsafePathTokenObserved;
    }

    public virtual bool Equals(CanonicalGeneratedArtifactReadProjectionArtifactFact? other) =>
        other is not null && Artifact.Equals(other.Artifact);
    public override int GetHashCode() => Artifact.GetHashCode();
}

public sealed record CanonicalGeneratedArtifactReadProjectionItem : IEquatable<CanonicalGeneratedArtifactReadProjectionItem>
{
    public string Id => string.Join("|", ObjectID, ArtifactKind.ToString());

    public CanonicalGeneratedArtifactReadProjectionSource Source { get; }
    public string ObjectID { get; }
    public string ArtifactID { get; }
    public CanonicalArtifact.Kind ArtifactKind { get; }
    public CanonicalArtifact.Availability Availability { get; }
    public long? ByteSize { get; }
    public string? HashPrefix { get; }
    public string? ProducerSummary { get; }
    public string? LogicalNameSummary { get; }
    public string? LogicalTokenSummary { get; }
    public string? LocalDownloadedState { get; }
    public string? PeerAuthoritativeState { get; }
    public string? UpdatedAtSummary { get; }
    public string? ParentObjectStateSummary { get; }
    public bool LocalAvailability { get; }
    public bool PeerAuthoritativeAvailability { get; }
    public bool ParentTombstoned { get; }
    public bool ContentIncluded { get; }
    public bool UnsafePathTokenObserved { get; }

    public CanonicalGeneratedArtifactReadProjectionItem(
        CanonicalGeneratedArtifactReadProjectionSource source,
        CanonicalGeneratedArtifactReadProjectionArtifactFact fact)
    {
        var a = fact.Artifact;
        Source = source;
        ObjectID = CanonicalProductionRedaction.SafeIdentifier(a.ObjectID, "unknown-recording");
        ArtifactID = CanonicalProductionRedaction.SafeIdentifier(a.ArtifactID, "artifact:unknown");
        ArtifactKind = a.Kind;
        Availability = a.AvailabilityValue;
        ByteSize = a.ByteSize;
        HashPrefix = a.ContentHash != null ? CanonicalProductionRedaction.HashPrefix(a.ContentHash.Value) : null;
        ProducerSummary = fact.ProducerSummary
            ?? a.ProducedBy?.ToString()
            ?? (a.ProducedByNodeID != null ? $"node:{CanonicalProductionRedaction.SafeIdentifier(a.ProducedByNodeID, "node")}" : null);
        LogicalNameSummary = a.LogicalName != null ? CanonicalProductionRedaction.SafeDiagnosticText(a.LogicalName) : null;
        LogicalTokenSummary = a.LogicalPathToken != null ? CanonicalProjectionContract.SafeLogicalPathToken(a.LogicalPathToken) : null;
        LocalDownloadedState = fact.LocalAvailability ? "downloadedOrLocalAvailable" : "notDownloadedOrUnavailable";
        PeerAuthoritativeState = fact.PeerAuthoritativeAvailability ? "peerAuthoritativeAvailable" : "peerNotAuthoritativeOrUnavailable";
        UpdatedAtSummary = a.ModifiedAt?.Date != null
            ? $"unixSeconds={(long)(a.ModifiedAt.Value.Date - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds}"
            : null;
        ParentObjectStateSummary = fact.ParentTombstoned ? "parentTombstoned" : "parentActiveOrUnknown";
        LocalAvailability = fact.LocalAvailability;
        PeerAuthoritativeAvailability = fact.PeerAuthoritativeAvailability;
        ParentTombstoned = fact.ParentTombstoned;
        ContentIncluded = false;
        UnsafePathTokenObserved = fact.UnsafePathTokenObserved;
    }

    public virtual bool Equals(CanonicalGeneratedArtifactReadProjectionItem? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}

public sealed record CanonicalGeneratedArtifactReadSnapshot : IEquatable<CanonicalGeneratedArtifactReadSnapshot>
{
    public CanonicalGeneratedArtifactReadProjectionSource Source { get; }
    public CanonicalTimestamp GeneratedAt { get; }
    public CanonicalGeneratedArtifactReadProjectionItem[] Items { get; }
    public CanonicalGeneratedArtifactReadProjectionFailure[] Failures { get; }
    public int ContentExcludedCount { get; }

    public int ItemCount => Items.Length;
    public int FailureCount => Failures.Length;
    public int ContentIncludedCount => Items.Count(i => i.ContentIncluded);

    public string DiagnosticsSummary => string.Join(",",
        $"source={Source}",
        $"items={ItemCount}",
        $"failures={FailureCount}",
        $"contentIncluded={ContentIncludedCount}",
        $"contentExcluded={ContentExcludedCount}"
    );

    public CanonicalGeneratedArtifactReadSnapshot(
        CanonicalGeneratedArtifactReadProjectionSource source,
        DateTime? generatedAt = null,
        CanonicalGeneratedArtifactReadProjectionItem[]? items = null,
        CanonicalGeneratedArtifactReadProjectionFailure[]? failures = null,
        int contentExcludedCount = 0)
    {
        Source = source;
        GeneratedAt = new CanonicalTimestamp(generatedAt ?? DateTime.UtcNow);
        Items = (items ?? Array.Empty<CanonicalGeneratedArtifactReadProjectionItem>())
            .OrderBy(i => i.Id, StringComparer.Ordinal).ToArray();
        Failures = UniqueFailures(failures ?? Array.Empty<CanonicalGeneratedArtifactReadProjectionFailure>());
        ContentExcludedCount = Math.Max(0, contentExcludedCount);
    }

    private static CanonicalGeneratedArtifactReadProjectionFailure[] UniqueFailures(
        CanonicalGeneratedArtifactReadProjectionFailure[] failures) =>
        failures.GroupBy(f => f.Id).Select(g => g.First())
            .OrderBy(f => f.Id, StringComparer.Ordinal).ToArray();

    public virtual bool Equals(CanonicalGeneratedArtifactReadSnapshot? other) =>
        other is not null && Source == other.Source;
    public override int GetHashCode() => Source.GetHashCode();
}

public static class CanonicalGeneratedArtifactReadProjection
{
    public static CanonicalGeneratedArtifactReadSnapshot Snapshot(
        CanonicalGeneratedArtifactReadProjectionSource source,
        CanonicalManifest? localManifest,
        CanonicalManifest? peerManifest,
        DateTime? generatedAt = null)
    {
        var facts = new List<CanonicalGeneratedArtifactReadProjectionArtifactFact>();
        var failures = new List<CanonicalGeneratedArtifactReadProjectionFailure>();

        if (localManifest == null && peerManifest == null)
            failures.Add(new CanonicalGeneratedArtifactReadProjectionFailure(
                CanonicalGeneratedArtifactReadProjectionFailureKind.snapshotMissing, source,
                reason: "generatedArtifactReadProjectionSnapshotMissing"));

        if (localManifest != null)
            AppendFacts(localManifest, false, facts, source);
        if (peerManifest != null)
            AppendFacts(peerManifest, true, facts, source);

        return Snapshot(source, facts.ToArray(), failures.ToArray(), generatedAt);
    }

    public static CanonicalGeneratedArtifactReadSnapshot Snapshot(
        CanonicalGeneratedArtifactReadProjectionSource source,
        CanonicalGeneratedArtifactReadProjectionArtifactFact[] facts,
        CanonicalGeneratedArtifactReadProjectionFailure[]? seedFailures = null,
        DateTime? generatedAt = null)
    {
        var failures = new List<CanonicalGeneratedArtifactReadProjectionFailure>(seedFailures ?? Array.Empty<CanonicalGeneratedArtifactReadProjectionFailure>());
        var items = new List<CanonicalGeneratedArtifactReadProjectionItem>();

        foreach (var fact in facts)
        {
            var artifact = fact.Artifact;
            if (artifact.Kind == CanonicalArtifact.Kind.audio)
            {
                failures.Add(new CanonicalGeneratedArtifactReadProjectionFailure(
                    CanonicalGeneratedArtifactReadProjectionFailureKind.audioConfusionRisk, source,
                    artifact.ObjectID, artifact.ArtifactID, artifact.Kind,
                    "audioArtifactExcludedFromGeneratedArtifactReadProjection"));
                continue;
            }
            if (!CanonicalProjectionContract.GeneratedArtifactKinds.Contains(artifact.Kind))
            {
                failures.Add(new CanonicalGeneratedArtifactReadProjectionFailure(
                    CanonicalGeneratedArtifactReadProjectionFailureKind.unsupportedArtifactKind, source,
                    artifact.ObjectID, artifact.ArtifactID, artifact.Kind, "unsupportedArtifactKind"));
                continue;
            }
            if (fact.UnsafePathTokenObserved)
                failures.Add(new CanonicalGeneratedArtifactReadProjectionFailure(
                    CanonicalGeneratedArtifactReadProjectionFailureKind.unsafePathToken, source,
                    artifact.ObjectID, artifact.ArtifactID, artifact.Kind, "unsafePathTokenObserved"));
            if (fact.ParentTombstoned && artifact.AvailabilityValue != CanonicalArtifact.Availability.missing && !artifact.Tombstone)
                failures.Add(new CanonicalGeneratedArtifactReadProjectionFailure(
                    CanonicalGeneratedArtifactReadProjectionFailureKind.tombstonedParentResurrectionRisk, source,
                    artifact.ObjectID, artifact.ArtifactID, artifact.Kind, "availableArtifactUnderTombstonedParent"));

            items.Add(new CanonicalGeneratedArtifactReadProjectionItem(source, fact));
        }

        if (items.Any(i => i.ContentIncluded))
            failures.Add(new CanonicalGeneratedArtifactReadProjectionFailure(
                CanonicalGeneratedArtifactReadProjectionFailureKind.contentLeakRisk, source,
                reason: "contentIncludedInGeneratedArtifactReadProjection"));

        return new CanonicalGeneratedArtifactReadSnapshot(source, generatedAt, items.ToArray(), failures.ToArray(), items.Count);
    }

    private static void AppendFacts(CanonicalManifest manifest, bool peerAuthoritative,
        List<CanonicalGeneratedArtifactReadProjectionArtifactFact> facts,
        CanonicalGeneratedArtifactReadProjectionSource source)
    {
        foreach (var obj in manifest.Objects)
        {
            foreach (var artifact in obj.Artifacts)
            {
                var localAvailability = !peerAuthoritative
                    && CanonicalProjectionContract.ProvesGeneratedArtifactAvailability(artifact);
                var peerAvailability = peerAuthoritative
                    && CanonicalProjectionContract.IsAuthoritativeProducer(artifact, manifest.Node);

                facts.Add(new CanonicalGeneratedArtifactReadProjectionArtifactFact(
                    artifact,
                    obj.Metadata?.IsDeleted == true || obj.SyncState == CanonicalSyncState.deleted,
                    localAvailability,
                    peerAvailability,
                    artifact.ProducedBy?.ToString() ?? manifest.Node.Platform,
                    false));
            }
        }
    }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactReadSideDivergenceKind
{
    missingCanonical,
    missingLegacy,
    availabilityMismatch,
    byteSizeMismatch,
    hashPrefixMismatch,
    producerMismatch,
    artifactKindMismatch,
    logicalTokenMismatch,
    localDownloadedStateMismatch,
    peerAuthoritativeStateMismatch,
    parentStateMismatch,
    unsafePathToken,
    contentLeakRisk,
    unsupportedArtifactKind,
    tombstonedParentResurrectionRisk,
    audioConfusionRisk,
    unknownButRequired
}

public sealed record CanonicalGeneratedArtifactReadSideDivergence : IEquatable<CanonicalGeneratedArtifactReadSideDivergence>
{
    public string Id => string.Join("|", Kind.ToString(), ObjectID ?? "", ArtifactID ?? "", ArtifactKind?.ToString() ?? "");

    public CanonicalGeneratedArtifactReadSideDivergenceKind Kind { get; }
    public string? ObjectID { get; }
    public string? ArtifactID { get; }
    public CanonicalArtifact.Kind? ArtifactKind { get; }
    public string? LegacyValue { get; }
    public string? CanonicalValue { get; }
    public bool Fatal { get; }

    public CanonicalGeneratedArtifactReadSideDivergence(
        CanonicalGeneratedArtifactReadSideDivergenceKind kind,
        string? objectID = null, string? artifactID = null,
        CanonicalArtifact.Kind? artifactKind = null,
        string? legacyValue = null, string? canonicalValue = null,
        bool fatal = false)
    {
        Kind = kind;
        ObjectID = objectID != null ? CanonicalProductionRedaction.SafeIdentifier(objectID, "unknown-recording") : null;
        ArtifactID = artifactID != null ? CanonicalProductionRedaction.SafeIdentifier(artifactID, "artifact:unknown") : null;
        ArtifactKind = artifactKind;
        LegacyValue = legacyValue != null ? CanonicalProductionRedaction.SafeDiagnosticText(legacyValue) : null;
        CanonicalValue = canonicalValue != null ? CanonicalProductionRedaction.SafeDiagnosticText(canonicalValue) : null;
        Fatal = fatal;
    }

    public virtual bool Equals(CanonicalGeneratedArtifactReadSideDivergence? other) =>
        other is not null && Id == other.Id;
    public override int GetHashCode() => Id.GetHashCode();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactReadSideBlocker
{
    missingLegacySnapshot,
    missingCanonicalSnapshot,
    blockingDivergence,
    unsafePathToken,
    contentLeakRisk,
    unsupportedArtifactKind,
    tombstonedParentResurrectionRisk,
    audioConfusionRisk
}

public sealed record CanonicalGeneratedArtifactReadSideDiffReport : IEquatable<CanonicalGeneratedArtifactReadSideDiffReport>
{
    public bool Equivalent { get; }
    public int DivergenceCount { get; }
    public int FatalDivergenceCount { get; }
    public CanonicalGeneratedArtifactReadSideDivergence[] Divergences { get; }
    public CanonicalGeneratedArtifactReadSideBlocker[] Blockers { get; }
    public string DiagnosticsSummary { get; }

    public bool HasFatalBlocker => FatalDivergenceCount > 0 || Blockers.Any(b =>
        b == CanonicalGeneratedArtifactReadSideBlocker.unsafePathToken
        || b == CanonicalGeneratedArtifactReadSideBlocker.contentLeakRisk
        || b == CanonicalGeneratedArtifactReadSideBlocker.unsupportedArtifactKind
        || b == CanonicalGeneratedArtifactReadSideBlocker.tombstonedParentResurrectionRisk
        || b == CanonicalGeneratedArtifactReadSideBlocker.audioConfusionRisk);

    public int UnsupportedArtifactCount => Divergences.Count(d => d.Kind == CanonicalGeneratedArtifactReadSideDivergenceKind.unsupportedArtifactKind);
    public int UnsafePathTokenCount => Divergences.Count(d => d.Kind == CanonicalGeneratedArtifactReadSideDivergenceKind.unsafePathToken);
    public int ContentLeakRiskCount => Divergences.Count(d => d.Kind == CanonicalGeneratedArtifactReadSideDivergenceKind.contentLeakRisk);
    public int ParentTombstoneBlockCount => Divergences.Count(d =>
        d.Kind == CanonicalGeneratedArtifactReadSideDivergenceKind.tombstonedParentResurrectionRisk
        || d.Kind == CanonicalGeneratedArtifactReadSideDivergenceKind.parentStateMismatch);
    public int AudioConfusionRiskCount => Divergences.Count(d => d.Kind == CanonicalGeneratedArtifactReadSideDivergenceKind.audioConfusionRisk);

    public CanonicalGeneratedArtifactReadSideDiffReport(
        CanonicalGeneratedArtifactReadSideDivergence[] divergences,
        CanonicalGeneratedArtifactReadSideBlocker[] blockers)
    {
        var uniqueDivergences = divergences.GroupBy(d => d.Id).Select(g => g.First())
            .OrderBy(d => d.Id, StringComparer.Ordinal).ToArray();
        var uniqueBlockers = new HashSet<CanonicalGeneratedArtifactReadSideBlocker>(blockers)
            .OrderBy(b => b.ToString(), StringComparer.Ordinal).ToArray();
        Equivalent = uniqueDivergences.Length == 0 && uniqueBlockers.Length == 0;
        DivergenceCount = uniqueDivergences.Length;
        FatalDivergenceCount = uniqueDivergences.Count(d => d.Fatal);
        Divergences = uniqueDivergences;
        Blockers = uniqueBlockers;
        DiagnosticsSummary = string.Join(",",
            "domain=generatedArtifacts",
            $"equivalent={Equivalent}",
            $"divergences={DivergenceCount}",
            $"fatal={FatalDivergenceCount}",
            $"blockers={string.Join("+", uniqueBlockers.Select(b => b.ToString()))}"
        );
    }

    public virtual bool Equals(CanonicalGeneratedArtifactReadSideDiffReport? other) =>
        other is not null && Equivalent == other.Equivalent;
    public override int GetHashCode() => Equivalent.GetHashCode();
}

public static class CanonicalGeneratedArtifactReadSideParallelDiff
{
    public static CanonicalGeneratedArtifactReadSideDiffReport Compare(
        CanonicalGeneratedArtifactReadSnapshot? legacy,
        CanonicalGeneratedArtifactReadSnapshot? canonical)
    {
        var divergences = new List<CanonicalGeneratedArtifactReadSideDivergence>();
        var blockers = new List<CanonicalGeneratedArtifactReadSideBlocker>();

        if (legacy == null)
            return new CanonicalGeneratedArtifactReadSideDiffReport(
                Array.Empty<CanonicalGeneratedArtifactReadSideDivergence>(),
                new[] { CanonicalGeneratedArtifactReadSideBlocker.missingLegacySnapshot });
        if (canonical == null)
            return new CanonicalGeneratedArtifactReadSideDiffReport(
                Array.Empty<CanonicalGeneratedArtifactReadSideDivergence>(),
                new[] { CanonicalGeneratedArtifactReadSideBlocker.missingCanonicalSnapshot });

        AppendFailureDivergences(legacy.Failures.Concat(canonical.Failures).ToArray(), divergences, blockers);

        var legacyByKey = legacy.Items.ToDictionary(i => i.Id);
        var canonicalByKey = canonical.Items.ToDictionary(i => i.Id);
        var keys = legacyByKey.Keys.Union(canonicalByKey.Keys).OrderBy(k => k, StringComparer.Ordinal);

        foreach (var key in keys)
        {
            if (!legacyByKey.TryGetValue(key, out var legacyItem))
            {
                var canonicalItem = canonicalByKey.GetValueOrDefault(key);
                divergences.Add(new CanonicalGeneratedArtifactReadSideDivergence(
                    CanonicalGeneratedArtifactReadSideDivergenceKind.missingLegacy,
                    canonicalItem?.ObjectID, canonicalItem?.ArtifactID,
                    canonicalItem?.ArtifactKind, canonicalValue: "present"));
                continue;
            }
            if (!canonicalByKey.TryGetValue(key, out var canonicalItem))
            {
                divergences.Add(new CanonicalGeneratedArtifactReadSideDivergence(
                    CanonicalGeneratedArtifactReadSideDivergenceKind.missingCanonical,
                    legacyItem.ObjectID, legacyItem.ArtifactID,
                    legacyItem.ArtifactKind, legacyValue: "present"));
                continue;
            }
            Compare(legacyItem, canonicalItem, divergences);
        }

        if (divergences.Count > 0) blockers.Add(CanonicalGeneratedArtifactReadSideBlocker.blockingDivergence);
        return new CanonicalGeneratedArtifactReadSideDiffReport(divergences.ToArray(), blockers.ToArray());
    }

    private static void Compare(CanonicalGeneratedArtifactReadProjectionItem legacy,
        CanonicalGeneratedArtifactReadProjectionItem canonical,
        List<CanonicalGeneratedArtifactReadSideDivergence> divergences)
    {
        if (legacy.ArtifactKind != canonical.ArtifactKind)
            divergences.Add(new CanonicalGeneratedArtifactReadSideDivergence(
                CanonicalGeneratedArtifactReadSideDivergenceKind.artifactKindMismatch,
                legacy.ObjectID, legacy.ArtifactID, legacy.ArtifactKind,
                legacy.ArtifactKind.ToString(), canonical.ArtifactKind.ToString()));
        if (legacy.Availability != canonical.Availability)
            divergences.Add(new CanonicalGeneratedArtifactReadSideDivergence(
                CanonicalGeneratedArtifactReadSideDivergenceKind.availabilityMismatch,
                legacy.ObjectID, legacy.ArtifactID, legacy.ArtifactKind,
                legacy.Availability.ToString(), canonical.Availability.ToString()));
        if (legacy.ByteSize != canonical.ByteSize)
            divergences.Add(new CanonicalGeneratedArtifactReadSideDivergence(
                CanonicalGeneratedArtifactReadSideDivergenceKind.byteSizeMismatch,
                legacy.ObjectID, legacy.ArtifactID, legacy.ArtifactKind,
                legacy.ByteSize?.ToString(), canonical.ByteSize?.ToString()));
        if (legacy.HashPrefix != null && canonical.HashPrefix != null && legacy.HashPrefix != canonical.HashPrefix)
            divergences.Add(new CanonicalGeneratedArtifactReadSideDivergence(
                CanonicalGeneratedArtifactReadSideDivergenceKind.hashPrefixMismatch,
                legacy.ObjectID, legacy.ArtifactID, legacy.ArtifactKind,
                legacy.HashPrefix, canonical.HashPrefix));
        if (legacy.ProducerSummary != canonical.ProducerSummary)
            divergences.Add(new CanonicalGeneratedArtifactReadSideDivergence(
                CanonicalGeneratedArtifactReadSideDivergenceKind.producerMismatch,
                legacy.ObjectID, legacy.ArtifactID, legacy.ArtifactKind,
                legacy.ProducerSummary, canonical.ProducerSummary));
        if (legacy.LogicalTokenSummary != canonical.LogicalTokenSummary)
            divergences.Add(new CanonicalGeneratedArtifactReadSideDivergence(
                CanonicalGeneratedArtifactReadSideDivergenceKind.logicalTokenMismatch,
                legacy.ObjectID, legacy.ArtifactID, legacy.ArtifactKind,
                legacy.LogicalTokenSummary, canonical.LogicalTokenSummary));
        if (legacy.LocalAvailability != canonical.LocalAvailability)
            divergences.Add(new CanonicalGeneratedArtifactReadSideDivergence(
                CanonicalGeneratedArtifactReadSideDivergenceKind.localDownloadedStateMismatch,
                legacy.ObjectID, legacy.ArtifactID, legacy.ArtifactKind,
                legacy.LocalDownloadedState, canonical.LocalDownloadedState));
        if (legacy.PeerAuthoritativeAvailability != canonical.PeerAuthoritativeAvailability)
            divergences.Add(new CanonicalGeneratedArtifactReadSideDivergence(
                CanonicalGeneratedArtifactReadSideDivergenceKind.peerAuthoritativeStateMismatch,
                legacy.ObjectID, legacy.ArtifactID, legacy.ArtifactKind,
                legacy.PeerAuthoritativeState, canonical.PeerAuthoritativeState));
        if (legacy.ParentTombstoned != canonical.ParentTombstoned)
            divergences.Add(new CanonicalGeneratedArtifactReadSideDivergence(
                CanonicalGeneratedArtifactReadSideDivergenceKind.parentStateMismatch,
                legacy.ObjectID, legacy.ArtifactID, legacy.ArtifactKind,
                legacy.ParentObjectStateSummary, canonical.ParentObjectStateSummary));
    }

    private static void AppendFailureDivergences(
        CanonicalGeneratedArtifactReadProjectionFailure[] failures,
        List<CanonicalGeneratedArtifactReadSideDivergence> divergences,
        List<CanonicalGeneratedArtifactReadSideBlocker> blockers)
    {
        foreach (var failure in failures)
        {
            switch (failure.Kind)
            {
                case CanonicalGeneratedArtifactReadProjectionFailureKind.snapshotMissing:
                    continue;
                case CanonicalGeneratedArtifactReadProjectionFailureKind.unsupportedArtifactKind:
                    blockers.Add(CanonicalGeneratedArtifactReadSideBlocker.unsupportedArtifactKind);
                    divergences.Add(FailureDivergence(CanonicalGeneratedArtifactReadSideDivergenceKind.unsupportedArtifactKind, failure, true));
                    break;
                case CanonicalGeneratedArtifactReadProjectionFailureKind.unsafePathToken:
                    blockers.Add(CanonicalGeneratedArtifactReadSideBlocker.unsafePathToken);
                    divergences.Add(FailureDivergence(CanonicalGeneratedArtifactReadSideDivergenceKind.unsafePathToken, failure, true));
                    break;
                case CanonicalGeneratedArtifactReadProjectionFailureKind.contentLeakRisk:
                    blockers.Add(CanonicalGeneratedArtifactReadSideBlocker.contentLeakRisk);
                    divergences.Add(FailureDivergence(CanonicalGeneratedArtifactReadSideDivergenceKind.contentLeakRisk, failure, true));
                    break;
                case CanonicalGeneratedArtifactReadProjectionFailureKind.audioConfusionRisk:
                    blockers.Add(CanonicalGeneratedArtifactReadSideBlocker.audioConfusionRisk);
                    divergences.Add(FailureDivergence(CanonicalGeneratedArtifactReadSideDivergenceKind.audioConfusionRisk, failure, true));
                    break;
                case CanonicalGeneratedArtifactReadProjectionFailureKind.tombstonedParentResurrectionRisk:
                    blockers.Add(CanonicalGeneratedArtifactReadSideBlocker.tombstonedParentResurrectionRisk);
                    divergences.Add(FailureDivergence(CanonicalGeneratedArtifactReadSideDivergenceKind.tombstonedParentResurrectionRisk, failure, true));
                    break;
            }
        }
    }

    private static CanonicalGeneratedArtifactReadSideDivergence FailureDivergence(
        CanonicalGeneratedArtifactReadSideDivergenceKind kind,
        CanonicalGeneratedArtifactReadProjectionFailure failure,
        bool fatal) =>
        new(kind, failure.ObjectID, failure.ArtifactID, failure.ArtifactKind,
            failure.Source == CanonicalGeneratedArtifactReadProjectionSource.legacy ? failure.Reason : null,
            failure.Source == CanonicalGeneratedArtifactReadProjectionSource.canonical ? failure.Reason : null,
            fatal);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactReadSideMode
{
    disabled,
    parallelOnly
}

public sealed record CanonicalGeneratedArtifactReadSidePolicy : IEquatable<CanonicalGeneratedArtifactReadSidePolicy>
{
    public bool RecordDiagnostics { get; }
    public int MaxDiagnosticsEvents { get; }

    public CanonicalGeneratedArtifactReadSidePolicy(bool recordDiagnostics = true, int maxDiagnosticsEvents = 200)
    {
        RecordDiagnostics = recordDiagnostics;
        MaxDiagnosticsEvents = Math.Max(1, maxDiagnosticsEvents);
    }

    public virtual bool Equals(CanonicalGeneratedArtifactReadSidePolicy? other) =>
        other is not null && RecordDiagnostics == other.RecordDiagnostics;
    public override int GetHashCode() => RecordDiagnostics.GetHashCode();
}

public sealed record CanonicalGeneratedArtifactReadSideConfiguration : IEquatable<CanonicalGeneratedArtifactReadSideConfiguration>
{
    public bool IsEnabled { get; }
    public CanonicalGeneratedArtifactReadSideMode Mode { get; }
    public CanonicalGeneratedArtifactReadSidePolicy Policy { get; }

    public CanonicalGeneratedArtifactReadSideConfiguration(
        bool isEnabled = false,
        CanonicalGeneratedArtifactReadSideMode mode = CanonicalGeneratedArtifactReadSideMode.disabled,
        CanonicalGeneratedArtifactReadSidePolicy? policy = null)
    {
        IsEnabled = isEnabled;
        Mode = isEnabled ? mode : CanonicalGeneratedArtifactReadSideMode.disabled;
        Policy = policy ?? new CanonicalGeneratedArtifactReadSidePolicy();
    }

    public static readonly CanonicalGeneratedArtifactReadSideConfiguration Disabled = new();

    public static CanonicalGeneratedArtifactReadSideConfiguration Enabled(
        CanonicalGeneratedArtifactReadSideMode mode = CanonicalGeneratedArtifactReadSideMode.parallelOnly,
        CanonicalGeneratedArtifactReadSidePolicy? policy = null) =>
        new(true, mode, policy);

    public virtual bool Equals(CanonicalGeneratedArtifactReadSideConfiguration? other) =>
        other is not null && IsEnabled == other.IsEnabled;
    public override int GetHashCode() => IsEnabled.GetHashCode();
}

public sealed record CanonicalGeneratedArtifactWriteSideEvidenceLinkage : IEquatable<CanonicalGeneratedArtifactWriteSideEvidenceLinkage>
{
    public CanonicalGeneratedArtifactStageEvidenceStatus CanaryStageStatus { get; }
    public CanonicalGeneratedArtifactCanaryStage? LatestSuccessfulStage { get; }
    public int SuccessfulCommitCount { get; }
    public int RollbackCount { get; }
    public int RollbackFailureCount { get; }
    public int LegacyFallbackCount { get; }
    public int DuplicateSuppressionCount { get; }
    public int UnresolvedConflictCount { get; }
    public int UnsupportedArtifactCount { get; }
    public int ContentLeakRiskCount { get; }
    public int UnsafePathTokenCount { get; }
    public int ParentTombstoneBlockCount { get; }
    public int AudioConfusionRiskCount { get; }
    public int ReadSideDivergenceCount { get; }
    public bool WriteSideDomainCutoverComplete { get; }
    public bool RuntimeSwitchEnabled { get; }
    public bool GeneratedArtifactUploadJobCreated { get; }
    public bool AudioAutoDownloaded { get; }

    public CanonicalGeneratedArtifactWriteSideEvidenceLinkage(
        CanonicalGeneratedArtifactStageEvidenceStatus canaryStageStatus = CanonicalGeneratedArtifactStageEvidenceStatus.missing,
        CanonicalGeneratedArtifactCanaryStage? latestSuccessfulStage = null,
        int successfulCommitCount = 0, int rollbackCount = 0, int rollbackFailureCount = 0,
        int legacyFallbackCount = 0, int duplicateSuppressionCount = 0, int unresolvedConflictCount = 0,
        int unsupportedArtifactCount = 0, int contentLeakRiskCount = 0, int unsafePathTokenCount = 0,
        int parentTombstoneBlockCount = 0, int audioConfusionRiskCount = 0, int readSideDivergenceCount = 0,
        bool writeSideDomainCutoverComplete = false, bool runtimeSwitchEnabled = false,
        bool generatedArtifactUploadJobCreated = false, bool audioAutoDownloaded = false)
    {
        CanaryStageStatus = canaryStageStatus;
        LatestSuccessfulStage = latestSuccessfulStage;
        SuccessfulCommitCount = Math.Max(0, successfulCommitCount);
        RollbackCount = Math.Max(0, rollbackCount);
        RollbackFailureCount = Math.Max(0, rollbackFailureCount);
        LegacyFallbackCount = Math.Max(0, legacyFallbackCount);
        DuplicateSuppressionCount = Math.Max(0, duplicateSuppressionCount);
        UnresolvedConflictCount = Math.Max(0, unresolvedConflictCount);
        UnsupportedArtifactCount = Math.Max(0, unsupportedArtifactCount);
        ContentLeakRiskCount = Math.Max(0, contentLeakRiskCount);
        UnsafePathTokenCount = Math.Max(0, unsafePathTokenCount);
        ParentTombstoneBlockCount = Math.Max(0, parentTombstoneBlockCount);
        AudioConfusionRiskCount = Math.Max(0, audioConfusionRiskCount);
        ReadSideDivergenceCount = Math.Max(0, readSideDivergenceCount);
        WriteSideDomainCutoverComplete = writeSideDomainCutoverComplete;
        RuntimeSwitchEnabled = runtimeSwitchEnabled;
        GeneratedArtifactUploadJobCreated = generatedArtifactUploadJobCreated;
        AudioAutoDownloaded = audioAutoDownloaded;
    }

    public static readonly CanonicalGeneratedArtifactWriteSideEvidenceLinkage Missing = new();

    public bool HasCleanStagedCanaryEvidence =>
        CanaryStageStatus.IsPassing() && LatestSuccessfulStage != null
        && SuccessfulCommitCount > 0 && RollbackFailureCount == 0
        && UnresolvedConflictCount == 0 && UnsupportedArtifactCount == 0
        && ContentLeakRiskCount == 0 && UnsafePathTokenCount == 0
        && ParentTombstoneBlockCount == 0 && AudioConfusionRiskCount == 0
        && ReadSideDivergenceCount == 0 && !RuntimeSwitchEnabled
        && !GeneratedArtifactUploadJobCreated && !AudioAutoDownloaded;

    public string DiagnosticsSummary => string.Join(",",
        $"stageStatus={CanaryStageStatus}",
        $"latestStage={LatestSuccessfulStage?.ToString() ?? "none"}",
        $"success={SuccessfulCommitCount}",
        $"rollbackFailure={RollbackFailureCount}",
        $"duplicateSuppression={DuplicateSuppressionCount}",
        $"unresolvedConflict={UnresolvedConflictCount}",
        $"contentLeakRisk={ContentLeakRiskCount}",
        $"unsafePathToken={UnsafePathTokenCount}",
        $"parentTombstone={ParentTombstoneBlockCount}",
        $"audioConfusion={AudioConfusionRiskCount}",
        $"readDivergence={ReadSideDivergenceCount}",
        $"domainCutover={WriteSideDomainCutoverComplete}",
        $"runtimeSwitch={RuntimeSwitchEnabled}",
        $"artifactUploadJob={GeneratedArtifactUploadJobCreated}",
        $"audioAutoDownloaded={AudioAutoDownloaded}"
    );

    public virtual bool Equals(CanonicalGeneratedArtifactWriteSideEvidenceLinkage? other) =>
        other is not null && CanaryStageStatus == other.CanaryStageStatus;
    public override int GetHashCode() => CanaryStageStatus.GetHashCode();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactReadSourceMode
{
    legacy,
    parallelCompare,
    canonicalCandidate,
    guardedCanonicalRead,
    blocked
}

public sealed record CanonicalGeneratedArtifactReadSourceConfiguration : IEquatable<CanonicalGeneratedArtifactReadSourceConfiguration>
{
    public CanonicalGeneratedArtifactReadSourceMode Mode { get; }
    public bool ExplicitInternalTestConfiguration { get; }
    public bool UiCutoverGlobal { get; }
    public bool RuntimeSwitchEnabled { get; }
    public bool DefaultReadCutoverEnabled { get; }
    public bool RecordDiagnostics { get; }
    public int MaxDiagnosticsEvents { get; }

    public CanonicalGeneratedArtifactReadSourceConfiguration(
        CanonicalGeneratedArtifactReadSourceMode mode = CanonicalGeneratedArtifactReadSourceMode.legacy,
        bool explicitInternalTestConfiguration = false, bool uiCutoverGlobal = false,
        bool runtimeSwitchEnabled = false, bool defaultReadCutoverEnabled = false,
        bool recordDiagnostics = true, int maxDiagnosticsEvents = 48)
    {
        Mode = mode;
        ExplicitInternalTestConfiguration = explicitInternalTestConfiguration;
        UiCutoverGlobal = uiCutoverGlobal;
        RuntimeSwitchEnabled = runtimeSwitchEnabled;
        DefaultReadCutoverEnabled = defaultReadCutoverEnabled;
        RecordDiagnostics = recordDiagnostics;
        MaxDiagnosticsEvents = Math.Max(0, maxDiagnosticsEvents);
    }

    public static readonly CanonicalGeneratedArtifactReadSourceConfiguration Legacy = new();
    public static CanonicalGeneratedArtifactReadSourceConfiguration ExplicitGuardedCanonicalRead() =>
        new(CanonicalGeneratedArtifactReadSourceMode.guardedCanonicalRead, true);

    public virtual bool Equals(CanonicalGeneratedArtifactReadSourceConfiguration? other) =>
        other is not null && Mode == other.Mode;
    public override int GetHashCode() => Mode.GetHashCode();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CanonicalGeneratedArtifactReadFallback
{
    none,
    legacyDefault,
    gateBlocked,
    canonicalProjectionMissing,
    unsupportedArtifact,
    divergenceDetected,
    unsafePathToken,
    contentLeakRisk,
    parentTombstone,
    audioConfusionRisk,
    canonicalReadException,
    blockedMode
}

public sealed record CanonicalGeneratedArtifactReadSource : IEquatable<CanonicalGeneratedArtifactReadSource>
{
    public CanonicalGeneratedArtifactReadProjectionSource Source { get; }
    public CanonicalGeneratedArtifactReadSnapshot Snapshot { get; }
    public bool MetadataAvailabilityOnly { get; }
    public bool CoversTranscriptArtifactMetadata { get; }
    public bool CoversNoteArtifactMetadata { get; }
    public bool CoversSummaryArtifactMetadata { get; }
    public bool ExcludesFullTranscriptContent { get; }
    public bool ExcludesFullNoteContent { get; }
    public bool ExcludesFullSummaryContent { get; }
    public bool ExcludesProviderResponse { get; }
    public bool ExcludesAudioBytes { get; }
    public bool ExcludesGeneratedArtifactUploadState { get; }

    public CanonicalGeneratedArtifactReadSource(
        CanonicalGeneratedArtifactReadProjectionSource source,
        CanonicalGeneratedArtifactReadSnapshot snapshot)
    {
        Source = source;
        Snapshot = snapshot;
        MetadataAvailabilityOnly = true;
        CoversTranscriptArtifactMetadata = true;
        CoversNoteArtifactMetadata = true;
        CoversSummaryArtifactMetadata = true;
        ExcludesFullTranscriptContent = true;
        ExcludesFullNoteContent = true;
        ExcludesFullSummaryContent = true;
        ExcludesProviderResponse = true;
        ExcludesAudioBytes = true;
        ExcludesGeneratedArtifactUploadState = true;
    }

    public string DiagnosticsSummary => string.Join(",",
        $"source={Source}",
        $"metadataAvailabilityOnly={MetadataAvailabilityOnly}",
        $"items={Snapshot.ItemCount}",
        $"failures={Snapshot.FailureCount}",
        $"excludeTranscript={ExcludesFullTranscriptContent}",
        $"excludeNote={ExcludesFullNoteContent}",
        $"excludeSummary={ExcludesFullSummaryContent}",
        $"excludeProviderResponse={ExcludesProviderResponse}",
        $"excludeAudio={ExcludesAudioBytes}"
    );

    public virtual bool Equals(CanonicalGeneratedArtifactReadSource? other) =>
        other is not null && Source == other.Source;
    public override int GetHashCode() => Source.GetHashCode();
}
