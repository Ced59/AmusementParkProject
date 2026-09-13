namespace AmusementPark.Core.Domain.Parks;

public enum ParkPriceValidationError
{
    InvalidMode = 0,
    NegativePrice = 1,
    FixedAmountRequired = 2,
    RangeBoundsRequired = 3,
    InvalidRange = 4,
}
