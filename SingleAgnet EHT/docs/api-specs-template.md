# 🔌 API SPECIFICATIONS

> **Dự án**: [Tên dự án]  
> **Version**: 1.0  
> **Base URL**: `https://api.example.com/v1`  
> **Ngày tạo**: [YYYY-MM-DD]

---

## 1. 📋 Overview

### 1.1 API Standards

| Aspect | Standard |
|--------|----------|
| Protocol | HTTPS |
| Format | JSON |
| Authentication | Bearer Token (JWT) |
| Versioning | URL path (`/v1/`, `/v2/`) |
| Date Format | ISO 8601 (`2026-01-09T12:00:00Z`) |
| Pagination | Cursor-based or Offset-based |

### 1.2 Common Headers

**Request Headers**:
```http
Content-Type: application/json
Authorization: Bearer <jwt_token>
Accept-Language: vi-VN
X-Request-ID: <uuid>
```

**Response Headers**:
```http
Content-Type: application/json
X-Request-ID: <uuid>
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1704790800
```

---

## 2. 🔐 Authentication

### 2.1 POST `/auth/login`

> Đăng nhập người dùng

**Request**:
```json
{
  "email": "user@example.com",
  "password": "securePassword123"
}
```

**Response `200 OK`**:
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "refreshToken": "dGhpcyBpcyBhIHJlZnJl...",
    "expiresIn": 3600,
    "tokenType": "Bearer",
    "user": {
      "id": 1,
      "email": "user@example.com",
      "fullName": "Nguyen Van A",
      "avatar": "https://...",
      "role": "user"
    }
  }
}
```

**Errors**:
| Code | Message | Description |
|------|---------|-------------|
| 400 | `INVALID_CREDENTIALS` | Email hoặc mật khẩu sai |
| 403 | `ACCOUNT_DISABLED` | Tài khoản bị khóa |
| 429 | `TOO_MANY_ATTEMPTS` | Quá nhiều lần thử (rate limit) |

---

### 2.2 POST `/auth/register`

> Đăng ký tài khoản mới

**Request**:
```json
{
  "email": "newuser@example.com",
  "password": "securePassword123",
  "fullName": "Nguyen Van B",
  "phone": "0901234567"
}
```

**Validation**:
| Field | Rules |
|-------|-------|
| email | Required, Email format, Unique |
| password | Required, Min 8 chars, 1 uppercase, 1 number |
| fullName | Required, 2-100 chars |
| phone | Optional, Vietnamese phone format |

**Response `201 Created`**:
```json
{
  "success": true,
  "data": {
    "id": 2,
    "email": "newuser@example.com",
    "fullName": "Nguyen Van B",
    "message": "Verification email sent"
  }
}
```

---

### 2.3 POST `/auth/refresh`

> Làm mới access token

**Request**:
```json
{
  "refreshToken": "dGhpcyBpcyBhIHJlZnJl..."
}
```

**Response `200 OK`**:
```json
{
  "success": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIs...",
    "expiresIn": 3600
  }
}
```

---

## 3. 📦 Products

### 3.1 GET `/products`

> Lấy danh sách sản phẩm với filter, sort, pagination

**Query Parameters**:
| Param | Type | Default | Description |
|-------|------|---------|-------------|
| `page` | int | 1 | Trang hiện tại |
| `limit` | int | 20 | Số items/trang (max 100) |
| `sort` | string | `created_at:desc` | Field:direction |
| `category` | string | - | Category slug |
| `minPrice` | decimal | - | Giá tối thiểu |
| `maxPrice` | decimal | - | Giá tối đa |
| `q` | string | - | Search keyword |
| `featured` | boolean | - | Chỉ lấy SP nổi bật |

**Example Request**:
```http
GET /products?category=dien-thoai&minPrice=5000000&sort=price:asc&page=1&limit=20
```

**Response `200 OK`**:
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 1,
        "sku": "PHONE-001",
        "name": "iPhone 15 Pro",
        "slug": "iphone-15-pro",
        "price": 28990000,
        "comparePrice": 32990000,
        "discount": 12,
        "imageUrl": "https://...",
        "rating": 4.8,
        "reviewCount": 256,
        "stockStatus": "in_stock",
        "category": {
          "id": 5,
          "name": "Điện thoại",
          "slug": "dien-thoai"
        }
      }
    ],
    "pagination": {
      "currentPage": 1,
      "totalPages": 10,
      "totalItems": 195,
      "itemsPerPage": 20,
      "hasNext": true,
      "hasPrev": false
    }
  }
}
```

---

### 3.2 GET `/products/{slug}`

> Lấy chi tiết sản phẩm

**Path Parameters**:
| Param | Type | Description |
|-------|------|-------------|
| `slug` | string | Product slug |

**Response `200 OK`**:
```json
{
  "success": true,
  "data": {
    "id": 1,
    "sku": "PHONE-001",
    "name": "iPhone 15 Pro",
    "slug": "iphone-15-pro",
    "description": "Mô tả ngắn...",
    "content": "<p>Nội dung HTML chi tiết...</p>",
    "price": 28990000,
    "comparePrice": 32990000,
    "discount": 12,
    "currency": "VND",
    "images": [
      { "url": "https://...", "alt": "Main image", "isPrimary": true },
      { "url": "https://...", "alt": "Side view", "isPrimary": false }
    ],
    "variants": [
      { "id": 1, "name": "Màu", "value": "Đen", "priceModifier": 0 },
      { "id": 2, "name": "Màu", "value": "Trắng", "priceModifier": 0 },
      { "id": 3, "name": "Dung lượng", "value": "256GB", "priceModifier": 0 },
      { "id": 4, "name": "Dung lượng", "value": "512GB", "priceModifier": 3000000 }
    ],
    "specifications": [
      { "name": "Màn hình", "value": "6.1 inch Super Retina XDR" },
      { "name": "Chip", "value": "A17 Pro" },
      { "name": "RAM", "value": "8GB" }
    ],
    "stockQuantity": 50,
    "stockStatus": "in_stock",
    "rating": 4.8,
    "reviewCount": 256,
    "category": {
      "id": 5,
      "name": "Điện thoại",
      "slug": "dien-thoai",
      "breadcrumb": [
        { "name": "Home", "slug": "/" },
        { "name": "Điện tử", "slug": "dien-tu" },
        { "name": "Điện thoại", "slug": "dien-thoai" }
      ]
    },
    "relatedProducts": [
      { "id": 2, "name": "iPhone 15", "slug": "iphone-15", "price": 22990000, "imageUrl": "..." }
    ],
    "seo": {
      "title": "iPhone 15 Pro - Mua ngay với giá tốt nhất",
      "description": "...",
      "keywords": ["iphone 15 pro", "điện thoại apple"]
    },
    "createdAt": "2026-01-01T00:00:00Z",
    "updatedAt": "2026-01-08T12:00:00Z"
  }
}
```

**Error `404 Not Found`**:
```json
{
  "success": false,
  "error": {
    "code": "PRODUCT_NOT_FOUND",
    "message": "Sản phẩm không tồn tại"
  }
}
```

---

## 4. 🛒 Cart

### 4.1 GET `/cart`

> Lấy giỏ hàng hiện tại (requires auth)

**Response `200 OK`**:
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 1,
        "productId": 1,
        "productName": "iPhone 15 Pro",
        "productSlug": "iphone-15-pro",
        "productImage": "https://...",
        "variant": "Đen / 256GB",
        "quantity": 2,
        "unitPrice": 28990000,
        "subtotal": 57980000
      }
    ],
    "summary": {
      "itemCount": 2,
      "subtotal": 57980000,
      "discount": 0,
      "shipping": 0,
      "total": 57980000,
      "currency": "VND"
    }
  }
}
```

---

### 4.2 POST `/cart/items`

> Thêm sản phẩm vào giỏ

**Request**:
```json
{
  "productId": 1,
  "quantity": 1,
  "variantIds": [1, 3]
}
```

**Response `201 Created`**:
```json
{
  "success": true,
  "data": {
    "cartItemId": 5,
    "message": "Đã thêm vào giỏ hàng"
  }
}
```

**Errors**:
| Code | Message |
|------|---------|
| 400 | `PRODUCT_OUT_OF_STOCK` |
| 400 | `INVALID_QUANTITY` |
| 404 | `PRODUCT_NOT_FOUND` |

---

### 4.3 PATCH `/cart/items/{itemId}`

> Cập nhật số lượng item trong giỏ

**Request**:
```json
{
  "quantity": 3
}
```

**Response `200 OK`**:
```json
{
  "success": true,
  "data": {
    "itemId": 5,
    "newQuantity": 3,
    "subtotal": 86970000
  }
}
```

---

### 4.4 DELETE `/cart/items/{itemId}`

> Xóa item khỏi giỏ

**Response `204 No Content`**

---

## 5. 📝 Orders

### 5.1 POST `/orders`

> Tạo đơn hàng (checkout)

**Request**:
```json
{
  "shippingInfo": {
    "fullName": "Nguyen Van A",
    "phone": "0901234567",
    "email": "user@example.com",
    "address": "123 Nguyen Hue, Q1",
    "city": "Hồ Chí Minh",
    "district": "Quận 1",
    "ward": "Phường Bến Nghé"
  },
  "paymentMethod": "cod",
  "couponCode": "SALE10",
  "note": "Giao giờ hành chính"
}
```

**Response `201 Created`**:
```json
{
  "success": true,
  "data": {
    "orderId": 12345,
    "orderNumber": "ORD-20260109-12345",
    "status": "pending",
    "total": 57980000,
    "paymentMethod": "cod",
    "message": "Đơn hàng đã được tạo thành công"
  }
}
```

---

### 5.2 GET `/orders`

> Lấy danh sách đơn hàng của user

**Query Parameters**:
| Param | Type | Description |
|-------|------|-------------|
| `status` | string | Filter by status |
| `page` | int | Page number |
| `limit` | int | Items per page |

**Response `200 OK`**:
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 12345,
        "orderNumber": "ORD-20260109-12345",
        "status": "delivered",
        "total": 57980000,
        "itemCount": 2,
        "createdAt": "2026-01-09T10:00:00Z"
      }
    ],
    "pagination": {...}
  }
}
```

---

### 5.3 GET `/orders/{orderNumber}`

> Chi tiết đơn hàng

**Response `200 OK`**:
```json
{
  "success": true,
  "data": {
    "id": 12345,
    "orderNumber": "ORD-20260109-12345",
    "status": "shipping",
    "statusHistory": [
      { "status": "pending", "timestamp": "2026-01-09T10:00:00Z" },
      { "status": "confirmed", "timestamp": "2026-01-09T10:30:00Z" },
      { "status": "shipping", "timestamp": "2026-01-09T14:00:00Z" }
    ],
    "items": [...],
    "shippingInfo": {...},
    "summary": {
      "subtotal": 57980000,
      "discount": 5798000,
      "shipping": 0,
      "total": 52182000
    },
    "paymentMethod": "cod",
    "paymentStatus": "pending",
    "trackingNumber": "GHTK12345678",
    "createdAt": "2026-01-09T10:00:00Z"
  }
}
```

---

## 6. 🔧 Error Handling

### 6.1 Error Response Format

```json
{
  "success": false,
  "error": {
    "code": "ERROR_CODE",
    "message": "Human readable message",
    "details": [
      { "field": "email", "message": "Email không hợp lệ" }
    ],
    "requestId": "uuid-xxx"
  }
}
```

### 6.2 HTTP Status Codes

| Code | Meaning | Use Case |
|------|---------|----------|
| 200 | OK | GET thành công |
| 201 | Created | POST tạo mới thành công |
| 204 | No Content | DELETE thành công |
| 400 | Bad Request | Validation error |
| 401 | Unauthorized | Chưa đăng nhập |
| 403 | Forbidden | Không có quyền |
| 404 | Not Found | Resource không tồn tại |
| 409 | Conflict | Duplicate, conflict |
| 422 | Unprocessable | Business logic error |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Error | Server error |

### 6.3 Common Error Codes

| Code | Message | HTTP Status |
|------|---------|-------------|
| `VALIDATION_ERROR` | Dữ liệu không hợp lệ | 400 |
| `UNAUTHORIZED` | Vui lòng đăng nhập | 401 |
| `FORBIDDEN` | Không có quyền truy cập | 403 |
| `NOT_FOUND` | Không tìm thấy | 404 |
| `DUPLICATE_EMAIL` | Email đã được sử dụng | 409 |
| `INSUFFICIENT_STOCK` | Không đủ hàng trong kho | 422 |
| `RATE_LIMIT_EXCEEDED` | Quá nhiều request | 429 |
| `INTERNAL_ERROR` | Lỗi hệ thống | 500 |

---

## 7. 📊 Rate Limiting

| Endpoint Type | Limit | Window |
|---------------|-------|--------|
| Auth endpoints | 5 requests | 1 minute |
| Public read | 100 requests | 1 minute |
| Authenticated | 1000 requests | 1 minute |
| Admin | 5000 requests | 1 minute |

**Rate Limit Headers**:
```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1704790800
```

---

## 8. 📚 API Changelog

| Version | Date | Changes |
|---------|------|---------|
| v1.0 | 2026-01-09 | Initial release |
| v1.1 | TBD | Add wishlist endpoints |

---

## 9. 🔗 Related Documents

- [PRD](prd.md)
- [Data Model](data-model.md)
- [UI Specs](ui-specs.md)

---

> 📝 **Note**: Document này được generate từ OpenAPI spec. Cập nhật spec khi có API thay đổi.
