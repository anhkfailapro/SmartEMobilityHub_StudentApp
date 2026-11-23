# Smart E-Mobility Hub

Smart E-Mobility Hub is a demo system for shared electric bikes and motorbikes, parking and charging hubs on a university campus.
It is the CO3001 (Software Engineering) project of Group 21.

The repository contains one Visual Studio solution with four projects:

| Project | Type | What it is |
|---|---|---|
| `SmartEMobilityHub` (repository root) | WPF app | **Student app.** Phone-style app for students and private EV owners: find a vehicle, reserve, pick up, ride, return, request or schedule charging, report problems, view history. |
| `src/SmartEMobilityHub.Core` | Class library | **Core services.** Hub status sync, overload alerts, auto-cancel of expired reservations, smart charging scheduling, incident management, capacity limits. No UI. |
| `src/SmartEMobilityHub.Operator` | WPF app | **Operator dashboard.** Control centre for incidents and maintenance, built on the Core library. |
| `tests/SmartEMobilityHub.Tests` | Console app | **Test runner** for the Core library. Writes a test report after each run. |

All data is in memory and generated at start-up. There is no backend, no database, no real payment and no real GPS/IoT device; these are simulated.
The UI language is Vietnamese.

---

## 1. Requirements

| Item | Version |
|---|---|
| OS | Windows 10 or Windows 11 |
| IDE | Visual Studio 2026, or Visual Studio 2022 version 17.14 or newer, with the workload **".NET desktop development"** |
| SDK | **.NET 10 SDK** — check with `dotnet --list-sdks` (a line starting with `10.` must exist) |

No NuGet packages are used. Nothing else needs to be installed.
Optional: install the **Roboto** font for the intended look of the student app. Without it, Segoe UI is used.

## 2. Build and run

### Visual Studio
1. Open `SmartEMobilityHub.slnx`.
2. In Solution Explorer, right-click the project you want to run → **Set as Startup Project**:
   - `SmartEMobilityHub` – student app
   - `SmartEMobilityHub.Operator` – operator dashboard
   - `SmartEMobilityHub.Tests` – test runner
3. Press **F5**.

To start the student app and the dashboard together: right-click the solution → **Configure Startup Projects** → **Multiple startup projects**.

### Command line (from the repository root)
```bat
rem Student app
dotnet run --project SmartEMobilityHub.csproj

rem Operator dashboard
dotnet run --project src\SmartEMobilityHub.Operator\SmartEMobilityHub.Operator.csproj

rem Tests (exit code 0 = all passed)
dotnet run --project tests\SmartEMobilityHub.Tests\SmartEMobilityHub.Tests.csproj
```

---

## 3. Student app

The window shows a **phone screen (390 × 844)** on the left and a **Demo panel** on the right. The window can be resized; the content scales.

- **Phone screen:** the app itself. Bottom tabs: Trang chủ (Home), Đặt chỗ (Reservations), Chuyến đi (Trip), Thông báo (Notifications), Tài khoản (Account).
- **Demo panel:** not part of the app. It simulates what real devices and time would do:
  - **Thời gian demo** – move the clock forward (+1 / +5 / +15 / +30 minutes) to test hold expiry, late-cancel fees and reminders.
  - **Công tắc tình huống** – switches that force an alternative path (for example "Hub trả xe hết chỗ" = return hub is full).
  - **Sự kiện hệ thống** – one-click system events: stale vehicle data, charging point assigned, charging point broken.
  - **Đặt lại toàn bộ demo** – reset all data.
- There is no login. The app opens as demo user **Minh**, who is both a student and a private EV owner.

### Use cases and how to show them

| Use case | Normal flow | Alternative paths and how to trigger them |
|---|---|---|
| UC-01 Plan journey | Home → type a destination (or tap a suggestion) → **Tìm** → *Chọn Hub xuất phát* → vehicle list | Tap a hub card on Home · Demo panel "Dịch vụ bản đồ không phản hồi" (map service timeout → hubs by straight-line distance) · *Xem tất cả* → *Hub Nhà văn hóa SV* (no vehicle → 3 nearest hubs) |
| UC-02 Vehicle status | Vehicle list → tap a vehicle | Chip "Pin > 50%" · EM-2060 (data 14 min old) or Demo panel "Làm cũ dữ liệu EB-1024" → stale warning · EM-2060 (faulty) / EB-1031 (reserved) → read-only, *Đặt trước* disabled |
| UC-03 Reserve vehicle | Detail → *Đặt trước* → *Xác nhận đặt* → 15-minute countdown + pick-up code | Demo panel "SV khác đặt xe trước" · reserve a second vehicle (limit 1) · *Hủy* on the confirm screen · Demo panel "+15 phút" (hold expires) |
| UC-04 Reserve parking | Home → *Đặt chỗ đỗ xe* → fill form → *Xác nhận đặt chỗ* → slot + entry QR | Switch "Đến ngay" · Demo panel "Hub đỗ xe đã chọn hết chỗ" → hubs within 1 km · a second parking reservation (limit 1) |
| UC-05 Cancel / change time | Tab *Đặt chỗ* → *Hủy* → confirm | *Đổi giờ* on a parking or charging booking (Demo panel "Khung giờ mới không còn chỗ" = old time kept) · cancel the vehicle reservation after pick-up (refused) · parking with *Đến ngay*, then cancel → late fee 10.000đ |
| UC-06 Pick up | Reservation → *Quét QR để nhận xe* → *Giả lập quét (đúng xe)* → unlocking → trip starts | *Nhập mã thủ công* (screen shows demo codes) · *Quét nhầm xe khác* (2 more attempts) · clock past the hold time · Demo panel "Xe không xác nhận mở khóa" → incident + support contact · Demo panel "Sinh viên chưa ở Hub" |
| UC-07 Return | Tab *Chuyến đi* → *Trả xe* → choose hub → *Xác nhận trả xe* → invoice | Demo panel "Xe không nằm trong Hub" · return at *KTX Khu A* (full) or Demo panel "Hub trả xe hết chỗ" → nearest hub + 10 free minutes · monthly-plan free minute is deducted |
| UC-08 Charging request | Trip → *Yêu cầu sạc* → *Xác nhận yêu cầu sạc* → queue position; Demo panel "Cấp cổng sạc" sends "charging point assigned" | Demo panel "Tất cả cổng sạc đang bận" · *Chọn Hub khác* · *Kết thúc chuyến, đặt xe mới và yêu cầu sạc* · submit twice for the same vehicle (duplicate refused) |
| UC-09 Schedule charging (private EV) | Home → *Lên lịch sạc* → fill form → *Xác nhận lịch sạc* → booking detail; reminder 30 minutes before | Switch "Sạc sớm nhất có thể" · Demo panel "Không còn cổng sạc trong khung giờ" → alternative windows · 150 minutes at *Thư viện Trung tâm* (hub limit 120) · Demo panel "Cổng sạc đã đặt bị hỏng" (booking moved or cancelled) |
| UC-10 Report problem | Trip → *Báo lỗi* (or trip summary → *Báo lỗi xe*) → fill form → *Gửi báo cáo* → report code | Report the vehicle you are riding → "Kết thúc chuyến và báo lỗi" · photo larger than 5 MB (or "Demo: thử chọn ảnh 7,2 MB") · report **EB-1031** (already has open incident BC-1187 → report is linked) |
| UC-11 Notifications and history | Home → *Hoạt động* (or tab *Thông báo*) → *Thông báo* / *Lịch sử* → tap an entry → detail + invoice | Notifications are newest first and marked as read · Demo panel "Không có lịch sử 3 tháng" → empty state |

The **Tài khoản** tab shows the registered private vehicles (read-only), the remaining monthly-plan minutes and a **dark mode** switch.

### Business rules used

| Rule | Value |
|---|---|
| Shared-vehicle hold time | 15 minutes |
| Active reservations per user | 1 vehicle, 1 parking, 1 charging booking |
| Vehicle data shown as stale | older than 10 minutes |
| Late-cancel fee | 10.000đ if cancelled less than 5 minutes before start |
| Unlock confirmation timeout | 30 seconds |
| Wrong vehicle at pick-up | 2 more attempts |
| Extra free time when return hub is full | 10 minutes |
| Vehicle becomes "Cần sạc" (needs charge) | battery below 20 % |
| Rental price | 1.500đ per started minute |
| Charging reminder | 30 minutes before start |
| Problem photo size | up to 5 MB |
| History period | last 3 months |
| Charging priority label (score 0–100) | 0–24 Thấp · 25–49 Trung bình · 50–74 Cao · 75–100 Khẩn cấp |

All prices and limits are in `Data/DemoConfig.cs`.

### Not simulated in the student app
- Payment failure and "Pay now" (payment always succeeds).
- Parking grace time, no-show and extension; deposit for charging bookings.
- QR codes are drawn placeholders and cannot be scanned.

---

## 4. Operator dashboard

A control-centre window (1520 × 940, minimum 1240 × 760) with three columns:

| Column | Content |
|---|---|
| Left | 24 incidents with code, device, priority, status and time; filter chips (Tất cả / Mới / Kiểm tra / Đang sửa / Đã xong); search by code, device, fault or hub |
| Middle | Incident detail: description, reporter, time; action buttons; maintenance task (technician, SLA left or overdue); processing log |
| Right | KPIs (new / checking-locked / in repair / done), overloaded hubs, alerts; 5 technicians with availability and distance; charging queue of HUB-08; recent alerts; demo scenario buttons; system log |

The header shows the simulated clock, "updated N seconds ago · 30-second cycle" and a **Quét ngay** (scan now) button.

### Incident actions

| Button | Enabled when | Result |
|---|---|---|
| Khoá thiết bị (lock device) | incident New / Checking | device and incident become Locked |
| Tạo phiếu bảo trì (create maintenance task) | incident not closed | task created with SLA deadline by priority |
| Gán KTV gần nhất (assign nearest technician) | incident not closed or resolved | creates the task if needed, assigns the nearest available technician |
| Hoàn tất xử lý (finish repair) | incident In repair | In repair → Resolved |
| Đóng sự cố (close incident) | incident Resolved | Resolved → Closed |
| Tạm dừng Hub (suspend hub) | hub not suspended | asks for confirmation, suspends the hub, cancels its upcoming reservations, notifies users |

Invalid actions show a Vietnamese message box; the app does not crash.

### Demo scenario buttons

| Button | Effect |
|---|---|
| Cổng CS-08-08 lỗi | charging point fault → point Faulty, sensor incident created |
| Mất kết nối HUB-03 | hub and its 4 points go Offline, connection-loss incident created |
| Dữ liệu cũ HUB-08 | hub data set 14 minutes old → stale-data alert |
| Mạng cảnh báo lỗi | alert channel down → HUB-08 overload alert is queued |
| Khôi phục & gửi lại | channel back, clock +31 s → queued alerts are resent |
| Sự cố mở khoá | unlock-timeout incident for vehicle XE-121, reservation R-103 |
| Thêm 3 yêu cầu sạc | 3 charging requests at HUB-08, scheduler assigns points |
| +1 / +5 / +15 phút | move the simulated clock |
| Tự động quét | turn the 30-second automatic scan on/off |
| Tăng tốc ×10 | 1 real second = 10 simulated seconds |
| Đặt lại demo | rebuild all data, services and clock |

### Suggested 5-minute demo
1. Select **SC-1024** (HUB-08, Khẩn cấp) and read the description and log.
2. **Tạo phiếu bảo trì** → task MT-0001 with a 30-minute deadline.
3. **Gán KTV gần nhất** → technician T-02; incident becomes In repair.
4. **Hoàn tất xử lý**, then **Đóng sự cố** → KPI "Đã xong" increases.
5. Select **SC-1017** (XE-087) → **Khoá thiết bị**.
6. **Cổng CS-08-08 lỗi** → a new incident and a "charging point fault" alert appear.

---

### What-if simulation (Nguyễn Minh Đức)

The operator dashboard includes a What-if simulation workflow for forecasting hub demand and evaluating dispatch actions. It supports five scenario families: a Metro demand surge, a full parking hub, a charging demand surge, a charger outage, and vehicle concentration at one hub.

To open it, run the operator dashboard and select **Mô phỏng & phân tích kịch bản**:

```bat
dotnet run --project src\SmartEMobilityHub.Operator\SmartEMobilityHub.Operator.csproj
```

#### Workflow

1. Select a scenario and target hub; configure demand change, the analysis window, time-step size, and failed-charger count where applicable.
2. Run the simulation to view the time-step forecast, hub impact summary, overload warnings, and dispatch recommendations.
3. Select **Mô phỏng** beside a recommendation to recalculate the scenario and review the updated result and recommendations.
4. Browse simulation history, rerun from the same input snapshot, or export the result as a PDF report.

#### Rules and behavior

- Input validation requires an existing hub, demand change from **−50% to +200%**, an analysis window no longer than **24 hours**, and time steps from **5 to 120 minutes**. Charger-outage scenarios also validate the number of disabled ports against the selected hub.
- The chart starts at **0%**, uses a minimum vertical scale of **200%**, and expands when a forecast exceeds that scale. Parking and charging utilization are plotted from the forecast points.
- **90%** is the high-load threshold. Parking utilization above **100%** indicates demand beyond the hub's parking capacity and is treated as critical. The chart and warning summary use the same simulation result.
- Each run uses an isolated snapshot and cloned state. Applying a recommendation reruns the simulation without changing operational state.
- Results include the time-step forecast, before/after hub impacts, affected and overloaded hub counts, recommendations, and a PDF export.

#### Main implementation files

| File | Responsibility |
|---|---|
| `src/SmartEMobilityHub.Core/Services/WhatIfController.cs` | Configures and runs sessions, applies recommendations, manages reruns/history, and exports reports. |
| `src/SmartEMobilityHub.Core/Services/WhatIfEngineService.cs` | Validates parameters, calculates forecasts, evaluates impacts, and generates/simulates recommendations on snapshots. |
| `src/SmartEMobilityHub.Core/Domain/SimulationModels.cs` | Defines simulation parameters, sessions, forecast points, hub impacts, and recommendations. |
| `src/SmartEMobilityHub.Core/Store/TempStateRepository.cs` | Stores temporary simulation snapshots and session history. |
| `src/SmartEMobilityHub.Operator/WhatIfWindow.xaml` | What-if configuration, chart, warnings, recommendations, and history UI. |
| `src/SmartEMobilityHub.Operator/ViewModels/WhatIfViewModel.cs` | Connects UI state to the controller and exposes simulation results and PDF export. |
## 5. Core services

`src/SmartEMobilityHub.Core` holds the business logic shared by the dashboard and the tests. It does not depend on WPF.

| Service | Responsibility |
|---|---|
| `StatusUpdaterTimer` | Runs every 30 seconds: sync hub status → check stale data → check thresholds → smart charging → auto-cancel → resend queued alerts. Each step is isolated, so one failure does not stop the others. |
| `HubStatusSyncService` | Reads telemetry for every hub; a lost device goes Offline without blocking other hubs. |
| `ThresholdMonitor` | Overload alert when a hub reaches 90 % of capacity (54/60 = alert, 53/60 = no alert). |
| `AlertRetryQueue` | Queues alerts when the channel is down and resends them after 30 seconds. |
| `AutoCancelService` | Cancels reservations held longer than 15 minutes and releases the resource; extends the hold if the user is picking up. |
| `SmartChargingScheduler` | Ranks charging requests by battery level, needed time and peak demand; respects hub power limits; moves vehicles from faulty points. |
| `IncidentManager` | Receives student, sensor and unlock-timeout reports; links duplicates; lock device, maintenance task, technician assignment, suspend hub. |
| `VehicleRedistributor` | Finds hubs with too many or too few vehicles, suggests dispatch orders, and executes or completes them. |

Folders: `Domain` (entities and enums), `Ports` (interfaces), `Adapters` (in-memory and mock implementations), `Seed` (12 hubs, vehicles, 24 incidents, technicians, reservations), `Store`, `Clock` (`SystemClock`, `FakeClock` for tests, and a scaled clock that speeds up simulated time for demos). Services never read `DateTime.Now` directly, so time-based rules can be tested.

## 6. Tests

`tests/SmartEMobilityHub.Tests` is a console test runner (no test framework, no NuGet). Each test builds a fresh in-memory world with a fixed `FakeClock`, so results are repeatable.

| Group | Count | Covers |
|---|---|---|
| Seed smoke tests | 7 | seed data, snapshot copy, clock |
| TCSYS02 – hub status sync | 3 | 30-second cycle, lost device, stale data |
| TCSYS03 – overload alerts | 3 | 90 % threshold, alert queue and resend |
| TCSYS04 – auto-cancel | 3 | 15-minute limit, boundary, pick-up in progress |
| TCSYS01 – smart charging | 4 | ranking, weight validation, power limit, faulty point |
| TCOP03 – incident management | 5 | student report, duplicate link, lock + task + technician, suspend hub, unlock timeout |
| Boundary unit tests | 5 | thresholds, stale data, priority score, SLA |

Each run prints the results and writes `tests/report/results.md` and `results.json`. These report files are generated locally and are not committed. The exit code is 0 when all tests pass.

---

## 7. Repository structure

```
SmartEMobilityHub.slnx              solution (4 projects)
SmartEMobilityHub.csproj            student app (WPF)
App.xaml, MainWindow.xaml(.cs)      student app window: phone frame + Demo panel
DemoApp.cs                          student app wiring: services, clock, timer, navigation
Models/                             student app domain classes
Data/                               DemoConfig (prices, limits, labels), DemoClock, DemoToggles, MockDataStore
Services/                           JourneyPlanner, VehicleManager, ReservationService, ParkingService, PickUpService,
                                    TripBillingService, ChargingService, BookingService, IncidentService,
                                    NotificationService, HistoryService
Ui/                                 Theme (light/dark), UiKit (shared controls), AppShell (navigation, dialogs), DemoPanel
Screens/                            one class per student app screen
src/SmartEMobilityHub.Core/         core services library
src/SmartEMobilityHub.Operator/     operator dashboard (WPF, MVVM)
tests/SmartEMobilityHub.Tests/      test runner
docs/                               project documents
```

### Architecture notes
- **Student app:** Screen → Service → in-memory store. Screens are C# classes built with the shared `UiKit`, so all screens share one style.
- **Operator dashboard:** MVVM (`ViewModels/MainViewModel.cs`) on top of the Core services.
- The student app and the dashboard each use their own demo data. The student app is not connected to the Core library yet; connecting it means adding a project reference to Core and mapping the vehicle and point status enums.

## 8. Team

CO3001 – Software Engineering, Group 21 · Ho Chi Minh City University of Technology (HCMUT), VNU-HCM.
Supervisor: Nguyen Minh Tam.

| Student ID | Member |
|---|---|
| 2310785 | Nguyễn Minh Đức |
| 2414064 | Châu Hoàng Vỹ |
| 2412520 | Dương Phúc Tất Nhiên |
| 2313698 | Chu Quang Trường |
| 2411035 | Đào Văn Trọng Hiệp |
