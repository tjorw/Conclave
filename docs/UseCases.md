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
- [x] All venues from source are persisted on target with new ids
- [x] All stations from source are persisted on target with new ids
- [x] Copying to a Published edition returns a validation error
- [x] Source and target from different conventions returns a validation error
- [x] StructureCopiedFromEdition domain event is raised
- [x] Command handler has a corresponding unit test

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
- [x] Correct registration flag is set to true on the edition
- [x] RegistrationOpened domain event is raised with correct type
- [x] Opening registration on a Draft edition returns a validation error
- [x] Opening an already-open registration type returns a validation error
- [x] Command handler has a corresponding unit test

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
- [x] Venue is persisted and linked to the correct EditionId
- [x] Command handler has a corresponding unit test

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
- [x] Station is persisted and linked to the correct EditionId
- [x] Responsible person not belonging to convention returns a validation error
- [x] Command handler has a corresponding unit test

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
- [x] Category is persisted and linked to the correct EditionId
- [x] Responsible person not belonging to convention returns a validation error
- [x] Command handler has a corresponding unit test

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
- [x] Category responsible is updated
- [x] New responsible not belonging to convention returns a validation error
- [x] Command handler has a corresponding unit test

---

# UC012 – Create Staff Area

## Summary
A convention administrator creates a staff functional area (e.g. reception, kitchen, cleaning) under an edition and assigns a responsible person. The responsible person can administer all stations and shifts within the area.

## Actor
Convention administrator

## Preconditions
- Edition exists
- Responsible person exists and belongs to the convention
- Performing user is an administrator of the convention

## Flow
1. Administrator provides name, optional description and responsible PersonId
2. System validates the responsible person belongs to the convention
3. System creates the staff area linked to the edition
4. System returns the new StaffAreaId

## Business Rules
- Name must not be empty
- Responsible person must belong to the convention
- One person may be responsible for multiple staff areas

## Domain Events
- None

## Acceptance Criteria
- [x] Staff area is persisted and linked to the correct EditionId
- [x] Responsible person not belonging to convention returns a validation error
- [x] Command handler has a corresponding unit test

---

# UC009 (revised) – Create Station

## Summary
An administrator creates a station (e.g. "Reception desk A") under a staff area for an edition.

## Actor
Convention administrator, Staff coordinator, or Staff area responsible

## Preconditions
- Edition exists
- Staff area exists and belongs to the edition
- Performing user is an administrator, staff coordinator, or responsible for the staff area

## Flow
1. Administrator provides name, optional description and StaffAreaId
2. System creates the station linked to the staff area
3. System returns the new StationId

## Business Rules
- Name must not be empty
- Station is scoped to a staff area (and thereby an edition)
- Station no longer has its own responsible person – the staff area responsible governs all stations in the area

## Domain Events
- None

## Acceptance Criteria
- [x] Station is persisted and linked to the correct StaffAreaId
- [x] StaffAreaId not belonging to the edition returns a validation error
- [x] Command handler has a corresponding unit test

---

# UC-ST001 – Create Shift

## Summary
A staff coordinator or staff area responsible creates a shift (time slot with staffing requirements) for a station.

## Actor
Convention administrator, Staff coordinator, or Staff area responsible

## Preconditions
- Station exists and belongs to the edition
- Responsible person (shift lead) exists and belongs to the convention
- Performing user is an administrator, staff coordinator, or responsible for the station's staff area

## Flow
1. Actor provides StationId, start time, end time, min persons, max persons, and shift lead PersonId
2. System validates date range and staffing requirements
3. System creates the shift with status Planned
4. System returns the new ShiftId

## Business Rules
- End time must be after start time
- MaxPersons must be >= MinPersons
- MinPersons must be >= 0
- Shift lead can be any person belonging to the convention
- Shift is created with status Planned

## Domain Events
- None

## Acceptance Criteria
- [x] Shift is persisted with status Planned and correct StationId
- [x] Invalid time range returns a validation error
- [x] Invalid staffing requirement returns a validation error
- [x] Command handler has a corresponding unit test

---

# UC-ST002 – Assign Person to Shift

## Summary
A staff coordinator or staff area responsible assigns a person to a shift. The primary scenario is assigning persons who have submitted a staff application, but any person in the convention can be assigned.

## Actor
Convention administrator, Staff coordinator, or Staff area responsible

## Preconditions
- Shift exists with status Planned
- Person exists and belongs to the convention

## Flow
1. Actor provides ShiftId and PersonId
2. System validates the shift is not cancelled and has available capacity
3. System checks for time overlap with the person's other shifts (warning only – does not block)
4. System creates the assignment with status Assigned
5. System returns the new StaffAssignmentId

## Business Rules
- Shift must not be cancelled
- MaxPersons capacity must not be exceeded
- A person cannot be assigned to the same shift twice
- Time overlap with other shifts is a warning, not a hard block
- Any person in the convention can be assigned (not limited to staff applicants)

## Domain Events
- `PersonAssignedToShift { assignmentId, shiftId, personId, assignedById, occurredAt }`

## Acceptance Criteria
- [x] Assignment is persisted with status Assigned
- [x] Assigning to a cancelled shift returns a validation error
- [x] Assigning beyond max capacity returns a validation error
- [x] Assigning the same person twice returns a validation error
- [x] PersonAssignedToShift domain event is raised
- [x] Command handler has a corresponding unit test

---

# UC-ST003 – Confirm Assignment

## Summary
A staff coordinator or staff area responsible confirms a staff assignment.

## Actor
Convention administrator, Staff coordinator, or Staff area responsible

## Preconditions
- Assignment exists with status Assigned

## Flow
1. Actor provides StaffAssignmentId
2. System transitions assignment status to Confirmed
3. System emits AssignmentConfirmed event

## Business Rules
- Only an Assigned assignment can be confirmed

## Domain Events
- `AssignmentConfirmed { assignmentId, shiftId, personId, occurredAt }`

## Acceptance Criteria
- [x] Assignment status transitions to Confirmed
- [x] AssignmentConfirmed domain event is raised
- [x] Confirming a non-Assigned assignment returns a validation error
- [x] Command handler has a corresponding unit test

---

# UC-ST004 – Reject Assignment

## Summary
A staff coordinator or staff area responsible rejects a staff assignment.

## Actor
Convention administrator, Staff coordinator, or Staff area responsible

## Preconditions
- Assignment exists with status Assigned

## Flow
1. Actor provides StaffAssignmentId
2. System transitions assignment status to Rejected
3. System emits AssignmentRejected event

## Business Rules
- Only an Assigned assignment can be rejected

## Domain Events
- `AssignmentRejected { assignmentId, shiftId, personId, occurredAt }`

## Acceptance Criteria
- [x] Assignment status transitions to Rejected
- [x] AssignmentRejected domain event is raised
- [x] Rejecting a non-Assigned assignment returns a validation error
- [x] Command handler has a corresponding unit test

---

# UC-ST005 – Cancel Assignment

## Summary
A staff coordinator, staff area responsible, or the assigned person themselves cancels a staff assignment.

## Actor
Convention administrator, Staff coordinator, Staff area responsible, or the assigned person

## Preconditions
- Assignment exists with status Assigned or Confirmed

## Flow
1. Actor provides StaffAssignmentId
2. System validates that the actor is either authorized staff admin or the assigned person
3. System transitions assignment status to Cancelled
4. System emits AssignmentCancelled event

## Business Rules
- An Assigned or Confirmed assignment can be cancelled
- A Rejected or already Cancelled assignment cannot be cancelled
- The assigned person may cancel their own assignment
- Administrators, staff coordinators, and staff area responsibles may cancel any assignment in their scope

## Domain Events
- `AssignmentCancelled { assignmentId, shiftId, personId, performedById, occurredAt }`

## Acceptance Criteria
- [x] Assignment status transitions to Cancelled
- [x] AssignmentCancelled domain event is raised
- [x] Cancelling an already-cancelled or rejected assignment returns a validation error
- [x] The assigned person can cancel their own assignment
- [x] Command handler has a corresponding unit test

---

# UC-ST006 – Cancel Shift

## Summary
A staff coordinator or staff area responsible cancels an entire shift. All active assignments are automatically cancelled via a domain event handler.

## Actor
Convention administrator, Staff coordinator, or Staff area responsible

## Preconditions
- Shift exists with status Planned

## Flow
1. Actor provides ShiftId
2. System transitions shift status to Cancelled
3. System emits ShiftCancelled event
4. Domain event handler cancels all active assignments on the shift

## Business Rules
- Only a Planned shift can be cancelled
- All Assigned and Confirmed assignments are cancelled as a side effect

## Domain Events
- `ShiftCancelled { shiftId, stationId, performedById, occurredAt }`

## Acceptance Criteria
- [x] Shift status transitions to Cancelled
- [x] ShiftCancelled domain event is raised
- [x] Cancelling an already-cancelled shift returns a validation error
- [x] Command handler has a corresponding unit test
