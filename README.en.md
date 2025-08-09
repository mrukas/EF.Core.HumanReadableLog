# EF Core Audit Logging

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
// Default: English localizer
services.AddEfCoreAuditLogging(opts =>
{
  // Optional: customize templates (leave empty to use localizer defaults)
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

- PropertyChangeTemplate (default via localizer: "{DisplayName}: {Old} -> {New}")
- CollectionAddedTemplate (default via localizer: "{Title} ({EntitySingular}) was added to {CollectionDisplay}")
- CollectionRemovedTemplate (default via localizer: "{Title} ({EntitySingular}) was removed from {CollectionDisplay}")
- VerboseDelete (default: true)
- IncludeUnchangedMarkedProperties (default: false)

Template tokens:
- {DisplayName}, {Old}, {New}
- {Title}, {EntitySingular}, {CollectionDisplay}

## Localization and value formatting

The library is localization-aware. By default, English is used. You can choose a localizer at registration or per DbContext/options instance.

- Default localizer: English
- Available localizers: English, German

Select a localizer in DI (recommended):

```csharp
// Use German for all DbContexts registered in this container
services.AddEfCoreAuditLogging<GermanAuditLocalizer>();
```

Or set it per options instance:

```csharp
services.AddEfCoreAuditLogging(opts =>
{
  opts.Localizer = new GermanAuditLocalizer();
});
```

Notes:
- If a template in AuditOptions is left empty, the localizer's template is used.
- Value formatting (null symbol and bool strings) are provided by the localizer.

Value formatting (English localizer):
- null → "∅"
- DateTime → "yyyy-MM-dd HH:mm:ss"
- DateOnly → "yyyy-MM-dd"
- TimeOnly → "HH:mm:ss"
- bool → "Yes"/"No"
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

## Examples

### 1) Minimal model and default English output

```csharp
using EF.Core.HumanReadableLog.Attributes;

[AuditEntityDisplay("User", "Users")]
public class User
{
  public int Id { get; set; }
  [AuditDisplay("Name"), AuditEntityTitle]
  public string DisplayName { get; set; } = string.Empty;
  [AuditDisplay("Pets")]
  public List<Pet> Pets { get; set; } = new();
}

[AuditEntityDisplay("Pet", "Pets")]
[AuditEntityTitleTemplate("{Name}")]
public class Pet
{
  public int Id { get; set; }
  [AuditDisplay("Name")]
  public string Name { get; set; } = string.Empty;
}
```

Register with default English localizer and use the interceptor:

```csharp
services.AddEfCoreAuditLogging(); // English by default
services.AddDbContext<AppDbContext>((sp, o) => o.UseSqlite("Data Source=sample.db").UseAuditLogging(sp));
```

Example actions and resulting messages:

- Property change: Name from Max to Moritz
  - Output: `Name: Max -> Moritz`
- Add to collection: Add Schnuffi to Pets
  - Output: `Schnuffi (Pet) was added to Pets`
- Remove from collection: Remove Schnuffi from Pets
  - Output: `Schnuffi (Pet) was removed from Pets`

Boolean and null formatting are localized (English):
- `Yes` / `No` for bools, `∅` for null

### 2) Switching to German

Select German via DI:

```csharp
using EF.Core.HumanReadableLog.Localization;

services.AddEfCoreAuditLogging<GermanAuditLocalizer>();
```

With German display names:

```csharp
[AuditEntityDisplay("Haustier", "Haustiere")]
public class Pet { /* ... */ }

public class User
{
  // ...
  [AuditDisplay("Haustiere")] // collection label
  public List<Pet> Pets { get; set; } = new();
}
```

Resulting messages (German):

- `Name: Max -> Moritz`
- `Schnuffi (Haustier) wurde zu Haustiere hinzugefügt`
- `Schnuffi (Haustier) wurde von Haustiere entfernt`
- Bools: `Ja` / `Nein`

### 3) Per-options override and custom template

```csharp
services.AddEfCoreAuditLogging(opts =>
{
  opts.Localizer = new GermanAuditLocalizer();
  // Custom property template (applies regardless of selected localizer)
  opts.PropertyChangeTemplate = "Changed: {DisplayName} from {Old} to {New}";
});
```

Property change output:
- `Changed: Name from Max to Moritz`

Tokens you can use:
- Property changes: `{DisplayName}`, `{Old}`, `{New}`
- Collection changes: `{Title}`, `{EntitySingular}`, `{CollectionDisplay}`
