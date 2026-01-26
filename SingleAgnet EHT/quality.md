# ✅ Quality - Đảm Bảo Chất Lượng Sản Phẩm

> 🎯 **Checklist và quy trình đảm bảo chất lượng khi phát triển phần mềm với AI Agent**
>
> 📅 Version: 2.0 | Updated: 2026-01-09

---

## 📋 Tổng Quan

Document này định nghĩa các tiêu chuẩn chất lượng cho mọi deliverables từ việc làm việc với AI Agent, bao gồm:
- TDD (Test-Driven Development) với Tester Lead role
- Code review standards
- Testing requirements
- Documentation standards
- Definition of Done

---

## 🎯 3 TIÊU CHÍ CHẤT LƯỢNG ĐẦU RA

> **Mọi sản phẩm PHẢI đạt đủ 3 tiêu chí này trước khi hoàn thành:**

| Tiêu Chí | Mô Tả | Verification |
|----------|-------|---------------|
| ✅ **Đúng đủ yêu cầu** | Tất cả tính năng hoạt động theo specs | All Functional Tests PASSED |
| 🎨 **Giao diện đẹp, dễ dùng** | UI/UX tốt, responsive, accessible | All UI/UX Tests PASSED |
| 🔒 **Bảo mật code tốt** | Không lỗ hổng bảo mật, best practices | All Security Tests PASSED |

---

## 🧪 TDD - Test-Driven Development

### Philosophy

```
AI first → Docs second → Code third → Quality check last
```

### Workflow với Tester Lead

```
┌───────────────────────────────────────────────────────────────┐
│                    TDD WORKFLOW                                │
├───────────────────────────────────────────────────────────────┤
│                                                                │
│   1. BA tạo User Stories + Acceptance Criteria                │
│                      │                                         │
│                      ▼                                         │
│   2. Tester Lead tạo Test Cases từ User Stories               │
│      ├── Functional Tests                                      │
│      ├── UI/UX Tests                                           │
│      ├── API Tests                                             │
│      ├── Security Tests                                        │
│      └── Performance Tests                                     │
│                      │                                         │
│                      ▼                                         │
│   3. Developer implement code                                  │
│                      │                                         │
│                      ▼                                         │
│   4. Chạy Test Cases → Phải PASS trước khi done              │
│                                                                │
└───────────────────────────────────────────────────────────────┘
```

### Test Cases Document

Mỗi dự án PHẢI có `docs/test-cases.md` chứa:

| Category | Source | Bắt buộc? |
|----------|--------|-----------|
| Functional Tests | User Stories + AC | ✅ Yes |
| UI/UX Tests | UI Specs | 🟡 If has UI |
| API Tests | API Specs | 🟡 If has API |
| Security Tests | Security requirements | ✅ Yes |
| Performance Tests | NFR | 🟡 Optional |

---

## 🔍 Code Review Checklist

### Functionality

```
[ ] Code thực hiện đúng yêu cầu đã đặt ra
[ ] Edge cases được xử lý (null, empty, negative values)
[ ] Error handling đầy đủ và meaningful
[ ] Input validation có mặt
[ ] Output đúng format mong đợi
```

### Code Quality

```
[ ] Naming conventions nhất quán (camelCase, PascalCase)
[ ] Functions/Methods ngắn gọn, single responsibility
[ ] Không có code duplication (DRY principle)
[ ] Comments có ý nghĩa (explain WHY, not WHAT)
[ ] Magic numbers được extract thành constants
[ ] Dead code đã được remove
```

### Security

```
[ ] Không hardcode secrets/passwords
[ ] Input được sanitize trước khi sử dụng
[ ] SQL queries sử dụng parameterized statements
[ ] Sensitive data được log ẩn đi
[ ] Authentication/Authorization đúng chỗ
```

### Performance

```
[ ] Không có N+1 query problems
[ ] Database queries có indexes phù hợp
[ ] Async operations được sử dụng đúng cách
[ ] Memory leaks được avoid (dispose, using)
[ ] Caching được implement cho repeated operations
```

---

## 🧪 Testing Requirements

### Levels of Testing

| Level | Khi nào cần | Coverage Goal |
|-------|-------------|---------------|
| **Unit Tests** | Business logic, utilities | 80%+ |
| **Integration Tests** | API endpoints, DB operations | Critical paths |
| **E2E Tests** | User flows quan trọng | Happy paths |
| **Manual Testing** | UI/UX, edge cases | All features |

### Minimum Testing Checklist

```
[ ] Happy path hoạt động đúng
[ ] Error cases trả về message có ý nghĩa
[ ] Boundary conditions (min, max, empty)
[ ] Permissions/Authorization
[ ] Concurrent access (nếu applicable)
```

### Testing Template

```markdown
## Test Case: [Tên feature/function]

### Preconditions
- [Điều kiện trước khi test]

### Test Steps
1. [Bước 1]
2. [Bước 2]
3. [Bước 3]

### Expected Result
- [Kết quả mong đợi]

### Actual Result
- [Kết quả thực tế]

### Status: ✅ Pass / ❌ Fail
```

---

## 📚 Documentation Standards

### Code Documentation

| Item | Requirement |
|------|-------------|
| **Public APIs** | XML/JSDoc comments bắt buộc |
| **Complex Logic** | Inline comments giải thích |
| **Assumptions** | Ghi chú rõ ràng |
| **TODOs** | Kèm theo context và owner |

### Project Documentation

```
[ ] README.md có hướng dẫn setup và run
[ ] API documentation (Swagger/OpenAPI)
[ ] Architecture diagram (cho dự án lớn)
[ ] Changelog cho mỗi release
[ ] context.md được update sau mỗi phase
```

### Commit Message Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types**: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`

**Example**:
```
feat(auth): implement JWT token refresh

- Add refresh token endpoint
- Store refresh tokens in Redis
- Auto-refresh before expiry

Closes #123
```

---

## ✔️ Definition of Done (DoD)

### Feature DoD

```
[ ] Code complete và build thành công
[ ] Code review passed (self hoặc peer)
[ ] Unit tests written và passing
[ ] Manual testing completed
[ ] Documentation updated
[ ] No critical/high bugs open
[ ] Performance acceptable
[ ] Security review passed
```

### Sprint/Phase DoD

```
[ ] Tất cả features satisfy Feature DoD
[ ] Integration testing passed
[ ] context.md updated
[ ] Demo/walkthrough ready
[ ] Known issues documented
[ ] Deployment tested (staging)
```

### Release DoD

```
[ ] Sprint/Phase DoD satisfied
[ ] Full regression testing
[ ] Performance benchmarks acceptable
[ ] Security audit (nếu applicable)
[ ] User documentation ready
[ ] Rollback plan documented
[ ] Stakeholder sign-off
```

---

## 📊 Quality Metrics

### Code Metrics

| Metric | Target | Tool |
|--------|--------|------|
| **Test Coverage** | >80% | dotCover, Jest |
| **Cyclomatic Complexity** | <10/method | SonarQube |
| **Duplication** | <3% | SonarQube |
| **Technical Debt** | <4 hours/KLOC | SonarQube |

### Process Metrics

| Metric | Description | Target |
|--------|-------------|--------|
| **Bug Escape Rate** | % bugs found in production | <5% |
| **First Pass Yield** | % code passed review first time | >70% |
| **Rework Rate** | Time spent fixing vs creating | <20% |
| **Cycle Time** | Request → Production | Depends on scope |

---

## 🚨 Quality Gates

### Pre-Commit

```bash
# Automated checks before commit
- Linting (ESLint, StyleCop)
- Formatting (Prettier, dotnet format)
- Unit tests (affected only)
```

### Pre-Merge

```bash
# CI/CD pipeline checks
- Full build
- All unit tests
- Code coverage threshold
- Static analysis (SonarQube)
```

### Pre-Deploy

```bash
# Deployment checklist
- Integration tests passed
- Manual QA sign-off
- Rollback plan ready
- Monitoring configured
```

---

## 🔗 Tài Liệu Liên Quan

- 📄 [workflow.md](workflow.md) - Quy trình làm việc
- 📄 [thinking.md](thinking.md) - Framework tư duy
- 📄 [context.md](context.md) - Template context dự án

---

> 🌟 *"Quality is not an act, it is a habit"* — Chất lượng không phải hành động đơn lẻ, mà là thói quen
