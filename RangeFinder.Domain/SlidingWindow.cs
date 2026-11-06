namespace RangeFinder.Domain;

public class SlidingWindow<T>
{
    private readonly int _windowSize;
    private readonly Queue<T> _queue;

    public SlidingWindow(int windowSize)
    {
        if (windowSize < 0)
            throw new ArgumentException("Window size must be greater than or equal to zero.", nameof(windowSize));
        _windowSize = windowSize;
        _queue = windowSize > 0 ? new Queue<T>(windowSize) : null!;
    }

    public void Add(T item)
    {
        if (_windowSize == 0)
        {
            return;
        }

        if (_queue.Count == _windowSize)
        {
            _queue.Dequeue();
        }
        _queue.Enqueue(item);
    }

    public IEnumerable<T> GetWindow()
    {
        return _windowSize == 0 ? [] : [.. _queue];
    }

    public IEnumerable<T> GetWindowAndClear()
    {
        var window = GetWindow();
        _queue?.Clear();
        return window;
    }
}
