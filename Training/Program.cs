using Training.Data;
using Training.Extensions;
using Training.Helpers;
using Training.Services;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// CORS
builder.Services.AddCors((options) =>           // CORS: Cross-Origin Resource Sharing. Controlls which apps can communicate with our API and how
{
    options.AddPolicy("DevCors", (corsBuilder) =>   //DEVELOPMENT, name of the new policy and behavior
    {
        corsBuilder.WithOrigins("http://localhost:4200", "http://localhost:3000", "http://localhost:8000")  //4200 default for Angular, 3000 for React, 8000 for View
            .AllowAnyMethod()   // Not only GET verb, but also PUT, DELETE, POST, ...
            .AllowAnyHeader()   // Allow any type of headers
            .AllowCredentials();// i.e. authentication tocken
    });
    options.AddPolicy("ProdCors", (corsBuilder) =>  // PRODUCTION, basically origin changes
    {
        corsBuilder.WithOrigins("https://myProductionSite.com")     // https, safer than http
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Swagger
builder.Services.AddEndpointsApiExplorer();     // Prepares metadata and throw them to Swagger
builder.Services.AddSwaggerGen();               // Generates the info Swagger needs

// Dependency injection
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<DataContextDapper>();
builder.Services.AddScoped<AuthHelper>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AuthService>();

// Token validation
builder.Services.AddJwtAuthentication(builder.Configuration);   // Using JwtExtensions.AddJwtAuthentication()

// ### BUILD ###
var app = builder.Build();

app.AddExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseCors("DevCors");
    app.UseSwagger();
    app.UseSwaggerUI();
}
else if (app.Environment.IsEnvironment("Testing"))
{
    app.UseCors("DevCors");
}
else    // Production, Staging, ...
{
    app.UseHttpsRedirection();
    app.UseCors("ProdCors");
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();