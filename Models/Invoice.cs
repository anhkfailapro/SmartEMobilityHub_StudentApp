namespace SmartEMobilityHub.Models
{
    public class Invoice
    {
        public string Number { get; init; } = "";
        public int Minutes { get; init; }
        public int FreeExtraMinutes { get; init; }
        public int RentalFee { get; init; }
        public int PlanDeduction { get; init; }
        public int Total { get; init; }
        public bool PaidByPlanOnly { get; init; }
        public bool Paid { get; set; } = true;
        public string PaymentMethod { get; init; } = "";
    }
}
