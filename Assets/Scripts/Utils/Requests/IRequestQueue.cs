using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Utils.Requests
{
    public interface IRequestQueue
    {
        UniTask<T> Enqueue<T>(string owner, Func<CancellationToken, UniTask<T>> operation, CancellationToken externalToken = default);
        void CancelByOwner(string owner);
        void Clear();
    }
}
