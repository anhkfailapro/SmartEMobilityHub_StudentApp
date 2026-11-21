# GĐ0 — Đọc tài liệu, kế hoạch thực thi, câu hỏi và điểm mâu thuẫn

**Người thực hiện:** Châu Hoàng Vỹ (MSSV 2414064) — Nhóm 21, CO3001
**Ngày:** 21/11/2026
**Trạng thái:** ⏸ Chờ Vỹ xác nhận — **chưa viết code** (đúng yêu cầu GĐ0).

> **CẬP NHẬT 21/11/2026 (sau Q1):** app của nhóm là **C#/WPF**, nên kế hoạch TypeScript trong tài liệu này **đã được thay** bằng C#/.NET — xem `docs/GD1_CORE_CSHARP.md`. Phần bên dưới giữ nguyên làm lịch sử quyết định GĐ0; các mục C1–C16 vẫn còn giá trị.


---

## 0. Tài liệu đã đọc

| Nguồn | Phần đã đọc | Dùng cho |
|---|---|---|
| `docs/PLAN_SUB4_VY.md` | Toàn bộ | Kế hoạch gốc, giả định A1–A6, seed, UI mục 6 |
| `docs/submission_1.pdf` | §2.2 (NFR 2.2.1–2.2.6); §3.3 UC-SYS-01..04; §3.4 UC-OP-03, UC-OP-04 | Hành vi use case + con số NFR |
| `docs/submission_2.pdf` | §5 (Hình 36–43: sequence/activity UC-SYS-01..04); §6.1–6.4 (UI Incident & Maintenance) | Luồng xử lý, bố cục màn hình, nhãn tiếng Việt |
| `docs/submission_3.pdf` | §3 Development View; §4.4 Bảng 26–47; §5 Bảng 62–67 | Tên class/method/attribute/enum, 18 test case |
| `docs/HK261_Assignment_Desc.pdf` | Project guidelines, rubric, quy định khai báo GenAI | Ràng buộc nộp bài |
| Figma `7oHDwgPdTjJOjJWG0Zhfje` | Chỉ xác nhận file **public** (oEmbed trả `title: "Smart E-Mobility Hub UI"`, có thumbnail). **Chưa đọc được frame** | GĐ6 |
| `SmartEMobilityHub_StudentApp/` | README + cấu trúc thư mục | Phát hiện xung đột stack (xem C14) |

**Lưu ý kỹ thuật:** PDF của nhóm bị lỗi font khi trích xuất text (mất dấu tiếng Việt, một số ô bảng bị dồn dòng). Tôi đã đọc theo ngữ cảnh và **đối chiếu chéo** các con số với PLAN; những chỗ không chắc chắn được liệt kê ở mục 3 (C4, C8). Bản trích text tạm nằm ở `docs/_extract/` — có thể xoá trước khi nộp.

---

## 1. Kế hoạch thực thi chi tiết

### 1.1 Vị trí dự án và cấu trúc file

Đề xuất đặt dự án TypeScript **ngay tại thư mục gốc workspace**, giữ nguyên `docs/` và **không đụng** `SmartEMobilityHub_StudentApp/` (bản C#/WPF của nhóm):

```
Công Nghệ Phần Mềm/
├─ docs/                        # giữ nguyên PDF; thêm các .md của Vỹ
│  ├─ GD0_REVIEW.md             # file này
│  ├─ DESIGN_CHANGES.md         # mọi sai lệch so với Sub 3
│  ├─ AI_USAGE_LOG.md           # khai báo GenAI (bắt buộc)
│  ├─ design-tokens.md          # trích từ Figma, làm trước GĐ6
│  └─ _extract/                 # text tạm từ PDF (có thể xoá)
├─ src/
│  ├─ core/                     # TypeScript thuần — KHÔNG import React/DOM
│  │  ├─ domain/                # enums.ts, types.ts, errors.ts, id.ts
│  │  ├─ clock.ts               # Clock, SystemClock, FakeClock, timeScale
│  │  ├─ store/                 # NetworkStore.ts, selectors.ts
│  │  ├─ seed/                  # hubs.ts, vehicles.ts, chargers.ts, slots.ts,
│  │  │                         # incidents.ts, technicians.ts, reservations.ts,
│  │  │                         # operators.ts, index.ts (buildSeed)
│  │  ├─ ports/                 # IoTSensorGateway, NotificationService,
│  │  │                         # ReservationPort, SystemLogger, Scheduler
│  │  ├─ adapters/              # MockIoTSensorGateway, InAppNotificationService,
│  │  │                         # InMemoryReservationAdapter, ConsoleLogger
│  │  └─ services/              # StatusUpdaterTimer, HubStatusSyncService,
│  │                            # ThresholdMonitor, AlertRetryQueue,
│  │                            # AutoCancelService, SmartChargingScheduler,
│  │                            # IncidentManager
│  └─ ui/                       # GĐ6 (React) — chỉ sau khi Vỹ xác nhận
├─ tests/
│  ├─ unit/                     # biên: score, stale, overload, SLA
│  ├─ usecase/                  # TCSYS02/03/04/01, TCOP03
│  ├─ fixtures/                 # buildTestWorld() dùng FakeClock
│  └─ report/                   # reporter sinh results.md + results.json
├─ package.json, tsconfig.json, vite.config.ts, vitest.config.ts
└─ SmartEMobilityHub_StudentApp/   # KHÔNG sửa
```

**Nguyên tắc phụ thuộc:** `ui → services → ports ← adapters`, `services → store/domain/clock`. `core` không import `ui`, không dùng `window`/`document`, không gọi `Date.now()`/`new Date()` trong service (chỉ trong `SystemClock`).

### 1.2 Contract GĐ1 sẽ "freeze" (cho nhóm review trước khi tôi code)

Đây là phần ảnh hưởng trực tiếp tới Đức (What-if), Nhiên (Dashboard), Trường (Reservation), Hiệp (frontend). Tôi sẽ dừng ở GĐ1 để nhóm xác nhận, sau đó **chỉ thêm field, không đổi tên/xoá**.

**Enum (Bảng 27 Sub 3):**
`DeviceStatus` (AVAILABLE, RESERVED, IN_USE, CHARGING, NEEDS_CHARGE, UNDER_CHECK, LOCKED, UNDER_REPAIR, FAULTY, OFFLINE) · `IncidentStatus` (NEW, UNDER_CHECK, LOCKED, IN_REPAIR, RESOLVED, CLOSED) · `MaintenanceStatus` (ASSIGNED, IN_PROGRESS, COMPLETED, CANCELLED) · `PriorityLevel` (LOW, MEDIUM, HIGH, CRITICAL) · `IncidentSource` (STUDENT_REPORT, IOT_SENSOR, UNLOCK_TIMEOUT) · `AlertType` (HUB_OVERLOAD, STALE_DATA, DEVICE_OFFLINE, CHARGER_FAULT).
Bổ sung (đã có trong PLAN, sẽ ghi DESIGN_CHANGES): `HubStatus` (ONLINE, OFFLINE, SUSPENDED), `ConnectionStatus` (ONLINE, OFFLINE — theo §4.3), `VehicleType`, `ReservationStatus`.

**Entity chính:** `MobilityHub` (alias `Hub`), `Vehicle`, `ChargingPoint`, `ParkingSlot`, `Reservation`, `Alert`, `IncidentRecord`, `MaintenanceTask`, `Technician`, `ChargingRequest`, `ChargingPlan`, `Operator`.

**Store API (observable, snapshot read-only):**

| Method | Ý nghĩa |
|---|---|
| `getState(): Readonly<NetworkState>` | Bản đang sống, chỉ đọc ở mức type |
| `snapshot(): NetworkState` | **Deep clone** — Đức dùng cho What-if, không thể ghi ngược |
| `subscribe(listener): () => void` | UI render lại; trả hàm unsubscribe |
| `update(mutator): void` | Mutator nhận draft, tự `notify()` sau khi xong |
| `getHub/getVehicle/getChargingPoint/...` | Truy vấn nhanh |

**Ports:**

| Port | Method chính |
|---|---|
| `IoTSensorGateway` | `fetchSensorTelemetry(hubId): Promise<HubTelemetry>`; `sendPowerCommand(portId, kw): Promise<PowerCommandResult>` |
| `NotificationService` | `pushToOperator(alert): Promise<void>`; `notifyUser(userId, message): Promise<void>`; `broadcast(hubId, message): Promise<void>` |
| `ReservationPort` | `findExpiredReservations(now, holdMin): Reservation[]`; `cancelReservation(id, reason)`; `extendHold(id, extraMin)`; `findUpcomingByHub(hubId, now)`; `markPickedUp(id)` |
| `SystemLogger` | `info/warn/error(message, context?)` |
| `Scheduler` (thêm) | `setInterval(fn, ms)`, `clearInterval(id)` — để test không phụ thuộc `setInterval` thật |

### 1.3 Kế hoạch từng giai đoạn

| GĐ | Nội dung | File chính | Tiêu chí nghiệm thu (Definition of Done) |
|---|---|---|---|
| **1** | Domain, enums, lỗi nghiệp vụ, Clock, NetworkStore, ports, seed hero + generator | `src/core/{domain,store,seed,ports}`, `clock.ts` | `tsc --noEmit` sạch; seed đủ hero entity §4.1 + đúng 24 incident (12/7/5); `snapshot()` deep-clone (sửa bản clone không ảnh hưởng store); không import UI |
| **2** | Gateway giả lập + đồng bộ + ngưỡng + retry | `MockIoTSensorGateway`, `HubStatusSyncService`, `ThresholdMonitor`, `AlertRetryQueue`, `Alert`, `InAppNotificationService` | Tick đồng bộ được 12 Hub; timeout 1 cổng không chặn Hub khác; 54/60 → alert; 53/60 → không; retry đúng 30s |
| **3** | Auto-cancel + Timer 6 bước | `AutoCancelService`, `StatusUpdaterTimer` | Mỗi bước try/catch riêng (test bơm lỗi 1 bước, 5 bước còn lại vẫn chạy); `tickNow()`, `getNextTickInSeconds()`; không còn `Date.now()` trong `services/` |
| **4** | Lập lịch sạc + sự cố/bảo trì | `SmartChargingScheduler`, `IncidentManager`, `IncidentRecord`, `MaintenanceTask`, `Technician` | Đúng chữ ký Bảng 37/41/43/45/47; A > C > B với trọng số mặc định; lỗi cổng → chuyển cổng/đầu hàng chờ |
| **5** | 18 test case + unit biên + báo cáo | `tests/**` | 18/18 có kết quả thật; `results.md` + `results.json`; fail thì ghi rõ nguyên nhân, không sửa test cho xanh |
| **6** | UI Incident & Maintenance + DemoControlPanel | `src/ui/**`, `design-tokens.md` | Chỉ làm sau xác nhận; khớp token/nhãn Figma; light/dark |
| **7** | Tích hợp, README, kịch bản demo | `README.md`, `docs/INTEGRATION.md` | Chạy `npm run dev`, demo 6 bước theo PLAN §10 |

**Thứ tự ưu tiên khi thiếu thời gian:** GĐ1 → 2 → 3 → 5 (bắt buộc) › GĐ4 › GĐ6.

### 1.4 Mapping 18 test case → file (chốt trước để không lệch Sub 3)

| Mã | File | Kịch bản then chốt |
|---|---|---|
| TCSYS02-01 | `tests/usecase/TCSYS02.spec.ts` | Tick 1 chu kỳ: HUB-01 40/60 → 20 chỗ trống, 6 cổng, `lastUpdatedAt` = giờ tick, chu kỳ 30s |
| TCSYS02-02 | nt | Timeout `CS-08-04` → cổng OFFLINE + log; Hub khác vẫn cập nhật |
| TCSYS02-03 | nt | Stale 14′ → `true` + alert; **đúng 10′ → `false`** |
| TCSYS03-01 | `tests/usecase/TCSYS03.spec.ts` | 54/60 = 90% → Alert HUB_OVERLOAD, `delivered = true` |
| TCSYS03-02 | nt | 53/60 = 88,3% → không alert |
| TCSYS03-03 | nt | Network down → `size() = 1`; <30s chưa gửi; ≥30s gửi lại, queue rỗng |
| TCSYS04-01 | `tests/usecase/TCSYS04.spec.ts` | R-101 (16′, not picking up) → huỷ, trả 1, tài nguyên AVAILABLE, có thông báo |
| TCSYS04-02 | nt | R-102 (14′) → giữ nguyên |
| TCSYS04-03 | nt | R-103 (16′, `isPickingUp`) → `extendHold`, không huỷ; sau unlock xe IN_USE, chu kỳ sau không huỷ |
| TCSYS01-01 | `tests/usecase/TCSYS01.spec.ts` | Trọng số mặc định 50/35/15; A(10%,30′) > C(30%,1h) > B(60%,2h); cấp 2 cổng, B chờ |
| TCSYS01-02 | nt | `(0.6,0.6,0.2)` → ném lỗi, giữ cấu hình cũ; `(0.4,0.4,0.2)` áp dụng |
| TCSYS01-03 | nt | Limit 20 kW, 7 kW/cổng, 4 yêu cầu 28 kW → `checkPowerLimit=false`, cấp 2 cổng, cảnh báo + gợi ý giãn |
| TCSYS01-04 | nt | CP-1 Fault → FAULTY, báo Operator, xe sang CP-2; hết cổng → đầu hàng chờ + `false` |
| TCOP03-01 | `tests/usecase/TCOP03.spec.ts` | Báo lỗi 2MB XE-204 → Incident NEW, xe UNDER_CHECK, rời danh sách available, vào queue |
| TCOP03-02 | nt | Báo trùng XE-204 → không tạo mới, `reporterIds` = 2; ảnh 6MB → từ chối kèm giới hạn 5MB |
| TCOP03-03 | nt | lock SC-1017 → LOCKED → tạo phiếu ASSIGNED có SLA → gán T-02 → IN_REPAIR; T-03 bận → `false` |
| TCOP03-04 | nt | `suspendHub("HUB-08")` → SUSPENDED, huỷ 5 reservation, trả 5; hub lạ → `HubNotFoundException` |
| TCOP03-05 | nt | Không ACK sau 30s → huỷ lệnh mở khoá, xe RESERVED, incident UNLOCK_TIMEOUT, hiện liên hệ hỗ trợ |

**Unit test biên (Bảng 62):** `calculatePriorityScore` (req null, pin −1/101, pin 0/100, urgency kẹp 0–1), `checkStaleData` (10′00″, 10′+1ms), `isOverloaded` (used=0, capacity=0, đúng 90%, vượt capacity), `isSlaBreached` (đúng hạn, quá 1ms, task COMPLETED).

### 1.5 Chiến lược test (không "làm xanh" giả)

- `FakeClock` + `ManualScheduler`; mọi test chạy trong <1s, không `sleep`.
- Mỗi test dựng `buildTestWorld()` riêng từ seed (deep clone) → test độc lập.
- Báo cáo do reporter riêng sinh ra: đọc `test.name` chứa mã `TCSYSxx-yy`, ghi `results.json` + `results.md` với cột **Mã | Kết quả thực tế (PASS/FAIL + ghi chú) | Ngày chạy**.
- Nếu test fail vì hành vi code sai Sub 3 → sửa code, **không** nới assertion.

### 1.6 GĐ6 (chỉ sau khi Vỹ xác nhận)

- `design-tokens.md` liệt kê frame đã xem + token trích ra (màu, font, bo góc, spacing, cỡ chữ) **trước khi code UI**.
- Không có Figma → dựng theo §6 Sub 2 và ghi rõ "chưa đối chiếu Figma" cho từng vùng.
- `DemoControlPanel` ẩn mặc định (`?demo=1` hoặc `Ctrl+Shift+D`), dev-only.

### 1.7 Rủi ro chính

| Rủi ro | Giảm thiểu |
|---|---|
| Contract đổi giữa chừng làm vỡ code nhóm | Freeze ở GĐ1; changelog trong `DESIGN_CHANGES.md`; chỉ thêm field |
| Figma không đọc được | Hỏi ảnh export; nếu không có, dựng theo Sub 2 + ghi rõ điểm chưa đối chiếu |
| Timer 30s thật khó demo | `timeScale` + `tickNow()`; test dùng `ManualScheduler` |
| Nhiều cảnh báo lặp mỗi 30s | Giữ đúng Sub 3 (phát mỗi chu kỳ khi vẫn ≥90%), UI gộp theo Hub; xem A8 |
| Trùng logic auto-cancel với Trường | Dùng `ReservationPort`; adapter in-memory mặc định, thay bằng adapter gọi `ReservationManager` khi có |
| Bị đánh giá lạm dụng AI | `AI_USAGE_LOG.md` + Vỹ tự giải thích từng service; cuối mỗi GĐ có mục "Vỹ cần tự hiểu" |

---

## 2. Câu hỏi cần Vỹ trả lời

### Chặn tiến độ (phải trả lời trước GĐ1)

| # | Câu hỏi | Vì sao chặn | Mặc định nếu không trả lời |
|---|---|---|---|
| **Q1** | **Stack/repo thật của nhóm là gì?** Trong workspace đã có `SmartEMobilityHub_StudentApp` — app C#/WPF (.NET 10) cho UC-01..UC-11 | Quyết định toàn bộ GĐ1–GĐ6. Nếu nhóm dùng C#, code TS của Vỹ sẽ không tích hợp được vào app đó | Tạo dự án TS mới tại gốc, **không sửa** app C#; core TS dùng được cho web demo và cho test |
| **Q2** | **Figma:** Vỹ có thể export PNG các frame (Incident & Maintenance, Operator Dashboard, sidebar/header, light + dark) vào `docs/figma/` không? Hoặc cấp quyền Dev Mode/MCP? | Không có frame thì GĐ6 không thể "bám Figma"; tôi chỉ được dựng theo mô tả và phải ghi rõ chưa đối chiếu | Tôi sẽ mở link bằng browser ở GĐ6 để đọc nhãn/màu bằng mắt, frame nào không mở được thì ghi rõ |
| **Q3** | **Vỹ có làm màn hình Quản lý sự cố & Bảo trì (GĐ6) không**, hay Hiệp/Nhiên đã nhận? | Ảnh hưởng 2 ngày công và việc freeze component với Hiệp | Làm, nhưng để cuối; core + test ưu tiên tuyệt đối |
| **Q4** | **Enum trạng thái xe:** dùng `DeviceStatus` (§4.4 Bảng 27, UPPER_SNAKE) hay `VehicleStatus` (§4.3 Bảng 11, PascalCase)? | Là contract với Nhiên/Trường/Đức; đổi sau sẽ vỡ code | `DeviceStatus` là chuẩn cho Vehicle/ChargingPoint/ParkingSlot; export alias `VehicleStatus` ánh xạ sang để không vỡ code §4.3 (xem C1) |
| **Q5** | **Repo chung của nhóm** (GitHub) và nhánh Vỹ được đẩy lên? Có quy ước commit/branch không? | Tránh Vỹ làm rồi phải chép tay; cần biết có phải tách `src/core` thành package | Không đẩy gì; làm local, xuất `src/core` + `tests` để Vỹ tự copy |

### Không chặn (trả lời lúc nào cũng được, tôi dùng mặc định)

| # | Câu hỏi | Mặc định |
|---|---|---|
| Q6 | Có cần `profile full` (24 Hub, ~1.248 xe) cho khớp KPI Figma của Nhiên? | Không; KPI tính từ dữ liệu, 12 Hub |
| Q7 | Tên 12 Hub còn lại (HUB-04..07, 09..12) lấy theo gợi ý PLAN hay Vỹ có danh sách riêng? | Theo PLAN: Thư viện Trung tâm, Nhà học B, KTX Khu B…, HUB-12 "Khu dịch vụ" |
| Q8 | Có cần i18n/đa ngôn ngữ không? | Không; UI tiếng Việt |
| Q9 | Có cần lưu dữ liệu qua reload (localStorage) không? | Không; in-memory đúng đề bài |
| Q10 | Thời gian bảo vệ/demo để tôi canh kịch bản? | Kịch bản 3–4 phút theo PLAN §10 |

---

## 3. Mâu thuẫn phát hiện giữa Sub 1 / Sub 2 / Sub 3

| # | Mâu thuẫn | Bằng chứng | Đề xuất xử lý | Ảnh hưởng |
|---|---|---|---|---|
| **C1** | Hai enum trạng thái xe khác kiểu chữ và khác tập giá trị | §4.3 Bảng 11 `VehicleStatus {Available, Reserved, InUse, NeedsCharge, Charging, Faulty, Relocating}` vs §4.4 Bảng 27 `DeviceStatus {AVAILABLE, RESERVED, IN_USE, CHARGING, NEEDS_CHARGE, UNDER_CHECK, LOCKED, UNDER_REPAIR, FAULTY, OFFLINE}` | `DeviceStatus` là chuẩn (Bảng 27 nói "dùng chung với phân hệ Network"); thêm `RELOCATING` vào `DeviceStatus`; export `VehicleStatus` như alias có map 1–1 | **Contract** với Nhiên (điều phối cần RELOCATING) và Trường |
| **C2** | Tên lớp Hub | §4.3/Development View: `Hub`; §4.4 Bảng 29/31/37/41/47: `MobilityHub` | Class `MobilityHub`, export `type Hub = MobilityHub` | Nhẹ, chỉ là tên |
| **C3** | Tên field thời điểm cập nhật | Bảng 23 `lastUpdatedAt` vs text Bảng 29/38 `lastUpdatedTime` | `lastUpdatedAt` | Nhẹ |
| **C4** | Cột "Kiểu trả về" ở Bảng 29/37/41 bị dồn dòng khi trích PDF, không xác định chắc `onTick()`, `syncHubStatus()`, `checkStaleData()`, `allocatePower()` trả gì | Bảng 29 hiện `void/void/boolean/int` cho 4 method; Bảng 37 hiện `int/boolean/List<...>` lệch hàng | Chốt: `onTick(): void`, `syncHubStatus(): number` (số Hub đồng bộ), `checkStaleData(hub): boolean`, `autoCancelExpiredReservations(): number`, `triggerSmartScheduling(): void`, `allocatePower(): number` (số cổng cấp được). Vỹ đối chiếu lại bản Word/PDF gốc | Nếu Vỹ xác nhận khác → sửa trước GĐ3, ghi DESIGN_CHANGES |
| **C5** | Bảng 29 đánh dấu `#` (protected) cho `syncHubStatus/checkStaleData/autoCancelExpiredReservations/triggerSmartScheduling`, nhưng TCSYS02-03 lại gọi trực tiếp `checkStaleData` | Bảng 29 vs Bảng 63 | Giữ `protected` đúng Sub 3; test dùng subclass `TestableTimer` phơi ra để gọi. Không đổi visibility | Test dễ hiểu hơn, không lệch thiết kế |
| **C6** | Chuỗi trạng thái sự cố là tuyến tính, nhưng luồng TCOP03-03 bắt buộc nhảy cóc | Bảng 41: `NEW→UNDER_CHECK→LOCKED→IN_REPAIR→RESOLVED→CLOSED`; TCOP03-03: tạo (NEW) → `lockDevice` → `createMaintenanceTask` → `assign` → IN_REPAIR | Cho phép **cạnh hợp lệ**: NEW→UNDER_CHECK, NEW→LOCKED, UNDER_CHECK→LOCKED, UNDER_CHECK→IN_REPAIR, LOCKED→IN_REPAIR, IN_REPAIR→RESOLVED, RESOLVED→CLOSED. Cấm lùi/không xác định | Ảnh hưởng `updateIncidentStatus` + test |
| **C7** | Tạo sự cố ghi "Incident NEW" nhưng thiết bị "UNDER_CHECK" | Bảng 41 + TCOP03-01 | Giữ đúng: incident NEW (để KPI "Mới" = 12), thiết bị UNDER_CHECK (để rời danh sách AVAILABLE). Ghi chú vào DESIGN_CHANGES | KPI + tab khớp Figma |
| **C8** | Bảng 66 đặt `updatePriorityRules(0.6,0.6,0.2)` ngay dưới TCSYS01-01 (tổng = 1,4 — không hợp lệ) | Bảng 66, cột "Các bước thực hiện" bị dồn hàng | Hiểu là: TC-01 dùng trọng số **mặc định 50/35/15**; `(0.6,0.6,0.2)` chỉ là **dữ liệu sai** của TC-02 | Nếu hiểu sai, TC-01 sẽ ném lỗi ngay |
| **C9** | `runSchedule` nói "cắt phần cuối hàng chờ" nhưng TCSYS01-03 lại kỳ vọng `checkPowerLimit = false` với 28 kW > 20 kW | Bảng 37 vs Bảng 66 | `runSchedule` trả plan **đầy đủ** (totalPowerKw = 28) để `checkPowerLimit` phát hiện false + cảnh báo + gợi ý giãn; `allocatePower` chỉ cấp trong ngân sách (2 cổng), phần còn lại giữ hàng chờ | Logic then chốt của UC-OP-04 |
| **C10** | `req.isPickingUp` là field của Reservation nhưng Bảng 38 (ChargingRequest) không có `vehicleId`, `hubId`, `pinned` — trong khi Bảng 37 yêu cầu ghim thứ tự và chuyển xe sang cổng khác | Bảng 37 vs Bảng 38 | Thêm field (chỉ thêm): `vehicleId`, `hubId`, `pinned`, `assignedPortId?` | Bắt buộc để chạy được |
| **C11** | "Dữ liệu cũ · 14 phút" gắn cho CS-08-04, nhưng CS-08-04 đồng thời là cổng OFFLINE | Sub 2 §6.2 | Seed: CS-08-04 OFFLINE, `lastUpdatedAt = now − 14′`. `checkStaleData` vẫn phát STALE_DATA; UI hiển thị **cả** nhãn Offline và nhãn dữ liệu cũ | UI GĐ6 |
| **C12** | Header Figma ghi "Cập nhật 10 giây trước" (số tĩnh) vs PLAN yêu cầu "N giây trước" (động) | Sub 2 §6.2 vs PLAN §6 | Hiển thị động theo `getNextTickInSeconds()`; ảnh chụp báo cáo có thể canh đúng 10s nếu cần khớp Figma | UI GĐ6 |
| **C13** | Sub 3 §3 mô tả PostgreSQL/Redis/WebSocket; đề bài + phạm vi MVP cho phép hard-code | Sub 3 §2.1 vs Assignment §4 | MVP in-memory; ports/adapters để sau này thay bằng adapter thật. Ghi rõ trong README | Không ảnh hưởng code |
| **C14** | Workspace đã có app **C#/WPF** cho Student app (UC-01..11), README nhắc `docs/DECISIONS.md`, `docs/PLAN.md` nhưng các file này không tồn tại | `SmartEMobilityHub_StudentApp/README.md` | **Không sửa app đó.** Hỏi Q1 trước khi tạo dự án TS | Có thể là hướng stack thật của nhóm |
| **C15** | `Hub` có `connection: ConnectionStatus` (§4.3) nhưng `suspendHub` lại đặt `SUSPENDED` | A5 trong PLAN + Bảng 41 | Tách 2 field: `connection` (Online/Offline) và `status: HubStatus` (ONLINE/OFFLINE/SUSPENDED) | Ảnh hưởng `checkAllHubs` bỏ Hub OFFLINE, `triggerSmartScheduling` chỉ Hub ONLINE |
| **C16** | `Alert` không có field phân biệt đã gửi hay chưa ngoài `delivered`; `processQueue` cần biết alert nào đến hạn | Bảng 32–35 | Giữ `delivered` + `nextRetryAt` trong map của queue; thêm getter `getPending()` chỉ để test/UI | Nhẹ |

---

## 4. Giả định mới (ngoài A1–A6 của PLAN)

| # | Vấn đề | Giả định |
|---|---|---|
| A7 | Ánh xạ `AlertType` → `PriorityLevel` khi tạo incident từ cảm biến | HUB_OVERLOAD → CRITICAL; DEVICE_OFFLINE → HIGH; CHARGER_FAULT → HIGH; STALE_DATA → MEDIUM |
| A8 | Cảnh báo lặp mỗi chu kỳ khi Hub vẫn ≥90% | Giữ đúng Sub 3: phát mỗi chu kỳ; UI gộp theo `(type, hubId)` để không spam |
| A9 | Mốc tính SLA | `slaDeadline = createdAt + SLA(priority)`; `assignTechnician` đặt lại `now + SLA` (theo Bảng 45) và ghi log |
| A10 | `timeScale` ảnh hưởng timer | Chu kỳ thật = `SCAN_INTERVAL_MS / timeScale`; `tickNow()` chạy đồng bộ đúng 6 bước |
| A11 | "Reservation sắp tới" khi tạm dừng Hub | `status = PENDING_PICKUP` và `startAt > now`; seed HUB-08 có đúng 5 |
| A12 | Biên "quá hạn" | Dùng so sánh **nghiêm ngặt** `>`: đúng 10′ chưa stale, đúng 15′ chưa huỷ |
| A13 | Công thức utilization | `parkingUtil = occupied / parkingCapacity`; `chargingUtil = portsCHARGING / portsKhôngOFFLINE`; lấy `max` |
| A14 | Nội dung cảnh báo | Tiếng Việt, ví dụ: "Hub KTX Khu B đạt 90% sức chứa (54/60 xe) — nguy cơ quá tải" |

Mọi giả định sẽ được ghi lại trong `docs/DESIGN_CHANGES.md` khi áp dụng vào code.

---

## 5. Vỹ cần tự hiểu/giải thích được (GĐ0)

- **Vì sao freeze contract ở GĐ1:** `NetworkStore` + `domain/types` là nền của cả 4 module còn lại; đổi tên field sau GĐ1 sẽ làm vỡ code của Đức/Nhiên/Trường, nên chỉ được **thêm** field.
- **Ba trạng thái khác nhau, đừng lẫn:** `ConnectionStatus` (Hub/xe có kết nối không), `HubStatus` (Hub đang vận hành / tạm dừng), `DeviceStatus` (nghiệp vụ của xe/cổng/chỗ). Ví dụ Hub vẫn Online nhưng bị Operator tạm dừng → `SUSPENDED`.
- **Vì sao inject `Clock`:** để test "14 phút/16 phút" và demo auto-cancel 15 phút mà không phải chờ thật; đồng thời cấm `Date.now()` trong service để không có thời gian "ẩn".
- **18 test case không phải 18 unit test:** chúng là black-box theo use case; phần white-box (biên) là bổ sung ở Bảng 62, không thay thế.
- **Ranh giới thiết kế và MVP:** Sub 3 mô tả PostgreSQL/Redis cho hệ thống thật, nhưng MVP in-memory là hợp lệ theo đề bài; ports/adapters là chỗ để chứng minh "đổi IoT thật/giả lập không sửa nghiệp vụ" (NFR 2.2.6).
