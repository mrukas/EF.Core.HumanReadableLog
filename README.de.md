# EF Core Audit Logging (DE)

Bibliothek für EF Core, die Änderungen an Entities in klaren, anpassbaren Nachrichten protokolliert.

## Features

- Property-Änderungen (Modified):
    - Meldung: „{DisplayName}: {Old} -> {New}“
    - Anzeigename per [AuditDisplay]
- Sammlungsänderungen:
    - 1:n (z. B. User.Pets): „Schnuffi (Haustier) wurde zu Haustiere hinzugefügt/entfernt“
    - n:m (Skip Navigations): wird ebenfalls erkannt und als obige Nachricht protokolliert
- Entitätstitel für natürliche Sprache:
    - Per [AuditEntityTitle] (eine Property) oder [AuditEntityTitleTemplate("{Name} ({Id})")] (Template mit Platzhaltern)
- Entitätsnamen (Singular/Plural) via [AuditEntityDisplay]
- Ausnahmen/Ignore via [AuditIgnore]
- Owned Types: Property-Änderungen werden protokolliert
- Delete-Kurzmeldung optional, Join-Entities werden nicht als „gelöscht“ gemeldet

## Installation & Setup

1) DI-Registrierung und Optionen:

```csharp
services.AddEfCoreAuditLogging(opts =>
{
        // Optional: Templates anpassen
        // opts.PropertyChangeTemplate = "{DisplayName}: {Old} -> {New}";
});
```

2) DbContext-Interceptor aktivieren:

```csharp
services.AddDbContext<AppDbContext>((sp, o) => o
        .UseSqlite("Data Source=sample.db")
        .UseAuditLogging(sp));
```

## Attribute-Referenz

- [AuditDisplay("Anzeigename")]
    - Auf Properties oder Sammlungen (Skip/Collection Navigations) nutzbar
    - Steuert den Namen in Meldungen, z. B. „Haustiere“ statt „Pets“

- [AuditEntityDisplay("Singular", "Plural")]
    - Auf Entitätsklassen
    - Wird in Sammlungs-Meldungen als „({EntitySingular})" genutzt

- [AuditEntityTitle]
    - Markiert die Titel-Property, z. B. „Name“

- [AuditEntityTitleTemplate("{Name} ({Id})")]
    - Auf Entitätsklassen
    - Template-Platzhalter in {}:
        - Support für verschachtelte Pfade: {Owner.Name}
        - Fallback auf [AuditEntityTitle], dann Klassenname

- [AuditIgnore]
    - Unterdrückt das Logging dieser Property

## Optionen (AuditOptions)

- PropertyChangeTemplate (Default: "{DisplayName}: {Old} -> {New}")
- CollectionAddedTemplate (Default: "{Title} ({EntitySingular}) wurde zu {CollectionDisplay} hinzugefügt")
- CollectionRemovedTemplate (Default: "{Title} ({EntitySingular}) wurde von {CollectionDisplay} entfernt")
- VerboseDelete (Default: true)
- IncludeUnchangedMarkedProperties (Default: false)

Tokens in Templates:
- {DisplayName}, {Old}, {New}
- {Title}, {EntitySingular}, {CollectionDisplay}

## Formatierung der Werte

- null → „∅“
- DateTime → „yyyy-MM-dd HH:mm:ss“
- DateOnly → „yyyy-MM-dd“
- TimeOnly → „HH:mm:ss“
- bool → „Ja“/„Nein“
- Enum → ToString()

## Unterstützte Änderungen

- Modified-Properties mit Delta
- 1:n Collection Add/Remove (über FK + inverse Collection)
- n:m Add/Remove (über Skip Navigations/Join-Entities)
- Owned Types Property-Änderungen
- Delete-Kurzmeldung (optional)

## Beispiele

```csharp
[AuditEntityDisplay("Haustier", "Haustiere")]
[AuditEntityTitleTemplate("{Name}")]
public class Pet
{
        public int Id { get; set; }
        [AuditEntityTitle]
        [AuditDisplay("Name")]
        public string Name { get; set; } = string.Empty;
}

[AuditEntityDisplay("Benutzer", "Benutzer")]
public class User
{
        public int Id { get; set; }
        [AuditDisplay("Haustiere")]
        public List<Pet> Pets { get; set; } = new();
}
```

Ergebnisbeispiele:
- Property: „Name: Bello -> Schnuffi“
- 1:n Add: „Schnuffi (Haustier) wurde zu Haustiere hinzugefügt“
- 1:n Remove: „Schnuffi (Haustier) wurde von Haustiere entfernt“
- n:m Add/Remove analog mit Skip-Navigations

## Erweiterbarkeit

- IAuditEventSink: Ziel der Nachrichten (Default: Logger)
- Eigene Sinks: z. B. Datenbank/Datei

## Tests (Abdeckung)

- Property-Änderungen inkl. [AuditDisplay], [AuditIgnore], Custom-Template
- 1:n Collection Add/Remove
- n:m (Skip Navigations) Add/Remove, inkl. Composite-Key-Beispiel
- Owned Types Property-Änderungen
- Delete-Kurzmeldung
- Datentypen: DateTime, DateOnly, TimeOnly, bool, decimal, Enum

Alle Tests laufen grün (xUnit, InMemory Provider).

## Roadmap

- Mehrsprachigkeit/Localization (Ressourcen/IStringLocalizer)
- Persistente Audit Trails (DB-Sink + Migrationsbeispiel)
