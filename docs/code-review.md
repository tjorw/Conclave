# Code Review
Användning:
> "Run REVIEW_<NAME> on this code"

---

## REVIEW_FULL

**Syfte:** Övergripande code review

**Prompt:**
Review this code as a senior developer. Focus on:
- Readability
- Maintainability
- Performance
- Potential bugs

Suggest improvements with clear reasoning and examples.

---

## REVIEW_ARCHITECTURE

**Syfte:** Arkitektur & design

**Prompt:**
Review this code from a software architecture perspective. Identify:
- Violations of SOLID principles
- Tight coupling
- Poor abstractions
- Architecture guidelines in frontend.md
- Architecture guidelines in backend.md

Suggest improvements and alternative designs.

---

## REVIEW_BUGS

**Syfte:** Hitta buggar och edge cases

**Prompt:**
Analyze this code for potential bugs, edge cases, and failure scenarios.
What could go wrong in production? Be critical and specific.

---

## REVIEW_SECURITY

**Syfte:** Säker kod

**Prompt:**
Review this code for security vulnerabilities. Consider:
- Input validation
- Injection risks
- Authentication and authorization issues
- Sensitive data handling

Suggest concrete fixes.

---

## REVIEW_PERFORMANCE

**Syfte:** Prestanda & skalbarhet

**Prompt:**
Evaluate this code for performance issues and scalability concerns.
Identify bottlenecks and suggest optimizations with reasoning.

---

## REVIEW_TESTABILITY

**Syfte:** Testbarhet & teststrategi

**Prompt:**
Review this code from a testing perspective:
- Is it testable?
- What unit and integration tests are missing?

Suggest specific test cases.

---

## REVIEW_CLEAN_CODE

**Syfte:** Refaktorering & kodkvalitet

**Prompt:**
Refactor this code to improve readability and maintainability without changing behavior.
Explain the changes.

---

## REVIEW_PR

**Syfte:** Simulera PR-review

**Prompt:**
Act as a reviewer in a pull request. Provide structured feedback:

- Major issues
- Minor improvements
- Questions for the author
- Suggested changes

---

## REVIEW_BUSINESS

**Syfte:** Affärspåverkan

**Prompt:**
Review this code in terms of business impact.
Does it introduce risk for critical flows (e.g. checkout, search, performance)?

Highlight anything that could affect users or revenue.

---

## REVIEW_STRICT

**Syfte:** Hård granskning

**Prompt:**
Be brutally honest in your review.
Prioritize critical feedback over politeness.

What would you block this PR on?

---

## REVIEW_DOTNET

**Syfte:** .NET-specifik review

**Prompt:**
Review this C#/.NET code following best practices for:
- async/await
- dependency injection
- error handling

---

## REVIEW_PLAYWRIGHT

**Syfte:** Testautomation (Playwright)

**Prompt:**
Review this Playwright test. Evaluate:
- Robustness
- Maintainability
- Selector strategy

Does it rely on fragile selectors?

---

## REVIEW_COACHING

**Syfte:** Lärande (junior developer)

**Prompt:**
Review this code and explain your feedback as if the author is a junior developer.
Focus on teaching and reasoning.

---

## REVIEW_CONTEXT (OPTIONAL ADD-ON)

**Syfte:** Lägg till kontext för bättre svar

**Prompt:**
This code is part of:
- [Describe system/context]
- [Traffic level / criticality]

Adjust your review accordingly.