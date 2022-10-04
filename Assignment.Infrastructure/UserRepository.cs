namespace Assignment.Infrastructure;

public class UserRepository : IUserRepository
{
    private KanbanContext _context;
    public UserRepository(KanbanContext context)
    {
        _context = context;
    }
    public (Response Response, int UserId) Create(UserCreateDTO user)
    {
        throw new NotImplementedException();
    }

    public Response Delete(int userId, bool force = false)
    {
        throw new NotImplementedException();
    }

    public UserDTO Find(int userId)
    {
        foreach (var user in _context.Users)
        {
            if (user.Id == userId)
            {
                return new UserDTO(user.Id, user.Name, user.Email);
            }
        }
        return null!;
    }

    public IReadOnlyCollection<UserDTO> Read()
    {
        throw new NotImplementedException();
    }

    public Response Update(UserUpdateDTO user)
    {
        throw new NotImplementedException();
    }
}
