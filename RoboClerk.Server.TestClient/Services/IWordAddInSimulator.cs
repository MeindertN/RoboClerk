namespace RoboClerk.Server.TestClient.Services
{
    public interface IWordAddInSimulator
    {
        Task<bool> SimulateWordAddInWorkflowAsync(
            string serverUrl, 
            string projectPath, 
            string spDriveId, 
            string projectRoot, 
            string spSiteUrl, 
            string documentName);
    }
}