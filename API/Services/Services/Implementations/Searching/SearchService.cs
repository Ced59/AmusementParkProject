using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dtos.Searching;
using Entities.Model.Errors;
using OneOf;
using Repositories.Interfaces;
using Services.Interfaces.Searching;

namespace Services.Implementations.Searching
{
    public class SearchService : ISearchService
    {
        private ISearchQueryHandler _queryHandler;

        public SearchService(ISearchQueryHandler queryHandler)
        {
            _queryHandler = queryHandler;
        }

        public Task<OneOf<SearchResultDto, ErrorCodes.ErrorDetail>> SearchAsync(string query, string[] categories, int page, int pageSize)
        {
            throw new NotImplementedException();
        }
    }
}