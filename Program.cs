using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// InMemory-tietokanta
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseInMemoryDatabase("SecureNotesDb"));

// JWT-autentikointi
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("supersecretkey123"))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ✅ CORS: Salli frontend localhost:5500
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://127.0.0.1:5500")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// === API-reitit ===

app.MapPost("/register", async ([FromBody] User user, AppDbContext db) =>
{
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return Results.Ok("User registered");
});

app.MapPost("/login", async ([FromBody] User login, AppDbContext db) =>
{
    Console.WriteLine($"Login yritetty: {login.Username}, {login.Password}");

    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == login.Username && u.Password == login.Password);
    if (user == null) return Results.Unauthorized();

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Username)
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("supersecretkey123"));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(claims: claims, expires: DateTime.Now.AddHours(1), signingCredentials: creds);

    return Results.Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
});


app.MapGet("/notes", [Authorize] async (ClaimsPrincipal user, AppDbContext db) =>
{
    int userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    var notes = await db.Notes.Where(n => n.UserId == userId).ToListAsync();
    return Results.Ok(notes);
});

app.MapPost("/notes", [Authorize] async ([FromBody] Note note, ClaimsPrincipal user, AppDbContext db) =>
{
    note.UserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    db.Notes.Add(note);
    await db.SaveChangesAsync();
    return Results.Ok(note);
});

app.MapDelete("/notes/{id}", [Authorize] async (int id, ClaimsPrincipal user, AppDbContext db) =>
{
    int userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    var note = await db.Notes.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
    if (note == null) return Results.NotFound();

    db.Notes.Remove(note);
    await db.SaveChangesAsync();
    return Results.Ok("Deleted");
});

app.Run();
