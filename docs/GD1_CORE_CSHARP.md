# GĐ1 — Core domain, Clock, NetworkStore, ports & seed (C#/.NET 10)

**Ngày:** 21/11/2026 · **Trạng thái:** ✅ Build + 7/7 seed check đạt — chờ Vỹ xác nhận để sang GĐ2.

---

## 1. Quyết định kiến trúc sau câu trả lời Q1

App của nhóm là **C#/WPF (.NET 10)**, nên phần TypeScript trong kế hoạch GĐ0 **không thể tích hợp**. Tôi chuyển sang C# và giữ nguyên tinh thần thiết kế:

| Yêu cầu ban đầu | Cách hiện thực trong C# |
|---|---|
| `src/core` thuần, không phụ thuộc UI | `src/SmartEMobilityHub.Core` — class library `net10.0`, **không** tham chiếu WPF |
| Clock inject được, cấm `Date.now()` | `IClock` + `SystemClock` + `FakeClock` + `ScaledClock`; `DateTime.Now` chỉ nằm trong `SystemClock` |
| `NetworkStore` + `snapshot()` deep clone | `NetworkStore` (`Snapshot()` clone sâu; UI tự gọi lại sau mỗi hành động, không dùng event) |
| Ports & Adapters | 5 interface trong `Ports/`; adapter giả lập ở GĐ2 |
| Seed hero + deterministic | `Seed/` — 12 Hub, 24 sự cố, 5 kỹ thuật viên |
| 18 test case + báo cáo | `tests/SmartEMobilityHub.Tests` — console runner (nhóm cấm NuGet nên không dùng xUnit) |

**Vì sao là project Core riêng thay vì sửa thẳng `MockDataStore` của app?**

1. App sinh viên (4.800 dòng) đang có seed riêng (`H1..H4`, KTX Khu A **đầy 100%** để demo UC-07, EM-2060 cũ 14 phút, BC-1187). Màn hình vận hành cần HUB-08 **đúng 90%** và đúng 1 cảnh báo quá tải — gộp hai bộ seed sẽ phá demo của Trường hoặc sai KPI của Figma.
2. Theo thiết kế Sub 2 §6, dashboard vận hành là **một ứng dụng desktop riêng**, không phải màn hình trong khung điện thoại 390×844.
3. Core là **hợp đồng dùng chung**: Đức (What-if) dùng `Snapshot()`, Trường implement `IReservationPort`, Nhiên đọc `Alert`/`HubStatus`; sau này app sinh viên có thể tham chiếu Core mà không phải viết lại.
4. Không sửa một dòng logic nào của app nhóm → build của nhóm vẫn xanh.

> **Điểm cần Vỹ quyết ở GĐ7:** hai app hiện có hai bộ dữ liệu mô phỏng độc lập (app sinh viên `H1..H4`, dashboard `HUB-01..12`). Cách demo hợp lý: coi app sinh viên là "mobile app" và dashboard là "operator web app" cùng chạy trên một backend giả lập; nếu nhóm muốn **dùng chung một store** thì phải chuyển app sinh viên sang đọc Core (việc này lớn hơn và cần Trường đồng ý).

---

## 2. Cấu trúc đã tạo

```
SmartEMobilityHub_StudentApp/            ← repo git của nhóm
├─ src/SmartEMobilityHub.Core/           ← MỚI (net10.0, không WPF)
│  ├─ Domain/  Enums, GeoPoint, MobilityHub, Vehicle, ChargingPoint,
│  │           Reservation, IncidentRecord (+log), MaintenanceTask, Technician,
│  │           Alert, ChargingModels, Operator, NetworkSnapshot, ServiceConstants, Errors
│  ├─ Clock/   Clock.cs (IClock, SystemClock, FakeClock, ScaledClock)
│  ├─ Store/   NetworkStore.cs
│  ├─ Ports/   IIoTSensorGateway, INotificationService, IReservationPort,
│  │           ISystemLogger, IScheduler, Telemetry
│  └─ Seed/    HubSeed, VehicleSeed, ReservationSeed, IncidentSeed,
│              TechnicianSeed, NetworkSeed
├─ tests/SmartEMobilityHub.Tests/        ← MỚI (console runner, không NuGet)
│  ├─ TestRunner.cs, Assert.cs, SeedSmokeTests.cs, Program.cs
├─ SmartEMobilityHub.csproj              ← sửa 6 dòng: loại src/** và tests/** khỏi glob
├─ SmartEMobilityHub.slnx                ← thêm 2 project
└─ (App.xaml, Models/, Services/, Screens/, Ui/… giữ nguyên 100%)
```

**Không sửa** bất kỳ file nào trong `Models/`, `Data/`, `Services/`, `Screens/`, `Ui/` của app.

---

## 3. Contract đã freeze (cho nhóm review)

### Enum (`Domain/Enums.cs`, Bảng 27)

`DeviceStatus` (Available, Reserved, InUse, Charging, NeedsCharge, UnderCheck, Locked, UnderRepair, Faulty, Offline, Relocating) · `HubStatus` (Online, Offline, Suspended) · `ConnectionStatus` · `IncidentStatus` · `IncidentSource` · `PriorityLevel` · `AlertType` · `MaintenanceStatus` · `ReservationStatus` · `IncidentObjectType` · `VehicleType` · `PowerCommandOutcome`.

C# dùng PascalCase theo quy ước của nhóm; ánh xạ 1–1 với UPPER_SNAKE trong Sub 3 (`IN_REPAIR` → `IncidentStatus.InRepair`).

### Store

| Thành viên | Ý nghĩa |
|---|---|
| `Hubs`, `Vehicles`, `Reservations`, `Incidents`, `Technicians`, `MaintenanceTasks`, `Alerts`, `ChargingRequests`, `Operators` | Danh sách sống (service ghi trực tiếp, giống `MockDataStore` của nhóm) |
| `Snapshot()` | Clone sâu toàn bộ — Đức dùng cho What-if, sửa snapshot không ảnh hưởng store |
| `FindHub/GetHub/FindVehicle/FindChargingPoint/FindIncident` | Truy vấn |
| `NextAlertId/NextTaskId/NextIncidentId` | ID tuần tự, không dùng thời gian; `NextIncidentId` đi sau mã lớn nhất đang có |

### Ports (GĐ2 sẽ có adapter giả lập)

`IIoTSensorGateway.FetchSensorTelemetry(hubId)` / `SendPowerCommand(portId, kw)` · `INotificationService.PushToOperator(alert)` / `NotifyUser` / `BroadcastToHubAffectedUsers` · `IReservationPort.FindExpiredReservations(now, holdMinutes)` / `FindUpcomingByHub` / `CancelReservation` / `ExtendHold` / `MarkPickedUp` · `ISystemLogger` · `IScheduler.Schedule(interval, action)`.

---

## 4. Seed — số liệu thực tế

| Hạng mục | Giá trị |
|---|---|
| Hub | **12** (HUB-01 Metro ĐHQG 40/60; HUB-02 KTX A; HUB-03 Nhà học A1; HUB-04 Thư viện; HUB-05…07; **HUB-08 KTX B 54/60, 8 cổng**; HUB-09…12) |
| Cổng sạc | 60 cổng; HUB-08: 01,02,03,05,06 **Charging**, 08 **Available**, 07 **Faulty 72°C**, 04 **Offline, cũ 14 phút** |
| Xe | 46 xe, gồm hero **XE-118 (18%, NeedsCharge, HUB-03)**, **XE-204 (Faulty)**, **XE-087 (UnderCheck)**, **XE-062 (Faulty phanh)**; xe Charging được gán cổng tự động nên trạng thái xe/cổng không mâu thuẫn |
| Sự cố | **24**: Mới **12** (8 sinh viên + 4 IoT) · Kiểm tra **7** (5 xe + 2 cổng) · Sửa chữa **5**; đủ 7 mã hero SC-1011/1014/1017/1019/1022/1023/**1024 CRITICAL** |
| Kỹ thuật viên | T-01…T-05; **T-02 Nguyễn Minh Tuấn** (Đội KT 02, sẵn sàng, **0,8 km** tới HUB-08); **T-03 đang bận** |
| Reservation | **R-101** (16 phút, phải huỷ) · **R-102** (14 phút, giữ) · **R-103** (16 phút, `IsPickingUp`) · **5 reservation sắp tới tại HUB-08** |
| Operator | OP-01 **Hoàng Vỹ – Quản trị vận hành**, Ca sáng 07:00–15:00 |

Ghi chú thiết kế: `OccupiedParkingSlots` là **bộ đếm độc lập**, không suy ra từ danh sách `Vehicle` — nhờ vậy 54/60 đúng mà không cần tạo 54 vật thể, và app sinh viên không bị phình danh sách xe (xem DC-25).

---

## 5. Kiểm chứng đã chạy

```
dotnet build SmartEMobilityHub.slnx   → Build succeeded. 0 Warning(s), 0 Error(s)
dotnet run --project tests/...        → 7/7 test đạt
```

| Mã | Nội dung | Kết quả |
|---|---|---|
| SMOKE-01 | Đủ 12 Hub, mã không trùng | PASS |
| SMOKE-02 | HUB-08 54/60, 8 cổng đúng trạng thái/nhiệt độ/độ cũ | PASS |
| SMOKE-03 | 24 sự cố đúng phân bố 12/7/5 và 8 SV + 4 IoT, 5 xe + 2 cổng | PASS |
| SMOKE-04 | Đủ 7 mã hero, SC-1024 CRITICAL | PASS |
| SMOKE-05 | XE-118 18%, T-02 cách 0,8 km, T-03 bận, R-101/102/103, 5 reservation HUB-08 | PASS |
| SMOKE-06 | Snapshot là clone sâu (sửa xe/cổng/sự cố trong snapshot không đổi store) | PASS |
| SMOKE-07 | FakeClock tua thời gian | PASS |

**Chưa kiểm:** chưa chạy app WPF (không có thay đổi UI ở GĐ1); các service GĐ2–GĐ4 chưa tồn tại nên chưa test use case.

### Môi trường
Máy chưa có .NET SDK (chỉ có runtime 10.0.11). Tôi đã cài SDK **cục bộ** vào `./.dotnet/` ở thư mục workspace (ngoài repo git, ~300 MB, xoá được). Lệnh build dùng `../.dotnet/dotnet`. Khi Vỹ mở bằng Visual Studio thì chỉ cần cài .NET 10 SDK như README của nhóm.

---

## 6. Sai lệch & giả định mới

Đã ghi vào `docs/DESIGN_CHANGES.md`: DC-01…DC-05, DC-09, DC-10, DC-13, DC-17, DC-19 (áp dụng) và **DC-20…DC-26** (C#, project Core riêng, test runner console, bộ đếm chỗ đỗ, HoldUntil).

Giả định dùng ở GĐ1: A1–A6 của PLAN + A9 (SLA tính từ lúc tạo/gán), A13 (utilization = max(parking, charging), loại cổng Offline khỏi mẫu số), A12 (biên dùng `>`).

---

## 7. Vỹ cần tự hiểu/giải thích được

- **Vì sao `DateTime.Now` chỉ được nằm trong `SystemClock`:** service nhận `IClock`, nên test 14/16 phút và demo auto-cancel 15 phút chạy tức thì; nếu service tự đọc giờ hệ thống thì không test được biên.
- **Vì sao chỗ đỗ là bộ đếm, không phải danh sách xe:** 54/60 là con số vận hành (gồm cả xe cá nhân đang gửi), không phải 54 đối tượng `Vehicle`; tách ra thì seed nhẹ và app sinh viên không bị 46 xe đổ vào một Hub.
- **Snapshot khác gì store:** `Snapshot()` clone sâu từng Hub/xe/cổng/sự cố, nên What-if của Đức sửa bản clone mà không làm bẩn dữ liệu vận hành (NFR 2.2.2).
- **Vì sao Core tách khỏi app WPF:** để `Core` không kéo WPF, chạy được trong test console và là hợp đồng chung; app chỉ tham chiếu khi cần.
- **PascalCase vs UPPER_SNAKE:** C# dùng `IncidentStatus.InRepair`, Sub 3 viết `IN_REPAIR` — cùng một giá trị, chỉ khác quy ước ngôn ngữ; khi bảo vệ phải nói rõ ánh xạ này.

---

## 8. Việc còn lại

- **GĐ2:** `MockIoTSensorGateway` (fault injection: `FailNext`, `SetStale`, `SetNetworkDown`), `HubStatusSyncService`, `ThresholdMonitor`, `AlertRetryQueue` + `Alert`, adapter `InAppNotificationService`, `InMemoryReservationAdapter`.
- **GĐ3:** `AutoCancelService` + `StatusUpdaterTimer` (6 bước, try/catch riêng, `TickNow()`, `GetNextTickInSeconds()`).
- **GĐ4:** `SmartChargingScheduler` + `IncidentManager`.
- **GĐ5:** 18 test case + `results.md/json`.
- **GĐ6:** màn hình Incident (cửa sổ WPF riêng, đọc Core).
- **GĐ7:** README, kịch bản demo, commit/push dưới tên Vỹ (không attribution AI).
