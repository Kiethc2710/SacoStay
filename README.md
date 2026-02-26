# SacoStay
This is a project for EXE201_FPTU_CampusHCM of team Ignite in Spring2026

# 📘 GIT NOTE – Hướng dẫn chạy dự án sau khi clone

## ✅ Bước 1: Clone dự án từ Git
```bash
git clone <link-git-repo>
cd SacoStay
```

---

## 🖥️ PHÍA UI (Frontend – Angular + NG-ZORRO)

**Yêu cầu:** Cài [Node.js](https://nodejs.org/) (phiên bản LTS, khuyến nghị 18+) và [Angular CLI](https://angular.dev/tools/cli) (tùy chọn: `npm i -g @angular/cli@20`).

### 1. Vào thư mục frontend
```bash
cd SacoStayUI
```

### 2. ⚠️ Cài đặt thư viện (bắt buộc)
```bash
npm install
```
hoặc
```bash
npm i
```

> **Lưu ý:** Nếu bỏ qua bước này, khi chạy hoặc build sẽ báo lỗi **Cannot find module 'ng-zorro-antd/...'** (TS2307). Project dùng **Angular** và **NG-ZORRO** (ng-zorro-antd), tất cả nằm trong `package.json` và chỉ được cài sau khi chạy `npm install`.

### 3. Chạy dự án
```bash
npm start
```
hoặc
```bash
ng serve
```

> Sau khi build xong, mở trình duyệt tại **http://localhost:4200** (trang login: http://localhost:4200/login).

### 4. Nếu vẫn lỗi module sau khi `npm install`
- **Linux/Mac** (trong thư mục **SacoStayUI**):
  ```bash
  rm -rf node_modules package-lock.json
  npm install
  ```
- **Windows:** Xóa thư mục `node_modules` và file `package-lock.json` (trong SacoStayUI) bằng tay, rồi mở lại terminal trong SacoStayUI và chạy `npm install`.

---

## ⚙️ PHÍA API (.NET Core Backend)

**Yêu cầu:** Cài [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) và SQL Server (đã cấu hình connection string trong `appsettings.json`).

### 1. Vào thư mục API
```bash
cd SacoStayAPI
cd SacoStayAPI
```
*(Hoặc từ thư mục gốc: `cd SacoStayAPI/SacoStayAPI`)*

### 2. Chạy backend
```bash
dotnet run
```
hoặc chạy với profile http (port 5219):
```bash
dotnet run --launch-profile http
```

> ⚙️ API chạy tại **http://localhost:5219**. Swagger: http://localhost:5219/swagger
