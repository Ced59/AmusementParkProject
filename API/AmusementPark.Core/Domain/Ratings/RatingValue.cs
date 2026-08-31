namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Note exacte comprise entre 0,5 et 5, représentée par un nombre entier de demi-points.
/// </summary>
public readonly record struct RatingValue
{
    public const byte MinimumHalfSteps = 1;

    public const byte MaximumHalfSteps = 10;

    private readonly byte halfSteps;

    private RatingValue(byte halfSteps)
    {
        this.halfSteps = halfSteps;
    }

    public byte HalfSteps => this.halfSteps >= MinimumHalfSteps
        ? this.halfSteps
        : throw new InvalidOperationException("An uninitialized rating has no value.");

    public decimal DecimalValue => this.HalfSteps / 2m;

    public double DoubleValue => this.HalfSteps / 2d;

    public static RatingValue FromHalfSteps(byte halfSteps)
    {
        if (halfSteps < MinimumHalfSteps || halfSteps > MaximumHalfSteps)
        {
            throw CreateValidationException(
                RatingValueErrorCodes.InvalidValue,
                "The rating must be between 0.5 and 5.",
                nameof(halfSteps));
        }

        return new RatingValue(halfSteps);
    }

    public static RatingValue FromDecimal(decimal value)
    {
        if (!TryFromDecimal(value, out RatingValue ratingValue, out string? errorCode))
        {
            throw CreateValidationException(errorCode!, "The rating must use a half-point between 0.5 and 5.", nameof(value));
        }

        return ratingValue;
    }

    public static RatingValue FromDouble(double value)
    {
        if (!TryFromDouble(value, out RatingValue ratingValue, out string? errorCode))
        {
            throw CreateValidationException(errorCode!, "The rating must use a half-point between 0.5 and 5.", nameof(value));
        }

        return ratingValue;
    }

    public static bool TryFromDecimal(
        decimal value,
        out RatingValue ratingValue,
        out string? errorCode)
    {
        ratingValue = default;

        if (value < 0m || value > 5m)
        {
            errorCode = RatingValueErrorCodes.InvalidValue;
            return false;
        }

        decimal halfSteps = value * 2m;
        if (halfSteps != decimal.Truncate(halfSteps))
        {
            errorCode = RatingValueErrorCodes.InvalidStep;
            return false;
        }

        byte exactHalfSteps = checked((byte)halfSteps);
        if (exactHalfSteps < MinimumHalfSteps)
        {
            errorCode = RatingValueErrorCodes.InvalidValue;
            return false;
        }

        ratingValue = new RatingValue(exactHalfSteps);
        errorCode = null;
        return true;
    }

    public static bool TryFromDouble(
        double value,
        out RatingValue ratingValue,
        out string? errorCode)
    {
        ratingValue = default;

        if (!double.IsFinite(value) || value < 0d || value > 5d)
        {
            errorCode = RatingValueErrorCodes.InvalidValue;
            return false;
        }

        double halfSteps = value * 2d;
        if (halfSteps != Math.Truncate(halfSteps))
        {
            errorCode = RatingValueErrorCodes.InvalidStep;
            return false;
        }

        byte exactHalfSteps = checked((byte)halfSteps);
        if (exactHalfSteps < MinimumHalfSteps)
        {
            errorCode = RatingValueErrorCodes.InvalidValue;
            return false;
        }

        ratingValue = new RatingValue(exactHalfSteps);
        errorCode = null;
        return true;
    }

    public override string ToString()
    {
        return this.DecimalValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static RatingValueValidationException CreateValidationException(
        string errorCode,
        string message,
        string parameterName)
    {
        return new RatingValueValidationException(errorCode, message, parameterName);
    }
}
