using Common.General.Localization;
using Dtos.ParkOperators.Creating;
using Dtos.ParkOperators.ParkOperators;
using Dtos.ParkOperators.Updating;
using Entities.Model.Errors;
using Entities.Model.Parks;
using Entities.Model.Searching;
using OneOf;
using Repositories.Interfaces;
using Services.Interfaces;
using Services.Interfaces.Searching;

namespace Services.Implementations
{
    public class ParkOperatorsService : IParkOperatorsService
    {
        private readonly IParkOperatorsQueryHandler operatorsQueryHandler;
        private readonly ISearchIndexService searchIndexService;
        private readonly IMongoDbSettings mongoDbSettings;

        public ParkOperatorsService(
            IParkOperatorsQueryHandler operatorsQueryHandler,
            ISearchIndexService searchIndexService,
            IMongoDbSettings mongoDbSettings)
        {
            this.operatorsQueryHandler = operatorsQueryHandler;
            this.searchIndexService = searchIndexService;
            this.mongoDbSettings = mongoDbSettings;
        }

        public async Task<OneOf<IEnumerable<ParkOperatorDto>, ErrorCodes.ErrorDetail>> GetAllAsync()
        {
            IEnumerable<ParkOperator> operators = await operatorsQueryHandler.GetAllAsync();
            List<ParkOperatorDto> result = operators.Select(MapToDto).ToList();
            return OneOf<IEnumerable<ParkOperatorDto>, ErrorCodes.ErrorDetail>.FromT0(result);
        }

        public async Task<OneOf<ParkOperatorDto, ErrorCodes.ErrorDetail>> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return ErrorCodes.ParkOperatorNotExists;
            }

            ParkOperator? parkOperator = await operatorsQueryHandler.GetByIdAsync(id);
            if (parkOperator == null)
            {
                return ErrorCodes.ParkOperatorNotExists;
            }

            return MapToDto(parkOperator);
        }

        public async Task<OneOf<ParkOperatorDto, ErrorCodes.ErrorDetail>> CreateAsync(ParkOperatorCreateDto dto)
        {
            ParkOperator parkOperator = new()
            {
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Name = dto.Name.Trim(),
                Description = NormalizeLocalizedTextItems(dto.Description)
            };

            ParkOperator? created = await operatorsQueryHandler.CreateAsync(parkOperator);
            if (created == null)
            {
                return ErrorCodes.ErrorCreatingParkOperator;
            }

            SearchItem searchItem = searchIndexService.ConvertParkOperatorToSearchItem(created);
            await searchIndexService.UpsertSearchItemAsync(searchItem, mongoDbSettings.SearchItemCollectionName);

            return MapToDto(created);
        }

        public async Task<OneOf<ParkOperatorDto, ErrorCodes.ErrorDetail>> UpdateAsync(string id, ParkOperatorUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return ErrorCodes.ParkOperatorNotExists;
            }

            ParkOperator? existing = await operatorsQueryHandler.GetByIdAsync(id);
            if (existing == null)
            {
                return ErrorCodes.ParkOperatorNotExists;
            }

            existing.Name = dto.Name.Trim();
            existing.Description = NormalizeLocalizedTextItems(dto.Description);
            existing.UpdatedAt = DateTime.UtcNow;

            ParkOperator? updated = await operatorsQueryHandler.UpdateAsync(existing);
            if (updated == null)
            {
                return ErrorCodes.ErrorUpdatingParkOperator;
            }

            SearchItem searchItem = searchIndexService.ConvertParkOperatorToSearchItem(updated);
            await searchIndexService.UpsertSearchItemAsync(searchItem, mongoDbSettings.SearchItemCollectionName);

            return MapToDto(updated);
        }

        private static ParkOperatorDto MapToDto(ParkOperator parkOperator)
        {
            return new ParkOperatorDto
            {
                Id = parkOperator.Id,
                Name = parkOperator.Name,
                Description = parkOperator.Description
            };
        }

        private static List<LocalizedItem<string>> NormalizeLocalizedTextItems(IEnumerable<LocalizedItem<string>>? items)
        {
            if (items == null)
            {
                return new List<LocalizedItem<string>>();
            }

            return items
                .Where(item => item != null)
                .Where(item => !string.IsNullOrWhiteSpace(item.LanguageCode))
                .Select(item => new LocalizedItem<string>
                {
                    LanguageCode = item.LanguageCode.Trim().ToLowerInvariant(),
                    Value = item.Value?.Trim() ?? string.Empty
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.Value))
                .GroupBy(item => item.LanguageCode)
                .Select(group => group.Last())
                .ToList();
        }
    }
}
