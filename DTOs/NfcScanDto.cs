namespace WasteManagement3.DTOs
{
    public class NfcScanDto
    {
        public string TagId { get; set; }
        public DateTime? ScanTime { get; set; }
        public string Location { get; set; }
    }
}