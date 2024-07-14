# 🚀Master .NET 8 Web API with MongoDB in Docker!🛠️ Step-by-Step Tutorial for Scalable Solutions📈
  <header>
        <h1>🎉 Welcome back, Netcode-Hub community! 🚀</h1>
    </header>
    <section>
        <p>
            In today's video, we're diving into the exciting world of Docker and MongoDB! If you've been following our series, you know we've already covered how to run .NET Web API with SQLite, SQL Server, and PostgreSQL. Today, we're taking it a step further by showing you how to connect your .NET Web API to MongoDB, all within a Docker container! 🌐💻
        </p>
    </section>
    <section>
        <h2>Scenario</h2>
        <p>
            Imagine you’re working on a project that involves handling a large amount of unstructured data—like logs, real-time analytics, or IoT data. MongoDB, a NoSQL database, is perfect for this scenario due to its flexible schema design. By running your .NET Web API connected to MongoDB in a Docker container, you ensure a consistent environment across all stages of development, testing, and production. This setup allows you to quickly spin up instances, making your development workflow much more efficient and scalable.
        </p>
    </section>
    <section>
        <h2>Summary</h2>
        <p>In this tutorial, we'll walk you through:</p>
        <ol>
            <li><strong>Setting Up MongoDB in Docker:</strong> We’ll start by pulling the official MongoDB image from Docker Hub and setting up a container.</li>
            <li><strong>Configuring .NET Web API:</strong> Next, we'll modify our .NET Web API project to connect to the MongoDB instance.</li>
            <li><strong>Running Everything Together:</strong> Finally, we'll use Docker Compose to orchestrate both the MongoDB and .NET Web API containers, ensuring seamless communication between them.</li>
        </ol>
    </section>

# Setup Connections
    {
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft.AspNetCore": "Warning"
        }
      },
      "AllowedHosts": "*",
      "ConnectionStrings": {
        "DefaultConnection": "mongodb://mongo:27017"
      },
      "MongoDB": {
        "DatabaseName": "MyMongoDatabase",
        "CollectionName": "MyMongoCollection"
      }
    }

# Install the packages
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="x.x.x" />
    <PackageReference Include="MongoDB.Driver" Version="x.x.x" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="x.x.x" />

# Create Model
    public class Product
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        [BsonElement("Name")]
        public string? Name { get; set; }
        [BsonElement("Description")]
        public string? Description { get; set; }
        [BsonElement("Quantity")]
        public int Quantity { get; set; }
    }

  # Create DB Context Class
       public class ProductDbContext
     {
         private readonly IMongoDatabase _database;
         private readonly IConfiguration Configuration;
         public ProductDbContext(IConfiguration configuration)
         {
             Configuration = configuration;
             string connectionString = Configuration.GetConnectionString("DefaultConnection")!;
             string databaseName = Configuration.GetSection("MongoDB:DatabaseName").Value!;
             var client = new MongoClient(connectionString);
             _database = client.GetDatabase(databaseName);
         }
         public IMongoCollection<Product> Products =>
             _database.GetCollection<Product>(Configuration.GetSection("MongoDB:CollectionName").Value!);
     }

# Create Product Endpoint
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

  # Create Docker File
      # See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.
    
    # This stage is used when running from VS in fast mode (Default for Debug configuration)
    FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
    USER app
    WORKDIR /app
    EXPOSE 80
    
    # This stage is used to build the service project
    FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
    ARG BUILD_CONFIGURATION=Release
    WORKDIR /src
    COPY ["DemoWebAPIWithMongoDbInDocker/DemoWebAPIWithMongoDbInDocker.csproj", "DemoWebAPIWithMongoDbInDocker/"]
    RUN dotnet restore "./DemoWebAPIWithMongoDbInDocker/DemoWebAPIWithMongoDbInDocker.csproj"
    COPY . .
    WORKDIR "/src/DemoWebAPIWithMongoDbInDocker"
    RUN dotnet build "./DemoWebAPIWithMongoDbInDocker.csproj" -c $BUILD_CONFIGURATION -o /app/build
    
    # This stage is used to publish the service project to be copied to the final stage
    FROM build AS publish
    ARG BUILD_CONFIGURATION=Release
    RUN dotnet publish "./DemoWebAPIWithMongoDbInDocker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
    
    # This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
    FROM base AS final
    WORKDIR /app
    COPY --from=publish /app/publish .
    ENTRYPOINT ["dotnet", "DemoWebAPIWithMongoDbInDocker.dll"]

# Create Docker-Compose File
    services:
      webapi:
        build: 
          context: .
          dockerfile: Dockerfile
        image: "myapi_mongo:latest"
        ports:
          - "5003:80"
        environment:
          - ASPNETCORE_URLS=http://+:80;
          - ASPNETCORE_ENVIRONMENT=Development
          - ConnectionStrings__DefaultConnection=mongodb://mongo:27017
          - MongoDB__DatabaseName=MyMongoDatabase
          - MongoDB__CollectionName=MyMongoCollection
        depends_on:
          - mongo
        networks:
          - my_custom_network
      mongo:
        image: mongo:latest
        ports:
          - "27017:27017"
        volumes:
          - mongo-data:/data/db
        networks:
          - my_custom_network
    networks:
      my_custom_network:
        external: false
    
    volumes:
      mongo-data:

  # Register Service And CORS
    builder.Services.AddSingleton<ProductDbContext>();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAllOrigins",
            builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
    });
    var app = builder.Build();
    
    // Configure the HTTP request pipeline.
    
        app.UseSwagger();
        app.UseSwaggerUI();
    
    app.UseCors("AllowAllOrigins");
    //app.UseHttpsRedirection();
    
    app.UseAuthorization();
    app.MapControllers();
    app.MapProductEndpoints();
    app.Run();

 <footer>
        <h2>Conclusion</h2>
        <p>
            Wow, we did it! 🚀 You've now mastered how to run your .NET Web API connected to MongoDB using Docker. This setup is incredibly powerful, providing you with a robust, scalable, and consistent environment that's perfect for handling large volumes of unstructured data.
        </p>
        <p>In this tutorial, we've covered:</p>
        <ol>
            <li><strong>Setting Up MongoDB in Docker:</strong> From pulling the image to running the container.</li>
            <li><strong>Configuring .NET Web API:</strong> Connecting our API to the MongoDB instance.</li>
            <li><strong>Orchestrating with Docker Compose:</strong> Ensuring seamless communication between services.</li>
        </ol>
        <p>
            By implementing this, you’ve taken a significant step towards modernizing your development workflow. Whether you’re building real-time analytics applications, managing IoT data, or handling large logs, this Docker and MongoDB setup ensures that you’re ready to tackle any challenge with efficiency and scalability.
        </p>
    </footer>
    
<div class="social-media">
    <a href="https://buymeacoffee.com/NetcodeHub"><span>☕</span>Buy Me a Coffee</a> |
    <a href="https://twitter.com/NetcodeHub"><span>🐦</span>Twitter</a> |
    <a href="https://www.linkedin.com/in/netcode-hub-90b188258/"><span>🔗</span>LinkedIn</a> |
    <a href="https://web.facebook.com/profile.php?id=100093980124689"><span>📘</span>Facebook</a> |
    <a href="https://www.youtube.com/channel/UCgRdOdIfOnTC_zV60aH5rRw"><span>📺</span>Netcode-Hub</a> |
    <a href="https://www.youtube.com/channel/UC98Qmaj6RNUWCZKRy65Qy7A"><span>📺</span>Code-Academy</a> |
    <a href="https://netcodehub.bloggerspot.com/"><span>📝</span>Blog</a>
</div>
