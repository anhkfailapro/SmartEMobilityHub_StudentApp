using System.Windows;
using System.Windows.Controls;
using SmartEMobilityHub.Core.Domain;
using SmartEMobilityHub.Operator;
using SmartEMobilityHub.Operator.ViewModels;
using SmartEMobilityHub.Operator.Views;
using SmartEMobilityHub.Tests.Ui.Support;

namespace SmartEMobilityHub.Tests.Ui;

/// <summary>
/// Trung tâm điều hành: mỗi trang có ViewModel riêng, liên lạc qua DashboardContext; MainViewModel chỉ điều hướng.
/// UI-OP-01..09 chạy trên ViewModel (không mở cửa sổ), UI-OP-10 mở cửa sổ thật và bắt lỗi binding.
/// </summary>
public static class OperatorUiTests
{
    private static (MainViewModel Vm, FakeDialogs Dialogs) Create()
    {
        var dialogs = new FakeDialogs();
        return (new MainViewModel(new OperatorDemo(), dialogs), dialogs);
    }

    public static void Register(TestRunner runner)
    {
        runner.Add("UI-OP-01", "Khung điều hướng: 6 trang, mỗi lúc chỉ một trang hiển thị, ListBox đặt -1 không đổi trang", () =>
        {
            var (vm, _) = Create();
            var visibilities = new Func<Visibility>[]
            {
                () => vm.OverviewVisibility, () => vm.IncidentsVisibility, () => vm.AssetsVisibility,
                () => vm.ChargingVisibility, () => vm.DispatchVisibility, () => vm.DemoVisibility
            };
            Assert.Equal(6, vm.NavItems.Count, "6 mục thanh bên");

            for (var page = 0; page < visibilities.Length; page++)
            {
                vm.PageIndex = page;
                Assert.Equal(1, visibilities.Count(v => v() == Visibility.Visible), $"Trang {page}: đúng một trang hiển thị");
                Assert.Equal(Visibility.Visible, visibilities[page](), $"Trang {page} là trang hiển thị");
            }

            vm.PageIndex = -1;
            Assert.Equal(MainViewModel.PageDemo, vm.PageIndex, "Giữ trang đang xem khi ListBox đặt -1");
        });

        runner.Add("UI-OP-02", "Bấm thẻ Hub ở Tổng quan: mở trang Xe & cổng sạc, chỉ xe và cổng của Hub đó", () =>
        {
            var (vm, _) = Create();
            var hubId = vm.Overview.Hubs[0].HubId;

            vm.Overview.OpenHubAssetsCommand.Execute(hubId);

            Assert.Equal(MainViewModel.PageAssets, vm.PageIndex, "Chuyển sang trang Xe & cổng sạc");
            Assert.Equal(hubId, vm.Assets.AssetHubFilter, "Lọc theo Hub vừa bấm");
            var store = vm.Shell.Session.Store;
            Assert.Equal(store.Vehicles.Count(v => v.HubId == hubId), vm.Assets.VehicleRows.Count, "Đủ xe của Hub");
            Assert.True(vm.Assets.VehicleRows.All(r => r.HubText.StartsWith(hubId)), "Không lẫn xe Hub khác");
            Assert.True(vm.Assets.PortGroups.All(g => g.HubId == hubId), "Chỉ lưới cổng sạc của Hub đó");
        });

        runner.Add("UI-OP-03", "Vòng đời sự cố trên trang Sự cố: khoá → phiếu → gán KTV → hoàn tất → đóng; KPI Tổng quan cập nhật", () =>
        {
            var (vm, dialogs) = Create();
            var page = vm.Incidents;
            var openBefore = vm.Overview.OpenIncidentCount;
            page.SelectedIncident = page.Incidents.First(r => r.StatusRaw == IncidentStatus.New);
            var id = page.SelectedIncident!.Id;

            Assert.True(page.CanLock, "Sự cố mới khoá được");
            Assert.Equal<string?>(null, page.LockSelected(), "Khoá thiết bị");
            Assert.Equal<string?>(null, page.CreateTaskSelected(), "Tạo phiếu bảo trì");
            Assert.Equal<string?>(null, page.AssignSelected(), "Gán kỹ thuật viên");
            Assert.Equal(id, page.SelectedIncident?.Id, "Vẫn chọn đúng sự cố sau mỗi lần làm mới");
            Assert.True(page.SelectedTasks.Count >= 1, "Phiếu bảo trì hiện ở chi tiết");
            Assert.True(page.CanResolve, "Đang sửa thì bật nút Hoàn tất");
            Assert.Equal<string?>(null, page.ResolveSelected(), "Hoàn tất xử lý");
            Assert.True(page.CanClose, "Đã xử lý thì bật nút Đóng");
            Assert.Equal<string?>(null, page.CloseSelected(), "Đóng sự cố");

            var record = vm.Shell.Session.Incidents.GetIncidentQueue().Single(i => i.IncidentId == id);
            Assert.Equal(IncidentStatus.Closed, record.Status, "Core ghi nhận đã đóng");
            Assert.Equal(openBefore - 1, vm.Overview.OpenIncidentCount, "Trang Tổng quan được làm mới theo");
            Assert.Equal((openBefore - 1).ToString(), vm.NavItems[MainViewModel.PageIncidents].Badge, "Huy hiệu thanh bên cập nhật");
            Assert.Equal(0, dialogs.Warnings.Count, "Không có hộp thoại lỗi");
            Assert.True(vm.Shell.LastActionText.Contains(id), "Dòng trạng thái báo thao tác cuối");
        });

        runner.Add("UI-OP-04", "Lọc và tìm sự cố: lọc Mới chỉ còn sự cố mới; tìm theo mã ra đúng một dòng", () =>
        {
            var (vm, _) = Create();
            var page = vm.Incidents;
            page.SelectedFilter = page.Filters.First(f => f.Label == "Mới");
            Assert.True(page.Incidents.Count > 0 && page.Incidents.All(r => r.StatusRaw == IncidentStatus.New), "Chỉ sự cố Mới");
            Assert.Equal($"{page.Incidents.Count} sự cố", page.VisibleCountText, "Đếm số dòng đang hiện");

            page.SelectedFilter = page.Filters[0];
            var id = page.Incidents[0].Id;
            page.SearchText = id;
            Assert.Equal(1, page.Incidents.Count(r => r.Id == id), "Tìm thấy sự cố theo mã");
            Assert.True(page.Incidents.All(r => r.Id.Contains(id, StringComparison.OrdinalIgnoreCase)
                                              || r.Code.Contains(id, StringComparison.OrdinalIgnoreCase)
                                              || r.HubId.Contains(id, StringComparison.OrdinalIgnoreCase)
                                              || r.Title.Contains(id, StringComparison.OrdinalIgnoreCase)), "Mọi dòng khớp từ khoá");
        });

        runner.Add("UI-OP-05", "Nút lỗi hiện hộp thoại: trọng số không phải số bị từ chối, trọng số đang dùng không đổi", () =>
        {
            var (vm, dialogs) = Create();
            var charging = vm.Shell.Session.Charging;
            var before = charging.WeightBattery;

            vm.Charging.WeightBatteryText = "abc";
            vm.Charging.ApplyWeightsCommand.Execute(null);

            Assert.Equal(1, dialogs.Warnings.Count, "Một hộp thoại cảnh báo");
            Assert.True(dialogs.Warnings[0].Message.Contains("Trọng số"), "Nội dung nói về trọng số");
            Assert.Equal(before, charging.WeightBattery, "Trọng số Core giữ nguyên");
        });

        runner.Add("UI-OP-06", "Lịch sạc: kịch bản 3 yêu cầu sạc mở Hub demo, có hàng chờ, lưới cổng và huy hiệu đúng số xe chờ", () =>
        {
            var (vm, _) = Create();
            vm.Charging.AddChargingRequests();

            Assert.Equal(OperatorDemo.ChargingDemoHubId, vm.Charging.ChargingHubId, "Mở Hub demo");
            Assert.True(vm.Charging.ChargingQueue.Count > 0, "Có hàng chờ sạc");
            Assert.True(vm.Charging.ChargingHubPorts is not null, "Có lưới cổng sạc của Hub");
            var waiting = vm.Shell.Session.Store.ChargingRequests.Count(r => r.AssignedPortId is null);
            Assert.Equal(waiting > 0 ? waiting.ToString() : "", vm.NavItems[MainViewModel.PageCharging].Badge, "Huy hiệu = số xe chờ cổng");
        });

        runner.Add("UI-OP-07", "Điều phối: số xe sai bị từ chối; tạo → xuất phát → hoàn tất lệnh, nút và huy hiệu đổi theo trạng thái", () =>
        {
            var (vm, _) = Create();
            var page = vm.Dispatch;
            page.DispatchVehicleCountText = "0";
            Assert.True(page.CreateDispatchOrder() is not null, "Số xe 0 bị từ chối");

            string? error = null;
            foreach (var hub in vm.Shell.Session.Store.Hubs.Select(h => h.Id))
            {
                page.SourceHubId = hub;
                page.TargetHubId = vm.Shell.Session.Store.Hubs.First(h => h.Id != hub).Id;
                page.DispatchVehicleCountText = "1";
                error = page.CreateDispatchOrder();
                if (error is null) break;
            }
            Assert.Equal<string?>(null, error, "Tạo được một lệnh điều phối");
            Assert.Equal("1", vm.NavItems[MainViewModel.PageDispatch].Badge, "Huy hiệu: 1 lệnh đang mở");
            Assert.True(page.CanExecuteDispatch && !page.CanCompleteDispatch, "Lệnh mới: chỉ bật Xuất phát");

            Assert.Equal<string?>(null, page.ExecuteSelectedDispatch(), "Xuất phát");
            Assert.True(!page.CanExecuteDispatch && page.CanCompleteDispatch, "Đang điều phối: chỉ bật Hoàn tất");

            Assert.Equal<string?>(null, page.CompleteSelectedDispatch(), "Hoàn tất");
            Assert.Equal("", vm.NavItems[MainViewModel.PageDispatch].Badge, "Không còn lệnh đang mở");
        });

        runner.Add("UI-OP-08", "Kịch bản demo: bơm lỗi cổng sạc làm tăng số thiết bị lỗi ở trang Xe & cổng sạc và huy hiệu", () =>
        {
            var (vm, _) = Create();
            var before = vm.Assets.FaultyCount;

            vm.Demo.BreakPortCommand.Execute(null);

            Assert.True(vm.Assets.FaultyCount > before, "Thêm thiết bị lỗi");
            Assert.Equal(vm.Assets.FaultyCount.ToString(), vm.NavItems[MainViewModel.PageAssets].Badge, "Huy hiệu = số thiết bị lỗi");
            Assert.True(vm.Shell.LastActionText.Contains(OperatorDemo.DemoBrokenPointId), "Dòng trạng thái nêu cổng bị lỗi");
            Assert.True(vm.Demo.Logs.Count > 0, "Nhật ký có dòng mới");
        });

        runner.Add("UI-OP-09", "Đặt lại demo: liên thông thì chỉ báo dùng app Sinh viên; chạy riêng thì hỏi rồi mới đặt lại", () =>
        {
            var (vm, dialogs) = Create();
            var resets = 0;
            vm.Shell.ResetRequested = () => resets++;

            vm.Shell.IsLinked = true;
            vm.Demo.ResetDemoCommand.Execute(null);
            Assert.Equal(1, dialogs.Informs.Count, "Liên thông: hiện thông báo");
            Assert.Equal(0, resets, "Liên thông: không tự đặt lại");

            vm.Shell.IsLinked = false;
            dialogs.ConfirmAnswer = false;
            vm.Demo.ResetDemoCommand.Execute(null);
            Assert.Equal(0, resets, "Không đồng ý thì không đặt lại");

            dialogs.ConfirmAnswer = true;
            vm.Demo.ResetDemoCommand.Execute(null);
            Assert.Equal(1, resets, "Đồng ý thì đặt lại");
        });

        runner.Add("UI-OP-10", "Cửa sổ Operator thật: mỗi trang nhận đúng ViewModel, mở cả 6 trang không có lỗi binding", () =>
        {
            var errors = BindingErrors.Attach();
            var window = new SmartEMobilityHub.Operator.MainWindow(new OperatorDemo());
            try
            {
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Left = -32000;
                window.Top = -32000;
                window.ShowActivated = false;
                window.ShowInTaskbar = false;
                window.Show();

                var vm = (MainViewModel)window.DataContext;
                for (var page = 0; page < vm.NavItems.Count; page++)
                {
                    vm.PageIndex = page;
                    UiHelpers.Pump();
                }

                Assert.True(ReferenceEquals(vm.Overview, UiHelpers.Find<OverviewPage>(window)?.DataContext), "Tổng quan ↔ OverviewPageViewModel");
                Assert.True(ReferenceEquals(vm.Incidents, UiHelpers.Find<IncidentsPage>(window)?.DataContext), "Sự cố ↔ IncidentsPageViewModel");
                Assert.True(ReferenceEquals(vm.Assets, UiHelpers.Find<AssetsPage>(window)?.DataContext), "Xe & cổng ↔ AssetsPageViewModel");
                Assert.True(ReferenceEquals(vm.Charging, UiHelpers.Find<ChargingPage>(window)?.DataContext), "Lịch sạc ↔ ChargingPageViewModel");
                Assert.True(ReferenceEquals(vm.Dispatch, UiHelpers.Find<DispatchPage>(window)?.DataContext), "Điều phối ↔ DispatchPageViewModel");
                Assert.True(ReferenceEquals(vm.Demo, UiHelpers.Find<DemoLogPage>(window)?.DataContext), "Kịch bản ↔ DemoPageViewModel");
                Assert.True(errors.Errors.Count == 0, "Lỗi binding: " + string.Join(" | ", errors.Errors.Take(3)));
            }
            finally
            {
                window.Close();
                errors.Detach();
            }
        });
    }
}
