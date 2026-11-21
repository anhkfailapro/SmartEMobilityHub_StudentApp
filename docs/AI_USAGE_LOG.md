# AI_USAGE_LOG — Khai báo sử dụng Generative AI

> Bắt buộc theo đề bài CO3001 (Submission #final: "A clear statement defining how Generative AI was used — tools, scope, and level of contribution").
> Mỗi mục: ngày · giai đoạn · AI đã sinh/sửa gì (file) · **Vỹ cần tự kiểm tra** · giả định đã dùng.
> Cột "AI đã làm" chỉ ghi phần do AI tạo; phần Vỹ tự viết/sửa lại cần được bổ sung vào cột "Vỹ đã sửa".

**Công cụ dùng:** Buffy — coding agent của Freebuff (mô hình ngôn ngữ, chạy trong IDE).
**Cách dùng dự kiến:** hỗ trợ scaffold TypeScript, sinh seed theo đặc tả, gợi ý test, soát lỗi biên. Vỹ chịu trách nhiệm đọc hiểu, chạy thử, chỉnh sửa và giải thích khi bảo vệ.

---

## 2026-11-21 — GĐ0: đọc tài liệu, lập kế hoạch, rà mâu thuẫn

**AI đã làm**

| File | Nội dung |
|---|---|
| `docs/GD0_REVIEW.md` | Tổng hợp nguồn đã đọc; kế hoạch thực thi theo 7 giai đoạn; cây thư mục; contract GĐ1; mapping 18 test case → file; 16 mâu thuẫn (C1–C16) kèm đề xuất; giả định mới A7–A14; câu hỏi Q1–Q10 |
| `docs/DESIGN_CHANGES.md` | 19 sai lệch dự kiến so với Sub 3 (DC-01…DC-19), trạng thái "Đề xuất" |
| `docs/AI_USAGE_LOG.md` | File này |
| `docs/_extract/*.txt` | Trích text từ 4 PDF để đối chiếu (công cụ `pdftotext`; PDF gốc bị lỗi font, mất dấu tiếng Việt) |

**Không sinh code** ở giai đoạn này (đúng yêu cầu "GĐ0 chưa viết code").

**Vỹ cần tự kiểm tra**

1. Đọc `GD0_REVIEW.md` mục 3 và xác nhận từng đề xuất C1–C16 — đây là các điểm sẽ ảnh hưởng code của cả nhóm.
2. Đối chiếu lại **kiểu trả về** của Bảng 29/37/41 trong bản Word/PDF gốc (mục C4) — bản trích text bị dồn cột.
3. Trả lời Q1 (stack/repo) và Q2 (Figma) trước khi sang GĐ1.
4. Kiểm tra `SmartEMobilityHub_StudentApp/` có phải phần của nhóm đang dùng hay chỉ là demo cũ.

**Giả định đã dùng**

- A1–A6 của PLAN (MAX_RETRY = 5; urgency = clamp(1 − phútCònLại/240, 0, 1); peakDemand = 0,5; SLA 30′/2h/8h/24h; enum `HubStatus` riêng; 7 kW/cổng).
- A7–A14 mới (mapping AlertType → PriorityLevel; phát cảnh báo mỗi chu kỳ; SLA tính từ lúc tạo; chu kỳ thật = 30s / timeScale; "reservation sắp tới" là `PENDING_PICKUP` với `startAt > now`; so sánh quá hạn nghiêm ngặt `>`; utilization = max(parking, charging); thông điệp cảnh báo tiếng Việt).

**Mức đóng góp của AI ở GĐ0:** ~90% phần soạn thảo tài liệu (tổng hợp, đối chiếu bảng); 100% phần phát hiện mâu thuẫn là do AI đọc và đối chiếu — **Vỹ phải tự xác nhận lại**, vì đây là các quyết định ảnh hưởng điểm bảo vệ.

---

### Mẫu cho các giai đoạn sau

```
## YYYY-MM-DD — GĐn: <tên>
**AI đã làm:** file → nội dung
**Vỹ đã sửa:** <liệt kê chỗ Vỹ tự chỉnh/viết lại>
**Vỹ cần tự kiểm tra:** 3–5 gạch đầu dòng
**Giả định đã dùng:** ...
```

## 2026-11-21 — GĐ1: Core C#/.NET (domain, clock, store, ports, seed)

**Bối cảnh:** sau khi Vỹ xác nhận app của nhóm là C#/WPF, toàn bộ kế hoạch TypeScript được chuyển sang C#. Máy chưa có .NET SDK nên AI cài SDK **cục bộ** vào `./.dotnet/` (ngoài repo, không commit).

**AI đã làm**

| Nhóm file | Nội dung |
|---|---|
| `src/SmartEMobilityHub.Core/Domain/` | 14 file: enum Bảng 27, MobilityHub, Vehicle, ChargingPoint, Reservation, IncidentRecord (+log), MaintenanceTask, Technician, Alert, ChargingRequest/Plan, Operator, NetworkSnapshot, ServiceConstants, exception |
| `src/SmartEMobilityHub.Core/Clock/Clock.cs` | `IClock`, `SystemClock`, `FakeClock`, `ScaledClock` |
| `src/SmartEMobilityHub.Core/Store/NetworkStore.cs` | Store observable, `Snapshot()` clone sâu, ID tuần tự |
| `src/SmartEMobilityHub.Core/Ports/` | 5 interface + kiểu telemetry |
| `src/SmartEMobilityHub.Core/Seed/` | 12 Hub/60 cổng/46 xe/24 sự cố/5 KTV/8 reservation |
| `tests/SmartEMobilityHub.Tests/` | Runner console + 7 seed check (không NuGet) |
| `SmartEMobilityHub.csproj`, `.slnx` | Loại `src/**`, `tests/**` khỏi glob của app; thêm 2 project |
| `docs/GD1_CORE_CSHARP.md`, `docs/DESIGN_CHANGES.md` | Bàn giao GĐ1 + DC-20…DC-26 |

**Kết quả chạy:** build solution 0 warning/0 error; 7/7 seed check PASS (xem GD1 §5).

**Vỹ cần tự kiểm tra**

1. Mở `Domain/Enums.cs` và đối chiếu từng giá trị với Bảng 27 — đặc biệt `DeviceStatus` có thêm `Relocating`, `Offline`.
2. Đọc `HubSeed.Build` và tự tính lại 54/60, 5 cổng Charging, cổng 07 = 72°C, cổng 04 cũ 14 phút.
3. Chạy `../.dotnet/dotnet run --project tests/SmartEMobilityHub.Tests` và tự sửa một assert để thấy nó fail (hiểu runner hoạt động).
4. Giải thích được vì sao `OccupiedParkingSlots` là bộ đếm độc lập và vì sao `Snapshot()` phải clone sâu.
5. Xác nhận với Trường/Nhiên: dùng Core riêng (không đụng `MockDataStore`) có ổn cho buổi demo chung không.

**Giả định đã dùng:** A1–A6 (PLAN) + A9 (SLA), A12 (biên `>`), A13 (utilization) + DC-25/DC-26 (bộ đếm chỗ đỗ, HoldUntil). Chưa dùng A7/A8 (GĐ2).

**Mức đóng góp của AI:** ~95% mã GĐ1 do AI sinh; Vỹ cần đọc hiểu và có thể viết lại phần seed/assert để tự tin bảo vệ.

---

## 2026-11-21 — GĐ2–GĐ4: dịch vụ lõi, timer, lập lịch sạc, quản lý sự cố

**AI đã làm**

| Nhóm file | Nội dung |
|---|---|
| `Core/Ports/` | `IAlertSink`, `ISmartSchedulingService`, `PortTelemetry.LastUpdatedAt` |
| `Core/Adapters/` | `MockIoTSensorGateway` (fault injection), `InAppNotificationService`, `InMemoryReservationAdapter`, `ConsoleSystemLogger`, `ThreadPoolScheduler` |
| `Core/Services/` | `HubStatusSyncService`, `ThresholdMonitor`, `AlertRetryQueue`, `AutoCancelService`, `StatusUpdaterTimer` (6 bước), `SmartChargingScheduler`, `IncidentManager` |
| `Core/Domain/` | `MobilityHub.PowerLimitKw` cho phép chỉnh; `MaintenanceTask.Assign(tech, now, sla)`; hằng `SupportHotline` |
| `docs/` | `GD2_GD4_SERVICES.md`, cập nhật `DESIGN_CHANGES.md` (DC-27…DC-33) |

**Kết quả chạy:** build 0 warning/0 error; test GĐ5 30/30 PASS.

**Vỹ cần tự kiểm tra**

1. Đọc `StatusUpdaterTimer.OnTick` và chỉ ra chỗ mỗi bước được bọc try/catch — thử bật `StepFailureInjector` cho bước 2 để thấy bước 3–6 vẫn chạy.
2. Tự tính điểm ưu tiên của CR-A (pin 10%, cần sau 30 phút, peak 0,5) = 83,125; giải thích vì sao urgency bị kẹp trong [0,1].
3. Giải thích vì sao `AlertRetryQueue` phải nhận `IClock` thay vì `DateTime.Now`, và vì sao alert chỉ bị loại sau 5 lần.
4. So lại `SuspendHub` với Bảng 41: vì sao chỉ huỷ reservation `PendingPickup` có `StartsAt > now`?
5. Kiểm tra `HandleFaultyChargingPoint`: khi không còn cổng, vì sao xe về `NeedsCharge` và yêu cầu lên đầu hàng chờ.

**Giả định đã dùng:** A1 (MAX_RETRY 5), A2 (urgency 240 phút), A3 (peak 0,5), A4 (SLA 30′/2h/8h/24h), A6 (7 kW/cổng), A7 (alert → priority), A9 (SLA từ lúc tạo), A12 (biên `>`), A13 (utilization max), A14 (thông điệp tiếng Việt). Thêm: Hub `Suspended` không bị bước 1 đưa về Online; log cảnh báo cổng chỉ phát khi trạng thái **đổi** (DC-27, DC-29).

**Mức đóng góp của AI:** ~95% mã GĐ2–GĐ4 do AI sinh; Vỹ cần đọc hiểu và tự chạy lại kịch bản.

---

## 2026-11-21 — GĐ5: 18 test case + test biên

**AI đã làm**

| File | Nội dung |
|---|---|
| `tests/.../Support/TestWorld.cs` | Dựng seed + đủ service trên `FakeClock`, `ManualScheduler` |
| `tests/.../UseCase/TCSYS02Tests.cs` … `TCOP03Tests.cs` | 18 test theo Bảng 62–67 |
| `tests/.../Unit/BoundaryTests.cs` | 5 test biên (ngưỡng, dữ liệu cũ, điểm ưu tiên, SLA) |
| `tests/.../ReportWriter.cs` | Sinh `tests/report/results.md` + `results.json` |
| `docs/GD5_TESTS.md` | Bảng kết quả + ghi chú dữ liệu test |

**Kết quả chạy thật:** `30/30 test đạt.` (18 case + 5 unit + 7 smoke), exit code 0.

**Vỹ cần tự kiểm tra**

1. Tự chạy lại lệnh test và đọc `tests/report/results.md`; thử sửa một assert để thấy báo cáo ghi FAIL.
2. Giải thích vì sao TCOP03-01 dùng XE-112 chứ không phải XE-204 (SC-1023 đang mở → gộp).
3. Kiểm tra TCSYS04-01: vì sao chỉ R-101 bị huỷ, R-102 và R-103 không.
4. Xem lại TCSYS01-03: vì sao 4 yêu cầu = 28 kW nhưng chỉ 2 cổng được cấp khi `PowerLimitKw = 20`.

**Giả định đã dùng:** như GĐ2–GĐ4 + DC-34.

**Mức đóng góp của AI:** ~95% mã test do AI sinh; Vỹ phải tự hiểu từng assert trước khi bảo vệ.

---

## 2026-11-21 — GĐ6: dashboard WPF "Quản lý sự cố & Bảo trì"

**AI đã làm**

| File | Nội dung |
|---|---|
| `src/SmartEMobilityHub.Operator/` | Project WPF `net10.0-windows`, tham chiếu Core, không NuGet |
| `App.xaml`, `MainWindow.xaml` | Style theo token Figma của nhóm; 3 cột: danh sách – chi tiết/hành động – KPI/demo |
| `ViewModels/MainViewModel.cs`, `Models/Rows.cs` | Lọc/tìm, KPI, hành động UC-OP-03, kịch bản demo |
| `OperatorDemo.cs`, `Infrastructure/` | Seed + service + `FakeClock`, logger hiển thị, scheduler do UI điều khiển |
| `docs/GD6_OPERATOR_UI.md` | Mô tả màn hình + kịch bản demo 10 bước |

**Kiểm tra đã chạy:** build solution 0 warning/0 error; chạy app thật → cửa sổ mở, title đúng, `Responding = True`. **Chưa** bấm thử toàn bộ nút / so ảnh với Figma.

**Vỹ cần tự kiểm tra**

1. Chạy `dotnet run --project src\SmartEMobilityHub.Operator` và bấm đủ 6 nút hành động; thử cả trường hợp sai (đóng sự cố khi chưa xử lý) để thấy MessageBox.
2. Kiểm tra seed hiển thị đúng 24 sự cố / 12-7-5 và HUB-08 54/60.
3. Thử "Mạng cảnh báo lỗi" rồi "Khôi phục & gửi lại" — giải thích vì sao phải tua 31 giây.
4. Chụp ảnh màn hình so với Figma frame `23-4` (nếu có quyền xem) và chỉnh khoảng cách/màu nếu lệch.
5. Xác nhận với nhóm: dashboard là cửa sổ riêng, không thay màn hình sinh viên.

**Giả định đã dùng:** như trên + DC-33 (UI riêng, tick do UI điều khiển).

**Mức đóng góp của AI:** ~95% mã UI do AI sinh; phần đánh giá thẩm mỹ và kịch bản bảo vệ là của Vỹ.
