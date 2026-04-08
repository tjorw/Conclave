# Convention System – Project Start

You will help me build a system for administering, announcing, registering for, and running conventions (tabletop gaming/hobby conventions in Sweden).

## Language Convention
- **Code and modelling:** English – all class names, method names, properties, variables, namespaces, database columns
- **Documentation and reasoning:** Swedish – comments, README, commit messages, responses in this conversation

## Technology Choices
- **Backend:** .NET 9, C#
- **Architecture:** Clean Architecture with DDD (Domain-Driven Design)
- **ORM:** Entity Framework Core
- **Database:** SQL Server
- **Frontend:** Angular (separate apps for admin and public view)
- **Auth:** ASP.NET Identity with support for social logins (OAuth)
- **API:** REST, minimal API endpoints

## System Overview

The system is multi-tenant where each convention is a tenant with its own database. A central system database handles the tenant registry and routing. A separate identity database handles accounts and authentication.

A person has an account at convention level that persists across editions. Each edition is a unique instance of a convention with a bounded time window.

## Domain Structure

The system is divided into four bounded contexts that communicate via domain events and id-references:

- **Convention** – tenant, edition, person, venue, station, category
- **Event** – event with draft/published versioning, sessions, session requests
- **Registration** – visitor registration, session registration, volunteer registration, ticket
- **Volunteer** – volunteer shift, assignment

## First Step

Set up the solution structure as follows:

```
ConventionSystem.sln
├── src/
│   ├── ConventionSystem.Domain/
│   │   ├── Convention/
│   │   ├── Event/
│   │   ├── Registration/
│   │   └── Volunteer/
│   ├── ConventionSystem.Application/
│   │   ├── Convention/
│   │   ├── Event/
│   │   ├── Registration/
│   │   └── Volunteer/
│   ├── ConventionSystem.Infrastructure/
│   └── ConventionSystem.Api/
└── tests/
    ├── ConventionSystem.Domain.Tests/
    └── ConventionSystem.Application.Tests/
```

Start by implementing the **Convention domain** completely:
- All aggregates, entities and value objects according to the domain model
- Strong id types (e.g. `ConventionId`, `PersonId` as wrapped `Guid`)
- Domain events as records
- No infrastructure or application dependencies in the domain layer

Use the following conventions:
- Private setters on all properties
- Constructors that enforce invariants
- Domain events are collected in a list on the aggregate root and published via the infrastructure layer
- All monetary amounts as `int` (cents/öre) or `decimal` at your discretion
- **ID generation:** Use `Guid.CreateVersion7()` (.NET 9) for all ids. This produces sequential, time-ordered GUIDs that work well as clustered indexes in SQL Server. Configure EF Core with `HasDefaultValueSql("newsequentialid()")` as a database-level fallback. Always generate the id in application code before insert, never in the database.

---

# Architecture Overview

## System Level

Three infrastructure layers:

**Clients:**
- Admin – Angular, role-based views
- Public view – Angular, styled per convention
- External CMS – REST feed, read-only

**API layer (.NET):**
- Tenant router – resolves the correct database per request based on domain/header
- Auth – JWT, social accounts via OAuth
- Public REST – feed + webhooks for published content

**Data level:**
- Tenant databases – one per convention (SQL Server)
- System database – tenant registry and routing
- Identity database – accounts and authentication

## Clean Architecture Layers

① Presentation – Controllers, minimal API endpoints, feed endpoints
② Application – Use cases, commands, queries (CQRS), validation
③ Domain – Convention | Event | Registration | Volunteer
④ Infrastructure – EF Core, repositories, identity, external auth, email

The dependency arrow always points inward. Infrastructure depends on domain, never the other way around.

---

# Context Map

## Id-references (read-only, no direct coupling between aggregates)

- **Event** reads: ConventionId, EditionId, CategoryId, VenueId from Convention
- **Registration** reads: ConventionId, EditionId, PersonId from Convention
- **Volunteer** reads: StationId from Convention; PersonId from Registration

## Domain Events and Flows

**Convention → Event + Registration:**
- `EditionPublished` – start signal for other contexts
- `RegistrationOpened` – opens the respective registration flow

**Event → Registration:**
- `SessionDeactivated` – triggers cancellation of session registrations
- `EventCancelled` – triggers cancellation of all related registrations

**Registration → Volunteer:**
- `VolunteerApplicationReceived` – notifies volunteer coordinator
- `VisitorRegistrationConfirmed` – triggers ticket dispatch

**Volunteer → Registration:**
- `VolunteerShiftCancelled` – triggers automatic cancellation of assignments
- `AssignmentConfirmed` – updates volunteer application status

---

# Data Model (ERD)

## Core Entities

### CONVENTION
- id: uuid PK
- name: string
- slug: string

### CONVENTION_ADMINISTRATOR
- convention_id: uuid FK
- person_id: uuid FK

### PERSON
- id: uuid PK
- convention_id: uuid FK
- name: string
- email: string
- phone: string

### EDITION
- id: uuid PK
- convention_id: uuid FK
- name: string
- start_date: date
- end_date: date
- status: string
- organiser_registration_open: bool
- volunteer_registration_open: bool
- visitor_registration_open: bool
- volunteer_coordinator_id: uuid FK
- event_coordinator_id: uuid FK

### VENUE
- id: uuid PK
- edition_id: uuid FK
- name: string
- building: string
- description: string

### CATEGORY
- id: uuid PK
- edition_id: uuid FK
- responsible_person_id: uuid FK
- name: string
- description: string

## Event

### EVENT
- id: uuid PK
- edition_id: uuid FK
- category_id: uuid FK
- lead_organiser_person_id: uuid FK
- published_version_id: uuid FK (nullable)
- draft_version_id: uuid FK (nullable)

### CO_ORGANISER
- id: uuid PK
- event_id: uuid FK
- person_id: uuid FK

### EVENT_VERSION
- id: uuid PK
- event_id: uuid FK
- title: string
- description: string
- registration_type: string
- drop_in_rules: string
- status: string
- created_at: datetime

### EVENT_COMMENT
- id: uuid PK
- event_id: uuid FK
- version_id: uuid FK (nullable)
- person_id: uuid FK
- text: string
- created_at: datetime

### SESSION
- id: uuid PK
- event_id: uuid FK
- venue_id: uuid FK
- start_time: datetime
- end_time: datetime
- start_type: string
- max_seats: int
- status: string

## Registration and Tickets

### VISITOR_REGISTRATION
- id: uuid PK
- person_id: uuid FK
- edition_id: uuid FK
- created_at: datetime
- status: string

### SESSION_REGISTRATION
- id: uuid PK
- session_id: uuid FK
- person_id: uuid FK
- ticket_id: uuid FK
- created_at: datetime
- status: string

### TICKET_TYPE
- id: uuid PK
- edition_id: uuid FK
- name: string
- price: int
- type: string

### TICKET_TYPE_PERK
- id: uuid PK
- ticket_type_id: uuid FK
- description: string

### TICKET
- id: uuid PK
- ticket_type_id: uuid FK
- person_id: uuid FK
- edition_id: uuid FK
- assigned_by_person_id: uuid FK (nullable)
- status: string
- collected_by_id: uuid (nullable)
- collected_at: datetime (nullable)
- created_at: datetime

## Volunteer

### VOLUNTEER_APPLICATION
- id: uuid PK
- person_id: uuid FK
- edition_id: uuid FK
- interest_description: string
- created_at: datetime
- status: string

### VOLUNTEER_APPLICATION_AVAILABILITY
- id: uuid PK
- volunteer_application_id: uuid FK
- from: datetime
- to: datetime

### VOLUNTEER_APPLICATION_STATION
- id: uuid PK
- volunteer_application_id: uuid FK
- station_id: uuid FK

### STATION
- id: uuid PK
- edition_id: uuid FK
- responsible_person_id: uuid FK
- name: string
- description: string

### VOLUNTEER_SHIFT
- id: uuid PK
- station_id: uuid FK
- start_time: datetime
- end_time: datetime
- min_persons: int
- max_persons: int
- status: string

### VOLUNTEER_ASSIGNMENT
- id: uuid PK
- volunteer_shift_id: uuid FK
- person_id: uuid FK
- assigned_by_id: uuid FK
- status: string
- assigned_at: datetime

---

# Domain Model: Convention

## Aggregate Roots

### Convention <<AggregateRoot>>
- id: ConventionId
- name: string
- slug: string
- RegisterPerson(name, email): Person
- AddAdministrator(personId, performedById)
- CreateEdition(name, dates): Edition

### Edition <<AggregateRoot>>
- id: EditionId
- conventionId: ConventionId
- name: string
- period: DatePeriod
- status: EditionStatus
- organiserRegistrationOpen: bool
- volunteerRegistrationOpen: bool
- visitorRegistrationOpen: bool
- volunteerCoordinatorId: PersonId
- eventCoordinatorId: PersonId
- Publish()
- OpenOrganiserRegistration()
- OpenVolunteerRegistration()
- OpenVisitorRegistration()
- CopyStructure(sourceEditionId)
- CreateVenue(name, building): Venue
- CreateStation(name, responsibleId): Station
- CreateCategory(name, responsibleId): Category

**Invariant:** Edition must be Published before any registration can be opened.

### Person <<Entity>>
- id: PersonId
- conventionId: ConventionId
- name: string
- email: string
- phone: string

## Entities under Edition

### ConventionAdministrator <<Entity>>
- personId: PersonId
- addedById: PersonId
- addedAt: datetime

### Venue <<Entity>>
- id: VenueId
- name: string
- building: string
- description: string

### Station <<Entity>>
- id: StationId
- responsibleId: PersonId
- name: string
- description: string

### Category <<Entity>>
- id: CategoryId
- responsibleId: PersonId
- name: string
- description: string
- ChangeResponsible(personId)

## Value Objects

### DatePeriod <<ValueObject>>
- startDate: date
- endDate: date
- DurationDays(): int

## Enums
- EditionStatus: Draft | Published

## Domain Events
- EditionPublished { editionId, performedById, occurredAt }
- RegistrationOpened { editionId, type: RegistrationType, performedById, occurredAt }
- StructureCopiedFromEdition { targetId, sourceId, venueCount, stationCount, performedById, occurredAt }

## Infrastructure
- DomainEventLog – listens to all domain events and persists them with conventionId, eventType, payload, performedById, occurredAt

---

# Domain Model: Event

## Aggregate Root

### Event <<AggregateRoot>>
- id: EventId
- editionId: EditionId
- categoryId: CategoryId
- leadOrganiserId: PersonId
- publishedVersionId: EventVersionId?
- draftVersionId: EventVersionId?
- status: EventStatus
- SubmitForReview()
- ApproveVersion(responsibleId)
- RejectVersion(responsibleId, comment)
- CancelEvent(responsibleId)
- CreateSession(venue, timeSlot, seats): Session
- DeactivateSession(sessionId, responsibleId)

**Note:** publishedVersionId and draftVersionId are nullable FKs with circular reference – handle with nullable in EF Core and correct migration order.

## Entities

### EventVersion <<Entity>>
- id: EventVersionId
- eventId: EventId
- title: string
- description: string
- registrationType: RegistrationType
- dropInRules: string
- status: VersionStatus
- createdAt: datetime
- EditTitle(title)
- EditDescription(description)
- AddSessionRequest(request): SessionRequest
- RemoveSessionRequest(requestId)

### Session <<Entity>>
- id: SessionId
- eventId: EventId
- venueId: VenueId
- timeSlot: TimeSlot
- maxSeats: int
- startType: StartType
- status: SessionStatus

### SessionRequest <<Entity>>
- id: SessionRequestId
- description: string
- requestedDurationMinutes: int
- requestedSeats: int
- startType: StartType

**Note:** SessionRequest has no coupling to Session – they are separate. The category responsible owns the schedule and does not need to follow the requests.

### CoOrganiser <<Entity>>
- personId: PersonId
- addedAt: datetime

### EventComment <<Entity>>
- id: EventCommentId
- eventId: EventId
- versionId: EventVersionId? (optional reference for context)
- authorId: PersonId
- text: string
- createdAt: datetime

## Value Objects

### TimeSlot <<ValueObject>>
- start: datetime
- end: datetime
- DurationMinutes(): int

## Enums
- EventStatus: Draft | UnderReview | Published | Cancelled
- VersionStatus: Draft | UnderReview | Approved | Rejected
- RegistrationType: DropIn | PreRegistration | Combined
- StartType: FixedTime | Rolling | Tournament
- SessionStatus: Active | Inactive

## Domain Events
- VersionApproved { eventId, versionId, responsibleId, occurredAt }
- VersionRejected { eventId, versionId, responsibleId, occurredAt }
- EventCancelled { eventId, responsibleId, occurredAt }
- SessionDeactivated { sessionId, eventId, performedById, occurredAt }

---

# Domain Model: Registration

## Aggregate Roots

### VisitorRegistration <<AggregateRoot>>
- id: VisitorRegistrationId
- personId: PersonId
- editionId: EditionId
- status: VisitorRegistrationStatus
- createdAt: datetime
- ConfirmPayment(externalReferenceId)
- Cancel()

### SessionRegistration <<AggregateRoot>>
- id: SessionRegistrationId
- sessionId: SessionId
- personId: PersonId
- ticketId: TicketId
- status: SessionRegistrationStatus
- createdAt: datetime
- Cancel()

### VolunteerApplication <<AggregateRoot>>
- id: VolunteerApplicationId
- personId: PersonId
- editionId: EditionId
- interestDescription: string
- status: VolunteerApplicationStatus
- createdAt: datetime
- AddAvailability(from, to)
- RemoveAvailability(availabilityId)
- AddStationPreference(stationId)
- RemoveStationPreference(stationId)

### Ticket <<AggregateRoot>>
- id: TicketId
- ticketTypeId: TicketTypeId
- personId: PersonId
- editionId: EditionId
- assignedById: PersonId?
- status: TicketStatus
- collectedById: PersonId?
- collectedAt: datetime?
- createdAt: datetime
- ConfirmPayment()
- Collect(performedById)
- Revoke(performedById)

## Entities and Value Objects

### Availability <<Entity>>
- id: AvailabilityId
- timeSlot: TimeSlot

### StationPreference <<Entity>>
- stationId: StationId

### TicketType <<Entity>>
- id: TicketTypeId
- editionId: EditionId
- name: string
- price: int
- type: TicketTypeCategory

### TicketPerk <<Entity>>
- id: TicketPerkId
- description: string

### TimeSlot <<ValueObject>>
- start: datetime
- end: datetime

## Domain Service

### RegistrationRuleService <<DomainService>>
- ValidateSeatAvailability(sessionId): bool – queries Event context
- ValidateTicket(ticketId, sessionId): bool – validates ticket is valid for edition

## Enums
- VisitorRegistrationStatus: PendingPayment | Confirmed | Cancelled
- SessionRegistrationStatus: Confirmed | Cancelled
- VolunteerApplicationStatus: Received | UnderReview | Assigned | Confirmed | Rejected
- TicketStatus: Reserved | Paid | Collected | Revoked
- TicketTypeCategory: Visitor | Organiser | Volunteer

## Domain Events
- VisitorRegistrationConfirmed { registrationId, personId, editionId, occurredAt }
- SessionRegistrationCancelled { registrationId, sessionId, personId, occurredAt }
- VolunteerApplicationReceived { applicationId, personId, editionId, occurredAt }
- TicketCollected { ticketId, personId, performedById, occurredAt }
- TicketRevoked { ticketId, personId, performedById, occurredAt }

---

# Domain Model: Volunteer

## Aggregate Root

### VolunteerShift <<AggregateRoot>>
- id: VolunteerShiftId
- stationId: StationId
- timeSlot: TimeSlot
- staffingRequirement: StaffingRequirement
- status: VolunteerShiftStatus
- AssignPerson(personId, assignedById): VolunteerAssignment
- CancelAssignment(assignmentId, performedById)
- Cancel(performedById)

## Entities

### VolunteerAssignment <<Entity>>
- id: VolunteerAssignmentId
- personId: PersonId
- assignedById: PersonId
- status: VolunteerAssignmentStatus
- assignedAt: datetime
- Confirm()
- Reject()

## Value Objects

### StaffingRequirement <<ValueObject>>
- minPersons: int
- maxPersons: int
- IsUnderstaffed(assignedCount): bool
- IsFullyStaffed(assignedCount): bool

### TimeSlot <<ValueObject>>
- start: datetime
- end: datetime
- DurationMinutes(): int

## Domain Service

### AssignmentService <<DomainService>>
- HasTimeOverlap(personId, timeSlot): bool – warning only, does not block
- GetAvailability(personId): list – fetches from Registration context

## Enums
- VolunteerShiftStatus: Planned | InProgress | Cancelled | Completed
- VolunteerAssignmentStatus: Assigned | Confirmed | Rejected | Cancelled

## Domain Events
- VolunteerShiftCancelled { shiftId, stationId, performedById, occurredAt }
- AssignmentConfirmed { assignmentId, shiftId, personId, occurredAt }
- AssignmentRejected { assignmentId, shiftId, personId, occurredAt }
- AssignmentCancelled { assignmentId, shiftId, personId, performedById, occurredAt }
