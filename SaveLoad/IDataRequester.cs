using Cysharp.Threading.Tasks;

namespace FakeMG.Framework.SaveLoad
{
    /// <summary>
    /// Interface for systems that need to request and apply save data.
    /// Systems implement this to register with DataApplicationManager and 
    /// receive data when available.
    /// </summary>
    public interface IDataRequester
    {
        /// <summary>
        /// The scene this system belongs to (used for per-scene data application)
        /// </summary>
        string SceneName { get; }

        UniTask ApplyDataAsync();
    }
}