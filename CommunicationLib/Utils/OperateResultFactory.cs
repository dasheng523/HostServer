
using CommunicationLib.Interface;

public static class OperateResultFactory
{
    public static IOperateResult<T> CreateSuccessResult<T>(T value)
    {
        return new SuccessResult<T>(value);
    }

    public static IOperateResult<T> CreateSuccessResult<T>()
    {
        return new SuccessResult<T>();
    }

    public static IOperateResult<T> CreateFailureResult<T>()
    {
        return new FailureResult<T>();
    }

    public static IOperateResult<T> CreateFailureResult<T>(int errorCode, string message)
    {
        return new FailureResult<T>(errorCode, message);
    }

}

class SuccessResult<T> : IOperateResult<T>
{
    private readonly T _value;

    public SuccessResult() { }
    public SuccessResult(T value) { _value = value; }
    public bool IsSuccess() => true;
    public string GetMessage() => "Success";
    public int GetErrorCode() => 0;
    public T Value() => default;
}

class FailureResult<T> : IOperateResult<T>
{
    private readonly int _errorCode;
    private readonly string _message;

    public FailureResult() { }
    public FailureResult(int errorCode, string message) { _errorCode = errorCode; _message = message; }
    public bool IsSuccess() => false;
    public string GetMessage() => _message;
    public int GetErrorCode() => _errorCode;
    public T Value() => default;
}