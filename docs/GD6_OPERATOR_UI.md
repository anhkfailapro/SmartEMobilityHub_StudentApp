# GĐ6 — Trung tâm điều hành (WPF)

**Project:** `src/SmartEMobilityHub.Operator` — WPF `net10.0-windows`, tham chiếu `SmartEMobilityHub.Core`, **không sửa app sinh viên**.
**Chạy:**

```bat
cd SmartEMobilityHub_StudentApp
..\.dotnet\dotnet run --project src\SmartEMobilityHub.Operator\SmartEMobilityHub.Operator.csproj
```

Cửa sổ mở ở kích thước 1520×940 (tối thiểu 1240×760). Style dùng lại token Figma của nhóm (`Ui/Theme.cs`): nền `#F1F5F9`, card trắng, primary `#2563EB`, chữ `#0F172A`/`#64748B`.

## 1. Ba cột

| Cột | Nội dung |
|---|---|
| Trái (385 px) | 24 sự cố: mã, thiết bị, mức ưu tiên (thanh màu), trạng thái, thời gian; 5 chip lọc (Tất cả / Mới / Kiểm tra / Đang sửa / Đã xong); ô tìm theo mã, thiết bị, lỗi, Hub |
| Giữa | Chi tiết sự cố: mô tả, người báo, thời điểm; 6 nút hành động; phiếu bảo trì (KTV, SLA còn lại/quá hạn); nhật ký xử lý theo thời gian |
| Phải | KPI (Mới / Kiểm tra-khoá / Đang sửa / Đã xong), số Hub quá tải, số cảnh báo; danh sách 5 KTV (sẵn sàng, khoảng cách); hàng chờ sạc HUB-08; cảnh báo gần đây; nút kịch bản demo; nhật ký hệ thống |

Header: đồng hồ mô phỏng, “Cập nhật N giây trước · chu kỳ 30 giây”, nút **Quét ngay**.
Thanh dưới: thông báo hành động gần nhất.

## 2. Hành động (UC-OP-03)

| Nút | Điều kiện bật | Việc làm |
|---|---|---|
| Khoá thiết bị | sự cố Mới / Đang kiểm tra | `LockDevice` → thiết bị `Locked`, sự cố `Locked` |
| Tạo phiếu bảo trì | sự cố chưa đóng | `CreateMaintenanceTask`, hạn SLA theo priority |
| Gán KTV gần nhất | sự cố chưa đóng/đã xử lý | tạo phiếu nếu chưa có, `SuggestTechnician`, `AssignTechnician` |
| Hoàn tất xử lý | sự cố Đang sửa | `InRepair → Resolved` |
| Đóng sự cố | sự cố Đã xử lý | `Resolved → Closed` |
| Tạm dừng Hub | Hub chưa bị tạm dừng | hỏi xác nhận, `SuspendHub`, huỷ reservation sắp tới, broadcast |

Lỗi nghiệp vụ (sai chuyển trạng thái, không có KTV, Hub không tồn tại) hiện `MessageBox` tiếng Việt; không crash.

## 3. Kịch bản demo panel

| Nút | Tác dụng | Use case |
|---|---|---|
| Cổng CS-08-08 lỗi | bơm lỗi gateway → cổng Faulty, tạo sự cố cảm biến | UC-SYS-02 |
| Mất kết nối HUB-03 | Hub Offline, 4 cổng Offline, tạo sự cố mất kết nối | UC-SYS-02 |
| Dữ liệu cũ HUB-08 | đặt `LastUpdatedAt` lùi 14 phút, phát cảnh báo STALE_DATA | NFR 2.2.3 |
| Mạng cảnh báo lỗi | `NetworkDown = true`, alert HUB-08 vào hàng đợi | UC-SYS-03 4a |
| Khôi phục & gửi lại | bật mạng, tua 31 giây, `ProcessQueue` | UC-SYS-03 4a |
| Sự cố mở khoá | `ReceiveUnlockTimeout("XE-121","R-103")` | UC-S-06 |
| Thêm 3 yêu cầu sạc | tạo 3 request HUB-08 và `RunSchedule` | UC-SYS-01 |
| +1 / +5 / +15 phút | tua `FakeClock` | demo hết hạn giữ chỗ |
| Tự động quét | bật/tắt quét mỗi 30 giây mô phỏng | UC-SYS-02 |
| Tăng tốc ×10 | mỗi giây thật trôi 10 giây mô phỏng | demo |
| Đặt lại demo | tạo lại toàn bộ store/service/clock | — |

## 4. Kịch bản demo gợi ý (5 phút)

1. Mở app, chỉ 24 sự cố và 5 chip lọc; chọn **SC-1024** (Hub HUB-08, Khẩn cấp) → đọc mô tả/nhật ký.
2. Bấm **Tạo phiếu bảo trì** → phiếu MT-0001 xuất hiện với hạn 30 phút (priority Khẩn cấp).
3. Bấm **Gán KTV gần nhất** → T-02 (gần HUB-08 nhất); sự cố sang **Đang sửa**, thiết bị `UnderRepair`.
4. Bấm **Hoàn tất xử lý** rồi **Đóng sự cố** → KPI “Đã xong” tăng.
5. Chọn **SC-1017** (XE-087) → **Khoá thiết bị** → sang cột “Đã khoá”.
6. Bấm **Cổng CS-08-08 lỗi** → sự cố mới xuất hiện, cảnh báo “Cổng sạc lỗi” ở cột phải.
7. Bấm **Mạng cảnh báo lỗi** → cảnh báo HUB-08 vào “Chờ gửi lại”; bấm **Khôi phục & gửi lại** → “Đã gửi”.
8. Bấm **Thêm 3 yêu cầu sạc** → hàng chờ HUB-08 có điểm ưu tiên, 1 cổng được cấp (còn 1 cổng trống, giới hạn 56 kW).
9. Chọn một sự cố thuộc HUB-08 → **Tạm dừng Hub** → xác nhận; 5 đặt chỗ bị huỷ và có broadcast.
10. Bấm **Đặt lại demo** để diễn lại từ đầu.

## 5. Kiểm tra đã chạy

- `dotnet build` toàn solution: **0 warning / 0 error**.
- Chạy app thật: cửa sổ mở, `MainWindowTitle = "Smart E-Mobility Hub · Trung tâm điều hành"`, `Responding = True`; đã đóng tiến trình sau khi kiểm tra.
- **Chưa** bấm thử toàn bộ nút bằng tay; chưa chụp ảnh so với Figma. Khi bảo vệ nên tự bấm lại kịch bản §4.

## 6. Ghi chú kỹ thuật

- Dashboard dùng `FakeClock` + `NoopScheduler`; mỗi giây UI gọi `Tick()` để trôi thời gian và tự chạy `TickNow()` khi đủ 30 giây. Nhờ vậy không có thread nền sửa `ObservableCollection`/`NetworkStore` đang bind.
- `UiLogger` giữ 50 dòng log gần nhất, hiển thị ở cột phải.
- Toàn bộ dữ liệu là seed trong bộ nhớ; **Đặt lại demo** tạo `OperatorDemo` mới và thay `DataContext`.
- App sinh viên (`SmartEMobilityHub.csproj`) chỉ bị sửa để loại `src/**` và `tests/**` khỏi glob; không đổi XAML/logic màn hình sinh viên.
