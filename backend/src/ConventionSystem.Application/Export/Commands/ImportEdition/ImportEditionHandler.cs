using System.Globalization;
using ConventionSystem.Application.Common.Exceptions;
using ConventionSystem.Application.Content.Abstractions;
using ConventionSystem.Application.Convention.Abstractions;
using ConventionSystem.Application.Event.Abstractions;
using ConventionSystem.Application.Export.Contracts;
using ConventionSystem.Application.Registration.Abstractions;
using ConventionSystem.Application.Staff.Abstractions;
using ConventionSystem.Domain.Content.Aggregates;
using ConventionSystem.Domain.Content.Ids;
using ConventionSystem.Domain.Convention.Entities;
using ConventionSystem.Domain.Convention.Ids;
using ConventionSystem.Domain.Convention.ValueObjects;
using ConventionSystem.Domain.Event.Enums;
using ConventionSystem.Domain.Event.Ids;
using ConventionSystem.Domain.Registration.Entities;
using ConventionSystem.Domain.Registration.Enums;
using ConventionSystem.Domain.Registration.Ids;
using ConventionSystem.Domain.Staff.Aggregates;
using ConventionSystem.Domain.Staff.Ids;
using ConventionSystem.Domain.Staff.ValueObjects;
using StaffTimeSlot = ConventionSystem.Domain.Staff.ValueObjects.TimeSlot;
using EventTimeSlot = ConventionSystem.Domain.Event.ValueObjects.TimeSlot;

namespace ConventionSystem.Application.Export.Commands.ImportEdition;

public sealed class ImportEditionHandler(
    IConventionRepository conventionRepository,
    IPersonRepository personRepository,
    IEditionRepository editionRepository,
    ITicketTypeRepository ticketTypeRepository,
    IEventRepository eventRepository,
    IShiftRepository shiftRepository,
    IPageRepository pageRepository,
    ICurrentUser currentUser)
    : ICommandHandler<ImportEditionCommand, ImportEditionResult>
{
    public async Task<ImportEditionResult> Handle(ImportEditionCommand command, CancellationToken ct)
    {
        if (!IsSupportedSchemaVersion(command.Document.SchemaVersion))
            throw new ArgumentException($"Okänd exportversion: {command.Document.SchemaVersion}.", nameof(command));

        if (command.Document.DurationDays <= 0)
            throw new ArgumentException("Exportdokumentets durationDays måste vara minst 1.", nameof(command));

        var conventionId = new ConventionId(command.ConventionId);
        var importerId = currentUser.PersonId;
        var warnings = new List<ImportWarning>();

        var convention = await conventionRepository.GetByIdAsync(conventionId, ct)
            ?? throw new ResourceNotFoundException("Konvention", command.ConventionId.ToString());

        if (!convention.IsAdministrator(importerId))
            throw new ForbiddenException("Utföraren har inte behörighet att importera upplagor.");

        var importer = await personRepository.GetByIdAsync(importerId, ct)
            ?? throw new ResourceNotFoundException("Person", importerId.Value.ToString());
        if (importer.ConventionId != conventionId)
            throw new ForbiddenException("Utföraren tillhör inte denna konvention.");

        var endDate = command.StartDate.AddDays(command.Document.DurationDays - 1);
        var period = new DatePeriod(command.StartDate, endDate);
        var edition = convention.CreateEdition(command.Name, period, importerId, importerId);

        var scheduleDays = BuildScheduleDays(command.Document, command.StartDate, warnings);
        if (scheduleDays.Count > 0)
        {
            edition.UpdateDetails(command.Name, period, importerId, importerId, scheduleDays);
        }

        foreach (var venue in command.Document.Venues)
            edition.CreateVenue(venue.Name, venue.Building, venue.Description);

        foreach (var area in command.Document.StaffAreas)
        {
            var responsibleId = await ResolvePersonIdAsync(
                conventionId,
                area.ResponsibleEmail,
                importerId,
                warnings,
                $"Funktionsområdet '{area.Name}'",
                ct);

            var staffArea = edition.CreateStaffArea(area.Name, responsibleId, area.Description);
            foreach (var station in area.Stations)
                edition.CreateStation(station.Name, staffArea.Id, station.Description);
        }

        var categoryTranslationsByName = new Dictionary<string, IReadOnlyList<ExportTranslationDto>>(StringComparer.OrdinalIgnoreCase);
        foreach (var category in command.Document.Categories)
        {
            var responsibleId = await ResolvePersonIdAsync(
                conventionId,
                category.ResponsibleEmail,
                importerId,
                warnings,
                $"Kategorin '{category.Name}'",
                ct);

            var publicDescription = string.IsNullOrWhiteSpace(category.PublicDescription)
                ? category.Description
                : category.PublicDescription;

            edition.CreateCategory(
                category.Name,
                responsibleId,
                category.OrganizerInstructions,
                publicDescription);

            if (category.Translations is { Count: > 0 })
                categoryTranslationsByName[category.Name] = category.Translations;
        }

        var tagNamesToImport = command.Document.SchemaVersion >= 4 && command.Document.ProgramTagDetails is not null
            ? command.Document.ProgramTagDetails.Select(t => t.Name).ToList()
            : command.Document.ProgramTagDefinitions?.ToList() ?? [];

        foreach (var tagDefinition in tagNamesToImport)
        {
            try
            {
                edition.AddProgramTagDefinition(tagDefinition);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                warnings.Add(new ImportWarning("ProgramTagDefinitionSkipped", $"Taggdefinitionen '{tagDefinition}' kunde inte skapas: {ex.Message}"));
            }
        }

        await editionRepository.AddAndSaveAsync(edition, ct);

        // Apply category translations (categories now have IDs assigned after save)
        foreach (var category in edition.Categories)
        {
            if (!categoryTranslationsByName.TryGetValue(category.Name, out var translations)) continue;
            foreach (var translation in translations)
            {
                try { edition.SetCategoryTranslation(category.Id, translation.Locale, translation.Name); }
                catch (ArgumentException) { }
            }
        }

        // Apply tag translations (v4)
        if (command.Document.SchemaVersion >= 4)
        {
            foreach (var tagDto in command.Document.ProgramTagDetails ?? [])
            {
                foreach (var translation in tagDto.Translations ?? [])
                {
                    try { edition.SetProgramTagTranslation(tagDto.Name, translation.Locale, translation.Name); }
                    catch (ArgumentException) { }
                }
            }
        }

        if (edition.Categories.Any(c => c.Translations.Count > 0) || edition.ProgramTagTranslations.Count > 0)
            await editionRepository.SaveAsync(ct);

        var stationMap = BuildStationMap(edition);
        var categoryMap = edition.Categories.ToDictionary(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);
        var venueMap = edition.Venues.ToDictionary(v => v.Name, v => v.Id, StringComparer.OrdinalIgnoreCase);
        var allowedProgramTags = edition.ProgramTagDefinitions
            .Select(t => t.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        await CreateShiftsAsync(command.Document, command.StartDate, conventionId, importerId, stationMap, warnings, ct);
        await CreateTicketTypesAsync(command.Document, edition.Id, command.StartDate, categoryMap, warnings, ct);
        await CreateEventsAsync(command.Document, edition.Id, command.StartDate, conventionId, importerId, categoryMap, venueMap, allowedProgramTags, warnings, ct);
        await CreatePagesAsync(command.Document, edition.Id, conventionId, warnings, ct);

        return new ImportEditionResult(edition.Id.Value, warnings);
    }

    private static IReadOnlyList<EditionScheduleDay> BuildScheduleDays(
        EditionExportDocument document,
        DateOnly startDate,
        List<ImportWarning> warnings)
    {
        var days = new List<EditionScheduleDay>();
        foreach (var day in document.ScheduleDays)
        {
            if (!TryRelativeDate(startDate, document.DurationDays, day.Day, out var date))
            {
                warnings.Add(new ImportWarning("ScheduleDaySkipped", $"Schemadag {day.Day} ligger utanför upplagans period."));
                continue;
            }

            if (!TryParseOptionalTime(day.StartTime, out var startTime) ||
                !TryParseOptionalTime(day.EndTime, out var endTime))
            {
                warnings.Add(new ImportWarning("ScheduleDaySkipped", $"Schemadag {day.Day} har ogiltigt tidsformat."));
                continue;
            }

            days.Add(new EditionScheduleDay(Guid.NewGuid(), date, startTime, endTime));
        }

        return days;
    }

    private async Task CreateShiftsAsync(
        EditionExportDocument document,
        DateOnly startDate,
        ConventionId conventionId,
        PersonId importerId,
        IReadOnlyDictionary<(string StaffAreaName, string StationName), StationId> stationMap,
        List<ImportWarning> warnings,
        CancellationToken ct)
    {
        foreach (var area in document.StaffAreas)
        {
            foreach (var station in area.Stations)
            {
                if (!stationMap.TryGetValue((area.Name, station.Name), out var stationId))
                {
                    warnings.Add(new ImportWarning("StationNotFound", $"Stationen '{area.Name}/{station.Name}' hittades inte vid import av pass."));
                    continue;
                }

                foreach (var shift in station.Shifts)
                {
                    if (!TryDateTime(startDate, document.DurationDays, shift.Day, shift.StartTime, out var start) ||
                        !TryDateTime(startDate, document.DurationDays, shift.Day, shift.EndTime, out var end))
                    {
                        warnings.Add(new ImportWarning("ShiftSkipped", $"Passet på stationen '{station.Name}' har ogiltig dag eller tid."));
                        continue;
                    }

                    var responsibleId = await ResolvePersonIdAsync(
                        conventionId,
                        shift.ResponsibleEmail,
                        importerId,
                        warnings,
                        $"Passet på stationen '{station.Name}'",
                        ct);

                    try
                    {
                        var importedShift = new Shift(
                            ShiftId.New(),
                            stationId,
                            responsibleId,
                            new StaffTimeSlot(start, end),
                            new StaffingRequirement(shift.MinPersons, shift.MaxPersons));

                        await shiftRepository.AddAndSaveAsync(importedShift, ct);
                    }
                    catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
                    {
                        warnings.Add(new ImportWarning("ShiftSkipped", $"Passet på stationen '{station.Name}' kunde inte skapas: {ex.Message}"));
                    }
                }
            }
        }
    }

    private async Task CreateTicketTypesAsync(
        EditionExportDocument document,
        EditionId editionId,
        DateOnly startDate,
        IReadOnlyDictionary<string, CategoryId> categoryMap,
        List<ImportWarning> warnings,
        CancellationToken ct)
    {
        foreach (var ticketType in document.TicketTypes ?? [])
        {
            if (!Enum.TryParse<TicketTypeCategory>(ticketType.Type, ignoreCase: true, out var type))
            {
                warnings.Add(new ImportWarning("TicketTypeSkipped", $"Biljettypen '{ticketType.Name}' har okänd typ '{ticketType.Type}'."));
                continue;
            }

            var allowedCategories = ticketType.AllowedCategoryNames?
                .Select(name => categoryMap.TryGetValue(name, out var id) ? id.Value : (Guid?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToArray();

            var validDays = ticketType.ValidDays?
                .Select(day => TryRelativeDate(startDate, document.DurationDays, day, out var date) ? date : (DateOnly?)null)
                .Where(date => date.HasValue)
                .Select(date => date!.Value)
                .ToList();

            try
            {
                var importedTicketType = new TicketType(
                    TicketTypeId.New(),
                    editionId,
                    ticketType.Name,
                    ticketType.Price,
                    type,
                    validDays,
                    allowedCategories,
                    ticketType.Description);

                await ticketTypeRepository.AddAndSaveAsync(importedTicketType, ct);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                warnings.Add(new ImportWarning("TicketTypeSkipped", $"Biljettypen '{ticketType.Name}' kunde inte skapas: {ex.Message}"));
            }
        }
    }

    private async Task CreateEventsAsync(
        EditionExportDocument document,
        EditionId editionId,
        DateOnly startDate,
        ConventionId conventionId,
        PersonId importerId,
        IReadOnlyDictionary<string, CategoryId> categoryMap,
        IReadOnlyDictionary<string, VenueId> venueMap,
        IReadOnlySet<string> allowedProgramTags,
        List<ImportWarning> warnings,
        CancellationToken ct)
    {
        foreach (var exportedEvent in document.Events ?? [])
        {
            if (!categoryMap.TryGetValue(exportedEvent.CategoryName, out var categoryId))
            {
                warnings.Add(new ImportWarning("CategoryNotFound", $"Evenemanget '{exportedEvent.Title}' hoppades över eftersom kategorin '{exportedEvent.CategoryName}' saknas."));
                continue;
            }

            if (!Enum.TryParse<RegistrationType>(exportedEvent.RegistrationType, ignoreCase: true, out var registrationType))
            {
                warnings.Add(new ImportWarning("EventSkipped", $"Evenemanget '{exportedEvent.Title}' har okänd registreringstyp '{exportedEvent.RegistrationType}'."));
                continue;
            }

            try
            {
                var importedEvent = new Domain.Event.Aggregates.Event(EventId.New(), editionId, categoryId, importerId);
                importedEvent.EditTitle(exportedEvent.Title);
                importedEvent.EditDescription(exportedEvent.Description);
                importedEvent.SetRegistrationType(registrationType, exportedEvent.DropInRules);
                importedEvent.UpdateScheduleRequestText(exportedEvent.ScheduleRequestText);
                var coOrganiserLimit = exportedEvent.CoOrganiserLimit > 0
                    ? exportedEvent.CoOrganiserLimit
                    : exportedEvent.CoOrganiserCount.GetValueOrDefault();
                importedEvent.AdjustCoOrganiserLimit(coOrganiserLimit);
                importedEvent.SetProgramTags(FilterSupportedProgramTags(exportedEvent, allowedProgramTags, warnings));

                foreach (var session in exportedEvent.Sessions)
                {
                    if (!venueMap.TryGetValue(session.VenueName, out var venueId))
                    {
                        warnings.Add(new ImportWarning("VenueNotFound", $"Session i evenemanget '{exportedEvent.Title}' hoppades över eftersom lokalen '{session.VenueName}' saknas."));
                        continue;
                    }

                    if (!Enum.TryParse<StartType>(session.StartType, ignoreCase: true, out var startType) ||
                        !TryDateTime(startDate, document.DurationDays, session.Day, session.StartTime, out var start) ||
                        !TryDateTime(startDate, document.DurationDays, session.Day, session.EndTime, out var end))
                    {
                        warnings.Add(new ImportWarning("EventSkipped", $"Session i evenemanget '{exportedEvent.Title}' har ogiltig dag, tid eller starttyp."));
                        continue;
                    }

                    importedEvent.CreateSession(venueId, new EventTimeSlot(start, end), session.MaxSeats, startType);
                }

                await eventRepository.AddAndSaveAsync(importedEvent, ct);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                warnings.Add(new ImportWarning("EventSkipped", $"Evenemanget '{exportedEvent.Title}' kunde inte skapas: {ex.Message}"));
            }
        }
    }

    private async Task<PersonId> ResolvePersonIdAsync(
        ConventionId conventionId,
        string? email,
        PersonId fallbackId,
        List<ImportWarning> warnings,
        string context,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            var person = await personRepository.FindByEmailInConventionAsync(conventionId, email, ct);
            if (person is not null)
                return person.Id;

            warnings.Add(new ImportWarning("PersonNotFound", $"{context}: e-post '{email}' hittades inte; importerande person används."));
        }

        return fallbackId;
    }

    private static IReadOnlyDictionary<(string StaffAreaName, string StationName), StationId> BuildStationMap(
        Domain.Convention.Aggregates.Edition edition)
    {
        var staffAreaNames = edition.StaffAreas.ToDictionary(a => a.Id, a => a.Name);
        return edition.Stations.ToDictionary(
            s => (staffAreaNames.GetValueOrDefault(s.StaffAreaId) ?? "", s.Name),
            s => s.Id);
    }

    private static bool TryRelativeDate(DateOnly startDate, int durationDays, int relativeDay, out DateOnly date)
    {
        date = default;
        if (relativeDay < 1 || relativeDay > durationDays)
            return false;

        date = startDate.AddDays(relativeDay - 1);
        return true;
    }

    private static IReadOnlyList<string> FilterSupportedProgramTags(
        ExportEventDto exportedEvent,
        IReadOnlySet<string> allowedProgramTags,
        List<ImportWarning> warnings)
    {
        var uniqueProgramTags = (exportedEvent.ProgramTags ?? [])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var unsupportedProgramTags = uniqueProgramTags
            .Where(tag => !allowedProgramTags.Contains(tag))
            .ToList();

        foreach (var unsupportedTag in unsupportedProgramTags)
        {
            warnings.Add(new ImportWarning(
                "ProgramTagSkipped",
                $"Evenemanget '{exportedEvent.Title}' använder taggen '{unsupportedTag}' som saknas i upplagans taggdefinitioner."));
        }

        return uniqueProgramTags
            .Where(tag => allowedProgramTags.Contains(tag))
            .ToList();
    }

    private async Task CreatePagesAsync(
        EditionExportDocument document,
        EditionId editionId,
        ConventionId conventionId,
        List<ImportWarning> warnings,
        CancellationToken ct)
    {
        foreach (var exportedPage in document.Pages ?? [])
        {
            var slugExists = await pageRepository.SlugExistsAsync(conventionId, editionId, exportedPage.Slug, null, ct);
            if (slugExists)
            {
                warnings.Add(new ImportWarning("PageSlugAlreadyExists", $"Sidan '{exportedPage.Slug}' hoppades över eftersom sluggen redan finns i upplagan."));
                continue;
            }

            try
            {
                var page = new Page(
                    PageId.New(),
                    conventionId,
                    editionId,
                    exportedPage.Slug,
                    exportedPage.Title,
                    exportedPage.Content,
                    exportedPage.ShowInPublicMenu);
                page.SetMenuSortOrder(exportedPage.MenuSortOrder);
                await pageRepository.AddAsync(page, ct);
                await pageRepository.SaveAsync(ct);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                warnings.Add(new ImportWarning("PageSkipped", $"Sidan '{exportedPage.Slug}' kunde inte skapas: {ex.Message}"));
            }
        }
    }

    private static bool IsSupportedSchemaVersion(int schemaVersion)
        => schemaVersion is 1 or 2 or EditionExportDocument.CurrentSchemaVersion;

    private static bool TryParseOptionalTime(string? value, out TimeOnly? time)
    {
        time = null;
        if (string.IsNullOrWhiteSpace(value))
            return true;

        if (!TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return false;

        time = parsed;
        return true;
    }

    private static bool TryDateTime(DateOnly startDate, int durationDays, int relativeDay, string time, out DateTime dateTime)
    {
        dateTime = default;
        if (!TryRelativeDate(startDate, durationDays, relativeDay, out var date))
            return false;

        if (!TimeOnly.TryParseExact(time, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTime))
            return false;

        dateTime = date.ToDateTime(parsedTime);
        return true;
    }
}
