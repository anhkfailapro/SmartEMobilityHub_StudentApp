# Kết quả kiểm thử — Smart E-Mobility Hub (Core)

- Thời điểm chạy: 2026-11-21 23:43:12
- Tổng số: **30** · Đạt: **30** · Không đạt: **0**
- Nguồn: `tests/SmartEMobilityHub.Tests` (console runner, không dùng NuGet)

| Mã | Tên kịch bản | Kết quả | Chi tiết lỗi |
| --- | --- | --- | --- |
| SMOKE-01 | Seed đủ 12 Hub | PASS | — |
| SMOKE-02 | HUB-08 đúng 54/60 và 8 cổng theo Figma | PASS | — |
| SMOKE-03 | Seed đúng 24 sự cố (12 mới / 7 kiểm tra / 5 sửa chữa) | PASS | — |
| SMOKE-04 | Đủ 7 mã sự cố hero | PASS | — |
| SMOKE-05 | Hero vehicle/technician/reservation | PASS | — |
| SMOKE-06 | Snapshot là bản sao sâu, không ghi ngược store | PASS | — |
| SMOKE-07 | FakeClock tua thời gian được | PASS | — |
| TCSYS02-01 | Đồng bộ trạng thái Hub theo chu kỳ 30 giây | PASS | — |
| TCSYS02-02 | Mất kết nối cổng CS-08-04 không chặn Hub khác | PASS | — |
| TCSYS02-03 | Dữ liệu cũ quá 10 phút mới cảnh báo | PASS | — |
| TCSYS03-01 | Hub 54/60 (90%) sinh cảnh báo quá tải | PASS | — |
| TCSYS03-02 | Hub 53/60 (88,3%) không sinh cảnh báo | PASS | — |
| TCSYS03-03 | Gửi cảnh báo lỗi mạng được xếp hàng và gửi lại sau 30 giây | PASS | — |
| TCSYS04-01 | Đặt chỗ quá 15 phút bị huỷ và trả tài nguyên | PASS | — |
| TCSYS04-02 | Đặt chỗ 14 phút chưa bị huỷ (biên 15 phút) | PASS | — |
| TCSYS04-03 | Người dùng đang quét QR được gia hạn, không bị huỷ | PASS | — |
| TCSYS01-01 | Xếp hạng ưu tiên sạc theo pin, thời gian cần và cao điểm | PASS | — |
| TCSYS01-02 | Đổi trọng số: tổng khác 1 bị từ chối, cấu hình cũ giữ nguyên | PASS | — |
| TCSYS01-03 | Kế hoạch vượt công suất bị phát hiện và không cấp quá tải | PASS | — |
| TCSYS01-04 | Cổng lỗi: chuyển xe sang cổng trống, hết cổng thì đưa lên đầu hàng chờ | PASS | — |
| TCOP03-01 | Tiếp nhận báo cáo sự cố từ sinh viên | PASS | — |
| TCOP03-02 | Báo cáo trùng được gộp vào sự cố đang mở | PASS | — |
| TCOP03-03 | Khoá thiết bị, tạo phiếu bảo trì và gán kỹ thuật viên | PASS | — |
| TCOP03-04 | Tạm dừng Hub huỷ đúng các đặt chỗ sắp tới | PASS | — |
| TCOP03-05 | Mở khoá quá 30 giây không xác nhận | PASS | — |
| UNIT-01 | Ngưỡng quá tải: 90% tính là quá tải, dưới 90% thì không | PASS | — |
| UNIT-02 | Ngưỡng cấu hình phải trong (0, 1] | PASS | — |
| UNIT-03 | Dữ liệu cũ: đúng 10 phút chưa cảnh báo, quá 1 giây thì cảnh báo | PASS | — |
| UNIT-04 | Điểm ưu tiên: pin 0% + cần ngay = 92,5; pin 100% + còn xa = 7,5 | PASS | — |
| UNIT-05 | SLA: đúng hạn chưa vi phạm, quá 1 giây thì vi phạm; đã xong thì không tính | PASS | — |
