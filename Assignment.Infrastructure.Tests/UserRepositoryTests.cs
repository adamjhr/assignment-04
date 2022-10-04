using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Assignment.Infrastructure;
using Assignment.Core;

namespace Assignment3.Entities.Tests;

public class UserRepositoryTests
{
    private KanbanContext _context;
    private IUserRepository _repository;

    public UserRepositoryTests() {
        var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();
        var builder = new DbContextOptionsBuilder<KanbanContext>();
        builder.UseSqlite(connection);
        var context = new KanbanContext(builder.Options);
        context.Database.EnsureCreated();

        context.Users.AddRange( new User("Alice", "user1@itu.dk"), 
                                new User("Bob", "user2@itu.dk"));
        context.SaveChanges();

        _context = context;
        _repository = new Infrastructure.UserRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public void UserRepository_create_returns_conflict_for_duplicate_email_case() 
    {
        //arrange

        //act
        var user = new UserCreateDTO ("CC","user2@itu.dk");
        var result = _repository.Create(user);

        //assert
        result.Response.Should().Be(Response.Conflict);
    }

    [Fact]
    public void UserRepository_delete_returns_conflict_when_the_force_is_false()
    {
        //arrange

        //act
        var result = _repository.Delete(2, false);

        //assert
        result.Should().Be(Response.Conflict);
    }

    [Fact]
    public void UserRepository_delete_returns_notfound_when_the_force_is_false_but_id_doesnt_exist()
    {
        //arrange

        //act
        var result = _repository.Delete(3, false);

        //assert
        result.Should().Be(Response.NotFound);
    }

    [Fact]
    public void UserRepository_update_returns_updated_when_user_exists()
    {
        //arrange
        var user = new UserUpdateDTO(1, "Alice", "user1@itu.dk");

        //act
        var result = _repository.Update(user);

        //assert        
        result.Should().Be(Response.Updated);

    }

    [Fact]
    public void UserRepository_Find_returns_correct_user()
    {
        //arrange
        var user = new UserDTO(2, "Bob", "user2@itu.dk");

        //act
        var result = _repository.Find(2);

        //assert        
        result.Should().Be(user);
    }

    [Fact]
    public void UserRepository_Find_returns_null_when_no_user_exists()
    {
        //assert
        Assert.Null(_repository.Find(3));
    }
}