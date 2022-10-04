using Assignment.Core;

namespace Assignment.Infrastructure;

public class TagRepository : ITagRepository
{
    private readonly KanbanContext _context;

    public TagRepository(KanbanContext context)
    {
        _context = context;
    }

    (Response Response, int TagId) ITagRepository.Create(TagCreateDTO tag)
    {
        var entity = _context.Tags.FirstOrDefault(c => c.Name == tag.Name);

        Response response;

        if (entity is null)
        {
            entity = new Tag(tag.Name);

            _context.Tags.Add(entity);
            _context.SaveChanges();

            response = Response.Created;
        }
        else
        {
            response = Response.Conflict;
        }

        return (response, entity.Id);
    }

    public Response Delete(int tagId, bool force = false)
    {
        var tag = _context.Tags.Include(t => t.WorkItems).SingleOrDefault(t => t.Id == tagId );
        
        if (tag == null)
        {
            return Response.NotFound;
        }
        
        if (!force)
        {
            return Response.Conflict;
        }

        if (force)
        {
            foreach (var t in tag.WorkItems)
            {
                if (t.State == State.Active) 
                {
                    return Response.Conflict;
                }
            }
            foreach (var t in tag.WorkItems)
            {
                t.Tags.Remove(tag);
            }
        }

        _context.Tags.Remove(tag);
        _context.SaveChanges();
            
        return Response.Deleted;   
    }

    TagDTO ITagRepository.Find(int tagId)
    {
        var entity = _context.Tags.FirstOrDefault(c => c.Id == tagId);
        
        if (entity is null) 
        {
            return null;
        }

        return new TagDTO(tagId, entity.Name);
    }

    IReadOnlyCollection<TagDTO> ITagRepository.Read()
    {
        List<TagDTO> list = new List<TagDTO>();
        var entity = _context.Tags;
        
        foreach (var e in entity) 
        {
            list.Add(new TagDTO(e.Id, e.Name));
        }

        return list;
    }

    Response ITagRepository.Update(TagUpdateDTO tag)
    {
        var entity = _context.Tags.FirstOrDefault(c => c.Id == tag.Id);

        if (entity is null) 
        {
            return Response.NotFound;
        } 
        else 
        {
            entity.Name = tag.Name;
            _context.SaveChanges();
            
            return Response.Updated;
        }
    }
}