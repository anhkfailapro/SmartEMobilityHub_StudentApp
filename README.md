# Smart E-Mobility Hub – Student App (Demo)

Frontend demo of the **Student / Private EV Owner mobile app** for the Smart E-Mobility Hub project (CO3001 – Group 21).
It covers use cases **UC-01 … UC-11**, including their exceptions and alternative flows, with in-memory mock data.

- Technology: **C# / WPF on .NET 10** (`net10.0-windows`). No NuGet packages.
- The window shows a **phone screen (390 × 844)** on the left and a **Demo panel** on the right.
- UI language: Vietnamese (same as the Figma design).
- Requirements: `docs/SmartEMobilityHub_StudentMobileApp_TechnicalSpec.pdf`. Decisions that override the spec: `docs/DECISIONS.md`.

## 1. Requirements

| Item | Version |
|---|---|
| OS | Windows 10 or Windows 11 |
| IDE | Visual Studio 2022 **17.14 or newer**, or Visual Studio 2026, with the workload **".NET desktop development"** |
| SDK | **.NET 10 SDK** (check with `dotnet --list-sdks`; a line starting with `10.` must exist) |

Optional: install the **Roboto** font to match Figma exactly. Without it the app uses Segoe UI.

## 2. How to run

### Option A – Visual Studio
1. Open `SmartEMobilityHub.slnx`.
2. Press **F5** (or *Debug → Start Debugging*).

### Option B – Command line
```bat
cd path\to\SmartEMobilityHub
dotnet run
```

The window can be resized. The content scales to fit.

## 3. How to use the demo

- **Phone screen (left):** the student app. The bottom bar has 5 tabs: Trang chủ, Đặt chỗ, Chuyến đi, Thông báo, Tài khoản.
- **Demo panel (right):** not part of the app. It simulates things a real phone/IoT system would provide:
  - **Thời gian demo:** move the clock forward (+1/+5/+15/+30 min) to test hold expiry, late-cancel fees and reminders.
  - **Công tắc tình huống:** switches that force an alternative path (for example "Hub trả xe hết chỗ").
  - **Sự kiện hệ thống:** one-click system events (stale data, charging point assigned, charging point broken).
  - **Đặt lại toàn bộ demo:** reset all data.
- No login: the app opens as demo user **Minh**, who is both a Student and a Private EV Owner.

## 4. Walkthrough per use case

| UC | Normal flow | Alternative / exception paths and how to trigger them |
|---|---|---|
| UC-01 Plan journey | Home → type a destination (or tap a suggestion) → **Tìm** → *Chọn Hub xuất phát* → vehicle list | **3a** tap a Hub card on Home · **4a** Demo panel "Dịch vụ bản đồ không phản hồi" · **7a** tap *Xem tất cả* → *Hub Nhà văn hóa SV* (no available vehicle → 3 nearest Hubs) |
| UC-02 Vehicle status | Vehicle list → tap a vehicle | **1a** chip "Pin > 50%" · **2a** EM-2060 (data 14 min old) or Demo panel "Làm cũ dữ liệu EB-1024" · **3a** EM-2060 (faulty) / EB-1031 (reserved) → read-only, *Đặt trước* disabled |
| UC-03 Reserve vehicle | Detail → *Đặt trước* → *Xác nhận đặt* → countdown + pick-up code | **2a** Demo panel "SV khác đặt xe trước" · **2b** reserve a second vehicle · **4a** *Hủy* on the confirm screen · **5a** Demo panel "+15 phút" |
| UC-04 Reserve parking | Home → *Đặt chỗ đỗ xe* → fill form → *Xác nhận đặt chỗ* → slot + entry QR | **3a** switch "Đến ngay" · **4a** Demo panel "Hub đỗ xe đã chọn hết chỗ" → Hubs within 1 km · **4b** make a second parking reservation (limit = 1) |
| UC-05 Cancel / change | Tab *Đặt chỗ* → *Hủy* → confirm | **3a–3c** *Đổi giờ* on a parking or charging booking (Demo panel "Khung giờ mới không còn chỗ" = time kept) · **6a** cancel the vehicle reservation after pick-up · **6b** book parking with *Đến ngay* (start < 5 min), then cancel → late fee 10.000đ |
| UC-06 Pick up | Reservation screen → *Quét QR để nhận xe* → *Giả lập quét (đúng xe)* → unlocking → trip starts | **1a** *Nhập mã thủ công* (the screen shows the demo codes) · **2a** *Quét nhầm xe khác* (2 more attempts) · **2b** move the clock past the hold time · **4a** Demo panel "Xe không xác nhận mở khóa" → incident + support contact. Extra: "Sinh viên chưa ở Hub" |
| UC-07 Return | Tab *Chuyến đi* → *Trả xe* → choose Hub → *Xác nhận trả xe* → invoice | **2a** Demo panel "Xe không nằm trong Hub" · **2b** return at *KTX Khu A* (full) or Demo panel "Hub trả xe hết chỗ" → nearest Hub + 10 free minutes · **4a** the demo user has 1 free monthly-plan minute |
| UC-08 Charging request | Trip → *Yêu cầu sạc* → *Xác nhận yêu cầu sạc* → queue position. Demo panel "Cấp cổng sạc" sends the "charging point assigned" notification | **2a** Demo panel "Tất cả cổng sạc đang bận" · **3a** *Chọn Hub khác* · **3b** *Kết thúc chuyến, đặt xe mới và yêu cầu sạc* · **5a** submit twice for the same vehicle |
| UC-09 Schedule charging | Home → *Lên lịch sạc* → fill form → *Xác nhận lịch sạc* → booking detail; reminder 30 min before | **3a** switch "Sạc sớm nhất có thể" · **4a** Demo panel "Không còn cổng sạc trong khung giờ" · **4b** choose 150 min at *Thư viện Trung tâm* (limit 120) · **5a** Demo panel "Cổng sạc đã đặt bị hỏng" (moved / cancelled) |
| UC-10 Report problem | Trip → *Báo lỗi* (or Trip summary → *Báo lỗi xe*) → fill form → *Gửi báo cáo* → report code | **2a** report the vehicle you are riding → "Kết thúc chuyến và báo lỗi" · **3a** pick a photo > 5 MB (or tap "Demo: thử chọn ảnh 7,2 MB") · **4a** report **EB-1031** (already has incident BC-1187 → linked) |
| UC-11 Notifications & history | Home → *Hoạt động* (or tab *Thông báo*) → tabs *Thông báo* / *Lịch sử* → tap an entry → detail + invoice | **3a** stay on *Thông báo* (newest first, marked as read) · **4a** Demo panel "Không có lịch sử 3 tháng" |

Other features: **Tài khoản** tab shows the read-only private vehicles, the monthly-plan minutes and a **dark mode** switch.

## 5. Not in this demo (by decision)

- Real login, real payment, real GPS/IoT/map (all simulated).
- Payment failure (UC-07 5a) and "Pay now" (UC-11 6a): payment always succeeds (OPEN-12).
- Parking grace time, no-show, extension (OPEN-07); deposit for charging bookings (OPEN-06).
- QR codes are drawn placeholders and cannot be scanned.
- Prices, labels and priority ranges marked *Assumed – review* in `docs/DECISIONS.md` are demo values.

## 6. Project structure

```
SmartEMobilityHub/
├─ SmartEMobilityHub.slnx / .csproj   WPF project (net10.0-windows)
├─ App.xaml, MainWindow.xaml(.cs)     window: phone frame + Demo panel
├─ DemoApp.cs                         wiring: services, clock, 1-second timer, tab navigation
├─ Models/      domain classes from spec §4 (Hub, Vehicle, VehicleReservation, ParkingReservation,
│               PrivateEVChargingSession, ChargingRequest, Trip, Invoice, Incident, …)
├─ Data/        DemoConfig (prices, limits, labels), DemoClock, DemoToggles, MockDataStore (seed data)
├─ Services/    spec §8 services: JourneyPlanner, VehicleManager, ReservationService, ParkingService,
│               PickUpService, TripBillingService, ChargingService, BookingService, IncidentService,
│               NotificationService, HistoryService
├─ Ui/          Theme (light/dark tokens from Figma), UiKit (shared controls), AppShell (navigation,
│               dialogs, bottom bar), DemoPanel
├─ Screens/     one class per screen (Home, VehicleList, VehicleDetail, Reservation, ScanQr, Trip, …)
└─ docs/        spec PDF, DECISIONS.md, PLAN.md, CHANGE_LOG_proposal.md
```

Architecture: **Screen (UI) → Service → in-memory data**, matching the spec's traceability table (§8).
Screens are built in C# with the shared `UiKit`, so every screen uses the same Figma style.

---

## 7. Phần của Vỹ — dịch vụ lõi & Trung tâm điều hành

Ngoài app sinh viên, repo có thêm ba project (vẫn **không dùng NuGet**, cùng .NET 10):

| Project | Vai trò |
|---|---|
| `src/SmartEMobilityHub.Core` | Lõi nghiệp vụ dùng chung: UC-SYS-01…04 (lập lịch sạc, đồng bộ trạng thái, ngưỡng quá tải, huỷ đặt chỗ quá hạn), UC-OP-03 (quản lý sự cố) và UC-OP-04 (giới hạn công suất). Không phụ thuộc WPF. |
| `src/SmartEMobilityHub.Operator` | **Trung tâm điều hành** (WPF): danh sách 24 sự cố, chi tiết + nhật ký, khoá thiết bị, tạo phiếu bảo trì, gán kỹ thuật viên, tạm dừng Hub; cột phải có KPI, hàng chờ sạc và panel demo. |
| `tests/SmartEMobilityHub.Tests` | Console runner: **18 test case bắt buộc** (Bảng 62–67) + 5 test biên + 7 smoke seed. Tự sinh `tests/report/results.md` và `results.json`. |

### Chạy

```bat
cd SmartEMobilityHub_StudentApp

rem Dashboard điều hành
dotnet run --project src\SmartEMobilityHub.Operator\SmartEMobilityHub.Operator.csproj

rem Kiểm thử (exit code 0 = tất cả đạt)
dotnet run --project tests\SmartEMobilityHub.Tests\SmartEMobilityHub.Tests.csproj
```

Máy chưa có .NET 10 SDK thì dùng SDK cục bộ mà Vỹ đã cài (thư mục ngoài repo): thay `dotnet` bằng `..\.dotnet\dotnet`.

### Tài liệu bàn giao

- `docs/GD2_GD4_SERVICES.md` — kiến trúc service, API, sáu bước timer, cách tích hợp.
- `docs/GD5_TESTS.md` — bảng 18 case + kết quả 30/30, ghi chú dữ liệu test.
- `docs/GD6_OPERATOR_UI.md` — mô tả màn hình và kịch bản demo 10 bước.
- `docs/DESIGN_CHANGES.md` — mọi sai lệch so với Submission #3 (DC-01…DC-34).
- `docs/AI_USAGE_LOG.md` — khai báo sử dụng AI theo từng giai đoạn.
- `docs/GD0_REVIEW.md`, `docs/GD1_CORE_CSHARP.md` — rà soát đặc tả và bàn giao GĐ1.

### Lưu ý tích hợp

- Dashboard dùng seed riêng của Core (`NetworkSeed`), **không sửa** `MockDataStore` của app sinh viên: seed app cố ý cho KTX Khu A đầy 100% để demo UC-07, trong khi KPI của dashboard cần HUB-08 = 90%.
- App sinh viên chỉ bị sửa `SmartEMobilityHub.csproj` (loại `src/**`, `tests/**` khỏi glob) và `SmartEMobilityHub.slnx` (thêm project); không đổi XAML hay logic màn hình sinh viên.
- Khi tích hợp thật, thêm `ProjectReference` sang Core và map enum `DeviceStatus` (PascalCase) ↔ `VehicleStatus`/`PointStatus` của app tại biên.

```
SmartEMobilityHub_StudentApp/
├─ SmartEMobilityHub.csproj          WPF app sinh viên (UC-01…UC-11)
├─ src/
│  ├─ SmartEMobilityHub.Core/        lõi nghiệp vụ + adapter + seed
│  └─ SmartEMobilityHub.Operator/    dashboard Quản lý sự cố & Bảo trì
├─ tests/
│  ├─ SmartEMobilityHub.Tests/       runner + 18 case + unit biên
│  └─ report/                        results.md / results.json (tự sinh)
└─ docs/                             đặc tả, quyết định, bàn giao, khai báo AI
```

---

## 8. Phần của Đức — mô phỏng What-if

Chức năng **What-if** trong Trung tâm điều hành: cấu hình kịch bản, dự báo tải Hub, đánh giá ảnh hưởng, đề xuất điều phối, mô phỏng lại sau đề xuất và xuất báo cáo PDF. Giao diện được thiết kế theo màn hình Figma và hiển thị biểu đồ sử dụng bãi đỗ/cổng sạc theo từng mốc thời gian.

### Chạy chức năng

```bat
dotnet run --project src\SmartEMobilityHub.Operator\SmartEMobilityHub.Operator.csproj
```

Trong cửa sổ Trung tâm điều hành, chọn **Mô phỏng & phân tích kịch bản** để mở What-if. Có thể chọn một trong năm nhóm kịch bản: tăng nhu cầu tại ga Metro, bãi đỗ đầy, tăng nhu cầu sạc, cổng sạc ngừng hoạt động hoặc phương tiện tập trung tại một Hub.

### Luồng sử dụng

1. Chọn kịch bản và Hub mục tiêu; nhập mức biến động nhu cầu, khung giờ, bước thời gian và số cổng sạc hỏng nếu phù hợp.
2. Chạy mô phỏng để xem dự báo theo thời gian, tóm tắt tác động, cảnh báo và các Hub bị ảnh hưởng.
3. Chọn **Mô phỏng** trên một phương án điều phối để tính lại kết quả và xem các đề xuất tiếp theo.
4. Dùng lịch sử để xem lại phiên, chạy lại từ trạng thái đầu vào hoặc xuất báo cáo PDF.

### Quy tắc và giới hạn chính

- Tham số được kiểm tra trước khi chạy: Hub phải tồn tại, mức biến động nhu cầu từ **−50% đến +200%**, khung mô phỏng tối đa **24 giờ**, bước thời gian từ **5 đến 120 phút**. Số cổng hỏng phải hợp lệ với kịch bản và Hub đã chọn.
- Biểu đồ biểu diễn phần trăm sử dụng bãi đỗ và cổng sạc từ mốc **0%**; thang tối thiểu đến **200%** và tự mở rộng khi dữ liệu dự báo cao hơn.
- Ngưỡng tải cao là **90%**; mức sử dụng bãi đỗ vượt **100%** được đánh dấu quá sức chứa và cảnh báo nghiêm trọng. Tóm tắt cảnh báo và biểu đồ lấy từ cùng kết quả mô phỏng.
- Mỗi lần chạy dùng snapshot và bản sao dữ liệu riêng. Mô phỏng phương án không ghi thay đổi vào trạng thái vận hành thật; kết quả và lịch sử chỉ phục vụ phân tích.
- Kết quả gồm dự báo theo bước thời gian, so sánh trước/sau theo Hub, số Hub bị ảnh hưởng, cảnh báo tải và phương án điều phối. Báo cáo PDF tóm tắt phiên và kết quả tương ứng.

### Thành phần mã nguồn

| Thành phần | Trách nhiệm |
|---|---|
| `src/SmartEMobilityHub.Core/Services/WhatIfController.cs` | Điều phối cấu hình, chạy mô phỏng, áp dụng đề xuất, lịch sử và xuất báo cáo. |
| `src/SmartEMobilityHub.Core/Services/WhatIfEngineService.cs` | Kiểm tra tham số, tính dự báo theo bước, đánh giá tác động và tạo/mô phỏng phương án điều phối trên snapshot. |
| `src/SmartEMobilityHub.Core/Domain/SimulationModels.cs` | Mô hình tham số, phiên chạy, điểm dự báo, tác động Hub và đề xuất. |
| `src/SmartEMobilityHub.Core/Store/TempStateRepository.cs` | Lưu snapshot tạm và lịch sử các phiên mô phỏng. |
| `src/SmartEMobilityHub.Operator/WhatIfWindow.xaml` | Giao diện cấu hình, biểu đồ, cảnh báo, phương án và lịch sử What-if. |
| `src/SmartEMobilityHub.Operator/ViewModels/WhatIfViewModel.cs` | Kết nối giao diện với controller, quản lý dữ liệu hiển thị và xuất báo cáo. |
