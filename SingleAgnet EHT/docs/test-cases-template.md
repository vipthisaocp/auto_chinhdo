# 🧪 TEST CASES TEMPLATE

> **Tester Lead** tạo test cases từ User Stories và Acceptance Criteria
> 
> 📅 Created: [Date] | Last Updated: [Date]

---

## 📋 Test Case Format

```markdown
### TC-XXX: [Tên Test Case]

| Field | Value |
|-------|-------|
| **User Story** | US-XXX |
| **Type** | Unit / Integration / E2E / Manual |
| **Priority** | 🔴 Critical / 🟠 High / 🟡 Medium / 🟢 Low |
| **Status** | ⬜ Not Run / 🔄 In Progress / ✅ Passed / ❌ Failed |

**Preconditions:**
- [Điều kiện 1]
- [Điều kiện 2]

**Test Steps:**
1. [Bước 1]
2. [Bước 2]
3. [Bước 3]

**Expected Result:**
- [Kết quả mong đợi]

**Actual Result:** _(Điền khi test)_
- [Kết quả thực tế]
```

---

## 🎯 1. FUNCTIONAL TEST CASES

> Từ User Stories + Acceptance Criteria

### TC-001: [Tên Test Case từ US-001]

| Field | Value |
|-------|-------|
| **User Story** | US-001 |
| **Type** | E2E |
| **Priority** | 🔴 Critical |
| **Status** | ⬜ Not Run |

**Preconditions:**
- User đã đăng nhập
- Hệ thống đang hoạt động

**Test Steps:**
1. Navigate to [page]
2. Click [button]
3. Enter [data]
4. Submit form

**Expected Result:**
- AC1: [Kết quả từ Acceptance Criteria 1]
- AC2: [Kết quả từ Acceptance Criteria 2]

---

## 🎨 2. UI/UX TEST CASES

> Từ UI Specs - Design, Layout, Responsive

### TC-UI-001: [Responsive Layout Test]

| Field | Value |
|-------|-------|
| **UI Page** | Homepage |
| **Type** | Manual |
| **Priority** | 🟠 High |
| **Status** | ⬜ Not Run |

**Test Steps:**
1. Mở trang ở Desktop (1920x1080)
2. Thu nhỏ xuống Tablet (768px)
3. Thu nhỏ xuống Mobile (375px)

**Expected Result:**
- Desktop: 3 columns layout
- Tablet: 2 columns layout
- Mobile: 1 column stacked

---

## 🔌 3. API TEST CASES

> Từ API Specs - Endpoints, Request/Response

### TC-API-001: [POST /api/endpoint]

| Field | Value |
|-------|-------|
| **Endpoint** | POST /api/users/login |
| **Type** | Integration |
| **Priority** | 🔴 Critical |
| **Status** | ⬜ Not Run |

**Request:**
```json
{
  "email": "test@example.com",
  "password": "SecurePass123"
}
```

**Expected Response (200):**
```json
{
  "success": true,
  "token": "jwt-token-here",
  "user": { "id": 1, "email": "test@example.com" }
}
```

**Error Cases:**
- 400: Invalid email format
- 401: Wrong password
- 404: User not found

---

## 🔒 4. SECURITY TEST CASES

> Đảm bảo bảo mật code tốt

### TC-SEC-001: SQL Injection Prevention

| Field | Value |
|-------|-------|
| **Target** | All input fields |
| **Type** | Manual |
| **Priority** | 🔴 Critical |
| **Status** | ⬜ Not Run |

**Test Steps:**
1. Nhập `'; DROP TABLE users; --` vào field
2. Submit form
3. Kiểm tra database

**Expected Result:**
- Input được sanitize
- Query sử dụng parameterized statements
- Database không bị ảnh hưởng

### TC-SEC-002: XSS Prevention

### TC-SEC-003: Authentication Required

### TC-SEC-004: Authorization Check

---

## ⚡ 5. PERFORMANCE TEST CASES

> Response time, Load testing

### TC-PERF-001: Page Load Time

| Field | Value |
|-------|-------|
| **Target** | Homepage |
| **Type** | Manual |
| **Priority** | 🟡 Medium |
| **Status** | ⬜ Not Run |

**Expected Result:**
- First Contentful Paint: < 1.5s
- Time to Interactive: < 3s
- Lighthouse Score: > 90

---

## 📊 TEST SUMMARY

| Category | Total | Passed | Failed | Not Run |
|----------|-------|--------|--------|---------|
| Functional | 0 | 0 | 0 | 0 |
| UI/UX | 0 | 0 | 0 | 0 |
| API | 0 | 0 | 0 | 0 |
| Security | 0 | 0 | 0 | 0 |
| Performance | 0 | 0 | 0 | 0 |
| **TOTAL** | **0** | **0** | **0** | **0** |

---

## 🎯 QUALITY CRITERIA

Sản phẩm phải đạt 3 tiêu chí:

| Criteria | Target | Status |
|----------|--------|--------|
| ✅ **Đúng đủ yêu cầu** | All functional tests PASSED | ⬜ |
| 🎨 **Giao diện đẹp, dễ dùng** | All UI/UX tests PASSED | ⬜ |
| 🔒 **Bảo mật code tốt** | All security tests PASSED | ⬜ |

---

> 🧪 *Test-Driven: Viết test cases trước, code pass tests sau*
