# GĐ8 — Sửa nhánh điều phối xe (`operator-dispatching-fixes`)

Nhánh này tách từ `operator-dispatching` (commit `b94a32c`). Nó sửa các lỗi trong `VehicleRedistributor` mà review bằng knowledge graph phát hiện (`docs/GD8_REVIEW.md` §4 trên nhánh `gd8-review-fixes`).

## Lỗi đã sửa

| Test | Lỗi trên code gốc | Sửa |
|---|---|---|
| DSP-01 | Hub cho không đủ xe thật thì sinh **mã xe giả** (`HUB-08-V1`) vào gợi ý; ViewModel cũng tự thêm `HUB-xx-XE-01` khi tạo lệnh | Chỉ dùng xe thật, đang `Available` ở Hub cho, chưa nằm trong lệnh khác. ViewModel báo "chỉ còn N xe" thay vì bù xe giả |
| DSP-02 | Nhiều Hub cho cùng đổ vào một Hub nhận vượt mức thiếu, vì phần thiếu không bị trừ dần | Theo dõi phần thiếu còn lại của từng Hub nhận, tính cả xe đang trên đường tới |
| DSP-03 | `CreateOrder` nhận xe không tồn tại, xe đang sạc, xe ở Hub khác, xe đã có lệnh | Kiểm tra từng xe, ném `IllegalArgumentException` kèm lý do |
| DSP-04 | Không kiểm tra chỗ trống ở Hub đích | Từ chối khi Hub đích không đủ chỗ (đã trừ xe của các lệnh đang mở) |
| DSP-05 | Xuất phát cả khi xe đã bị đặt hoặc đổi trạng thái sau lúc tạo lệnh; vẫn trừ chỗ Hub nguồn | Mọi xe phải còn `Available` tại Hub nguồn, nếu không thì lệnh giữ nguyên `Pending` |
| DSP-06 | `CompleteOrder` âm thầm cắt số xe theo sức chứa nên xe "biến mất" khỏi bộ đếm | Hub đích không đủ chỗ thì trả `false`, lệnh giữ `InProgress` |
| DSP-07 | Không có cách huỷ lệnh; snapshot thiếu `DispatchOrders` | Thêm `CancelOrder` (lệnh `Pending`), `DispatchOrder.Clone`, snapshot có `DispatchOrders` |
| DSP-08 | Viết lại công thức Haversine; ngưỡng 0,85/0,35/0,65 và tốc độ 25 km/h nằm cứng trong code | Dùng `GeoPoint.DistanceKmTo`; đưa hằng số vào `ServiceConstants.Dispatch*` |

DSP-01..06 đỏ trên `VehicleRedistributor` gốc và xanh sau khi sửa. DSP-07/08 kiểm tra API mới.

**Kết quả:** build 0 warning/0 error, **38/38 test PASS** (30 test cũ + 8 test DSP). Thay đổi UI chỉ ở `MainViewModel.CreateDispatchOrder` và chưa được biên dịch WPF trong môi trường AI, nên cần build trên Windows.

**Khi merge vào `main`:** thêm vào `DESIGN_CHANGES.md` hai dòng DC-45 (kiểm tra hợp lệ của lệnh điều phối, DSP-01..06) và DC-46 (hằng số điều phối trong `ServiceConstants`, `CancelOrder`, snapshot). Nhóm cần xác nhận các ngưỡng 0,85 / 0,35 / 0,65 và tốc độ 25 km/h.
