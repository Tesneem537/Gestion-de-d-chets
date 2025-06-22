namespace WasteManagement3.Services
{
    public interface ILocationService
    {
        Task<string> GetCurrentLocationAsync();
    }
}