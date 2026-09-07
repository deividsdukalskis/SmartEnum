namespace SmartEnum.Shared;

public abstract record Result<TValue, TError>;

public record Failure<TValue, TError> : Result<TValue, TError>
{
	public TError Error { get; }

	public Failure(TError error) => this.Error = error;
}

public record Success<TValue, TError> : Result<TValue, TError>
{
	public TValue Value { get; }

	public Success(TValue value) => this.Value = value;
}

public abstract record Result<TError>;

public record Failure<TError> : Result<TError>
{
	public TError Error { get; }

	public Failure(TError error) => this.Error = error;
}

public record Success<TError> : Result<TError>;