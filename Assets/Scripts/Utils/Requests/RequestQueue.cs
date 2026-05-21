using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Utils.Requests
{
    public sealed class RequestQueue : IRequestQueue, IDisposable
    {
        private readonly object _gate = new();
        private readonly Queue<IQueuedRequest> _pending = new();

        private bool _isProcessing;
        private IQueuedRequest _current;
        private bool _disposed;

        public UniTask<T> Enqueue<T>(string owner, Func<CancellationToken, UniTask<T>> operation, CancellationToken externalToken = default)
        {
            if (string.IsNullOrWhiteSpace(owner))
            {
                throw new ArgumentException("Queue owner must be provided", nameof(owner));
            }

            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            ThrowIfDisposed();

            var request = new QueuedRequest<T>(owner, operation, externalToken);
            var shouldStartProcessor = false;

            lock (_gate)
            {
                _pending.Enqueue(request);
                if (!_isProcessing)
                {
                    _isProcessing = true;
                    shouldStartProcessor = true;
                }
            }

            if (shouldStartProcessor)
            {
                ProcessQueueAsync().Forget();
            }

            return request.Task;
        }

        public void CancelByOwner(string owner)
        {
            if (string.IsNullOrWhiteSpace(owner))
            {
                return;
            }

            lock (_gate)
            {
                if (_current != null && _current.Owner == owner)
                {
                    _current.Cancel();
                }

                if (_pending.Count == 0)
                {
                    return;
                }

                var survivors = new Queue<IQueuedRequest>(_pending.Count);
                while (_pending.Count > 0)
                {
                    var request = _pending.Dequeue();
                    if (request.Owner == owner)
                    {
                        request.Cancel();
                        continue;
                    }

                    survivors.Enqueue(request);
                }

                while (survivors.Count > 0)
                {
                    _pending.Enqueue(survivors.Dequeue());
                }
            }
        }

        public void Clear()
        {
            lock (_gate)
            {
                _current?.Cancel();

                while (_pending.Count > 0)
                {
                    var pending = _pending.Dequeue();
                    pending.Cancel();
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            Clear();
        }

        private async UniTaskVoid ProcessQueueAsync()
        {
            while (true)
            {
                IQueuedRequest request;
                lock (_gate)
                {
                    if (_pending.Count == 0)
                    {
                        _isProcessing = false;
                        _current = null;
                        return;
                    }

                    request = _pending.Dequeue();
                    _current = request;
                }

                await request.ExecuteAsync();

                lock (_gate)
                {
                    if (ReferenceEquals(_current, request))
                    {
                        _current = null;
                    }
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(RequestQueue));
            }
        }

        private interface IQueuedRequest
        {
            string Owner { get; }
            UniTask ExecuteAsync();
            void Cancel();
        }

        private sealed class QueuedRequest<T> : IQueuedRequest
        {
            private readonly Func<CancellationToken, UniTask<T>> _operation;
            private readonly CancellationToken _externalToken;
            private readonly CancellationTokenSource _localCts = new();
            private readonly UniTaskCompletionSource<T> _completion = new();

            public QueuedRequest(string owner, Func<CancellationToken, UniTask<T>> operation, CancellationToken externalToken)
            {
                Owner = owner;
                _operation = operation;
                _externalToken = externalToken;
            }

            public string Owner { get; }
            public UniTask<T> Task => _completion.Task;

            public async UniTask ExecuteAsync()
            {
                if (_localCts.IsCancellationRequested || _externalToken.IsCancellationRequested)
                {
                    _completion.TrySetCanceled();
                    return;
                }

                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_localCts.Token, _externalToken);
                try
                {
                    var result = await _operation(linkedCts.Token);
                    _completion.TrySetResult(result);
                }
                catch (OperationCanceledException)
                {
                    _completion.TrySetCanceled();
                }
                catch (Exception exception)
                {
                    _completion.TrySetException(exception);
                }
            }

            public void Cancel()
            {
                if (!_localCts.IsCancellationRequested)
                {
                    _localCts.Cancel();
                }

                _completion.TrySetCanceled();
            }
        }
    }
}
