# GĐ8 — Review codebase bằng knowledge graph (Graphify)

Ngày: 23/11/2026 · Bản gốc: `main` @ `e69d9af` · Công cụ: Graphify 0.9.115 (`graphify update .`, chỉ phân tích AST, không gọi LLM)

## 1. Knowledge graph nói gì về codebase

| Chỉ số | Giá trị |
|---|---|
| File được phân tích | 164 (C#, XAML, Markdown, csproj) |
| Node / cạnh / cụm | 1.804 / 4.518 / 98 (sau khi sửa: 1.860 / 4.537 / 93) |
| Vòng phụ thuộc (import cycle) | Không có |
| Node gần như cô lập (≤ 1 cạnh) | 703, phần lớn là thuộc tính DTO và giá trị enum |

**God node** (nhiều cạnh nhất): `Window` (92), `MainViewModel` (73), namespace `SmartEMobilityHub.Models` (69), `NetworkStore` (61), `MockDataStore` (49), `MobilityHub` (45), `OperatorDemo` (44).

**Nhận xét kiến trúc rút ra từ graph**

1. **Có hai "thế giới" dữ liệu không nối với nhau.** App sinh viên ở thư mục gốc (`MockDataStore`, `Models/`, `Services/`) và lõi `src/SmartEMobilityHub.Core` (`NetworkStore`, `Domain/`) là hai cụm tách rời: không có cạnh nào giữa chúng. Nhiều khái niệm bị định nghĩa hai lần với tên trùng nhau: `Vehicle`, `ChargingRequest`, `ChargingPoint`, `IncidentStatus`, `IncidentSource`. Csproj gốc còn chủ động loại `src/**` khỏi quá trình biên dịch. Đây là rủi ro lớn nhất khi tích hợp (DC-23 đã ghi nhận việc phải map `DeviceStatus` ↔ `VehicleStatus`).
2. **`MainViewModel` và `OperatorDemo` là hai cây cầu duy nhất** (betweenness ≈ 0,137) nối UI vận hành với lõi. Mọi thay đổi ở lõi đều đi qua hai lớp này, nên đây là nơi cần test tích hợp đầu tiên.
3. **Các service lõi đều đi qua `NetworkStore`** (61 cạnh) với `List<T>` công khai và có thể sửa trực tiếp. Hiện tại chạy ổn vì UI điều khiển tick trên một luồng, nhưng `ThreadPoolScheduler` chạy trên thread pool. Nếu sau này dùng nó cùng UI thì sẽ xảy ra tranh chấp dữ liệu.
4. Graph dẫn tới bốn service có vòng đời phức tạp nhất: `StatusUpdaterTimer`, `HubStatusSyncService`, `SmartChargingScheduler`, `IncidentManager`. Đọc kỹ bốn service này tìm ra các lỗi ở mục 2.

## 2. Lỗi đã sửa (có test hồi quy)

Mỗi lỗi có một test `REG-xx` trong `tests/SmartEMobilityHub.Tests/Unit/RegressionTests.cs`. Cả 8 test đều **đỏ trên bản gốc** và **xanh sau khi sửa**.

| Test | Mức độ | Lỗi | Sửa | DC |
|---|---|---|---|---|
| REG-01 | Cao | HUB-08 ở mức 90% sinh cảnh báo quá tải mới **mỗi 30 giây**: 3 chu kỳ tạo ra 3 cảnh báo | Cảnh báo theo đợt: chỉ báo khi bắt đầu quá tải, hết quá tải thì cho phép báo lại | DC-37 |
| REG-02 | Cao | Mất mạng cảm biến: 12 Hub × mỗi chu kỳ = 36 cảnh báo `DEVICE_OFFLINE` sau 3 chu kỳ | Chỉ báo ở lần chuyển ONLINE → OFFLINE | DC-37 |
| REG-03 | Trung bình | `CheckStaleData` phát cảnh báo `STALE_DATA` mới mỗi lần gọi | Cảnh báo theo đợt | DC-37 |
| REG-04 | **Nghiêm trọng** | Sau **một lần** mất mạng cảm biến, mọi cổng sạc của mọi Hub **kẹt OFFLINE vĩnh viễn**. Nguyên nhân: `MarkOffline` ghi đè trạng thái cổng, rồi gateway giả lập đọc lại chính trạng thái đó từ store | Lưu trạng thái cổng trước khi mất kết nối, khôi phục khi kết nối lại | DC-38 |
| REG-05 | Cao | `AssignTechnician` không chuyển KTV sang bận: một người có thể nhận vô hạn phiếu, và "gợi ý KTV gần nhất" luôn trả về cùng một người | KTV chuyển bận khi được gán; gán lại thì trả người cũ về rảnh | DC-39 |
| REG-06 | Cao | "Hoàn tất xử lý" chỉ đổi trạng thái sự cố. Xe/cổng kẹt `UNDER_REPAIR`/`LOCKED`, phiếu không bao giờ `COMPLETED`, KTV không được giải phóng | Thêm `ResolveIncident`; `UpdateIncidentStatus(Resolved)` đi qua hàm này, nên UI không cần sửa | DC-39 |
| REG-07 | Trung bình | UC-SYS-01 5a: xe bị gián đoạn sạc được đưa lên đầu hàng chờ, nhưng chu kỳ lập lịch kế tiếp xếp lại theo điểm và đẩy xe xuống | Ghim yêu cầu (`Pinned = true`) | DC-40 |
| REG-08 | Tính năng thiếu | Có `SuspendHub` nhưng không có đường mở lại Hub | `ResumeHub` + nút "Mở lại Hub" trên dashboard | DC-41 |

Ngoài ra cảnh báo "vượt công suất" của `SmartChargingScheduler` cũng được chuyển sang báo theo đợt (cùng lỗi với REG-01).

**Kết quả:** Core + Tests build 0 warning/0 error, **38/38 test PASS** (30 test cũ + 8 test mới). Sau đợt sửa thứ hai (mục 3): **44/44**.

## 3. Đợt sửa thứ hai — các điểm còn mở

Cả 6 điểm dưới đây đã được sửa, mỗi điểm có test hồi quy. Riêng O-6 là việc kiến trúc lớn nên chưa làm (xem cuối mục).

| # | Test | Đã làm | DC |
|---|---|---|---|
| O-1 | REG-09 | Giữ hành vi RESERVED theo TCOP03-05. Xe được giải phóng khi sự cố được xử lý xong qua `ResolveIncident` (DC-39), nên không còn kẹt | DC-39 |
| O-2 | REG-10 | Giới hạn 5 lần gia hạn khi đang quét QR (giả định A10) | DC-42 |
| O-3 | REG-11 | Timeout không còn làm cổng FAULTY | DC-43 |
| O-4 | REG-12 | Kiểm tra công suất tính cả các cổng đang sạc | DC-43 |
| O-5 | REG-13 | Thêm `AlertType.PowerLimit` và nhãn "Vượt công suất" trên dashboard | DC-44 |
| O-7 | REG-14 | Snapshot tìm xe không phân biệt hoa thường | DC-43 |

**O-6 (chưa làm):** để app sinh viên dùng chung Core cần thay `MockDataStore` và toàn bộ `Models/` bằng adapter sang `NetworkStore`, tức là viết lại phần lớn app sinh viên. Đây là quyết định phạm vi của cả nhóm, không phải một bản sửa lỗi.

### Bảng gốc của các điểm còn mở (để tham khảo)

| # | Vị trí | Vấn đề | Đề xuất |
|---|---|---|---|
| O-1 | `IncidentManager.ReceiveUnlockTimeout` | Huỷ đặt chỗ (xe về `Available`) rồi lại đặt xe về `Reserved`, nên xe bị giữ mà không còn đặt chỗ nào. `AutoCancelService` không bao giờ giải phóng được xe này | Hỏi lại Sub 3 xem "giữ RESERVED" kéo dài bao lâu. Nếu chỉ chờ người vận hành kiểm tra thì nên đặt `UnderCheck` |
| O-2 | `AutoCancelService` | Khi `IsPickingUp = true`, đơn được gia hạn thêm 1 phút ở **mỗi** chu kỳ, không có giới hạn | Thêm số lần gia hạn tối đa (cần chốt con số trong Sub 3 trước) |
| O-3 | `SmartChargingScheduler.AllocatePower` | Lệnh cấp nguồn bị **Timeout** cũng đánh dấu cổng `FAULTY`. Một lần mạng chập chờn làm cổng hỏng cho tới khi có người xử lý | Phân biệt Timeout (thử lại ở chu kỳ sau) với Fault |
| O-4 | `RunSchedule` | `TotalPowerKw` chỉ tính các yêu cầu đang chờ, bỏ qua các cổng đang sạc, trong khi `AllocatePower` có trừ phần đang dùng. Hai con số lệch nhau | Cộng phần công suất đang sạc vào `TotalPowerKw` |
| O-5 | Cảnh báo vượt công suất | Dùng `AlertType.HubOverload` nên bị coi là CRITICAL như quá tải chỗ đỗ (A7) | Thêm `AlertType.PowerLimit` |
| O-6 | Kiến trúc (mục 1.1) | App sinh viên và Core trùng model, chưa tích hợp | Lập kế hoạch cho app sinh viên dùng Core qua adapter, hoặc ghi rõ phạm vi demo |
| O-7 | `NetworkSnapshot.FindVehicle` | Phân biệt hoa thường, trong khi `NetworkStore.FindVehicle` thì không | Thống nhất dùng `OrdinalIgnoreCase` |

## 4. Nhánh `operator-dispatching` (mới trên remote, chưa merge)

Nhánh này build được và merge vào bản đã sửa **không xung đột**. Tuy vậy `VehicleRedistributor` (212 dòng) **chưa có test nào**, và có các vấn đề sau:

1. Khi Hub không đủ xe thật, hàm tự sinh **mã xe giả** (`HUB-08-V1`, …) vào lệnh điều phối. `ExecuteRedistribution` sau đó vẫn trừ `OccupiedParkingSlots` theo cả số xe giả.
2. `CreateOrder` không kiểm tra xe có thuộc Hub nguồn và đang `Available` hay không. Có thể điều phối xe đang sạc hoặc đang chạy, và một xe có thể nằm trong hai lệnh cùng lúc.
3. `GenerateDispatchSuggestions` không trừ `deficit` của Hub nhận sau mỗi lệnh. Nhiều Hub cho có thể cùng đổ vào một Hub nhận vượt mức mục tiêu.
4. `CompleteOrder` âm thầm cắt số xe theo `ParkingCapacity`. Nếu Hub đích đã đầy thì xe "biến mất" khỏi bộ đếm.
5. Các ngưỡng 0,85 / 0,35 / 0,65 và tốc độ 25 km/h nằm cứng trong code, trái quy ước "hằng số ở `ServiceConstants`". Công thức Haversine bị viết lại trong khi `GeoPoint.DistanceKmTo` đã có sẵn.
6. `NetworkSnapshot` chưa chứa `DispatchOrders`.

**Đã sửa trên nhánh `operator-dispatching-fixes`** (tách từ `operator-dispatching`), có 8 test `DSP-01..08`. 6 test đầu đỏ trên code gốc và xanh sau khi sửa. Chi tiết xem `docs/GD8_DISPATCH_FIXES.md` trên nhánh đó.

## 5. Cách chạy lại

```bash
dotnet run --project tests/SmartEMobilityHub.Tests   # 44 test
pip install graphifyy && graphify update .           # dựng lại graph → graphify-out/
```

Mở `graphify-out/graph.html` trong trình duyệt để xem graph tương tác. `graphify-out/GRAPH_REPORT.md` là bản tóm tắt.

**Lưu ý:** project WPF `SmartEMobilityHub.Operator` chưa được biên dịch trong môi trường review (thiếu WPF targeting pack). Cần build trên Windows để xác nhận nút "Mở lại Hub".
