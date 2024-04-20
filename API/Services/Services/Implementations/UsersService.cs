using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entities.Model.Users;
using Services.Interfaces;

namespace Services.Implementations
{
    public class UsersService : IUsersService
    {
        public UserCreated? CreateUser(UserCreate user)
        {
            throw new NotImplementedException();
        }
    }
}
