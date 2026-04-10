using Common.General.Localization;
using OneOf;
using Dtos.ParkFounders.Creating;
using Dtos.ParkFounders.ParkFounders;
using Dtos.ParkFounders.Updating;
using Entities.Model.Errors;
using Entities.Model.Parks;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations;

public class ParkFoundersService : IParkFoundersService
    {
        private readonly IParkFoundersQueryHandler foundersQueryHandler;

        public ParkFoundersService(IParkFoundersQueryHandler foundersQueryHandler)
        {
            this.foundersQueryHandler = foundersQueryHandler;
        }

        public async Task<OneOf<IEnumerable<ParkFounderDto>, ErrorCodes.ErrorDetail>> GetAllAsync()
        {
            IEnumerable<ParkFounder> founders = await foundersQueryHandler.GetAllAsync();

            List<ParkFounderDto> result = founders.Select(MapToDto).ToList();
            return OneOf.OneOf<IEnumerable<ParkFounderDto>, ErrorCodes.ErrorDetail>.FromT0(result);
        }

        public async Task<OneOf<ParkFounderDto, ErrorCodes.ErrorDetail>> GetByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return ErrorCodes.ParkFounderNotExists;
            }

            ParkFounder? founder = await foundersQueryHandler.GetByIdAsync(id);

            if (founder == null)
            {
                return ErrorCodes.ParkFounderNotExists;
            }

            return MapToDto(founder);
        }

        public async Task<OneOf<ParkFounderDto, ErrorCodes.ErrorDetail>> CreateAsync(ParkFounderCreateDto dto)
        {
            ParkFounder founder = new()
            {
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Name = dto.Name.Trim(),
                Biography = NormalizeLocalizedTextItems(dto.Biography)
            };

            ParkFounder? created = await foundersQueryHandler.CreateAsync(founder);

            if (created == null)
            {
                return ErrorCodes.ErrorCreatingParkFounder;
            }

            return MapToDto(created);
        }

        public async Task<OneOf<ParkFounderDto, ErrorCodes.ErrorDetail>> UpdateAsync(string id, ParkFounderUpdateDto dto)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return ErrorCodes.ParkFounderNotExists;
            }

            ParkFounder? existing = await foundersQueryHandler.GetByIdAsync(id);

            if (existing == null)
            {
                return ErrorCodes.ParkFounderNotExists;
            }

            existing.Name = dto.Name.Trim();
            existing.Biography = NormalizeLocalizedTextItems(dto.Biography);
            existing.UpdatedAt = DateTime.UtcNow;

            ParkFounder? updated = await foundersQueryHandler.UpdateAsync(existing);

            if (updated == null)
            {
                return ErrorCodes.ErrorUpdatingParkFounder;
            }

            return MapToDto(updated);
        }

        private static ParkFounderDto MapToDto(ParkFounder founder)
        {
            return new ParkFounderDto
            {
                Id = founder.Id,
                Name = founder.Name,
                Biography = founder.Biography
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