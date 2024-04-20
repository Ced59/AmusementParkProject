using Entities.Model.Errors;
using Entities.Model.Users;
using OneOf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IUsersService
    {
        Task<OneOf<UserCreated, ErrorCodes.ErrorDetail>>? CreateUser(UserCreate user);
    }
}
