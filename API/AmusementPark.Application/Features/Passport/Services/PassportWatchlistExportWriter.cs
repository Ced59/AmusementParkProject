using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Passport.Services;

internal static class PassportWatchlistExportWriter
{
    public static IReadOnlyCollection<string> CsvFileNames { get; } = new[]
    {
        "collections.csv",
        "watch-subscriptions.csv",
        "notifications.csv",
        "notification-digests.csv",
        "notification-digest-entries.csv",
        "notification-email-preference.csv",
        "notification-delivery-attempts.csv",
    };

    public static void WriteJson(
        Utf8JsonWriter writer,
        PassportExportWriteRequest request,
        PassportExportReferenceMap references)
    {
        PassportWatchlistExportData data = request.WatchlistLifecycle;
        writer.WriteStartArray("collections");
        foreach (UserCollectionEntry entry in data.CollectionEntries)
        {
            PassportWatchlistTargetSnapshot target = ResolveTarget(data, entry.TargetType, entry.TargetId);
            writer.WriteStartObject();
            writer.WriteString("reference", references.Collection(entry.Id));
            writer.WriteString("kind", entry.Kind.ToString());
            WriteTarget(writer, target);
            WriteNullableString(writer, "privateNote", entry.PrivateNote);
            WriteNullableNumber(writer, "priority", entry.Priority);
            WriteNullableDate(writer, "preferredStartsOn", entry.PreferredPeriod?.StartsOn);
            WriteNullableDate(writer, "preferredEndsOn", entry.PreferredPeriod?.EndsOn);
            writer.WriteNumber("version", entry.Version);
            writer.WriteString("createdAtUtc", FormatUtc(entry.CreatedAtUtc));
            writer.WriteString("updatedAtUtc", FormatUtc(entry.UpdatedAtUtc));
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteStartArray("watchSubscriptions");
        foreach (WatchSubscription subscription in data.Subscriptions)
        {
            PassportWatchlistTargetSnapshot target = ResolveTarget(
                data,
                subscription.TargetType,
                subscription.TargetId);
            writer.WriteStartObject();
            WriteNullableString(
                writer,
                "reference",
                references.SubscriptionOrDefault(subscription.Id));
            WriteTarget(writer, target);
            writer.WriteStartArray("eventTypes");
            foreach (FactualEventType eventType in subscription.EventTypes.OrderBy(static value => value))
            {
                writer.WriteStringValue(eventType.ToString());
            }

            writer.WriteEndArray();
            writer.WriteString("frequency", subscription.Frequency.ToString());
            writer.WriteStartArray("channels");
            foreach (NotificationChannel channel in subscription.Channels.OrderBy(static value => value))
            {
                writer.WriteStringValue(channel.ToString());
            }

            writer.WriteEndArray();
            writer.WriteBoolean("isPaused", subscription.IsPaused);
            writer.WriteNumber("version", subscription.Version);
            writer.WriteString("createdAtUtc", FormatUtc(subscription.CreatedAtUtc));
            writer.WriteString("updatedAtUtc", FormatUtc(subscription.UpdatedAtUtc));
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteStartArray("notifications");
        foreach (UserNotification notification in data.Notifications)
        {
            WriteNotification(writer, notification, data, references);
        }

        writer.WriteEndArray();
        writer.WriteStartArray("notificationDigests");
        foreach (NotificationDigest digest in data.Digests)
        {
            WriteDigest(writer, digest, data, references);
        }

        writer.WriteEndArray();
        writer.WritePropertyName("notificationEmailPreference");
        WriteEmailPreference(writer, data.EmailPreference);
        writer.WriteStartArray("notificationDeliveryAttempts");
        foreach (NotificationDeliveryAttempt attempt in data.DeliveryAttempts)
        {
            writer.WriteStartObject();
            writer.WriteString("reference", references.Delivery(attempt.Id));
            WriteNullableString(writer, "digestReference", references.DigestOrDefault(attempt.DigestId));
            writer.WriteString("status", attempt.Status.ToString());
            writer.WriteNumber("attemptCount", attempt.AttemptCount);
            WriteNullableString(writer, "lastOutcomeCode", attempt.LastErrorCode);
            writer.WriteString("createdAtUtc", FormatUtc(attempt.CreatedAtUtc));
            writer.WriteString("updatedAtUtc", FormatUtc(attempt.UpdatedAtUtc));
            WriteNullableTimestamp(writer, "completedAtUtc", attempt.CompletedAtUtc);
            writer.WriteString("retainedUntilUtc", FormatUtc(attempt.ExpiresAtUtc));
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    public static void WriteCsvEntries(
        ZipArchive archive,
        PassportExportWriteRequest request,
        PassportExportReferenceMap references)
    {
        WriteCollectionsCsv(archive, request, references);
        WriteSubscriptionsCsv(archive, request, references);
        WriteNotificationsCsv(archive, request, references);
        WriteDigestsCsv(archive, request, references);
        WriteDigestEntriesCsv(archive, request, references);
        WriteEmailPreferenceCsv(archive, request);
        WriteDeliveryAttemptsCsv(archive, request, references);
    }

    private static void WriteNotification(
        Utf8JsonWriter writer,
        UserNotification notification,
        PassportWatchlistExportData data,
        PassportExportReferenceMap references)
    {
        PassportWatchlistTargetSnapshot target = ResolveTarget(
            data,
            ToCollectionTargetType(notification.TargetType),
            notification.TargetId);
        data.FactualEvents.TryGetValue(notification.FactualEventId, out FactualChangeEvent? factualEvent);
        writer.WriteStartObject();
        writer.WriteString("reference", references.Notification(notification.Id));
        WriteNullableString(
            writer,
            "subscriptionReference",
            references.SubscriptionOrDefault(notification.SubscriptionId));
        writer.WriteString("eventType", notification.EventType.ToString());
        WriteTarget(writer, target);
        writer.WriteString("status", notification.Status.ToString());
        writer.WriteString("language", notification.Language);
        writer.WriteNumber("sourceRevision", notification.SourceRevision);
        writer.WriteNumber("templateVersion", notification.TemplateVersion);
        writer.WriteString("deliveredAtUtc", FormatUtc(notification.DeliveredAtUtc));
        WriteNullableTimestamp(writer, "readAtUtc", notification.ReadAtUtc);
        WriteNullableTimestamp(writer, "dismissedAtUtc", notification.DismissedAtUtc);
        writer.WriteString("retainedUntilUtc", FormatUtc(notification.ExpiresAtUtc));
        writer.WriteNumber("version", notification.Version);
        writer.WritePropertyName("evidence");
        WriteEvidence(writer, factualEvent);
        writer.WriteEndObject();
    }

    private static void WriteDigest(
        Utf8JsonWriter writer,
        NotificationDigest digest,
        PassportWatchlistExportData data,
        PassportExportReferenceMap references)
    {
        writer.WriteStartObject();
        writer.WriteString("reference", references.Digest(digest.Id));
        writer.WriteString("channel", digest.Channel.ToString());
        writer.WriteString("frequency", digest.Frequency.ToString());
        writer.WriteString("periodStartUtc", FormatUtc(digest.PeriodStartUtc));
        writer.WriteString("periodEndUtc", FormatUtc(digest.PeriodEndUtc));
        writer.WriteNumber("observedNotificationCount", digest.ObservedNotificationCount);
        writer.WriteStartArray("entries");
        foreach (NotificationDigestEntry entry in digest.Entries)
        {
            PassportWatchlistTargetSnapshot target = ResolveTarget(
                data,
                ToCollectionTargetType(entry.TargetType),
                entry.TargetId);
            writer.WriteStartObject();
            WriteNullableString(
                writer,
                "subscriptionReference",
                references.SubscriptionOrDefault(entry.SubscriptionId));
            writer.WriteString("eventType", entry.EventType.ToString());
            WriteTarget(writer, target);
            writer.WriteNumber("sourceRevision", entry.SourceRevision);
            writer.WriteString("sourceStatus", entry.SourceStatus.ToString());
            writer.WriteString("occurredAtUtc", FormatUtc(entry.OccurredAtUtc));
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteString("createdAtUtc", FormatUtc(digest.CreatedAtUtc));
        writer.WriteString("updatedAtUtc", FormatUtc(digest.UpdatedAtUtc));
        writer.WriteEndObject();
    }

    private static void WriteEmailPreference(
        Utf8JsonWriter writer,
        NotificationEmailPreference? preference)
    {
        if (preference is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();
        writer.WriteBoolean("isEnabled", preference.IsEnabled);
        WriteNullableString(writer, "consentTextVersion", preference.ConsentTextVersion);
        WriteNullableString(writer, "consentLocale", preference.ConsentLocale);
        WriteNullableTimestamp(writer, "consentGrantedAtUtc", preference.ConsentGrantedAtUtc);
        WriteNullableTimestamp(writer, "revokedAtUtc", preference.RevokedAtUtc);
        writer.WriteNumber("version", preference.Version);
        writer.WriteString("createdAtUtc", FormatUtc(preference.CreatedAtUtc));
        writer.WriteString("updatedAtUtc", FormatUtc(preference.UpdatedAtUtc));
        writer.WriteEndObject();
    }

    private static void WriteEvidence(Utf8JsonWriter writer, FactualChangeEvent? factualEvent)
    {
        if (factualEvent is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();
        writer.WriteString("status", factualEvent.Status.ToString());
        writer.WriteString("confidence", factualEvent.Confidence.ToString());
        writer.WriteNumber("revision", factualEvent.Revision);
        WriteFactValue(writer, "previousValue", factualEvent.PreviousValue);
        WriteFactValue(writer, "newValue", factualEvent.NewValue);
        writer.WriteStartObject("source");
        writer.WriteString("type", factualEvent.Source.Type.ToString());
        writer.WriteString("publisher", factualEvent.Source.PublisherName);
        writer.WriteString("title", factualEvent.Source.Title);
        writer.WriteString("url", factualEvent.Source.Url);
        writer.WriteString("publishedAtUtc", FormatUtc(factualEvent.Source.PublishedAtUtc));
        writer.WriteEndObject();
        WriteNullableTimestamp(writer, "verifiedAtUtc", factualEvent.VerifiedAtUtc);
        WriteNullableTimestamp(writer, "publishedAtUtc", factualEvent.PublishedAtUtc);
        WriteNullableTimestamp(writer, "terminalAtUtc", factualEvent.TerminalAtUtc);
        WriteNullableString(writer, "reasonCode", factualEvent.ReasonCode);
        writer.WriteEndObject();
    }

    private static void WriteFactValue(
        Utf8JsonWriter writer,
        string propertyName,
        FactValue? value)
    {
        writer.WritePropertyName(propertyName);
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();
        writer.WriteString("kind", value.Kind.ToString());
        writer.WriteString("value", value.CanonicalValue);
        WriteNullableString(writer, "unit", value.UnitCode);
        writer.WriteEndObject();
    }

    private static void WriteCollectionsCsv(
        ZipArchive archive,
        PassportExportWriteRequest request,
        PassportExportReferenceMap references)
    {
        using StreamWriter writer = PassportShareLifecycleExportWriter.CreateCsvWriter(
            archive,
            "collections.csv");
        PassportShareLifecycleExportWriter.WriteCsvRow(writer, new[]
        {
            "reference", "kind", "targetType", "targetName", "parkName", "targetStatus",
            "privateNote", "priority", "preferredStartsOn", "preferredEndsOn", "version",
            "createdAtUtc", "updatedAtUtc",
        });
        foreach (UserCollectionEntry entry in request.WatchlistLifecycle.CollectionEntries)
        {
            PassportWatchlistTargetSnapshot target = ResolveTarget(
                request.WatchlistLifecycle,
                entry.TargetType,
                entry.TargetId);
            PassportShareLifecycleExportWriter.WriteCsvRow(writer, new[]
            {
                references.Collection(entry.Id), entry.Kind.ToString(), target.TargetType.ToString(),
                TargetName(target), target.ParkName, target.Status.ToString(), entry.PrivateNote,
                NullableInteger(entry.Priority), FormatDate(entry.PreferredPeriod?.StartsOn),
                FormatDate(entry.PreferredPeriod?.EndsOn), Integer(entry.Version),
                FormatUtc(entry.CreatedAtUtc), FormatUtc(entry.UpdatedAtUtc),
            });
        }
    }

    private static void WriteSubscriptionsCsv(
        ZipArchive archive,
        PassportExportWriteRequest request,
        PassportExportReferenceMap references)
    {
        using StreamWriter writer = PassportShareLifecycleExportWriter.CreateCsvWriter(
            archive,
            "watch-subscriptions.csv");
        PassportShareLifecycleExportWriter.WriteCsvRow(writer, new[]
        {
            "reference", "targetType", "targetName", "parkName", "targetStatus", "eventTypes",
            "frequency", "channels", "isPaused", "version", "createdAtUtc", "updatedAtUtc",
        });
        foreach (WatchSubscription subscription in request.WatchlistLifecycle.Subscriptions)
        {
            PassportWatchlistTargetSnapshot target = ResolveTarget(
                request.WatchlistLifecycle,
                subscription.TargetType,
                subscription.TargetId);
            PassportShareLifecycleExportWriter.WriteCsvRow(writer, new[]
            {
                references.SubscriptionOrDefault(subscription.Id), target.TargetType.ToString(),
                TargetName(target), target.ParkName, target.Status.ToString(),
                Join(subscription.EventTypes), subscription.Frequency.ToString(),
                Join(subscription.Channels), Boolean(subscription.IsPaused), Integer(subscription.Version),
                FormatUtc(subscription.CreatedAtUtc), FormatUtc(subscription.UpdatedAtUtc),
            });
        }
    }

    private static void WriteNotificationsCsv(
        ZipArchive archive,
        PassportExportWriteRequest request,
        PassportExportReferenceMap references)
    {
        using StreamWriter writer = PassportShareLifecycleExportWriter.CreateCsvWriter(
            archive,
            "notifications.csv");
        PassportShareLifecycleExportWriter.WriteCsvRow(writer, new[]
        {
            "reference", "subscriptionReference", "eventType", "targetType", "targetName",
            "parkName", "targetStatus", "status", "language", "sourceRevision",
            "previousValue", "newValue", "sourcePublisher", "sourceTitle", "sourceUrl",
            "sourcePublishedAtUtc", "deliveredAtUtc", "readAtUtc", "dismissedAtUtc",
            "retainedUntilUtc", "version",
        });
        foreach (UserNotification notification in request.WatchlistLifecycle.Notifications)
        {
            PassportWatchlistTargetSnapshot target = ResolveTarget(
                request.WatchlistLifecycle,
                ToCollectionTargetType(notification.TargetType),
                notification.TargetId);
            request.WatchlistLifecycle.FactualEvents.TryGetValue(
                notification.FactualEventId,
                out FactualChangeEvent? factualEvent);
            PassportShareLifecycleExportWriter.WriteCsvRow(writer, new[]
            {
                references.Notification(notification.Id),
                references.SubscriptionOrDefault(notification.SubscriptionId),
                notification.EventType.ToString(), target.TargetType.ToString(), TargetName(target),
                target.ParkName, target.Status.ToString(), notification.Status.ToString(),
                notification.Language, Integer(notification.SourceRevision), FactValueText(factualEvent?.PreviousValue),
                FactValueText(factualEvent?.NewValue), factualEvent?.Source.PublisherName,
                factualEvent?.Source.Title, factualEvent?.Source.Url,
                NullableTimestamp(factualEvent?.Source.PublishedAtUtc),
                FormatUtc(notification.DeliveredAtUtc), NullableTimestamp(notification.ReadAtUtc),
                NullableTimestamp(notification.DismissedAtUtc), FormatUtc(notification.ExpiresAtUtc),
                Integer(notification.Version),
            });
        }
    }

    private static void WriteDigestsCsv(
        ZipArchive archive,
        PassportExportWriteRequest request,
        PassportExportReferenceMap references)
    {
        using StreamWriter writer = PassportShareLifecycleExportWriter.CreateCsvWriter(
            archive,
            "notification-digests.csv");
        PassportShareLifecycleExportWriter.WriteCsvRow(writer, new[]
        {
            "reference", "channel", "frequency", "periodStartUtc", "periodEndUtc",
            "entryCount", "observedNotificationCount", "createdAtUtc", "updatedAtUtc",
        });
        foreach (NotificationDigest digest in request.WatchlistLifecycle.Digests)
        {
            PassportShareLifecycleExportWriter.WriteCsvRow(writer, new[]
            {
                references.Digest(digest.Id), digest.Channel.ToString(), digest.Frequency.ToString(),
                FormatUtc(digest.PeriodStartUtc), FormatUtc(digest.PeriodEndUtc),
                Integer(digest.Entries.Count), Integer(digest.ObservedNotificationCount),
                FormatUtc(digest.CreatedAtUtc), FormatUtc(digest.UpdatedAtUtc),
            });
        }
    }

    private static void WriteDigestEntriesCsv(
        ZipArchive archive,
        PassportExportWriteRequest request,
        PassportExportReferenceMap references)
    {
        using StreamWriter writer = PassportShareLifecycleExportWriter.CreateCsvWriter(
            archive,
            "notification-digest-entries.csv");
        PassportShareLifecycleExportWriter.WriteCsvRow(writer, new[]
        {
            "digestReference", "subscriptionReference", "eventType", "targetType", "targetName",
            "parkName", "targetStatus", "sourceRevision", "sourceStatus", "occurredAtUtc",
        });
        foreach (NotificationDigest digest in request.WatchlistLifecycle.Digests)
        {
            foreach (NotificationDigestEntry entry in digest.Entries)
            {
                PassportWatchlistTargetSnapshot target = ResolveTarget(
                    request.WatchlistLifecycle,
                    ToCollectionTargetType(entry.TargetType),
                    entry.TargetId);
                PassportShareLifecycleExportWriter.WriteCsvRow(writer, new[]
                {
                    references.Digest(digest.Id), references.SubscriptionOrDefault(entry.SubscriptionId),
                    entry.EventType.ToString(), target.TargetType.ToString(), TargetName(target),
                    target.ParkName, target.Status.ToString(), Integer(entry.SourceRevision),
                    entry.SourceStatus.ToString(), FormatUtc(entry.OccurredAtUtc),
                });
            }
        }
    }

    private static void WriteEmailPreferenceCsv(
        ZipArchive archive,
        PassportExportWriteRequest request)
    {
        using StreamWriter writer = PassportShareLifecycleExportWriter.CreateCsvWriter(
            archive,
            "notification-email-preference.csv");
        PassportShareLifecycleExportWriter.WriteCsvRow(writer, new[]
        {
            "isEnabled", "consentTextVersion", "consentLocale", "consentGrantedAtUtc",
            "revokedAtUtc", "version", "createdAtUtc", "updatedAtUtc",
        });
        NotificationEmailPreference? preference = request.WatchlistLifecycle.EmailPreference;
        if (preference is not null)
        {
            PassportShareLifecycleExportWriter.WriteCsvRow(writer, new[]
            {
                Boolean(preference.IsEnabled), preference.ConsentTextVersion, preference.ConsentLocale,
                NullableTimestamp(preference.ConsentGrantedAtUtc), NullableTimestamp(preference.RevokedAtUtc),
                Integer(preference.Version), FormatUtc(preference.CreatedAtUtc),
                FormatUtc(preference.UpdatedAtUtc),
            });
        }
    }

    private static void WriteDeliveryAttemptsCsv(
        ZipArchive archive,
        PassportExportWriteRequest request,
        PassportExportReferenceMap references)
    {
        using StreamWriter writer = PassportShareLifecycleExportWriter.CreateCsvWriter(
            archive,
            "notification-delivery-attempts.csv");
        PassportShareLifecycleExportWriter.WriteCsvRow(writer, new[]
        {
            "reference", "digestReference", "status", "attemptCount", "lastOutcomeCode",
            "createdAtUtc", "updatedAtUtc", "completedAtUtc", "retainedUntilUtc",
        });
        foreach (NotificationDeliveryAttempt attempt in request.WatchlistLifecycle.DeliveryAttempts)
        {
            PassportShareLifecycleExportWriter.WriteCsvRow(writer, new[]
            {
                references.Delivery(attempt.Id), references.DigestOrDefault(attempt.DigestId),
                attempt.Status.ToString(), Integer(attempt.AttemptCount), attempt.LastErrorCode,
                FormatUtc(attempt.CreatedAtUtc), FormatUtc(attempt.UpdatedAtUtc),
                NullableTimestamp(attempt.CompletedAtUtc), FormatUtc(attempt.ExpiresAtUtc),
            });
        }
    }

    private static PassportWatchlistTargetSnapshot ResolveTarget(
        PassportWatchlistExportData data,
        CollectionTargetType targetType,
        string targetId)
    {
        IReadOnlyDictionary<string, PassportWatchlistTargetSnapshot> targets =
            targetType == CollectionTargetType.Park ? data.ParkTargets : data.ParkItemTargets;
        return targets.GetValueOrDefault(targetId)
            ?? new PassportWatchlistTargetSnapshot(
                targetType,
                CollectionTargetStatus.Unknown,
                null,
                null);
    }

    private static string TargetName(PassportWatchlistTargetSnapshot target)
    {
        return !string.IsNullOrWhiteSpace(target.Name)
            ? target.Name
            : target.TargetType == CollectionTargetType.Park
                ? "Unavailable park"
                : "Unavailable attraction";
    }

    private static void WriteTarget(Utf8JsonWriter writer, PassportWatchlistTargetSnapshot target)
    {
        writer.WriteString("targetType", target.TargetType.ToString());
        writer.WriteString("targetName", TargetName(target));
        WriteNullableString(writer, "parkName", target.ParkName);
        writer.WriteString("targetStatus", target.Status.ToString());
    }

    private static CollectionTargetType ToCollectionTargetType(FactualTargetType targetType)
    {
        return targetType == FactualTargetType.Park
            ? CollectionTargetType.Park
            : CollectionTargetType.ParkItem;
    }

    private static string FactValueText(FactValue? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(value.UnitCode)
            ? value.CanonicalValue
            : $"{value.CanonicalValue} {value.UnitCode}";
    }

    private static string Join<T>(IEnumerable<T> values)
    {
        return string.Join("|", values.OrderBy(static value => value).Select(static value => value?.ToString()));
    }

    private static string FormatUtc(DateTime value)
    {
        return value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }

    private static string? NullableTimestamp(DateTime? value)
    {
        return value.HasValue ? FormatUtc(value.Value) : null;
    }

    private static string FormatDate(DateOnly? value)
    {
        return value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static string Integer(long value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string NullableInteger(int? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static string Boolean(bool value)
    {
        return value ? "true" : "false";
    }

    private static void WriteNullableString(
        Utf8JsonWriter writer,
        string propertyName,
        string? value)
    {
        if (value is null)
        {
            writer.WriteNull(propertyName);
            return;
        }

        writer.WriteString(propertyName, value);
    }

    private static void WriteNullableNumber(
        Utf8JsonWriter writer,
        string propertyName,
        int? value)
    {
        if (value.HasValue)
        {
            writer.WriteNumber(propertyName, value.Value);
            return;
        }

        writer.WriteNull(propertyName);
    }

    private static void WriteNullableDate(
        Utf8JsonWriter writer,
        string propertyName,
        DateOnly? value)
    {
        WriteNullableString(writer, propertyName, FormatDate(value).NullIfEmpty());
    }

    private static void WriteNullableTimestamp(
        Utf8JsonWriter writer,
        string propertyName,
        DateTime? value)
    {
        WriteNullableString(writer, propertyName, NullableTimestamp(value));
    }

    private static string? NullIfEmpty(this string value)
    {
        return value.Length == 0 ? null : value;
    }
}
