using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Infrastructure.Tests;

public class WorkItemRepositoryTests
{

    private readonly SqliteConnection _connection;
    private KanbanContext _context;
    private IWorkItemRepository _repository;
    private ITagRepository _tagRepository;
    private IUserRepository _userRepository;

    public WorkItemRepositoryTests() 
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
        var builder = new DbContextOptionsBuilder<KanbanContext>().UseSqlite(_connection);
        _context = new KanbanContext(builder.Options);
        _context.Database.EnsureCreated();

        _repository = new WorkItemRepository(_context);
        _tagRepository = new TagRepository(_context);
        _userRepository = new UserRepository(_context);
    }


    [Fact]
    public void Delete_With_State_New_Returns_Deleted() {
        
        var workItem = _repository.Create(new Core.WorkItemCreateDTO("testWorkItem", null, null, new List<string> {}));
        var response = _repository.Delete(workItem.ItemId);

        response.Should().Be(Core.Response.Deleted);
    }

    [Fact]
    public void State_Is_New_Upon_Creation() {
        
        var workItem = _repository.Create(new Core.WorkItemCreateDTO("testWorkItem", null, null, new List<string> {}));
        var response = _repository.Find(workItem.ItemId).State;

        response.Should().Be(Core.State.New);
    }

    [Fact]
    public void Creation_Time_Is_Correct_Time() {

        var expectedTime = DateTime.Now;
        var workItem = _repository.Create(new Core.WorkItemCreateDTO("testWorkItem", null, null, new List<string> {}));
        var response = _repository.Find(workItem.ItemId).Created;

        response.Should().BeCloseTo(expectedTime, TimeSpan.FromSeconds(5));
    }


    [Fact]
    public void Update_Time_Is_Correct_Time_After_Create() {

        var expectedTime = DateTime.Now;
        var workItem = _repository.Create(new Core.WorkItemCreateDTO("testWorkItem", null, null, new List<string> {}));
        var response = _repository.Find(workItem.ItemId).StateUpdated;

        response.Should().BeCloseTo(expectedTime, TimeSpan.FromSeconds(5));
    }
    [Fact]
    public void Update_Time_Is_Correct_Time_After_Updated() {

        var workItem = _repository.Create(new Core.WorkItemCreateDTO("testWorkItem", null, null, new List<string> {}));
        
        var expectedTime = DateTime.Now;
        _repository.Update(new Core.WorkItemUpdateDTO(workItem.ItemId, "testWorkItem", null, null, new List<string> {}, Core.State.Active));
        var response = _repository.Find(workItem.ItemId).StateUpdated;

        response.Should().BeCloseTo(expectedTime, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Update_State_To_Resolved_Sets_State_To_Resolved() {
        
        var workItem = _repository.Create(new Core.WorkItemCreateDTO("testWorkItem", null, null, new List<string> {}));
        var updated = _repository.Update(new Core.WorkItemUpdateDTO(workItem.ItemId, "testWorkItem", null, null, new List<string> {}, Core.State.Resolved));
        
        var response = _repository.Find(workItem.ItemId).State;

        response.Should().Be(Core.State.Resolved);
    }

        [Fact]
    public void Update_State_To_Active_Sets_State_To_Active() {

        var workItem = _repository.Create(new Core.WorkItemCreateDTO("testWorkItem", null, null, new List<string> {}));
        var updated = _repository.Update(new Core.WorkItemUpdateDTO(workItem.ItemId, "testWorkItem", null, null, new List<string> {}, Core.State.Active));
        
        var response = _repository.Find(workItem.ItemId).State;

        response.Should().Be(Core.State.Active);
    }

    [Fact]
    public void Update_State_To_Closed_Sets_State_To_Closed() {
        
        var workItem = _repository.Create(new Core.WorkItemCreateDTO("testWorkItem", null, null, new List<string> {}));
        var updated = _repository.Update(new Core.WorkItemUpdateDTO(workItem.ItemId, "testWorkItem", null, null, new List<string> {}, Core.State.Closed));
        
        var response = _repository.Find(workItem.ItemId).State;

        response.Should().Be(Core.State.Closed);
    }

    [Fact]
    public void Delete_With_State_Active_Sets_State_To_Removed() {

        var workItem = _repository.Create(new Core.WorkItemCreateDTO("testWorkItem", null, null, new List<string> {}));
        _repository.Update(new Core.WorkItemUpdateDTO(workItem.ItemId, "testWorkItem", null, null, new List<string> {}, Core.State.Active));

        _repository.Delete(workItem.ItemId);
        var response = _repository.Find(workItem.ItemId).State;

        response.Should().Be(Core.State.Removed);
    }

    [Fact]
    public void Delete_With_State_Removed_Returns_Conflict() {
        
        var workItem = _repository.Create(new Core.WorkItemCreateDTO("testWorkItem", null, null, new List<string> {}));
        _repository.Update(new Core.WorkItemUpdateDTO(workItem.ItemId, "testWorkItem", null, null, new List<string> {}, Core.State.Active));

        _repository.Delete(workItem.ItemId);
        var response = _repository.Delete(workItem.ItemId);

        response.Should().Be(Core.Response.Conflict);
    }

    [Fact]
    public void Delete_With_State_Resolved_Returns_Conflict() {
        
        var workItem = _repository.Create(new Core.WorkItemCreateDTO("testWorkItem", null, null, new List<string> {}));
        _repository.Update(new Core.WorkItemUpdateDTO(workItem.ItemId, "testWorkItem", null, null, new List<string> {}, Core.State.Resolved));

        var response = _repository.Delete(workItem.ItemId);

        response.Should().Be(Core.Response.Conflict);
    }

    [Fact]
    public void Delete_With_State_Closed_Returns_Conflict() {
        
        var workItem = _repository.Create(new Core.WorkItemCreateDTO("testWorkItem", null, null, new List<string> {}));
        _repository.Update(new Core.WorkItemUpdateDTO(workItem.ItemId, "testWorkItem", null, null, new List<string> {}, Core.State.Closed));

        var response = _repository.Delete(workItem.ItemId);

        response.Should().Be(Core.Response.Conflict);
    }

    [Fact]
    public void Assign_NonExistant_User_Gives_BadRequest() {

        var workItem = _repository.Create(new Core.WorkItemCreateDTO("testWorkItem", 1, null, new List<string> {}));
        var response = workItem.Response;

        response.Should().Be(Core.Response.BadRequest);
    }
    
    [Fact]
    public void Delete_NonExistant_Returns_NotFound() {
        
        var response = _repository.Delete(0);

        response.Should().Be(Core.Response.NotFound);
    }

    [Fact]
    public void Update_NonExistant_Returns_NotFound() {
        
        var response = _repository.Update(new Core.WorkItemUpdateDTO(0, "testWorkItem", null, null, new List<string> {}, Core.State.Closed));

        response.Should().Be(Core.Response.NotFound);
    }

    [Fact]
    public void Find_NonExistant_Returns_Null() {
        
        var response = _repository.Find(0);

        response.Should().Be(null);
    }

    [Fact]
    public void Read_Returns_5() {

        _repository.Create(new Core.WorkItemCreateDTO("workItem1", null, null, new List<string> {}));
        _repository.Create(new Core.WorkItemCreateDTO("workItem2", null, null, new List<string> {}));
        _repository.Create(new Core.WorkItemCreateDTO("workItem3", null, null, new List<string> {}));
        _repository.Create(new Core.WorkItemCreateDTO("workItem4", null, null, new List<string> {}));
        _repository.Create(new Core.WorkItemCreateDTO("workItem5", null, null, new List<string> {}));
        
        var total = _repository.Read().Count();

        total.Should().Be(5);
    }

    [Fact]
    public void ReadAByState_New_Returns_3() {

        _repository.Create(new Core.WorkItemCreateDTO("workItem1", null, null, new List<string> {}));
        _repository.Create(new Core.WorkItemCreateDTO("workItem2", null, null, new List<string> {}));
        _repository.Create(new Core.WorkItemCreateDTO("workItem3", null, null, new List<string> {}));
        var workItem4 = _repository.Create(new Core.WorkItemCreateDTO("workItem4", null, null, new List<string> {}));
        var workItem5 = _repository.Create(new Core.WorkItemCreateDTO("workItem5", null, null, new List<string> {}));

        _repository.Update(new Core.WorkItemUpdateDTO(workItem4.ItemId, "workItem4", null, null, new List<string> {}, Core.State.Active));
        _repository.Update(new Core.WorkItemUpdateDTO(workItem5.ItemId, "workItem5", null, null, new List<string> {}, Core.State.Active));

        var total = _repository.ReadByState(Core.State.New).Count();

        total.Should().Be(3);
    }

    [Fact]
    public void ReadByUser_New_Returns_2() {
        
        var user1 = _userRepository.Create(new Core.UserCreateDTO("name", "email1"));
        var user2 = _userRepository.Create(new Core.UserCreateDTO("name", "email2"));
        _repository.Create(new Core.WorkItemCreateDTO("workItem1", user1.UserId, null, new List<string> {}));
        _repository.Create(new Core.WorkItemCreateDTO("workItem2", null, null, new List<string> {}));
        _repository.Create(new Core.WorkItemCreateDTO("workItem3", null, null, new List<string> {}));
        _repository.Create(new Core.WorkItemCreateDTO("workItem4", user1.UserId, null, new List<string> {}));
        _repository.Create(new Core.WorkItemCreateDTO("workItem5", user2.UserId, null, new List<string> {}));
     
        var total = _repository.ReadByUser(user1.UserId).Count();

        total.Should().Be(2);
    }

    [Fact]
    public void ReadByTag_New_Returns_3() {

        _tagRepository.Create(new Core.TagCreateDTO("tag1"));
        _tagRepository.Create(new Core.TagCreateDTO("tag2"));
        _tagRepository.Create(new Core.TagCreateDTO("tag3"));
        _repository.Create(new Core.WorkItemCreateDTO("workItem1", null, null, new List<string> {"tag1"}));
        _repository.Create(new Core.WorkItemCreateDTO("workItem2", null, null, new List<string> {"tag2"}));
        _repository.Create(new Core.WorkItemCreateDTO("workItem3", null, null, new List<string> {"tag1", "tag3"}));
        _repository.Create(new Core.WorkItemCreateDTO("workItem4", null, null, new List<string> {}));
        _repository.Create(new Core.WorkItemCreateDTO("workItem5", null, null, new List<string> {"tag3"}));
        _repository.Create(new Core.WorkItemCreateDTO("workItem6", null, null, new List<string> {}));
        _repository.Create(new Core.WorkItemCreateDTO("workItem7", null, null, new List<string> {"tag1", "tag2", "tag3"}));

        var total = _repository.ReadByTag("tag1").Count();

        total.Should().Be(3);
    }

    [Fact]
    public void Read_WorkItem_Returns_Title_workItem1() {

        var workItem = _repository.Create(new Core.WorkItemCreateDTO("workItem1", null, null, new List<string> {}));

        var readWorkItem = _repository.Find(workItem.ItemId);
        var workItemTitle = readWorkItem.Title;

        workItemTitle.Should().Be("workItem1");
    }

}
