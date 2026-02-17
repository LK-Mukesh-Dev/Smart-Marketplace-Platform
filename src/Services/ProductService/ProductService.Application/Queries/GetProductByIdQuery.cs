namespace ProductService.Application.Queries;

public class GetProductByIdQuery
{
    public Guid Id { get; set; }

    public GetProductByIdQuery(Guid id)
    {
        Id = id;
    }
}
