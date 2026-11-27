# Smart E-Mobility Hub

Smart E-Mobility Hub is a demo system for shared electric bikes and motorbikes, parking and charging hubs on a university campus.
It is the CO3001 (Software Engineering) project of Group 21.

The repository contains one Visual Studio solution with four projects:

| Project | Type | What it is |
|---|---|---|
| `SmartEMobilityHub` (repository root) | WPF app | **Student app.** Phone-style app for students and private EV owners: find a vehicle, reserve, pick up, ride, return, request or schedule charging, report problems, view history. |
| `src/SmartEMobilityHub.Core` | Class library | **Core services.** Hub status sync, overload alerts, auto-cancel of expired reservations, smart charging scheduling, incident management, capacity limits. No UI. |
| `src/SmartEMobilityHub.Operator` | WPF app | **Operator dashboard.** Control centre for incidents and maintenance, built on the Core library. |
| `tests/SmartEMobilityHub.Tests` | Console app | **Test runner** for the Core library and the student app logic (models, data, services). Writes a test report after each run. |

All data is in memory and generated at start-up. There is no backend, no database, no real payment and no real GPS/IoT device; these are simulated.

**One network for both apps.** The student app and the operator dashboard read and write the same in-memory network (`NetworkSession` in Core: 12 hubs, vehicles, charging points, reservations, incidents, charging queues, one simulated clock). What a student does is the operator's data, and what the operator does is what the student sees — there is no copy and no synchronisation step. Only data that exists only in the student app (trips and invoices, private vehicles, parking bookings, private EV charging bookings, notifications, monthly plan) is kept by the student app itself.
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
- The app opens on a **login screen** (student ID + password). Demo account: **2310001 / sv123456** → user **Minh**, who is both a student and a private EV owner. *Tài khoản → Đăng xuất* returns to the login screen (reservations and trips are kept).
- Login alternative paths: empty field · wrong ID or password (same message, shows attempts left) · 5 wrong attempts lock the account for 5 minutes (use the Demo panel clock to unlock) · an operator account is refused on the student app.

### Use cases and how to show them

| Use case | Normal flow | Alternative paths and how to trigger them |
|---|---|---|
| UC-01 Plan journey | Home → type a destination (or tap a suggestion) → **Tìm** → *Chọn Hub xuất phát* → vehicle list | Tap a hub card on Home · Demo panel "Dịch vụ bản đồ không phản hồi" (map service timeout → hubs by straight-line distance) · *Xem tất cả* → *Hub Nhà văn hóa SV* (no vehicle → 3 nearest hubs) |
| UC-02 Vehicle status | Vehicle list → tap a vehicle | Chip "Pin > 50%" · EM-2060 (data 14 min old) or Demo panel "Làm cũ dữ liệu EB-1024" → stale warning · EM-2060 (faulty) / EB-1031 (held by another student for the first 12 minutes) → read-only, *Đặt trước* disabled |
| UC-03 Reserve vehicle | Detail → *Đặt trước* → *Xác nhận đặt* → 15-minute countdown + pick-up code | Demo panel "SV khác đặt xe trước" · reserve a second vehicle (limit 1) · *Hủy* on the confirm screen · Demo panel "+15 phút" (hold expires) |
| UC-04 Reserve parking | Home → *Đặt chỗ đỗ xe* → fill form → *Xác nhận đặt chỗ* → slot + entry QR | Switch "Đến ngay" · Demo panel "Hub đỗ xe đã chọn hết chỗ" → hubs within 1 km · a second parking reservation (limit 1) |
| UC-05 Cancel / change time | Tab *Đặt chỗ* → *Hủy* → confirm | *Đổi giờ* on a parking or charging booking (Demo panel "Khung giờ mới không còn chỗ" = old time kept) · cancel the vehicle reservation after pick-up (refused) · parking with *Đến ngay*, then cancel → late fee 10.000đ |
| UC-06 Pick up | Reservation → *Quét QR để nhận xe* → *Giả lập quét (đúng xe)* → unlocking → trip starts | *Nhập mã thủ công* (screen shows demo codes) · *Quét nhầm xe khác* (2 more attempts) · clock past the hold time · Demo panel "Xe không xác nhận mở khóa" → unlock cancelled, reservation cancelled (no fee), vehicle kept Reserved for checking, incident in the operator queue + support contact · Demo panel "Sinh viên chưa ở Hub" |
| UC-07 Return | Tab *Chuyến đi* → *Trả xe* → choose hub → *Xác nhận trả xe* → invoice | Demo panel "Xe không nằm trong Hub" · return at a hub that is really full, or Demo panel "Hub trả xe hết chỗ" → nearest hub + 10 free minutes · monthly-plan free minute is deducted |
| UC-08 Charging request | Trip → *Yêu cầu sạc* → *Xác nhận yêu cầu sạc* → the Core scheduler assigns a free port at once, or gives a queue position; Demo panel "Cấp cổng sạc" (a charging vehicle finishes, the port goes to the first request) sends "charging point assigned" | Demo panel "Tất cả cổng sạc đang bận" (other vehicles take the free ports, visible on the dashboard) · *Chọn Hub khác* · *Kết thúc chuyến, đặt xe mới và yêu cầu sạc* · submit twice for the same vehicle (duplicate refused) |
| UC-09 Schedule charging (private EV) | Home → *Lên lịch sạc* → fill form → *Xác nhận lịch sạc* → booking detail; reminder 30 minutes before | Switch "Sạc sớm nhất có thể" · Demo panel "Không còn cổng sạc trong khung giờ" → alternative windows · 150 minutes at *Thư viện Trung tâm* (hub limit 120) · Demo panel "Cổng sạc đã đặt bị hỏng" (booking moved or cancelled) |
| UC-10 Report problem | Trip → *Báo lỗi* (or trip summary → *Báo lỗi xe*) → fill form → *Gửi báo cáo* → report code | Report the vehicle you are riding → "Kết thúc chuyến và báo lỗi" · photo larger than 5 MB (or "Demo: thử chọn ảnh 7,2 MB") · report **EB-1031** (already has an open incident from yesterday → report is linked). The report code is the operator's incident code (SC-…) |
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

A control-centre window (1520 × 940, minimum 1240 × 760) with a **left sidebar** and six pages. The sidebar shows a live count (badge) next to each page and the 30-second scan settings at the bottom.

| Page | Content |
|---|---|
| **Tổng quan mạng lưới** (network overview) | 6 KPI tiles (hubs online, vehicles available, free charging points, overloaded hubs, open incidents, alerts); one card per hub with parking and charging utilisation bars (blue < 80 %, orange 80–89 %, red ≥ 90 %), available vehicles and free points; list of hubs at ≥ 80 % capacity; recent alerts. Click a hub card to open its vehicles and charging points. |
| **Sự cố & bảo trì** (incidents) | 25 incidents (24 operator scenarios + the student's earlier report on EB-1031) with filter chips and search; incident detail with actions, maintenance task and processing log; status KPIs and the 5 technicians. |
| **Xe & cổng sạc** (vehicles & charging points) | Table of every vehicle (connection, type/model, hub/slot, battery bar, status, last sensor update — older than 10 minutes is orange), sorted by lowest battery; filter by hub, status group and text. Grid of charging points per hub (free / charging / assigned / faulty / offline, vehicle, power used against the hub limit). |
| **Lịch sạc thông minh** (smart charging) | Charging queue of the selected hub, pin up/down, reschedule, the hub's charging-point grid, and the priority weights. |
| **Điều phối phương tiện** (dispatch) | Load balance of every hub (surplus / balanced / short of vehicles) and the dispatch form with the list of orders. |
| **Kịch bản demo & nhật ký** (demo & log) | Demo scenario buttons, simulated clock, auto-scan / ×10 speed, recent alerts and the system log. |

The **Mô phỏng What-if** button (header and sidebar) opens the What-if page inside the same window, in place of the current page; the header and sidebar stay visible. **← Quay lại** or any sidebar item returns to the page you were on, and reopening What-if keeps the last results.

A **login window** opens first. Only operator accounts can sign in — demo account **op01 / op123456** (OP-01 Hoàng Vỹ). The same rules apply as in the student app (5 wrong attempts → 5-minute lock; a student account is refused).

The header shows the simulated clock, "updated N seconds ago · 30-second cycle", a **Quét ngay** (scan now) button, the signed-in operator with role and shift, and **Đăng xuất** (log out → back to the login window).

### Light / dark mode

**☾ Chế độ tối / ☀ Chế độ sáng** in the header switches the whole dashboard (also the login window and the What-if page). Colours live in `Themes/Light.xaml` and `Themes/Dark.xaml`; every window uses `DynamicResource`, so the switch is immediate.

### Smart charging schedule (UC-OP-04)

The **Lịch sạc thông minh** page shows the charging queue of any hub (hub picker), ports in use, free ports and power used against the hub limit. The operator can:

- change the priority weights (low battery / needed soon / peak hour, sum = 1) — invalid values are refused and the old weights are kept;
- **Lập lịch lại** (reschedule) the selected hub;
- **▲ Ghim lên / ▼ Ghim xuống** to pin a waiting request to a position; pinned requests keep their place when the queue is re-ranked.

### Linked mode with the student app

In the student app, Demo panel → **Mở Trung tâm điều hành (dùng chung dữ liệu)** opens the operator login (**op01 / op123456**) and then the dashboard **in the same process, on the same `NetworkSession`** as the student app:

| Student app does | Operator sees |
|---|---|
| reserve / cancel a vehicle, hold expires | vehicle Reserved / Available, the student's reservation in the network |
| pick up, return at another hub | hub parking counters change, vehicle moves hub |
| report a problem | new incident in the queue (duplicates merged), device Under check |
| request charging | request in the hub's charging queue, scheduled within the power limit |
| reserve a parking slot | hub occupancy +1 |

| Operator does | Student app shows |
|---|---|
| suspend / resume a hub | active holds at that hub cancelled; hub marked "Tạm dừng", no new reservations, parking or charging; notification on suspend and resume |
| lock a vehicle / finish repair | vehicle becomes unavailable / available again |
| dispatch vehicles to another hub | vehicle appears at the new hub |
| charging port assigned (scan cycle or reschedule) | the student's request shows its port, with a notification |
| resolve the student's incident | the report is marked resolved, with a notification |

Hub and vehicle codes are the same in both apps (HUB-01 … HUB-12, EB-/EM-/XE-…). Both apps use one simulated clock: fast-forwarding in either app moves both. The student app finds out about operator changes with `Services/NetworkWatcher.cs` (it compares states between ticks to send notifications and redraw; it copies no data).

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

### Dispatch (page "Điều phối phương tiện")

**Gợi ý tự động** looks for a hub at ≥ 85 % parking use and a hub at ≤ 35 % and creates an order that moves real, available vehicles towards 65 %. If no hub is at ≤ 35 %, it accepts a receiving hub below 50 % so an overloaded hub can still be relieved (the status bar says so). **Tạo lệnh** creates an order from the form, **Xuất phát** starts it (vehicles become "Đang điều chuyển") and **Hoàn tất** completes it and updates both hubs.

### Demo scenario buttons (page "Kịch bản demo & nhật ký")

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
0. On **Tổng quan mạng lưới**, point out the KPI tiles and the red HUB-08 card (90 %); click it to see its vehicles and charging points. Then open **Sự cố & bảo trì**.
1. Select **SC-1024** (HUB-08, Khẩn cấp) and read the description and log.
2. **Tạo phiếu bảo trì** → task MT-0001 with a 30-minute deadline.
3. **Gán KTV gần nhất** → technician T-02; incident becomes In repair.
4. **Hoàn tất xử lý**, then **Đóng sự cố** → KPI "Đã xong" increases.
5. Select **SC-1017** (XE-087) → **Khoá thiết bị**.
6. **Kịch bản demo & nhật ký** → **Cổng CS-08-08 lỗi** → a new incident and a "charging point fault" alert appear; on **Xe & cổng sạc** (HUB-08) port #8 is shown as faulty.
7. **Điều phối phương tiện** → **Gợi ý tự động** → **Xuất phát** → **Hoàn tất**.

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
| `src/SmartEMobilityHub.Operator/Views/WhatIfPage.xaml` | What-if configuration, chart, warnings, recommendations, and history UI, embedded in the main window. |
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

Folders: `Domain` (entities and enums), `Ports` (interfaces), `Adapters` (in-memory and mock implementations), `Seed` (12 hubs, vehicles, 24 incidents, technicians, reservations; `CampusFleetSeed` adds the student-app fleet and scenario data to the shared network), `Store`, `Clock` (`SystemClock`, `FakeClock` for tests, `SimulationClock` shared by both apps: start time + real time + fast-forward, and a scaled clock). `NetworkSession` wires one store, one clock, one log and all services for both apps. Services never read `DateTime.Now` directly, so time-based rules can be tested. This also holds for the What-if engine and session store and for the dashboard log: they all use the simulated clock, so +15 minutes in the demo moves log times and What-if run times too.

`StatusUpdaterTimer` must be given a scheduler. Both the dashboard and the tests use `ManualScheduler` (the scan runs on the UI thread or when a test fires it), because `NetworkStore` is not thread-safe.

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
| REG – regression | 17 | bugs found in the knowledge-graph reviews (alert floods, stuck ports, technician lifecycle, reopen hub, simulated clock in What-if, required scheduler, …) |
| DSP – dispatch | 8 | only real available vehicles, target capacity, cancel / execute / complete orders |
| AUTH – login | 8 | correct login, empty fields, wrong password, 5-attempt lock, role check, hashed passwords, logout |
| LINK – student app ↔ operator | 10 | shared vehicles, reserve / cancel / pick up / return, hub suspension cancels holds, incident merge, charging queue and port release, parking occupancy |
| APP – student app on the shared network | 17 | every check looks at both sides: what the student sees and what the operator has in Core — 15-minute hold and expiry, one vehicle per student, wrong-vehicle attempts, pick up / return moving hub counters, fare (1.500đ per started minute, monthly plan), full return hub (+10 free minutes), parking occupancy and late-cancel fee, charging queue and port assignment, incident merge and resolve, hub suspend / resume, unlock timeout, operator lock / dispatch |
| MAP – status labels | 2 | Core vehicle status → student label, charging point codes |

The student app logic is tested without WPF: the test project compiles `Models/`, `Data/` and `Services/` directly (except `NetworkLink.cs`, which only opens the operator window) and runs them on a real `NetworkSession` with a `FakeClock`.

Each run prints the results and writes `tests/report/results.md` and `results.json`. These report files are generated locally and are not committed. The exit code is 0 when all tests pass.

---

## 7. Repository structure

```
SmartEMobilityHub.slnx              solution (4 projects)
SmartEMobilityHub.csproj            student app (WPF)
App.xaml, MainWindow.xaml(.cs)      student app window: phone frame + Demo panel
DemoApp.cs                          student app wiring: services, clock, timer, navigation
Models/                             student app views over Core entities (Hub, Vehicle, VehicleReservation, …) + student-only classes
Data/                               DemoConfig (prices, limits, labels), CampusMap (map coordinates), DemoToggles,
                                    MockDataStore (student-only data + views on the shared network)
Services/                           StudentContext (student data + services on one NetworkSession), JourneyPlanner,
                                    VehicleManager, ReservationService, ParkingService, PickUpService, TripBillingService,
                                    ChargingService, BookingService, IncidentService, NotificationService, HistoryService,
                                    NetworkWatcher (notifications for operator changes), NetworkMapping (status labels),
                                    NetworkLink (opens the operator window on the same session)
Ui/                                 Theme (light/dark), UiKit*.cs (shared controls, split by group), AppShell, DemoPanel
Screens/                            one class per student app screen
src/SmartEMobilityHub.Core/         core services library
src/SmartEMobilityHub.Operator/     operator dashboard (WPF, MVVM): Views/ (one UserControl per page),
                                    ViewModels/ (MainViewModel split by page + Commands), Infrastructure/ (RelayCommand, dialogs, logger)
tests/SmartEMobilityHub.Tests/      test runner
docs/                               project documents
```

### Architecture notes
- **Student app:** Screen → Service → in-memory store. Screens are C# classes built with the shared `UiKit`, so all screens share one style. Services get everything they need from a `StudentContext` passed to their constructor (no static access to the WPF `DemoApp`), so they can run in the test project.
- **Operator dashboard:** MVVM on top of the Core services. Each sidebar page is a `UserControl` in `Views/`; its buttons bind to `RelayCommand`s in `MainViewModel.Commands.cs`. The window code-behind only does window work (log out, light/dark, show/hide the What-if page). Warnings and confirmations go through `IDialogService`.
- **One network:** student models (`Hub`, `Vehicle`, `ChargingPoint`, `VehicleReservation`, `ChargingRequest`, `Incident`) are read-only views over the Core entities. Every change goes through Core services (`StudentAppGateway`, `IncidentManager`, `SmartChargingScheduler`, `AutoCancelService`), so the rules (15-minute hold, extensions while scanning, duplicate incident merge, power limits) exist once.
- `ShotKit` (screen capture for the Working demonstration) exists once, in the operator project; the student app compiles the same file.

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
