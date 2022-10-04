using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection.Metadata;
using Assignment.Core;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Infrastructure;

public class WorkItemRepository : IWorkItemRepository
{
    private readonly KanbanContext _context;
    public WorkItemRepository(KanbanContext context)
    {
        _context = context;
    }

    (Response Response, int ItemId) IWorkItemRepository.Create(WorkItemCreateDTO workItem)
    {   

        var workItemEntity = new WorkItem(workItem.Title);
        workItemEntity.AssignedToId = workItem.AssignedToId;
        workItemEntity.Description = workItem.Description;

        // var tags = from tag in _context.Tags where workItem.Tags.Contains(tag.Name) select tag;
        // var tagList = tags.ToList();


        // ??
        // User assignedTo = new User();
        // if (workItem.AssignedToId != null) {
        //     assignedTo = _context.Users.Find(workItem.AssignedToId)!;
        //     if (assignedTo == null) return (Response.BadRequest, 0);
        // }
        
        _context.Items.Add(workItemEntity);
        _context.SaveChanges();
        return (Response.Created, workItemEntity.Id);
    
    }

    public IReadOnlyCollection<WorkItemDTO> Read()
    {
        var queryResult = _context.Items.ToList();

        var workItemDTOs = from t in _context.Items 
            select new WorkItemDTO(t.Id, t.Title, t.AssignedTo!.Name, t.Tags.Select(tag => tag.Name).ToImmutableList(), t.State);
        
        return workItemDTOs.ToList().AsReadOnly();

    }

    public IReadOnlyCollection<WorkItemDTO> ReadRemoved()
    {
        var queryResult = _context.Items.Where(t => t.State == State.Removed).ToList();
        
        var workItemDTOs = queryResult.Select(workItem => new WorkItemDTO(workItem.Id, workItem.Title, workItem.AssignedTo!.Name,
            workItem.Tags.Select(t => t.Name).ToImmutableList(), workItem.State)).ToImmutableList();

        return workItemDTOs;
    }

    public IReadOnlyCollection<WorkItemDTO> ReadByTag(string tag)
    {
        
        var queryResult = _context.Tags.Include(t => t.WorkItems).SingleOrDefault(t => t.Name == tag);

        if (queryResult == null) return null!;

        var workItemDTOs = queryResult.WorkItems.Select(workItem => new WorkItemDTO(workItem.Id, workItem.Title, workItem.AssignedTo!.Name,
            workItem.Tags.Select(t => t.Name).ToImmutableList(), workItem.State)).ToImmutableList();
        
        return workItemDTOs;
        
    }

    public IReadOnlyCollection<WorkItemDTO> ReadByUser(int userId)
    {
        var queryResult = _context.Users.Include(u => u.Items).SingleOrDefault(u => u.Id == userId);
        
        if (queryResult == null) return null!;

        var workItemDTOs = queryResult.Items.Select(workItem => new WorkItemDTO(workItem.Id, workItem.Title, workItem.AssignedTo!.Name,
            workItem.Tags.Select(t => t.Name).ToImmutableList(), workItem.State)).ToImmutableList();
        
        return workItemDTOs;
        
    }

    public IReadOnlyCollection<WorkItemDTO> ReadByState(State state)
    {
        var queryResult = _context.Items
            .Include(t => t.AssignedTo)
            .Include(t => t.Tags)
            .Where(t => t.State == state)
            .ToList();
        var workItemDTOs = queryResult.Select(workItem => new WorkItemDTO(workItem.Id, workItem.Title, workItem.AssignedTo!.Name,
            workItem.Tags.Select(t => t.Name).ToImmutableList(), workItem.State)).ToImmutableList();
        return workItemDTOs;
    }

    public WorkItemDetailsDTO Find(int workItemId)
    {
        var workItem = _context.Items.Include(t => t.AssignedTo).Include(t => t.Tags).SingleOrDefault(t => t.Id == workItemId);

        if (workItem == null) return null!;

        return new WorkItemDetailsDTO(workItem.Id, workItem.Title, workItem.Description!, workItem.CreatedDate, workItem.AssignedTo!.Name, workItem.Tags.Select(tag => tag.Name).ToImmutableList(),
            workItem.State, workItem.StateUpdated);
    }

    public Response Update(WorkItemUpdateDTO workItem)
    {
        var query = _context.Items.Include(t => t.Tags).SingleOrDefault(t => t.Id == workItem.Id);

        if (query is null) 
            return Response.NotFound;
        
        if (workItem.Description is not null)
            query.Description = workItem.Description;
        
        if (workItem.Title is not null)
            query.Title = workItem.Title;

        if (workItem.AssignedToId is not null) {
            var user = _context.Users.Find(workItem.AssignedToId)!;
            if (query.AssignedTo == null) return Response.BadRequest;
            query.AssignedTo = user;
            query.AssignedToId = user.Id;
        }

        if (query.State != workItem.State) {
            query.StateUpdated = DateTime.Now;
        }

        query.State = workItem.State;

        foreach (var tag in workItem.Tags.Except(query.Tags.Select(t => t.Name)))
            query.Tags.Add(_context.Tags.Single(t => t.Name == tag));
        
        foreach (var tag in query.Tags.ExceptBy(workItem.Tags, t => t.Name))
            query.Tags.Remove(_context.Tags.Single(t => t.Name == tag.Name));
        
        
        _context.SaveChanges();
        return Response.Updated;
    }

    public Response Delete(int workItemId)
    {
        
        var query = _context.Items.Find(workItemId);
        if (query == null) return Response.NotFound;
        var state = query.State;

        if (state == State.Resolved || state == State.Closed || state == State.Removed) return Response.Conflict;
        
        if (state == State.New)
            _context.Items.Remove(query);
        else if (state == State.Active)
            query.State = State.Removed;

        _context.SaveChanges();

        return Response.Deleted;
    }
}

