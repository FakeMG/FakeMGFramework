namespace FakeMG.Framework.Database
{
    /// <summary>
    /// Interface for objects that can be stored in a database with a unique ID
    /// </summary>
    public interface IIdentifiable
    {
        string ID { get; }
    }
}
