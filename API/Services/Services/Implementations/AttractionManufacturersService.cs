using Common.General.Localization;
using Dtos.AttractionManufacturers.AttractionManufacturers;
using Dtos.AttractionManufacturers.Creating;
using Dtos.AttractionManufacturers.Updating;
using Entities.Model.Errors;
using Entities.Model.Parks;
using Entities.Model.Searching;
using OneOf;
using Repositories.Interfaces;
using Services.Interfaces;
using Services.Interfaces.Searching;

namespace Services.Implementations
{
    public class AttractionManufacturersService : IAttractionManufacturersService
    {
        private readonly IAttractionManufacturersQueryHandler manufacturersQueryHandler;
        private readonly IParkItemsQueryHandler parkItemsQueryHandler;
        private readonly ISearchIndexService searchIndexService;
        private readonly IMongoDbSettings mongoDbSettings;

        public AttractionManufacturersService(
            IAttractionManufacturersQueryHandler manufacturersQueryHandler,
            IParkItemsQueryHandler parkItemsQueryHandler,
            ISearchIndexService searchIndexService,
            IMongoDbSettings mongoDbSettings)
        {
            this.manufacturersQueryHandler = manufacturersQueryHandler;
            this.parkItemsQueryHandler = parkItemsQueryHandler;
            this.searchIndexService = searchIndexService;
            this.mongoDbSettings = mongoDbSettings;
        }

        public async Task<OneOf<IEnumerable<AttractionManufacturerDto>, ErrorCodes.ErrorDetail>> GetAllAsync()
        {
            IEnumerable<AttractionManufacturer> manufacturers = await manufacturersQueryHandler.GetAllAsync();
            List<AttractionManufacturer> manufacturersList = manufacturers.ToList();
            Dictionary<string, int> counts = await parkItemsQueryHandler.GetAttractionCountsByManufacturerIdsAsync(
                manufacturersList
                    .Where(manufacturer => !string.IsNullOrWhiteSpace(manufacturer.Id))
                    .Select(manufacturer => manufacturer.Id!));

            List<AttractionManufacturerDto> result = manufacturersList
                .Select(manufacturer => MapToDto(manufacturer, counts))
                .ToList();

            return OneOf<IEnumerable<AttractionManufacturerDto>, ErrorCodes.ErrorDetail>.FromT0(result);
        }

        public async Task<OneOf<AttractionManufacturerDto, ErrorCodes.ErrorDetail>> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return ErrorCodes.AttractionManufacturerNotExists;
            }

            AttractionManufacturer? manufacturer = await manufacturersQueryHandler.GetByIdAsync(id);
            if (manufacturer == null)
            {
                return ErrorCodes.AttractionManufacturerNotExists;
            }

            Dictionary<string, int> counts = await parkItemsQueryHandler.GetAttractionCountsByManufacturerIdsAsync(new[] { id });
            return MapToDto(manufacturer, counts);
        }

        public async Task<OneOf<AttractionManufacturerDto, ErrorCodes.ErrorDetail>> CreateAsync(AttractionManufacturerCreateDto dto)
        {
            AttractionManufacturer manufacturer = new()
            {
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Name = dto.Name.Trim(),
                Biography = NormalizeLocalizedTextItems(dto.Biography)
            };

            AttractionManufacturer? created = await manufacturersQueryHandler.CreateAsync(manufacturer);
            if (created == null)
            {
                return ErrorCodes.ErrorCreatingAttractionManufacturer;
            }

            SearchItem searchItem = searchIndexService.ConvertAttractionManufacturerToSearchItem(created);
            await searchIndexService.UpsertSearchItemAsync(searchItem, mongoDbSettings.SearchItemCollectionName);

            return MapToDto(created, new Dictionary<string, int>());
        }

        public async Task<OneOf<AttractionManufacturerDto, ErrorCodes.ErrorDetail>> UpdateAsync(string id, AttractionManufacturerUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return ErrorCodes.AttractionManufacturerNotExists;
            }

            AttractionManufacturer? existing = await manufacturersQueryHandler.GetByIdAsync(id);
            if (existing == null)
            {
                return ErrorCodes.AttractionManufacturerNotExists;
            }

            existing.Name = dto.Name.Trim();
            existing.Biography = NormalizeLocalizedTextItems(dto.Biography);
            existing.UpdatedAt = DateTime.UtcNow;

            AttractionManufacturer? updated = await manufacturersQueryHandler.UpdateAsync(existing);
            if (updated == null)
            {
                return ErrorCodes.ErrorUpdatingAttractionManufacturer;
            }

            SearchItem searchItem = searchIndexService.ConvertAttractionManufacturerToSearchItem(updated);
            await searchIndexService.UpsertSearchItemAsync(searchItem, mongoDbSettings.SearchItemCollectionName);

            Dictionary<string, int> counts = await parkItemsQueryHandler.GetAttractionCountsByManufacturerIdsAsync(new[] { id });
            return MapToDto(updated, counts);
        }

        private static AttractionManufacturerDto MapToDto(AttractionManufacturer manufacturer, IReadOnlyDictionary<string, int> counts)
        {
            int attractionCount = 0;
            if (!string.IsNullOrWhiteSpace(manufacturer.Id) && counts.TryGetValue(manufacturer.Id, out int count))
            {
                attractionCount = count;
            }

            return new AttractionManufacturerDto
            {
                Id = manufacturer.Id,
                Name = manufacturer.Name,
                Biography = manufacturer.Biography,
                AttractionCount = attractionCount
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
