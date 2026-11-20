namespace SmartEMobilityHub.Data
{
    public class DemoToggles
    {
        public bool MapServiceTimeout { get; set; }
        public bool AnotherStudentReservesFirst { get; set; }
        public bool NotAtHub { get; set; }
        public bool UnlockTimeout { get; set; }
        public bool VehicleOutsideHub { get; set; }
        public bool ReturnHubFull { get; set; }
        public bool ParkingHubFull { get; set; }
        public bool ChangeTimeUnavailable { get; set; }
        public bool AllChargingPointsBusy { get; set; }
        public bool NoChargingPointInWindow { get; set; }
        public bool HistoryEmpty { get; set; }

        public void Reset()
        {
            MapServiceTimeout = AnotherStudentReservesFirst = NotAtHub = UnlockTimeout = false;
            VehicleOutsideHub = ReturnHubFull = ParkingHubFull = ChangeTimeUnavailable = false;
            AllChargingPointsBusy = NoChargingPointInWindow = HistoryEmpty = false;
        }
    }
}
