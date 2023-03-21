namespace Grocy.RestAPI.Models;

public record Entity
{
    protected Entity()
    {

    }
    
    public Entity(int Id)
    {
        this.Id = Id;
    }

    public int Id { get; init; }
}