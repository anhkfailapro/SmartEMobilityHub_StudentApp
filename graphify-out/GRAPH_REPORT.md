# Graph Report - SmartEMobilityHub_StudentApp  (2026-11-27)

## Corpus Check
- 234 files · ~91,332 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 4 file(s) not represented in the graph (top: (none) 2, .bat 1, .cmd 1)

## Summary
- 3033 nodes · 6690 edges · 137 communities (110 shown, 27 thin omitted)
- Extraction: 83% EXTRACTED · 17% INFERRED · 0% AMBIGUOUS · INFERRED: 1157 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `b954ecd8`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- Window
- system_windows
- MockDataStore.GetHub
- SmartEMobilityHub.Core.Domain
- BookingService
- Theme
- RegressionTests.Register
- HubRow
- IncidentsPageViewModel
- Vehicle
- StudentUiTests.Register
- Screen
- DateTime
- Alert
- PrivateEVChargingSession
- UIElement
- SmartChargingScheduler
- ChargingPoint
- MainWindow
- Screen
- UserAccount
- MockDataStore
- ShotKit
- DashboardContext
- Trip
- Incident
- MobilityHub
- DemoPageViewModel
- DispatchOrderRow
- DateTime
- DispatchOrder
- IncidentManager
- AlertType
- VehicleReservation
- MaintenanceTask
- MockDataStore.GetHub
- OperatorDemo.OperatorDemo
- SmartEMobilityHub.Core.Clock
- Window
- ChargingRequest
- StudentAppGateway
- DemoApp
- WhatIfViewModel
- ReservationScreen
- List
- DemoApp.Start
- ParkingReservation
- NetworkSession
- DeviceStatus
- UiKit.Button
- Operator
- VehicleRedistributor
- Exception
- WhatIfWindow
- system_windows_controls
- IncidentStatus
- NetworkSnapshot
- LoginResult
- DemoConfig.Time
- Vehicle
- ResourceDictionary
- SimulationResult
- Window
- ButtonVariant
- TripBillingService
- AppNotification
- StudentCapture.Script
- Technician
- SmartEMobilityHub.slnx
- ObservableObject.SetProperty
- TextKind
- Domain/Enums.cs
- DemoToggles
- NetworkSession
- QueueRow
- AssetsPageViewModel
- SmartEMobilityHub.Models
- MockDataStore.GetHub
- PowerCommandResult
- AlertRow
- WhatIfController
- SmartEMobilityHub.Tests.Support
- IncidentRow
- MainViewModel
- Application
- JourneyPlanner
- ChargingPageViewModel
- OperatorTheme
- ChargingRequest
- SmartEMobilityHub.Operator.ViewModels
- VehicleStatus
- VehicleRow.VehicleRow
- IncidentRow.IncidentRow
- SeedSmokeTests.Register
- ReportProblemScreen
- Application
- NetworkSnapshot
- WhatIfEngineService.ApplyScenarioStep
- StudentHarness
- StudentUiTests.cs
- Dark.xaml
- NavTab
- ChargingRequestScreen
- ChargingPoint
- Light.xaml
- InAppNotificationService
- FakeDialogs
- JourneyResult
- TripScreen
- MainWindow
- DemoApp.ResetDemo
- GeoPoint
- List
- IncidentLogRow
- Bd
- DispatchPageViewModel
- PortTile
- ParkingService
- NavTab
- IllegalStateException
- MaintenanceStatus
- DemoApp.cs
- NetworkWatcher
- Window
- FakeClock.AdvanceMinutes
- App
- AppNotification
- PrivateVehicle
- TimeOptions.cs
- SmartEMobilityHub.Core.Services
- Icons.cs

## God Nodes (most connected - your core abstractions)
1. `UserControl` - 66 edges
2. `DemoApp` - 59 edges
3. `WhatIfViewModel` - 59 edges
4. `MockDataStore` - 58 edges
5. `IncidentsPageViewModel` - 56 edges
6. `NetworkSession` - 56 edges
7. `SmartEMobilityHub.Ui` - 53 edges
8. `Screen` - 52 edges
9. `NetworkStore` - 51 edges
10. `SmartEMobilityHub.Core.Domain` - 48 edges

## Surprising Connections (you probably didn't know these)
- `Visual Studio` --references--> `Operator`  [INFERRED]
  README.md → src/SmartEMobilityHub.Core/Domain/Operator.cs
- `Linked mode with the student app` --references--> `NetworkSession`  [INFERRED]
  README.md → src/SmartEMobilityHub.Core/NetworkSession.cs
- `Architecture notes` --references--> `IDialogService`  [INFERRED]
  README.md → src/SmartEMobilityHub.Operator/Infrastructure/IDialogService.cs
- `5. Core services` --references--> `NetworkStore`  [INFERRED]
  README.md → src/SmartEMobilityHub.Core/Store/NetworkStore.cs
- `5. Core services` --references--> `SystemClock`  [INFERRED]
  README.md → src/SmartEMobilityHub.Core/Clock/Clock.cs

## Import Cycles
- None detected.

## Communities (137 total, 27 thin omitted)

### Community 0 - "Window"
Cohesion: 0.03
Nodes (65): Actor, AssignCommand, AvailableBrush, AvailableText, CanAssign, CanClose, CanCreateTask, CanLock (+57 more)

### Community 1 - "system_windows"
Cohesion: 0.21
Nodes (4): SmartEMobilityHub.Screens, SmartEMobilityHub.Ui, InvoiceView, ReservationDialogs

### Community 2 - "MockDataStore.GetHub"
Cohesion: 0.09
Nodes (13): DemoConfig, Tone, Charging, Danger, Info, Neutral, Success, Warning (+5 more)

### Community 3 - "SmartEMobilityHub.Core.Domain"
Cohesion: 0.14
Nodes (5): SmartEMobilityHub.Core.Domain, SmartEMobilityHub.Core.Ports, SmartEMobilityHub.Core.Store, SmartEMobilityHub.Core.Seed, ServiceConstants

### Community 4 - "BookingService"
Cohesion: 0.04
Nodes (44): AccentBrush, AlertCountText, AtRiskEmptyVisibility, AtRiskHint, AtRiskHubs, ChargingBrush, ChargingPercent, ChargingText (+36 more)

### Community 5 - "Theme"
Cohesion: 0.13
Nodes (27): Theme, Border, BorderStrong, CanvasBg, Charging, ChargingSubtle, Danger, DangerSubtle (+19 more)

### Community 6 - "RegressionTests.Register"
Cohesion: 0.05
Nodes (27): SmartEMobilityHub.Tests, Assert, AssertionException, ReportWriter, SeedSmokeTests, StudentAppTests, StudentWorld, App (+19 more)

### Community 7 - "HubRow"
Cohesion: 0.07
Nodes (27): HubRow, AccentBrush, AvailableVehiclesText, ChargingBrush, ChargingPercent, ChargingText, FreeChargersText, FreeSlotsText (+19 more)

### Community 8 - "IncidentsPageViewModel"
Cohesion: 0.06
Nodes (32): IncidentsPageViewModel, AssignCommand, CanAssign, CanClose, CanCreateTask, CanLock, CanResolve, CanResume (+24 more)

### Community 9 - "Vehicle"
Cohesion: 0.10
Nodes (16): AssignedPoint, Vehicle, Battery, Code, Core, HubId, IsAvailable, LastUpdate (+8 more)

### Community 10 - "StudentUiTests.Register"
Cohesion: 0.05
Nodes (36): AccountScreen, ShowBack, Tab, Title, ActivityScreen, ShowBack, Tab, Title (+28 more)

### Community 11 - "Screen"
Cohesion: 0.12
Nodes (9): Screen, App, Shell, ShowBack, Tab, Title, WantsTick, AppShell (+1 more)

### Community 12 - "DateTime"
Cohesion: 0.13
Nodes (14): NetworkSeed, ReservationSeed, VehicleSeed, NetworkStore, Alerts, ChargingRequests, DispatchOrders, Hubs (+6 more)

### Community 13 - "Alert"
Cohesion: 0.05
Nodes (24): Alert, AlertId, CreatedAt, Delivered, HubId, Message, RetryCount, Type (+16 more)

### Community 14 - "PrivateEVChargingSession"
Cohesion: 0.10
Nodes (20): SessionState, Cancelled, Charging, Completed, Failed, Pending, PrivateEVChargingSession, CancellationFee (+12 more)

### Community 16 - "SmartChargingScheduler"
Cohesion: 0.13
Nodes (5): IllegalArgumentException, SmartChargingScheduler, WeightBattery, WeightNeedTime, WeightPeak

### Community 17 - "ChargingPoint"
Cohesion: 0.06
Nodes (31): AssetHubFilter, AssetHubOptions, AssetSearchText, AssetSummaryText, Battery, BatteryBrush, ConnectionBrush, ConnectionGlyph (+23 more)

### Community 18 - "MainWindow"
Cohesion: 0.11
Nodes (9): App, Application, App, Application, App, Auth, LoginWindow, MainWindow (+1 more)

### Community 19 - "Screen"
Cohesion: 0.10
Nodes (13): ChargingAlternativesScreen, Title, ChargingBookingDetailScreen, Title, ParkingDetailScreen, Title, ParkingFullScreen, Title (+5 more)

### Community 20 - "UserAccount"
Cohesion: 0.07
Nodes (29): CanCompleteDispatch, CanExecuteDispatch, CompleteDispatchCommand, CreateDispatchCommand, DispatchOrders, DispatchStaffId, DispatchVehicleCountText, ExecuteDispatchCommand (+21 more)

### Community 21 - "MockDataStore"
Cohesion: 0.10
Nodes (16): MockDataStore, ActiveTrip, ChargingRequests, ChargingSessions, Hubs, Incidents, Network, Notifications (+8 more)

### Community 22 - "ShotKit"
Cohesion: 0.16
Nodes (6): OperatorCapture, ShotKit, Count, Dir, Section, ShotInfo

### Community 23 - "DashboardContext"
Cohesion: 0.09
Nodes (14): DashboardContext, AutoScan, Demo, Dialogs, IsLinked, LastActionText, ResetRequested, Session (+6 more)

### Community 24 - "Trip"
Cohesion: 0.06
Nodes (30): HistoryEntry, Date, Duration, Fee, HubName, Status, Title, Trip (+22 more)

### Community 25 - "Incident"
Cohesion: 0.10
Nodes (14): Incident, Code, Core, CreatedAt, Description, HubId, ObjectCode, ObjectKind (+6 more)

### Community 26 - "MobilityHub"
Cohesion: 0.08
Nodes (22): HubStatus, Offline, Online, Suspended, MobilityHub, ChargingPoints, ChargingUtilization, Connection (+14 more)

### Community 27 - "DemoPageViewModel"
Cohesion: 0.06
Nodes (16): OperatorDemo, Logger, Session, DemoPageViewModel, AdvanceCommand, Alerts, BreakChannelCommand, BreakPortCommand (+8 more)

### Community 28 - "DispatchOrderRow"
Cohesion: 0.20
Nodes (8): DispatchOrderRow, MetaText, OrderId, RouteText, StatusBrush, StatusRaw, StatusText, VehiclesText

### Community 29 - "DateTime"
Cohesion: 0.12
Nodes (14): InMemoryReservationAdapter, Reservation, CancelReason, ClosedAt, CreatedAt, ExtensionCount, HoldUntil, HubId (+6 more)

### Community 30 - "DispatchOrder"
Cohesion: 0.09
Nodes (19): DispatchOrder, CompletedAt, CreatedAt, EstimatedDistanceKm, EstimatedDurationMinutes, IsActive, OrderId, Reason (+11 more)

### Community 31 - "IncidentManager"
Cohesion: 0.10
Nodes (8): 5. Core services, Architecture notes, AutoCancelService, PickupExtensionsExhaustedReason, IncidentManager, FilterOption, Label, Statuses

### Community 32 - "AlertType"
Cohesion: 0.08
Nodes (25): AddChargingRequestsCommand, ApplyWeightsCommand, CanMoveQueueDown, CanMoveQueueUp, ChargingHubId, ChargingHubPorts.Ports, ChargingHubPorts.SummaryText, ChargingQueue (+17 more)

### Community 33 - "VehicleReservation"
Cohesion: 0.11
Nodes (15): VehicleReservation, CancelReason, ClosedAt, Core, CreatedAt, HoldUntil, HubId, Id (+7 more)

### Community 34 - "MaintenanceTask"
Cohesion: 0.12
Nodes (15): MaintenanceStatus, Assigned, Cancelled, Completed, InProgress, MaintenanceTask, AssignedTech, CancelReason (+7 more)

### Community 35 - "MockDataStore.GetHub"
Cohesion: 0.12
Nodes (7): BookingService, ActiveSession, Now, Store, ParkingService, ActiveParking, Store

### Community 36 - "OperatorDemo.OperatorDemo"
Cohesion: 0.19
Nodes (4): ManualScheduler, IsScheduled, Subscription, IScheduler

### Community 37 - "SmartEMobilityHub.Core.Clock"
Cohesion: 0.25
Nodes (3): SmartEMobilityHub.Core, SmartEMobilityHub.Core.Adapters, SmartEMobilityHub.Core.Clock

### Community 38 - "Window"
Cohesion: 0.05
Nodes (41): AffectedHubCount, At, CanRun, ChargingUtilization, CriticalHubCount, DataContext.ForecastScale, DemandSurgeText, DemandSurgeValue (+33 more)

### Community 39 - "ChargingRequest"
Cohesion: 0.12
Nodes (15): ChargingPlan, HubId, OrderedRequests, TotalPowerKw, ChargingRequest, AssignedPortId, BatteryLevel, HubId (+7 more)

### Community 40 - "StudentAppGateway"
Cohesion: 0.09
Nodes (4): CampusFleetSeed, StudentAppGateway, Charging, Store

### Community 41 - "DemoApp"
Cohesion: 0.06
Nodes (19): DemoApp, Auth, Billing, Booking, Charging, Clock, Context, History (+11 more)

### Community 42 - "WhatIfViewModel"
Cohesion: 0.05
Nodes (36): HubChoice, WhatIfViewModel, AffectedHubCount, CanRun, CriticalHubCount, CurrentSessionId, DemandSurgeValue, DisabledChargerText (+28 more)

### Community 43 - "ReservationScreen"
Cohesion: 0.11
Nodes (14): ConfirmReservationScreen, Title, ReservationScreen, Title, WantsTick, UnlockFailedScreen, Title, UnlockingScreen (+6 more)

### Community 44 - "List"
Cohesion: 0.08
Nodes (23): ChargingPoint, Core, Id, Number, Status, PointStatus, Charging, Free (+15 more)

### Community 46 - "ParkingReservation"
Cohesion: 0.15
Nodes (12): ParkingReservation, CancellationFee, ClosedAt, DurationMinutes, End, EntryCode, HubId, Id (+4 more)

### Community 47 - "NetworkSession"
Cohesion: 0.18
Nodes (8): IOperatorConsole, IsOpen, OperatorConsole, IsOpen, FakeOperatorConsole, IsOpen, Opened, Sessions

### Community 48 - "DeviceStatus"
Cohesion: 0.07
Nodes (28): DeviceStatus, Available, Charging, Faulty, InUse, Locked, NeedsCharge, Offline (+20 more)

### Community 50 - "Operator"
Cohesion: 0.08
Nodes (24): 1. Requirements, 2. Build and run, 3. Student app, 4. Operator dashboard, 6. Tests, 7. Repository structure, 8. Team, Business rules used (+16 more)

### Community 51 - "VehicleRedistributor"
Cohesion: 0.23
Nodes (5): VehicleRedistributor, HighLoadThreshold, LowLoadThreshold, SuggestedOrders, TargetLoadRatio

### Community 52 - "Exception"
Cohesion: 0.12
Nodes (11): FaultKind, Fault, Timeout, Unreachable, MockIoTSensorGateway, SimulateBatteryDrift, FileTooLargeException, GatewayTimeoutException (+3 more)

### Community 55 - "IncidentStatus"
Cohesion: 0.09
Nodes (17): IncidentLogEntry, Actor, At, Message, IncidentRecord, CreatedAt, Description, HubId (+9 more)

### Community 56 - "NetworkSnapshot"
Cohesion: 0.09
Nodes (17): NetworkSnapshot, Alerts, ChargingRequests, DispatchOrders, Hubs, Incidents, MaintenanceTasks, Operators (+9 more)

### Community 57 - "LoginResult"
Cohesion: 0.07
Nodes (29): AccountRole, Operator, Student, UserAccount, DisplayName, FailedAttempts, LinkedId, LockedUntil (+21 more)

### Community 58 - "DemoConfig.Time"
Cohesion: 0.15
Nodes (17): IncidentObjectType, ChargingPoint, Hub, Vehicle, IncidentStatus, Closed, InRepair, Locked (+9 more)

### Community 59 - "Vehicle"
Cohesion: 0.12
Nodes (14): VehicleType, EBike, EMotorbike, Vehicle, AssignedPortId, BatteryLevel, Code, Connection (+6 more)

### Community 60 - "ResourceDictionary"
Cohesion: 0.11
Nodes (21): ActualWidth, Background, BorderBrush, BorderThickness, DetailText, IsDropDownOpen, NumberText, ToolTipText (+13 more)

### Community 61 - "SimulationResult"
Cohesion: 0.05
Nodes (46): DispatchRecommendation, ForecastPoint, HubImpact, ChargingPortsComparison, ChargingWaitComparison, OverallUtilizationAfter, OverallUtilizationBefore, ImpactReport (+38 more)

### Community 62 - "Window"
Cohesion: 0.13
Nodes (14): DemoHint, ErrorPanel, ErrorText, LoginButton, PART_ContentHost, PasswordBox, UsernameBox, Window (+6 more)

### Community 63 - "ButtonVariant"
Cohesion: 0.17
Nodes (9): ButtonVariant, Charging, Danger, DangerOutline, DangerSubtle, Outline, Primary, Secondary (+1 more)

### Community 64 - "TripBillingService"
Cohesion: 0.42
Nodes (3): TripBillingService, ActiveTrip, Store

### Community 65 - "AppNotification"
Cohesion: 0.15
Nodes (12): BookingItem, CanChangeTime, HubName, Id, IsActive, Kind, Rows, Start (+4 more)

### Community 67 - "Technician"
Cohesion: 0.11
Nodes (10): GeoPoint, Lat, Lng, Technician, Available, Location, Name, Team (+2 more)

### Community 68 - "SmartEMobilityHub.slnx"
Cohesion: 0.12
Nodes (12): net10.0, net10.0-windows, Microsoft.NET.Sdk, net10.0, Microsoft.NET.Sdk, net10.0-windows, Microsoft.NET.Sdk, net10.0-windows (+4 more)

### Community 69 - "ObservableObject.SetProperty"
Cohesion: 0.10
Nodes (27): ObservableObject, SelectedQueueRow, SelectedDispatchOrder, PageIndex, OverviewPageViewModel, AlertCountText, Alerts, AtRiskEmptyVisibility (+19 more)

### Community 70 - "TextKind"
Cohesion: 0.20
Nodes (9): TextKind, Big, Body, Button, Caption, Heading, Huge, Subtitle (+1 more)

### Community 71 - "Domain/Enums.cs"
Cohesion: 0.14
Nodes (13): ConnectionStatus, Offline, Online, IncidentSource, IotSensor, StudentReport, UnlockTimeout, ReservationStatus (+5 more)

### Community 72 - "DemoToggles"
Cohesion: 0.15
Nodes (12): DemoToggles, AllChargingPointsBusy, AnotherStudentReservesFirst, ChangeTimeUnavailable, HistoryEmpty, MapServiceTimeout, NoChargingPointInWindow, NotAtHub (+4 more)

### Community 73 - "NetworkSession"
Cohesion: 0.04
Nodes (37): NetworkSession, AutoCancel, Charging, Clock, Gateway, Incidents, Log, Notification (+29 more)

### Community 74 - "QueueRow"
Cohesion: 0.20
Nodes (8): QueueRow, BatteryText, IsWaiting, PositionText, RequestId, ScoreText, StatusText, Vehicle

### Community 75 - "AssetsPageViewModel"
Cohesion: 0.07
Nodes (30): DeviceFilterOption, Label, Statuses, VehicleRow, Battery, BatteryBrush, BatteryText, Code (+22 more)

### Community 76 - "SmartEMobilityHub.Models"
Cohesion: 0.21
Nodes (3): SmartEMobilityHub.Services, SmartEMobilityHub.Data, SmartEMobilityHub.Models

### Community 77 - "MockDataStore.GetHub"
Cohesion: 0.13
Nodes (9): BookingKind, ChargingRequest, ChargingSession, Parking, Vehicle, ReservationService, ActiveVehicleReservation, Now (+1 more)

### Community 78 - "PowerCommandResult"
Cohesion: 0.23
Nodes (7): PowerCommandOutcome, Ack, Fault, Timeout, PowerCommandResult, Message, Outcome

### Community 79 - "AlertRow"
Cohesion: 0.29
Nodes (6): AlertRow, DeliveryBrush, DeliveryText, Message, TimeText, Title

### Community 80 - "WhatIfController"
Cohesion: 0.05
Nodes (13): SystemLog, Lines, SystemClock, Now, ValidationResult, WhatIfController, WhatIfEngineService, TempStateRepository (+5 more)

### Community 81 - "SmartEMobilityHub.Tests.Support"
Cohesion: 0.14
Nodes (9): SmartEMobilityHub.Tests.UseCase, SmartEMobilityHub.Tests.Unit, SmartEMobilityHub.Tests.StudentApp, SmartEMobilityHub.Tests.Support, TCOP03Tests, TCSYS01Tests, TCSYS02Tests, TCSYS03Tests (+1 more)

### Community 82 - "IncidentRow"
Cohesion: 0.12
Nodes (16): IncidentRow, Code, CreatedText, Description, HubId, Id, Logs, Meta (+8 more)

### Community 83 - "MainViewModel"
Cohesion: 0.08
Nodes (23): NavItem, Badge, BadgeVisibility, Glyph, Label, MainViewModel, Assets, AssetsVisibility (+15 more)

### Community 85 - "JourneyPlanner"
Cohesion: 0.06
Nodes (26): HistoryService, Store, JourneyPlanner, Destinations, NotificationService, UnreadCount, PickUpService, Store (+18 more)

### Community 86 - "ChargingPageViewModel"
Cohesion: 0.07
Nodes (32): HubPortGroup, HubId, Name, Ports, SummaryBrush, SummaryText, PortTile, Background (+24 more)

### Community 87 - "OperatorTheme"
Cohesion: 0.24
Nodes (5): SmartEMobilityHub.Operator.Themes, ThemeButton, OperatorTheme, IsDark, Button

### Community 88 - "ChargingRequest"
Cohesion: 0.08
Nodes (22): ChargingEstimate, AllBusy, FreePoints, Hub, Priority, QueuePosition, Score, WaitMinutes (+14 more)

### Community 90 - "VehicleStatus"
Cohesion: 0.22
Nodes (9): VehicleStatus, Available, Charging, Faulty, InUse, NeedsCharge, Relocating, Reserved (+1 more)

### Community 93 - "SeedSmokeTests.Register"
Cohesion: 0.15
Nodes (7): FakeClock, Now, IClock, Now, ScaledClock, Now, Scale

### Community 94 - "ReportProblemScreen"
Cohesion: 0.22
Nodes (4): ReportProblemScreen, Title, VehicleListScreen, Title

### Community 96 - "NetworkSnapshot"
Cohesion: 0.13
Nodes (7): SmartEMobilityHub.Operator.Views, AssetsPage, ChargingPage, DemoLogPage, DispatchPage, IncidentsPage, OverviewPage

### Community 97 - "WhatIfEngineService.ApplyScenarioStep"
Cohesion: 0.11
Nodes (18): AdvanceCommand, BreakChannelCommand, BreakPortCommand, DisconnectHubCommand, Logs, MakeStaleCommand, ResetDemoCommand, RestoreChannelCommand (+10 more)

### Community 98 - "StudentHarness"
Cohesion: 0.17
Nodes (7): AccountSeed, StudentHarness, App, Panel, Phone, RealClock, Start

### Community 99 - "StudentUiTests.cs"
Cohesion: 0.16
Nodes (4): SmartEMobilityHub.Launcher, SmartEMobilityHub.Tests.Ui, SmartEMobilityHub.Operator, SmartEMobilityHub.Tests.Ui.Support

### Community 101 - "NavTab"
Cohesion: 0.29
Nodes (6): NavTab, Account, Booking, Home, Notify, Trip

### Community 102 - "ChargingRequestScreen"
Cohesion: 0.25
Nodes (6): ChargingQueuedScreen, Title, WantsTick, ChargingRequestScreen, ApplyBusyToggle, Title

### Community 103 - "ChargingPoint"
Cohesion: 0.14
Nodes (10): ChargingPoint, AssignedVehicleCode, HubId, Id, LastUpdatedAt, Number, PowerKw, Status (+2 more)

### Community 105 - "InAppNotificationService"
Cohesion: 0.27
Nodes (7): InAppNotificationService, Broadcasts, FailedDeliveries, NetworkDown, OperatorAlerts, UserMessages, NotificationDeliveryException

### Community 106 - "FakeDialogs"
Cohesion: 0.20
Nodes (5): FakeDialogs, ConfirmAnswer, Confirms, Informs, Warnings

### Community 107 - "JourneyResult"
Cohesion: 0.20
Nodes (9): JourneyResult, Destination, EndHub, EndToDestinationKm, HubsByStraightLine, RideKm, RouteAvailable, StartHub (+1 more)

### Community 108 - "TripScreen"
Cohesion: 0.25
Nodes (5): TripScreen, ShowBack, Tab, Title, WantsTick

### Community 110 - "MainWindow"
Cohesion: 0.28
Nodes (6): DemoPanelHost, PhoneHost, Window, MainWindow, App, Border

### Community 115 - "IncidentLogRow"
Cohesion: 0.50
Nodes (4): IncidentLogRow, Actor, Message, TimeText

### Community 116 - "Bd"
Cohesion: 0.50
Nodes (3): Bd, ResourceDictionary, Border

### Community 121 - "DispatchPageViewModel"
Cohesion: 0.09
Nodes (17): UiLogger, Lines, Source, DispatchPageViewModel, CanCompleteDispatch, CanExecuteDispatch, CompleteDispatchCommand, CreateDispatchCommand (+9 more)

### Community 124 - "ParkingService"
Cohesion: 0.33
Nodes (6): TechnicianRow, AvailableBrush, AvailableText, DistanceText, Name, Team

### Community 125 - "NavTab"
Cohesion: 0.25
Nodes (5): OpResult, Error, Message, Ok, Value

### Community 126 - "IllegalStateException"
Cohesion: 0.13
Nodes (3): IllegalStateException, IncidentNotFoundException, IReservationPort

### Community 127 - "MaintenanceStatus"
Cohesion: 0.29
Nodes (6): MaintenanceRow, SlaBrush, SlaText, StatusText, TaskId, TechText

### Community 128 - "DemoApp.cs"
Cohesion: 0.11
Nodes (3): SmartEMobilityHub.Operator.Infrastructure, SmartEMobilityHub, RelayCommand

### Community 129 - "NetworkWatcher"
Cohesion: 0.10
Nodes (18): ChargingRequestState, Assigned, Cancelled, InQueue, ReportObjectKind, ChargingPoint, Vehicle, ReservationState (+10 more)

### Community 130 - "Window"
Cohesion: 0.06
Nodes (31): Assets, Badge, BadgeVisibility, Charging, ClockText, DataContext.AssetsVisibility, DataContext.ChargingVisibility, DataContext.DemoVisibility (+23 more)

### Community 131 - "FakeClock.AdvanceMinutes"
Cohesion: 0.25
Nodes (3): SimulationClock, Now, Offset

### Community 137 - "AppNotification"
Cohesion: 0.29
Nodes (6): AppNotification, Body, Read, Time, Title, Tone

### Community 143 - "PrivateVehicle"
Cohesion: 0.40
Nodes (4): PrivateVehicle, Id, Model, Plate

## Knowledge Gaps
- **1226 isolated node(s):** `App`, `Shell`, `Context`, `OperatorConsole`, `Auth` (+1221 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 1405 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **27 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `PageViewModel` connect `DashboardContext` to `SmartEMobilityHub.Operator.ViewModels`, `ObservableObject.SetProperty`, `IncidentsPageViewModel`, `StudentAppGateway`, `AssetsPageViewModel`, `NetworkSession`, `MainViewModel`, `ChargingPageViewModel`, `DispatchPageViewModel`, `DemoConfig.Time`, `DemoPageViewModel`?**
  _High betweenness centrality (0.063) - this node is a cross-community bridge._
- **Are the 3 inferred relationships involving `DemoApp` (e.g. with `MainWindow.MainWindow()` and `Architecture notes`) actually correct?**
  _`DemoApp` has 3 INFERRED edges - model-reasoned connections that need verification._
- **What connects `App`, `Shell`, `Context` to the rest of the system?**
  _1226 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Window` be split into smaller, more focused modules?**
  _Cohesion score 0.030303030303030304 - nodes in this community are weakly interconnected._
- **Why does `MainViewModel` connect `MainViewModel` to `DispatchPageViewModel`, `ObservableObject.SetProperty`, `IncidentsPageViewModel`, `StudentUiTests.Register`, `AssetsPageViewModel`, `MainWindow`, `MainWindow`, `ChargingPageViewModel`, `DashboardContext`, `SmartEMobilityHub.Operator.ViewModels`, `DemoPageViewModel`?**
  _High betweenness centrality (0.056) - this node is a cross-community bridge._
- **Should `MockDataStore.GetHub` be split into smaller, more focused modules?**
  _Cohesion score 0.08817204301075268 - nodes in this community are weakly interconnected._
- **Why does `IncidentsPage` connect `NetworkSnapshot` to `Window`, `RegressionTests.Register`?**
  _High betweenness centrality (0.050) - this node is a cross-community bridge._