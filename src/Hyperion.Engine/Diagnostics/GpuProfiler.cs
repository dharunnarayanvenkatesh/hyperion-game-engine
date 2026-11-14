using OpenTK.Graphics.OpenGL4;

namespace Hyperion.Engine.Diagnostics;

internal sealed class GpuProfiler : IDisposable
{
    private readonly Queue<int> _pendingQueries = new();
    private readonly Stack<int> _availableQueries = new();
    private int _activeQuery;

    public double LastFrameMilliseconds { get; private set; }

    public void BeginFrame()
    {
        ResolveCompletedQueries();
        _activeQuery = _availableQueries.Count > 0 ? _availableQueries.Pop() : GL.GenQuery();
        GL.BeginQuery(QueryTarget.TimeElapsed, _activeQuery);
    }

    public void EndFrame()
    {
        if (_activeQuery == 0)
        {
            return;
        }

        GL.EndQuery(QueryTarget.TimeElapsed);
        _pendingQueries.Enqueue(_activeQuery);
        _activeQuery = 0;
    }

    public void Dispose()
    {
        if (_activeQuery != 0)
        {
            GL.EndQuery(QueryTarget.TimeElapsed);
            GL.DeleteQuery(_activeQuery);
            _activeQuery = 0;
        }

        while (_pendingQueries.TryDequeue(out var query))
        {
            GL.DeleteQuery(query);
        }

        while (_availableQueries.TryPop(out var query))
        {
            GL.DeleteQuery(query);
        }
    }

    private void ResolveCompletedQueries()
    {
        while (_pendingQueries.TryPeek(out var query))
        {
            GL.GetQueryObject(query, GetQueryObjectParam.QueryResultAvailable, out int isAvailable);
            if (isAvailable == 0)
            {
                break;
            }

            GL.GetQueryObject(query, GetQueryObjectParam.QueryResult, out long nanoseconds);
            LastFrameMilliseconds = nanoseconds / 1_000_000.0;
            _pendingQueries.Dequeue();
            _availableQueries.Push(query);
        }
    }
}
