namespace kumaS.NuGetImporter.Editor.DataClasses
{
    public struct OperationResult
    {
        public OperationState State { get; }
        public string Message { get; }

        public OperationResult(OperationState state, string message)
        {
            State = state;
            Message = message;
        }
    }

    public enum OperationState
    {
        Progress,
        Success,
        Failure,
        Cancel
    }
}