namespace MiniETicaret.Products.WebAPI.Dtos
{
    public sealed record ChangeProductStockDtos(
        Guid ProductId,
        int Quantity
        );

}
