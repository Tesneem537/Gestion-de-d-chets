namespace WasteManagement3.DTOs
{
    public class MLResult
    {
        internal string Message;

        public double PredictedNextWeekQuantity { get; set; }
        public List<HotelCluster> HotelClusters { get; set; }
    }

    public class HotelCluster
    {
        public string HotelId { get; set; }
        public double TotalQuantity { get; set; }
        public int Cluster { get; set; }
    }
}
