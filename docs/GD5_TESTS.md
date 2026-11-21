# GĐ5 — Kiểm thử

**Chạy:** `..\.dotnet\dotnet run --project tests\SmartEMobilityHub.Tests\SmartEMobilityHub.Tests.csproj`
**Kết quả lần chạy cuối (21/11/2026): 30/30 PASS** — 18 test case bắt buộc, 5 test biên, 7 smoke seed.
**Báo cáo tự sinh:** `tests/report/results.md` và `tests/report/results.json`.

## 1. Cách tổ chức

- Console runner tự viết (`TestRunner` + `Assert`), **không NuGet** theo ràng buộc của nhóm.
- Mỗi test dựng một `TestWorld` mới: `FakeClock` cố định `2026-11-21 09:41`, seed sạch, đủ service, `MockIoTSensorGateway` tắt drift pin; không gọi `Start()` nên không có timer nền.
- Test gọi `TickNow()` hoặc service trực tiếp; thời gian tua bằng `FakeClock` nên kết quả tất định.
- TCSYS01-01 còn kiểm tra `ManualReorder`: yêu cầu bị ghim phải giữ vị trí sau khi `RunSchedule` tính lại điểm.
- Test fail in rõ `Mã · Tên` + exception; runner trả exit code 1 nếu có case fail (không bỏ qua).

## 2. 18 test case bắt buộc

| Mã | Nội dung | Bảng | Kết quả |
|---|---|---|---|
| TCSYS02-01 | Đồng bộ trạng thái Hub theo chu kỳ 30 giây | 63 | PASS |
| TCSYS02-02 | Mất kết nối cổng không chặn Hub khác | 63 | PASS |
| TCSYS02-03 | Dữ liệu cũ quá 10 phút mới cảnh báo | 63 | PASS |
| TCSYS03-01 | Hub 54/60 (90%) sinh cảnh báo quá tải | 64 | PASS |
| TCSYS03-02 | Hub 53/60 (88,3%) không sinh cảnh báo | 64 | PASS |
| TCSYS03-03 | Gửi cảnh báo lỗi mạng được xếp hàng và gửi lại sau 30 giây | 64 | PASS |
| TCSYS04-01 | Đặt chỗ quá 15 phút bị huỷ và trả tài nguyên | 65 | PASS |
| TCSYS04-02 | Đặt chỗ 14 phút chưa bị huỷ (biên 15 phút) | 65 | PASS |
| TCSYS04-03 | Người dùng đang quét QR được gia hạn, không bị huỷ | 65 | PASS |
| TCSYS01-01 | Xếp hạng ưu tiên sạc theo pin, thời gian cần và cao điểm | 62 | PASS |
| TCSYS01-02 | Đổi trọng số: tổng khác 1 bị từ chối, cấu hình cũ giữ nguyên | 62 | PASS |
| TCSYS01-03 | Kế hoạch vượt công suất bị phát hiện và không cấp quá tải | 62 | PASS |
| TCSYS01-04 | Cổng lỗi: chuyển xe sang cổng trống, hết cổng thì đưa lên đầu hàng chờ | 62 | PASS |
| TCOP03-01 | Tiếp nhận báo cáo sự cố từ sinh viên | 67 | PASS |
| TCOP03-02 | Báo cáo trùng được gộp vào sự cố đang mở | 67 | PASS |
| TCOP03-03 | Khoá thiết bị, tạo phiếu bảo trì và gán kỹ thuật viên | 67 | PASS |
| TCOP03-04 | Tạm dừng Hub huỷ đúng các đặt chỗ sắp tới | 67 | PASS |
| TCOP03-05 | Mở khoá quá 30 giây không xác nhận | 67 | PASS |

## 3. Test biên bổ sung

| Mã | Nội dung | Điểm biên |
|---|---|---|
| UNIT-01 | Ngưỡng quá tải | 54/60 = 90% là quá tải; 53/60 không; capacity 0 không chia cho 0 |
| UNIT-02 | Cấu hình ngưỡng | 0 và 1,2 bị từ chối; 0,8 thì 48/60 quá tải, 47/60 không |
| UNIT-03 | Dữ liệu cũ | Đúng 10 phút chưa cảnh báo; quá 1 giây mới cảnh báo |
| UNIT-04 | Điểm ưu tiên | Pin 0% + cần ngay = 92,5; pin 100% + còn 300 phút = 7,5; pin 101% bị từ chối |
| UNIT-05 | SLA | Đúng hạn chưa vi phạm; quá 1 giây vi phạm; phiếu Completed không tính |

## 4. Ghi chú về dữ liệu test (đã ghi trong `DESIGN_CHANGES.md` DC-34)

- **TCOP03-01 dùng XE-112**, không dùng XE-204 như bảng kịch bản: seed đã có **SC-1023 mở cho XE-204**. Nếu báo XE-204, hệ thống đúng đắn sẽ **gộp** vào SC-1023 (đó chính là TCOP03-02) và test “tạo mới” sẽ không còn kiểm tra đúng điều cần kiểm tra. XE-112 không có sự cố mở nên tạo mới sạch.
- **TCSYS02-02** bơm lỗi cho `CS-08-04` (vốn đã Offline trong seed) **và** `CS-01-02` (đang Available) để chứng minh cả hai điều: cổng mất kết nối giữ nguyên trạng thái, và một cổng vừa mất kết nối không chặn các Hub khác; log WARN chỉ phát khi cổng **chuyển** sang Offline (tránh spam mỗi 30 giây).
- TCSYS01-01/03 dùng HUB-05 (4 cổng, chưa sạc) và đánh Faulty 2 cổng để kiểm soát số cổng trống; không sửa seed gốc.
- TCSYS04-01 chạy trên seed: R-101 quá hạn 16 phút (huỷ), R-102 còn hạn 14 phút (giữ), R-103 quá hạn nhưng `isPickingUp` (gia hạn).

## 5. Kết quả thô

```
30/30 test đạt.
```

Chi tiết từng case (thời điểm, mã, PASS/FAIL, lỗi nếu có) nằm trong `tests/report/results.md` và `results.json`. File được ghi lại **mỗi lần chạy**, nên nếu sửa code mà quên chạy lại, báo cáo sẽ cũ — luôn chạy lại trước khi nộp.

## 6. Điều chưa kiểm thử

- Chưa có UI test tự động cho dashboard WPF (kiểm tra thủ công: cửa sổ mở, title đúng, `Responding = True`; chưa bấm hết từng nút).
- Chưa test đa luồng với `ThreadPoolScheduler` thật (dashboard dùng `NoopScheduler` + `TickNow()` để tránh sửa dữ liệu từ thread nền).
- Chưa test với adapter IoT/notification thật (mới có mock).
