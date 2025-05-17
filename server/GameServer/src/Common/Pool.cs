
public class Pool<T>
{
    public int Count { get { return _list.Count; } }
    private Func<T> _createFunction;
    private Queue<T> _list = new Queue<T>();
    public Pool(Func<T> create)
    {
        _createFunction = create;
    }
    public void Clear(Action<T> itemCallback = null)
    {
        int len = _list.Count;
        while (len-- > 0)
        {
            var ins = _list.Dequeue();
            if (itemCallback != null)
            {
                itemCallback.Invoke(ins);
            }
        }
    }
    public T Pop()
    {
        if (_list.Count > 0)
        {
            return _list.Dequeue();
        }
        return _createFunction();
    }
    public void Push(T value)
    {
        _list.Enqueue(value);
    }
}