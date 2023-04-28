namespace KubeMQ.Contract
{
    public class TaggedResponse<T>
    {
        public Dictionary<string, string>? Tags { get; init; } = null;
        public T Response { get; init; }
    }
}
