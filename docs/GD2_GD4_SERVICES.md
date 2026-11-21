# GĐ2–GĐ4 — Dịch vụ lõi (C#/.NET)

**Phạm vi:** UC-SYS-01, UC-SYS-02, UC-SYS-03, UC-SYS-04, UC-OP-03, UC-OP-04 (phần lõi).
**Vị trí:** `src/SmartEMobilityHub.Core` — class library `net10.0`, **không phụ thuộc WPF**, không NuGet.
**Trạng thái:** build 0 warning / 0 error; 30/30 test PASS (xem `GD5_TESTS.md`).

---

## 1. Kiến trúc

```
UI / Demo panel          (GĐ6: SmartEMobilityHub.Operator)
        │  gọi method, đọc NetworkStore
        ▼
Services                 StatusUpdaterTimer, HubStatusSyncService, ThresholdMonitor,
                         AlertRetryQueue, AutoCancelService, SmartChargingScheduler, IncidentManager
        │  chỉ dùng port + store
        ▼
Ports                    IClock, IIoTSensorGateway, INotificationService, IReservationPort,
                         ISystemLogger, IScheduler, IAlertSink, ISmartSchedulingService
        ▲
Adapters                 MockIoTSensorGateway, InAppNotificationService, InMemoryReservationAdapter,
                         ConsoleSystemLogger, ThreadPoolScheduler, UiLogger/NoopScheduler (Operator)
        │
        ▼
NetworkStore             dữ liệu trong bộ nhớ + Snapshot() clone sâu
```

Quy tắc đã giữ:

- **Không `DateTime.Now` trong service** — mọi thời điểm lấy từ `IClock`; chỉ `SystemClock` được đọc đồng hồ thật.
- Port giúp thay adapter thật (IoT, notification, reservation của Trường) mà không sửa service.
- Service không biết WPF; UI chỉ đọc `NetworkStore` và gọi method.

## 2. Sáu bước của `StatusUpdaterTimer`

| Bước | Việc làm | Service | Bảng |
|---|---|---|---|
| 1 | Đồng bộ trạng thái Hub/xe/cổng | `HubStatusSyncService.SyncHubStatus()` | 29 |
| 2 | Kiểm tra dữ liệu cũ &gt; 10 phút | `CheckStaleData(hub)` + `AlertRetryQueue` | 29 |
| 3 | So ngưỡng quá tải 90% | `ThresholdMonitor.CheckAllHubs()` | 30/31 |
| 4 | Lập lịch sạc thông minh | `ISmartSchedulingService.TriggerFor(hub)` → `SmartChargingScheduler.RunSchedule` | 36/37 |
| 5 | Huỷ đặt chỗ quá 15 phút | `AutoCancelService.AutoCancelExpiredReservations()` | 29/65 |
| 6 | Gửi lại cảnh báo | `AlertRetryQueue.ProcessQueue(now)` | 32/33 |

- Mỗi bước bọc `try/catch` riêng (`RunStep`) — lỗi một bước không chặn các bước sau (NFR 2.2.2).
- `Start()` idempotent; `Stop()` giải phóng subscription; `IsRunning`, `LastTickAt`.
- `TickNow()` chạy đúng một chu kỳ (DC-06), dùng cho demo và test.
- `GetNextTickInSeconds()`, `GetSecondsSinceLastTick()` phục vụ header “Chu kỳ quét 30s · Cập nhật N giây trước”.
- `StepFailureInjector` là hook **chỉ dùng cho test** để chứng minh cô lập lỗi (DC-27).

## 3. API công khai

### `HubStatusSyncService`
| Method | Trả về | Ghi chú |
|---|---|---|
| `SyncHubStatus()` | `int` | số Hub đồng bộ thành công |

- Timeout/mất kết nối của một Hub: Hub đó `ConnectionStatus.Offline`, `HubStatus.Offline` (nếu đang Online), **tất cả cổng Offline**, phát `Alert(DeviceOffline)`, giữ nguyên `LastUpdatedAt` để UI báo dữ liệu cũ.
- Lỗi một cổng chỉ làm cổng đó Offline/Faulty; các Hub khác vẫn chạy.

### `ThresholdMonitor`
| Method | Trả về |
|---|---|
| `EvaluateHub(hub)` | `Alert?` |
| `CheckAllHubs(hubs = null)` | `List<Alert>` |
| `SetThreshold(value)` | `void` — ném `IllegalArgumentException` nếu ngoài `(0, 1]` |
| `IsOverloaded(used, capacity)` | `bool` |

- Ngưỡng mặc định 0,90; biên dùng `>=` (54/60 = 90% là quá tải, 53/60 không).
- Bỏ qua Hub `SUSPENDED`/`OFFLINE` (Bảng 31).
- Thông điệp cảnh báo tiếng Việt, có số chỗ thực tế.

### `AlertRetryQueue` (`IAlertSink`)
| Method | Trả về |
|---|---|
| `DispatchAlert(alert)` | `bool` — gửi ngay, thất bại thì đưa vào hàng đợi |
| `EnqueueAlert(alert)` | `void` |
| `ProcessQueue(now)` | `int` — số alert gửi lại thành công |
| `Size()` | `int` |

- Gửi lại sau **30 giây**, tối đa **5 lần** (A1); hết lượt thì ghi log ERROR và loại bỏ.
- Dùng `IClock` nên test không phải chờ thật.

### `AutoCancelService`
| Method | Trả về |
|---|---|
| `AutoCancelExpiredReservations()` | `int` — số đơn đã huỷ |

- Hạn hiệu lực = `max(CreatedAt + 15 phút, HoldUntil)`; đơn `isPickingUp` được gia hạn 1 phút thay vì huỷ (Exception 5a).
- Huỷ đơn → xe về `Available`, gửi thông báo cho người dùng.
- Lỗi một đơn chỉ ghi log, vòng lặp tiếp tục.

### `SmartChargingScheduler` (`ISmartSchedulingService`)
| Method | Trả về |
|---|---|
| `RunSchedule(hub)` | `ChargingPlan` |
| `CalculatePriorityScore(request)` | `double` |
| `UpdatePriorityRules(wB, wN, wP)` | `void` — tổng phải bằng 1, mỗi số trong `[0,1]` |
| `ManualReorder(requestId, newPosition)` | `bool` |
| `CheckPowerLimit(hub, plan)` | `bool` |
| `AllocatePower(hub, plan)` | `int` — số cổng cấp được |
| `HandleFaultyChargingPoint(point)` | `bool` — còn cổng thay thế hay không |
| `GetQueue(hubId)` | `List<ChargingRequest>` |

- Điểm = `100 × (0.50 × (1 − pin/100) + 0.35 × urgency + 0.15 × peak)`, `urgency = clamp(1 − phútCònLại/240, 0, 1)`.
- Sắp xếp giảm dần theo điểm, cùng điểm thì `NeededAt` sớm hơn trước, cuối cùng theo `RequestId`.
- Cấp cổng chỉ trong ngân sách `PowerLimitKw − công suất cổng đang Charging`; cổng lỗi bị đánh `Faulty`, phát alert và thử cổng kế tiếp.
- Cổng đang sạc bị lỗi: chuyển xe sang cổng trống; hết cổng thì xe `NeedsCharge`, yêu cầu lên đầu hàng chờ.
- Vượt công suất: vẫn trả kế hoạch đầy đủ nhưng phát alert “vượt giới hạn” và chỉ cấp trong ngân sách.

### `IncidentManager`
| Method | Trả về |
|---|---|
| `ReceiveStudentReport(userId, objectType, objectCode, problemType, description, photoSize)` | `IncidentRecord` |
| `ReceiveSensorAlert(hubId, objectCode, alertType)` | `IncidentRecord` |
| `ReceiveUnlockTimeout(vehicleId, reservationId)` | `IncidentRecord` |
| `FindOpenIncident(objectCode)` | `IncidentRecord?` |
| `LockDevice(incidentId)` | `bool` |
| `CreateMaintenanceTask(incidentId)` | `MaintenanceTask` |
| `AssignTechnician(taskId, techId)` | `bool` |
| `SuggestTechnician(hubId)` | `Technician?` |
| `SuspendHub(hubId, operatorId)` | `int` — số reservation đã huỷ |
| `UpdateIncidentStatus(incidentId, status)` | `void` |
| `GetIncidentQueue(hubId = null, status = null)` | `List<IncidentRecord>` |

- Báo cáo trùng (cùng `objectCode`, sự cố còn mở) được **gộp**: thêm người báo, nối mô tả, ghi log.
- Ảnh &gt; 5 MB ném `FileTooLargeException`.
- Vòng đời: `New → UnderCheck → Locked → InRepair → Resolved → Closed`, cho phép nhảy cóc đã chốt ở DC-11.
- Khoá thiết bị idempotent; phiếu bảo trì lấy SLA theo priority (30′/2h/8h/24h).
- Gán KTV: chỉ KTV `Available`, ưu tiên khoảng cách gần Hub; sự cố sang `InRepair`, thiết bị `UnderRepair`.
- `SuspendHub`: Hub `Suspended`, huỷ các reservation `PendingPickup` có `StartsAt > now`, broadcast cho người bị ảnh hưởng.

## 4. Adapter

| Adapter | Dùng cho | Chức năng demo/test |
|---|---|---|
| `MockIoTSensorGateway` | `IIoTSensorGateway` | `FailNext(deviceId, FaultKind)`, `SetStale`, `SetNetworkDown`; lỗi cổng → Offline/Faulty, lỗi Hub → exception |
| `InAppNotificationService` | `INotificationService` | `NetworkDown` để giả lập mất mạng; lưu `OperatorAlerts`, `UserMessages`, `Broadcasts` |
| `InMemoryReservationAdapter` | `IReservationPort` | tìm đơn quá hạn/sắp tới, huỷ, gia hạn, đánh dấu đã nhận |
| `ConsoleSystemLogger` | `ISystemLogger` | log ra console |
| `ThreadPoolScheduler` | `IScheduler` | `System.Threading.Timer`, dùng ở app thật |
| `UiLogger`, `NoopScheduler` | — | bản dành cho dashboard WPF (log hiển thị, tick do UI điều khiển) |

## 5. Tích hợp với app của nhóm

1. Thêm `ProjectReference` từ app WPF sang `src/SmartEMobilityHub.Core/SmartEMobilityHub.Core.csproj`.
2. Dùng `NetworkStore` + `NetworkSeed` cho dashboard vận hành; **không sửa** `MockDataStore` của màn hình sinh viên.
3. Thay adapter khi có dịch vụ thật:
   - `IIoTSensorGateway` → cổng gọi API cảm biến.
   - `INotificationService` → push notification.
   - `IReservationPort` → `ReservationService` của Trường (nếu chuyển sang dùng chung một store).
4. Map enum Core ↔ enum app (`DeviceStatus` PascalCase ↔ `VehicleStatus`/`PointStatus` UPPER_SNAKE của app) tại biên tích hợp.
5. Nếu app chạy timer thật: dùng `ThreadPoolScheduler`; nếu UI tự tick: dùng `NoopScheduler` + `TickNow()` để tránh sửa dữ liệu từ thread nền.

## 6. Sai lệch & giả định

Xem `DESIGN_CHANGES.md` (DC-07, DC-08, DC-11, DC-14…DC-18, DC-27…DC-33) và `GD0_REVIEW.md` (A1–A14). Các điểm đáng chú ý:

- `HandleFaultyChargingPoint` trả `bool` thay vì `void` (cần biết còn cổng hay không).
- `Assign` của `MaintenanceTask` nhận thêm `sla` để tính hạn theo priority.
- `AlertRetryQueue` là lớp riêng thay vì nằm trong Timer (DC-18), nhưng vẫn được Timer gọi ở bước 6.
- Hub bị `Suspend` không bị bước 1 đưa về Online; chỉ `Offline` mới được phục hồi khi telemetry có lại.
- `ThresholdMonitor` bỏ qua cả `Suspended` lẫn `Offline` (Bảng 31 chỉ nói Offline — mở rộng hợp lý, ghi DC-29).

## 7. Cách chạy

```bat
cd SmartEMobilityHub_StudentApp
..\.dotnet\dotnet build SmartEMobilityHub.slnx
..\.dotnet\dotnet run --project tests\SmartEMobilityHub.Tests\SmartEMobilityHub.Tests.csproj
```

> Máy Vỹ không có .NET SDK toàn cục; SDK được cài cục bộ ở `../.dotnet/` (ngoài repo, không commit).
> Nếu máy khác đã có .NET 10 SDK thì thay `..\.dotnet\dotnet` bằng `dotnet`.
