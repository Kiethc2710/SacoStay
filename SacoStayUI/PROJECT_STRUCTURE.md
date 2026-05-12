# 📁 SacoStay UI - Cấu trúc Project

## 🏗️ Cấu trúc Folder (Theo MagicPattern Style)

```
src/
├── app/
│   ├── components/           # Reusable components
│   │   └── shared/
│   │       └── loading/     # Loading spinner component
│   ├── pages/              # Page components (routes)
│   │   ├── auth/           # Authentication pages
│   │   │   ├── auth.component.ts
│   │   │   ├── auth.component.html
│   │   │   └── auth.component.css
│   │   └── home/           # Home page
│   │       ├── home.component.ts
│   │       ├── home.component.html
│   │       └── home.component.css
│   ├── layouts/            # Layout components
│   ├── services/           # API services
│   │   └── auth.service.ts
│   ├── models/             # TypeScript interfaces
│   │   └── auth.models.ts
│   ├── utils/              # Utility functions
│   │   ├── constants.ts
│   │   └── helpers.ts
│   ├── core/               # Core functionality
│   │   ├── guards/         # Route guards
│   │   └── interceptors/    # HTTP interceptors
│   ├── app.config.ts       # App configuration
│   ├── app.routes.ts        # Routing configuration
│   └── app.ts              # Root component
├── environments/           # Environment variables
├── styles.css             # Global styles
├── tailwind.config.js     # Tailwind configuration
└── postcss.config.js      # PostCSS configuration
```

## 🎯 Mô tả các folder chính

### 📄 `/pages`
- **Mục đích**: Chứa các page components được mapping với routes
- **Ví dụ**: Auth, Home, Profile, Settings
- **Structure**: Mỗi page có 3 file: `.ts`, `.html`, `.css`

### 🧩 `/components`
- **Mục đích**: Reusable components dùng ở nhiều nơi
- **Ví dụ**: Loading, Button, Modal, Card
- **Structure**: Organized by feature (shared, forms, etc.)

### 🔧 `/services`
- **Mục đích**: API calls và business logic
- **Ví dụ**: AuthService, UserService, RoomService
- **Pattern**: Injectable services với RxJS

### 📝 `/models`
- **Mục đích**: TypeScript interfaces và types
- **Ví dụ**: User, Room, Booking interfaces
- **Naming**: Sử dụng `.models.ts` cho mỗi domain

### 🛠️ `/utils`
- **Mục đích**: Utility functions và helpers
- **Ví dụ**: Validators, Formatters, Constants
- **Files**: `constants.ts`, `helpers.ts`

### 🎨 `/layouts`
- **Mục đích**: Layout components (header, footer, sidebar)
- **Ví dụ**: MainLayout, AuthLayout
- **Usage**: Wrapper cho pages

### 🏛️ `/core`
- **Mục đích**: Core app functionality
- **Ví dụ**: Guards, Interceptors, Base classes
- **Access**: Global và không thay đổi thường xuyên

## 🔄 Luồng hoạt động

1. **Route → Page**: `app.routes.ts` → `/pages/`
2. **Page → Components**: Page components import từ `/components/`
3. **Page → Services**: API calls qua `/services/`
4. **Services → Models**: Data typing qua `/models/`
5. **Utils → All**: Helper functions từ `/utils/`

## 📋 Quy tắc đặt tên

### Components
- **Selector**: `kebab-case` (app-auth, app-home)
- **Class**: `PascalCase` (AuthComponent, HomeComponent)
- **Files**: `component-name.component.ts/html/css`

### Services
- **Class**: `PascalCase` + `Service` (AuthService, UserService)
- **Files**: `service-name.service.ts`

### Models
- **Interface**: `PascalCase` (User, Room, Booking)
- **Files**: `domain.models.ts`

### Utils
- **Functions**: `camelCase` (formatDate, isValidEmail)
- **Constants**: `UPPER_SNAKE_CASE` (API_BASE_URL, TOKEN_KEY)

## 🎨 Design System

### Colors (Tailwind)
- `saco-orange`: #FF9F43 (Primary)
- `saco-orange-dark`: #FF8C2A (Primary hover)
- `saco-blue`: #1A1A2E (Text)
- `saco-gray`: #6B7280 (Secondary)

### Components
- **Buttons**: `.btn-primary-saco`, `.btn-secondary`
- **Inputs**: `.input-saco`
- **Cards**: `.card-saco`
- **Loading**: `<app-loading>`

## 🚀 Development Workflow

1. **Tạo page mới**: Thêm vào `/pages/` và update routes
2. **Tạo component**: Thêm vào `/components/shared/` hoặc feature folder
3. **Tạo service**: Thêm vào `/services/` với injectable pattern
4. **Tạo model**: Thêm vào `/models/` với proper typing
5. **Tạo utility**: Thêm vào `/utils/` nếu reusable

## 📦 Dependencies

### Core
- Angular 20 (standalone components)
- TypeScript
- RxJS

### UI
- Tailwind CSS v3
- ng-zorro-antd (legacy)

### Development
- Angular CLI
- ESLint (nếu có)
- Prettier (nếu có)

## 🌐 Routes Structure

```
/                    → Home (authenticated)
/login              → Auth page (login mode)
/register           → Auth page (register mode)
/auth               → Auth page (default login)
/dashboard          → Dashboard (authenticated)
/profile            → User profile (authenticated)
/settings           → Settings (authenticated)
```
