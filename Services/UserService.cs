
using ProdmasterProvidersService.Repositories;
using ProvidersDomain.Models;
using ProvidersDomain.Services;
using ProvidersDomain.ViewModels.Account;

namespace ProdmasterProvidersService.Services
{
    public class UserService : IUserService
    {
        private readonly UserRepository _repository;
        public UserService(UserRepository repository)
        {
            _repository = repository;
        }
        public Task<User> Add(string email, string password)
        {
            return _repository.Add(new User() { Email = email, Password = password });
        }

        public Task<User> Add(RegisterModel model)
        {
            return _repository.Add(new User 
            { 
                Name = model.Name,
                Email = model.Email, 
                Password = model.Password,
                INN = model.INN,
                Phone = model.Phone,                
            });
        }

        public Task<User> Get(string email, string password)
        {
            return _repository.First(c => c.Email == email && c.Password == password);
        }

        public Task<User> GetByEmail(string email)
        {
            return _repository.First(c => c.Email == email);
        }

        public Task<User> GetByINN(string inn)
        {
            return _repository.First(c => c.INN == inn);
        }

        public async Task<UserModel> GetModelFromUser(User user)
        {
            return new UserModel
            {
                Name = user.Name,
                Email = user.Email,
                INN = user.INN,
                Phone = user.Phone,
            };
        }

        public Task<User> UpdateUser(User user)
        {
            return _repository.Update(user);
        }

        public async Task<bool> UserExists(RegisterModel model)
        {
            var user = await _repository.First(c => c.Email == model.Email || c.INN == model.INN);
            return !ReferenceEquals(user, null);
        }
    }
}
