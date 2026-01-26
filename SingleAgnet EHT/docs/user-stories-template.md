# 📖 USER STORIES & ACCEPTANCE CRITERIA

> **Dự án**: [Tên dự án]  
> **Version**: 1.0  
> **Ngày tạo**: [YYYY-MM-DD]

---

## 📌 Hướng Dẫn Viết User Story

### Format Chuẩn
```
As a [user type/persona]
I want to [action/goal]
So that [benefit/value]
```

### Acceptance Criteria Format (Given-When-Then)
```
GIVEN [precondition/context]
WHEN [action]
THEN [expected result]
```

### Priority Levels
- 🔴 **P0** - Must have (MVP blocker)
- 🟠 **P1** - Should have (Important)
- 🟢 **P2** - Nice to have (Enhancement)

---

## 🎯 Epic 1: [Tên Epic - VD: User Authentication]

> **Mô tả**: [Mục tiêu của epic này]

### US-001: [Tên User Story]

| Thuộc tính | Giá trị |
|------------|---------|
| **Priority** | 🔴 P0 |
| **Persona** | [Tên persona] |
| **Epic** | [Tên Epic] |
| **Sprint** | [Sprint số] |
| **Estimate** | [Story points / Hours] |

**User Story**:
```
As a [user type]
I want to [action]
So that [benefit]
```

**Acceptance Criteria**:

| # | Scenario | Given | When | Then | Status |
|---|----------|-------|------|------|--------|
| AC1 | Happy path | [Context] | [Action] | [Result] | ⬜ |
| AC2 | Edge case | [Context] | [Action] | [Result] | ⬜ |
| AC3 | Error case | [Context] | [Action] | [Result] | ⬜ |

**UI Requirements**:
- [ ] Input field: [Name, type, validation]
- [ ] Button: [Label, action, state]
- [ ] Error message: [Format, placement]

**Technical Notes**:
```
- API endpoint: [endpoint]
- DB changes: [Yes/No - details]
- Dependencies: [List]
```

---

### US-002: [Tên User Story]

| Thuộc tính | Giá trị |
|------------|---------|
| **Priority** | 🟠 P1 |
| **Persona** | [Tên persona] |
| **Epic** | [Tên Epic] |
| **Sprint** | [Sprint số] |
| **Estimate** | [Story points / Hours] |

**User Story**:
```
As a [user type]
I want to [action]
So that [benefit]
```

**Acceptance Criteria**:

| # | Scenario | Given | When | Then | Status |
|---|----------|-------|------|------|--------|
| AC1 | [Scenario] | [Context] | [Action] | [Result] | ⬜ |
| AC2 | [Scenario] | [Context] | [Action] | [Result] | ⬜ |

---

## 🎯 Epic 2: [Tên Epic - VD: Product Management]

> **Mô tả**: [Mục tiêu của epic này]

### US-003: [Tên User Story]

| Thuộc tính | Giá trị |
|------------|---------|
| **Priority** | 🔴 P0 |
| **Persona** | [Tên persona] |
| **Epic** | [Tên Epic] |
| **Sprint** | [Sprint số] |
| **Estimate** | [Story points / Hours] |

**User Story**:
```
As a [user type]
I want to [action]
So that [benefit]
```

**Acceptance Criteria**:

| # | Scenario | Given | When | Then | Status |
|---|----------|-------|------|------|--------|
| AC1 | [Scenario] | [Context] | [Action] | [Result] | ⬜ |
| AC2 | [Scenario] | [Context] | [Action] | [Result] | ⬜ |

---

## 📊 Story Map Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                           USER JOURNEY                               │
├──────────────┬──────────────┬──────────────┬──────────────┬─────────┤
│   Discovery  │   Onboard    │   Core Use   │   Engage     │  Exit   │
├──────────────┼──────────────┼──────────────┼──────────────┼─────────┤
│              │              │              │              │         │
│  ┌────────┐  │  ┌────────┐  │  ┌────────┐  │  ┌────────┐  │         │
│  │US-001  │  │  │US-002  │  │  │US-004  │  │  │US-007  │  │         │
│  │P0      │  │  │P0      │  │  │P0      │  │  │P1      │  │         │
│  └────────┘  │  └────────┘  │  └────────┘  │  └────────┘  │         │
│              │              │              │              │         │
│  ┌────────┐  │  ┌────────┐  │  ┌────────┐  │  ┌────────┐  │         │
│  │US-003  │  │  │US-005  │  │  │US-006  │  │  │US-008  │  │         │
│  │P1      │  │  │P1      │  │  │P1      │  │  │P2      │  │         │
│  └────────┘  │  └────────┘  │  └────────┘  │  └────────┘  │         │
│              │              │              │              │         │
└──────────────┴──────────────┴──────────────┴──────────────┴─────────┘
```

---

## 📋 Summary Table

| ID | User Story | Epic | Priority | Status | Sprint |
|----|------------|------|----------|--------|--------|
| US-001 | [Title] | [Epic] | 🔴 P0 | ⬜ Todo | S1 |
| US-002 | [Title] | [Epic] | 🟠 P1 | ⬜ Todo | S1 |
| US-003 | [Title] | [Epic] | 🔴 P0 | ⬜ Todo | S2 |
| US-004 | [Title] | [Epic] | 🟢 P2 | ⬜ Todo | S3 |

**Status Legend**:
- ⬜ Todo
- 🔄 In Progress
- ✅ Done
- ⏸️ Blocked

---

## 🔗 Related Documents

- [PRD](prd.md)
- [Data Model](data-model.md)
- [UI Specs](ui-specs.md)
- [API Specs](api-specs.md)

---

> 📝 **Note**: Mỗi User Story được hoàn thành phải có tất cả Acceptance Criteria pass.
