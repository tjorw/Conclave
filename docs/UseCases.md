# UC001 – Create Convention

## Summary
An administrator creates a new convention tenant in the system.

## Actor
System administrator

## Preconditions
- None

## Flow
1. Administrator provides convention name, slug, and their own name and email
2. System validates that slug is unique across all conventions
3. System creates the convention
4. System creates a person account for the registering user within the new convention
5. System adds that person as a convention administrator (with themselves as addedById)
6. System returns the new ConventionId

## Business Rules
- Slug must be unique across all conventions
- Slug may only contain lowercase letters, digits and hyphens
- Name must not be empty
- A person account is always scoped to a convention – the registering user's person is created as part of this flow
- The registering person is automatically added as administrator

## Domain Events
- Convention Created, Person Created, Admin Added

## Acceptance Criteria
- [x] Convention is persisted with a valid ConventionId (Guid.CreateVersion7)
- [x] A person account is created and linked to the new convention
- [x] That person is added as administrator of the new convention
- [x] Duplicate slug returns a validation error
- [x] Invalid slug format returns a validation error
- [x] Command handler has a corresponding unit test

---

# UC002 – Identify or Create Person During Registration Flow

## Summary
When a person participates in any registration flow (visitor, staff or organiser), the system either identifies an existing person account or creates a new one. This is not a standalone operation – it always occurs as part of another flow (UC-VR001, UC-ST001, UC-EV001).

## Actor
Any user initiating a registration flow

## Preconditions
- Convention exists
- User is authenticated via identity provider (email/password or social login)

## Flow
1. User authenticates via identity provider
2. System checks if a person account exists for this identity within the convention
3a. If person exists: system links the session to the existing person account
3b. If person does not exist: system creates a new person account linked to the convention and the authenticated identity
4. Registration flow continues

## Business Rules
- A person account is always scoped to a convention
- The same physical person may have separate person accounts in different conventions
- Person creation is a side effect of authentication – never a standalone operation for end users
- Name and phone are collected as part of the registration flow if not already present on the person account

## Domain Events
- None (person creation is infrastructural, not a domain event)

## Acceptance Criteria
- [ ] Existing person is identified correctly on re-login
- [ ] New person account is created on first login to a convention
- [ ] Person is linked to correct ConventionId
- [ ] No duplicate person accounts are created for the same identity within a convention

---

# UC002b – Manage Person Registry

## Summary
An administrator creates, updates or deactivates person accounts in the convention's person registry.

## Actor
Convention administrator

## Preconditions
- Convention exists
- Performing user is an administrator of the convention

## Flow – Create
1. Administrator provides name, email and optionally phone
2. System validates email is unique within the convention
3. System creates person account
4. System returns PersonId

## Flow – Update
1. Administrator provides PersonId and updated fields (name, email, phone)
2. System validates email uniqueness if email is changed
3. System updates the person account

## Flow – Deactivate
1. Administrator provides PersonId
2. System marks person as inactive
3. Inactive persons cannot initiate new registrations but existing data is preserved

## Business Rules
- Email must be unique per convention
- Deactivation is soft – person data is never deleted
- Administrator-created persons may not have an associated identity account initially

## Domain Events
- Person Created and Person Updated

## Acceptance Criteria
- [x] Person is persisted and linked to correct ConventionId
- [x] Duplicate email returns a validation error
- [x] Deactivated person cannot initiate new registrations
- [x] Command handlers have corresponding unit tests

---

# UC003 – Add Convention Administrator

## Summary
An existing administrator grants administrator rights to a person within a convention.

## Actor
Convention administrator

## Preconditions
- Convention exists
- Person exists within the convention
- Performing user is an administrator of the convention

## Flow
1. Administrator searches for person by email within the convention
2. System returns matching person
3. Administrator confirms and grants admin rights to the person
4. System validates that the person belongs to the convention
5. System adds the person as administrator
6. System records who performed the action and when

## Business Rules
- Only existing administrators may add new administrators
- A person may only be added as administrator once (idempotent or validation error)

## Domain Events
- None

## Acceptance Criteria
- [x] ConventionAdministrator record is persisted with addedById and addedAt
- [x] Adding a non-member of the convention returns a validation error
- [x] Adding an already-existing administrator is handled gracefully
- [x] Command handler has a corresponding unit test

---

# UC004 – Create Edition

## Summary
An administrator creates a new edition of a convention.

## Actor
Convention administrator

## Preconditions
- Convention exists
- Performing user is an administrator of the convention

## Flow
1. Administrator provides name, start date, end date, staff coordinator and event coordinator
2. System validates date range (end must be after start)
3. System creates the edition with status Draft
4. System returns the new EditionId

## Business Rules
- End date must be after start date
- Edition is created with status Draft
- Staff coordinator and event coordinator must be persons belonging to the convention
- An edition cannot be published without a staff coordinator and event coordinator assigned

## Domain Events
- None (edition created but not yet published)

## Acceptance Criteria
- [x] Edition is persisted with status Draft and valid EditionId
- [x] Invalid date range returns a validation error
- [x] Coordinator not belonging to convention returns a validation error
- [x] Command handler has a corresponding unit test

---

# UC005 – Publish Edition

## Summary
An administrator publishes an edition, making it visible and enabling registration flows to be opened.

## Actor
Convention administrator

## Preconditions
- Edition exists with status Draft
- Edition has a staff coordinator assigned
- Edition has an event coordinator assigned

## Flow
1. Administrator triggers publish
2. System validates all preconditions
3. System transitions edition status to Published
4. System emits EditionPublished event

## Business Rules
- Only a Draft edition can be published
- Staff coordinator must be assigned
- Event coordinator must be assigned
- Once published, the edition cannot revert to Draft

## Domain Events
- `EditionPublished { editionId, performedById, occurredAt }`

## Acceptance Criteria
- [x] Edition status transitions to Published
- [x] EditionPublished domain event is raised
- [x] Publishing without coordinators returns a validation error
- [x] Publishing an already-published edition returns a validation error
- [x] Command handler has a corresponding unit test

---

# UC006 – Copy Structure from Previous Edition

## Summary
An administrator copies venues and stations from a previous edition to a new edition as a starting point.

## Actor
Convention administrator

## Preconditions
- Target edition exists with status Draft
- Source edition exists and belongs to the same convention
- Performing user is an administrator of the convention

## Flow
1. Administrator provides source EditionId and target EditionId
2. System copies all venues from source to target
3. System copies all stations from source to target (station responsible is copied as a reference but may need to be re-assigned)
4. System emits StructureCopiedFromEdition event

## Business Rules
- Only a Draft edition can receive a copied structure
- Source and target must belong to the same convention
- Copying overwrites any existing venues and stations on the target edition
- Categories are not copied – they are created separately per edition

## Domain Events
- `StructureCopiedFromEdition { targetId, sourceId, venueCount, stationCount, performedById, occurredAt }`

## Acceptance Criteria
- [ ] All venues from source are persisted on target with new ids
- [ ] All stations from source are persisted on target with new ids
- [ ] Copying to a Published edition returns a validation error
- [ ] Source and target from different conventions returns a validation error
- [ ] StructureCopiedFromEdition domain event is raised
- [ ] Command handler has a corresponding unit test

---

# UC007 – Open Registration

## Summary
An administrator opens one of the three registration flows (organiser, staff, visitor) for an edition.

## Actor
Convention administrator

## Preconditions
- Edition exists with status Published
- The specific registration flow is not already open

## Flow
1. Administrator specifies which registration type to open (Organiser | Staff | Visitor)
2. System validates that the edition is published
3. System marks the registration type as open
4. System emits RegistrationOpened event

## Business Rules
- Registration can only be opened on a Published edition
- Each registration type (organiser, staff, visitor) is opened independently
- There are no ordering rules between the three types – any can be opened first
- A registration type cannot be opened twice

## Domain Events
- `RegistrationOpened { editionId, type: RegistrationType, performedById, occurredAt }`

## Acceptance Criteria
- [ ] Correct registration flag is set to true on the edition
- [ ] RegistrationOpened domain event is raised with correct type
- [ ] Opening registration on a Draft edition returns a validation error
- [ ] Opening an already-open registration type returns a validation error
- [ ] Command handler has a corresponding unit test

---

# UC008 – Create Venue

## Summary
An administrator creates a venue (physical room or space) for an edition.

## Actor
Convention administrator

## Preconditions
- Edition exists
- Performing user is an administrator of the convention

## Flow
1. Administrator provides name and building
2. System creates the venue linked to the edition
3. System returns the new VenueId

## Business Rules
- Name must not be empty
- Venue is scoped to an edition

## Domain Events
- None

## Acceptance Criteria
- [ ] Venue is persisted and linked to the correct EditionId
- [ ] Command handler has a corresponding unit test

---

# UC009 – Create Station

## Summary
An administrator creates a volunteer station (e.g. reception, kitchen, cleaning) for an edition.

## Actor
Convention administrator

## Preconditions
- Edition exists
- Responsible person exists and belongs to the convention

## Flow
1. Administrator provides name, description and responsible PersonId
2. System creates the station linked to the edition
3. System returns the new StationId

## Business Rules
- Name must not be empty
- Responsible person must belong to the convention

## Domain Events
- None

## Acceptance Criteria
- [ ] Station is persisted and linked to the correct EditionId
- [ ] Responsible person not belonging to convention returns a validation error
- [ ] Command handler has a corresponding unit test

---

# UC010 – Create Category

## Summary
A convention administrator creates an event category (e.g. board games, roleplaying, auction) and assigns a responsible person.

## Actor
Convention administrator

## Preconditions
- Edition exists
- Responsible person exists and belongs to the convention

## Flow
1. Administrator provides name, description and responsible PersonId
2. System creates the category linked to the edition
3. System returns the new CategoryId

## Business Rules
- Name must not be empty
- Responsible person must belong to the convention
- One person may be responsible for multiple categories

## Domain Events
- None

## Acceptance Criteria
- [ ] Category is persisted and linked to the correct EditionId
- [ ] Responsible person not belonging to convention returns a validation error
- [ ] Command handler has a corresponding unit test

---

# UC011 – Change Category Responsible

## Summary
A convention administrator reassigns the responsible person for a category.

## Actor
Convention administrator

## Preconditions
- Category exists
- New responsible person exists and belongs to the convention

## Flow
1. Administrator provides CategoryId and new responsible PersonId
2. System validates the new responsible person belongs to the convention
3. System updates the responsible person on the category

## Business Rules
- New responsible person must belong to the convention

## Domain Events
- None

## Acceptance Criteria
- [ ] Category responsible is updated
- [ ] New responsible not belonging to convention returns a validation error
- [ ] Command handler has a corresponding unit test
