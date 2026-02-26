# SacoStay
This is a project for EXE201_FPTU_CampusHCM of team Ignite in Spring2026
# 📘 GIT NOTE – Hướng dẫn chạy dự án sau khi clone

## ✅ Bước 1: Clone dự án từ Git
```bash
git clone <link-git-repo>
```

---

## 🖥️ PHÍA UI (Frontend)

1. Di chuyển vào thư mục giao diện:
```bash
cd <tên-thư-mục-ui>
```

2. Cài đặt thư viện:
```bash
npm i
```
> 📦 Tự động cài đặt tất cả các thư viện phụ thuộc từ `package.json`.

3. Chạy dự án:
- Nếu dùng **React/Vite**:
```bash
npm start
```

- Nếu dùng **Angular**:
```bash
ng serve
```

---

##  PHÍA API (.NET Core Backend)

1. Di chuyển vào thư mục API:
```bash
cd <tên-thư-mục-api>
```

2. Chạy dự án:
```bash
dotnet run
```
> ⚙️ Lệnh này sẽ khởi động backend ASP.NET Core và phục vụ các API.
3. Các lệnh cho migration(CodeFirst)
```bash
dotnet ef migrations add TenMigration //tạo migration
dotnet ef database update            

dotnet ef migrations list          // xem danh sách migration
dotnet ef migrations remove        // xoá migration cuối (chưa update DB)
dotnet ef database drop            // xoá database
```