using System.Threading;
using System.Threading.Tasks;

namespace SFA_PWA.Services;

public interface IStaticJsonAssetLoader
{
    Task<T?> LoadAsync<T>(string relativePath, CancellationToken cancellationToken = default);
}
