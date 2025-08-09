# EF Core Audit Logging (EN)

A library for EF Core that produces clear, human-readable audit messages for entity and relationship changes.

## Features

- Property changes (Modified)
  - Message: "{DisplayName}: {Old} -> {New}"
  - Display name via [AuditDisplay]
- Collection changes
  - One-to-many (e.g., User.Pets): "Schnuffi (Pet) was added to Pets" / "... was removed from ..."
  - Many-to-many (skip navigations) handled similarly
- Entity title for natural language
  - Via [AuditEntityTitle] (single property) or [AuditEntityTitleTemplate("{Name} ({Id})")] (template with placeholders)
- Entity names (singular/plural) via [AuditEntityDisplay]
- Exclusions via [AuditIgnore]
- Owned types: property changes are logged
- Optional short delete message; join entities are not reported as deleted

## Install & Setup

1) Register services and configure options

```csharp
services.AddEfCoreAuditLogging(opts =>
{
    // Optional: customize templates
    // opts.PropertyChangeTemplate = "{DisplayName}: {Old} -> {New}";
});
```

2) Attach the interceptor to your DbContext

```csharp
services.AddDbContext<AppDbContext>((sp, o) => o
    .UseSqlite("Data Source=sample.db")
    .UseAuditLogging(sp));
```

## Attributes

- [AuditDisplay("Display Name")] — on properties or collection/skip navigations
- [AuditEntityDisplay("Singular", "Plural")] — on entity classes
- [AuditEntityTitle] — marks the title property
- [AuditEntityTitleTemplate("{Name} ({Id})")] — class-level title template with nested paths like {Owner.Name}
- [AuditIgnore] — suppress audit logging for the property

## Options (AuditOptions)

- PropertyChangeTemplate (default: "{DisplayName}: {Old} -> {New}")
- CollectionAddedTemplate (default: "{Title} ({EntitySingular}) wurde zu {CollectionDisplay} hinzugefügt")
- CollectionRemovedTemplate (default: "{Title} ({EntitySingular}) wurde von {CollectionDisplay} entfernt")
- VerboseDelete (default: true)
- IncludeUnchangedMarkedProperties (default: false)

Template tokens:
- {DisplayName}, {Old}, {New}
- {Title}, {EntitySingular}, {CollectionDisplay}

## Value formatting

- null → "∅"
- DateTime → "yyyy-MM-dd HH:mm:ss"
- DateOnly → "yyyy-MM-dd"
- TimeOnly → "HH:mm:ss"
- bool → "Ja"/"Nein" (German by default)
- Enum → ToString()

## Supported changes

- Modified properties with deltas
- One-to-many collection add/remove (via FK + inverse collection)
- Many-to-many add/remove (skip navigations / join entities)
- Owned type property changes
- Optional short delete message (join entities excluded)

## Extensibility

- IAuditEventSink target (default: logger)
- Implement custom sinks (database/file/etc.)

## Tests

Coverage includes property changes, 1:n and n:m, owned types, delete, and formatting for DateTime, DateOnly, TimeOnly, bool, decimal, and enums. All tests pass using xUnit with the InMemory provider.

## Roadmap

- Localization (resource-based, culture-aware formatting)
- Persistent sinks (DB + example migration)
