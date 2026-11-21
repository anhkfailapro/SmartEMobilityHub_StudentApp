# DESIGN_CHANGES — Sai lệch so với Submission #3

**Quy tắc:** mọi điểm khác Sub 3 (thêm method/field/enum, đổi kiểu, đổi visibility) phải ghi ở đây **trước khi** áp dụng vào code. Không sửa âm thầm.
**Trạng thái:** `Đề xuất` = chờ Vỹ xác nhận · `Đã áp dụng` = có trong code · `Từ chối` = không làm.

Cập nhật lần cuối: 21/11/2026 (GĐ6).

| ID | Loại | Nội dung | Lý do | Ảnh hưởng | Trạng thái |
|---|---|---|---|---|---|
| DC-01 | Thêm enum | `HubStatus { ONLINE, OFFLINE, SUSPENDED }` | Bảng 41 yêu cầu `suspendHub` đặt Hub SUSPENDED, nhưng `DeviceStatus` (Bảng 27) không có giá trị này (giả định A5 của PLAN) | `MobilityHub.status`, `triggerSmartScheduling`, UI | Đề xuất |
| DC-02 | Thêm enum + field | `ConnectionStatus { ONLINE, OFFLINE }`; `MobilityHub.connection` tách khỏi `MobilityHub.status` | Bảng 23 có `connection`; UC-SYS-02 Exception 2a đánh dấu "Offline" khi mất cảm biến — khác với bị Operator tạm dừng | Đồng bộ, UI, cảnh báo | Đề xuất |
| DC-03 | Thêm giá trị + alias | Thêm `RELOCATING` vào `DeviceStatus`; export `VehicleStatus` như alias có map 1–1 sang `DeviceStatus` | §4.3 Bảng 11 dùng `VehicleStatus` (PascalCase, có Relocating); §4.4 Bảng 27 dùng `DeviceStatus` (UPPER_SNAKE). Cần một nguồn sự thật cho contract với Nhiên/Trường | Contract GĐ1 | Đề xuất |
| DC-04 | Đổi tên lớp | Lớp `Hub` (§4.3, Development View) đặt tên `MobilityHub` (§4.4), export `type Hub = MobilityHub` | Hai tài liệu dùng hai tên; Bảng 29/31/37/41/47 (phần của Vỹ) dùng `MobilityHub` | Nhẹ, có alias nên code nhóm không vỡ | Đề xuất |
| DC-05 | Đổi tên field | Dùng `lastUpdatedAt` thay cho `lastUpdatedTime` | Bảng 23 (thuộc tính) vs phần text Bảng 29/38 không thống nhất | Nhẹ | Đề xuất |
| DC-06 | Thêm method | `StatusUpdaterTimer.tickNow(): void`, `getNextTickInSeconds(): number` | Sub 3 không có; cần cho demo (không chờ 30s), test và nhãn header "Chu kỳ quét 30s · Cập nhật N giây trước" | UI, test, demo | Đã áp dụng |
| DC-07 | Thêm port | `IScheduler` inject vào `StatusUpdaterTimer` | Nếu dùng timer toàn cục, test phải chờ thật; inject giúp test tất định và UI tự điều khiển nhịp | Constructor timer (tham số optional, mặc định adapter thật) | Đã áp dụng |
| DC-08 | Chốt kiểu trả về | `onTick(): void`; `syncHubStatus(): int`; `checkStaleData(hub): bool`; `autoCancelExpiredReservations(): int`; `triggerSmartScheduling(): void`; `allocatePower(hub, plan): int` (số cổng cấp được) | Cột "Kiểu trả về" trong Bảng 29/37 bị dồn dòng khi trích PDF, không đọc chắc (C4) | Chữ ký method — cần Vỹ đối chiếu bản gốc | Đã áp dụng |
| DC-09 | Thêm field | `ChargingRequest` thêm `vehicleId`, `hubId`, `pinned: boolean`, `assignedPortId?: string` | Bảng 38 thiếu các field này nhưng Bảng 37 yêu cầu "ghim thứ tự" và "chuyển xe sang cổng khác" | Lập lịch sạc, seed, test | Đề xuất |
| DC-10 | Thêm field | `MobilityHub` thêm `parkingCapacity`, `occupiedParkingSlots`, `chargingPortIds`, `powerLimitKw`; `Vehicle` thêm `hubId`, `model`, `connection`, `lastUpdatedAt` | Bảng 23/17 chỉ liệt kê một phần thuộc tính; seed và công thức utilization (A13) cần các field này | Contract GĐ1, seed | Đề xuất |
| DC-11 | Nới quy tắc trạng thái | `updateIncidentStatus` cho phép các cạnh: NEW→UNDER_CHECK, NEW→LOCKED, UNDER_CHECK→LOCKED, UNDER_CHECK→IN_REPAIR, LOCKED→IN_REPAIR, IN_REPAIR→RESOLVED, RESOLVED→CLOSED | Bảng 41 viết chuỗi tuyến tính, nhưng TCOP03-03 bắt buộc NEW→LOCKED→IN_REPAIR (C6) | `IncidentManager`, test TCOP03 | Đã áp dụng |
| DC-12 | Bổ sung ngữ nghĩa | `reportCode` trả về chính `incidentId` (không tạo bộ đếm riêng) | TCOP03-01 yêu cầu "có report code" nhưng Bảng 42 không có field này | API trả về của `receiveStudentReport` | Đã áp dụng |
| DC-13 | Thêm field | `MaintenanceTask.note?`, `MaintenanceTask.cancelReason?` | Bảng 45 có `complete(note)` và `cancel(reason)` nhưng Bảng 44 không có chỗ lưu | Phiếu bảo trì, nhật ký UI | Đề xuất |
| DC-14 | Bổ sung tham số | `ThresholdMonitor.checkAllHubs(hubs = null)` — bỏ trống thì lấy từ store | Bảng 31 yêu cầu tham số `hubs`, PLAN lại gọi `checkAllHubs()` từ Timer | Timer, test | Đã áp dụng |
| DC-15 | Chốt kiểu trả về | `evaluateHub(hub): Alert?` | Bảng 31 ghi `Alert` nhưng mô tả "ngược lại null" | ThresholdMonitor, test | Đã áp dụng |
| DC-16 | Thêm tham số optional | `IncidentManager.getIncidentQueue(hubId = null, status = null)` | Bảng 41 ghi 2 tham số nhưng UI cần "tất cả" và lọc theo tab | UI queue, test | Đã áp dụng |
| DC-17 | Thêm thành phần | `NetworkStore` (observable, `snapshot()` deep clone), `Clock`, `ports/*` | Không có trong Sub 3 (Sub 3 là thiết kế lớp nghiệp vụ); cần cho yêu cầu contract với nhóm và NFR 2.2.6, và để test không phụ thuộc UI | Toàn bộ GĐ1 | Đề xuất (PLAN đã dự kiến) |
| DC-18 | Tách lớp | `AutoCancelService` là lớp riêng, Timer gọi qua nó | Sub 3 chỉ có method `#autoCancelExpiredReservations()` trong Timer; Development View lại có `AutoCancelService` trong package Reservation (C9/PLAN §5.5) | Timer, test TCSYS04 | Đã áp dụng |
| DC-19 | Quy ước lỗi | Giữ nguyên **tên** exception như Sub 3 dưới dạng class kế thừa `Error`: `IllegalArgumentException`, `IllegalStateException`, `FileTooLargeException`, `HubNotFoundException`, `IncidentNotFoundException` | Bảo toàn tên trong bảng 41/45; TypeScript không có exception kiểu Java | Toàn bộ service, test | Đề xuất |

## Quy ước không tính là thay đổi thiết kế

- Thêm **getter/helper thuần** (`getParkingUtilization()`, `getChargingUtilization()`) phục vụ công thức ở Bảng 31 — Sub 3 đã ngầm gọi các hàm này.
- Thêm **comment tham chiếu** use case/bảng (yêu cầu của đồ án).
- `id` sinh tuần tự bằng bộ đếm nội bộ có seed (không dùng `Date.now()`), dạng `AL-0001`, `MT-0001`.

---

## Cập nhật GĐ1 (21/11/2026) — chuyển sang C#/.NET

| ID | Loại | Nội dung | Lý do | Trạng thái |
|---|---|---|---|---|
| DC-20 | Đổi stack | Toàn bộ GĐ1–GĐ5 hiện thực bằng **C#/.NET 10** thay TypeScript | App của nhóm là C#/WPF; TypeScript không thể tích hợp vào MVP (câu trả lời Q1) | Đã áp dụng |
| DC-21 | Kiến trúc | Thêm `src/SmartEMobilityHub.Core` (class library `net10.0`, không WPF) và `tests/SmartEMobilityHub.Tests` (console runner) vào repo nhóm; app WPF không bị sửa logic | Tránh phá seed/demo UC-01..11 của Trường; dashboard vận hành là app riêng theo Sub 2 §6; Core là hợp đồng chung | Đã áp dụng |
| DC-22 | Đặt tên | `NetworkStore` nằm ở `SmartEMobilityHub.Core.Store`; `MockDataStore` của app giữ nguyên | Hai bộ dữ liệu phục vụ hai màn hình demo khác nhau (xem GD1 §1) | Đã áp dụng |
| DC-23 | Enum | `DeviceStatus` viết PascalCase (`Available`, `InUse`, …) thay UPPER_SNAKE; khi tích hợp với app phải map sang `VehicleStatus` của app | Quy ước C# của nhóm | Đã áp dụng |
| DC-24 | Test | 18 test case chạy bằng console runner tự viết (`tests/`), không dùng xUnit/NUnit vì nhóm cấm NuGet | Ràng buộc "No NuGet packages" trong README nhóm | Đã áp dụng (khung) |
| DC-25 | Dữ liệu | `MobilityHub.OccupiedParkingSlots` là **bộ đếm độc lập**, không suy ra từ danh sách `Vehicle` | 54/60 là số chỗ vận hành (gồm xe cá nhân), tránh tạo 54 đối tượng và tránh phình danh sách xe của app sinh viên | Đã áp dụng |
| DC-26 | Ngữ nghĩa | `Reservation.HoldUntil` là nguồn sự thật của hạn giữ chỗ; `IReservationPort` nhận thêm `holdMinutes` để đối chiếu ngưỡng 15 phút | Cần cho `ExtendHold` (Exception 5a) và biên 14/16 phút | Đã áp dụng |

**Đã áp dụng ở GĐ1:** DC-01, DC-02, DC-04, DC-05, DC-09, DC-10, DC-13, DC-17, DC-19, DC-20…DC-26.
**Đã áp dụng ở GĐ2–GĐ6:** DC-03, DC-06, DC-07, DC-08, DC-11, DC-12, DC-14, DC-15, DC-16, DC-18, DC-27…DC-34.

---

## Cập nhật GĐ2–GĐ6 (21/11/2026)

| ID | Loại | Nội dung | Lý do | Trạng thái |
|---|---|---|---|---|
| DC-27 | Thêm field + hook | `PortTelemetry.LastUpdatedAt` (mốc dữ liệu từng cổng); `StatusUpdaterTimer.StepFailureInjector` (chỉ test) | Cần giữ mốc cũ khi cổng mất kết nối để UI báo "dữ liệu cũ"; cần chứng minh lỗi một bước không chặn bước khác (NFR 2.2.2) | Đã áp dụng |
| DC-28 | Chốt hành vi | `SmartChargingScheduler`: `HandleFaultyChargingPoint` trả `bool`; `UpdatePriorityRules` từ chối nếu mỗi trọng số ngoài [0,1] hoặc tổng ≠ 1; `AllocatePower` chỉ cấp cổng `Available` trong ngân sách còn lại, cổng lỗi thì thử cổng kế tiếp | Bảng 37 không nói rõ kiểu trả về và cách xử lý cổng lỗi; test TCSYS01-02/04 cần hành vi xác định | Đã áp dụng |
| DC-29 | Mở rộng điều kiện | `ThresholdMonitor.CheckAllHubs` bỏ qua Hub `SUSPENDED` **và** `OFFLINE`; so sánh dùng `>=` | Bảng 31 chỉ nói bỏ Hub OFFLINE; Hub tạm dừng cũng không nên sinh cảnh báo quá tải; biên 90% phải tính là quá tải (TCSYS03-01) | Đã áp dụng |
| DC-30 | Thêm port | `IAlertSink` + `AlertRetryQueue` là lớp riêng, gửi lại sau 30 giây, tối đa 5 lần | Bảng 32/33 mô tả hàng đợi nhưng không có lớp; cần inject vào Timer và các service khác | Đã áp dụng |
| DC-31 | Đổi kiểu trả về | `IncidentManager.SuspendHub(hubId, operatorId)` trả `int` (số reservation đã huỷ) và broadcast thông báo | Bảng 41 không ghi kiểu trả về; TCOP03-04 cần đếm đúng 5 đơn | Đã áp dụng |
| DC-32 | Đổi chữ ký | `MaintenanceTask.Assign(tech, now, sla)` — nhận hạn SLA thay vì tự suy từ priority | Bảng 45 không nói ai tính SLA; `IncidentManager` biết priority của sự cố | Đã áp dụng |
| DC-33 | Thêm project UI | `src/SmartEMobilityHub.Operator` (WPF) là dashboard vận hành riêng; app sinh viên không bị sửa logic/XAML | Sub 2 §6 tách màn hình vận hành; tránh xung đột seed KTX A đầy 100% của app sinh viên | Đã áp dụng |
| DC-34 | Dữ liệu test | TCOP03-01 dùng XE-112 thay XE-204 (XE-204 đã có SC-1023 mở → sẽ gộp); TCSYS02-02 bơm lỗi thêm CS-01-02 để kiểm tra log chuyển Offline | Seed cố định của Sub 2 §6.2 mâu thuẫn với kịch bản test nếu dùng XE-204; log chỉ nên phát khi trạng thái **đổi** | Đã áp dụng |
