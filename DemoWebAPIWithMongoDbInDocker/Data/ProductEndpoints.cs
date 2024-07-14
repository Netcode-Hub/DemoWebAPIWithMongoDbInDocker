using Microsoft.AspNetCore.Http.HttpResults;
using MongoDB.Driver;
namespace DemoWebAPIWithMongoDbInDocker.Data
{
    public static class ProductEndpoints
    {
        public static void MapProductEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/Product").WithTags(nameof(Product));
            group.MapGet("/", async (ProductDbContext db) =>
            {
                return await db.Products.FindAsync(_ => true).Result.ToListAsync();
            })
           .WithName("GetAllProducts")
           .WithOpenApi();

            group.MapGet("/{id}", async Task<Results<Ok<Product>, NotFound>> (string id, ProductDbContext db) =>
            {
                var filter = Builders<Product>.Filter.Eq(p => p.Id, id);
                var data = await db.Products.Find(filter).FirstOrDefaultAsync();
                return TypedResults.Ok(data);
            })
           .WithName("GetProductById")
           .WithOpenApi();

            group.MapPut("/{id}", async Task<Results<Ok, NotFound>> (string id, Product product, ProductDbContext db) =>
            {

                var affected = await db.Products.ReplaceOneAsync(m => m.Id == id, product);
                if (affected.MatchedCount > 0)
                    return TypedResults.Ok();
                else return TypedResults.NotFound();
            })
           .WithName("UpdateProduct")
           .WithOpenApi();

            group.MapPost("/", async (Product product, ProductDbContext db) =>
            {
                await db.Products.InsertOneAsync(product);
                return TypedResults.Created($"/api/Product/{product.Id}", product);
            })
           .WithName("CreateProduct")
           .WithOpenApi();

            group.MapDelete("/{id}", async Task<Results<Ok, NotFound>> (string id, ProductDbContext db) =>
            {
                var result = await db.Products.DeleteOneAsync(model => model.Id == id);
                if (result.DeletedCount > 0)
                    return TypedResults.Ok();
                else return TypedResults.NotFound();
            })
          .WithName("DeleteProduct")
          .WithOpenApi();
        }
    }
}
