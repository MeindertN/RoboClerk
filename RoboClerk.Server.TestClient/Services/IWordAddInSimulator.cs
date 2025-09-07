namespace RoboClerk.Server.TestClient.Services
{
    public interface IWordAddInSimulator
    {
        Task<bool> SimulateWordAddInWorkflowAsync(string serverUrl, string sharePointProjectUrl, string documentName);
    }
}