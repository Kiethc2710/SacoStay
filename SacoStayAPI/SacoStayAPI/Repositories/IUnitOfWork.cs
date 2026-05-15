namespace SacoStayAPI.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<T> Repository<T>() where T : class;
        Task<int> CompleteAsync();
        ILifestyleRepository LifestyleRepository { get; }

    }
}
