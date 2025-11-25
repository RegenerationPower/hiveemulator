namespace DevOpsProject.CommunicationControl.Logic.Services.Interfaces
{
    public interface IRedisKeyValueService
    {
        Task<T> GetAsync<T>(string key);
        Task<List<T>> GetAllAsync<T>(string keyPattern);
        Task<bool> SetAsync<T>(string key, T value);
        Task<bool> UpdateAsync<T>(string key, Action<T> updateAction);
        Task<bool> CheckIfKeyExists(string key);
        Task<bool> DeleteAsync(string key);
    }
}
